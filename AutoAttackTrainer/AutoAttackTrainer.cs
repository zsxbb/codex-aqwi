using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Pixelplacement;
using UnityEngine;
using HarmonyLib;

public class AutoAttackTrainer : MonoBehaviour
{
	public static AutoAttackTrainer Instance { get; private set; }

	public bool isEnabled
	{
		get
		{
			return this._isEnabled;
		}
		set
		{
			bool flag = this._isEnabled != value;
			if (flag)
			{
				this._isEnabled = value;
				bool flag2 = !this._isEnabled;
				if (flag2)
				{
					this.ResetAllFeatures();
				}
			}
		}
	}

	private void ResetAllFeatures()
	{
		this.autoTarget = false;
		this.targetByName = false;
		this.autoMove = false;
		this.autoAttackSlot0 = false;
		this.autoSkills = false;
		this.infiniteRange = false;
		this.keepCellOnDeath = false;
		this.autoLoot = false;
		this.lootMode = AutoAttackTrainer.LootMode.None;
		this.maidEnabled = false;
		this.maidActive = false;
		this.noclip = false;
	}

	public bool maidEnabled
	{
		get
		{
			return this._maidEnabled;
		}
		set
		{
			bool flag = this._maidEnabled != value;
			if (flag)
			{
				this._maidEnabled = value;
				bool flag2 = !this._maidEnabled;
				if (flag2)
				{
					this.maidActive = false;
				}
			}
		}
	}

	public bool maidActive
	{
		get
		{
			return this._maidActive;
		}
		set
		{
			bool flag = this._maidActive != value;
			if (flag)
			{
				this._maidActive = value;
				bool maidActive = this._maidActive;
				if (maidActive)
				{
					this.gotoTimer = 2f;
				}
			}
		}
	}

	private void Awake()
	{
		bool flag = AutoAttackTrainer.Instance == null;
		if (flag)
		{
			AutoAttackTrainer.Instance = this;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			try
			{
				var harmony = new Harmony("com.codex.trainer");
				harmony.PatchAll(Assembly.GetExecutingAssembly());
				Debug.Log("[Trainer] Harmony patches applied successfully!");
			}
			catch (Exception ex)
			{
				Debug.LogError("[Trainer] Failed to initialize Harmony patches: " + ex.Message);
			}

			try
			{
				bool flag2 = ChatAction.chatActionConstructors != null;
				if (flag2)
				{
					bool flag4 = !ChatAction.chatActionConstructors.ContainsKey("codex");
					if (flag4)
					{
						ChatAction.chatActionConstructors["codex"] = ((List<string> p) => new TrainerCmd(p));
					}
					ChatAction.chatActionConstructors["dev"] = ((List<string> p) => new DevConsoleCmd(p));
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("[Trainer] Failed to register chat command: " + ex.Message);
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void Start()
	{
		this.activationKey = PlayerPrefs.GetString("Codex_ActivationKey", "");
		this.trainerCoroutine = base.StartCoroutine(this.TrainerLoop());
	}

	private void Update()
	{
		bool flag = Input.GetKeyDown(KeyCode.F11);
		if (flag)
		{
			this.ToggleMenu();
		}
		if (this.noclip && Entity.mainPlayer != null)
		{
			this.SetNoclip(true);
		}
	}

	public void ToggleMenu()
	{
		this.showMenu = !this.showMenu;
		bool flag = Singleton<UIChat>.Instance != null;
		if (flag)
		{
			Singleton<UIChat>.Instance.SetText(this.showMenu ? "<color=green>[Codex] Draggable menu is now visible.</color>" : "<color=yellow>[Codex] Draggable menu hidden. Press F11 to show it.</color>");
		}
	}

	public static bool InterceptChat(string s)
	{
		if (string.IsNullOrEmpty(s)) return false;
		string[] parts = s.Trim().Split(' ');
		if (parts.Length == 0) return false;
		string cmd = parts[0].Trim('/').ToLower();
		if (cmd == "codex")
		{
			if (AutoAttackTrainer.Instance != null)
			{
				AutoAttackTrainer.Instance.ToggleMenu();
			}
			else
			{
				if (Singleton<UIChat>.Instance != null)
				{
					Singleton<UIChat>.Instance.SetText("<color=red>[Codex] Error: AutoAttackTrainer instance is missing.</color>");
				}
			}
			return true;
		}
		if (cmd == "dev")
		{
			UnityEngine.Object.Instantiate<GameObject>((GameObject)Resources.Load("UI/DevConsole"));
			return true;
		}
		return false;
	}

	public static bool IsRangeBypassed(Entity src)
	{
		return Instance != null && Instance.infiniteRange && src != null && src.isMainPlayer;
	}

	private IEnumerator TrainerLoop()
	{
		for (;;)
		{
			yield return new WaitForSeconds(this.tickRate);
			bool flag = !this.isEnabled || !this.isActivated;
			if (!flag)
			{
				Player mainPlayer = Entity.mainPlayer;
				bool flag2 = mainPlayer == null || mainPlayer.getGameObject() == null;
				if (!flag2)
				{
					bool flag3 = mainPlayer.currentState == Entity.State.Dead;
					if (!flag3)
					{
						bool shouldCombat = true;
						bool flag4 = this.maidEnabled && this.maidActive && !string.IsNullOrEmpty(this.maidTargetUsername);
						if (flag4)
						{
							Player targetPlayer = Entity.AllPlayers().FirstOrDefault((Player p) => p.Name.Equals(this.maidTargetUsername, StringComparison.OrdinalIgnoreCase));
							bool flag5 = targetPlayer == null || targetPlayer.Frame != mainPlayer.Frame;
							if (flag5)
							{
								shouldCombat = false;
								
								// Stop targeting and auto-attacks
								if (mainPlayer.target != null)
								{
									mainPlayer.target = null;
								}
								
								if (Singleton<Combat>.Instance != null)
								{
									Combat combatInstance = Singleton<Combat>.Instance;
									FieldInfo autoAttackField = typeof(Combat).GetField("autoAttackController", BindingFlags.Instance | BindingFlags.NonPublic);
									if (autoAttackField != null)
									{
										AutoAttackController autoAttackCtrl = autoAttackField.GetValue(combatInstance) as AutoAttackController;
										if (autoAttackCtrl != null)
										{
											autoAttackCtrl.Stop();
										}
									}
									if (combatInstance.ExecutionState != null)
									{
										combatInstance.ExecutionState.ClearAll();
									}
								}

								// Check if we are actually in combat
								bool inCombat = mainPlayer.currentState == Entity.State.Combat;
								
								this.gotoTimer += this.tickRate;
								if (this.gotoTimer >= 1.5f)
								{
									this.gotoTimer = 0f;
									if (Singleton<AEC>.Instance != null)
									{
										if (inCombat)
										{
											// We must run to a different cell or starting cell ("Enter") to drop combat aggro
											Singleton<AEC>.Instance.sendRequest(new RequestMoveToCell("Enter", "Spawn"));
										}
										else
										{
											// Safely transition to the master player now that we are out of combat
											Singleton<AEC>.Instance.sendRequest(new RequestGoToPlayer(this.maidTargetUsername));
										}
									}
								}
							}
							else
							{
								// Keep the timer primed so we warp quickly when the master leaves
								this.gotoTimer = 1.3f;
							}
							targetPlayer = null;
						}
						bool flag15 = shouldCombat && this.autoTarget;
						if (flag15)
						{
							Entity target = mainPlayer.target;
							bool flag16 = target == null || target.currentState == Entity.State.Dead || target.reactionType != Entity.ReactionType.Hostile;
							if (flag16)
							{
								this.TargetMonster(mainPlayer);
							}
							target = null;
						}
						bool flag17 = shouldCombat && this.autoMove && mainPlayer.target != null && mainPlayer.target.currentState > Entity.State.Dead;
						if (flag17)
						{
							this.CheckAndMoveToTarget(mainPlayer);
						}
						bool flag18 = shouldCombat && mainPlayer.target != null && mainPlayer.target.currentState > Entity.State.Dead;
						if (flag18)
						{
							bool flag19 = this.autoAttackSlot0;
							if (flag19)
							{
								this.UseSkillIfReady(0);
							}
							bool flag20 = this.autoSkills;
							if (flag20)
							{
								int num;
								for (int i = 1; i <= 4; i = num + 1)
								{
									this.UseSkillIfReady(i);
									num = i;
								}
							}
						}
						bool flag21 = this.autoLoot && this.lootMode != AutoAttackTrainer.LootMode.None && Game.lootItems != null && Game.lootItems.hasLoot();
						if (flag21)
						{
							this.AutoLootDrops();
						}
						mainPlayer = null;
					}
				}
			}
		}
	}

	private void TargetMonster(Player mainPlayer)
	{
		bool flag = Area.currentArea == null || Area.currentArea.Monsters == null;
		if (!flag)
		{
			Entity entity = null;
			float num = float.MaxValue;
			Vector3 position = mainPlayer.getGameObject().transform.position;
			string[] array = null;
			bool flag2 = this.targetByName && !string.IsNullOrWhiteSpace(this.targetSearchNames);
			if (flag2)
			{
				array = this.targetSearchNames.Split(new char[]
				{
					','
				}, StringSplitOptions.RemoveEmptyEntries);
			}
			foreach (Monster monster in Area.currentArea.Monsters.Values)
			{
				bool flag3 = monster == null || monster.getGameObject() == null;
				if (!flag3)
				{
					bool flag4 = monster.Frame == mainPlayer.Frame && monster.currentState != Entity.State.Dead && monster.reactionType == Entity.ReactionType.Hostile;
					if (flag4)
					{
						bool flag5 = this.targetByName;
						if (flag5)
						{
							bool flag6 = array == null || array.Length == 0;
							if (flag6)
							{
								continue;
							}
							bool flag7 = false;
							foreach (string text in array)
							{
								bool flag8 = monster.Name != null && monster.Name.IndexOf(text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
								if (flag8)
								{
									flag7 = true;
									break;
								}
							}
							bool flag9 = !flag7;
							if (flag9)
							{
								continue;
							}
						}
						float num2 = Vector3.Distance(position, monster.getGameObject().transform.position);
						bool flag10 = num2 < num;
						if (flag10)
						{
							num = num2;
							entity = monster;
						}
					}
				}
			}
			bool flag11 = entity != null;
			if (flag11)
			{
				mainPlayer.target = entity;
				Targetable component = entity.getGameObject().GetComponent<Targetable>();
				bool flag12 = component != null;
				if (flag12)
				{
					component.newTarget();
				}
			}
		}
	}

	private void CheckAndMoveToTarget(Player mainPlayer)
	{
		Entity target = mainPlayer.target;
		bool flag = target == null || target.getGameObject() == null;
		if (!flag)
		{
			float num = Singleton<Combat>.Instance.autoHRange;
			float num2 = Singleton<Combat>.Instance.autoVRange;
			bool flag2 = num <= 0f;
			if (flag2)
			{
				num = 8f;
			}
			bool flag3 = num2 <= 0f;
			if (flag3)
			{
				num2 = 5f;
			}
			bool flag4 = !Singleton<Combat>.Instance.isTargetInRange(mainPlayer, target, num, num2);
			if (flag4)
			{
				mainPlayer.Charge(new int?(0), 0f, 0f, false);
			}
		}
	}


	private void UseSkillIfReady(int slotNum)
	{
		UISkillSlots instance = Singleton<UISkillSlots>.Instance;
		bool flag = instance == null;
		if (!flag)
		{
			SkillSlotButton slot = instance.GetSlot(slotNum);
			bool flag2 = slot == null || slot.sk == null;
			if (!flag2)
			{
				bool flag3 = slot.cooldown != null && slot.cooldown.cooldownActive();
				if (!flag3)
				{
					bool flag4 = slot.blockerImage != null && slot.blockerImage.activeSelf;
					if (!flag4)
					{
						slot.UseSkill(false);
					}
				}
			}
		}
	}

	private void AutoLootDrops()
	{
		try
		{
			bool flag = this.lootMode == AutoAttackTrainer.LootMode.None;
			if (!flag)
			{
				List<InventoryItem> lootList = Game.lootItems.getLootList();
				bool flag2 = lootList != null && lootList.Count > 0;
				if (flag2)
				{
					foreach (InventoryItem inventoryItem in lootList)
					{
						bool flag3 = inventoryItem != null;
						if (flag3)
						{
							bool flag4 = this.lootMode == AutoAttackTrainer.LootMode.SelectDrop;
							if (flag4)
							{
								bool flag5 = string.IsNullOrWhiteSpace(this.lootSearchNames);
								if (flag5)
								{
									continue;
								}
								string[] array = this.lootSearchNames.Split(new char[]
								{
									','
								}, StringSplitOptions.RemoveEmptyEntries);
								bool flag6 = false;
								foreach (string text in array)
								{
									bool flag7 = inventoryItem.Name != null && inventoryItem.Name.IndexOf(text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
									if (flag7)
									{
										flag6 = true;
										break;
									}
								}
								bool flag8 = !flag6;
								if (flag8)
								{
									continue;
								}
							}
							Game.lootItems.MoveToInv(inventoryItem);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Error in AutoLoot: " + ex.Message);
		}
	}

	private void UpdateBackgroundTexture()
	{
		if (privateBgTexture == null)
		{
			privateBgTexture = new Texture2D(1, 1);
		}
		if (this.windowOpacity != lastOpacity)
		{
			lastOpacity = this.windowOpacity;
			privateBgTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, this.windowOpacity));
			privateBgTexture.Apply();
		}
	}

	private void OnDestroy()
	{
		if (privateBgTexture != null)
		{
			Destroy(privateBgTexture);
		}
	}

	private void OnGUI()
	{
		bool flag = !this.showMenu;
		if (!flag)
		{
			Event current = Event.current;
			bool flag2 = current != null && !this.isMinimized;
			if (flag2)
			{
				Rect rect = new Rect(this.windowRect.x + this.windowRect.width - 20f, this.windowRect.y + this.windowRect.height - 20f, 20f, 20f);
				bool flag3 = current.type == EventType.MouseDown && rect.Contains(current.mousePosition);
				if (flag3)
				{
					this.isResizing = true;
					this.resizeStartMousePos = current.mousePosition;
					this.resizeStartRect = this.windowRect;
					current.Use();
				}
				bool flag4 = this.isResizing;
				if (flag4)
				{
					bool flag5 = current.type == EventType.MouseDrag;
					if (flag5)
					{
						float num = current.mousePosition.x - this.resizeStartMousePos.x;
						float num2 = current.mousePosition.y - this.resizeStartMousePos.y;
						this.windowWidth = Mathf.Clamp(this.resizeStartRect.width + num, 220f, 800f);
						this.windowHeight = Mathf.Clamp(this.resizeStartRect.height + num2, 250f, 1000f);
					}
					else
					{
						bool flag6 = current.type == EventType.MouseUp;
						if (flag6)
						{
							this.isResizing = false;
						}
					}
				}
			}
			if (this.isMinimized)
			{
				this.windowRect.width = 150f;
				this.windowRect.height = 60f;
			}
			else
			{
				this.windowRect.width = this.windowWidth;
				this.windowRect.height = this.windowHeight;
			}
			this.UpdateBackgroundTexture();
			if (privateWindowStyle == null)
			{
				privateWindowStyle = new GUIStyle(GUI.skin.window);
				privateWindowStyle.normal.background = privateBgTexture;
				privateWindowStyle.onNormal.background = privateBgTexture;
				privateWindowStyle.active.background = privateBgTexture;
				privateWindowStyle.onActive.background = privateBgTexture;
				privateWindowStyle.focused.background = privateBgTexture;
				privateWindowStyle.onFocused.background = privateBgTexture;
			}
			this.windowRect = GUI.Window(999, this.windowRect, new GUI.WindowFunction(this.DrawWindow), "Codex - AQW Infinity", privateWindowStyle);
		}
	}

	private void DrawWindow(int windowID)
	{
		GUI.DragWindow(new Rect(0f, 0f, this.windowRect.width, 25f));
		GUILayout.Space(10f);

		if (this.isMinimized)
		{
			if (GUILayout.Button("<b><color=green>Expand Codex</color></b>", Array.Empty<GUILayoutOption>()))
			{
				this.isMinimized = false;
				this.windowRect.width = this.windowWidth;
				this.windowRect.height = this.windowHeight;
			}
			return;
		}

		bool hasPlayer = Entity.mainPlayer != null && !string.IsNullOrEmpty(Entity.mainPlayer.Name);
		if (hasPlayer)
		{
			string expected = this.GenerateKey(Entity.mainPlayer.Name);
			this.isActivated = (this.activationKey.Trim() == expected);
		}
		else
		{
			this.isActivated = false;
		}

		if (!this.isActivated)
		{
			GUILayout.Label("<b><color=orange>Activation Required</color></b>", Array.Empty<GUILayoutOption>());
			GUILayout.Space(5f);
			if (hasPlayer)
			{
				GUILayout.Label($"Username: <color=cyan>{Entity.mainPlayer.Name}</color>", Array.Empty<GUILayoutOption>());
				GUILayout.Space(5f);
				GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				GUILayout.Label("Key:", new GUILayoutOption[] { GUILayout.Width(40f) });
				string newKey = GUILayout.TextField(this.activationKey, 5, Array.Empty<GUILayoutOption>());
				if (newKey != this.activationKey)
				{
					this.activationKey = newKey.Trim();
					PlayerPrefs.SetString("Codex_ActivationKey", this.activationKey);
					PlayerPrefs.Save();
				}
				GUILayout.EndHorizontal();
				
				GUILayout.Space(5f);
				if (!string.IsNullOrEmpty(this.activationKey))
				{
					GUILayout.Label("<color=red>Invalid activation key!</color>", Array.Empty<GUILayoutOption>());
				}
				else
				{
					GUILayout.Label("<color=yellow>Enter your 5-digit activation key.</color>", Array.Empty<GUILayoutOption>());
				}
			}
			else
			{
				GUILayout.Label("<color=yellow>Waiting for character to load...</color>", Array.Empty<GUILayoutOption>());
			}
			
			GUILayout.Space(15f);
			GUILayout.Label("<color=grey>Press <b>F11</b> to toggle menu visibility.</color>", Array.Empty<GUILayoutOption>());
			return;
		}

		string text = "Status: No Character Loaded";
		bool flag = Entity.mainPlayer != null;
		if (flag)
		{
			text = string.Format("Player: <color=cyan>{0}</color> (Cell: {1})", Entity.mainPlayer.Name, Entity.mainPlayer.Frame);
		}
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(text, new GUILayoutOption[]
		{
			GUILayout.Height(20f)
		});
		if (GUILayout.Button("<b><color=yellow>Minimize</color></b>", GUILayout.Width(75f), GUILayout.Height(20f)))
		{
			this.isMinimized = true;
			this.windowRect.width = 150f;
			this.windowRect.height = 60f;
		}
		GUILayout.EndHorizontal();
		string text2 = "Target: <color=yellow>None</color>";
		bool flag2 = Entity.mainPlayer != null && Entity.mainPlayer.target != null;
		if (flag2)
		{
			Entity target = Entity.mainPlayer.target;
			text2 = string.Format("Target: <color=orange>{0}</color> ({1}/{2} HP)", target.Name, target.HP, target.MaxHP);
		}
		GUILayout.Label(text2, new GUILayoutOption[]
		{
			GUILayout.Height(20f)
		});
		int num = (Game.lootItems != null) ? Game.lootItems.getLootCount() : 0;
		GUILayout.Label(string.Format("Loot Queue: <color=yellow>{0}</color> items", num), new GUILayoutOption[]
		{
			GUILayout.Height(20f)
		});
		GUILayout.Space(10f);
		this.isEnabled = GUILayout.Toggle(this.isEnabled, "  <b><color=green>ENABLE Codex</color></b>", Array.Empty<GUILayoutOption>());
		GUILayout.Space(5f);
		GUI.enabled = this.isEnabled;
		this.autoTarget = GUILayout.Toggle(this.autoTarget, "  Auto Target Monster", Array.Empty<GUILayoutOption>());
		bool flag3 = this.autoTarget;
		if (flag3)
		{
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Space(20f);
			bool flag4 = !this.targetByName;
			bool flag5 = this.targetByName;
			bool flag6 = GUILayout.Toggle(flag4, "Nearest", Array.Empty<GUILayoutOption>());
			bool flag7 = GUILayout.Toggle(flag5, "Keyword", Array.Empty<GUILayoutOption>());
			bool flag8 = flag6 != flag4;
			if (flag8)
			{
				this.targetByName = !flag6;
			}
			else
			{
				bool flag9 = flag7 != flag5;
				if (flag9)
				{
					this.targetByName = flag7;
				}
			}
			GUILayout.EndHorizontal();
			bool flag10 = this.targetByName;
			if (flag10)
			{
				GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				GUILayout.Space(20f);
				GUILayout.Label("Keywords:", new GUILayoutOption[]
				{
					GUILayout.Width(80f)
				});
				this.targetSearchNames = GUILayout.TextField(this.targetSearchNames, Array.Empty<GUILayoutOption>());
				GUILayout.EndHorizontal();
			}
		}
		this.autoMove = GUILayout.Toggle(this.autoMove, "  Auto Charge to Target", Array.Empty<GUILayoutOption>());
		bool combinedAuto = this.autoAttackSlot0 || this.autoSkills;
		bool newCombinedAuto = GUILayout.Toggle(combinedAuto, "  Auto Attack", Array.Empty<GUILayoutOption>());
		if (newCombinedAuto != combinedAuto)
		{
			this.autoAttackSlot0 = newCombinedAuto;
			this.autoSkills = newCombinedAuto;
		}
		this.infiniteRange = GUILayout.Toggle(this.infiniteRange, "  Infinite Range", Array.Empty<GUILayoutOption>());
		this.keepCellOnDeath = GUILayout.Toggle(this.keepCellOnDeath, "  Death-Respawn Cell", Array.Empty<GUILayoutOption>());
		GUILayout.Space(5f);
		GUILayout.Label("<b>Loot Settings:</b>", Array.Empty<GUILayoutOption>());
		this.autoLoot = GUILayout.Toggle(this.autoLoot, "  Auto Loot Drops", Array.Empty<GUILayoutOption>());
		bool flag11 = this.autoLoot;
		if (flag11)
		{
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Space(20f);
			bool flag12 = this.lootMode == AutoAttackTrainer.LootMode.SelectDrop;
			bool flag13 = this.lootMode == AutoAttackTrainer.LootMode.AllDrop;
			bool flag14 = GUILayout.Toggle(flag12, "SELECT DROP", Array.Empty<GUILayoutOption>());
			bool flag15 = GUILayout.Toggle(flag13, "ALL DROP", Array.Empty<GUILayoutOption>());
			bool flag16 = flag14 && !flag12;
			if (flag16)
			{
				this.lootMode = AutoAttackTrainer.LootMode.SelectDrop;
			}
			else
			{
				bool flag17 = !flag14 && flag12;
				if (flag17)
				{
					this.lootMode = AutoAttackTrainer.LootMode.None;
				}
			}
			bool flag18 = flag15 && !flag13;
			if (flag18)
			{
				this.lootMode = AutoAttackTrainer.LootMode.AllDrop;
			}
			else
			{
				bool flag19 = !flag15 && flag13;
				if (flag19)
				{
					this.lootMode = AutoAttackTrainer.LootMode.None;
				}
			}
			GUILayout.EndHorizontal();
			bool flag20 = this.lootMode == AutoAttackTrainer.LootMode.SelectDrop;
			if (flag20)
			{
				GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				GUILayout.Space(20f);
				GUILayout.Label("Keywords:", new GUILayoutOption[]
				{
					GUILayout.Width(100f)
				});
				this.lootSearchNames = GUILayout.TextField(this.lootSearchNames, Array.Empty<GUILayoutOption>());
				GUILayout.EndHorizontal();
			}
		}
		else
		{
			this.lootMode = AutoAttackTrainer.LootMode.None;
		}
		GUILayout.Space(5f);
		GUILayout.Label("<b>MAID Settings:</b>", Array.Empty<GUILayoutOption>());
		this.maidEnabled = GUILayout.Toggle(this.maidEnabled, "  Feature Maid (Px)", Array.Empty<GUILayoutOption>());
		bool maidEnabled = this.maidEnabled;
		if (maidEnabled)
		{
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Space(20f);
			GUILayout.Label("Goto:", new GUILayoutOption[]
			{
				GUILayout.Width(90f)
			});
			this.maidTargetUsername = GUILayout.TextField(this.maidTargetUsername, Array.Empty<GUILayoutOption>());
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Space(20f);
			this.maidActive = GUILayout.Toggle(this.maidActive, "  Active", Array.Empty<GUILayoutOption>());
			GUILayout.EndHorizontal();
		}
		GUILayout.Space(10f);
		GUILayout.Label(string.Format("Bot Tick Rate: <b>{0:F2}s</b>", this.tickRate), Array.Empty<GUILayoutOption>());
		this.tickRate = GUILayout.HorizontalSlider(this.tickRate, 0.05f, 1f, Array.Empty<GUILayoutOption>());
		GUI.enabled = true;
		GUILayout.Space(5f);
		if (GUILayout.Button("<b><color=orange>Open Dev Console</color></b>", new GUILayoutOption[] { GUILayout.Height(25f) }))
		{
			UnityEngine.Object.Instantiate<GameObject>((GameObject)Resources.Load("UI/DevConsole"));
		}
		GUILayout.Space(5f);
		GUILayout.Label("<b>Developer / Debug Cheats:</b>", Array.Empty<GUILayoutOption>());
		bool newDevMode = GUILayout.Toggle(Game.DevMode, "  Enable Developer Mode", Array.Empty<GUILayoutOption>());
		if (newDevMode != Game.DevMode)
		{
			Game.DevMode = newDevMode;
			DebugSprite._showAll = newDevMode;
			if (Game.OnDevMode != null)
			{
				try
				{
					Game.OnDevMode(newDevMode);
				}
				catch {}
			}
		}
		this.noclip = GUILayout.Toggle(this.noclip, "  Noclip (Walk Through Walls)", Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button("Show Map Info", Array.Empty<GUILayoutOption>()))
		{
			if (Area.currentArea != null && Entity.mainPlayer != null && Singleton<UIChat>.Instance != null)
			{
				string info = string.Format("<b>Map Info:</b> ID {0} | Name: {1} | Cell: {2}", Area.currentArea.ID, Area.currentArea.Name, Entity.mainPlayer.Frame);
				Singleton<UIChat>.Instance.SetText(info);
			}
		}
		if (GUILayout.Button("List Map Cells", Array.Empty<GUILayoutOption>()))
		{
			if (Entity.mainPlayer != null && Entity.mainPlayer.currentArea != null && Singleton<UIChat>.Instance != null)
			{
				string cells = "<b>Cells:</b> " + string.Join(", ", Entity.mainPlayer.currentArea.Cells.Keys);
				Singleton<UIChat>.Instance.SetText(cells);
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(10f);
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label("<color=green>Activated</color> for: <b>" + Entity.mainPlayer.Name + "</b>", Array.Empty<GUILayoutOption>());
		if (GUILayout.Button("Reset Key", GUILayout.Width(80f)))
		{
			this.activationKey = "";
			this.isActivated = false;
			PlayerPrefs.DeleteKey("Codex_ActivationKey");
			PlayerPrefs.Save();
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(5f);
		GUILayout.Label("<b>UI Settings:</b>", Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label("Opacity:", new GUILayoutOption[]
		{
			GUILayout.Width(70f)
		});
		this.windowOpacity = GUILayout.HorizontalSlider(this.windowOpacity, 0f, 1f, Array.Empty<GUILayoutOption>());
		GUILayout.EndHorizontal();

		GUILayout.Space(15f);
		GUILayout.Label("<color=grey>Press <b>F11</b> to toggle menu visibility.\nType <b>/codex</b> to toggle.</color>", Array.Empty<GUILayoutOption>());
		GUILayout.Label("<b><color=white>creator: sxbb</color></b>", Array.Empty<GUILayoutOption>());
		GUI.Label(new Rect(this.windowRect.width - 15f, this.windowRect.height - 15f, 15f, 15f), "<color=grey>◢</color>");
	}

	private string activationKey = "";
	private bool isActivated = false;
	private bool isMinimized = false;

	private string GenerateKey(string username)
	{
		if (string.IsNullOrEmpty(username)) return "00000";
		string input = username.Trim().ToLower() + "codex_secret_2026";
		uint hash = 2166136261U;
		foreach (char c in input)
		{
			hash ^= (uint)c;
			hash *= 16777619U;
		}
		return ((hash % 90000U) + 10000U).ToString();
	}

	private bool _isEnabled = false;

	private bool _noclip = false;

	public bool noclip
	{
		get
		{
			return this._noclip;
		}
		set
		{
			if (this._noclip != value)
			{
				this._noclip = value;
				this.SetNoclip(value);
			}
		}
	}

	private void SetNoclip(bool enabled)
	{
		if (Entity.mainPlayer == null) return;
		try
		{
			MethodInfo method = typeof(Player).GetMethod("EnableCollider", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (method != null)
			{
				method.Invoke(Entity.mainPlayer, new object[] { !enabled });
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[Trainer] Failed to set noclip: " + ex.Message);
		}
	}

	public bool autoTarget = false;

	public bool targetByName = false;

	public string targetSearchNames = "";

	public bool autoAttackSlot0 = false;

	public bool autoSkills = false;

	public bool autoMove = false;

	public bool autoLoot = false;

	public float tickRate = 0.3f;

	public bool infiniteRange = false;
	public bool keepCellOnDeath = false;

	public AutoAttackTrainer.LootMode lootMode = AutoAttackTrainer.LootMode.None;

	public string lootSearchNames = "";

	private bool _maidEnabled = false;

	private bool _maidActive = false;

	public string maidTargetUsername = "";

	private bool showMenu = true;

	private Rect windowRect = new Rect(100f, 100f, 320f, 725f);

	private Coroutine trainerCoroutine;

	private float gotoTimer = 0f;

	public float windowOpacity = 0.95f;

	public float windowWidth = 320f;

	public float windowHeight = 725f;

	private bool isResizing = false;

	private Vector2 resizeStartMousePos;

	private Rect resizeStartRect;

	private Texture2D privateBgTexture;
	private GUIStyle privateWindowStyle;
	private float lastOpacity = -1f;

	public enum LootMode
	{
		None,
		SelectDrop,
		AllDrop
	}
}

[HarmonyPatch(typeof(UIChatActions), "SendChatMessage")]
public static class UIChatActions_SendChatMessage_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(string s)
	{
		if (AutoAttackTrainer.InterceptChat(s))
		{
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(Combat), "isTargetInRange")]
public static class Combat_isTargetInRange_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(Entity src, Entity tgt, float hRange, float vRange, ref bool __result)
	{
		if (AutoAttackTrainer.Instance != null && AutoAttackTrainer.Instance.infiniteRange && src != null && src.isMainPlayer)
		{
			__result = true;
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(ResponseResPlayer), "Execute")]
public static class ResponseResPlayer_Execute_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(ResponseResPlayer __instance)
	{
		if (AutoAttackTrainer.Instance != null && AutoAttackTrainer.Instance.isEnabled && AutoAttackTrainer.Instance.keepCellOnDeath)
		{
			Player player = Entity.getPlayer(__instance.unm);
			if (player == null)
			{
				string a = __instance.unm;
				Player mainPlayer = Entity.mainPlayer;
				if (a == ((mainPlayer != null) ? mainPlayer.Name : null))
				{
					player = Entity.mainPlayer;
				}
			}
			if (player != null)
			{
				player.Stats.MaxHP = __instance.MaxHP;
				player.HP = __instance.HP;
				player.RP = player.ResourceStart;
				player.resPlayer();
				if (player.isMainPlayer)
				{
					string currentFrame = string.IsNullOrEmpty(player.Frame) ? "Enter" : player.Frame;
					string currentPad = string.IsNullOrEmpty(player.Pad) ? "Spawn" : player.Pad;
					Singleton<AEC>.Instance.sendRequest(new RequestMoveToCell(currentFrame, currentPad));
				}
			}
			return false;
		}
		return true;
	}
}

// - sxbb
