using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Modeling;

namespace Sawczyn.EFDesigner.EFModel
{
   [RuleOn(typeof(ModelView), FireTime = TimeToFire.TopLevelCommit)]
   internal class ModelViewAddRules : AddRule
   {
      public override void ElementAdded(ElementAddedEventArgs e)
      {
         base.ElementAdded(e);

         ModelView element = (ModelView)e.ModelElement;
         Store store = element.Store;
         Transaction current = store.TransactionManager.CurrentTransaction;

         if (current.IsSerializing)
            return;

        // TODO: Add diagram 

      }
   }
}
