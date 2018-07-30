using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sawczyn.EFDesigner.EFModel
{
    partial class EFModelEditorFactory
    {
        /// <summary>
        /// Called when the shell asks us to map a logical view to a physical one.  Logical
        /// views correspond to view types, physical views correspond to view instances.  Because we potentially
        /// want to support multiple physical views of a given logical view open at once, we
        /// also pass along an object which derived classes can use to differentiate the physical
        /// views.  For example, in the case of multiple web services being viewed in the Service Designer,
        /// the logical view (GUID of the Service Designer) would be the same, but the viewContext would
        /// allow derived classes to distiguish between designer instances and return a different
        /// physical view (it might be a some IMS element, for example).
        /// Derived classes must handle the case where the viewContext is null.  This will occur when the user
        /// double-clicks on a file as opposed to drilling down to a different view from one of our editors.
        /// Most likely they will just return the default physical view, the empty string.  Note that this means there may be only one physical view
        /// for the default logical view for a file (this would correspond to the ApplicationDesigner, for example).
        /// </summary>
        protected override string MapLogicalView(Guid logicalView, object viewContext)
        {
            return base.MapLogicalView(logicalView, viewContext);
        }
    }
}
