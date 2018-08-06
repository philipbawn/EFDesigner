using System.ComponentModel;

using Microsoft.VisualStudio.Modeling.Shell;
using Microsoft.VisualStudio.Shell;

using Sawczyn.EFDesigner.EFModel.DslPackage.CustomCode;

namespace Sawczyn.EFDesigner.EFModel
{
   [ProvideEditorLogicalView(typeof(EFModelEditorFactory), Constants.LogicalViewValue, IsTrusted = true)]
   [ProvideRelatedFile("." + Constants.DesignerFileExtension, Constants.DiagramxExtension, ProjectSystem = ProvideRelatedFileAttribute.CSharpProjectGuid, FileOptions = RelatedFileType.FileName)]
   internal sealed partial class EFModelPackage
   {
      protected override void Initialize()
      {
         TypeDescriptor.AddProvider(new ModelClassTypeDescriptionProvider(), typeof(ModelClass));
         TypeDescriptor.AddProvider(new ModelEnumTypeDescriptionProvider(), typeof(ModelEnum));
         TypeDescriptor.AddProvider(new AssociationTypeDescriptionProvider(), typeof(Association));
         TypeDescriptor.AddProvider(new ModelAttributeTypeDescriptionProvider(), typeof(ModelAttribute));
         TypeDescriptor.AddProvider(new ModelRootTypeDescriptionProvider(), typeof(ModelRoot));

         Messages.Initialize(this);

         base.Initialize();
      }
   }
}
