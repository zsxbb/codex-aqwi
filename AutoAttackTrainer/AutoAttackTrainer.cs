using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Pixelplacement;
using UnityEngine;
using UnityEngine.EventSystems;
using HarmonyLib;

// Token: 0x02000006 RID: 6
public class AutoAttackTrainer : MonoBehaviour
{
	// Token: 0x17000001 RID: 1
	// (get) Token: 0x06000006 RID: 6 RVA: 0x000020A2 File Offset: 0x000002A2
	// (set) Token: 0x06000007 RID: 7 RVA: 0x000020A9 File Offset: 0x000002A9
	public static AutoAttackTrainer Instance { get; private set; }

	// Token: 0x17000002 RID: 2
	// (get) Token: 0x06000008 RID: 8 RVA: 0x000020B4 File Offset: 0x000002B4
	// (set) Token: 0x06000009 RID: 9 RVA: 0x000020CC File Offset: 0x000002CC
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
				if (!this._isEnabled)
				{
					if (this.noclip)
					{
						this.SetNoclip(false);
					}
					if (Entity.mainPlayer != null && Entity.mainPlayer.currentState != Entity.State.Dead)
					{
						Entity.mainPlayer.StopWalking();
					}
				}
				else
				{
					if (this.noclip)
					{
						this.SetNoclip(true);
					}
				}
			}
		}
	}

	// Token: 0x0600000A RID: 10 RVA: 0x0000210C File Offset: 0x0000030C
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

	// Token: 0x17000003 RID: 3
	// (get) Token: 0x0600000B RID: 11 RVA: 0x00002164 File Offset: 0x00000364
	// (set) Token: 0x0600000C RID: 12 RVA: 0x0000217C File Offset: 0x0000037C
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

	// Token: 0x17000004 RID: 4
	// (get) Token: 0x0600000D RID: 13 RVA: 0x000021BC File Offset: 0x000003BC
	// (set) Token: 0x0600000E RID: 14 RVA: 0x000021D4 File Offset: 0x000003D4
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

	// Token: 0x0600000F RID: 15 RVA: 0x00002214 File Offset: 0x00000414
	private void Awake()
	{
		bool flag = AutoAttackTrainer.Instance == null;
		if (flag)
		{
			AutoAttackTrainer.Instance = this;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			Loom.Initialize();
			base.gameObject.AddComponent<VoiceChatManager>();
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

	// Token: 0x06000010 RID: 16 RVA: 0x0000232C File Offset: 0x0000052C
	private void Start()
	{
		this.activationKey = PlayerPrefs.GetString("Codex_ActivationKey", "");
		this.trainerCoroutine = base.StartCoroutine(this.TrainerLoop());
	}

	// Token: 0x06000011 RID: 17 RVA: 0x00002344 File Offset: 0x00000544
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

	// Token: 0x06000012 RID: 18 RVA: 0x00002378 File Offset: 0x00000578
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

	// Token: 0x06000013 RID: 19 RVA: 0x000023C5 File Offset: 0x000005C5
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
						bool flag17 = shouldCombat && this.autoMove && !this.infiniteRange && mainPlayer.target != null && mainPlayer.target.currentState > Entity.State.Dead;
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

	// Token: 0x06000014 RID: 20 RVA: 0x000023D4 File Offset: 0x000005D4
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

	// Token: 0x06000015 RID: 21 RVA: 0x00002600 File Offset: 0x00000800
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


	// Token: 0x06000016 RID: 22 RVA: 0x000026A4 File Offset: 0x000008A4
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

	// Token: 0x06000017 RID: 23 RVA: 0x00002738 File Offset: 0x00000938
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

	// Token: 0x06000018 RID: 24 RVA: 0x000028CC File Offset: 0x00000ACC
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
				this.windowRect.width = 160f;
				if (VoiceChatManager.Instance != null && VoiceChatManager.Instance.voiceEnabled)
				{
					this.windowRect.height = 90f;
				}
				else
				{
					this.windowRect.height = 55f;
				}
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

	// Token: 0x06000019 RID: 25 RVA: 0x00002AB0 File Offset: 0x00000CB0
	private void DrawWindow(int windowID)
	{
		GUILayout.Space(10f);

		if (this.isMinimized)
		{
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			if (GUILayout.Button("<b><color=green>Expand</color></b>", GUILayout.Height(20f)))
			{
				this.isMinimized = false;
				this.windowRect.width = this.windowWidth;
				this.windowRect.height = this.windowHeight;
			}
			GUILayout.EndHorizontal();

			if (VoiceChatManager.Instance != null && VoiceChatManager.Instance.voiceEnabled)
			{
				GUILayout.Space(2f);
				GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				bool newMute = GUILayout.Toggle(VoiceChatManager.Instance.isMuted, " Mute", Array.Empty<GUILayoutOption>());
				if (newMute != VoiceChatManager.Instance.isMuted) VoiceChatManager.Instance.isMuted = newMute;

				bool newDeafen = GUILayout.Toggle(VoiceChatManager.Instance.isDeafened, " Deafen", Array.Empty<GUILayoutOption>());
				if (newDeafen != VoiceChatManager.Instance.isDeafened) VoiceChatManager.Instance.isDeafened = newDeafen;
				GUILayout.EndHorizontal();
			}
			GUI.DragWindow();
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
				if (Event.current.type == EventType.Repaint) this.activationKeyRect = GUILayoutUtility.GetLastRect();
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
			this.windowRect.width = 160f;
			if (VoiceChatManager.Instance != null && VoiceChatManager.Instance.voiceEnabled)
			{
				this.windowRect.height = 90f;
			}
			else
			{
				this.windowRect.height = 55f;
			}
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

		// Pagination Tab Selector
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(this.currentTab == 0 ? "<b><color=lime>Codex Cheats</color></b>" : "Codex Cheats", GUILayout.Height(22f))) this.currentTab = 0;
		if (GUILayout.Button(this.currentTab == 1 ? "<b><color=lime>Voice Chat</color></b>" : "Voice Chat", GUILayout.Height(22f))) this.currentTab = 1;
		if (GUILayout.Button(this.currentTab == 2 ? "<b><color=lime>Dev Card</color></b>" : "Dev Card", GUILayout.Height(22f))) this.currentTab = 2;
		GUILayout.EndHorizontal();
		GUILayout.Space(5f);

		if (this.currentTab == 0)
		{
			this.isEnabled = GUILayout.Toggle(this.isEnabled, "  <b><color=green>ENABLE Codex</color></b>", Array.Empty<GUILayoutOption>());
			GUILayout.Space(5f);
			GUI.enabled = this.isEnabled;

			bool isAutoAttacking = this.autoAttackSlot0 || this.autoSkills;
			bool newAutoAttacking = GUILayout.Toggle(isAutoAttacking, "  Auto Attack", Array.Empty<GUILayoutOption>());
			if (newAutoAttacking != isAutoAttacking)
			{
				this.autoAttackSlot0 = newAutoAttacking;
				this.autoSkills = newAutoAttacking;
				this.autoTarget = newAutoAttacking;
				this.autoMove = newAutoAttacking;
			}
			if (this.autoAttackSlot0 || this.autoSkills)
			{
				this.autoTarget = true;
				this.autoMove = true;

				GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				GUILayout.Space(20f);
				bool isNearest = !this.targetByName;
				bool isKeyword = this.targetByName;
				bool newNearest = GUILayout.Toggle(isNearest, "Nearest", Array.Empty<GUILayoutOption>());
				bool newKeyword = GUILayout.Toggle(isKeyword, "Keyword", Array.Empty<GUILayoutOption>());
				if (newNearest != isNearest)
				{
					this.targetByName = !newNearest;
				}
				else if (newKeyword != isKeyword)
				{
					this.targetByName = newKeyword;
				}
				GUILayout.EndHorizontal();

				if (this.targetByName)
				{
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
					GUILayout.Space(20f);
					GUILayout.Label("Keywords:", new GUILayoutOption[]
					{
						GUILayout.Width(80f)
					});
					this.targetSearchNames = GUILayout.TextField(this.targetSearchNames, Array.Empty<GUILayoutOption>());
					if (Event.current.type == EventType.Repaint) this.targetSearchRect = GUILayoutUtility.GetLastRect();
					GUILayout.EndHorizontal();
				}
			}
			else
			{
				this.autoTarget = false;
				this.autoMove = false;
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
					if (Event.current.type == EventType.Repaint) this.lootSearchRect = GUILayoutUtility.GetLastRect();
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
			bool maidEnabled2 = this.maidEnabled;
			if (maidEnabled2)
			{
				GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				GUILayout.Space(20f);
				GUILayout.Label("Goto:", new GUILayoutOption[]
				{
					GUILayout.Width(90f)
				});
				this.maidTargetUsername = GUILayout.TextField(this.maidTargetUsername, Array.Empty<GUILayoutOption>());
				if (Event.current.type == EventType.Repaint) this.maidTargetRect = GUILayoutUtility.GetLastRect();
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
		}
		else if (this.currentTab == 2)
		{
			GUILayout.Label("<b>Dev Tools & Utilities:</b>", Array.Empty<GUILayoutOption>());
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
			GUI.enabled = true;
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
		}
		else if (this.currentTab == 1)
		{
			GUILayout.Label("<b>Voice Chat Settings:</b>", Array.Empty<GUILayoutOption>());
			if (VoiceChatManager.Instance != null)
			{
				bool newVoice = GUILayout.Toggle(VoiceChatManager.Instance.voiceEnabled, "  Enable Voice Room (P2P)", Array.Empty<GUILayoutOption>());
				if (newVoice != VoiceChatManager.Instance.voiceEnabled)
				{
					VoiceChatManager.Instance.ToggleVoice(newVoice);
				}

				if (VoiceChatManager.Instance.voiceEnabled)
				{
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
					GUILayout.Space(20f);
					GUILayout.Label("Status: " + VoiceChatManager.Instance.connectionStatus, Array.Empty<GUILayoutOption>());
					GUILayout.EndHorizontal();

					// --- Mic Device Selector ---
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
					GUILayout.Space(20f);
					GUILayout.Label("Mic:", GUILayout.Width(40f));
					if (Microphone.devices.Length > 0)
					{
						int idx = VoiceChatManager.Instance.selectedMicIndex;
						string currentMic = (idx >= 0 && idx < Microphone.devices.Length) ? Microphone.devices[idx] : "Default";
						if (currentMic.Length > 22) currentMic = currentMic.Substring(0, 19) + "...";
						if (GUILayout.Button(currentMic, GUILayout.Height(18f)))
						{
							int nextIdx = (idx + 1) % Microphone.devices.Length;
							VoiceChatManager.Instance.ChangeMicrophone(nextIdx);
						}
					}
					else
					{
						GUILayout.Label("<color=red>No Microphone Found</color>");
					}
					GUILayout.EndHorizontal();

					// --- Mic Meter (VU Indicator) ---
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
					GUILayout.Space(20f);
					GUILayout.Label("Input Level:", GUILayout.Width(80f));
					int barWidth = (int)(VoiceChatManager.Instance.micLevel * 100f);
					if (barWidth > 100) barWidth = 100;
					string bar = new string('|', Math.Max(0, barWidth / 5));
					GUILayout.Label($"<color=lime>{bar}</color>", Array.Empty<GUILayoutOption>());
					GUILayout.EndHorizontal();

					// --- Speaker Volume Slider ---
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
					GUILayout.Space(20f);
					GUILayout.Label("Output Volume:", GUILayout.Width(100f));
					float newVol = GUILayout.HorizontalSlider(VoiceChatManager.Instance.voiceVolume, 0f, 1f, Array.Empty<GUILayoutOption>());
					if (newVol != VoiceChatManager.Instance.voiceVolume)
					{
						VoiceChatManager.Instance.SetVoiceVolume(newVol);
					}
					GUILayout.EndHorizontal();

					// Tombol jejer: mute, deafen, mictest
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
					GUILayout.Space(20f);
					bool newMute = GUILayout.Toggle(VoiceChatManager.Instance.isMuted, " Mute", Array.Empty<GUILayoutOption>());
					if (newMute != VoiceChatManager.Instance.isMuted) VoiceChatManager.Instance.isMuted = newMute;

					bool newDeafen = GUILayout.Toggle(VoiceChatManager.Instance.isDeafened, " Deafen", Array.Empty<GUILayoutOption>());
					if (newDeafen != VoiceChatManager.Instance.isDeafened) VoiceChatManager.Instance.isDeafened = newDeafen;

					bool newLoopback = GUILayout.Toggle(VoiceChatManager.Instance.isLoopback, " Mic Test", Array.Empty<GUILayoutOption>());
					if (newLoopback != VoiceChatManager.Instance.isLoopback) VoiceChatManager.Instance.isLoopback = newLoopback;
					GUILayout.EndHorizontal();

					// Voice Info button/toggle
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
					GUILayout.Space(20f);
					VoiceChatManager.Instance.showVoiceDebug = GUILayout.Toggle(VoiceChatManager.Instance.showVoiceDebug, " Voice Info", Array.Empty<GUILayoutOption>());
					GUILayout.EndHorizontal();

					if (VoiceChatManager.Instance.showVoiceDebug)
					{
						GUILayout.BeginVertical(GUI.skin.box, Array.Empty<GUILayoutOption>());
						// Status: CONNECTED or NO
						GUILayout.Label($"<b>Status:</b> {(VoiceChatManager.Instance.isTcpConnected ? "<color=green>CONNECTED</color>" : "<color=red>NO</color>")}", Array.Empty<GUILayoutOption>());
						
						// Show Active Peers ON OFF
						this.showActivePeers = GUILayout.Toggle(this.showActivePeers, " Show Active Peers", Array.Empty<GUILayoutOption>());
						if (this.showActivePeers)
						{
							string peersStr = "";
							List<string> peerNames = VoiceChatManager.Instance.GetActivePeerNames();
							if (peerNames != null)
							{
								foreach (var name in peerNames)
								{
									peersStr += name + ", ";
								}
							}
							if (peersStr.Length > 0) peersStr = peersStr.Substring(0, peersStr.Length - 2);
							else peersStr = "None";
							GUILayout.Label($"<b>Active Peers:</b> {peersStr}", Array.Empty<GUILayoutOption>());
						}
						GUILayout.EndVertical();
					}
				}
			}
			else
			{
				GUILayout.Label("<color=red>Voice Manager not initialized.</color>", Array.Empty<GUILayoutOption>());
			}
		}

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
		GUI.Label(new Rect(this.windowRect.width - 18f, this.windowRect.height - 18f, 18f, 18f), "<b><color=orange>◢</color></b>");
		Rect resizeZone = new Rect(this.windowRect.width - 25f, this.windowRect.height - 25f, 25f, 25f);
		if (!this.isResizing && !(Event.current.type == EventType.MouseDown && resizeZone.Contains(Event.current.mousePosition)))
		{
			Vector2 mousePos = Event.current.mousePosition;
			bool overInput = (this.activationKeyRect.Contains(mousePos) || 
							  this.targetSearchRect.Contains(mousePos) || 
							  this.lootSearchRect.Contains(mousePos) || 
							  this.maidTargetRect.Contains(mousePos));
			if (!overInput)
			{
				GUI.DragWindow();
			}
		}
	}

	private string activationKey = "";
	private bool isActivated = false;
	private bool isMinimized = false;
	private int currentTab = 0;
	private bool showActivePeers = false;

	private Rect activationKeyRect = Rect.zero;
	private Rect targetSearchRect = Rect.zero;
	private Rect lootSearchRect = Rect.zero;
	private Rect maidTargetRect = Rect.zero;

	private string GenerateKey(string username)
	{
		if (string.IsNullOrEmpty(username)) return "00000";
		string input = username.Trim().ToLower() + Config.ActivationHashKey;
		uint hash = 2166136261U;
		foreach (char c in input)
		{
			hash ^= (uint)c;
			hash *= 16777619U;
		}
		return ((hash % 90000U) + 10000U).ToString();
	}

	// Token: 0x04000005 RID: 5
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

	public static bool IsMouseOverTrainerUI()
	{
		if (AutoAttackTrainer.Instance == null || !AutoAttackTrainer.Instance.showMenu)
		{
			return false;
		}
		Vector2 mp = Input.mousePosition;
		Vector2 guiPos = new Vector2(mp.x, (float)Screen.height - mp.y);
		return AutoAttackTrainer.Instance.windowRect.Contains(guiPos);
	}

	public static bool IsPointOverTrainerUI(Vector2 screenPoint)
	{
		if (AutoAttackTrainer.Instance == null || !AutoAttackTrainer.Instance.showMenu)
		{
			return false;
		}
		Vector2 guiPos = new Vector2(screenPoint.x, (float)Screen.height - screenPoint.y);
		return AutoAttackTrainer.Instance.windowRect.Contains(guiPos);
	}

	// Token: 0x04000006 RID: 6
	public bool autoTarget = false;

	// Token: 0x04000007 RID: 7
	public bool targetByName = false;

	// Token: 0x04000008 RID: 8
	public string targetSearchNames = "";

	// Token: 0x04000009 RID: 9
	public bool autoAttackSlot0 = false;

	// Token: 0x0400000A RID: 10
	public bool autoSkills = false;

	// Token: 0x0400000B RID: 11
	public bool autoMove = false;

	// Token: 0x0400000C RID: 12
	public bool autoLoot = false;

	// Token: 0x0400000D RID: 13
	public float tickRate = 0.3f;

	// Token: 0x0400000E RID: 14
	public bool infiniteRange = false;
	public bool keepCellOnDeath = false;

	// Token: 0x0400000F RID: 15
	public AutoAttackTrainer.LootMode lootMode = AutoAttackTrainer.LootMode.None;

	// Token: 0x04000010 RID: 16
	public string lootSearchNames = "";

	// Token: 0x04000011 RID: 17
	private bool _maidEnabled = false;

	// Token: 0x04000012 RID: 18
	private bool _maidActive = false;

	// Token: 0x04000013 RID: 19
	public string maidTargetUsername = "";

	// Token: 0x04000014 RID: 20
	public bool showMenu = true;

	// Token: 0x04000015 RID: 21
	public Rect windowRect = new Rect(100f, 100f, 320f, 460f);

	// Token: 0x04000016 RID: 22
	private Coroutine trainerCoroutine;

	// Token: 0x04000017 RID: 23
	private float gotoTimer = 0f;

	// Token: 0x04000018 RID: 24
	public float windowOpacity = 0.95f;

	// Token: 0x04000019 RID: 25
	public float windowWidth = 320f;

	// Token: 0x0400001A RID: 26
	public float windowHeight = 460f;

	// Token: 0x0400001B RID: 27
	private bool isResizing = false;

	// Token: 0x0400001C RID: 28
	private Vector2 resizeStartMousePos;

	// Token: 0x0400001D RID: 29
	private Rect resizeStartRect;

	private Texture2D privateBgTexture;
	private GUIStyle privateWindowStyle;
	private float lastOpacity = -1f;

	// Token: 0x02000008 RID: 8
	public enum LootMode
	{
		// Token: 0x0400001F RID: 31
		None,
		// Token: 0x04000020 RID: 32
		SelectDrop,
		// Token: 0x04000021 RID: 33
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

[HarmonyPatch(typeof(UIChatActions), "UpdateText")]
public static class UIChatActions_UpdateText_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(ResponseChat rc)
	{
		if (VoiceChatManager.Instance != null && VoiceChatManager.Instance.HandleIncomingChat(rc))
		{
			return false; // Skip the original UpdateText (blocks chat from UI)
		}
		return true;
	}
}

[HarmonyPatch(typeof(InputManager), "IsPointerOverUI", new Type[] { typeof(Vector2) })]
public static class InputManager_IsPointerOverUI_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(Vector2 screenPos, ref bool __result)
	{
		if (AutoAttackTrainer.IsPointOverTrainerUI(screenPos))
		{
			__result = true;
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(InputManager), "WorldClickBlockedByUI")]
public static class InputManager_WorldClickBlockedByUI_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(ref bool __result)
	{
		if (AutoAttackTrainer.IsMouseOverTrainerUI())
		{
			__result = true;
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(TouchWalkController), "BeginWalkAt")]
public static class TouchWalkController_BeginWalkAt_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(Vector2 screenPoint, ref bool __result)
	{
		if (AutoAttackTrainer.IsPointOverTrainerUI(screenPoint))
		{
			__result = false;
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(TouchWalkController), "UpdateMouseWalk")]
public static class TouchWalkController_UpdateMouseWalk_Patch
{
	[HarmonyPrefix]
	public static bool Prefix()
	{
		if (AutoAttackTrainer.IsMouseOverTrainerUI())
		{
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(DragClickWalk), "OnPointerDown")]
public static class DragClickWalk_OnPointerDown_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(PointerEventData e)
	{
		if (e != null && AutoAttackTrainer.IsPointOverTrainerUI(e.position))
		{
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(DragClickWalk), "OnPointerUp")]
public static class DragClickWalk_OnPointerUp_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(PointerEventData e)
	{
		if (e != null && AutoAttackTrainer.IsPointOverTrainerUI(e.position))
		{
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(ClickableWalk), "OnMouseDown")]
public static class ClickableWalk_OnMouseDown_Patch
{
	[HarmonyPrefix]
	public static bool Prefix()
	{
		if (AutoAttackTrainer.IsMouseOverTrainerUI())
		{
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(Targetable), "OnMouseDown")]
public static class Targetable_OnMouseDown_Patch
{
	[HarmonyPrefix]
	public static bool Prefix()
	{
		if (AutoAttackTrainer.IsMouseOverTrainerUI())
		{
			return false;
		}
		return true;
	}
}

// - sxbb
