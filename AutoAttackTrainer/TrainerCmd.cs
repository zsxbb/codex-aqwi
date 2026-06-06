using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pixelplacement;
using UnityEngine;

public class TrainerCmd : ChatAction
{
	public TrainerCmd(List<string> p)
	{
	}

	public override void Execute()
	{
		bool flag = AutoAttackTrainer.Instance != null;
		if (flag)
		{
			AutoAttackTrainer.Instance.ToggleMenu();
		}
		else
		{
			bool flag2 = Singleton<UIChat>.Instance != null;
			if (flag2)
			{
				Singleton<UIChat>.Instance.SetText("<color=red>[Codex] Error: AutoAttackTrainer instance is missing.</color>");
			}
		}
	}
}

public class DevConsoleCmd : ChatAction
{
	public DevConsoleCmd(List<string> p)
	{
	}

	public override void Execute()
	{
		UnityEngine.Object.Instantiate<GameObject>((GameObject)Resources.Load("UI/DevConsole"));
	}
}
