using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Shell;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VSLangProj;

namespace Sawczyn.EFDesigner.EFModel
{
   internal partial class EFModelDocData
   {
      private static DTE _dte;
      private static DTE2 _dte2;
      private IComponentModel _componentModel;
      private IVsPackageInstaller _nugetInstaller;
      private IVsPackageInstallerServices _nugetInstallerServices;
      private IVsPackageUninstaller _nugetUninstaller;
      private IVsOutputWindowPane _outputWindow;

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

      public static EFModelDocData Current { get; private set; }

      /// <summary>
      ///    Validate the model before the file is saved.
      /// </summary>
      protected override bool CanSave(bool allowUserInterface)
      {
         if (allowUserInterface)
            ValidationController?.ClearMessages();

         return base.CanSave(allowUserInterface);
      }

      /// <summary>
      /// Loads the given file.
      /// </summary>
      protected override void Load(string fileName, bool isReload)
      {
         base.Load(fileName, isReload);
         Store.RuleManager.DisableRule(typeof(FixUpDiagram));
         Store.RuleManager.EnableRule(typeof(DiagramFixup));
         Current = this;
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

      internal static void GenerateCode(string filepath = null)
      {
         ProjectItem modelProjectItem = Dte2.Solution.FindProjectItem(filepath ?? Dte2.ActiveDocument.FullName);
         modelProjectItem?.Save();

         string templateFilename = Path.ChangeExtension(filepath ?? Dte2.ActiveDocument.FullName, "tt");

         ProjectItem templateProjectItem = Dte2.Solution.FindProjectItem(templateFilename);
         VSProjectItem templateVsProjectItem = templateProjectItem?.Object as VSProjectItem;

         if (templateVsProjectItem == null)
            Messages.AddError($"Tried to generate code but couldn't find {templateFilename} in the solution.");
         else
         {
            try
            {
               Dte.StatusBar.Text = $"Generating code from {templateFilename}";
               templateVsProjectItem.RunCustomTool();
               Dte.StatusBar.Text = $"Finished generating code from {templateFilename}";
            }
            catch (COMException)
            {
               string message = $"Encountered an error generating code from {templateFilename}. Please transform T4 template manually.";
               Dte.StatusBar.Text = message;
               Messages.AddError(message);
            }
         }
      }

      private static EFVersionDetails GetEFVersionDetails(ModelRoot modelRoot)
      {
         EFVersionDetails versionInfo = new EFVersionDetails
         {
            TargetPackageId = modelRoot.NuGetPackageVersion.PackageId,
            TargetPackageVersion = modelRoot.NuGetPackageVersion.ActualPackageVersion,
            CurrentPackageId = null,
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

      /// <summary>
      ///    Called on both document load and reload.
      /// </summary>
      protected override void OnDocumentLoaded()
      {
         base.OnDocumentLoaded();
         ErrorDisplay.RegisterDisplayHandler(ShowError);
         WarningDisplay.RegisterDisplayHandler(ShowWarning);
         QuestionDisplay.RegisterDisplayHandler(ShowBooleanQuestionBox);

         if (!(RootElement is ModelRoot modelRoot))
            return;

         if (NuGetInstaller == null || NuGetUninstaller == null || NuGetInstallerServices == null)
            ModelRoot.CanLoadNugetPackages = false;

         // set to the project's namespace if no namespace set
         if (string.IsNullOrEmpty(modelRoot.Namespace))
         {
            using (Transaction tx = modelRoot.Store.TransactionManager.BeginTransaction("SetDefaultNamespace"))
            {
               modelRoot.Namespace = ActiveProject.Properties.Item("DefaultNamespace")?.Value as string;
               tx.Commit();
            }
         }

         ReadOnlyCollection<Association> associations = modelRoot.Store.ElementDirectory.FindElements<Association>();

         if (associations.Any())
         {
            using (Transaction tx = modelRoot.Store.TransactionManager.BeginTransaction("StyleConnectors"))
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
         }

         List<GeneralizationConnector> generalizationConnectors = modelRoot.Store
                                                                           .ElementDirectory
                                                                           .FindElements<GeneralizationConnector>()
                                                                           .Where(x => !x.FromShape.IsVisible || !x.ToShape.IsVisible)
                                                                           .ToList();

         List<AssociationConnector> associationConnectors = modelRoot.Store
                                                                     .ElementDirectory
                                                                     .FindElements<AssociationConnector>()
                                                                     .Where(x => !x.FromShape.IsVisible || !x.ToShape.IsVisible)
                                                                     .ToList();

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
         }

         using (Transaction tx = modelRoot.Store.TransactionManager.BeginTransaction("ColorShapeOutlines"))
         {
            foreach (ModelClass modelClass in modelRoot.Store.ElementDirectory.FindElements<ModelClass>())
               PresentationHelper.ColorShapeOutline(modelClass);

            tx.Commit();
         }

         foreach (EFModelDiagram diagram in PresentationViewsSubject.GetPresentation(modelRoot).OfType<EFModelDiagram>())
            diagram.SubscribeCompartmentItemsEvents();

         // Update legacy diagrams to contain appropriate display proxy
         if (!Store.ElementDirectory.AllElements.OfType<EFModelDiagramProxy>().Any())
         {
            using (Transaction tx = Store.TransactionManager.BeginTransaction("Create EFModelDiagramProxy"))
            {
               modelRoot.ModelDiagrams.Add(new EFModelDiagramProxy(Store.DefaultPartition, new PropertyAssignment(EFModelDiagramProxy.NameDomainPropertyId, "Default")));
               tx.Commit();
            }
         }

         SetDocDataDirty(0);
      }

      /// <summary>
      ///    Called before the document is initially loaded with data.
      /// </summary>
      protected override void OnDocumentLoading(EventArgs e)
      {
         base.OnDocumentLoading(e);
         ValidationController?.ClearMessages();
      }

      public override void OpenView(Guid logicalView, object viewContextObj)
      {
         if (viewContextObj is string viewName)
         {
            EFModelDiagram diagram = Store.ElementDirectory.FindElements<EFModelDiagram>().SingleOrDefault(d => d.Name == viewName);
            EFModelDiagramProxy diagramProxy = Store.ElementDirectory.FindElements<EFModelDiagramProxy>().SingleOrDefault(d => d.Name == viewName);

            if (diagram == null || diagramProxy == null)
            {
               using (Transaction transaction = Store.TransactionManager.BeginTransaction("DocData.OpenView", true))
               {
                  if (diagram == null)
                  {
                     // ReSharper disable once UseObjectOrCollectionInitializer
                     diagram = new EFModelDiagram(GetDiagramPartition(), new PropertyAssignment(Diagram.NameDomainPropertyId, viewName));
                     diagram.ModelElement = RootElement;
                  }

                  if (diagramProxy == null)
                  {
                     diagramProxy = new EFModelDiagramProxy(Store, new PropertyAssignment(EFModelDiagramProxy.NameDomainPropertyId, viewName ?? "Default"));
                     ModelRoot modelRoot = (ModelRoot)RootElement;
                     modelRoot.ModelDiagrams.Add(diagramProxy);
                  }

                  transaction.Commit();
               }
            } 

            base.OpenView(logicalView, diagram);
         }
         else
            base.OpenView(logicalView, viewContextObj);
      }


      protected override void OnDocumentSaved(EventArgs e)
      {
         base.OnDocumentSaved(e);

         // Notify the Running Document Table that the subordinate has been saved
         // If this was a SaveAs, then let the subordinate document do this notification itself.
         // Otherwise VS will never ask the subordinate to save itself.
         if (e is DocumentSavedEventArgs savedEventArgs && ServiceProvider != null)
         {
            if (StringComparer.OrdinalIgnoreCase.Compare(savedEventArgs.OldFileName, savedEventArgs.NewFileName) == 0)
            {
               IVsRunningDocumentTable rdt = (IVsRunningDocumentTable)ServiceProvider.GetService(typeof(IVsRunningDocumentTable));

               if (rdt != null && diagramDocumentLockHolder?.SubordinateDocData != null)
                  Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(rdt.NotifyOnAfterSave(diagramDocumentLockHolder.SubordinateDocData.Cookie));
            }
         }

         if (RootElement is ModelRoot modelRoot)
         {
            // if false, don't even check
            if (modelRoot.InstallNuGetPackages != AutomaticAction.False)
               EnsureCorrectNuGetPackages(modelRoot, false);

            if (modelRoot.TransformOnSave)
               GenerateCode(((DocumentSavedEventArgs)e).NewFileName);
         }
      }

      /// <summary>
      ///    Saves the given file.
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

         IList<PresentationElement> diagrams = PresentationViewsSubject.GetPresentation(RootElement);

         if (diagrams.Count > 0 && (!saveAs || diagramDocumentLockHolder == null))
         {
            if (diagrams[0] is EFModelDiagram)
            {
               string diagramFileName = fileName + DiagramExtension;

               try
               {
                  SuspendFileChangeNotification(diagramFileName);

                  EFModelSerializationHelper.Instance.SaveModelAndDiagram(serializationResult, modelRoot, fileName, diagrams.OfType<EFModelDiagram>(), diagramFileName, Encoding, false);
               }
               finally
               {
                  ResumeFileChangeNotification(diagramFileName);
               }
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
         {
            // Save failed.
            throw new InvalidOperationException(EFModelDomainModel.SingletonResourceManager.GetString("CannotSaveDocument"));
         }
      }

      protected override void SaveSubordinateFile(DocData subordinateDocument, string fileName)
      {
         SerializationResult serializationResult = new SerializationResult();
         List<EFModelDiagram> diagrams = PresentationViewsSubject.GetPresentation(RootElement).OfType<EFModelDiagram>().ToList();
         if (diagrams.Any())
         {
            try
            {
               SuspendFileChangeNotification(fileName);
               EFModelSerializationHelper.Instance.SaveDiagrams(serializationResult, diagrams.ToArray(), fileName, Encoding, false);
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

         if (!serializationResult.Failed)
         {
            // Notify the Running Document Table that the subordinate has been saved
            IVsRunningDocumentTable rdt = (IVsRunningDocumentTable)ServiceProvider?.GetService(typeof(IVsRunningDocumentTable));

            if (rdt != null && diagramDocumentLockHolder?.SubordinateDocData != null)
               Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(rdt.NotifyOnAfterSave(diagramDocumentLockHolder.SubordinateDocData.Cookie));
         }
         else
         {
            // Save failed.
            throw new InvalidOperationException(EFModelDomainModel.SingletonResourceManager.GetString("CannotSaveDocument"));
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

      private bool ShowBooleanQuestionBox(string question) => ShowQuestionBox(question) == DialogResult.Yes;

      private void ShowError(string message)
      {
         Messages.AddError(message);
         PackageUtility.ShowMessageBox(ServiceProvider, message, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_CRITICAL);
      }

      // ReSharper disable once UnusedMember.Local
      private void ShowMessage(string message) => Messages.AddMessage(message);

      private DialogResult ShowQuestionBox(string question) => PackageUtility.ShowMessageBox(ServiceProvider, question, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND, OLEMSGICON.OLEMSGICON_QUERY);

      private void ShowWarning(string message) => Messages.AddWarning(message);

      private class EFVersionDetails
      {
         public string TargetPackageId { get; set; }
         public string TargetPackageVersion { get; set; }
         public string CurrentPackageId { get; set; }
         public string CurrentPackageVersion { get; set; }
      }
   }
}
