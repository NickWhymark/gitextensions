﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GitCommands;
using GitUI.Blame;
using GitUI.Plugin;
using GitUI.Properties;
using GitUI.RepoHosting;
using GitUI.Tag;
using GitUIPluginInterfaces;
using GitUIPluginInterfaces.RepositoryHosts;
using PatchApply;
using Settings = GitCommands.Settings;
using Gravatar;

namespace GitUI
{
    public sealed class GitUICommands : IGitUICommands
    {
        private static GitUICommands instance;

        public static GitUICommands Instance
        {
            [DebuggerStepThrough]
            get { return instance ?? (instance = new GitUICommands()); }
        }

        #region IGitUICommands Members

        public event GitUIEventHandler PreBrowse;
        public event GitUIEventHandler PostBrowse;

        public event GitUIEventHandler PreDeleteBranch;
        public event GitUIEventHandler PostDeleteBranch;

        public event GitUIEventHandler PreCheckoutRevision;
        public event GitUIPostActionEventHandler PostCheckoutRevision;

        public event GitUIEventHandler PreCheckoutBranch;
        public event GitUIPostActionEventHandler PostCheckoutBranch;

        public event GitUIEventHandler PreFileHistory;
        public event GitUIEventHandler PostFileHistory;

        public event GitUIEventHandler PreCompareRevisions;
        public event GitUIEventHandler PostCompareRevisions;

        public event GitUIEventHandler PreAddFiles;
        public event GitUIEventHandler PostAddFiles;

        public event GitUIEventHandler PreCreateBranch;
        public event GitUIEventHandler PostCreateBranch;

        public event GitUIEventHandler PreClone;
        public event GitUIEventHandler PostClone;

        public event GitUIEventHandler PreSvnClone;
        public event GitUIEventHandler PostSvnClone;

        public event GitUIEventHandler PreCommit;
        public event GitUIEventHandler PostCommit;

        public event GitUIEventHandler PreSvnDcommit;
        public event GitUIEventHandler PostSvnDcommit;

        public event GitUIEventHandler PreSvnRebase;
        public event GitUIEventHandler PostSvnRebase;

        public event GitUIEventHandler PreSvnFetch;
        public event GitUIEventHandler PostSvnFetch;

        public event GitUIEventHandler PreInitialize;
        public event GitUIEventHandler PostInitialize;

        public event GitUIEventHandler PrePush;
        public event GitUIEventHandler PostPush;

        public event GitUIEventHandler PrePull;
        public event GitUIEventHandler PostPull;

        public event GitUIEventHandler PreViewPatch;
        public event GitUIEventHandler PostViewPatch;

        public event GitUIEventHandler PreApplyPatch;
        public event GitUIEventHandler PostApplyPatch;

        public event GitUIEventHandler PreFormatPatch;
        public event GitUIEventHandler PostFormatPatch;

        public event GitUIEventHandler PreStash;
        public event GitUIEventHandler PostStash;

        public event GitUIEventHandler PreResolveConflicts;
        public event GitUIEventHandler PostResolveConflicts;

        public event GitUIEventHandler PreCherryPick;
        public event GitUIEventHandler PostCherryPick;

        public event GitUIEventHandler PreMergeBranch;
        public event GitUIEventHandler PostMergeBranch;

        public event GitUIEventHandler PreCreateTag;
        public event GitUIEventHandler PostCreateTag;

        public event GitUIEventHandler PreDeleteTag;
        public event GitUIEventHandler PostDeleteTag;

        public event GitUIEventHandler PreEditGitIgnore;
        public event GitUIEventHandler PostEditGitIgnore;

        public event GitUIEventHandler PreSettings;
        public event GitUIEventHandler PostSettings;

        public event GitUIEventHandler PreArchive;
        public event GitUIEventHandler PostArchive;

        public event GitUIEventHandler PreMailMap;
        public event GitUIEventHandler PostMailMap;

        public event GitUIEventHandler PreVerifyDatabase;
        public event GitUIEventHandler PostVerifyDatabase;

        public event GitUIEventHandler PreRemotes;
        public event GitUIEventHandler PostRemotes;

        public event GitUIEventHandler PreRebase;
        public event GitUIEventHandler PostRebase;

        public event GitUIEventHandler PreRename;
        public event GitUIEventHandler PostRename;

        public event GitUIEventHandler PreSubmodulesEdit;
        public event GitUIEventHandler PostSubmodulesEdit;

        public event GitUIEventHandler PreUpdateSubmodules;
        public event GitUIEventHandler PostUpdateSubmodules;

        public event GitUIEventHandler PreSyncSubmodules;
        public event GitUIEventHandler PostSyncSubmodules;

        public event GitUIEventHandler PreBlame;
        public event GitUIEventHandler PostBlame;

        public event GitUIEventHandler PreEditGitAttributes;
        public event GitUIEventHandler PostEditGitAttributes;

        public event GitUIEventHandler PreBrowseInitialize;
        public event GitUIEventHandler PostBrowseInitialize;
        public event GitUIEventHandler BrowseInitialize;

        #endregion

        public string GitCommand(string arguments)
        {
            return Module.RunGitCmd(arguments);
        }

        public string CommandLineCommand(string cmd, string arguments)
        {
            return Module.RunCmd(cmd, arguments);
        }

        private bool RequiresValidWorkingDir(object owner)
        {
            if (!Module.ValidWorkingDir())
            {
                MessageBoxes.NotValidGitDirectory(owner as IWin32Window);
                return false;
            }

            return true;
        }

        private bool RequiredValidGitSvnWorikingDir(object owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!GitSvnCommandHelpers.ValidSvnWorkingDir())
            {
                MessageBoxes.NotValidGitSVNDirectory(owner as IWin32Window);
                return false;
            }

            if (!GitSvnCommandHelpers.CheckRefsRemoteSvn())
            {
                MessageBoxes.UnableGetSVNInformation(owner as IWin32Window);
                return false;
            }

            return true;
        }

        public void CacheAvatar(string email)
        {
            FallBackService gravatarFallBack = FallBackService.Identicon;
            try
            {
                gravatarFallBack =
                    (FallBackService)Enum.Parse(typeof(FallBackService), Settings.GravatarFallbackService);
            }
            catch
            {
                Settings.GravatarFallbackService = gravatarFallBack.ToString();
            }
            GravatarService.CacheImage(email + ".png", email, Settings.AuthorImageSize,
                gravatarFallBack);
        }

        public bool StartBatchFileProcessDialog(object owner, string batchFile)
        {
            string tempFileName = Path.ChangeExtension(Path.GetTempFileName(), ".cmd");
            using (var writer = new StreamWriter(tempFileName))
            {
                writer.WriteLine("@prompt $G");
                writer.Write(batchFile);
            }
            FormProcess.ShowDialog(owner as IWin32Window, "cmd.exe", "/C \"" + tempFileName + "\"");
            File.Delete(tempFileName);
            return true;
        }

        public bool StartBatchFileProcessDialog(string batchFile)
        {
            return StartBatchFileProcessDialog(null, batchFile);
        }

        public bool StartCommandLineProcessDialog(GitCommand cmd, IWin32Window parentForm)
        {
            if (cmd.AccessesRemote())
                return FormRemoteProcess.ShowDialog(parentForm, cmd.ToLine());
            else
                return FormProcess.ShowDialog(parentForm, cmd.ToLine());
        }

        public bool StartCommandLineProcessDialog(object owner, string command, string arguments)
        {
            FormProcess.ShowDialog(owner as IWin32Window, command, arguments);
            return true;
        }

        public bool StartCommandLineProcessDialog(string command, string arguments)
        {
            return StartCommandLineProcessDialog(null, command, arguments);
        }

        public bool StartGitCommandProcessDialog(IWin32Window owner, string arguments)
        {
            FormProcess.ShowDialog(owner, arguments);
            return true;
        }

        public bool StartGitCommandProcessDialog(string arguments)
        {
            return StartGitCommandProcessDialog(null, arguments);
        }

        public bool StartBrowseDialog()
        {
            return StartBrowseDialog("");
        }

        public bool StartDeleteBranchDialog(IWin32Window owner, string branch)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreDeleteBranch))
                return false;

            using (var form = new FormDeleteBranch(branch))
                form.ShowDialog(owner);

            InvokeEvent(owner, PostDeleteBranch);

            return true;
        }

        public bool StartDeleteBranchDialog(string branch)
        {
            return StartDeleteBranchDialog(null, branch);
        }

        public bool StartCheckoutRevisionDialog(IWin32Window owner)
        {
            return DoAction(owner, true, PreCheckoutRevision, PostCheckoutRevision, () =>
                {
                    using (var form = new FormCheckout())
                        form.ShowDialog(owner);
                    return true;
                }
            );
        }

        public bool StartCheckoutRevisionDialog()
        {
            return StartCheckoutRevisionDialog(null);
        }

        public void Stash(IWin32Window owner)
        {
            var arguments = GitCommandHelpers.StashSaveCmd(Settings.IncludeUntrackedFilesInAutoStash);
            FormProcess.ShowDialog(owner, arguments);
        }

        public bool StartCheckoutBranchDialog(IWin32Window owner, string branch, bool remote, string containRevison)
        {
            return DoAction(owner, true, PreCheckoutBranch, PostCheckoutBranch, () =>
                {
                    using (var form = new FormCheckoutBranch(branch, remote, containRevison))
                        return form.DoDefaultActionOrShow(owner) != DialogResult.Cancel;                 
                }
            );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requiresValidWorkingDir">If action requires valid working directory</param>
        /// <param name="owner">Owner window</param>
        /// <param name="preEvent">Event invoked before performing action</param>
        /// <param name="postEvent">Event invoked after performing action</param>
        /// <param name="action">Action to do</param>
        /// <returns>true if action was done, false otherwise</returns>
        public bool DoAction(IWin32Window owner, bool requiresValidWorkingDir, GitUIEventHandler preEvent, GitUIPostActionEventHandler postEvent, Func<bool> action)
        {
            if (requiresValidWorkingDir && !RequiresValidWorkingDir(owner))
                return false;
            
            if (!InvokeEvent(owner, preEvent))
                return false;

            bool actionDone = action();

            InvokePostEvent(owner, actionDone, postEvent);

            return actionDone;
        }


        public bool StartCheckoutBranchDialog(IWin32Window owner, string branch, bool remote)
        {
            return StartCheckoutBranchDialog(null, branch, remote, null);
        }

        public bool StartCheckoutBranchDialog(string branch, bool remote)
        {
            return StartCheckoutBranchDialog(null, branch, remote, null);
        }

        public bool StartCheckoutBranchDialog(string containRevison)
        {
            return StartCheckoutBranchDialog(null, "", false, containRevison);
        }

        public bool StartCheckoutBranchDialog(IWin32Window owner)
        {
            return StartCheckoutBranchDialog(owner, "", false, null);
        }

        public bool StartCheckoutBranchDialog()
        {
            return StartCheckoutBranchDialog(null, "", false, null);
        }

        public bool StartCheckoutRemoteBranchDialog(IWin32Window owner, string branch)
        {
            return StartCheckoutBranchDialog(owner, branch, true);
        }

        public bool StartCompareRevisionsDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreCompareRevisions))
                return false;

            using (var form = new FormDiff())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostCompareRevisions);

            return false;
        }

        public bool StartCompareRevisionsDialog()
        {
            return StartCompareRevisionsDialog(null);
        }

        public bool StartAddFilesDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreAddFiles))
                return false;

            using (var form = new FormAddFiles())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostAddFiles);

            return false;
        }

        public bool StartAddFilesDialog()
        {
            return StartAddFilesDialog(null);
        }

        public bool StartCreateBranchDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreCreateBranch))
                return false;

            using (var form = new FormBranch())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostCreateBranch);

            return true;
        }

        public bool StartCreateBranchDialog()
        {
            return StartCreateBranchDialog(null);
        }

        public bool StartCloneDialog(IWin32Window owner, string url, bool openedFromProtocolHandler)
        {
            if (!InvokeEvent(owner, PreClone))
                return false;

            using (var form = new FormClone(url, openedFromProtocolHandler))
                form.ShowDialog(owner);

            InvokeEvent(owner, PostClone);

            return true;
        }

        public bool StartCloneDialog(IWin32Window owner, string url)
        {
            return StartCloneDialog(owner, url, false);
        }

        public bool StartCloneDialog(IWin32Window owner)
        {
            return StartCloneDialog(owner, null);
        }

        public bool StartCloneDialog(string url)
        {
            return StartCloneDialog(null, url);
        }

        public bool StartCloneDialog()
        {
            return StartCloneDialog(null, null);
        }

        public bool StartSvnCloneDialog(IWin32Window owner)
        {
            if (!InvokeEvent(owner, PreSvnClone))
                return false;

            using (var form = new FormSvnClone())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostSvnClone);

            return true;
        }

        public bool StartSvnCloneDialog()
        {
            return StartSvnCloneDialog(null);
        }

        public bool StartCommitDialog(IWin32Window owner, bool showWhenNoChanges)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreCommit))
                return true;

            using (var form = new FormCommit())
            {
                if (showWhenNoChanges)
                    form.ShowDialogWhenChanges(owner);
                else
                    form.ShowDialog(owner);

                InvokeEvent(owner, PostCommit);

                if (!form.NeedRefresh)
                    return false;
            }

            return true;
        }

        public bool StartCommitDialog(IWin32Window owner)
        {
            return StartCommitDialog(owner, false);
        }

        public bool StartCommitDialog(bool showWhenNoChanges)
        {
            return StartCommitDialog(null, showWhenNoChanges);
        }

        public bool StartCommitDialog()
        {
            return StartCommitDialog(null, false);
        }

        public bool StartSvnDcommitDialog(IWin32Window owner)
        {
            if (!RequiredValidGitSvnWorikingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreSvnDcommit))
                return true;

            FormProcess.ShowDialog(owner, Settings.GitCommand, GitSvnCommandHelpers.DcommitCmd());

            InvokeEvent(owner, PostSvnDcommit);

            return true;
        }

        public bool StartSvnDcommitDialog()
        {
            return StartSvnDcommitDialog(null);
        }

        public bool StartSvnRebaseDialog(IWin32Window owner)
        {
            if (!RequiredValidGitSvnWorikingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreSvnRebase))
                return true;

            FormProcess.ShowDialog(owner, Settings.GitCommand, GitSvnCommandHelpers.RebaseCmd());

            InvokeEvent(owner, PostSvnRebase);

            return true;
        }

        public bool StartSvnRebaseDialog()
        {
            return StartSvnRebaseDialog(null);
        }

        public bool StartSvnFetchDialog(IWin32Window owner)
        {
            if (!RequiredValidGitSvnWorikingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreSvnFetch))
                return true;

            FormProcess.ShowDialog(owner, Settings.GitCommand, GitSvnCommandHelpers.FetchCmd());

            InvokeEvent(owner, PostSvnFetch);

            return true;
        }

        public bool StartSvnFetchDialog()
        {
            return StartSvnFetchDialog(null);
        }

        public bool StartInitializeDialog(IWin32Window owner)
        {
            if (!InvokeEvent(owner, PreInitialize))
                return true;

            using (var frm = new FormInit()) frm.ShowDialog(owner);

            InvokeEvent(owner, PostInitialize);

            return true;
        }

        public bool StartInitializeDialog()
        {
            return StartInitializeDialog((IWin32Window)null);
        }

        public bool StartInitializeDialog(IWin32Window owner, string dir)
        {
            if (!InvokeEvent(owner, PreInitialize))
                return true;

            using (var frm = new FormInit(dir)) frm.ShowDialog(owner);

            InvokeEvent(owner, PostInitialize);

            return true;
        }

        public bool StartInitializeDialog(string dir)
        {
            return StartInitializeDialog(null, dir);
        }

        public bool StartPushDialog()
        {
            return StartPushDialog(false);
        }

        /// <summary>
        /// Starts pull dialog
        /// </summary>
        /// <param name="owner">An implementation of IWin32Window that will own the modal dialog box.</param>
        /// <param name="pullOnShow"></param>
        /// <param name="pullCompleted">true if pull completed with no errors</param>
        /// <returns>if revision grid should be refreshed</returns>
        public bool StartPullDialog(IWin32Window owner, bool pullOnShow, out bool pullCompleted, ConfigureFormPull configProc)
        {
            pullCompleted = false;

            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PrePull))
                return true;

            using (FormPull formPull = new FormPull())
            {
                if (configProc != null)
                    configProc(formPull);

                DialogResult dlgResult;
                if (pullOnShow)
                    dlgResult = formPull.PullAndShowDialogWhenFailed(owner);
                else
                    dlgResult = formPull.ShowDialog(owner);

                if (dlgResult == DialogResult.OK)
                {
                    InvokeEvent(owner, PostPull);
                    pullCompleted = !formPull.ErrorOccurred;
                }
            }

            return true;//maybe InvokeEvent should have 'needRefresh' out parameter?
        }

        public bool StartPullDialog(IWin32Window owner, bool pullOnShow, out bool pullCompleted)
        {
            return StartPullDialog(owner, pullOnShow, out pullCompleted, null);
        }

        public bool StartPullDialog(IWin32Window owner, bool pullOnShow)
        {
            bool errorOccurred;
            return StartPullDialog(owner, pullOnShow, out errorOccurred, null);
        }

        public bool StartPullDialog(bool pullOnShow, out bool pullCompleted)
        {
            return StartPullDialog(null, pullOnShow, out pullCompleted, null);
        }

        public bool StartPullDialog(bool pullOnShow)
        {
            bool errorOccurred;
            return StartPullDialog(pullOnShow, out errorOccurred);
        }

        public bool StartPullDialog(IWin32Window owner)
        {
            bool errorOccurred;
            return StartPullDialog(owner, false, out errorOccurred, null);
        }

        public bool StartPullDialog()
        {
            return StartPullDialog(false);
        }

        public bool StartViewPatchDialog(IWin32Window owner)
        {
            if (!InvokeEvent(owner, PreViewPatch))
                return true;

            using (var applyPatch = new ViewPatch())
                applyPatch.ShowDialog(owner);

            InvokeEvent(owner, PostViewPatch);

            return true;
        }

        public bool StartViewPatchDialog()
        {
            return StartViewPatchDialog(null);
        }

        public bool StartFormatPatchDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreFormatPatch))
                return true;

            using (var form = new FormFormatPatch())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostFormatPatch);

            return false;
        }

        public bool StartFormatPatchDialog()
        {
            return StartFormatPatchDialog(null);
        }

        public bool StartStashDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreStash))
                return true;

            using (var form = new FormStash())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostStash);

            return true;
        }

        public bool StartStashDialog()
        {
            return StartStashDialog(null);
        }

        public bool StartResolveConflictsDialog(IWin32Window owner, bool offerCommit)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreResolveConflicts))
                return true;

            using (var form = new FormResolveConflicts(offerCommit))
                form.ShowDialog(owner);

            InvokeEvent(owner, PostResolveConflicts);

            return true;
        }

        public bool StartResolveConflictsDialog(IWin32Window owner)
        {
            return StartResolveConflictsDialog(owner, true);
        }

        public bool StartResolveConflictsDialog(bool offerCommit)
        {
            return StartResolveConflictsDialog(null, offerCommit);
        }

        public bool StartResolveConflictsDialog()
        {
            return StartResolveConflictsDialog(null, true);
        }

        public bool StartCherryPickDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreCherryPick))
                return true;

            using (var form = new FormCherryPick())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostCherryPick);

            return true;
        }

        public bool StartCherryPickDialog()
        {
            return StartCherryPickDialog(null);
        }

        public bool StartMergeBranchDialog(IWin32Window owner, string branch)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreMergeBranch))
                return true;

            using (var form = new FormMergeBranch(branch))
                form.ShowDialog(owner);

            InvokeEvent(owner, PostMergeBranch);

            return true;
        }

        public bool StartMergeBranchDialog(string branch)
        {
            return StartMergeBranchDialog(null, branch);
        }

        public bool StartCreateTagDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreCreateTag))
                return true;

            using (var form = new FormTag())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostCreateTag);

            return true;
        }

        public bool StartCreateTagDialog()
        {
            return StartCreateTagDialog(null);
        }

        public bool StartDeleteTagDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreDeleteTag))
                return true;

            using (var form = new FormDeleteTag())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostDeleteTag);

            return true;
        }

        public bool StartDeleteTagDialog()
        {
            return StartDeleteTagDialog(null);
        }

        public bool StartEditGitIgnoreDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreEditGitIgnore))
                return true;

            using (var form = new FormGitIgnore())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostEditGitIgnore);

            return false;
        }

        public bool StartEditGitIgnoreDialog()
        {
            return StartEditGitIgnoreDialog(null);
        }

        public bool StartAddToGitIgnoreDialog(IWin32Window owner, string filePattern)
        {
            if (!RequiresValidWorkingDir(this))
                return false;

            try
            {
                if (!InvokeEvent(owner, PreEditGitIgnore))
                    return false;

                using (var frm = new FormAddToGitIgnore(filePattern))
                    frm.ShowDialog(owner);
            }
            finally
            {
                InvokeEvent(owner, PostEditGitIgnore);
            }

            return false;
        }

        public bool StartSettingsDialog(IWin32Window owner)
        {
            if (!InvokeEvent(owner, PreSettings))
                return true;

            using (var form = new FormSettings())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostSettings);

            return true;
        }

        public bool StartSettingsDialog()
        {
            return StartSettingsDialog(null);
        }

        public bool StartArchiveDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreArchive))
                return true;

            using (var form = new FormArchive())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostArchive);

            return false;
        }

        public bool StartArchiveDialog()
        {
            return StartArchiveDialog(null);
        }

        public bool StartMailMapDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreMailMap))
                return true;

            using (var form = new FormMailMap())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostMailMap);

            return true;
        }

        public bool StartMailMapDialog()
        {
            return StartMailMapDialog(null);
        }

        public bool StartVerifyDatabaseDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreVerifyDatabase))
                return true;

            using (var form = new FormVerify())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostVerifyDatabase);

            return true;
        }

        public bool StartVerifyDatabaseDialog()
        {
            return StartVerifyDatabaseDialog(null);
        }

        public bool StartRemotesDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreRemotes))
                return true;

            using (var form = new FormRemotes())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostRemotes);

            return true;
        }

        public bool StartRemotesDialog()
        {
            return StartRemotesDialog(null);
        }

        public bool StartRebaseDialog(IWin32Window owner, string branch)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreRebase))
                return true;

            using (var form = new FormRebase(branch))
                form.ShowDialog(owner);

            InvokeEvent(owner, PostRebase);

            return true;
        }

        public bool StartRenameDialog(string branch)
        {
            return StartRenameDialog(null, branch);
        }

        public bool StartRenameDialog(IWin32Window owner, string branch)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreRename))
                return true;

            using (var form = new FormRenameBranch(branch))
            {

                if (form.ShowDialog(owner) != DialogResult.OK)
                    return false;
            }

            InvokeEvent(owner, PostRename);

            return true;
        }

        public bool StartRebaseDialog(string branch)
        {
            return StartRebaseDialog(null, branch);
        }

        public bool StartSubmodulesDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreSubmodulesEdit))
                return true;

            using (var form = new FormSubmodules())
                form.ShowDialog(owner);

            InvokeEvent(owner, PostSubmodulesEdit);

            return true;
        }

        public bool StartSubmodulesDialog()
        {
            return StartSubmodulesDialog(null);
        }

        public bool StartUpdateSubmodulesDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreUpdateSubmodules))
                return true;

            FormProcess.ShowDialog(owner, GitCommandHelpers.SubmoduleUpdateCmd(""));

            InvokeEvent(owner, PostUpdateSubmodules);

            return true;
        }

        public bool StartUpdateSubmodulesDialog()
        {
            return StartUpdateSubmodulesDialog(null);
        }

        public bool StartSyncSubmodulesDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreSyncSubmodules))
                return true;

            FormProcess.ShowDialog(owner, GitCommandHelpers.SubmoduleSyncCmd(""));

            InvokeEvent(owner, PostSyncSubmodules);

            return true;
        }

        public bool StartSyncSubmodulesDialog()
        {
            return StartSyncSubmodulesDialog(null);
        }

        public bool StartPluginSettingsDialog(IWin32Window owner)
        {
            using (var frm = new FormPluginSettings()) frm.ShowDialog(owner);
            return true;
        }

        public bool StartPluginSettingsDialog()
        {
            return StartPluginSettingsDialog(null);
        }

        public bool StartBrowseDialog(IWin32Window owner, string filter)
        {
            if (!InvokeEvent(owner, PreBrowse))
                return false;

            using (var form = new FormBrowse(filter))
                form.ShowDialog(owner);

            InvokeEvent(owner, PostBrowse);

            return true;
        }

        public bool StartBrowseDialog(string filter)
        {
            return StartBrowseDialog(null, filter);
        }

        public bool StartFileHistoryDialog(IWin32Window owner, string fileName, GitRevision revision, bool filterByRevision, bool showBlame)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreFileHistory))
                return false;

            using (var form = new FormFileHistory(fileName, revision, filterByRevision))
            {
                if (showBlame)
                    form.SelectBlameTab();
                form.ShowDialog(owner);
            }

            InvokeEvent(owner, PostFileHistory);

            return false;
        }

        public bool StartFileHistoryDialog(IWin32Window owner, string fileName, GitRevision revision, bool filterByRevision)
        {
            return StartFileHistoryDialog(owner, fileName, revision, filterByRevision, false);
        }

        public bool StartFileHistoryDialog(IWin32Window owner, string fileName, GitRevision revision)
        {
            return StartFileHistoryDialog(owner, fileName, revision, false);
        }

        public bool StartFileHistoryDialog(IWin32Window owner, string fileName)
        {
            return StartFileHistoryDialog(owner, fileName, null, false);
        }

        public bool StartFileHistoryDialog(string fileName, GitRevision revision)
        {
            return StartFileHistoryDialog(null, fileName, revision, false);
        }

        public bool StartFileHistoryDialog(string fileName)
        {
            return StartFileHistoryDialog(fileName, null);
        }

        public bool StartPushDialog(IWin32Window owner, bool pushOnShow)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PrePush))
                return true;

            using (var form = new FormPush())
            {
                if (pushOnShow)
                    form.PushAndShowDialogWhenFailed(owner);
                else
                    form.ShowDialog(owner);
            }

            InvokeEvent(owner, PostPush);

            return true;
        }

        public bool StartPushDialog(bool pushOnShow)
        {
            return StartPushDialog(null, pushOnShow);
        }

        public bool StartApplyPatchDialog(IWin32Window owner, string patchFile)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreApplyPatch))
                return true;

            using (var form = new FormApplyPatch())
            {
                form.SetPatchFile(patchFile);
                form.ShowDialog(owner);
            }

            InvokeEvent(owner, PostApplyPatch);

            return true;
        }

        public bool StartApplyPatchDialog(string patchFile)
        {
            return StartApplyPatchDialog(null, patchFile);
        }

        public bool StartApplyPatchDialog(IWin32Window owner)
        {
            return StartApplyPatchDialog(owner, null);
        }

        public bool StartApplyPatchDialog()
        {
            return StartApplyPatchDialog(null, null);
        }

        public bool StartEditGitAttributesDialog(IWin32Window owner)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreEditGitAttributes))
                return true;

            using (var form = new FormGitAttributes())
            {
                form.ShowDialog(owner);
            }

            InvokeEvent(owner, PostEditGitAttributes);

            return false;
        }

        public bool StartEditGitAttributesDialog()
        {
            return StartEditGitAttributesDialog(null);
        }

        private bool InvokeEvent(IWin32Window ownerForm, GitUIEventHandler gitUIEventHandler)
        {
            return InvokeEvent(this, ownerForm, gitUIEventHandler);
        }

        public GitModule Module
        {
            get
            {
                return GitCommands.GitModule.Current;
            }
        }

        public IGitModule GitModule
        {
            get
            {
                return Module;
            }
        }

        private void InvokePostEvent(IWin32Window ownerForm, bool actionDone, GitUIPostActionEventHandler gitUIEventHandler)
        {
            
            if (gitUIEventHandler != null)
            {
                var e = new GitUIPostActionEventArgs(ownerForm, this, actionDone);
                gitUIEventHandler(this, e);
            }
        }
        
        internal static bool InvokeEvent(object sender, IWin32Window ownerForm, GitUIEventHandler gitUIEventHandler)
        {
            try
            {
                var e = new GitUIEventArgs(ownerForm, Instance);
                if (gitUIEventHandler != null)
                    gitUIEventHandler(sender, e);

                return !e.Cancel;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception");
            }
            return true;
        }

        public bool StartBlameDialog(IWin32Window owner, string fileName)
        {
            return StartBlameDialog(owner, fileName, null);
        }

        private bool StartBlameDialog(IWin32Window owner, string fileName, GitRevision revision)
        {
            if (!RequiresValidWorkingDir(owner))
                return false;

            if (!InvokeEvent(owner, PreBlame))
                return false;

            using (var frm = new FormBlame(fileName, revision)) frm.ShowDialog(owner);

            InvokeEvent(owner, PostBlame);

            return false;
        }

        public bool StartBlameDialog(string fileName)
        {
            return StartBlameDialog(null, fileName, null);
        }

        private bool StartBlameDialog(string fileName, GitRevision revision)
        {
            return StartBlameDialog(null, fileName, revision);
        }

        private static void WrapRepoHostingCall(string name, IRepositoryHostPlugin gitHoster,
                                                Action<IRepositoryHostPlugin> call)
        {
            if (!gitHoster.ConfigurationOk)
            {
                var eventArgs = new GitUIEventArgs(null, Instance);
                gitHoster.Execute(eventArgs);
            }

            if (gitHoster.ConfigurationOk)
            {
                try
                {
                    call(gitHoster);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format("ERROR: {0} failed. Message: {1}\r\n\r\n{2}", name, ex.Message, ex.StackTrace),
                        "Error! :(");
                }
            }
        }

        public void StartCloneForkFromHoster(IWin32Window owner, IRepositoryHostPlugin gitHoster)
        {
            WrapRepoHostingCall("View pull requests", gitHoster, gh =>
            {
                using (var frm = new ForkAndCloneForm(gitHoster)) frm.ShowDialog(owner);
            });
        }

        public void StartCloneForkFromHoster(IRepositoryHostPlugin gitHoster)
        {
            StartCloneForkFromHoster(null, gitHoster);
        }

        internal void StartPullRequestsDialog(IWin32Window owner, IRepositoryHostPlugin gitHoster)
        {
            WrapRepoHostingCall("View pull requests", gitHoster,
                                gh =>
                                {
                                    using (var frm = new ViewPullRequestsForm(gitHoster)) frm.ShowDialog(owner);
                                });
        }

        internal void StartPullRequestsDialog(IRepositoryHostPlugin gitHoster)
        {
            StartPullRequestsDialog(null, gitHoster);
        }

        public void StartCreatePullRequest(IWin32Window owner)
        {
            List<IRepositoryHostPlugin> relevantHosts =
                (from gh in RepoHosts.GitHosters where gh.CurrentWorkingDirRepoIsRelevantToMe select gh).ToList();
            if (relevantHosts.Count == 0)
                MessageBox.Show(owner, "Could not find any repo hosts for current working directory");
            else if (relevantHosts.Count == 1)
                StartCreatePullRequest(owner, relevantHosts.First());
            else
                MessageBox.Show("StartCreatePullRequest:Selection not implemented!");
        }

        public void StartCreatePullRequest()
        {
            StartCreatePullRequest((IRepositoryHostPlugin)null);
        }

        public void StartCreatePullRequest(IWin32Window owner, IRepositoryHostPlugin gitHoster)
        {
            StartCreatePullRequest(owner, gitHoster, null, null);
        }

        public void StartCreatePullRequest(IRepositoryHostPlugin gitHoster)
        {
            StartCreatePullRequest(null, gitHoster, null, null);
        }

        public void StartCreatePullRequest(IRepositoryHostPlugin gitHoster, string chooseRemote, string chooseBranch)
        {
            StartCreatePullRequest(null, gitHoster, chooseRemote, chooseBranch);
        }

        public void StartCreatePullRequest(IWin32Window owner, IRepositoryHostPlugin gitHoster, string chooseRemote, string chooseBranch)
        {
            WrapRepoHostingCall("Create pull request", gitHoster,
                                gh =>
                                {
                                    new CreatePullRequestForm(gitHoster, chooseRemote, chooseBranch).Show(owner);
                                });
        }

        internal void RaisePreBrowseInitialize(IWin32Window owner)
        {
            InvokeEvent(owner, PreBrowseInitialize);
        }

        internal void RaisePostBrowseInitialize(IWin32Window owner)
        {
            InvokeEvent(owner, PostBrowseInitialize);
        }

        public void RaiseBrowseInitialize()
        {
            InvokeEvent(null, BrowseInitialize);
        }

        public IGitRemoteCommand CreateRemoteCommand()
        {
            return new GitRemoteCommand();
        }

        private class GitRemoteCommand : IGitRemoteCommand
        {
            public object OwnerForm { get; set; }

            public string Remote { get; set; }

            public string Title { get; set; }

            public string CommandText { get; set; }

            public bool ErrorOccurred { get; private set; }

            public string CommandOutput { get; private set; }

            public event GitRemoteCommandCompletedEventHandler Completed;

            public void Execute()
            {
                if (CommandText == null)
                    throw new InvalidOperationException("CommandText is required");

                using (var form = new FormRemoteProcess(CommandText))
                {
                    if (Title != null)
                        form.Text = Title;
                    if (Remote != null)
                        form.Remote = Remote;

                    form.HandleOnExitCallback = HandleOnExit;

                    form.ShowDialog(OwnerForm as IWin32Window);

                    ErrorOccurred = form.ErrorOccurred();
                    CommandOutput = form.OutputString.ToString();
                }
            }

            private bool HandleOnExit(ref bool isError, FormProcess form)
            {
                CommandOutput = form.OutputString.ToString();

                var e = new GitRemoteCommandCompletedEventArgs(this, isError, false);

                if (Completed != null)
                    Completed(form, e);

                isError = e.IsError;

                return e.Handled;
            }
        }
    }
}
