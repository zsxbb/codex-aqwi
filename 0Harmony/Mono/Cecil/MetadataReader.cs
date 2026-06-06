using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	internal sealed class MetadataReader : ByteBuffer
	{
		public MetadataReader(ModuleDefinition module) : base(module.Image.TableHeap.data)
		{
			this.image = module.Image;
			this.module = module;
			this.metadata = module.MetadataSystem;
			this.code = new CodeReader(this);
		}

		public MetadataReader(Image image, ModuleDefinition module, MetadataReader metadata_reader) : base(image.TableHeap.data)
		{
			this.image = image;
			this.module = module;
			this.metadata = module.MetadataSystem;
			this.metadata_reader = metadata_reader;
		}

		private int GetCodedIndexSize(CodedIndex index)
		{
			return this.image.GetCodedIndexSize(index);
		}

		private uint ReadByIndexSize(int size)
		{
			if (size == 4)
			{
				return base.ReadUInt32();
			}
			return (uint)base.ReadUInt16();
		}

		private byte[] ReadBlob()
		{
			BlobHeap blobHeap = this.image.BlobHeap;
			if (blobHeap == null)
			{
				this.position += 2;
				return Empty<byte>.Array;
			}
			return blobHeap.Read(this.ReadBlobIndex());
		}

		private byte[] ReadBlob(uint signature)
		{
			BlobHeap blobHeap = this.image.BlobHeap;
			if (blobHeap == null)
			{
				return Empty<byte>.Array;
			}
			return blobHeap.Read(signature);
		}

		private uint ReadBlobIndex()
		{
			BlobHeap blobHeap = this.image.BlobHeap;
			return this.ReadByIndexSize((blobHeap != null) ? blobHeap.IndexSize : 2);
		}

		private void GetBlobView(uint signature, out byte[] blob, out int index, out int count)
		{
			BlobHeap blobHeap = this.image.BlobHeap;
			if (blobHeap == null)
			{
				blob = null;
				index = (count = 0);
				return;
			}
			blobHeap.GetView(signature, out blob, out index, out count);
		}

		private string ReadString()
		{
			return this.image.StringHeap.Read(this.ReadByIndexSize(this.image.StringHeap.IndexSize));
		}

		private uint ReadStringIndex()
		{
			return this.ReadByIndexSize(this.image.StringHeap.IndexSize);
		}

		private Guid ReadGuid()
		{
			return this.image.GuidHeap.Read(this.ReadByIndexSize(this.image.GuidHeap.IndexSize));
		}

		private uint ReadTableIndex(Table table)
		{
			return this.ReadByIndexSize(this.image.GetTableIndexSize(table));
		}

		private MetadataToken ReadMetadataToken(CodedIndex index)
		{
			return index.GetMetadataToken(this.ReadByIndexSize(this.GetCodedIndexSize(index)));
		}

		private int MoveTo(Table table)
		{
			TableInformation tableInformation = this.image.TableHeap[table];
			if (tableInformation.Length != 0U)
			{
				this.position = (int)tableInformation.Offset;
			}
			return (int)tableInformation.Length;
		}

		private bool MoveTo(Table table, uint row)
		{
			TableInformation tableInformation = this.image.TableHeap[table];
			uint length = tableInformation.Length;
			if (length == 0U || row > length)
			{
				return false;
			}
			this.position = (int)(tableInformation.Offset + tableInformation.RowSize * (row - 1U));
			return true;
		}

		public AssemblyNameDefinition ReadAssemblyNameDefinition()
		{
			if (this.MoveTo(Table.Assembly) == 0)
			{
				return null;
			}
			AssemblyNameDefinition assemblyNameDefinition = new AssemblyNameDefinition();
			assemblyNameDefinition.HashAlgorithm = (AssemblyHashAlgorithm)base.ReadUInt32();
			this.PopulateVersionAndFlags(assemblyNameDefinition);
			assemblyNameDefinition.PublicKey = this.ReadBlob();
			this.PopulateNameAndCulture(assemblyNameDefinition);
			return assemblyNameDefinition;
		}

		public ModuleDefinition Populate(ModuleDefinition module)
		{
			if (this.MoveTo(Table.Module) == 0)
			{
				return module;
			}
			base.Advance(2);
			module.Name = this.ReadString();
			module.Mvid = this.ReadGuid();
			return module;
		}

		private void InitializeAssemblyReferences()
		{
			if (this.metadata.AssemblyReferences != null)
			{
				return;
			}
			int num = this.MoveTo(Table.AssemblyRef);
			AssemblyNameReference[] array = this.metadata.AssemblyReferences = new AssemblyNameReference[num];
			uint num2 = 0U;
			while ((ulong)num2 < (ulong)((long)num))
			{
				AssemblyNameReference assemblyNameReference = new AssemblyNameReference();
				assemblyNameReference.token = new MetadataToken(TokenType.AssemblyRef, num2 + 1U);
				this.PopulateVersionAndFlags(assemblyNameReference);
				byte[] array2 = this.ReadBlob();
				if (assemblyNameReference.HasPublicKey)
				{
					assemblyNameReference.PublicKey = array2;
				}
				else
				{
					assemblyNameReference.PublicKeyToken = array2;
				}
				this.PopulateNameAndCulture(assemblyNameReference);
				assemblyNameReference.Hash = this.ReadBlob();
				array[(int)num2] = assemblyNameReference;
				num2 += 1U;
			}
		}

		public Collection<AssemblyNameReference> ReadAssemblyReferences()
		{
			this.InitializeAssemblyReferences();
			Collection<AssemblyNameReference> collection = new Collection<AssemblyNameReference>(this.metadata.AssemblyReferences);
			if (this.module.IsWindowsMetadata())
			{
				this.module.Projections.AddVirtualReferences(collection);
			}
			return collection;
		}

		public MethodDefinition ReadEntryPoint()
		{
			if (this.module.Image.EntryPointToken == 0U)
			{
				return null;
			}
			MetadataToken metadataToken = new MetadataToken(this.module.Image.EntryPointToken);
			return this.GetMethodDefinition(metadataToken.RID);
		}

		public Collection<ModuleDefinition> ReadModules()
		{
			Collection<ModuleDefinition> collection = new Collection<ModuleDefinition>(1);
			collection.Add(this.module);
			int num = this.MoveTo(Table.File);
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				bool flag = base.ReadUInt32() != 0U;
				string name = this.ReadString();
				this.ReadBlobIndex();
				if (!flag)
				{
					ReaderParameters parameters = new ReaderParameters
					{
						ReadingMode = this.module.ReadingMode,
						SymbolReaderProvider = this.module.SymbolReaderProvider,
						AssemblyResolver = this.module.AssemblyResolver
					};
					ModuleDefinition moduleDefinition = ModuleDefinition.ReadModule(this.GetModuleFileName(name), parameters);
					moduleDefinition.assembly = this.module.assembly;
					collection.Add(moduleDefinition);
				}
				num2 += 1U;
			}
			return collection;
		}

		private string GetModuleFileName(string name)
		{
			if (this.module.FileName == null)
			{
				throw new NotSupportedException();
			}
			return Path.Combine(Path.GetDirectoryName(this.module.FileName), name);
		}

		private void InitializeModuleReferences()
		{
			if (this.metadata.ModuleReferences != null)
			{
				return;
			}
			int num = this.MoveTo(Table.ModuleRef);
			ModuleReference[] array = this.metadata.ModuleReferences = new ModuleReference[num];
			uint num2 = 0U;
			while ((ulong)num2 < (ulong)((long)num))
			{
				array[(int)num2] = new ModuleReference(this.ReadString())
				{
					token = new MetadataToken(TokenType.ModuleRef, num2 + 1U)
				};
				num2 += 1U;
			}
		}

		public Collection<ModuleReference> ReadModuleReferences()
		{
			this.InitializeModuleReferences();
			return new Collection<ModuleReference>(this.metadata.ModuleReferences);
		}

		public bool HasFileResource()
		{
			int num = this.MoveTo(Table.File);
			if (num == 0)
			{
				return false;
			}
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				if (this.ReadFileRecord(num2).Col1 == FileAttributes.ContainsNoMetaData)
				{
					return true;
				}
				num2 += 1U;
			}
			return false;
		}

		public Collection<Resource> ReadResources()
		{
			int num = this.MoveTo(Table.ManifestResource);
			Collection<Resource> collection = new Collection<Resource>(num);
			int i = 1;
			while (i <= num)
			{
				uint offset = base.ReadUInt32();
				ManifestResourceAttributes manifestResourceAttributes = (ManifestResourceAttributes)base.ReadUInt32();
				string name = this.ReadString();
				MetadataToken scope = this.ReadMetadataToken(CodedIndex.Implementation);
				Resource item;
				if (scope.RID == 0U)
				{
					item = new EmbeddedResource(name, manifestResourceAttributes, offset, this);
					goto IL_C6;
				}
				if (scope.TokenType == TokenType.AssemblyRef)
				{
					item = new AssemblyLinkedResource(name, manifestResourceAttributes)
					{
						Assembly = (AssemblyNameReference)this.GetTypeReferenceScope(scope)
					};
					goto IL_C6;
				}
				if (scope.TokenType == TokenType.File)
				{
					Row<FileAttributes, string, uint> row = this.ReadFileRecord(scope.RID);
					item = new LinkedResource(name, manifestResourceAttributes)
					{
						File = row.Col2,
						hash = this.ReadBlob(row.Col3)
					};
					goto IL_C6;
				}
				IL_CE:
				i++;
				continue;
				IL_C6:
				collection.Add(item);
				goto IL_CE;
			}
			return collection;
		}

		private Row<FileAttributes, string, uint> ReadFileRecord(uint rid)
		{
			int position = this.position;
			if (!this.MoveTo(Table.File, rid))
			{
				throw new ArgumentException();
			}
			Row<FileAttributes, string, uint> result = new Row<FileAttributes, string, uint>((FileAttributes)base.ReadUInt32(), this.ReadString(), this.ReadBlobIndex());
			this.position = position;
			return result;
		}

		public byte[] GetManagedResource(uint offset)
		{
			return this.image.GetReaderAt<uint, byte[]>(this.image.Resources.VirtualAddress, offset, delegate(uint o, BinaryStreamReader reader)
			{
				reader.Advance((int)o);
				return reader.ReadBytes(reader.ReadInt32());
			}) ?? Empty<byte>.Array;
		}

		private void PopulateVersionAndFlags(AssemblyNameReference name)
		{
			name.Version = new Version((int)base.ReadUInt16(), (int)base.ReadUInt16(), (int)base.ReadUInt16(), (int)base.ReadUInt16());
			name.Attributes = (AssemblyAttributes)base.ReadUInt32();
		}

		private void PopulateNameAndCulture(AssemblyNameReference name)
		{
			name.Name = this.ReadString();
			name.Culture = this.ReadString();
		}

		public TypeDefinitionCollection ReadTypes()
		{
			this.InitializeTypeDefinitions();
			TypeDefinition[] types = this.metadata.Types;
			int capacity = types.Length - this.metadata.NestedTypes.Count;
			TypeDefinitionCollection typeDefinitionCollection = new TypeDefinitionCollection(this.module, capacity);
			foreach (TypeDefinition typeDefinition in types)
			{
				if (!MetadataReader.IsNested(typeDefinition.Attributes))
				{
					typeDefinitionCollection.Add(typeDefinition);
				}
			}
			if (this.image.HasTable(Table.MethodPtr) || this.image.HasTable(Table.FieldPtr))
			{
				this.CompleteTypes();
			}
			return typeDefinitionCollection;
		}

		private void CompleteTypes()
		{
			foreach (TypeDefinition typeDefinition in this.metadata.Types)
			{
				Mixin.Read(typeDefinition.Fields);
				Mixin.Read(typeDefinition.Methods);
			}
		}

		private void InitializeTypeDefinitions()
		{
			if (this.metadata.Types != null)
			{
				return;
			}
			this.InitializeNestedTypes();
			this.InitializeFields();
			this.InitializeMethods();
			int num = this.MoveTo(Table.TypeDef);
			TypeDefinition[] array = this.metadata.Types = new TypeDefinition[num];
			uint num2 = 0U;
			while ((ulong)num2 < (ulong)((long)num))
			{
				if (array[(int)num2] == null)
				{
					array[(int)num2] = this.ReadType(num2 + 1U);
				}
				num2 += 1U;
			}
			if (this.module.IsWindowsMetadata())
			{
				uint num3 = 0U;
				while ((ulong)num3 < (ulong)((long)num))
				{
					WindowsRuntimeProjections.Project(array[(int)num3]);
					num3 += 1U;
				}
			}
		}

		private static bool IsNested(TypeAttributes attributes)
		{
			TypeAttributes typeAttributes = attributes & TypeAttributes.VisibilityMask;
			return typeAttributes - TypeAttributes.NestedPublic <= 5U;
		}

		public bool HasNestedTypes(TypeDefinition type)
		{
			this.InitializeNestedTypes();
			Collection<uint> collection;
			return this.metadata.TryGetNestedTypeMapping(type, out collection) && collection.Count > 0;
		}

		public Collection<TypeDefinition> ReadNestedTypes(TypeDefinition type)
		{
			this.InitializeNestedTypes();
			Collection<uint> collection;
			if (!this.metadata.TryGetNestedTypeMapping(type, out collection))
			{
				return new MemberDefinitionCollection<TypeDefinition>(type);
			}
			MemberDefinitionCollection<TypeDefinition> memberDefinitionCollection = new MemberDefinitionCollection<TypeDefinition>(type, collection.Count);
			for (int i = 0; i < collection.Count; i++)
			{
				TypeDefinition typeDefinition = this.GetTypeDefinition(collection[i]);
				if (typeDefinition != null)
				{
					memberDefinitionCollection.Add(typeDefinition);
				}
			}
			return memberDefinitionCollection;
		}

		private void InitializeNestedTypes()
		{
			if (this.metadata.NestedTypes != null)
			{
				return;
			}
			int num = this.MoveTo(Table.NestedClass);
			this.metadata.NestedTypes = new Dictionary<uint, Collection<uint>>(num);
			this.metadata.ReverseNestedTypes = new Dictionary<uint, uint>(num);
			if (num == 0)
			{
				return;
			}
			for (int i = 1; i <= num; i++)
			{
				uint nested = this.ReadTableIndex(Table.TypeDef);
				uint declaring = this.ReadTableIndex(Table.TypeDef);
				this.AddNestedMapping(declaring, nested);
			}
		}

		private void AddNestedMapping(uint declaring, uint nested)
		{
			this.metadata.SetNestedTypeMapping(declaring, MetadataReader.AddMapping<uint, uint>(this.metadata.NestedTypes, declaring, nested));
			this.metadata.SetReverseNestedTypeMapping(nested, declaring);
		}

		private static Collection<TValue> AddMapping<TKey, TValue>(Dictionary<TKey, Collection<TValue>> cache, TKey key, TValue value)
		{
			Collection<TValue> collection;
			if (!cache.TryGetValue(key, out collection))
			{
				collection = new Collection<TValue>();
			}
			collection.Add(value);
			return collection;
		}

		private TypeDefinition ReadType(uint rid)
		{
			if (!this.MoveTo(Table.TypeDef, rid))
			{
				return null;
			}
			TypeAttributes attributes = (TypeAttributes)base.ReadUInt32();
			string name = this.ReadString();
			TypeDefinition typeDefinition = new TypeDefinition(this.ReadString(), name, attributes);
			typeDefinition.token = new MetadataToken(TokenType.TypeDef, rid);
			typeDefinition.scope = this.module;
			typeDefinition.module = this.module;
			this.metadata.AddTypeDefinition(typeDefinition);
			this.context = typeDefinition;
			typeDefinition.BaseType = this.GetTypeDefOrRef(this.ReadMetadataToken(CodedIndex.TypeDefOrRef));
			typeDefinition.fields_range = this.ReadListRange(rid, Table.TypeDef, Table.Field);
			typeDefinition.methods_range = this.ReadListRange(rid, Table.TypeDef, Table.Method);
			if (MetadataReader.IsNested(attributes))
			{
				typeDefinition.DeclaringType = this.GetNestedTypeDeclaringType(typeDefinition);
			}
			return typeDefinition;
		}

		private TypeDefinition GetNestedTypeDeclaringType(TypeDefinition type)
		{
			uint rid;
			if (!this.metadata.TryGetReverseNestedTypeMapping(type, out rid))
			{
				return null;
			}
			return this.GetTypeDefinition(rid);
		}

		private Range ReadListRange(uint current_index, Table current, Table target)
		{
			Range result = default(Range);
			uint num = this.ReadTableIndex(target);
			if (num == 0U)
			{
				return result;
			}
			TableInformation tableInformation = this.image.TableHeap[current];
			uint num2;
			if (current_index == tableInformation.Length)
			{
				num2 = this.image.TableHeap[target].Length + 1U;
			}
			else
			{
				int position = this.position;
				this.position += (int)((ulong)tableInformation.RowSize - (ulong)((long)this.image.GetTableIndexSize(target)));
				num2 = this.ReadTableIndex(target);
				this.position = position;
			}
			result.Start = num;
			result.Length = num2 - num;
			return result;
		}

		public Row<short, int> ReadTypeLayout(TypeDefinition type)
		{
			this.InitializeTypeLayouts();
			uint rid = type.token.RID;
			Row<ushort, uint> row;
			if (!this.metadata.ClassLayouts.TryGetValue(rid, out row))
			{
				return new Row<short, int>(-1, -1);
			}
			type.PackingSize = (short)row.Col1;
			type.ClassSize = (int)row.Col2;
			this.metadata.ClassLayouts.Remove(rid);
			return new Row<short, int>((short)row.Col1, (int)row.Col2);
		}

		private void InitializeTypeLayouts()
		{
			if (this.metadata.ClassLayouts != null)
			{
				return;
			}
			int num = this.MoveTo(Table.ClassLayout);
			Dictionary<uint, Row<ushort, uint>> dictionary = this.metadata.ClassLayouts = new Dictionary<uint, Row<ushort, uint>>(num);
			uint num2 = 0U;
			while ((ulong)num2 < (ulong)((long)num))
			{
				ushort col = base.ReadUInt16();
				uint col2 = base.ReadUInt32();
				uint key = this.ReadTableIndex(Table.TypeDef);
				dictionary.Add(key, new Row<ushort, uint>(col, col2));
				num2 += 1U;
			}
		}

		public TypeReference GetTypeDefOrRef(MetadataToken token)
		{
			return (TypeReference)this.LookupToken(token);
		}

		public TypeDefinition GetTypeDefinition(uint rid)
		{
			this.InitializeTypeDefinitions();
			TypeDefinition typeDefinition = this.metadata.GetTypeDefinition(rid);
			if (typeDefinition != null)
			{
				return typeDefinition;
			}
			typeDefinition = this.ReadTypeDefinition(rid);
			if (this.module.IsWindowsMetadata())
			{
				WindowsRuntimeProjections.Project(typeDefinition);
			}
			return typeDefinition;
		}

		private TypeDefinition ReadTypeDefinition(uint rid)
		{
			if (!this.MoveTo(Table.TypeDef, rid))
			{
				return null;
			}
			return this.ReadType(rid);
		}

		private void InitializeTypeReferences()
		{
			if (this.metadata.TypeReferences != null)
			{
				return;
			}
			this.metadata.TypeReferences = new TypeReference[this.image.GetTableLength(Table.TypeRef)];
		}

		public TypeReference GetTypeReference(string scope, string full_name)
		{
			this.InitializeTypeReferences();
			int num = this.metadata.TypeReferences.Length;
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				TypeReference typeReference = this.GetTypeReference(num2);
				if (!(typeReference.FullName != full_name))
				{
					if (string.IsNullOrEmpty(scope))
					{
						return typeReference;
					}
					if (typeReference.Scope.Name == scope)
					{
						return typeReference;
					}
				}
				num2 += 1U;
			}
			return null;
		}

		private TypeReference GetTypeReference(uint rid)
		{
			this.InitializeTypeReferences();
			TypeReference typeReference = this.metadata.GetTypeReference(rid);
			if (typeReference != null)
			{
				return typeReference;
			}
			return this.ReadTypeReference(rid);
		}

		private TypeReference ReadTypeReference(uint rid)
		{
			if (!this.MoveTo(Table.TypeRef, rid))
			{
				return null;
			}
			TypeReference typeReference = null;
			MetadataToken metadataToken = this.ReadMetadataToken(CodedIndex.ResolutionScope);
			string name = this.ReadString();
			TypeReference typeReference2 = new TypeReference(this.ReadString(), name, this.module, null);
			typeReference2.token = new MetadataToken(TokenType.TypeRef, rid);
			this.metadata.AddTypeReference(typeReference2);
			IMetadataScope scope;
			if (metadataToken.TokenType == TokenType.TypeRef)
			{
				if (metadataToken.RID != rid)
				{
					typeReference = this.GetTypeDefOrRef(metadataToken);
					IMetadataScope metadataScope2;
					if (typeReference == null)
					{
						IMetadataScope metadataScope = this.module;
						metadataScope2 = metadataScope;
					}
					else
					{
						metadataScope2 = typeReference.Scope;
					}
					scope = metadataScope2;
				}
				else
				{
					scope = this.module;
				}
			}
			else
			{
				scope = this.GetTypeReferenceScope(metadataToken);
			}
			typeReference2.scope = scope;
			typeReference2.DeclaringType = typeReference;
			MetadataSystem.TryProcessPrimitiveTypeReference(typeReference2);
			if (typeReference2.Module.IsWindowsMetadata())
			{
				WindowsRuntimeProjections.Project(typeReference2);
			}
			return typeReference2;
		}

		private IMetadataScope GetTypeReferenceScope(MetadataToken scope)
		{
			if (scope.TokenType == TokenType.Module)
			{
				return this.module;
			}
			TokenType tokenType = scope.TokenType;
			IMetadataScope[] array2;
			if (tokenType != TokenType.ModuleRef)
			{
				if (tokenType != TokenType.AssemblyRef)
				{
					throw new NotSupportedException();
				}
				this.InitializeAssemblyReferences();
				IMetadataScope[] array = this.metadata.AssemblyReferences;
				array2 = array;
			}
			else
			{
				this.InitializeModuleReferences();
				IMetadataScope[] array = this.metadata.ModuleReferences;
				array2 = array;
			}
			uint num = scope.RID - 1U;
			if (num < 0U || (ulong)num >= (ulong)((long)array2.Length))
			{
				return null;
			}
			return array2[(int)num];
		}

		public IEnumerable<TypeReference> GetTypeReferences()
		{
			this.InitializeTypeReferences();
			int tableLength = this.image.GetTableLength(Table.TypeRef);
			TypeReference[] array = new TypeReference[tableLength];
			uint num = 1U;
			while ((ulong)num <= (ulong)((long)tableLength))
			{
				array[(int)(num - 1U)] = this.GetTypeReference(num);
				num += 1U;
			}
			return array;
		}

		private TypeReference GetTypeSpecification(uint rid)
		{
			if (!this.MoveTo(Table.TypeSpec, rid))
			{
				return null;
			}
			TypeReference typeReference = this.ReadSignature(this.ReadBlobIndex()).ReadTypeSignature();
			if (typeReference.token.RID == 0U)
			{
				typeReference.token = new MetadataToken(TokenType.TypeSpec, rid);
			}
			return typeReference;
		}

		private SignatureReader ReadSignature(uint signature)
		{
			return new SignatureReader(signature, this);
		}

		public bool HasInterfaces(TypeDefinition type)
		{
			this.InitializeInterfaces();
			Collection<Row<uint, MetadataToken>> collection;
			return this.metadata.TryGetInterfaceMapping(type, out collection);
		}

		public InterfaceImplementationCollection ReadInterfaces(TypeDefinition type)
		{
			this.InitializeInterfaces();
			Collection<Row<uint, MetadataToken>> collection;
			if (!this.metadata.TryGetInterfaceMapping(type, out collection))
			{
				return new InterfaceImplementationCollection(type);
			}
			InterfaceImplementationCollection interfaceImplementationCollection = new InterfaceImplementationCollection(type, collection.Count);
			this.context = type;
			for (int i = 0; i < collection.Count; i++)
			{
				interfaceImplementationCollection.Add(new InterfaceImplementation(this.GetTypeDefOrRef(collection[i].Col2), new MetadataToken(TokenType.InterfaceImpl, collection[i].Col1)));
			}
			return interfaceImplementationCollection;
		}

		private void InitializeInterfaces()
		{
			if (this.metadata.Interfaces != null)
			{
				return;
			}
			int num = this.MoveTo(Table.InterfaceImpl);
			this.metadata.Interfaces = new Dictionary<uint, Collection<Row<uint, MetadataToken>>>(num);
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				uint type = this.ReadTableIndex(Table.TypeDef);
				MetadataToken col = this.ReadMetadataToken(CodedIndex.TypeDefOrRef);
				this.AddInterfaceMapping(type, new Row<uint, MetadataToken>(num2, col));
				num2 += 1U;
			}
		}

		private void AddInterfaceMapping(uint type, Row<uint, MetadataToken> @interface)
		{
			this.metadata.SetInterfaceMapping(type, MetadataReader.AddMapping<uint, Row<uint, MetadataToken>>(this.metadata.Interfaces, type, @interface));
		}

		public Collection<FieldDefinition> ReadFields(TypeDefinition type)
		{
			Range fields_range = type.fields_range;
			if (fields_range.Length == 0U)
			{
				return new MemberDefinitionCollection<FieldDefinition>(type);
			}
			MemberDefinitionCollection<FieldDefinition> memberDefinitionCollection = new MemberDefinitionCollection<FieldDefinition>(type, (int)fields_range.Length);
			this.context = type;
			if (!this.MoveTo(Table.FieldPtr, fields_range.Start))
			{
				if (!this.MoveTo(Table.Field, fields_range.Start))
				{
					return memberDefinitionCollection;
				}
				for (uint num = 0U; num < fields_range.Length; num += 1U)
				{
					this.ReadField(fields_range.Start + num, memberDefinitionCollection);
				}
			}
			else
			{
				this.ReadPointers<FieldDefinition>(Table.FieldPtr, Table.Field, fields_range, memberDefinitionCollection, new Action<uint, Collection<FieldDefinition>>(this.ReadField));
			}
			return memberDefinitionCollection;
		}

		private void ReadField(uint field_rid, Collection<FieldDefinition> fields)
		{
			FieldAttributes attributes = (FieldAttributes)base.ReadUInt16();
			string name = this.ReadString();
			uint signature = this.ReadBlobIndex();
			FieldDefinition fieldDefinition = new FieldDefinition(name, attributes, this.ReadFieldType(signature));
			fieldDefinition.token = new MetadataToken(TokenType.Field, field_rid);
			this.metadata.AddFieldDefinition(fieldDefinition);
			if (MetadataReader.IsDeleted(fieldDefinition))
			{
				return;
			}
			fields.Add(fieldDefinition);
			if (this.module.IsWindowsMetadata())
			{
				WindowsRuntimeProjections.Project(fieldDefinition);
			}
		}

		private void InitializeFields()
		{
			if (this.metadata.Fields != null)
			{
				return;
			}
			this.metadata.Fields = new FieldDefinition[this.image.GetTableLength(Table.Field)];
		}

		private TypeReference ReadFieldType(uint signature)
		{
			SignatureReader signatureReader = this.ReadSignature(signature);
			if (signatureReader.ReadByte() != 6)
			{
				throw new NotSupportedException();
			}
			return signatureReader.ReadTypeSignature();
		}

		public int ReadFieldRVA(FieldDefinition field)
		{
			this.InitializeFieldRVAs();
			uint rid = field.token.RID;
			uint num;
			if (!this.metadata.FieldRVAs.TryGetValue(rid, out num))
			{
				return 0;
			}
			int fieldTypeSize = MetadataReader.GetFieldTypeSize(field.FieldType);
			if (fieldTypeSize == 0 || num == 0U)
			{
				return 0;
			}
			this.metadata.FieldRVAs.Remove(rid);
			field.InitialValue = this.GetFieldInitializeValue(fieldTypeSize, num);
			return (int)num;
		}

		private byte[] GetFieldInitializeValue(int size, uint rva)
		{
			return this.image.GetReaderAt<int, byte[]>(rva, size, (int s, BinaryStreamReader reader) => reader.ReadBytes(s)) ?? Empty<byte>.Array;
		}

		private static int GetFieldTypeSize(TypeReference type)
		{
			int result = 0;
			switch (type.etype)
			{
			case ElementType.Boolean:
			case ElementType.I1:
			case ElementType.U1:
				return 1;
			case ElementType.Char:
			case ElementType.I2:
			case ElementType.U2:
				return 2;
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.R4:
				return 4;
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R8:
				return 8;
			case ElementType.Ptr:
			case ElementType.FnPtr:
				return IntPtr.Size;
			case ElementType.CModReqD:
			case ElementType.CModOpt:
				return MetadataReader.GetFieldTypeSize(((IModifierType)type).ElementType);
			}
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition != null && typeDefinition.HasLayoutInfo)
			{
				result = typeDefinition.ClassSize;
			}
			return result;
		}

		private void InitializeFieldRVAs()
		{
			if (this.metadata.FieldRVAs != null)
			{
				return;
			}
			int num = this.MoveTo(Table.FieldRVA);
			Dictionary<uint, uint> dictionary = this.metadata.FieldRVAs = new Dictionary<uint, uint>(num);
			for (int i = 0; i < num; i++)
			{
				uint value = base.ReadUInt32();
				uint key = this.ReadTableIndex(Table.Field);
				dictionary.Add(key, value);
			}
		}

		public int ReadFieldLayout(FieldDefinition field)
		{
			this.InitializeFieldLayouts();
			uint rid = field.token.RID;
			uint result;
			if (!this.metadata.FieldLayouts.TryGetValue(rid, out result))
			{
				return -1;
			}
			this.metadata.FieldLayouts.Remove(rid);
			return (int)result;
		}

		private void InitializeFieldLayouts()
		{
			if (this.metadata.FieldLayouts != null)
			{
				return;
			}
			int num = this.MoveTo(Table.FieldLayout);
			Dictionary<uint, uint> dictionary = this.metadata.FieldLayouts = new Dictionary<uint, uint>(num);
			for (int i = 0; i < num; i++)
			{
				uint value = base.ReadUInt32();
				uint key = this.ReadTableIndex(Table.Field);
				dictionary.Add(key, value);
			}
		}

		public bool HasEvents(TypeDefinition type)
		{
			this.InitializeEvents();
			Range range;
			return this.metadata.TryGetEventsRange(type, out range) && range.Length > 0U;
		}

		public Collection<EventDefinition> ReadEvents(TypeDefinition type)
		{
			this.InitializeEvents();
			Range range;
			if (!this.metadata.TryGetEventsRange(type, out range))
			{
				return new MemberDefinitionCollection<EventDefinition>(type);
			}
			MemberDefinitionCollection<EventDefinition> memberDefinitionCollection = new MemberDefinitionCollection<EventDefinition>(type, (int)range.Length);
			if (range.Length == 0U)
			{
				return memberDefinitionCollection;
			}
			this.context = type;
			if (!this.MoveTo(Table.EventPtr, range.Start))
			{
				if (!this.MoveTo(Table.Event, range.Start))
				{
					return memberDefinitionCollection;
				}
				for (uint num = 0U; num < range.Length; num += 1U)
				{
					this.ReadEvent(range.Start + num, memberDefinitionCollection);
				}
			}
			else
			{
				this.ReadPointers<EventDefinition>(Table.EventPtr, Table.Event, range, memberDefinitionCollection, new Action<uint, Collection<EventDefinition>>(this.ReadEvent));
			}
			return memberDefinitionCollection;
		}

		private void ReadEvent(uint event_rid, Collection<EventDefinition> events)
		{
			EventAttributes attributes = (EventAttributes)base.ReadUInt16();
			string name = this.ReadString();
			TypeReference typeDefOrRef = this.GetTypeDefOrRef(this.ReadMetadataToken(CodedIndex.TypeDefOrRef));
			EventDefinition eventDefinition = new EventDefinition(name, attributes, typeDefOrRef);
			eventDefinition.token = new MetadataToken(TokenType.Event, event_rid);
			if (MetadataReader.IsDeleted(eventDefinition))
			{
				return;
			}
			events.Add(eventDefinition);
		}

		private void InitializeEvents()
		{
			if (this.metadata.Events != null)
			{
				return;
			}
			int num = this.MoveTo(Table.EventMap);
			this.metadata.Events = new Dictionary<uint, Range>(num);
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				uint type_rid = this.ReadTableIndex(Table.TypeDef);
				Range range = this.ReadListRange(num2, Table.EventMap, Table.Event);
				this.metadata.AddEventsRange(type_rid, range);
				num2 += 1U;
			}
		}

		public bool HasProperties(TypeDefinition type)
		{
			this.InitializeProperties();
			Range range;
			return this.metadata.TryGetPropertiesRange(type, out range) && range.Length > 0U;
		}

		public Collection<PropertyDefinition> ReadProperties(TypeDefinition type)
		{
			this.InitializeProperties();
			Range range;
			if (!this.metadata.TryGetPropertiesRange(type, out range))
			{
				return new MemberDefinitionCollection<PropertyDefinition>(type);
			}
			MemberDefinitionCollection<PropertyDefinition> memberDefinitionCollection = new MemberDefinitionCollection<PropertyDefinition>(type, (int)range.Length);
			if (range.Length == 0U)
			{
				return memberDefinitionCollection;
			}
			this.context = type;
			if (!this.MoveTo(Table.PropertyPtr, range.Start))
			{
				if (!this.MoveTo(Table.Property, range.Start))
				{
					return memberDefinitionCollection;
				}
				for (uint num = 0U; num < range.Length; num += 1U)
				{
					this.ReadProperty(range.Start + num, memberDefinitionCollection);
				}
			}
			else
			{
				this.ReadPointers<PropertyDefinition>(Table.PropertyPtr, Table.Property, range, memberDefinitionCollection, new Action<uint, Collection<PropertyDefinition>>(this.ReadProperty));
			}
			return memberDefinitionCollection;
		}

		private void ReadProperty(uint property_rid, Collection<PropertyDefinition> properties)
		{
			PropertyAttributes attributes = (PropertyAttributes)base.ReadUInt16();
			string name = this.ReadString();
			uint signature = this.ReadBlobIndex();
			SignatureReader signatureReader = this.ReadSignature(signature);
			byte b = signatureReader.ReadByte();
			if ((b & 8) == 0)
			{
				throw new NotSupportedException();
			}
			bool hasThis = (b & 32) > 0;
			signatureReader.ReadCompressedUInt32();
			PropertyDefinition propertyDefinition = new PropertyDefinition(name, attributes, signatureReader.ReadTypeSignature());
			propertyDefinition.HasThis = hasThis;
			propertyDefinition.token = new MetadataToken(TokenType.Property, property_rid);
			if (MetadataReader.IsDeleted(propertyDefinition))
			{
				return;
			}
			properties.Add(propertyDefinition);
		}

		private void InitializeProperties()
		{
			if (this.metadata.Properties != null)
			{
				return;
			}
			int num = this.MoveTo(Table.PropertyMap);
			this.metadata.Properties = new Dictionary<uint, Range>(num);
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				uint type_rid = this.ReadTableIndex(Table.TypeDef);
				Range range = this.ReadListRange(num2, Table.PropertyMap, Table.Property);
				this.metadata.AddPropertiesRange(type_rid, range);
				num2 += 1U;
			}
		}

		private MethodSemanticsAttributes ReadMethodSemantics(MethodDefinition method)
		{
			this.InitializeMethodSemantics();
			Row<MethodSemanticsAttributes, MetadataToken> row;
			if (!this.metadata.Semantics.TryGetValue(method.token.RID, out row))
			{
				return MethodSemanticsAttributes.None;
			}
			TypeDefinition declaringType = method.DeclaringType;
			MethodSemanticsAttributes col = row.Col1;
			if (col <= MethodSemanticsAttributes.AddOn)
			{
				switch (col)
				{
				case MethodSemanticsAttributes.Setter:
					MetadataReader.GetProperty(declaringType, row.Col2).set_method = method;
					goto IL_16B;
				case MethodSemanticsAttributes.Getter:
					MetadataReader.GetProperty(declaringType, row.Col2).get_method = method;
					goto IL_16B;
				case MethodSemanticsAttributes.Setter | MethodSemanticsAttributes.Getter:
					break;
				case MethodSemanticsAttributes.Other:
				{
					TokenType tokenType = row.Col2.TokenType;
					if (tokenType == TokenType.Event)
					{
						EventDefinition @event = MetadataReader.GetEvent(declaringType, row.Col2);
						if (@event.other_methods == null)
						{
							@event.other_methods = new Collection<MethodDefinition>();
						}
						@event.other_methods.Add(method);
						goto IL_16B;
					}
					if (tokenType != TokenType.Property)
					{
						throw new NotSupportedException();
					}
					PropertyDefinition property = MetadataReader.GetProperty(declaringType, row.Col2);
					if (property.other_methods == null)
					{
						property.other_methods = new Collection<MethodDefinition>();
					}
					property.other_methods.Add(method);
					goto IL_16B;
				}
				default:
					if (col == MethodSemanticsAttributes.AddOn)
					{
						MetadataReader.GetEvent(declaringType, row.Col2).add_method = method;
						goto IL_16B;
					}
					break;
				}
			}
			else
			{
				if (col == MethodSemanticsAttributes.RemoveOn)
				{
					MetadataReader.GetEvent(declaringType, row.Col2).remove_method = method;
					goto IL_16B;
				}
				if (col == MethodSemanticsAttributes.Fire)
				{
					MetadataReader.GetEvent(declaringType, row.Col2).invoke_method = method;
					goto IL_16B;
				}
			}
			throw new NotSupportedException();
			IL_16B:
			this.metadata.Semantics.Remove(method.token.RID);
			return row.Col1;
		}

		private static EventDefinition GetEvent(TypeDefinition type, MetadataToken token)
		{
			if (token.TokenType != TokenType.Event)
			{
				throw new ArgumentException();
			}
			return MetadataReader.GetMember<EventDefinition>(type.Events, token);
		}

		private static PropertyDefinition GetProperty(TypeDefinition type, MetadataToken token)
		{
			if (token.TokenType != TokenType.Property)
			{
				throw new ArgumentException();
			}
			return MetadataReader.GetMember<PropertyDefinition>(type.Properties, token);
		}

		private static TMember GetMember<TMember>(Collection<TMember> members, MetadataToken token) where TMember : IMemberDefinition
		{
			for (int i = 0; i < members.Count; i++)
			{
				TMember result = members[i];
				if (result.MetadataToken == token)
				{
					return result;
				}
			}
			throw new ArgumentException();
		}

		private void InitializeMethodSemantics()
		{
			if (this.metadata.Semantics != null)
			{
				return;
			}
			int num = this.MoveTo(Table.MethodSemantics);
			Dictionary<uint, Row<MethodSemanticsAttributes, MetadataToken>> dictionary = this.metadata.Semantics = new Dictionary<uint, Row<MethodSemanticsAttributes, MetadataToken>>(0);
			uint num2 = 0U;
			while ((ulong)num2 < (ulong)((long)num))
			{
				MethodSemanticsAttributes col = (MethodSemanticsAttributes)base.ReadUInt16();
				uint key = this.ReadTableIndex(Table.Method);
				MetadataToken col2 = this.ReadMetadataToken(CodedIndex.HasSemantics);
				dictionary[key] = new Row<MethodSemanticsAttributes, MetadataToken>(col, col2);
				num2 += 1U;
			}
		}

		public void ReadMethods(PropertyDefinition property)
		{
			this.ReadAllSemantics(property.DeclaringType);
		}

		public void ReadMethods(EventDefinition @event)
		{
			this.ReadAllSemantics(@event.DeclaringType);
		}

		public void ReadAllSemantics(MethodDefinition method)
		{
			this.ReadAllSemantics(method.DeclaringType);
		}

		private void ReadAllSemantics(TypeDefinition type)
		{
			Collection<MethodDefinition> methods = type.Methods;
			for (int i = 0; i < methods.Count; i++)
			{
				MethodDefinition methodDefinition = methods[i];
				if (!methodDefinition.sem_attrs_ready)
				{
					methodDefinition.sem_attrs = this.ReadMethodSemantics(methodDefinition);
					methodDefinition.sem_attrs_ready = true;
				}
			}
		}

		public Collection<MethodDefinition> ReadMethods(TypeDefinition type)
		{
			Range methods_range = type.methods_range;
			if (methods_range.Length == 0U)
			{
				return new MemberDefinitionCollection<MethodDefinition>(type);
			}
			MemberDefinitionCollection<MethodDefinition> memberDefinitionCollection = new MemberDefinitionCollection<MethodDefinition>(type, (int)methods_range.Length);
			if (!this.MoveTo(Table.MethodPtr, methods_range.Start))
			{
				if (!this.MoveTo(Table.Method, methods_range.Start))
				{
					return memberDefinitionCollection;
				}
				for (uint num = 0U; num < methods_range.Length; num += 1U)
				{
					this.ReadMethod(methods_range.Start + num, memberDefinitionCollection);
				}
			}
			else
			{
				this.ReadPointers<MethodDefinition>(Table.MethodPtr, Table.Method, methods_range, memberDefinitionCollection, new Action<uint, Collection<MethodDefinition>>(this.ReadMethod));
			}
			return memberDefinitionCollection;
		}

		private void ReadPointers<TMember>(Table ptr, Table table, Range range, Collection<TMember> members, Action<uint, Collection<TMember>> reader) where TMember : IMemberDefinition
		{
			for (uint num = 0U; num < range.Length; num += 1U)
			{
				this.MoveTo(ptr, range.Start + num);
				uint num2 = this.ReadTableIndex(table);
				this.MoveTo(table, num2);
				reader(num2, members);
			}
		}

		private static bool IsDeleted(IMemberDefinition member)
		{
			return member.IsSpecialName && member.Name == "_Deleted";
		}

		private void InitializeMethods()
		{
			if (this.metadata.Methods != null)
			{
				return;
			}
			this.metadata.Methods = new MethodDefinition[this.image.GetTableLength(Table.Method)];
		}

		private void ReadMethod(uint method_rid, Collection<MethodDefinition> methods)
		{
			MethodDefinition methodDefinition = new MethodDefinition();
			methodDefinition.rva = base.ReadUInt32();
			methodDefinition.ImplAttributes = (MethodImplAttributes)base.ReadUInt16();
			methodDefinition.Attributes = (MethodAttributes)base.ReadUInt16();
			methodDefinition.Name = this.ReadString();
			methodDefinition.token = new MetadataToken(TokenType.Method, method_rid);
			if (MetadataReader.IsDeleted(methodDefinition))
			{
				return;
			}
			methods.Add(methodDefinition);
			uint signature = this.ReadBlobIndex();
			Range range = this.ReadListRange(method_rid, Table.Method, Table.Param);
			this.context = methodDefinition;
			this.ReadMethodSignature(signature, methodDefinition);
			this.metadata.AddMethodDefinition(methodDefinition);
			if (range.Length != 0U)
			{
				int position = this.position;
				this.ReadParameters(methodDefinition, range);
				this.position = position;
			}
			if (this.module.IsWindowsMetadata())
			{
				WindowsRuntimeProjections.Project(methodDefinition);
			}
		}

		private void ReadParameters(MethodDefinition method, Range param_range)
		{
			if (this.MoveTo(Table.ParamPtr, param_range.Start))
			{
				this.ReadParameterPointers(method, param_range);
				return;
			}
			if (!this.MoveTo(Table.Param, param_range.Start))
			{
				return;
			}
			for (uint num = 0U; num < param_range.Length; num += 1U)
			{
				this.ReadParameter(param_range.Start + num, method);
			}
		}

		private void ReadParameterPointers(MethodDefinition method, Range range)
		{
			for (uint num = 0U; num < range.Length; num += 1U)
			{
				this.MoveTo(Table.ParamPtr, range.Start + num);
				uint num2 = this.ReadTableIndex(Table.Param);
				this.MoveTo(Table.Param, num2);
				this.ReadParameter(num2, method);
			}
		}

		private void ReadParameter(uint param_rid, MethodDefinition method)
		{
			ParameterAttributes attributes = (ParameterAttributes)base.ReadUInt16();
			ushort num = base.ReadUInt16();
			string name = this.ReadString();
			ParameterDefinition parameterDefinition = (num == 0) ? method.MethodReturnType.Parameter : method.Parameters[(int)(num - 1)];
			parameterDefinition.token = new MetadataToken(TokenType.Param, param_rid);
			parameterDefinition.Name = name;
			parameterDefinition.Attributes = attributes;
		}

		private void ReadMethodSignature(uint signature, IMethodSignature method)
		{
			this.ReadSignature(signature).ReadMethodSignature(method);
		}

		public PInvokeInfo ReadPInvokeInfo(MethodDefinition method)
		{
			this.InitializePInvokes();
			uint rid = method.token.RID;
			Row<PInvokeAttributes, uint, uint> row;
			if (!this.metadata.PInvokes.TryGetValue(rid, out row))
			{
				return null;
			}
			this.metadata.PInvokes.Remove(rid);
			return new PInvokeInfo(row.Col1, this.image.StringHeap.Read(row.Col2), this.module.ModuleReferences[(int)(row.Col3 - 1U)]);
		}

		private void InitializePInvokes()
		{
			if (this.metadata.PInvokes != null)
			{
				return;
			}
			int num = this.MoveTo(Table.ImplMap);
			Dictionary<uint, Row<PInvokeAttributes, uint, uint>> dictionary = this.metadata.PInvokes = new Dictionary<uint, Row<PInvokeAttributes, uint, uint>>(num);
			for (int i = 1; i <= num; i++)
			{
				PInvokeAttributes col = (PInvokeAttributes)base.ReadUInt16();
				MetadataToken metadataToken = this.ReadMetadataToken(CodedIndex.MemberForwarded);
				uint col2 = this.ReadStringIndex();
				uint col3 = this.ReadTableIndex(Table.File);
				if (metadataToken.TokenType == TokenType.Method)
				{
					dictionary.Add(metadataToken.RID, new Row<PInvokeAttributes, uint, uint>(col, col2, col3));
				}
			}
		}

		public bool HasGenericParameters(IGenericParameterProvider provider)
		{
			this.InitializeGenericParameters();
			Range[] ranges;
			return this.metadata.TryGetGenericParameterRanges(provider, out ranges) && MetadataReader.RangesSize(ranges) > 0;
		}

		public Collection<GenericParameter> ReadGenericParameters(IGenericParameterProvider provider)
		{
			this.InitializeGenericParameters();
			Range[] array;
			if (!this.metadata.TryGetGenericParameterRanges(provider, out array))
			{
				return new GenericParameterCollection(provider);
			}
			GenericParameterCollection genericParameterCollection = new GenericParameterCollection(provider, MetadataReader.RangesSize(array));
			for (int i = 0; i < array.Length; i++)
			{
				this.ReadGenericParametersRange(array[i], provider, genericParameterCollection);
			}
			return genericParameterCollection;
		}

		private void ReadGenericParametersRange(Range range, IGenericParameterProvider provider, GenericParameterCollection generic_parameters)
		{
			if (!this.MoveTo(Table.GenericParam, range.Start))
			{
				return;
			}
			for (uint num = 0U; num < range.Length; num += 1U)
			{
				base.ReadUInt16();
				GenericParameterAttributes attributes = (GenericParameterAttributes)base.ReadUInt16();
				this.ReadMetadataToken(CodedIndex.TypeOrMethodDef);
				generic_parameters.Add(new GenericParameter(this.ReadString(), provider)
				{
					token = new MetadataToken(TokenType.GenericParam, range.Start + num),
					Attributes = attributes
				});
			}
		}

		private void InitializeGenericParameters()
		{
			if (this.metadata.GenericParameters != null)
			{
				return;
			}
			this.metadata.GenericParameters = this.InitializeRanges(Table.GenericParam, delegate
			{
				base.Advance(4);
				MetadataToken result = this.ReadMetadataToken(CodedIndex.TypeOrMethodDef);
				this.ReadStringIndex();
				return result;
			});
		}

		private Dictionary<MetadataToken, Range[]> InitializeRanges(Table table, Func<MetadataToken> get_next)
		{
			int num = this.MoveTo(table);
			Dictionary<MetadataToken, Range[]> dictionary = new Dictionary<MetadataToken, Range[]>(num);
			if (num == 0)
			{
				return dictionary;
			}
			MetadataToken metadataToken = MetadataToken.Zero;
			Range range = new Range(1U, 0U);
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				MetadataToken metadataToken2 = get_next();
				if (num2 == 1U)
				{
					metadataToken = metadataToken2;
					range.Length += 1U;
				}
				else if (metadataToken2 != metadataToken)
				{
					MetadataReader.AddRange(dictionary, metadataToken, range);
					range = new Range(num2, 1U);
					metadataToken = metadataToken2;
				}
				else
				{
					range.Length += 1U;
				}
				num2 += 1U;
			}
			MetadataReader.AddRange(dictionary, metadataToken, range);
			return dictionary;
		}

		private static void AddRange(Dictionary<MetadataToken, Range[]> ranges, MetadataToken owner, Range range)
		{
			if (owner.RID == 0U)
			{
				return;
			}
			Range[] self;
			if (!ranges.TryGetValue(owner, out self))
			{
				ranges.Add(owner, new Range[]
				{
					range
				});
				return;
			}
			ranges[owner] = self.Add(range);
		}

		public bool HasGenericConstraints(GenericParameter generic_parameter)
		{
			this.InitializeGenericConstraints();
			Collection<Row<uint, MetadataToken>> collection;
			return this.metadata.TryGetGenericConstraintMapping(generic_parameter, out collection) && collection.Count > 0;
		}

		public GenericParameterConstraintCollection ReadGenericConstraints(GenericParameter generic_parameter)
		{
			this.InitializeGenericConstraints();
			Collection<Row<uint, MetadataToken>> collection;
			if (!this.metadata.TryGetGenericConstraintMapping(generic_parameter, out collection))
			{
				return new GenericParameterConstraintCollection(generic_parameter);
			}
			GenericParameterConstraintCollection genericParameterConstraintCollection = new GenericParameterConstraintCollection(generic_parameter, collection.Count);
			this.context = (IGenericContext)generic_parameter.Owner;
			for (int i = 0; i < collection.Count; i++)
			{
				genericParameterConstraintCollection.Add(new GenericParameterConstraint(this.GetTypeDefOrRef(collection[i].Col2), new MetadataToken(TokenType.GenericParamConstraint, collection[i].Col1)));
			}
			return genericParameterConstraintCollection;
		}

		private void InitializeGenericConstraints()
		{
			if (this.metadata.GenericConstraints != null)
			{
				return;
			}
			int num = this.MoveTo(Table.GenericParamConstraint);
			this.metadata.GenericConstraints = new Dictionary<uint, Collection<Row<uint, MetadataToken>>>(num);
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				this.AddGenericConstraintMapping(this.ReadTableIndex(Table.GenericParam), new Row<uint, MetadataToken>(num2, this.ReadMetadataToken(CodedIndex.TypeDefOrRef)));
				num2 += 1U;
			}
		}

		private void AddGenericConstraintMapping(uint generic_parameter, Row<uint, MetadataToken> constraint)
		{
			this.metadata.SetGenericConstraintMapping(generic_parameter, MetadataReader.AddMapping<uint, Row<uint, MetadataToken>>(this.metadata.GenericConstraints, generic_parameter, constraint));
		}

		public bool HasOverrides(MethodDefinition method)
		{
			this.InitializeOverrides();
			Collection<MetadataToken> collection;
			return this.metadata.TryGetOverrideMapping(method, out collection) && collection.Count > 0;
		}

		public Collection<MethodReference> ReadOverrides(MethodDefinition method)
		{
			this.InitializeOverrides();
			Collection<MetadataToken> collection;
			if (!this.metadata.TryGetOverrideMapping(method, out collection))
			{
				return new Collection<MethodReference>();
			}
			Collection<MethodReference> collection2 = new Collection<MethodReference>(collection.Count);
			this.context = method;
			for (int i = 0; i < collection.Count; i++)
			{
				collection2.Add((MethodReference)this.LookupToken(collection[i]));
			}
			return collection2;
		}

		private void InitializeOverrides()
		{
			if (this.metadata.Overrides != null)
			{
				return;
			}
			int num = this.MoveTo(Table.MethodImpl);
			this.metadata.Overrides = new Dictionary<uint, Collection<MetadataToken>>(num);
			for (int i = 1; i <= num; i++)
			{
				this.ReadTableIndex(Table.TypeDef);
				MetadataToken metadataToken = this.ReadMetadataToken(CodedIndex.MethodDefOrRef);
				if (metadataToken.TokenType != TokenType.Method)
				{
					throw new NotSupportedException();
				}
				MetadataToken @override = this.ReadMetadataToken(CodedIndex.MethodDefOrRef);
				this.AddOverrideMapping(metadataToken.RID, @override);
			}
		}

		private void AddOverrideMapping(uint method_rid, MetadataToken @override)
		{
			this.metadata.SetOverrideMapping(method_rid, MetadataReader.AddMapping<uint, MetadataToken>(this.metadata.Overrides, method_rid, @override));
		}

		public MethodBody ReadMethodBody(MethodDefinition method)
		{
			return this.code.ReadMethodBody(method);
		}

		public int ReadCodeSize(MethodDefinition method)
		{
			return this.code.ReadCodeSize(method);
		}

		public CallSite ReadCallSite(MetadataToken token)
		{
			if (!this.MoveTo(Table.StandAloneSig, token.RID))
			{
				return null;
			}
			uint signature = this.ReadBlobIndex();
			CallSite callSite = new CallSite();
			this.ReadMethodSignature(signature, callSite);
			callSite.MetadataToken = token;
			return callSite;
		}

		public VariableDefinitionCollection ReadVariables(MetadataToken local_var_token, MethodDefinition method = null)
		{
			if (!this.MoveTo(Table.StandAloneSig, local_var_token.RID))
			{
				return null;
			}
			SignatureReader signatureReader = this.ReadSignature(this.ReadBlobIndex());
			if (signatureReader.ReadByte() != 7)
			{
				throw new NotSupportedException();
			}
			uint num = signatureReader.ReadCompressedUInt32();
			if (num == 0U)
			{
				return null;
			}
			VariableDefinitionCollection variableDefinitionCollection = new VariableDefinitionCollection(method, (int)num);
			int num2 = 0;
			while ((long)num2 < (long)((ulong)num))
			{
				variableDefinitionCollection.Add(new VariableDefinition(signatureReader.ReadTypeSignature()));
				num2++;
			}
			return variableDefinitionCollection;
		}

		public IMetadataTokenProvider LookupToken(MetadataToken token)
		{
			uint rid = token.RID;
			if (rid == 0U)
			{
				return null;
			}
			if (this.metadata_reader != null)
			{
				return this.metadata_reader.LookupToken(token);
			}
			int position = this.position;
			IGenericContext genericContext = this.context;
			TokenType tokenType = token.TokenType;
			IMetadataTokenProvider result;
			if (tokenType <= TokenType.Field)
			{
				if (tokenType == TokenType.TypeRef)
				{
					result = this.GetTypeReference(rid);
					goto IL_D8;
				}
				if (tokenType == TokenType.TypeDef)
				{
					result = this.GetTypeDefinition(rid);
					goto IL_D8;
				}
				if (tokenType == TokenType.Field)
				{
					result = this.GetFieldDefinition(rid);
					goto IL_D8;
				}
			}
			else if (tokenType <= TokenType.MemberRef)
			{
				if (tokenType == TokenType.Method)
				{
					result = this.GetMethodDefinition(rid);
					goto IL_D8;
				}
				if (tokenType == TokenType.MemberRef)
				{
					result = this.GetMemberReference(rid);
					goto IL_D8;
				}
			}
			else
			{
				if (tokenType == TokenType.TypeSpec)
				{
					result = this.GetTypeSpecification(rid);
					goto IL_D8;
				}
				if (tokenType == TokenType.MethodSpec)
				{
					result = this.GetMethodSpecification(rid);
					goto IL_D8;
				}
			}
			return null;
			IL_D8:
			this.position = position;
			this.context = genericContext;
			return result;
		}

		public FieldDefinition GetFieldDefinition(uint rid)
		{
			this.InitializeTypeDefinitions();
			FieldDefinition fieldDefinition = this.metadata.GetFieldDefinition(rid);
			if (fieldDefinition != null)
			{
				return fieldDefinition;
			}
			return this.LookupField(rid);
		}

		private FieldDefinition LookupField(uint rid)
		{
			TypeDefinition fieldDeclaringType = this.metadata.GetFieldDeclaringType(rid);
			if (fieldDeclaringType == null)
			{
				return null;
			}
			Mixin.Read(fieldDeclaringType.Fields);
			return this.metadata.GetFieldDefinition(rid);
		}

		public MethodDefinition GetMethodDefinition(uint rid)
		{
			this.InitializeTypeDefinitions();
			MethodDefinition methodDefinition = this.metadata.GetMethodDefinition(rid);
			if (methodDefinition != null)
			{
				return methodDefinition;
			}
			return this.LookupMethod(rid);
		}

		private MethodDefinition LookupMethod(uint rid)
		{
			TypeDefinition methodDeclaringType = this.metadata.GetMethodDeclaringType(rid);
			if (methodDeclaringType == null)
			{
				return null;
			}
			Mixin.Read(methodDeclaringType.Methods);
			return this.metadata.GetMethodDefinition(rid);
		}

		private MethodSpecification GetMethodSpecification(uint rid)
		{
			if (!this.MoveTo(Table.MethodSpec, rid))
			{
				return null;
			}
			MethodReference method = (MethodReference)this.LookupToken(this.ReadMetadataToken(CodedIndex.MethodDefOrRef));
			uint signature = this.ReadBlobIndex();
			MethodSpecification methodSpecification = this.ReadMethodSpecSignature(signature, method);
			methodSpecification.token = new MetadataToken(TokenType.MethodSpec, rid);
			return methodSpecification;
		}

		private MethodSpecification ReadMethodSpecSignature(uint signature, MethodReference method)
		{
			SignatureReader signatureReader = this.ReadSignature(signature);
			if (signatureReader.ReadByte() != 10)
			{
				throw new NotSupportedException();
			}
			uint arity = signatureReader.ReadCompressedUInt32();
			GenericInstanceMethod genericInstanceMethod = new GenericInstanceMethod(method, (int)arity);
			signatureReader.ReadGenericInstanceSignature(method, genericInstanceMethod, arity);
			return genericInstanceMethod;
		}

		private MemberReference GetMemberReference(uint rid)
		{
			this.InitializeMemberReferences();
			MemberReference memberReference = this.metadata.GetMemberReference(rid);
			if (memberReference != null)
			{
				return memberReference;
			}
			memberReference = this.ReadMemberReference(rid);
			if (memberReference != null && !memberReference.ContainsGenericParameter)
			{
				this.metadata.AddMemberReference(memberReference);
			}
			return memberReference;
		}

		private MemberReference ReadMemberReference(uint rid)
		{
			if (!this.MoveTo(Table.MemberRef, rid))
			{
				return null;
			}
			MetadataToken metadataToken = this.ReadMetadataToken(CodedIndex.MemberRefParent);
			string name = this.ReadString();
			uint signature = this.ReadBlobIndex();
			TokenType tokenType = metadataToken.TokenType;
			MemberReference memberReference;
			if (tokenType <= TokenType.TypeDef)
			{
				if (tokenType != TokenType.TypeRef && tokenType != TokenType.TypeDef)
				{
					goto IL_73;
				}
			}
			else
			{
				if (tokenType == TokenType.Method)
				{
					memberReference = this.ReadMethodMemberReference(metadataToken, name, signature);
					goto IL_79;
				}
				if (tokenType != TokenType.TypeSpec)
				{
					goto IL_73;
				}
			}
			memberReference = this.ReadTypeMemberReference(metadataToken, name, signature);
			goto IL_79;
			IL_73:
			throw new NotSupportedException();
			IL_79:
			memberReference.token = new MetadataToken(TokenType.MemberRef, rid);
			return memberReference;
		}

		private MemberReference ReadTypeMemberReference(MetadataToken type, string name, uint signature)
		{
			TypeReference typeDefOrRef = this.GetTypeDefOrRef(type);
			if (!typeDefOrRef.IsArray)
			{
				this.context = typeDefOrRef;
			}
			MemberReference memberReference = this.ReadMemberReferenceSignature(signature, typeDefOrRef);
			memberReference.Name = name;
			return memberReference;
		}

		private MemberReference ReadMemberReferenceSignature(uint signature, TypeReference declaring_type)
		{
			SignatureReader signatureReader = this.ReadSignature(signature);
			if (signatureReader.buffer[signatureReader.position] == 6)
			{
				signatureReader.position++;
				return new FieldReference
				{
					DeclaringType = declaring_type,
					FieldType = signatureReader.ReadTypeSignature()
				};
			}
			MethodReference methodReference = new MethodReference();
			methodReference.DeclaringType = declaring_type;
			signatureReader.ReadMethodSignature(methodReference);
			return methodReference;
		}

		private MemberReference ReadMethodMemberReference(MetadataToken token, string name, uint signature)
		{
			MethodDefinition methodDefinition = this.GetMethodDefinition(token.RID);
			this.context = methodDefinition;
			MemberReference memberReference = this.ReadMemberReferenceSignature(signature, methodDefinition.DeclaringType);
			memberReference.Name = name;
			return memberReference;
		}

		private void InitializeMemberReferences()
		{
			if (this.metadata.MemberReferences != null)
			{
				return;
			}
			this.metadata.MemberReferences = new MemberReference[this.image.GetTableLength(Table.MemberRef)];
		}

		public IEnumerable<MemberReference> GetMemberReferences()
		{
			this.InitializeMemberReferences();
			int tableLength = this.image.GetTableLength(Table.MemberRef);
			TypeSystem typeSystem = this.module.TypeSystem;
			MethodDefinition methodDefinition = new MethodDefinition(string.Empty, MethodAttributes.Static, typeSystem.Void);
			methodDefinition.DeclaringType = new TypeDefinition(string.Empty, string.Empty, TypeAttributes.Public);
			MemberReference[] array = new MemberReference[tableLength];
			uint num = 1U;
			while ((ulong)num <= (ulong)((long)tableLength))
			{
				this.context = methodDefinition;
				array[(int)(num - 1U)] = this.GetMemberReference(num);
				num += 1U;
			}
			return array;
		}

		private void InitializeConstants()
		{
			if (this.metadata.Constants != null)
			{
				return;
			}
			int num = this.MoveTo(Table.Constant);
			Dictionary<MetadataToken, Row<ElementType, uint>> dictionary = this.metadata.Constants = new Dictionary<MetadataToken, Row<ElementType, uint>>(num);
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				ElementType col = (ElementType)base.ReadUInt16();
				MetadataToken key = this.ReadMetadataToken(CodedIndex.HasConstant);
				uint col2 = this.ReadBlobIndex();
				dictionary.Add(key, new Row<ElementType, uint>(col, col2));
				num2 += 1U;
			}
		}

		public TypeReference ReadConstantSignature(MetadataToken token)
		{
			if (token.TokenType != TokenType.Signature)
			{
				throw new NotSupportedException();
			}
			if (token.RID == 0U)
			{
				return null;
			}
			if (!this.MoveTo(Table.StandAloneSig, token.RID))
			{
				return null;
			}
			return this.ReadFieldType(this.ReadBlobIndex());
		}

		public object ReadConstant(IConstantProvider owner)
		{
			this.InitializeConstants();
			Row<ElementType, uint> row;
			if (!this.metadata.Constants.TryGetValue(owner.MetadataToken, out row))
			{
				return Mixin.NoValue;
			}
			this.metadata.Constants.Remove(owner.MetadataToken);
			return this.ReadConstantValue(row.Col1, row.Col2);
		}

		private object ReadConstantValue(ElementType etype, uint signature)
		{
			if (etype == ElementType.String)
			{
				return this.ReadConstantString(signature);
			}
			if (etype == ElementType.Class || etype == ElementType.Object)
			{
				return null;
			}
			return this.ReadConstantPrimitive(etype, signature);
		}

		private string ReadConstantString(uint signature)
		{
			byte[] bytes;
			int index;
			int num;
			this.GetBlobView(signature, out bytes, out index, out num);
			if (num == 0)
			{
				return string.Empty;
			}
			if ((num & 1) == 1)
			{
				num--;
			}
			return Encoding.Unicode.GetString(bytes, index, num);
		}

		private object ReadConstantPrimitive(ElementType type, uint signature)
		{
			return this.ReadSignature(signature).ReadConstantSignature(type);
		}

		internal void InitializeCustomAttributes()
		{
			if (this.metadata.CustomAttributes != null)
			{
				return;
			}
			this.metadata.CustomAttributes = this.InitializeRanges(Table.CustomAttribute, delegate
			{
				MetadataToken result = this.ReadMetadataToken(CodedIndex.HasCustomAttribute);
				this.ReadMetadataToken(CodedIndex.CustomAttributeType);
				this.ReadBlobIndex();
				return result;
			});
		}

		public bool HasCustomAttributes(ICustomAttributeProvider owner)
		{
			this.InitializeCustomAttributes();
			Range[] ranges;
			return this.metadata.TryGetCustomAttributeRanges(owner, out ranges) && MetadataReader.RangesSize(ranges) > 0;
		}

		public Collection<CustomAttribute> ReadCustomAttributes(ICustomAttributeProvider owner)
		{
			this.InitializeCustomAttributes();
			Range[] array;
			if (!this.metadata.TryGetCustomAttributeRanges(owner, out array))
			{
				return new Collection<CustomAttribute>();
			}
			Collection<CustomAttribute> collection = new Collection<CustomAttribute>(MetadataReader.RangesSize(array));
			for (int i = 0; i < array.Length; i++)
			{
				this.ReadCustomAttributeRange(array[i], collection);
			}
			if (this.module.IsWindowsMetadata())
			{
				foreach (CustomAttribute attribute in collection)
				{
					WindowsRuntimeProjections.Project(owner, collection, attribute);
				}
			}
			return collection;
		}

		private void ReadCustomAttributeRange(Range range, Collection<CustomAttribute> custom_attributes)
		{
			if (!this.MoveTo(Table.CustomAttribute, range.Start))
			{
				return;
			}
			int num = 0;
			while ((long)num < (long)((ulong)range.Length))
			{
				this.ReadMetadataToken(CodedIndex.HasCustomAttribute);
				MethodReference constructor = (MethodReference)this.LookupToken(this.ReadMetadataToken(CodedIndex.CustomAttributeType));
				uint signature = this.ReadBlobIndex();
				custom_attributes.Add(new CustomAttribute(signature, constructor));
				num++;
			}
		}

		private static int RangesSize(Range[] ranges)
		{
			uint num = 0U;
			for (int i = 0; i < ranges.Length; i++)
			{
				num += ranges[i].Length;
			}
			return (int)num;
		}

		public IEnumerable<CustomAttribute> GetCustomAttributes()
		{
			this.InitializeTypeDefinitions();
			uint length = this.image.TableHeap[Table.CustomAttribute].Length;
			Collection<CustomAttribute> collection = new Collection<CustomAttribute>((int)length);
			this.ReadCustomAttributeRange(new Range(1U, length), collection);
			return collection;
		}

		public byte[] ReadCustomAttributeBlob(uint signature)
		{
			return this.ReadBlob(signature);
		}

		public void ReadCustomAttributeSignature(CustomAttribute attribute)
		{
			SignatureReader signatureReader = this.ReadSignature(attribute.signature);
			if (!signatureReader.CanReadMore())
			{
				return;
			}
			if (signatureReader.ReadUInt16() != 1)
			{
				throw new InvalidOperationException();
			}
			MethodReference constructor = attribute.Constructor;
			if (constructor.HasParameters)
			{
				signatureReader.ReadCustomAttributeConstructorArguments(attribute, constructor.Parameters);
			}
			if (!signatureReader.CanReadMore())
			{
				return;
			}
			ushort num = signatureReader.ReadUInt16();
			if (num == 0)
			{
				return;
			}
			signatureReader.ReadCustomAttributeNamedArguments(num, ref attribute.fields, ref attribute.properties);
		}

		private void InitializeMarshalInfos()
		{
			if (this.metadata.FieldMarshals != null)
			{
				return;
			}
			int num = this.MoveTo(Table.FieldMarshal);
			Dictionary<MetadataToken, uint> dictionary = this.metadata.FieldMarshals = new Dictionary<MetadataToken, uint>(num);
			for (int i = 0; i < num; i++)
			{
				MetadataToken key = this.ReadMetadataToken(CodedIndex.HasFieldMarshal);
				uint value = this.ReadBlobIndex();
				if (key.RID != 0U)
				{
					dictionary.Add(key, value);
				}
			}
		}

		public bool HasMarshalInfo(IMarshalInfoProvider owner)
		{
			this.InitializeMarshalInfos();
			return this.metadata.FieldMarshals.ContainsKey(owner.MetadataToken);
		}

		public MarshalInfo ReadMarshalInfo(IMarshalInfoProvider owner)
		{
			this.InitializeMarshalInfos();
			uint signature;
			if (!this.metadata.FieldMarshals.TryGetValue(owner.MetadataToken, out signature))
			{
				return null;
			}
			SignatureReader signatureReader = this.ReadSignature(signature);
			this.metadata.FieldMarshals.Remove(owner.MetadataToken);
			return signatureReader.ReadMarshalInfo();
		}

		private void InitializeSecurityDeclarations()
		{
			if (this.metadata.SecurityDeclarations != null)
			{
				return;
			}
			this.metadata.SecurityDeclarations = this.InitializeRanges(Table.DeclSecurity, delegate
			{
				base.ReadUInt16();
				MetadataToken result = this.ReadMetadataToken(CodedIndex.HasDeclSecurity);
				this.ReadBlobIndex();
				return result;
			});
		}

		public bool HasSecurityDeclarations(ISecurityDeclarationProvider owner)
		{
			this.InitializeSecurityDeclarations();
			Range[] ranges;
			return this.metadata.TryGetSecurityDeclarationRanges(owner, out ranges) && MetadataReader.RangesSize(ranges) > 0;
		}

		public Collection<SecurityDeclaration> ReadSecurityDeclarations(ISecurityDeclarationProvider owner)
		{
			this.InitializeSecurityDeclarations();
			Range[] array;
			if (!this.metadata.TryGetSecurityDeclarationRanges(owner, out array))
			{
				return new Collection<SecurityDeclaration>();
			}
			Collection<SecurityDeclaration> collection = new Collection<SecurityDeclaration>(MetadataReader.RangesSize(array));
			for (int i = 0; i < array.Length; i++)
			{
				this.ReadSecurityDeclarationRange(array[i], collection);
			}
			return collection;
		}

		private void ReadSecurityDeclarationRange(Range range, Collection<SecurityDeclaration> security_declarations)
		{
			if (!this.MoveTo(Table.DeclSecurity, range.Start))
			{
				return;
			}
			int num = 0;
			while ((long)num < (long)((ulong)range.Length))
			{
				SecurityAction action = (SecurityAction)base.ReadUInt16();
				this.ReadMetadataToken(CodedIndex.HasDeclSecurity);
				uint signature = this.ReadBlobIndex();
				security_declarations.Add(new SecurityDeclaration(action, signature, this.module));
				num++;
			}
		}

		public byte[] ReadSecurityDeclarationBlob(uint signature)
		{
			return this.ReadBlob(signature);
		}

		public void ReadSecurityDeclarationSignature(SecurityDeclaration declaration)
		{
			uint signature = declaration.signature;
			SignatureReader signatureReader = this.ReadSignature(signature);
			if (signatureReader.buffer[signatureReader.position] != 46)
			{
				this.ReadXmlSecurityDeclaration(signature, declaration);
				return;
			}
			signatureReader.position++;
			uint num = signatureReader.ReadCompressedUInt32();
			Collection<SecurityAttribute> collection = new Collection<SecurityAttribute>((int)num);
			int num2 = 0;
			while ((long)num2 < (long)((ulong)num))
			{
				collection.Add(signatureReader.ReadSecurityAttribute());
				num2++;
			}
			declaration.security_attributes = collection;
		}

		private void ReadXmlSecurityDeclaration(uint signature, SecurityDeclaration declaration)
		{
			declaration.security_attributes = new Collection<SecurityAttribute>(1)
			{
				new SecurityAttribute(this.module.TypeSystem.LookupType("System.Security.Permissions", "PermissionSetAttribute"))
				{
					properties = new Collection<CustomAttributeNamedArgument>(1),
					properties = 
					{
						new CustomAttributeNamedArgument("XML", new CustomAttributeArgument(this.module.TypeSystem.String, this.ReadUnicodeStringBlob(signature)))
					}
				}
			};
		}

		public Collection<ExportedType> ReadExportedTypes()
		{
			int num = this.MoveTo(Table.ExportedType);
			if (num == 0)
			{
				return new Collection<ExportedType>();
			}
			Collection<ExportedType> collection = new Collection<ExportedType>(num);
			for (int i = 1; i <= num; i++)
			{
				TypeAttributes attributes = (TypeAttributes)base.ReadUInt32();
				uint identifier = base.ReadUInt32();
				string name = this.ReadString();
				string @namespace = this.ReadString();
				MetadataToken token = this.ReadMetadataToken(CodedIndex.Implementation);
				ExportedType declaringType = null;
				IMetadataScope scope = null;
				TokenType tokenType = token.TokenType;
				if (tokenType != TokenType.AssemblyRef && tokenType != TokenType.File)
				{
					if (tokenType == TokenType.ExportedType)
					{
						declaringType = collection[(int)(token.RID - 1U)];
					}
				}
				else
				{
					scope = this.GetExportedTypeScope(token);
				}
				ExportedType exportedType = new ExportedType(@namespace, name, this.module, scope)
				{
					Attributes = attributes,
					Identifier = (int)identifier,
					DeclaringType = declaringType
				};
				exportedType.token = new MetadataToken(TokenType.ExportedType, i);
				collection.Add(exportedType);
			}
			return collection;
		}

		private IMetadataScope GetExportedTypeScope(MetadataToken token)
		{
			int position = this.position;
			TokenType tokenType = token.TokenType;
			IMetadataScope result;
			if (tokenType != TokenType.AssemblyRef)
			{
				if (tokenType != TokenType.File)
				{
					throw new NotSupportedException();
				}
				this.InitializeModuleReferences();
				result = this.GetModuleReferenceFromFile(token);
			}
			else
			{
				this.InitializeAssemblyReferences();
				result = this.metadata.GetAssemblyNameReference(token.RID);
			}
			this.position = position;
			return result;
		}

		private ModuleReference GetModuleReferenceFromFile(MetadataToken token)
		{
			if (!this.MoveTo(Table.File, token.RID))
			{
				return null;
			}
			base.ReadUInt32();
			string text = this.ReadString();
			Collection<ModuleReference> moduleReferences = this.module.ModuleReferences;
			ModuleReference moduleReference;
			for (int i = 0; i < moduleReferences.Count; i++)
			{
				moduleReference = moduleReferences[i];
				if (moduleReference.Name == text)
				{
					return moduleReference;
				}
			}
			moduleReference = new ModuleReference(text);
			moduleReferences.Add(moduleReference);
			return moduleReference;
		}

		private void InitializeDocuments()
		{
			if (this.metadata.Documents != null)
			{
				return;
			}
			int num = this.MoveTo(Table.Document);
			Document[] array = this.metadata.Documents = new Document[num];
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				uint signature = this.ReadBlobIndex();
				Guid hashAlgorithmGuid = this.ReadGuid();
				byte[] hash = this.ReadBlob();
				Guid languageGuid = this.ReadGuid();
				string url = this.ReadSignature(signature).ReadDocumentName();
				array[(int)(num2 - 1U)] = new Document(url)
				{
					HashAlgorithmGuid = hashAlgorithmGuid,
					Hash = hash,
					LanguageGuid = languageGuid,
					token = new MetadataToken(TokenType.Document, num2)
				};
				num2 += 1U;
			}
		}

		public Collection<SequencePoint> ReadSequencePoints(MethodDefinition method)
		{
			this.InitializeDocuments();
			if (!this.MoveTo(Table.MethodDebugInformation, method.MetadataToken.RID))
			{
				return new Collection<SequencePoint>(0);
			}
			uint rid = this.ReadTableIndex(Table.Document);
			uint num = this.ReadBlobIndex();
			if (num == 0U)
			{
				return new Collection<SequencePoint>(0);
			}
			Document document = this.GetDocument(rid);
			return this.ReadSignature(num).ReadSequencePoints(document);
		}

		public Document GetDocument(uint rid)
		{
			Document document = this.metadata.GetDocument(rid);
			if (document == null)
			{
				return null;
			}
			document.custom_infos = this.GetCustomDebugInformation(document);
			return document;
		}

		private void InitializeLocalScopes()
		{
			if (this.metadata.LocalScopes != null)
			{
				return;
			}
			this.InitializeMethods();
			int num = this.MoveTo(Table.LocalScope);
			this.metadata.LocalScopes = new Dictionary<uint, Collection<Row<uint, Range, Range, uint, uint, uint>>>();
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				uint num3 = this.ReadTableIndex(Table.Method);
				uint col = this.ReadTableIndex(Table.ImportScope);
				Range col2 = this.ReadListRange(num2, Table.LocalScope, Table.LocalVariable);
				Range col3 = this.ReadListRange(num2, Table.LocalScope, Table.LocalConstant);
				uint col4 = base.ReadUInt32();
				uint col5 = base.ReadUInt32();
				this.metadata.SetLocalScopes(num3, MetadataReader.AddMapping<uint, Row<uint, Range, Range, uint, uint, uint>>(this.metadata.LocalScopes, num3, new Row<uint, Range, Range, uint, uint, uint>(col, col2, col3, col4, col5, num2)));
				num2 += 1U;
			}
		}

		public ScopeDebugInformation ReadScope(MethodDefinition method)
		{
			this.InitializeLocalScopes();
			this.InitializeImportScopes();
			Collection<Row<uint, Range, Range, uint, uint, uint>> collection;
			if (!this.metadata.TryGetLocalScopes(method, out collection))
			{
				return null;
			}
			ScopeDebugInformation scopeDebugInformation = null;
			for (int i = 0; i < collection.Count; i++)
			{
				ScopeDebugInformation scopeDebugInformation2 = this.ReadLocalScope(collection[i]);
				if (i == 0)
				{
					scopeDebugInformation = scopeDebugInformation2;
				}
				else if (!MetadataReader.AddScope(scopeDebugInformation.scopes, scopeDebugInformation2))
				{
					scopeDebugInformation.Scopes.Add(scopeDebugInformation2);
				}
			}
			return scopeDebugInformation;
		}

		private static bool AddScope(Collection<ScopeDebugInformation> scopes, ScopeDebugInformation scope)
		{
			if (scopes.IsNullOrEmpty<ScopeDebugInformation>())
			{
				return false;
			}
			foreach (ScopeDebugInformation scopeDebugInformation in scopes)
			{
				if (scopeDebugInformation.HasScopes && MetadataReader.AddScope(scopeDebugInformation.Scopes, scope))
				{
					return true;
				}
				if (scope.Start.Offset >= scopeDebugInformation.Start.Offset && scope.End.Offset <= scopeDebugInformation.End.Offset)
				{
					scopeDebugInformation.Scopes.Add(scope);
					return true;
				}
			}
			return false;
		}

		private ScopeDebugInformation ReadLocalScope(Row<uint, Range, Range, uint, uint, uint> record)
		{
			ScopeDebugInformation scopeDebugInformation = new ScopeDebugInformation
			{
				start = new InstructionOffset((int)record.Col4),
				end = new InstructionOffset((int)(record.Col4 + record.Col5)),
				token = new MetadataToken(TokenType.LocalScope, record.Col6)
			};
			if (record.Col1 > 0U)
			{
				scopeDebugInformation.import = this.metadata.GetImportScope(record.Col1);
			}
			if (record.Col2.Length > 0U)
			{
				scopeDebugInformation.variables = new Collection<VariableDebugInformation>((int)record.Col2.Length);
				for (uint num = 0U; num < record.Col2.Length; num += 1U)
				{
					VariableDebugInformation variableDebugInformation = this.ReadLocalVariable(record.Col2.Start + num);
					if (variableDebugInformation != null)
					{
						scopeDebugInformation.variables.Add(variableDebugInformation);
					}
				}
			}
			if (record.Col3.Length > 0U)
			{
				scopeDebugInformation.constants = new Collection<ConstantDebugInformation>((int)record.Col3.Length);
				for (uint num2 = 0U; num2 < record.Col3.Length; num2 += 1U)
				{
					ConstantDebugInformation constantDebugInformation = this.ReadLocalConstant(record.Col3.Start + num2);
					if (constantDebugInformation != null)
					{
						scopeDebugInformation.constants.Add(constantDebugInformation);
					}
				}
			}
			return scopeDebugInformation;
		}

		private VariableDebugInformation ReadLocalVariable(uint rid)
		{
			if (!this.MoveTo(Table.LocalVariable, rid))
			{
				return null;
			}
			VariableAttributes attributes = (VariableAttributes)base.ReadUInt16();
			int index = (int)base.ReadUInt16();
			string name = this.ReadString();
			VariableDebugInformation variableDebugInformation = new VariableDebugInformation(index, name)
			{
				Attributes = attributes,
				token = new MetadataToken(TokenType.LocalVariable, rid)
			};
			variableDebugInformation.custom_infos = this.GetCustomDebugInformation(variableDebugInformation);
			return variableDebugInformation;
		}

		private ConstantDebugInformation ReadLocalConstant(uint rid)
		{
			if (!this.MoveTo(Table.LocalConstant, rid))
			{
				return null;
			}
			string name = this.ReadString();
			SignatureReader signatureReader = this.ReadSignature(this.ReadBlobIndex());
			TypeReference typeReference = signatureReader.ReadTypeSignature();
			object value;
			if (typeReference.etype == ElementType.String)
			{
				if (!signatureReader.CanReadMore())
				{
					value = "";
				}
				else if (signatureReader.buffer[signatureReader.position] != 255)
				{
					byte[] array = signatureReader.ReadBytes((int)((ulong)signatureReader.sig_length - (ulong)((long)signatureReader.position - (long)((ulong)signatureReader.start))));
					value = Encoding.Unicode.GetString(array, 0, array.Length);
				}
				else
				{
					value = null;
				}
			}
			else if (typeReference.IsTypeOf("System", "Decimal"))
			{
				byte b = signatureReader.ReadByte();
				value = new decimal(signatureReader.ReadInt32(), signatureReader.ReadInt32(), signatureReader.ReadInt32(), (b & 128) > 0, b & 127);
			}
			else if (typeReference.IsTypeOf("System", "DateTime"))
			{
				value = new DateTime(signatureReader.ReadInt64());
			}
			else if (typeReference.etype == ElementType.Object || typeReference.etype == ElementType.None || typeReference.etype == ElementType.Class || typeReference.etype == ElementType.Array || typeReference.etype == ElementType.GenericInst)
			{
				value = null;
			}
			else
			{
				value = signatureReader.ReadConstantSignature(typeReference.etype);
			}
			ConstantDebugInformation constantDebugInformation = new ConstantDebugInformation(name, typeReference, value)
			{
				token = new MetadataToken(TokenType.LocalConstant, rid)
			};
			constantDebugInformation.custom_infos = this.GetCustomDebugInformation(constantDebugInformation);
			return constantDebugInformation;
		}

		private void InitializeImportScopes()
		{
			if (this.metadata.ImportScopes != null)
			{
				return;
			}
			int num = this.MoveTo(Table.ImportScope);
			this.metadata.ImportScopes = new ImportDebugInformation[num];
			for (int i = 1; i <= num; i++)
			{
				this.ReadTableIndex(Table.ImportScope);
				ImportDebugInformation importDebugInformation = new ImportDebugInformation();
				importDebugInformation.token = new MetadataToken(TokenType.ImportScope, i);
				SignatureReader signatureReader = this.ReadSignature(this.ReadBlobIndex());
				while (signatureReader.CanReadMore())
				{
					importDebugInformation.Targets.Add(this.ReadImportTarget(signatureReader));
				}
				this.metadata.ImportScopes[i - 1] = importDebugInformation;
			}
			this.MoveTo(Table.ImportScope);
			for (int j = 0; j < num; j++)
			{
				uint num2 = this.ReadTableIndex(Table.ImportScope);
				this.ReadBlobIndex();
				if (num2 != 0U)
				{
					this.metadata.ImportScopes[j].Parent = this.metadata.GetImportScope(num2);
				}
			}
		}

		public string ReadUTF8StringBlob(uint signature)
		{
			return this.ReadStringBlob(signature, Encoding.UTF8);
		}

		private string ReadUnicodeStringBlob(uint signature)
		{
			return this.ReadStringBlob(signature, Encoding.Unicode);
		}

		private string ReadStringBlob(uint signature, Encoding encoding)
		{
			byte[] bytes;
			int index;
			int num;
			this.GetBlobView(signature, out bytes, out index, out num);
			if (num == 0)
			{
				return string.Empty;
			}
			return encoding.GetString(bytes, index, num);
		}

		private ImportTarget ReadImportTarget(SignatureReader signature)
		{
			AssemblyNameReference reference = null;
			string @namespace = null;
			string alias = null;
			TypeReference type = null;
			ImportTargetKind kind = (ImportTargetKind)signature.ReadCompressedUInt32();
			switch (kind)
			{
			case ImportTargetKind.ImportNamespace:
				@namespace = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.ImportNamespaceInAssembly:
				reference = this.metadata.GetAssemblyNameReference(signature.ReadCompressedUInt32());
				@namespace = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.ImportType:
				type = signature.ReadTypeToken();
				break;
			case ImportTargetKind.ImportXmlNamespaceWithAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				@namespace = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.ImportAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.DefineAssemblyAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				reference = this.metadata.GetAssemblyNameReference(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.DefineNamespaceAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				@namespace = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.DefineNamespaceInAssemblyAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				reference = this.metadata.GetAssemblyNameReference(signature.ReadCompressedUInt32());
				@namespace = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.DefineTypeAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				type = signature.ReadTypeToken();
				break;
			}
			return new ImportTarget(kind)
			{
				alias = alias,
				type = type,
				@namespace = @namespace,
				reference = reference
			};
		}

		private void InitializeStateMachineMethods()
		{
			if (this.metadata.StateMachineMethods != null)
			{
				return;
			}
			int num = this.MoveTo(Table.StateMachineMethod);
			this.metadata.StateMachineMethods = new Dictionary<uint, uint>(num);
			for (int i = 0; i < num; i++)
			{
				this.metadata.StateMachineMethods.Add(this.ReadTableIndex(Table.Method), this.ReadTableIndex(Table.Method));
			}
		}

		public MethodDefinition ReadStateMachineKickoffMethod(MethodDefinition method)
		{
			this.InitializeStateMachineMethods();
			uint rid;
			if (!this.metadata.TryGetStateMachineKickOffMethod(method, out rid))
			{
				return null;
			}
			return this.GetMethodDefinition(rid);
		}

		private void InitializeCustomDebugInformations()
		{
			if (this.metadata.CustomDebugInformations != null)
			{
				return;
			}
			int num = this.MoveTo(Table.CustomDebugInformation);
			this.metadata.CustomDebugInformations = new Dictionary<MetadataToken, Row<Guid, uint, uint>[]>();
			uint num2 = 1U;
			while ((ulong)num2 <= (ulong)((long)num))
			{
				MetadataToken key = this.ReadMetadataToken(CodedIndex.HasCustomDebugInformation);
				Row<Guid, uint, uint> item = new Row<Guid, uint, uint>(this.ReadGuid(), this.ReadBlobIndex(), num2);
				Row<Guid, uint, uint>[] self;
				this.metadata.CustomDebugInformations.TryGetValue(key, out self);
				this.metadata.CustomDebugInformations[key] = self.Add(item);
				num2 += 1U;
			}
		}

		public bool HasCustomDebugInformation(ICustomDebugInformationProvider provider)
		{
			this.InitializeCustomDebugInformations();
			Row<Guid, uint, uint>[] array;
			return this.metadata.CustomDebugInformations.TryGetValue(provider.MetadataToken, out array) && array.Length != 0;
		}

		public Collection<CustomDebugInformation> GetCustomDebugInformation(ICustomDebugInformationProvider provider)
		{
			this.InitializeCustomDebugInformations();
			Row<Guid, uint, uint>[] array;
			if (!this.metadata.CustomDebugInformations.TryGetValue(provider.MetadataToken, out array))
			{
				return null;
			}
			Collection<CustomDebugInformation> collection = new Collection<CustomDebugInformation>(array.Length);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Col1 == StateMachineScopeDebugInformation.KindIdentifier)
				{
					SignatureReader signatureReader = this.ReadSignature(array[i].Col2);
					Collection<StateMachineScope> collection2 = new Collection<StateMachineScope>();
					while (signatureReader.CanReadMore())
					{
						int num = signatureReader.ReadInt32();
						int end = num + signatureReader.ReadInt32();
						collection2.Add(new StateMachineScope(num, end));
					}
					collection.Add(new StateMachineScopeDebugInformation
					{
						scopes = collection2
					});
				}
				else if (array[i].Col1 == AsyncMethodBodyDebugInformation.KindIdentifier)
				{
					SignatureReader signatureReader2 = this.ReadSignature(array[i].Col2);
					int catchHandler = signatureReader2.ReadInt32() - 1;
					Collection<InstructionOffset> collection3 = new Collection<InstructionOffset>();
					Collection<InstructionOffset> collection4 = new Collection<InstructionOffset>();
					Collection<MethodDefinition> collection5 = new Collection<MethodDefinition>();
					while (signatureReader2.CanReadMore())
					{
						collection3.Add(new InstructionOffset(signatureReader2.ReadInt32()));
						collection4.Add(new InstructionOffset(signatureReader2.ReadInt32()));
						collection5.Add(this.GetMethodDefinition(signatureReader2.ReadCompressedUInt32()));
					}
					collection.Add(new AsyncMethodBodyDebugInformation(catchHandler)
					{
						yields = collection3,
						resumes = collection4,
						resume_methods = collection5
					});
				}
				else if (array[i].Col1 == EmbeddedSourceDebugInformation.KindIdentifier)
				{
					collection.Add(new EmbeddedSourceDebugInformation(array[i].Col2, this));
				}
				else if (array[i].Col1 == SourceLinkDebugInformation.KindIdentifier)
				{
					collection.Add(new SourceLinkDebugInformation(Encoding.UTF8.GetString(this.ReadBlob(array[i].Col2))));
				}
				else
				{
					collection.Add(new BinaryCustomDebugInformation(array[i].Col1, this.ReadBlob(array[i].Col2)));
				}
				collection[i].token = new MetadataToken(TokenType.CustomDebugInformation, array[i].Col3);
			}
			return collection;
		}

		public byte[] ReadRawEmbeddedSourceDebugInformation(uint index)
		{
			SignatureReader signatureReader = this.ReadSignature(index);
			return signatureReader.ReadBytes((int)signatureReader.sig_length);
		}

		public Row<byte[], bool> ReadEmbeddedSourceDebugInformation(uint index)
		{
			SignatureReader signatureReader = this.ReadSignature(index);
			int num = signatureReader.ReadInt32();
			uint length = signatureReader.sig_length - 4U;
			if (num == 0)
			{
				return new Row<byte[], bool>(signatureReader.ReadBytes((int)length), false);
			}
			if (num > 0)
			{
				Stream stream = new MemoryStream(signatureReader.ReadBytes((int)length));
				byte[] array = new byte[num];
				MemoryStream destination = new MemoryStream(array);
				using (DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress, true))
				{
					deflateStream.CopyTo(destination);
				}
				return new Row<byte[], bool>(array, true);
			}
			throw new NotSupportedException();
		}

		internal readonly Image image;

		internal readonly ModuleDefinition module;

		internal readonly MetadataSystem metadata;

		internal CodeReader code;

		internal IGenericContext context;

		private readonly MetadataReader metadata_reader;
	}
}
