
using Microsoft.VisualStudio.Modeling;

namespace Sawczyn.EFDesigner.EFModel
{
   [RuleOn(typeof(EFModelDiagramProxy), FireTime = TimeToFire.Inline)]
   internal class DiagramProxyAddRules : AddRule
   {
      public override void ElementAdded(ElementAddedEventArgs e)
      {

         EFModelDiagramProxy element = (EFModelDiagramProxy)e.ModelElement;
         Store store = element.Store;
         Transaction current = store.TransactionManager.CurrentTransaction;

         if (current.IsSerializing)
            return;


      }


   }
}
