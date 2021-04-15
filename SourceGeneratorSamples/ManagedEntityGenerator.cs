using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGeneratorSamples
{
	[Generator]
	public class PersistedGenerator : ISourceGenerator

	{
		private const string attributeText =
			@"using System;
				namespace Persisted
				{
					public enum PersistenceType
					{
						Direct, Nested, NestedCollection
					}

					[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
					[System.Diagnostics.Conditional(""PersistedGenerator_DEBUG"")]
					public class ManagedAttribute : Attribute
					{
						public ManagedAttribute() { }

						public bool EnableAudit { get; set; }
						public bool EnableSoftDelete { get; set; }
					}

					[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
					[System.Diagnostics.Conditional(""PersistedGenerator_DEBUG"")]
					public class PersistedAttribute : Attribute
					{
						public PersistedAttribute(PersistenceType persistedType)
						{
							PersistedVia = persistedType;
						}
						public PersistedAttribute()
						{ }

						public PersistenceType PersistedVia { get; protected set; } = PersistenceType.Direct;
						public string PropertyName { get; set; } = null;
						public string Prefix { get; set; } = null;
						public Type TypeOverride { get; set; } = null;
						public bool SetOnInsert { get; set; }
						public bool SetOnUpdate { get; set; }
						public bool SetOnDelete { get; set; }
						public bool SetAllActions {
							get =>
								SetOnInsert && SetOnUpdate && SetOnDelete;
							set { setAll(value); }
						}

						private void setAll(bool value)
						{
							SetOnInsert = value;
							SetOnUpdate = value;
							SetOnDelete = value;
						}
					}
				}";

		public void Initialize(GeneratorInitializationContext context)
		{
			// Register the attribute source
			context.RegisterForPostInitialization((i) => i.AddSource("PersistedAttribute", attributeText));

			// Register a syntax receiver that will be created for each generation pass
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			// retrieve the populated receiver 
			if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
				return;

			// get the added attribute, and INotifyPropertyChanged
			INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName("Persisted.PersistedAttribute");
			//INamedTypeSymbol notifySymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");

			// group the fields by class, and generate the source
			foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in receiver.Fields.GroupBy(f => f.ContainingType))
			{
				string classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, context);
				//string classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, notifySymbol, context);
				context.AddSource($"{group.Key.Name}_managedEntity.cs", SourceText.From(classSource, Encoding.UTF8));
			}
		}

		private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, GeneratorExecutionContext context)
		{
			if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
			{
				return null; //TODO: issue a diagnostic that it must be top level
			}

			INamedTypeSymbol classAttributeSymbol = context.Compilation.GetTypeByMetadataName("Persisted.ManagedAttribute");

			// get the Managed attribute from the class
			AttributeData classAttributeData =
				classSymbol
					.GetAttributes()
						.Single(a =>
							a.AttributeClass
								.Equals(classAttributeSymbol, SymbolEqualityComparer.Default));

			TypedConstant argEnableAudit =
				classAttributeData
					.NamedArguments
						.SingleOrDefault(kvp =>
							kvp.Key == "EnableAudit")
								.Value;

			TypedConstant argEnableSoftDelete =
				classAttributeData
					.NamedArguments
						.SingleOrDefault(kvp =>
							kvp.Key == "EnableSoftDelete")
								.Value;

			bool enableAudit = 
				argEnableAudit.IsNull 
				? false 
				: argEnableAudit.Value.Equals(true);
			
			bool enableSoftDelete = argEnableSoftDelete.IsNull ? false : argEnableSoftDelete.Value.Equals(true);

			string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
			// begin building the generated source
			StringBuilder source = new StringBuilder($@"
using System;

namespace {namespaceName}
{{
    public partial class {classSymbol.Name} 
    {{
				public Guid Id {{get; set;}} = Guid.NewGuid();
");
			StringBuilder mappingCreate = new StringBuilder();
			StringBuilder mappingUpdate = new StringBuilder();
			StringBuilder mappingDelete = new StringBuilder();

			// create mappings 
			foreach (IFieldSymbol fieldSymbol in fields)
			{
				// get the Persisted attribute from the field
				AttributeData attributeData =
					fieldSymbol
						.GetAttributes()
							.Single(a =>
								a.AttributeClass
									.Equals(attributeSymbol, SymbolEqualityComparer.Default));

				string propertyName =
					CreatePropertyForField(source, fieldSymbol, attributeData);

				if (!attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "SetOnInsert").Value.IsNull)
					mappingCreate.Append($@"
				toAuthority.{propertyName} = this.{propertyName};
");
				if (!attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "SetOnUpdate").Value.IsNull)
					mappingUpdate.Append($@"
				toAuthority.{propertyName} = this.{propertyName};
");
				if (!attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "SetOnDelete").Value.IsNull)
					mappingDelete.Append($@"
				toAuthority.{propertyName} = this.{propertyName};
");
			}

			if(enableAudit)
			{
				source.Append($@"
				#region IAuditable

				public DateTime CreatedDate {{ get; set; }}
				public string CreatedBy {{ get; set; }}
				public DateTime? ModifiedLastDate {{ get; set; }} = null;
				public string ModifiedLastBy {{ get; set; }} = null;

				#endregion IAuditable
");
				mappingCreate.Append($@"
				toAuthority.CreatedDate = DateTime.UtcNow;
				toAuthority.CreatedBy = this.CreatedBy;
");
				mappingUpdate.Append($@"
				toAuthority.ModifiedLastDate = DateTime.UtcNow;
				toAuthority.ModifiedLastBy = this.ModifiedLastBy;
");
			}

			if(enableSoftDelete)
			{
				source.Append($@"
				#region ISoftDelete

				public bool IsDeleted {{ get; set; }}
				public DateTime? DeletedDate {{ get; set; }}
				public string DeletedBy {{ get; set; }}

				#endregion ISoftDelete
");
				mappingDelete.Append($@"
				toAuthority.IsDeleted = true;
				toAuthority.DeletedDate = DateTime.UtcNow;
				toAuthority.DeletedBy = this.DeletedBy;
");
			}

			CreateAuthorityMapper(source, classSymbol, mappingCreate, "Insert");
			CreateAuthorityMapper(source, classSymbol, mappingUpdate, "Update");
			CreateAuthorityMapper(source, classSymbol, mappingDelete, "Delete");

			source.Append("} }");
			return source.ToString();
		}

		private string CreatePropertyForField(StringBuilder source, IFieldSymbol fieldSymbol, AttributeData attributeData)
		{
			// get the name and type of the field
			string fieldName = fieldSymbol.Name;
			ITypeSymbol fieldType = fieldSymbol.Type;

			TypedConstant overriddenNameOpt = 
				attributeData
					.NamedArguments
						.SingleOrDefault(kvp => 
							kvp.Key == "PropertyName")
						.Value;

			string propertyName = chooseName(fieldName, overriddenNameOpt);
			if (propertyName.Length == 0 || propertyName == fieldName)
			{
				//TODO: issue a diagnostic that we can't process this field
				return String.Empty;
			}

			source.Append($@"
				public {fieldType} {propertyName} 
				{{
						get 
						{{
								return this.{fieldName};
						}}

						set
						{{
								this.{fieldName} = value;
						}}
				}}

");
			return propertyName;

			string chooseName(string fieldName, TypedConstant overridenNameOpt)
			{
				if (!overridenNameOpt.IsNull)
					return overridenNameOpt.Value.ToString();

				fieldName = fieldName.TrimStart('_');
				if (fieldName.Length == 0)
					return string.Empty;

				if (fieldName.Length == 1)
					return fieldName.ToUpper();

				return fieldName
					.Substring(0, 1)
						.ToUpper() 
					+ fieldName.Substring(1);
			}

		}

		private void CreateAuthorityMapper(StringBuilder source, INamedTypeSymbol classSymbol, StringBuilder mappedProperties, string actionName)
		{
			source.Append($@"
				internal {classSymbol.Name} MapToAuthority{actionName}({classSymbol.Name} toAuthority) 
				{{
						{mappedProperties.ToString()}
				return toAuthority;
				}} 
");

		}

		/// <summary>
		/// Created on demand before each generation pass
		/// </summary>
		class SyntaxReceiver : ISyntaxContextReceiver
		{
			public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();
			public List<ITypeSymbol> Classes { get; } = new List<ITypeSymbol>();

			/// <summary>
			/// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
			/// </summary>
			public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
			{
				// any class with at least one attribute is a candidate for generation 
				if (context.Node is ClassDeclarationSyntax classDeclarationSyntax
						&& classDeclarationSyntax.AttributeLists.Count > 0)
				{
					ITypeSymbol classModel = (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node);
					if (classModel.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "Persisted.ManagedAttribute"))
						Classes.Add(classModel);
				}

				// any field with at least one attribute is a candidate for generation 
				if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
						&& fieldDeclarationSyntax.AttributeLists.Count > 0)
				{
					foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
					{
						// Get the symbol being declared by the field, and keep it if its annotated
						IFieldSymbol fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
						if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "Persisted.PersistedAttribute"))
							Fields.Add(fieldSymbol);
					}
				}
			}
		}
	}
}
