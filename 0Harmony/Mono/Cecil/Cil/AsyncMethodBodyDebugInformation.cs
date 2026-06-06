using System;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	internal sealed class AsyncMethodBodyDebugInformation : CustomDebugInformation
	{
		public InstructionOffset CatchHandler
		{
			get
			{
				return this.catch_handler;
			}
			set
			{
				this.catch_handler = value;
			}
		}

		public Collection<InstructionOffset> Yields
		{
			get
			{
				if (this.yields == null)
				{
					Interlocked.CompareExchange<Collection<InstructionOffset>>(ref this.yields, new Collection<InstructionOffset>(), null);
				}
				return this.yields;
			}
		}

		public Collection<InstructionOffset> Resumes
		{
			get
			{
				if (this.resumes == null)
				{
					Interlocked.CompareExchange<Collection<InstructionOffset>>(ref this.resumes, new Collection<InstructionOffset>(), null);
				}
				return this.resumes;
			}
		}

		public Collection<MethodDefinition> ResumeMethods
		{
			get
			{
				Collection<MethodDefinition> result;
				if ((result = this.resume_methods) == null)
				{
					result = (this.resume_methods = new Collection<MethodDefinition>());
				}
				return result;
			}
		}

		public override CustomDebugInformationKind Kind
		{
			get
			{
				return CustomDebugInformationKind.AsyncMethodBody;
			}
		}

		internal AsyncMethodBodyDebugInformation(int catchHandler) : base(AsyncMethodBodyDebugInformation.KindIdentifier)
		{
			this.catch_handler = new InstructionOffset(catchHandler);
		}

		public AsyncMethodBodyDebugInformation(Instruction catchHandler) : base(AsyncMethodBodyDebugInformation.KindIdentifier)
		{
			this.catch_handler = new InstructionOffset(catchHandler);
		}

		public AsyncMethodBodyDebugInformation() : base(AsyncMethodBodyDebugInformation.KindIdentifier)
		{
			this.catch_handler = new InstructionOffset(-1);
		}

		internal InstructionOffset catch_handler;

		internal Collection<InstructionOffset> yields;

		internal Collection<InstructionOffset> resumes;

		internal Collection<MethodDefinition> resume_methods;

		public static Guid KindIdentifier = new Guid("{54FD2AC5-E925-401A-9C2A-F94F171072F8}");
	}
}
