using System;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	internal sealed class StateMachineScopeDebugInformation : CustomDebugInformation
	{
		public Collection<StateMachineScope> Scopes
		{
			get
			{
				Collection<StateMachineScope> result;
				if ((result = this.scopes) == null)
				{
					result = (this.scopes = new Collection<StateMachineScope>());
				}
				return result;
			}
		}

		public override CustomDebugInformationKind Kind
		{
			get
			{
				return CustomDebugInformationKind.StateMachineScope;
			}
		}

		public StateMachineScopeDebugInformation() : base(StateMachineScopeDebugInformation.KindIdentifier)
		{
		}

		internal Collection<StateMachineScope> scopes;

		public static Guid KindIdentifier = new Guid("{6DA9A61E-F8C7-4874-BE62-68BC5630DF71}");
	}
}
