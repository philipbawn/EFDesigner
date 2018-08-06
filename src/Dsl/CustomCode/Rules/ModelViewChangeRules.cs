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

         EFModelDiagram diagram = store.ElementDirectory.AllElements.OfType<EFModelDiagram>().FirstOrDefault(d => d.Name == (string)e.OldValue) 
                               ?? store.ElementDirectory.AllElements.OfType<EFModelDiagram>().FirstOrDefault(d => d.Name == (string)e.NewValue);

         switch (e.DomainProperty.Name)
         {
            case "Name":
               if (store.ElementDirectory.AllElements.OfType<EFModelDiagram>().Any(d => d.Name == element.Name && d != diagram))
               {
                  current.Rollback();
                  ErrorDisplay.Show($"Diagram name '{element.Name}' already in use.");
               }
               else
               {
                  diagram.Name = element.Name;
               }

               break;
         }
      }
   }
}
