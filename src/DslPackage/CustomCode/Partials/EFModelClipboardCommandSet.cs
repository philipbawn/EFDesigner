//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Design;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//using Microsoft.VisualStudio.Modeling.Shell;

//namespace Sawczyn.EFDesigner.EFModel
//{
//   partial class EFModelClipboardCommandSet
//   {
//      /// <summary>Virtual method to process the menu Cut operation</summary>
//      /// <remarks>
//      /// This is provided so the behavior can be overridden;
//      /// FxCop does not allow ProcessOnStatusCutCommand() to be protected because of the EventArgs parameter.
//      /// </remarks>
//      protected virtual void ProcessOnStatusCutCommand(MenuCommand command)
//      {
//         ProcessOnStatusCopyCommand(command);
//         if (command.Enabled)
//            command.Enabled = CanDeleteSelectedItems();
//         command.Visible = true;
//      }

//   }
//}
