using System;
using System.Linq;
using Sawczyn.EFDesigner.EFModel.CustomCode.Rules;

namespace Sawczyn.EFDesigner.EFModel
{
   public partial class EFModelDomainModel
   {
      private static readonly Type[] RuleClasses =
      {
         typeof(AssociationAddRules),
         typeof(AssociationChangeRules),
         typeof(GeneralizationAddRules),
         typeof(GeneralizationChangeRules),
         typeof(ModelAttributeAddRules),
         typeof(ModelAttributeChangeRules),
         typeof(ModelClassAddRules),
         typeof(ModelClassChangeRules),
         typeof(ModelEnumChangeRules),
         typeof(ModelEnumValueAddRules),
         typeof(ModelEnumValueChangeRules),
         typeof(ModelRootChangeRules),
         typeof(ModelViewAddRules),
         typeof(ModelViewChangeRules),
         typeof(ModelViewDeleteRules)
      };

      protected override Type[] GetCustomDomainModelTypes()
      {
         return base.GetCustomDomainModelTypes().Concat(RuleClasses).ToArray();
      }
   }
}
