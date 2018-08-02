using System;
using System.Diagnostics;

using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Validation;

namespace Sawczyn.EFDesigner.EFModel
{
   internal sealed class SerializationValidationObserver : ValidationMessageObserver, IDisposable
   {
      /// <summary>
      ///    Called with validation messages are added.
      /// </summary>
      protected override void OnValidationMessageAdded(ValidationMessage addedMessage)
      {
#region Check Parameters

         Debug.Assert(addedMessage != null);

#endregion

         if (addedMessage != null && serializationResult != null)
         {
            // Record the validation message as a serialization message.
            SerializationUtilities.AddValidationMessage(serializationResult, addedMessage);
         }

         base.OnValidationMessageAdded(addedMessage);
      }

      /// <summary>
      ///    SerializationResult to store the messages.
      /// </summary>
      private SerializationResult serializationResult;

      /// <summary>
      ///    ValidationController to get messages from.
      /// </summary>
      private ValidationController validationController;

      /// <summary>
      ///    Constructor
      /// </summary>
      internal SerializationValidationObserver(SerializationResult serializationResult, ValidationController validationController)
      {
#region Check Parameters

         Debug.Assert(serializationResult != null);
         Debug.Assert(validationController != null);

#endregion

         this.serializationResult = serializationResult;
         this.validationController = validationController;

         // Subscribe to validation messages.
         this.validationController.AddObserver(this);
      }

      /// <summary>
      ///    Destructor
      /// </summary>
      ~SerializationValidationObserver()
      {
         Dispose(false);
      }

      /// <summary>
      ///    IDisposable.Dispose().
      /// </summary>
      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      /// <summary>
      ///    Unregister the observer on dispose.
      /// </summary>
      private void Dispose(bool disposing)
      {
         Debug.Assert(disposing, "SerializationValidationObserver finalized without being disposed!");

         if (disposing && validationController != null)
         {
            validationController.RemoveObserver(this);
            validationController = null;
         }

         serializationResult = null;
      }
   }
}
