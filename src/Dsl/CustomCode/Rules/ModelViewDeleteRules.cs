using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Modeling;

namespace Sawczyn.EFDesigner.EFModel
{
   [RuleOn(typeof(ModelView), FireTime = TimeToFire.TopLevelCommit)]
   public class ModelViewDeleteRules : DeleteRule
   {
      /// <summary>
      /// public virtual method for the client to have his own user-defined delete rule class
      /// </summary>
      /// <param name="e"></param>
      public override void ElementDeleted(ElementDeletedEventArgs e)
      {
         base.ElementDeleted(e);

         ModelView element = (ModelView)e.ModelElement;
         Store store = element.Store;
         Transaction current = store.TransactionManager.CurrentTransaction;

         if (current.IsSerializing)
            return;

         EFModelDiagram diagram = store.ElementDirectory.AllElements.OfType<EFModelDiagram>().FirstOrDefault(d => d.Name == element.Name);
         diagram?.Delete();
      }
   }
}
