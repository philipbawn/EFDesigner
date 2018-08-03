using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Modeling;

namespace Sawczyn.EFDesigner.EFModel
{
   [RuleOn(typeof(ModelView), FireTime = TimeToFire.TopLevelCommit)]
   internal class ModelViewChangeRules : ChangeRule
   {
      public override void ElementPropertyChanged(ElementPropertyChangedEventArgs e)
      {
         base.ElementPropertyChanged(e);

         ModelView element = (ModelView)e.ModelElement;
         Store store = element.Store;
         Transaction current = store.TransactionManager.CurrentTransaction;

         if (current.IsSerializing)
            return;

         if (Equals(e.NewValue, e.OldValue))
            return;

         switch (e.DomainProperty.Name)
         {
            case "Name":
               // TODO: Change name of current diagram
               break;
         }
      }
   }
}
