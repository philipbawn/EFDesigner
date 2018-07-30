using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Shell;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using NuGet.VisualStudio;

using Sawczyn.EFDesigner.EFModel.DslPackage.CustomCode;

using VSLangProj;

namespace Sawczyn.EFDesigner.EFModel
{
   internal partial class EFModelDocData
   {
      private static DTE _dte;
      private static DTE2 _dte2;
      private IComponentModel _componentModel;
      private IVsOutputWindowPane _outputWindow;
      private IVsPackageInstaller _nugetInstaller;
      private IVsPackageUninstaller _nugetUninstaller;
      private IVsPackageInstallerServices _nugetInstallerServices;

      private static DTE Dte => _dte ?? (_dte = Package.GetGlobalService(typeof(DTE)) as DTE);
      private static DTE2 Dte2 => _dte2 ?? (_dte2 = Package.GetGlobalService(typeof(SDTE)) as DTE2);
      private IComponentModel ComponentModel => _componentModel ?? (_componentModel = (IComponentModel)GetService(typeof(SComponentModel)));
      private IVsOutputWindowPane OutputWindow => _outputWindow ?? (_outputWindow = (IVsOutputWindowPane)GetService(typeof(SVsGeneralOutputWindowPane)));
      private IVsPackageInstallerServices NuGetInstallerServices => _nugetInstallerServices ?? (_nugetInstallerServices = ComponentModel?.GetService<IVsPackageInstallerServices>());
      private IVsPackageInstaller NuGetInstaller => _nugetInstaller ?? (_nugetInstaller = ComponentModel.GetService<IVsPackageInstaller>());
      private IVsPackageUninstaller NuGetUninstaller => _nugetUninstaller ?? (_nugetUninstaller = ComponentModel.GetService<IVsPackageUninstaller>());

      private static Project ActiveProject => Dte.ActiveSolutionProjects is Array activeSolutionProjects && activeSolutionProjects.Length > 0
                                                 ? activeSolutionProjects.GetValue(0) as Project
                                                 : null;

      protected override string DiagramExtension => Constants.DiagramxExtension;

      internal static void GenerateCode(string filepath = null)
      {
         string filename = Path.ChangeExtension(filepath ?? Dte2.ActiveDocument.FullName, "tt");

         if (!(Dte2.Solution.FindProjectItem(filename)?.Object is VSProjectItem item))
            Messages.AddError($"Tried to generate code but couldn't find {filename} in the solution.");
         else
         {
            try
            {
               Dte.StatusBar.Text = $"Generating code from {filename}";
               item.RunCustomTool();
               Dte.StatusBar.Text = $"Finished generating code from {filename}";
            }
            catch (COMException)
            {
               string message = $"Encountered an error generating code from {filename}. Please transform T4 template manually.";
               Dte.StatusBar.Text = message;
               Messages.AddError(message);
            }
         }
      }

      protected override void OnDocumentLoaded()
      {
         base.OnDocumentLoaded();
         ErrorDisplay.RegisterDisplayHandler(ShowErrorBox);
         WarningDisplay.RegisterDisplayHandler(ShowWarning);
         QuestionDisplay.RegisterDisplayHandler(ShowBooleanQuestionBox);

         if (!(RootElement is ModelRoot modelRoot))
            return;

         IList<PresentationElement> presentationElementList = PresentationViewsSubject.GetPresentation(modelRoot);

         foreach (EFModelDiagram diagram in presentationElementList.Select(x => x as EFModelDiagram))
            diagram?.SubscribeCompartmentItemsEvents();

         if (NuGetInstaller == null
          || NuGetUninstaller == null
          || NuGetInstallerServices == null)
            ModelRoot.CanLoadNugetPackages = false;

         // set to the project's namespace if no namespace set
         if (string.IsNullOrEmpty(modelRoot.Namespace))
         {
            using (Transaction tx =
                modelRoot.Store.TransactionManager.BeginTransaction("SetDefaultNamespace"))
            {
               modelRoot.Namespace =
                   ActiveProject.Properties.Item("DefaultNamespace")?.Value as string;

               tx.Commit();
            }
         }

         ReadOnlyCollection<Association> associations =
             modelRoot.Store.ElementDirectory.FindElements<Association>();

         if (associations.Any())
         {
            using (Transaction tx =
                modelRoot.Store.TransactionManager.BeginTransaction("StyleConnectors"))
            {
               // style association connectors if needed
               foreach (Association element in associations)
               {
                  AssociationChangeRules.UpdateDisplayForPersistence(element);
                  AssociationChangeRules.UpdateDisplayForCascadeDelete(element);

                  // for older diagrams that didn't calculate this initially
                  AssociationChangeRules.SetEndpointRoles(element);
               }

               tx.Commit();
            }

            ErrorHandler.ThrowOnFailure(SetDocDataDirty(0));
         }

         List<GeneralizationConnector> generalizationConnectors = modelRoot.Store.ElementDirectory.FindElements<GeneralizationConnector>().Where(x => !x.FromShape.IsVisible || !x.ToShape.IsVisible).ToList();
         List<AssociationConnector> associationConnectors = modelRoot.Store.ElementDirectory.FindElements<AssociationConnector>().Where(x => !x.FromShape.IsVisible || !x.ToShape.IsVisible).ToList();

         if (generalizationConnectors.Any() || associationConnectors.Any())
         {
            using (Transaction tx = modelRoot.Store.TransactionManager.BeginTransaction("HideConnectors"))
            {
               // hide any connectors that may have been hidden due to hidden shapes
               foreach (GeneralizationConnector connector in generalizationConnectors)
                  connector.Hide();

               foreach (AssociationConnector connector in associationConnectors)
                  connector.Hide();

               tx.Commit();
            }

            ErrorHandler.ThrowOnFailure(SetDocDataDirty(0));
         }
      }

      // ReSharper disable once UnusedMember.Local
      private void ShowMessageBox(string message)
      {
         Messages.AddMessage(message);
         PackageUtility.ShowMessageBox(ServiceProvider, message, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO);
      }

      // ReSharper disable once UnusedMember.Local
      private void ShowErrorBox(string message)
      {
         Messages.AddError(message);
         PackageUtility.ShowMessageBox(ServiceProvider, message, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_CRITICAL);
      }

      // ReSharper disable once UnusedMember.Local
      private DialogResult ShowQuestionBox(string question)
      {
         return PackageUtility.ShowMessageBox(ServiceProvider, question, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND, OLEMSGICON.OLEMSGICON_QUERY);
      }

      private bool ShowBooleanQuestionBox(string question)
      {
         return ShowQuestionBox(question) == DialogResult.Yes;
      }

      private void ShowWarning(string message)
      {
         Messages.AddWarning(message);
      }

      /// <summary>Called before the document is saved.</summary>
      protected override void OnDocumentSaving(EventArgs e)
      {
         // make sure that, if a model element is highlighted, we set the colors back to where they should be before saving it
         EFModelExplorerToolWindow.ClearHighlight();
         base.OnDocumentSaving(e);
      }

      protected override void OnDocumentSaved(EventArgs e)
      {
         base.OnDocumentSaved(e);

         if (RootElement is ModelRoot modelRoot)
         {
            base.OnDocumentSaved(e);

            // if false, don't even check
            if (modelRoot.InstallNuGetPackages != AutomaticAction.False)
               EnsureCorrectNuGetPackages(modelRoot, false);

            if (modelRoot.TransformOnSave)
               GenerateCode(((DocumentSavedEventArgs)e).NewFileName);
         }
      }

      /// <summary>
      /// Saves the given file.
      /// </summary>
      protected override void Save(string fileName)
      {
         SerializationResult serializationResult = new SerializationResult();
         ModelRoot modelRoot = (ModelRoot)RootElement;

         // Only save the diagrams if
         // a) There are any to save
         // b) This is NOT a SaveAs operation.  SaveAs should allow the subordinate document to control the save of its data as it is writing a new file.
         //    Except DO save the diagram on SaveAs if there isn't currently a diagram as there won't be a subordinate document yet to save it.

         bool saveAs = StringComparer.OrdinalIgnoreCase.Compare(fileName, FileName) != 0;

         IList<PresentationElement> presentationElementList = PresentationViewsSubject.GetPresentation(modelRoot);
         Diagram[] diagramList = presentationElementList.OfType<Diagram>().ToArray();

         if (diagramList.Length > 0 && (!saveAs || diagramDocumentLockHolder == null))
         {
            string diagramxFileName = fileName + Constants.DiagramxExtension;
            try
            {
               SuspendFileChangeNotification(diagramxFileName);
               EFModelSerializationHelper.Instance.SaveModelAndDiagrams(serializationResult, RootElement, fileName, diagramList, diagramxFileName, Encoding, false);
            }
            finally
            {
               ResumeFileChangeNotification(diagramxFileName);
            }
         }
         else
         {
            EFModelSerializationHelper.Instance.SaveModel(serializationResult, modelRoot, fileName, Encoding, false);
         }

         // Report serialization messages.
         SuspendErrorListRefresh();
         try
         {
            foreach (SerializationMessage serializationMessage in serializationResult)
            {
               AddErrorListItem(new SerializationErrorListItem(ServiceProvider, serializationMessage));
            }
         }
         finally
         {
            ResumeErrorListRefresh();
         }

         if (serializationResult.Failed)
            throw new InvalidOperationException(EFModelDomainModel.SingletonResourceManager.GetString("CannotSaveDocument"));
      }

      /// <summary>Called to open a particular view on this DocData.</summary>
      /// <param name="viewContext">Object that gives further context about the view to open.  The editor factory that
      /// supports the given logical view must be able to interpret this object.</param>
      /// <param name="logicalView">Guid that specifies the view to open.  Must match the value specified in the
      /// registry for the editor that supports this view.</param>
      public override void OpenView(Guid logicalView, object viewContext)
      {
         if (viewContext is string context)
         {
            Diagram diagram = Store.ElementDirectory.FindElements<Diagram>().SingleOrDefault(d => d.Name == context);

            if (diagram == null)
            {
               using (Transaction transaction = Store.TransactionManager.BeginTransaction("DocData.OpenView", true))
               {
                  diagram = new EFModelDiagram(Store, new PropertyAssignment(Diagram.NameDomainPropertyId, context)) { ModelElement = RootElement };
                  transaction.Commit();
               }
            }

            base.OpenView(logicalView, diagram);
         }
         else
            base.OpenView(logicalView, viewContext);
      }

      /// <summary>
      /// Save the given document that is subordinate to this document.
      /// </summary>
      /// <param name="subordinateDocument"></param>
      /// <param name="fileName"></param>
      protected override void SaveSubordinateFile(DocData subordinateDocument, string fileName)
      {
         // In this case, the only subordinate is the diagram.
         SerializationResult serializationResult = new SerializationResult();
         Diagram[] diagrams = PresentationViewsSubject.GetPresentation(RootElement).OfType<Diagram>().ToArray();

         if (diagrams.Length > 0)
         {
            try
            {
               SuspendFileChangeNotification(fileName);
               EFModelSerializationHelper.Instance.SaveDiagrams(serializationResult, diagrams, fileName, Encoding, false);
            }
            finally
            {
               ResumeFileChangeNotification(fileName);
            }
         }

         // Report serialization messages.
         SuspendErrorListRefresh();
         try
         {
            foreach (SerializationMessage serializationMessage in serializationResult)
               AddErrorListItem(new SerializationErrorListItem(ServiceProvider, serializationMessage));
         }
         finally
         {
            ResumeErrorListRefresh();
         }

         if (serializationResult.Failed)
            throw new InvalidOperationException(EFModelDomainModel.SingletonResourceManager.GetString("CannotSaveDocument"));

         NotifySubordinateDocumentSaved(subordinateDocument.FileName, fileName);
      }

      protected override void Load(string fileName, bool isReload)
      {
         SerializationResult serializationResult = new SerializationResult();
         ISchemaResolver schemaResolver = new ModelingSchemaResolver(ServiceProvider);
        
         //clear the current root element
         SetRootElement(null);
        
         // Enable diagram fixup rules in our store, because we will load diagram data.
         EFModelDomainModel.EnableDiagramRules(Store);
         string diagramFileName = fileName + DiagramExtension;

         ModelRoot modelRoot = EFModelSerializationHelper.Instance.LoadModelAndDiagrams(serializationResult, GetModelPartition().Store, fileName, diagramFileName, schemaResolver, ValidationController, SerializerLocator);

         // Report serialization messages.
         SuspendErrorListRefresh();
         try
         {
            foreach (SerializationMessage serializationMessage in serializationResult)
            {
               AddErrorListItem(new SerializationErrorListItem(ServiceProvider, serializationMessage));
            }
         }
         finally
         {
            ResumeErrorListRefresh();
         }

         if (serializationResult.Failed)
         {
            // Load failed, can't open the file.
            throw new InvalidOperationException(EFModelDomainModel.SingletonResourceManager.GetString("CannotOpenDocument"));
         }
         else
         {
            SetRootElement(modelRoot);

            // Attempt to set the encoding
            if (serializationResult.Encoding != null)
            {
               ModelingDocStore.SetEncoding(serializationResult.Encoding);
               ErrorHandler.ThrowOnFailure(SetDocDataDirty(0)); // Setting the encoding will mark the document as dirty, so clear the dirty flag.
            }

            if (Hierarchy != null && File.Exists(diagramFileName))
            {
               // Add a lock to the subordinate diagram file.
               if (diagramDocumentLockHolder == null)
               {
                  uint itemId = SubordinateFileHelper.GetChildProjectItemId(Hierarchy, ItemId, DiagramExtension);
                  if (itemId != VSConstants.VSITEMID_NIL)
                  {
                     diagramDocumentLockHolder = SubordinateFileHelper.LockSubordinateDocument(ServiceProvider, this, diagramFileName, itemId);
                     if (diagramDocumentLockHolder == null)
                     {
                        throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                       EFModelDomainModel.SingletonResourceManager.GetString("CannotCloseExistingDiagramDocument"),
                                       diagramFileName));
                     }
                  }
               }
            }
         }
      }

      private class EFVersionDetails
      {
         public string TargetPackageId { get; set; }
         public string TargetPackageVersion { get; set; }
         public string CurrentPackageId { get; set; }
         public string CurrentPackageVersion { get; set; }
      }

      public void EnsureCorrectNuGetPackages(ModelRoot modelRoot, bool force = true)
      {
         EFVersionDetails versionInfo = GetEFVersionDetails(modelRoot);

         if (force || ShouldLoadPackages(modelRoot, versionInfo))
         {
            // first unload what's there, if anything
            if (versionInfo.CurrentPackageId != null)
            {
               // only remove dependencies if we're switching EF types
               Dte.StatusBar.Text = $"Uninstalling {versionInfo.CurrentPackageId} v{versionInfo.CurrentPackageVersion}";

               try
               {
                  NuGetUninstaller.UninstallPackage(ActiveProject, versionInfo.CurrentPackageId, true);
                  Dte.StatusBar.Text = $"Finished uninstalling {versionInfo.CurrentPackageId} v{versionInfo.CurrentPackageVersion}";
               }
               catch (Exception ex)
               {
                  string message = $"Error uninstalling {versionInfo.CurrentPackageId} v{versionInfo.CurrentPackageVersion}";
                  Dte.StatusBar.Text = message;
                  OutputWindow.OutputString(message + "\n");
                  OutputWindow.OutputString(ex.Message + "\n");
                  OutputWindow.Activate();
                  return;
               }
            }

            Dte.StatusBar.Text = $"Installing {versionInfo.TargetPackageId} v{versionInfo.TargetPackageVersion}";

            try
            {
               NuGetInstaller.InstallPackage(null, ActiveProject, versionInfo.TargetPackageId, versionInfo.TargetPackageVersion, false);
               Dte.StatusBar.Text = $"Finished installing {versionInfo.TargetPackageId} v{versionInfo.TargetPackageVersion}";
            }
            catch (Exception ex)
            {
               string message = $"Error installing {versionInfo.TargetPackageId} v{versionInfo.TargetPackageVersion}";
               Dte.StatusBar.Text = message;
               OutputWindow.OutputString(message + "\n");
               OutputWindow.OutputString(ex.Message + "\n");
               OutputWindow.Activate();
            }
         }
         else if (versionInfo.CurrentPackageId == versionInfo.TargetPackageId && versionInfo.CurrentPackageVersion == versionInfo.TargetPackageVersion)
         {
            string message = $"{versionInfo.TargetPackageId} v{versionInfo.TargetPackageVersion} already installed";
            Dte.StatusBar.Text = message;
         }
      }

      private bool ShouldLoadPackages(ModelRoot modelRoot, EFVersionDetails versionInfo)
      {
         Version currentPackageVersion = new Version(versionInfo.CurrentPackageVersion);
         Version targetPackageVersion = new Version(versionInfo.TargetPackageVersion);

         return ModelRoot.CanLoadNugetPackages &&
                (versionInfo.CurrentPackageId != versionInfo.TargetPackageId || currentPackageVersion != targetPackageVersion) &&
                (modelRoot.InstallNuGetPackages == AutomaticAction.True ||
                 ShowQuestionBox($"Referenced libraries don't match Entity Framework {modelRoot.NuGetPackageVersion.ActualPackageVersion}. Fix that now?") == DialogResult.Yes);
      }

      private static EFVersionDetails GetEFVersionDetails(ModelRoot modelRoot)
      {

         EFVersionDetails versionInfo = new EFVersionDetails
         {
            TargetPackageId = modelRoot.NuGetPackageVersion.PackageId
                                         ,
            TargetPackageVersion = modelRoot.NuGetPackageVersion.ActualPackageVersion
                                         ,
            CurrentPackageId = null
                                         ,
            CurrentPackageVersion = null
         };

         References references = ((VSProject)ActiveProject.Object).References;

         foreach (Reference reference in references)
         {
            if (string.Compare(reference.Name, NuGetHelper.PACKAGEID_EF6, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(reference.Name, NuGetHelper.PACKAGEID_EFCORE, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
               versionInfo.CurrentPackageId = reference.Name;
               versionInfo.CurrentPackageVersion = reference.Version;

               break;
            }
         }

         return versionInfo;
      }
   }
}
