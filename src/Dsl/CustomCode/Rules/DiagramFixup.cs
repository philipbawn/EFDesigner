using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;

namespace Sawczyn.EFDesigner.EFModel
{
   /// <inheritdoc />
   /// <summary>
   ///    Rule that initiates view fixup when an element that has an associated shape is added to the model.
   /// </summary>
   [RuleOn(typeof(ModelClass), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddShapeParentExistRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(ModelEnum), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddShapeParentExistRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(BidirectionalAssociation), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddConnectionRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(UnidirectionalAssociation), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddConnectionRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(Comment), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddShapeParentExistRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(CommentReferencesSubjects), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddConnectionRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(Generalization), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddConnectionRulePriority, InitiallyDisabled = true)]
   // ReSharper disable once UnusedMember.Global
   internal class DiagramFixup : AddRule
   {
      [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
      public override void ElementAdded(ElementAddedEventArgs e)
      {
         if (e == null)
            throw new ArgumentNullException(nameof(e));

         ModelElement childElement = e.ModelElement;

         if (childElement.IsDeleted)
            return;

         ICollection<ModelElement> allElements = childElement.Store.ElementDirectory.AllElements;

         if (childElement is ElementLink)
         {
            foreach (ModelElement parentElement in allElements
                                                  .OfType<EFModelDiagram>()
                                                  .Select(diagram => GetParentForRelationship(diagram, (ElementLink)childElement))
                                                  .Where(parentElement => parentElement != null))
            {
               Diagram.FixUpDiagram(parentElement, childElement);
            }
         }
         else if (childElement is ModelClass ||
                  childElement is ModelEnum ||
                  childElement is Comment)
         {
            Diagram.FixUpDiagram(allElements.OfType<ModelRoot>().Single(), childElement);
         }

      }

      private static ModelElement GetParentForRelationship(EFModelDiagram diagram, ElementLink elementLink)
      {
         ReadOnlyCollection<ModelElement> linkedElements = elementLink.LinkedElements;

         if (linkedElements.Count == 2)
         {
            ShapeElement sourceShape = linkedElements[0] as ShapeElement ?? PresentationViewsSubject.GetPresentation(linkedElements[0]).OfType<ShapeElement>().FirstOrDefault(s => s.Diagram == diagram);
            ShapeElement targetShape = linkedElements[1] as ShapeElement ?? PresentationViewsSubject.GetPresentation(linkedElements[1]).OfType<ShapeElement>().FirstOrDefault(s => s.Diagram == diagram);

            if (sourceShape == null || targetShape == null)
               return null;

            ShapeElement sourceParent = sourceShape.ParentShape;
            ShapeElement targetParent = targetShape.ParentShape;

            while (sourceParent != targetParent && sourceParent != null)
            {
               ShapeElement curParent = targetParent;

               while (sourceParent != curParent && curParent != null)
                  curParent = curParent.ParentShape;

               if (sourceParent == curParent)
                  break;

               sourceParent = sourceParent.ParentShape;
            }

            while (sourceParent != null && !(sourceParent is EFModelDiagram))
               sourceParent = sourceParent.ParentShape;

            Debug.Assert(sourceParent?.ModelElement != null, "Unable to find common parent for view fixup.");
            return sourceParent.ModelElement;
         }

         return null;
      }
   }

   partial class FixUpDiagram
   {
      protected override bool SkipFixup(ModelElement childElement)
      {
         // disables this class should it ever get used
         return true;
      }
   }
}
