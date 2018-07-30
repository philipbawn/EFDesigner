using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;

namespace Sawczyn.EFDesigner.EFModel.CustomCode.Rules
{
   /// <summary>
   /// Rule that initiates view fixup when an element that has an associated shape is added to the model. 
   /// </summary>
   [RuleOn(typeof(ModelClass), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddShapeParentExistRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(ModelEnum), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddShapeParentExistRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(BidirectionalAssociation), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddConnectionRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(UnidirectionalAssociation), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddConnectionRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(Comment), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddShapeParentExistRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(CommentReferencesSubjects), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddConnectionRulePriority, InitiallyDisabled = true)]
   [RuleOn(typeof(Generalization), FireTime = TimeToFire.TopLevelCommit, Priority = DiagramFixupConstants.AddConnectionRulePriority, InitiallyDisabled = true)]
   internal sealed partial class FixupDiagramIOnElementAddedRule : FixUpDiagramBase
   {
      internal static void Fixup(Diagram diagram, ModelElement childElement)
      {
         
         if (childElement.IsDeleted)
            return;

         ModelElement parentElement;
         if (childElement is ElementLink)
         {
            parentElement = GetParentForRelationship(diagram, (ElementLink)childElement);
         }
         else
             if (childElement is global::Sparta.Panoptes.MultipleAssociation)
         {
            parentElement = GetParentForMultipleAssociation((global::Sparta.Panoptes.MultipleAssociation)childElement);
         }
         else
                 if (childElement is global::Sparta.Panoptes.ClassOperation)
         {
            parentElement = GetParentForClassOperation((global::Sparta.Panoptes.ClassOperation)childElement);
         }
         else
                     if (childElement is global::Sparta.Panoptes.ModelInterface)
         {
            parentElement = GetParentForModelInterface((global::Sparta.Panoptes.ModelInterface)childElement);
         }
         else
                         if (childElement is global::Sparta.Panoptes.ModelClass)
         {
            parentElement = GetParentForModelClass((global::Sparta.Panoptes.ModelClass)childElement);
         }
         else
                             if (childElement is global::Sparta.Panoptes.ModelNamespace)
         {
            parentElement = GetParentForModelNamespace((global::Sparta.Panoptes.ModelNamespace)childElement);
         }
         else
                                 if (childElement is global::Sparta.Panoptes.Comment)
         {
            parentElement = GetParentForComment((global::Sparta.Panoptes.Comment)childElement);
         }
         else
         {
            parentElement = null;
         }
         if (parentElement != null)
         {
            DslDiagrams::Diagram.FixUpDiagram(parentElement, childElement);
         }
      }


      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
      public override void ElementAdded(ElementAddedEventArgs e)
      {
         if (e == null)
            throw new ArgumentNullException(nameof(e));

         ModelElement childElement = e.ModelElement;

         if (SkipFixup(childElement))
            return;

         ModelElement parentElement = null;

         if (childElement is ElementLink link)
            parentElement = GetParentForRelationship(link);
         else if (childElement is ModelClass @class)
            parentElement = @class.ModelRoot;
         else if (childElement is ModelEnum @enum)
            parentElement = @enum.ModelRoot;
         else if (childElement is Comment comment)
            parentElement = comment.ModelRoot;

         if (parentElement != null)
            Diagram.FixUpDiagram(parentElement, childElement);
      }

      private static ModelElement GetParentForRelationship(ElementLink elementLink)
      {
         System.Collections.ObjectModel.ReadOnlyCollection<ModelElement> linkedElements = elementLink.LinkedElements;

         if (linkedElements.Count == 2)
         {
            ShapeElement sourceShape = linkedElements[0] as ShapeElement;
            ShapeElement targetShape = linkedElements[1] as ShapeElement;

            if (sourceShape == null)
            {
               LinkedElementCollection<PresentationElement> presentationElements = PresentationViewsSubject.GetPresentation(linkedElements[0]);
               foreach (PresentationElement presentationElement in presentationElements)
               {
                  ShapeElement shape = presentationElement as ShapeElement;
                  if (shape != null)
                  {
                     sourceShape = shape;
                     break;
                  }
               }
            }

            if (targetShape == null)
            {
               LinkedElementCollection<PresentationElement> presentationElements = PresentationViewsSubject.GetPresentation(linkedElements[1]);
               foreach (PresentationElement presentationElement in presentationElements)
               {
                  ShapeElement shape = presentationElement as ShapeElement;
                  if (shape != null)
                  {
                     targetShape = shape;
                     break;
                  }
               }
            }

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

            while (sourceParent != null)
            {
               // ensure that the parent can parent connectors (i.e., a diagram).
               if (sourceParent is Diagram)
                  break;

               sourceParent = sourceParent.ParentShape;
            }

            return sourceParent.ModelElement;
         }

         return null;
      }
   }

}
