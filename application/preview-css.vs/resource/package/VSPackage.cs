
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
//using Microsoft.VisualStudio.Workspace;
//using Microsoft.VisualStudio.Workspace.VSIntegration.Contracts;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

//namespace Microsoft.Samples.VisualStudio.MSDNSearch
//{
//    public class MSDNSearchResult : IVsSearchItemResult
//    {
//        public MSDNSearchResult(string displaytext, string url, string description, MSDNSearchProvider provider)
//        {
//            this.DisplayText = displaytext;  // Stores the text of the link
//            // We'll use the description as tooltip - because it's pretty long and all items have it,
//            // returning it as part of Description will overload the QL popup with too much information
//            this.Tooltip = description;
//            // All items use the same icon
//            this.Icon = provider.ResultsIcon;

//            this.SearchProvider = provider;
//            this.Url = url;
//        }

//        public static MSDNSearchResult FromPersistenceData(string persistenceData, MSDNSearchProvider provider)
//        {
//            string[] strArr = persistenceData.Split(new string[] { Separator }, StringSplitOptions.None);

//            // Let's validate the string, to avoid crashing if someone is messing up with registry values
//            if (strArr.Length != 3)
//                return null;

//            string displayText = UnescapePersistenceString(strArr[0]);
//            string url = UnescapePersistenceString(strArr[1]);
//            string description = UnescapePersistenceString(strArr[2]);

//            if (string.IsNullOrEmpty(displayText) || string.IsNullOrEmpty(url))
//                return null;

//            return new MSDNSearchResult(displayText, url, description, provider);
//        }

//        const string Separator = "|";
//        const string Escape = "&";
//        const string EscapedSeparator = "&#124";
//        const string EscapedEscape = "&amp;";

//        static string EscapePersistenceString(string text)
//        {
//            if (text == null)
//            {
//                return String.Empty;
//            }
//            StringBuilder textBuilder = new StringBuilder(text);
//            textBuilder.Replace(Escape, EscapedEscape);
//            textBuilder.Replace(Separator, EscapedSeparator);
//            return textBuilder.ToString();
//        }

//        static string UnescapePersistenceString(string text)
//        {
//            if (String.IsNullOrEmpty(text))
//            {
//                return null;
//            }
//            StringBuilder textBuilder = new StringBuilder(text);
//            textBuilder.Replace(EscapedSeparator, Separator);
//            textBuilder.Replace(EscapedEscape, Escape);
//            return textBuilder.ToString();
//        }

//        // The URL to use for invoking the item
//        private string Url { get; set; }

//        // Action to be performed on execution of result from result list
//        public void InvokeAction()
//        {
//            //Process.Start(this.Url);
//        }

//        public string Description
//        {
//            get;
//            private set;
//        }

//        public string DisplayText
//        {
//            get;
//            private set;
//        }

//        public IVsUIObject Icon
//        {
//            get;
//            private set;
//        }

//        // Retrieves persistence data for this result
//        public string PersistenceData
//        {
//            get
//            {
//                // This is used for the MRU list.  We need to be able to fully recreate the result data.
//                return String.Join(Separator,
//                                    EscapePersistenceString(this.DisplayText),
//                                    EscapePersistenceString(this.Url),
//                                    EscapePersistenceString(this.Tooltip));
//            }
//        }

//        public IVsSearchProvider SearchProvider
//        {
//            get;
//            private set;
//        }

//        public string Tooltip
//        {
//            get;
//            private set;
//        }
//    }
//}

//namespace Microsoft.Samples.VisualStudio.MSDNSearch
//{
//    public class MSDNSearchTask : VsSearchTask
//    {
//        private MSDNSearchProvider provider;

//        public MSDNSearchTask(MSDNSearchProvider provider, uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchProviderCallback pSearchCallback)
//            : base(dwCookie, pSearchQuery, pSearchCallback)
//        {
//            this.provider = provider;
//        }

//        // The web client used to perform the web query
//        WebClient WebClient { get; set; }

//        // Starts the search by sending Query to MSDN. This function is called on a background thread.
//        protected override void OnStartSearch()
//        {
//            try
//            {
//                this.WebClient = new WebClient();
//                Uri webQuery = new Uri(String.Format("http://social.msdn.microsoft.com/search/en-US/feed?query={0}&format=RSS", Uri.EscapeDataString(this.SearchQuery.SearchString)));

//                // Don't use WebClient.DownloadXXXX synchronous functions because they can only be called on one thread at a time.
//                // After starting a search in Quick Launch the user may type a different string and start a different search,
//                // in which case a different MSDNSearchTask will be created (possibly on other thread) and will be used to start
//                // a new web request - therefore we need to use async functions to do the online query.
//                this.WebClient.DownloadDataCompleted += RSSDownloadComplete;
//                this.WebClient.DownloadDataAsync(webQuery);

//                // The search task will be marked complete when the completion event is signaled.
//            }
//            catch (WebException ex)
//            {
//                // Failed to download the RSS feed; remember the error code and set the task status
//                this.ErrorCode = ex.HResult;
//                this.SetTaskStatus(VSConstants.VsSearchTaskStatus.Error);
//                // Report completion
//                this.SearchCallback.ReportComplete(this, this.SearchResults);
//            }
//        }

//        protected override void OnStopSearch()
//        {
//            // If the search is stopped and we have an async download in progress, stop it
//            if (this.WebClient != null)
//            {
//                this.WebClient.CancelAsync();
//            }
//        }

//        void RSSDownloadComplete(object sender, DownloadDataCompletedEventArgs e)
//        {
//            // If the request was cancelled because the search was cancelled/abandoned, there is nothing else to do here
//            // The task completion was already notified to the search callback and the task status is already set.
//            if (e.Cancelled)
//            {
//                return;
//            }

//            // If the request threw an exception, remember the code and set the task status
//            if (e.Error != null)
//            {
//                this.ErrorCode = e.Error.HResult;
//                this.SetTaskStatus(VSConstants.VsSearchTaskStatus.Error);
//            }
//            else
//            {
//                try
//                {
//                    // Parser code to parse through RSS results
//                    var xmlDocument = new XmlDocument();

//                    // The result is UTF-8 encoded, so make sure to decode it correctly
//                    string resultXml = Encoding.UTF8.GetString(e.Result);
//                    xmlDocument.LoadXml(resultXml);

//                    var root = xmlDocument.DocumentElement;

//                    // Each item/entry is a unique result
//                    var entries = root.GetElementsByTagName("item");
//                    if (entries.Count == 0)
//                        entries = root.GetElementsByTagName("entry");

//                    foreach (var node in entries)
//                    {
//                        // As we prepare the results, periodically check if the search was canceled
//                        if (this.TaskStatus == VSConstants.VsSearchTaskStatus.Stopped)
//                        {
//                            // The completion was already notified by the base.OnStopSearch, there is nothing else to do
//                            return;
//                        }

//                        var entry = node as XmlElement;
//                        if (entry != null)
//                        {
//                            string title = null;
//                            string url = null;
//                            string description = null;

//                            // Title tag provides result title
//                            var titleNodes = entry.GetElementsByTagName("title");
//                            if (titleNodes.Count > 0)
//                            {
//                                title = (titleNodes[0] as XmlElement).InnerText;
//                            }

//                            // Get the item's description as well
//                            var descriptionNode = entry.GetElementsByTagName("description");
//                            if (descriptionNode.Count > 0)
//                            {
//                                description = (descriptionNode[0] as XmlElement).InnerText;
//                            }

//                            // Link / URL / ID tag provides the URL linking the result string to its page
//                            var linkNodes = entry.GetElementsByTagName("link");
//                            if (linkNodes.Count == 0)
//                                linkNodes = entry.GetElementsByTagName("url");
//                            if (linkNodes.Count == 0)
//                                linkNodes = entry.GetElementsByTagName("id");

//                            if (linkNodes.Count > 0)
//                            {
//                                url = (linkNodes[0] as XmlElement).InnerText;
//                            }

//                            if (title != null && url != null)
//                            {
//                                // Create the results and then have the task report the result
//                                var result = new MSDNSearchResult(title, url, description, this.provider);
//                                this.SearchCallback.ReportResult(this, result);
//                                // Increment the number of results reported by the provider
//                                this.SearchResults++;
//                            }
//                        }
//                    }

//                    // Mark the task completed
//                    this.SetTaskStatus(VSConstants.VsSearchTaskStatus.Completed);
//                }
//                catch (XmlException ex)
//                {
//                    // Remember the error code and set correct task status but otherwise don't report xml parsing errors, in case MSDN RSS format changes
//                    this.ErrorCode = ex.HResult;
//                    this.SetTaskStatus(VSConstants.VsSearchTaskStatus.Error);
//                }
//            }

//            // Report completion of the search (with error or success)
//            this.SearchCallback.ReportComplete(this, this.SearchResults);
//        }

//        protected new IVsSearchProviderCallback SearchCallback
//        {
//            get
//            {
//                return (IVsSearchProviderCallback)base.SearchCallback;
//            }
//        }
//    }
//}

//namespace Microsoft.Samples.VisualStudio.MSDNSearch
//{
//    /// <summary>
//    ///  Search Provider for MSDN Library
//    ///  GUID uniquely identifies and differentiates MSDN search from other Quick Launch searches
//    ///  Also, the category Shortcut is a unique string identifier, allowing scoping the results only from this provider.
//    /// </summary>
//    //
//    // A global search provider declared statically in the registry needs the following:
//    // 1) a class implementing the IVsSearchProvider interface
//    // 2) the provider class specifying a Guid attribute of the search provider (provider_identifier)
//    // 3) the provider class type declared on the Package-derived class using the ProvideSearchProvider attribute
//    // 4) the package must derive from ExtensionPointPackage for automatic extension creation.
//    //    An alternate solution is for the package to implement IVsPackageExtensionProvider and create the search
//    //    provider when CreateExtensionPoint(typeof(IVsSearchProvider).GUID, provider_identifier) is called.
//    //
//    // Declare the search provider guid, to be used during registration
//    // and during the provider's automatic creation as an extension point
//    [Guid("55DC15FE-B1CD-40E2-B7DC-68012FCFE674")]
//    public class MSDNSearchProvider : IVsSearchProvider
//    {
//        // Defines all string variables like Description(Hover over Search Heading), Search Heading text, Category Shortcut
//        private const string CategoryShortcutString = "METAOUTPUT";

//        // Main Search method that calls MSDNSearchTask to create and execute search query
//        public IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchProviderCallback pSearchCallback)
//        {
//            if (dwCookie == VSConstants.VSCOOKIE_NIL)
//            {
//                return null;
//            }

//            return null;// new MSDNSearchTask(this, dwCookie, pSearchQuery, pSearchCallback);
//        }

//        // Verifies persistent data to populate MRU list with previously selected result
//        public IVsSearchItemResult CreateItemResult(string lpszPersistenceData)
//        {
//            return MSDNSearchResult.FromPersistenceData(lpszPersistenceData, this);
//        }

//        // Get the GUID that identifies this search provider
//        public Guid Category
//        {
//            get
//            {
//                return GetType().GUID;
//            }
//        }

//        // MSDN Search Category Heading
//        public string DisplayText
//        {
//            get
//            {
//                return "METAOUTPUT DISPLAY TEXT";// Resources.MSDNSearchProviderDisplayText;
//            }
//        }

//        // MSDN Search Description - shows as tooltip on hover over Search Category Heading
//        public string Description
//        {
//            get
//            {
//                return "METAOUTPUT DESCRIPTION";// Resources.MSDNSearchProviderDescription;
//            }
//        }

//        protected IVsUIObject _resultsIcon = null;
//        /// <summary>
//        /// Returns the icon for each result. In this case, the same icon is returned for each result,
//        /// so we'll use the same object to save memory and time creating the images.
//        ///
//        /// Helper classses in Microsoft.Internal.VisualStudio.PlatformUI can be used to construct an IVsUIObject of VsUIType.Icon type.
//        /// Use Win32IconUIObject if you have an HICON, use WinFormsIconUIObject if you have a System.Drawing.Icon, or
//        /// use WpfPropertyValue.CreateIconObject() if you have a WPF ImageSource.
//        /// There are also similar classes and functions that can be used to create objects implementing IVsUIObject of type VsUIType.Bitmap
//        /// starting from a bitmap image (e.g. Win32BitmapUIObject, WpfPropertyValue.CreateBitmapObject).
//        /// </summary>
//        public IVsUIObject ResultsIcon
//        {
//            get
//            {
//                //if (this._resultsIcon == null)
//                //{
//                //    // Create an IVsUIObject from the winforms icon object
//                //    this._resultsIcon = new WinFormsIconUIObject(Resources.ResultsIcon);
//                //}
//                return this._resultsIcon;
//            }
//        }


//        // MSDN Category shortcut to scope results to to show only from MSDN Library
//        // This is a unique string, and should not be localized.
//        public string Shortcut
//        {
//            get
//            {
//                return CategoryShortcutString;
//            }
//        }

//        public string Tooltip
//        {
//            get { return null; } // No additional tooltip
//        }


//        public void ProvideSearchSettings(IVsUIDataSource pSearchOptions)
//        {
//            // This provider uses the default settings, there is no need to change the data source properties
//        }
//    }
//}

//namespace Menees.VsTools.Editor
//{
//    internal sealed class FindResultsClassifier : ClassifierBase
//	{
//		#region Private Data Members

//		// This pattern looks for optional whitespace, DRIVE:\ or \\, a valid NAMEPART (disallowing the chars returned by
//		// Path.GetInvalidFileNameChars()), an optional \ separator, and then allows zero or more repeats of the NAMEPART[\] portion.
//		private const string FilenamePrefixPattern = @"^\s*(\w\:\\|\\\\)([^\""\<\>\|\u0000\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r" +
//			@"\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f\:\*\?\\\/]\\?)*";

//		// FilenameOnlyRegex must match to the end of the line.  FilenameAndLineNumberRegex requires either
//		// a subsequent "(Line):" (for Find) or "(Line,Col):" (for Replace) before the match details.
//		private static readonly Regex FilenameOnlyRegex = new(FilenamePrefixPattern + "$", RegexOptions.Compiled);
//		private static readonly Regex FilenameAndLineNumberRegex = new(FilenamePrefixPattern + @"\(\d+(\,\d+)?\)\:", RegexOptions.Compiled);

//		private static readonly Regex FindAllPattern = new("(?n)Find all \"(?<pattern>.+?)\",", RegexOptions.Compiled);
//		private static readonly Regex ReplaceAllPattern = new("(?n)Replace all \".+?\", \"(?<pattern>.+?)\",", RegexOptions.Compiled);

//		private static readonly object ResourceLock = new();
//		private static IClassificationType matchType;
//		private static IClassificationType fileNameType;
//		private static IClassificationType detailType;

//		private FindArgs findArgs;
//		#endregion

//		#region Constructors

//		public FindResultsClassifier(ITextBuffer buffer, IClassificationTypeRegistryService registry)
//			: base(buffer)
//		{
//			lock (ResourceLock)
//			{
//				if (matchType == null)
//				{
//					matchType = registry.GetClassificationType(FindResultsFormats.MatchFormat.ClassificationName);
//					fileNameType = registry.GetClassificationType(FindResultsFormats.FileNameFormat.ClassificationName);
//					detailType = registry.GetClassificationType(FindResultsFormats.DetailFormat.ClassificationName);
//				}
//			}
//		}

//		#endregion

//		#region Protected Methods

//		protected override void GetClassificationSpans(List<ClassificationSpan> result, SnapshotSpan span, HighlightOptions options)
//		{
//			bool showFileNames = options.HighlightFindResultsFileNames;
//			bool showMatches = options.HighlightFindResultsMatches;
//			bool showDetails = options.HighlightFindResultsDetails;

//			if (showFileNames || showMatches || showDetails)
//			{
//				foreach (ITextSnapshotLine line in GetSpanLines(span))
//				{
//					string text = line.GetText();
//					if (!string.IsNullOrEmpty(text))
//					{
//						// The first line in the window may contain the Find arguments, so we parse and highlight it specially.
//						// In VS 2019 16.5 the Find All References results List View will not contain the Find arguments though.
//						bool firstLine = line.LineNumber == 0;
//						if (firstLine)
//						{
//							// We have to store this in a member variable because VS may call us for multiple consecutive spans in one window.
//							this.findArgs = FindArgs.TryParse(text);
//						}

//						if (firstLine && this.findArgs != null)
//						{
//							if (showMatches)
//							{
//								AddClassificationSpan(result, line, this.findArgs.PatternIndex, this.findArgs.PatternLength, matchType);
//							}
//						}
//						else
//						{
//							// If we couldn't parse any findArgs (e.g., for Find All References), then we shouldn't do detail highlighting.
//							HighlightResultLine(result, line, text, showFileNames, showMatches, showDetails && this.findArgs != null, this.findArgs);
//						}
//					}
//				}
//			}
//		}

//		#endregion

//		#region Private Methods

//		private static void AddClassificationSpan(List<ClassificationSpan> result, ITextSnapshotLine line, int start, int length, IClassificationType type)
//		{
//			SnapshotPoint startPoint = line.Start + start;
//			SnapshotPoint endPoint = startPoint + length;
//			SnapshotSpan snapshotSpan = new(startPoint, endPoint);
//			ClassificationSpan classificationSpan = new(snapshotSpan, type);
//			result.Add(classificationSpan);
//		}

//		private static void HighlightResultLine(
//			List<ClassificationSpan> result,
//			ITextSnapshotLine line,
//			string text,
//			bool showFileNames,
//			bool showMatches,
//			bool showDetails,
//			FindArgs findArgs)
//		{
//			try
//			{
//				if (findArgs?.ListFileNamesOnly ?? false)
//				{
//					if (showFileNames && FilenameOnlyRegex.IsMatch(text))
//					{
//						AddClassificationSpan(result, line, 0, text.Length, fileNameType);
//					}
//				}
//				else
//				{
//					Match fileNameMatch = FilenameAndLineNumberRegex.Match(text);
//					if (fileNameMatch.Success)
//					{
//						if (showFileNames)
//						{
//							AddClassificationSpan(result, line, fileNameMatch.Index, fileNameMatch.Length, fileNameType);
//						}

//						// Note: If a match line has no leading whitespace, then there's no space between the 'file(num):' and the match line text.
//						int start = fileNameMatch.Index + fileNameMatch.Length;
//						Match patternMatch = findArgs?.MatchExpression.Match(text, start);
//						while (patternMatch?.Success ?? false)
//						{
//							if (showDetails && start < patternMatch.Index)
//							{
//								AddClassificationSpan(result, line, start, patternMatch.Index - start, detailType);
//							}

//							if (showMatches)
//							{
//								AddClassificationSpan(result, line, patternMatch.Index, patternMatch.Length, matchType);
//							}

//							start = patternMatch.Index + patternMatch.Length;
//							patternMatch = patternMatch.NextMatch();
//						}

//						if (showDetails && start < text.Length)
//						{
//							AddClassificationSpan(result, line, start, text.Length - start, detailType);
//						}
//					}
//				}
//			}
//#pragma warning disable CC0004 // Catch block cannot be empty. Comment explains.
//			catch (RegexMatchTimeoutException)
//			{
//				// We set a short Regex timeout because we don't want highlighting to add significant time.
//				// It's better to skip highlighting than to make the results take forever to display.
//			}
//#pragma warning restore CC0004 // Catch block cannot be empty
//		}

//		#endregion

//		#region Private Types

//		private sealed class FindArgs
//		{
//			#region Constructors

//			private FindArgs()
//			{
//				// This should only be called by TryParse.
//			}

//			#endregion

//			#region Public Properties

//			public int PatternIndex { get; private set; }

//			public int PatternLength { get; private set; }

//			public Regex MatchExpression { get; private set; }

//			public bool ListFileNamesOnly { get; private set; }

//			#endregion

//			#region Public Methods

//			public static FindArgs TryParse(string text)
//			{
//				FindArgs result = null;

//				// VS 2019 16.5 totally changed the Find Results window and options. Update 16.5.4 restored some functionality to its List View,
//				// but now it truncates the pattern after 20 characters. It still doesn't escape patterns, so comma and double quote are ambiguous.
//				Match match = FindAllPattern.Match(text);
//				if (!match.Success)
//				{
//					match = ReplaceAllPattern.Match(text);
//				}

//				if (match.Success && match.Groups.Count == 2)
//				{
//					int afterMatchIndex = match.Index + match.Value.Length;
//					int listFileNamesOnly = text.IndexOf("List filenames only", afterMatchIndex, StringComparison.OrdinalIgnoreCase);
//					int regularExpressions = text.IndexOf("Regular expressions", afterMatchIndex, StringComparison.OrdinalIgnoreCase);
//					int wholeWord = text.IndexOf("Whole word", afterMatchIndex, StringComparison.OrdinalIgnoreCase);
//					int matchCase = text.IndexOf("Match case", afterMatchIndex, StringComparison.OrdinalIgnoreCase);

//					Group group = match.Groups[1];
//					string pattern = group.Value;
//					const string Ellipsis = "...";
//					bool truncated = false;
//					if (pattern.EndsWith(Ellipsis))
//					{
//						pattern = pattern.Substring(0, pattern.Length - Ellipsis.Length);
//						truncated = true;
//					}

//					result = new FindArgs
//					{
//						ListFileNamesOnly = listFileNamesOnly >= 0,
//						PatternIndex = group.Index,
//						PatternLength = pattern.Length,
//					};

//					try
//					{
//						if (regularExpressions < 0)
//						{
//							pattern = Regex.Escape(pattern);

//							// VS seems to only apply the "Whole word" option when "Regular expressions" isn't used, so we'll do the same.
//							// We can't apply it at the end of a truncated pattern because the truncation might have occurred mid-word.
//							if (wholeWord >= 0)
//							{
//								const string WholeWordBoundary = @"\b";
//								pattern = WholeWordBoundary + pattern + (truncated ? string.Empty : WholeWordBoundary);
//							}
//						}

//						// We don't want to spend too much time searching each line.
//						TimeSpan timeout = TimeSpan.FromMilliseconds(100);
//						result.MatchExpression = new Regex(pattern, matchCase >= 0 ? RegexOptions.None : RegexOptions.IgnoreCase, timeout);
//					}
//					catch (ArgumentException)
//					{
//						// We did our best to parse out the pattern and build a suitable regex.  But it's possible that the
//						// parsed pattern was wrong (e.g., if it contained an unescaped ", " substring).  So if Regex
//						// throws an ArgumentException, we just can't highlight this time.
//						result = null;
//					}
//				}

//				return result;
//			}

//			#endregion
//		}

//		#endregion
//	}
//}

namespace resource.package
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    //[ProvideSearchProvider(typeof(Microsoft.Samples.VisualStudio.MSDNSearch.MSDNSearchProvider), "METAOUTPUT")]
    [Guid(CONSTANT.GUID)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class PreviewCSS : AsyncPackage
    {
        private class MyEvents : IVsSolutionEvents
        {
            public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
            {
                return 0;
            }

            public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
            {
                return 0;
            }

            public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
            {
                return 0;
            }

            public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
            {
                return 0;
            }

            public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
            {
                return 0;
            }

            public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
            {
                return 0;
            }

            public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
            {
                //var a_Workspace = GetWorkspaceService();
                //var a_Context111 = a_Workspace.CurrentWorkspace?.GetFindFilesService();
                return 0;
            }

            public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
            {
                return 0;
            }

            public int OnBeforeCloseSolution(object pUnkReserved)
            {
                return 0;
            }

            public int OnAfterCloseSolution(object pUnkReserved)
            {
                return 0;
            }
        }

        //private static IVsFolderWorkspaceService GetWorkspaceService()
        //{
        //    IComponentModel componentModel = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel).GUID) as IComponentModel;
        //    var workspaceServices = componentModel.DefaultExportProvider.GetExports<IVsFolderWorkspaceService>();

        //    if (workspaceServices != null && workspaceServices.Any())
        //    {
        //        return workspaceServices.First().Value;
        //    }
        //    return null;
        //}

        internal static class CONSTANT
        {
            public const string APPLICATION = "Visual Studio";
            public const string COPYRIGHT = "Copyright (c) 2020-2022 by Viacheslav Lozinskyi. All rights reserved.";
            public const string DESCRIPTION = "Quick preview of CSS files";
            public const string GUID = "C6915272-78B1-4B1E-A31C-00BACBE2A500";
            public const string NAME = "Preview-CSS";
            public const string VERSION = "1.0.2";
        }

        private Events s_Events = null;
        private FindEvents s_FindEvents = null;
        //private IVsFolderWorkspaceService s_WorkspaceService = null;
        //private Package s_Package = null;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            {
                extension.AnyPreview.Connect();
                extension.AnyPreview.Register(".CSS", new resource.preview.VSPreview());
            }
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            }
            try
            {
                if (string.IsNullOrEmpty(atom.Trace.GetFailState(CONSTANT.APPLICATION)) == false)
                {
                    var a_Context = Package.GetGlobalService(typeof(SDTE)) as DTE2;
                    if (a_Context != null)
                    {
                        var a_Context1 = (OutputWindowPane)null;
                        for (var i = a_Context.ToolWindows.OutputWindow.OutputWindowPanes.Count; i >= 1; i--)
                        {
                            if (a_Context.ToolWindows.OutputWindow.OutputWindowPanes.Item(i).Name == "MetaOutput")
                            {
                                a_Context1 = a_Context.ToolWindows.OutputWindow.OutputWindowPanes.Item(i);
                                break;
                            }
                        }
                        if (a_Context1 == null)
                        {
                            a_Context1 = a_Context.ToolWindows.OutputWindow.OutputWindowPanes.Add("MetaOutput");
                        }
                        if (a_Context1 != null)
                        {
                            a_Context1.OutputString("\r\n" + CONSTANT.NAME + " extension doesn't work without MetaOutput.\r\n    Please install it (https://www.metaoutput.net/download)\r\n");
                            a_Context1.Activate();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            {
                Package s_Package = this;
                //var a_ServiceProvider = this;
                //var a_ServiceProvider = GetService(typeof(IVsUIShell));// as IServiceProvider;
                var a_ServiceProvider1 = GetGlobalService(typeof(IVsUIShell));// as IServiceProvider;
                //IVsSolutionUIHierarchyWindow solutionWindows = (IVsSolutionUIHierarchyWindow)VsShellUtilities.GetUIHierarchyWindow(s_Package as IServiceProvider, VSConstants.StandardToolWindows.SolutionExplorer);
                //if (solutionWindows is IVsWindowSearch)
                {
                    //var slw = solutionWindows as IVsWindowSearch;
                    //slw.ClearSearch();
                }
                //a_ServiceProvider = a_ServiceProvider;
            }
            //{
            //    var a_Context = GetGlobalService(typeof(DTE)) as DTE2;
            //    s_Events = a_Context.Events;
            //    s_FindEvents = s_Events.FindEvents;
            //    //s_FindEvents.FindDone += __FindEvents_FindDone;
            //    a_Context = a_Context;
            //}
            //{
            //    var a_Context = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            //    var aaa = (uint)0;
            //    a_Context.AdviseSolutionEvents(new MyEvents(), out aaa);
            //    a_Context = a_Context;
            //}
            //{
            //    var a_Context = GetGlobalService(typeof(IVsTaskList));
            //    var a_Context1 = GetGlobalService(typeof(IVsSearchItemDynamicResult));
            //    var a_Context2 = GetGlobalService(typeof(IVsSearchTask));
            //    //var a_Context2 = GetGlobalService(typeof(ITextSearchNavigator));
            //    //var a_Context3 = GetGlobalService(typeof(ITextSearchNavigatorFactoryService));
            //    a_Context = a_Context;
            //}
            //{
            //    //var a_Context = Package.GetGlobalService(typeof(IVsSearchQuery));
            //    s_WorkspaceService = GetWorkspaceService();
            //    //s_WorkspaceService.OnActiveWorkspaceChanged = __SolutionEvents_Opened1;
            //    var a_Context111 = s_WorkspaceService.CurrentWorkspace?.GetFindFilesService();
            //    //var componentModel = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel).GUID) as IComponentModel;
            //    var a_Context0 = Package.GetGlobalService(typeof(IWorkspace));
            //    //var a_Context2 = componentModel.GetService<IWorkspace>();
            //    //var a_Context3 = componentModel.DefaultExportProvider.GetExport<IWorkspace>();
            //    var a_Context = s_WorkspaceService.CurrentWorkspace.GetFindFilesService();
            //    //var a_Context = Package.GetGlobalService(typeof(IFindFilesService));
            //    //var a_Context1 = GetService(typeof(IFindFilesService));
            //    a_Context = a_Context;
            //}
            //{
            //    //var a_Context = Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.FindResults.SVsFindResults));
            //    //var a_Context1 = a_Context as Microsoft.VisualStudio.Shell.FindResults.IFindResultsWindow;
            //    //var a_Context2 = a_Context as Microsoft.VisualStudio.Shell.FindResults.IFindResultsService;
            //    //var a_Name1 = a_Context.GetType();
            //    //var a_Name2 = a_Context.ToString();
            //    //var a_Context3 = a_Context2.StartSearch("METAOUTPUT", "METAOUTPUT DESCRIPTION");
            //    //a_Context = a_Context;
            //}
        }

        public static void __FindEvents_FindDone(vsFindResult Result, bool Cancelled)
        {
            //var a_Workspace = GetWorkspaceService();
            //var a_Context111 = a_Workspace.CurrentWorkspace?.GetFindFilesService();
        }

        protected override int QueryClose(out bool canClose)
        {
            {
                extension.AnyPreview.Disconnect();
                canClose = true;
            }
            return VSConstants.S_OK;
        }
    }
}
