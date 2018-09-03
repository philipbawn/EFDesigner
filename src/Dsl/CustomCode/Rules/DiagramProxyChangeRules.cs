
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Modeling;

namespace Sawczyn.EFDesigner.EFModel.CustomCode.Rules
{
   [RuleOn(typeof(EFModelDiagramProxy), FireTime = TimeToFire.TopLevelCommit)]
   public class DiagramProxyChangeRules : ChangeRule
   {
      public override void ElementPropertyChanged(ElementPropertyChangedEventArgs e)
      {
         base.ElementPropertyChanged(e);

         EFModelDiagramProxy element = (EFModelDiagramProxy)e.ModelElement;
         Store store = element.Store;
         Transaction current = store.TransactionManager.CurrentTransaction;

         if (current.IsSerializing)
            return;

         if (Equals(e.NewValue, e.OldValue))
            return;

         List<string> errorMessages = EFCoreValidator.GetErrors(element).ToList();
         string oldName = (string)e.OldValue;
         string newName = (string)e.NewValue;

         switch (e.DomainProperty.Name)
         {
            case "Name":

               if (oldName == "Default")
                  errorMessages.Add("Can't rename default diagram");
               else if (string.IsNullOrEmpty(newName))
                  errorMessages.Add("Diagram must have a name");
               else if (store.ElementDirectory.AllElements.OfType<EFModelDiagramProxy>().Any(d => d.Name == newName && d.Id != element.Id))
                  errorMessages.Add($"A diagram named {newName} already exists");
               else
               {
                  EFModelDiagram diagram = store
                                          .DefaultPartitionForClass(EFModelDiagram.DomainClassId)
                                          .ElementDirectory
                                          .AllElements
                                          .OfType<EFModelDiagram>()
                                          .FirstOrDefault(d => d.Name == newName || 
                                                               (d.Name == null && newName == "Default"));

                  diagram.Name = newName;
               }
               break;
         }

         errorMessages = errorMessages.Where(m => m != null).ToList();
         if (errorMessages.Any())
         {
            current.Rollback();
            ErrorDisplay.Show(string.Join("\n", errorMessages));
         }
      }
   }
}
