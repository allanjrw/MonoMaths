using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Gtk;
using Gdk;
using JLib;

namespace MonoMaths
{

public partial class MainWindow : Gtk.Window
{
  // Keep this instance field up the top of the file, for ready access.
  public string MainWindowTitle = "MonoMaths 1.11.00";// Form title changes during op'n.; when 'File | New' is clicked,
        // the title is reset to this value. NB: (1) Increment the last 2 digits every time any change is made which would affect
        // future coding by a user; (2) Increment the middle 2 digits whenever code updating would cause existing functions to behave
        // differently in some preexisting user program.

//-------------------------------
//   STATIC FIELDS:
//-------------------------------
// SERVICING TEXT VIEWS:
  public static byte[] ClrAssBack    = new byte[]{ 255, 250, 244 };
  public static byte[] ClrAssBackPaused = new byte[]{ 255, 212, 169 };
  public static byte[] ClrResBack    = new byte[]{ 242, 255, 255 };
  public static byte[] ClrAssText    = new byte[]{ 0, 0, 255 };
  public static byte[] ClrComment    = new byte[]{ 255, 0, 255 };
  public static byte[] ClrHeader     = new byte[]{ 255, 0, 0 };
  public static byte[] ClrIgnore     = new byte[]{ 0, 200, 0 };
  public static byte[] ClrPausedAt   = new byte[]{ 128, 255, 0 }; // colour of line which has just evoked 'pause(.);'
  public static byte[] ClrFinds      = new byte[]{ 128, 255, 0 }; // colour of finds after a search of text.
  public static byte[] ClrReplaced   = new byte[]{ 255, 197, 197 };
  public static byte[] ClrQuery      = new byte[]{ 255, 255, 128 };
  public static byte[] ClrBreakpoint = new byte[]{ 255, 191, 127 };
  public static byte[] ClrCurrentBreak = new byte[]{ 255, 165, 74 };

  public static string OrigFontNameAss = FontNameAss = "DejaVu Sans Condensed"; // Other defaults, if not found: see system-specific section.
  public static string FontNameAss = OrigFontNameAss;
  public static float FontPointsAss = 11F;
  public static string FontNameRes = "DejaVu Sans Condensed";
  public static float FontPointsRes = 11F;
  public static string
    CommentCue = "//",       HeaderCue = "__ ", // Note the final space.
    IgnoreCueClose = "*/",   IgnoreCueOpen = "/*";
  public static string CueCharacters = "/*_ "; // **** Used in KeyRelease handler. If changing the above, change this. If allowing user to
        // change cue strings, whatever routine handled that change would also have to reset this as unduplicated set of all cue chars.
  public static bool UseRemarksTags = true, UseMarkUpTags = false;
  public static string ProgramPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/MonoMaths/";
  public static string HomePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/";
        // The Environment call always returns "/home/xxx", where typically "xxx" is the nickname of the person logged in.
  public static string SysFn1FilePathName = "/home/jon/Projects_MonoDevelop/MonoMaths/SysFns1.cs";
  public static string SysFn2FilePathName = "/home/jon/Projects_MonoDevelop/MonoMaths/SysFns2.cs";
  public static string SysFn3FilePathName = "/home/jon/Projects_MonoDevelop/MonoMaths/SysFns3.cs";
  public static string CapturedArrayPathName = ProgramPath + "Captured Array"; // If you click 'capture' on the variable display,
                                              // the variable's descriptors and contents are saved in this file; fn. 'captured(.)' reloads it.
  public static string ArrayDisplayFormatStr = ""; // The initial format when you double-click on an array name in Assts. Window, after a run.
  public static int NoRecentFileSlots = 12; // *** Sadly, this is fixed for now. To make this alterable, you will have to
                                            //  master how to change settings all over the place, including in gui.stetic.
  private static int KeysDownTimeOut = 5000; // msecs. If the previous keypress was a prefix key, after this time it is forgotten.
  private Boost PrefixKey = new Boost(false);
  public static char StopNow; // set by double-clicking on 'GO' (--> 'G') or 'ABORT' (--> 'A'); normal inactive state is ' ';
  public static MainWindow ThisWindow; // This allows static methods to access the one-and-only running instance of MainForm.
  public static int CurrentRunStatus; // 0 = no run; 1 = running; 2 = interrupted; 3 = on hold.
  public static string LblAssDefaultText = "    ASSIGNMENTS:";
  public static string LblResDefaultText = "    RESULTS:";
  public static string[] RunStatusText = new string[]
    // Size must match the no. of possible categories of values of CurrentRunStatus in method HoldingPattern(.). In each
    //  string except the first empty string, the last char. MUST be ']' (no spaces).
    { "", "   [RUNNING]", "   [INTERRUPTED]", "   [ON HOLD]", "   [PAUSE #]", "   [BREAK POINT]" }; // '#' will be replaced by the pause index
  public static string NextPlotsPointChars = ""; // point chars string for the next plot only - used in graphing.
  public static int MaxExtraSubmenus = 20; // *** As the menus were built in at design time, this no. is NOT arbitrary; if you
                                           //  want to change it, you have to create/delete more submenus via the MonoDevelop designer.
                                           //  AND you will have to alter InitializeExtraMenus() accordingly.
  public static List<Plot> Plots2D = new List<Plot>();
  public static List<Plot> Plots3D = new List<Plot>();
  private static bool KillOldGraphs = true; // If changing this, change the startup text of Graph_OldGraphs.
  public static Duo SelStore; // .X can be used for very temporary storage of REditAss.SelectionStart, .Y do. .SelectionLength.
  public static bool ShutDown = false; // If set to 'true', the instance is closed in method GO().
  public static Strint2 ExitPlus = new Strint2(0); // Used to force actions of 'File | New' or '..| Load' after current user pgm ends.
  public static Gdk.Color Black = Gdk.Color.Zero; // for use as default in _ParseColour(.) calls.
  public static bool FailedRetrievalOfHintsText = false; // Set to true on first failure to retrieve; saves recurrent error messages with F1.;
      // important, as F1 doubles up for Hints text and for local function headers (which do not use Hints text).
  public static string HoldREditAssText = "";
  public static int HoldREditAssTopLocnForMarkUp = -1;
  public static int HoldREditAssCursorLocnForMarkUp = 0;
  public static bool HoldUseRemarksTags;
  public static bool HoldUnsavedData;
  public static bool DisplayScalarValuesAfterTheRun = true;
  public static Tetro LastF1BoxPosition = new Tetro(-1, -1, -1, -1); // Ensures the next F1 message box posn & size are as for the last one when it closed.
  public static Tetro LastVarsBoxPosition = new Tetro(-1, -1, -1, -1); // Same idea, for the display of current variable values.
  public static List<string> Pnemonic; // Double-keypress at KeyDown() looks for Pnemonic[n], to replace with PnemonicReplacemt[n].
  public static List<string> PnemonicReplacemt;
  // Defaults for variable display (by key F1 with cursor on variable name):
  public static string OrigStringFmt = "G5"; // string to go betweeen brackets of '.ToString()'.
  public static string OrigDelimiter = ""; // Can be empty string (for ", ") or JS.TabS (for tabs).
  public static int OrigTabStep = 50;
  public static bool OrigShowColumnNos = false;
  public static int ScreenHeight, ScreenWidth;

//-------------------------------
//   INSTANCE FIELDS
//-------------------------------

  private string CurrentDataPath = ""; // Set with default at startup. Has a final slash.
  private string PnemonicFName = ""; // Set to "" at startup, then filled from INI file.
  private double RelHtAss = 0.75;//REditAss ht. div. by (REditAss ht.+REditRes ht.)
  public int ButtonDownY = -1; // Used in storing the mouse-button-down value for ref. by button-up event.
  public bool UnsavedData = false; // Should be changed only by using method SetUnsavedData(.).
  public int MinimumForUnsavedDataWarning = 50; // If BuffAss holds less text than this there will still
      // be a star in the main window title to indicate unsaved data, but e.g. 'File | New' will not evoke a warning message box.
  private string INIFileName = "MonoMaths.INI";
  private string[] RawLines; // Will become the contents of REditAss, line by line,
  // with remarks and rem-lines and blank lines removed.
  private bool PreserveResults = false; // If true, REditRes not cleaned at start of a new program run.
  public string CurrentPath = HomePath; // Set to the home path here at startup, but should be reset by INI file.
  public string CurrentName = "";
  public string PgmFilePath = "", PgmFileName = ""; // refers to user's pgm, not MonoMaths code itself. When GO runs, CurrentP/Nm --> PgmFileP/Nm.
  public string CurrentFnPath = ""; // Location of stored user functions.
  private const int SizeOfINR = 52; // = no. available for general use (currently 40) + NoRecentFileSlots (fixed at 12 -- see comments at its def'n.)
  public INIRecord[] INR = new INIRecord[SizeOfINR]; // Filled with default values at startup, then
  // with alterations with INI file loading; is basis for INI file saving.
  // Any corrections to data that can be stored in the INI file is reflected in
  // this structure, in case the user wants to save it.
  private string[] RecentFiles = new string[NoRecentFileSlots];
  private int NoRecentFiles; // Can vary during run, but can't exceed NoRecentFileSlots.
  private List<ChangeRec> HistoryStack;
  private int HistoryStackMaxSize = 50;
  private int HistoryPtr;
  private List<Duo> NavigationStack; // .X = top left cnr. of the screen; .Y = cursor position.
  private int NavigationStackMaxSize = 50;
  private int NavigationPtr;
  private Duo BeforeGOPtr; // .X is the .Top field of the visible rectangle (like ChangeRec field VisibleTop), .Y the cursor position;
                           // filled at each call to GO(.), and then available to return the screen visible upon starting the run.
  private string LastAssText = ""; // set, and used for comparisons, in the TextChanged event.
  private bool Undoing, Redoing, Navigating;
  bool BlockTextChangeEvents = true; // Blocks stuff in OnBuffAssChangedEvent() event.
  private TextMark BkMk1; // Used to bookmark the char. posn. at top left of visible part of buffer.
 // Event catching and blocking parameters:
  public delegate int KeySnoopFunc (Widget widge, Gdk.EventKey eve);
  private string LastSoughtText = "";  private bool LastMatchCase = true, LastWholeWord = false; // All are search parameters.
  private int LastKeyDownTime = 0; // time of last keypress.
  public int WhatKeyIsDown = 0; // Holds the value of the key currently being pressed down.
  public int KeyQueueLength = 25; // Defines the length of the two arrays in the next line.
  public int[] KeyQueue, LastKeyCombo; // Each will be set to length KeyQueueLength. KeyQueue[0] is set to 0 at startup + whenever
      // a key is released. When a key goes down, KeyQueue is rotated up, and the key code goes into KeyQueue[0]. If, while it is down,
      // another key goes down, that in turn rotates KeyQueue and puts its code into KeyQueue[0]. Upon key release, again rotation occurs,
      // and 0 goes to [0]; then the whole lot is copied to LastKeyCombo, of which [0] is set to 1. (System function "keyed" changes this
      // to -1 after reading it.)
  private Dictionary<string, int> SpecialKeys; // Special keypresses go here. KEY: Format is "<helper key(s)>_<printable key value>"
      // for a single keying (e.g. 'cntrl-A' would be "C_97", 'cntrl-shift-A' would be "C_65", 'ctrl-alt-A' "CA_97". Note that
      // the shift key is taken up into the printable key value.) For a two-press key, a second '_' is involved; e.g.
      // 'cntrl-B a' has the dictionary key "C_98_97". The dictionary output value is a code for the action to be invoked.
      // All prefix keys (= first of a two-press pair) have the value "-1", indicating that a second value is awaited.
  private string SystemBkMk = "/**/";
  private Gtk.Action[] ExtraSubmenu = new Gtk.Action[MaxExtraSubmenus];
  public int ExtraClicked = -1; // If the 'Extra' menu is clicked, this is set to the ExtraSubmenu index (0 upwards). It is reset to
  public Gtk.ButtonPressEventArgs[] ButtonClicks = new Gtk.ButtonPressEventArgs[2]; // 2-deep stack of mouse clicks; [0] is the latest.
  public string[] Completions; // The range of strings available to the OnCodeCompletionItemClick handler.
  public int CompletionSelStart, CompletionSelLength; // Used by the OnCodeCompletionItemClick handler to implant into REditAss.
  public bool RecordRunTime;
  public DateTime StartTime;
  public double RunDuration;
  public List<int> PgmBreakPoints = new List<int>(); // Has to go in this class, as later classes not created till the first runtime.
    // *** Don't change this to an int[], as R.Run(.) uses the '.IndexOf' List property to search it.
  // Defaults for variable display (by key F1 with cursor on variable name), which apply till the end of this run unless altered by user.
  public string RunStringFmt = OrigStringFmt;
  public string RunDelimiter = OrigDelimiter;
  public int RunTabStep = OrigTabStep;
  public bool RunShowColumnNos = OrigShowColumnNos;
  public bool BufferHasLineNos = false; // Should be TRUE after line nos. have been inserted.
  public string HoldBufferContentsDuringLineNosDisplay = "";
  public List<string> BlockFileSave; // Only accessed if a user program is running and main menu item 'File|Save' or '..|SaveAs' is clicked.
                                     // By default, the currently running progam is on this list; sys. fn. "blocksave(.)" can undo this.
  public double[] ButtonReleaseData = new double[15]; // Filled by OnEboxAssButtonReleaseEvent(.); zeroed (size retained) by R.KillData().
                                                   // System fn. 'btnrelease' resets [0] to 0, but otherwise leaves as is.
                                                   // See the event method for description of fields.
  public int AutoColourBrackets = 0; // If menu item Search | Colour brackets... is activated, this is set: 1 for '(..)', 2 for '{..}'.
                                     // It is reset to 0 by keying ESC. 
  public List<string> BlockOfText; // Used with menu item for carrying out actions on a square block of text from the Assts. window

// ===================================================================
//   MAIN WINDOW EVENT-HANDLERS
// -------------------------------------------------------------------
  public MainWindow () : base(Gtk.WindowType.Toplevel)
  {   Build ();
    ScreenHeight = Screen.Height; ScreenWidth = Screen.Width;
    ThisWindow = this;
    SetLabelDefaultTexts("ARC");
    InitializeExtraMenus();
    for (int i = 0; i < NoRecentFileSlots; i++) RecentFiles[i] = "";
    PackParametersIntoINR(false); // All parameters must be mirrored in INR[], array of type INIRecord. (Put cursor on method name here, to
      // see description of its fields.) So we must initially fill INR[] with machine default settings of parameters. Next, we try to
      // load the INI file. If found, and where its elements parse properly, such elements will replace corresponding elements in INR[].
    Quad quack = Load_INIFile_AndFill_INR_FromItsData();
    if (!quack.B) JD.Msg("The 'INI' file could not be loaded, or if loaded, was completely unparsable.\n\nMiniMaths will still run, " +
              "but will use default values for all parameters which are normally set from the 'INI' file.");
    else
    { SetParametersFromINR(); // At least partial parsing success (quack.I = 1), if not total success (quack.I = 0).
      if (quack.I == 1) JD.Msg(quack.S); // Show the errors, but don't crash. (NB: quack.B is still TRUE for this case.)
    }
    // Reallocate initial window sizes:
    if (RelHtAss < 0.1) RelHtAss = 0.1;  else if (RelHtAss > 0.9) RelHtAss = 0.9; // Weird effects occur beyond these very adequate ranges.
    int heightAss = this.vbox12.Allocation.Height;
    int heightRes = this.vbox13.Allocation.Height;
    int newHtAss = (int) (RelHtAss * (double) (heightAss + heightRes) );
    int newHtRes = heightAss + heightRes - newHtAss;
    vbox12.HeightRequest = newHtAss; // vbox12 holds REditAss and the label BELOW it (not the label above it).
    vbox13.HeightRequest = newHtRes; // vbox13 holds REditRes and the buttons and label below it.
    UpdateReloadMenuTexts();
    Undoing = false; Redoing = false; HistoryPtr = 0;
    Navigating = false;
    DisplaySystemInitializing(); // Specific to the display system used (e.g. Gtk, Windows.Systems.Forms)
    BlockTextChangeEvents = false;
    this.Title = MainWindowTitle;
    RecordRunTime = false;
    Gtk.Key.SnooperInstall(KeySnooper);
    SetUpSpecialKeys();
    //########vvvvvvvvvvv
    KeyQueue = new int[KeyQueueLength];
    LastKeyCombo = new int[KeyQueueLength];
    //########^^^^^^^^^^^
    V.PersistentArray = new double[] {double.NaN}; // The only time this field is ever set in MonoMaths, apart from within a system fn.
    // Set up the system functions:
    F.SetUpSysFns();
    C.IsFirstUse = true; // reset in C.Conform(.), after setting up CH[] etc.
    UpdateReloadMenuTexts();
    R.Store = new List<StoreItem>();
    R.StoreUsage = new List<byte>();
    R.UserFnLog = new System.Collections.Queue();   R.UserFnLogEntryNo = 0;
    ClearREditAss();
    Boost outcome = LoadPnemonics();
            if (outcome.S != "") LblComments.Text = "Unable to load pnemonics file '" + PnemonicFName + "'.";
    NavigationStack = new List<Duo>(NavigationStackMaxSize);
    NavigationStack.Add(new Duo(0, 0)); // .X = top left cnr. of the screen; .Y = cursor position.
    NavigationPtr = 0; // the top of the stack.
    // Are there command line arguments?
    string[] commandLineParts = Environment.GetCommandLineArgs();
    if (commandLineParts.Length > 1) // It always has length at least 1, as [0] is the program name.
    { commandLineParts[0] = ""; // we don't want the program name.
      string commandLine = String.Join(" ", commandLineParts); // If there were contiguous spaces anywhere in the line, they are now
            // replaced by a single space - whether inside or outside of quotes.
      string[] Assmts = commandLine.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries );
        // Define the default values for all parameters:
      double _Left = -1, _Top = -1, _Width = -1, _Height = -1, _AssWinHt = -1;
      bool _Formatted = false;
      bool needsRepositioning = false;
      for (int i=0; i < Assmts.Length; i++)
      { string ass = Assmts[i].Trim();
        if (ass.Length < 1) continue;
        // Parameters that stand alone
        if (ass == "formatted") { _Formatted = true;  continue; }
        if (ass.IndexOf('=') == -1) // No assignment, so we will assume this is something to be loaded:
        { // If it does not contain a quote mark, give it one:
          int n = ass.IndexOfAny(C.AllowedQuoteMarks.ToCharArray());
          if (n == -1) ass = "`" + ass + '`';
          ass = "load=" + ass;
          // If no sizing parameters have been supplied, set them to a reasonable display size for text,
          //   as this facility would nearly always be used for loading text for reading.
          if (_Left == -1)
          { _Left = 0.085; _Top = 0;  _Width = 0.8;  _Height = 0.94; _AssWinHt = 650;
            needsRepositioning = true;
          }
        } // and it will be handled in what follows.


        int pEq = ass.IndexOf("="); // Guaranteed to be found
        string nm = ass._Between(-1, pEq).Trim();
        string argmt = ass._Extent(pEq+1).Trim();
        if (nm.Length < 1  ||  argmt.Length < 1) continue;
        if (nm == "load" || nm == "run")
        { bool isRun = (nm == "run");
          string _RunArgs = null;
          int pOpener = argmt.IndexOfAny(C.AllowedQuoteMarks.ToCharArray());
          if (pOpener == -1 || pOpener == argmt.Length-1) continue;
          char quoteMark = argmt[pOpener];
          int pCloser = argmt.IndexOf(quoteMark, pOpener+1);  if (pCloser == -1) continue;
          string fname = argmt._Between(pOpener, pCloser);  if (fname == "") continue;
          if (isRun)
          { // look for a subsequent section of args. for the new instance:
            int pOpener2 = argmt._IndexOf(quoteMark, pCloser+1);
            if (pOpener2 != -1)
            { int pCloser2 = argmt._IndexOf(quoteMark, pOpener2+1);
              if (pCloser2 > pOpener2 + 1) _RunArgs = argmt._Between(pOpener2, pCloser2);
            }
          }
          string[] psst = fname._ParseFileName(CurrentPath); // the arg. is the default, if no path found in fname.
          string thePath = psst[0],  theFile = psst[1];
          Boost bu = LoadProgramAndResetParams(thePath, theFile, false);
          if (bu.B)
          { if (isRun)
            { if (!String.IsNullOrEmpty(_RunArgs)) V.PersistentArray = F.StringToStoreroom(-1, _RunArgs);
              // Invoke the scalar command line assts, if any:
              if (needsRepositioning) ChangeWindowLocation(new double[] {_Left, _Top, _Width, _Height});
              if (_AssWinHt > 0) Repartition('A', Convert.ToInt32(_AssWinHt));
              if (_Formatted) WriteWindow('A', BuffAss.Text, "fill", true);
              GO(); 
            }
          }
          else // error:
          { string ss = "Failed to access command line file '" + fname + "'";
            JTV.DisplayMarkUpText(REditRes, ref ss, "append");
          }
          break; // Any further arguments are ignored after both 'load' and 'run'.
        }
        else // expect a positive scalar value:
        { double x = argmt._ParseDouble(-1.0);
          if (x < 0.0) continue;
          if      (nm == "left" || nm == "L") { _Left = x;  needsRepositioning = true; }
          else if (nm == "top" || nm == "T") { _Top = x;  needsRepositioning = true; }
          else if (nm == "width" || nm == "W") { _Width = x;  needsRepositioning = true; }
          else if (nm == "height" || nm == "H") { _Height = x;  needsRepositioning = true; }
          else if (nm == "topwindowheight" || nm == "TWH") _AssWinHt = x;
          else continue;
        }
      } // End of trawl through command line assignments
      // Invoke the scalar command line assts, if any:
      if (needsRepositioning) ChangeWindowLocation(new double[] {_Left, _Top, _Width, _Height});
      if (_AssWinHt > 0) Repartition('A', Convert.ToInt32(_AssWinHt));
      if (_Formatted) WriteWindow('A', BuffAss.Text, "fill", true);
    } // End of command line handler
  }

// Corner icon click, Main Window:
  protected void OnDeleteEvent (object sender, DeleteEventArgs a) // if a.RetVal is set to TRUE inside the method, then the window does not close.
  {
    if (CurrentRunStatus > 0)
    { a.RetVal = true; // Main window won't close; instead, hold or pause states are cancelled.
      if (CurrentRunStatus == 1)
      { StopNow = 'G'; }
      ReadjustAfterInterrupt(); return;
    }
    else if (OverwriteAfterWarning("lose") )
    { a.RetVal = false; // This allows the window to close, but leaves the application invisibly still working...
      Application.Quit(); // ...unless this step is added.
    }
    else a.RetVal = true;
  }

  /// <summary>
  /// Which label: Any or all of: 'A' for LblAss, 'R' for LblRes, 'C for LblComments. Any other value is ignored.
  /// </param>
  protected void SetLabelDefaultTexts(string WhichLabel)
  { if (WhichLabel.IndexOf('A') >= 0) LblAss.Markup = LblAssDefaultText;
    if (WhichLabel.IndexOf('R') >= 0) LblRes.Markup = LblResDefaultText;
    if (WhichLabel.IndexOf('C') >= 0) LblComments.Text = " ";
  }

// ===================================================================
//   COMPONENT EVENT-HANDLERS
// -------------------------------------------------------------------

// ==== MAIN MENU =========================== //menu

// ---- FILE MENU:                            //file
// File | New:
  protected virtual void OnNewActionActivated (object sender, System.EventArgs e)
  { if (CurrentRunStatus > 0) // Without this exit, R.KillData() below would crash MonoMaths.
    { JD.Msg("A program is running, so this menu option cannot be used. If you must clear text, do it by hand.");
      return;
    }
    if (OverwriteAfterWarning("lose"))
    { KillCurrentGraphs();
      ClearREditAss();
      R.KillData();
    }
  }
/// <summary>
/// UnsavedData should only be changed through this method, which triggers a change in the main window title if UnsavedData
/// changes its value as a result of this call.
/// </summary>
  protected void SetUnsavedData(bool toThis)
  { if (UnsavedData != toThis)
    { UnsavedData = toThis;
      string ss = this.Title;
      if (UnsavedData && ss._Extent(0, 1) != "*") this.Title = "*  " + ss; // not " && ss[0]..", as ss is empty e.g. after fn. "pgm_load(.)".
      else if (!UnsavedData && ss._Extent(0, 3) == "*  ")  this.Title = ss._Extent(3);
    }
  }

  /// <summary>
  /// If UnsavedData is FALSE, simply returns TRUE, without displaying a message box. Also if UnsavedData is TRUE but there is less
  /// than MinimumForUnsavedDataWarning chars. in BuffAss, returns TRUE (no need for warnings). Otherwise displays the message box,
  /// and returns TRUE if the user clicked the 'overwrite' button, otherwise FALSE.
  /// 'TheWordBeforeData' - the message goes: "Proceed (and ... data)?". The '...' part is replaced by this argument, AND the word
  /// (in upper case) goes on the button.
  /// </summary>
  protected bool OverwriteAfterWarning(string TheWordBeforeData)
  { if (!UnsavedData || BuffAss.Text.Length < MinimumForUnsavedDataWarning) return true;
    JD.BoxSize = new Duo(350, 200);
    int btn = JD.DecideBox("UNSAVED CHANGES", "Changes have not been saved to disk. Proceed (and " + TheWordBeforeData +
                              " data)?", TheWordBeforeData.ToUpper(), "CANCEL");
    return (btn == 1);
  }

  public void ClearREditAss()
  { BuffAss.Clear();  BuffRes.Clear();
    UseRemarksTags = true;
    UseMarkUpTags = false;
    REditAss.Editable = true;
    REditAss.ModifyText(StateType.Normal, ColourAssText);
    HoldREditAssText = "";
    HoldREditAssCursorLocnForMarkUp = 0;
    HoldREditAssTopLocnForMarkUp = -1;
    JTV.SetUpTabStops(REditAss, 20);
    SetUnsavedData(false);
    CurrentName = "";  // Leave CurrentPath as is.
    this.Title = MainWindowTitle;
    HistoryStack = null; Undoing = false; Redoing = false; HistoryPtr = 0;
    if (NavigationStack != null)
    { NavigationStack.Clear();  NavigationStack.Add(new Duo(0,0));  NavigationPtr = 0; }
    WordOccurrencePtr = new Strint2(-1, -1, "", "");
    BeforeGOPtr = new Duo(-1, -1);
  }

// File | Open
  protected virtual void OnOpenActionActivated (object sender, System.EventArgs e)
  {
    if (CurrentRunStatus > 0) // Without this exit, R.KillData() below would crash MonoMaths.
    { JD.Msg("A program is running, so this menu option cannot be used. If you must load text, use submenu 'Load Function' instead.");
      return;
    }
    if (!OverwriteAfterWarning("overwrite")) return;
    string trialPath = CurrentPath, trialName = CurrentName;
    BlockTextChangeEvents = true;
    R.KillData();
    Boost boodle = LoadFileIntoREditAss(true, ref trialPath, ref trialName, true);
    if (!boodle.B)
    { if (boodle.S != "") JD.Msg("FILE LOAD FAILURE", boodle.S);
      BlockTextChangeEvents = false;
      return;
    }
    CurrentPath = trialPath;
    CurrentName = trialName;
    // No need to call "ClearREditAss(.)" here, as it is called within "LoadFileIntoREditAss(.)" if loading is successful.
    this.Title = CurrentName + "    (folder:  " + CurrentPath + ")";
    RecolourAllAssignmentsText("remove all"); // preserve no tags
    BlockTextChangeEvents = false;
    REditAss.GrabFocus();
    JTV.PlaceCursorAt(BuffAss, 0);
  }

// File | Save  and  File | SaveAs
  protected virtual void OnSaveActionActivated (object sender, System.EventArgs e)
  { string ThePath = "", TheName = "";
    if (sender == SaveAction) { ThePath = CurrentPath;  TheName = CurrentName; }
    Boost boodle = SaveWithOptions(BuffAss.Text, ref ThePath, ref TheName, "txt");
    if (!boodle.B)
    { if (boodle.S != "") JD.Msg("FILE SAVE FAILURE", boodle.S);
      return;
    }
    CurrentPath = ThePath; CurrentName = TheName;
    this.Title = CurrentName + "    (folder:  " + CurrentPath + ")";
    SetUnsavedData(false);
    // Successful load, so...
    AdjustRecentFilesList(CurrentPath + CurrentName);
    REditAss.GrabFocus();
  }
// File | Load Function
  protected virtual void OnLoadFunctionActionActivated (object sender, System.EventArgs e)
  { string thePath = CurrentFnPath, theName = "";
    Boost boodle = LoadFileIntoREditAss(true, ref thePath, ref theName, false);
    if (!boodle.B)
    { if (boodle.S != "") JD.Msg("FILE LOAD FAILURE", boodle.S);
      return;
    }
    CurrentFnPath = thePath;
    SetUnsavedData(true);
    RecolourAllAssignmentsText("remove all"); // remove all tags
    REditAss.GrabFocus();
  }
// File | Save Function
  protected virtual void OnSaveFunctionActionActivated (object sender, System.EventArgs e)
  { string text = JTV.ReadSelectedText(BuffAss);
    if (text == "")
    { JD.Msg("To use this menu item, you must have some text selected (as it is only the selected text that will be saved to disk).");
      return;
    }
    string thePath = CurrentFnPath, theName = "";
    Boost boo = SaveWithOptions(text, ref thePath, ref theName, "txt");
    if (!boo.B)
    { if (boo.S != "") JD.Msg("FILE SAVE FAILURE", boo.S);
      return;
    }
    CurrentFnPath = thePath;
  }
  protected virtual void OnSaveArrayOrSystemListActionActivated (object sender, System.EventArgs e)
  { if (V.Vars == null || V.Vars[0].Count == 0)
    { JD.Msg("No variable data. (Run your program first.)"); return; }
    string header = "SAVE VARIABLE OR SYSTEM LIST";
    string preamble =
      "SAVE TO DISK:\n*  A main program variable; or\n*  A function variable (if program interrupted in the function); or\n" +
      "*  A system list (given its numerical index).\n\n" +
      "NB: Any preexisting file with the same name will be overwritten without warning.\n\n" +
      "Data will be saved in prefixed binary format. You can later retrieve the data with this instruction:\n" +
      "     <span foreground=\"#0000FF\">load('A', \"~/.../myDataName\", true);</span>";
    string[] prompts = new string[] { "Name of VARIABLE, or No. of LIST:", "File Path and Name:" };
    string datapath = CurrentDataPath;  if (datapath[datapath.Length-1] != '/') datapath += '/';
    string[] boxtexts = new string[] { "", datapath, "" };
    string[] buttontitles = new string[] { "BROWSE", "SAVE", "CANCEL" };
    string varName = "",  filePathName = "";
    while (true)
    { JD.PlaceBox(700, 300, 0.5, 0.5);
      int btn = JD.InputBox(header, preamble, prompts, ref boxtexts, buttontitles);
      if (btn == 0 || btn == 3) return; // Cancelled by button or by corner icon.
      varName = boxtexts[0].Trim();  filePathName = boxtexts[1].Trim();
      if (btn == 1) // BROWSE:
      { Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog("Choose or supply file name for saving", null,
             FileChooserAction.Save, "Cancel",ResponseType.Cancel, "Save",ResponseType.Accept);
        fc.SetCurrentFolder(CurrentDataPath);
        int outcome = fc.Run();
        if (outcome == (int) ResponseType.Accept) filePathName = fc.Filename.Trim();
        fc.Destroy(); // Without this, the FileChooserDialog window won't get closed.
        if (outcome != (int) ResponseType.Accept || filePathName == "") return; // latter: user cancelled using empty text
        boxtexts[1] = filePathName;
      }
      else if (varName == "" || filePathName == "") JD.Msg("Not all fields have been filled in.");
      else if (filePathName[filePathName.Length-1] == '/') JD.Msg("There is no file name after the path.");
      else break;
    }
    int At;
    if (JS.CharsNotInList(varName, JS.DigitList) == 0) // then a nonneg. integer, so should be a list ref.:
    { int lno = varName._ParseInt(Int32.MaxValue);
      if (F.Sys == null || lno >= F.NoSysLists){ JD.Msg("There is no list no. " + varName); return; }
      string listname = "List__" + varName; // not checked, as must be a conformative variable name.
      At = V.AddUnassignedArray(0, listname);
      if (At == -1) At = V.FindVar(0, listname); // User must have already saved it once, as -1 from above = duplication.
      int slot = V.GenerateTempStoreRoom(F.Sys[lno].LIST.Count);
      F.Sys[lno].LIST.CopyTo(R.Store[slot].Data);
      V.TransferTempArray(slot, 0, At);
    }
    else At = V.FindVar(0, varName); // a regular variable, not a list:
    if (At == -1) { JD.Msg("Variable '" + varName + "' not found."); return; }
    short varuse = V.GetVarUse(0, At);
    if (varuse < 11) { JD.Msg("Scalars cannot be saved using this menu item."); return; }
    int Slot = (int) V.GetVarPtr(0, At);
    Boost qq = F.SaveVariable(Slot, 0.0, filePathName, 'O', "identified data", varName); // 'true' = save as formatted text
    if (!qq.B) JD.Msg(qq.S);
    else if (filePathName.IndexOf('/') > 0) // successful save, and a path is included in the file name, so adjust CurrentDataPath:
    { FileInfo gen = new FileInfo(filePathName); // no checks, as save was successful.
      CurrentDataPath = gen.DirectoryName;  CurrentDataPath = CurrentDataPath.Replace('\\', '/');
    }
  }

  protected virtual void OnSaveSettingsForFutureRunsActionActivated (object sender, System.EventArgs e)
  { PackParametersIntoINR(false); string ss = ProgramPath + INIFileName;
    SaveINISettings(ss);
    // No check of bool return; fn. would have shown any error msg.
    JD.Msg("INI file saved as '" + ss + "'");
  }

  protected virtual void OnNewMonoMathsInstanceActionActivated (object sender, System.EventArgs e)
  { Quid quid = JS.RunProcess(ProgramPath + "MonoMaths.exe", "", 0);
    if (!quid.B) JD.Msg(quid.S);
  }

  protected virtual void OnReloadPnemonicsActionActivated (object sender, System.EventArgs e)
  { if (CurrentRunStatus > 0) // Without this exit, R.KillData() below would crash MonoMaths.
    { JD.Msg("A program is running, so this menu option cannot be used.");
      return;
    }
    Boost outcome = LoadPnemonics();
    if (outcome.S != "") LblComments.Text = "Unable to load pnemonics file '" + PnemonicFName + "'.";
  }

// Menu closure (File | Exit):
  protected virtual void OnExitActionActivated (object sender, System.EventArgs e)
  { if (!OverwriteAfterWarning("lose")) return;
    Application.Quit ();
  }

/// <summary>
/// Currently called (indirectly) by system function 'kill_on_exit()', and maybe by other functions in the future. Note that there
/// is no use of 'OverwriteAfterWarning(.)' as in the above OnExit method. This is because present and future functions calling this
///  won't want to be interrupted, as they would typically be part of an automatized chain of events involving multiple instances of MonoMaths.
/// </summary>
  public void CloseThisWindow()
  { CurrentRunStatus = 0; // *** I'm not sure if this is necessary. Probably not. (It carries across from the preexisting Windows version.)
    Application.Quit();
  }

// ----------------------------------------------------------

// ---- RELOAD MENU:                            //reload
  protected virtual void OnReloadActionActivated (object sender, System.EventArgs e)
  { if (CurrentRunStatus > 0) // Without this exit, R.KillData() below would crash MonoMaths.
    { JD.Msg("A program is running, so this menu option cannot be used.");
      return;
    }
    if (!OverwriteAfterWarning("overwrite")) return;
    Gtk.Action bonk = sender as Gtk.Action;
    string ss = bonk.Name;
    int p = ss.IndexOf('n');  if (p == -1) return; // Should never happen (unless names of menus changed).
    string tt = ss._Extent(p+1);
    int n = tt._ParseInt(-1); if (n == -1) return; // Should never happen (unless names of menus changed).
    string[] stuff = Filing.Parse(RecentFiles[n]);   if (stuff == null) return; // RecentFiles[n] is the empty string.
    string trialPath = stuff[0], trialName = stuff[1];
    R.KillData();
    // No need to call "ClearREditAss(.)" here, as it is called within "LoadFileIntoREditAss(.)" if loading is successful.
    Boost boodle = LoadFileIntoREditAss(false, ref trialPath, ref trialName, true);
    if (!boodle.B)
    { if (boodle.S != "") JD.Msg("FILE LOAD FAILURE", boodle.S);
      return;
    }
    CurrentPath = trialPath;
    CurrentName = trialName;
    HistoryStack = null;  Undoing = false;  Redoing = false;  HistoryPtr = 0;
    NavigationStack.Clear();  NavigationStack.Add(new Duo(0,0));  NavigationPtr = 0;
    this.Title = CurrentName + "    (folder:  " + CurrentPath + ")";
    SetUnsavedData(false);
    RecolourAllAssignmentsText("remove all"); // remove all tags
    REditAss.GrabFocus();
    JTV.PlaceCursorAt(BuffAss, 0);
  }
// Used when loading command line files.
// returned: The usual error-indicating values.
  public Boost LoadProgramAndResetParams(string ThePath, string TheName, bool ForceDialog)
  { Boost result = new Boost(false);
    BlockTextChangeEvents = true;
    R.KillData();
    Boost boodle = LoadFileIntoREditAss(ForceDialog, ref ThePath, ref TheName, true);
    if (!boodle.B)
    { if (boodle.S != "") { result.S = "Failed to load file '" + ThePath + TheName + "';  " + boodle.S; }
      BlockTextChangeEvents = false;
      return result;
    }
    CurrentPath = ThePath;
    CurrentName = TheName;
    HistoryStack = null;  Undoing = false;  Redoing = false;  HistoryPtr = 0;
    NavigationStack.Clear();  NavigationStack.Add(new Duo(0,0));  NavigationPtr = 0;
    this.Title = CurrentName + "    (folder:  " + CurrentPath + ")";
    SetUnsavedData(false);
    RecolourAllAssignmentsText("remove all"); // remove all tags
    BlockTextChangeEvents = false;
    REditAss.GrabFocus();
    JTV.PlaceCursorAt(BuffAss, 0);
    result.B = true;
    return result;
  }

//--------------------------------------------------------
//  EDIT MENU                                      //edit

  protected virtual void OnUndoActionActivated (object sender, System.EventArgs e)
  { Undo();

  }
  protected virtual void OnRedoActionActivated (object sender, System.EventArgs e)
  { Redo();
  }

  protected virtual void OnCopyActionActivated (object sender, System.EventArgs e)
  { TextBuffer buff;
    if (WhichTextViewHasFocus(out buff) > 0) JTV.CopyToClipboard(buff, false);
  }

  protected virtual void OnCutActionActivated (object sender, System.EventArgs e)
  { TextBuffer buff;
    if (WhichTextViewHasFocus(out buff) > 0) JTV.CopyToClipboard(buff, true);
  }

  protected virtual void OnPasteActionActivated (object sender, System.EventArgs e)
  { TextBuffer buff;
    if (WhichTextViewHasFocus(out buff) > 0) JTV.PasteFromClipboard(buff);
  }
  
  protected void OnBlockOfTextActionsAction1Activated (object sender, EventArgs e)
  { if (BlockOfText == null) BlockOfText = new List<string>();
    TextIter selstart, selend;
    string hdr = "BLOCK OF TEXT", bodytext = "";
    string[] buttons = null;
    int choice;
    bool isSelection = BuffAss.GetSelectionBounds(out selstart, out selend);
    if (isSelection)
    { int startLine = selstart.Line,  endLine = selend.Line;
      int lowCharPosn = selstart.LineOffset,  highCharPosn = selend.LineOffset-1; // -1 because we don't want the char. after end of selection.
      int blockWidth = highCharPosn - lowCharPosn + 1;
      int blockHeight = endLine - startLine;
      if (blockWidth < 1 || blockHeight < 1)
      { JD.Msg("To use this facility, the selection must cross at least one end-of-paragraph marker, " +
                " and the selection end must be more characters into a line than is the selection end.");
        return;
      } 
      bodytext = String.Format("Your block of text goes from\n\n   LINE {0}  to  LINE {1}\n    CHAR {2}  to  CHAR {3}\n\n\n" + 
        "Before you make your selection, consider these two steps for proper vertical alignment of text:\n\n" + 
        "1. Convert text to a fixed-letter-width font\n" +
        "     using menu item 'Appearance | Toggle proportional font'.\n\n" +
        "2. Then temporarily replace tabs by a character pair\n     (e.g. '\\t' --> '££' in the 'Replace' dialog).\n",
             startLine+1, endLine+1, lowCharPosn+1, highCharPosn+1);
      buttons = new [] {"COPY", "CUT", "CANCEL"};
      choice = JD.DecideBox(hdr, bodytext, buttons);
      if (choice == 1 || choice == 2)
      { string ss = (choice == 1) ? "C" : "CX";
        string errmsg = MassageText(ss, BuffAss, selstart, selend, ref BlockOfText);
        if (errmsg != String.Empty) { JD.Msg("Block text action failed: " + errmsg);  return;  }
      }
    }
    else // No selection:
    { 
      if (BlockOfText.Count == 0)
      { JD.Msg("There is no block of text currently in memory, so there is nothing to paste here.");
        return;
      }
      int longestLength = 0;
      foreach (string stroo in BlockOfText) 
      { if (stroo.Length > longestLength) longestLength = stroo.Length;
      }
      bodytext = String.Format("The stored block of text has {0} LINES and a width of {1} characters.\n\n", BlockOfText.Count, longestLength); 

      bodytext += "Buttons --\n  INSERT: Lines inserted, existing text displaced to the right.\n" + 
                  "  OVERWRITE: Lines inserted, overwriting existing text.\n" + 
                  "  CANCEL:  No insertion, but the lines remain in memory.\n  CLEAR MEM:  No insertion, and memory is cleared.";
      buttons = new string[] {"INSERT", "OVERWRITE", "CANCEL", "CLEAR MEM"};
      choice = JD.DecideBox(hdr, bodytext, buttons);
      if (choice >= 3)
      { if (choice == 4) BlockOfText.Clear();
        return;
      }
      // Strings will be inserted.
      string Opn = (choice == 1) ? "I" : "O";
      string errmsg = MassageText(Opn, BuffAss, selstart, selend, ref BlockOfText);
      if (errmsg != String.Empty) { JD.Msg("Block text action failed: " + errmsg);  return;  }
    }
    return;
  }

// UTILITY METHOD FOR THE ABOVE EVENT HANDLER:
  /// <summary>
  /// <para>'DoWhat': Any mix of these chars.: "C"(copy); "X"(erase); "I"(insert); "O"(overwrite). Operations, if multiple,
  ///   will be done in this order rather than the order in which they appear in 'DoWhat'. NB: Don't combine 'O' and 'X', or
  ///   who knows what will happen (they are intended for different scenarios).</para>
  /// <para>'Block' - points to BlockOfText, which can be empty but not null, or a crash will occur. For operation 'C' it will be
  ///   given data; otherwise it will be left as is.</para>
  /// <para>RETURNED: If no errors, the empty string. Otherwise an error message.</para>
  /// </summary>
  public string MassageText(string DoWhat, TextBuffer Buff, TextIter SelStart, TextIter SelEnd, ref List<string>Block) 
  { bool doCopy = (DoWhat.IndexOf('C') != -1),  doErase = (DoWhat.IndexOf('X') != -1),   
         doInsert = (DoWhat.IndexOf('I') != -1),   doOverwrite = (DoWhat.IndexOf('O') != -1);
    // Delimit the text to be read and/or altered:
    TextIter startIt = SelStart, endIt;
    int noLinesToRead = 0, blockWidth = 0;
    startIt.BackwardChars(startIt.LineOffset); // Go back to the start of the line
    if (SelEnd.Equal(SelStart)) // then there is no selection, so copy according to the length of Block:
    { if (Block.Count == 0) return "cannot insert text here because there is no stored text to insert";
      noLinesToRead = BlockOfText.Count;
      endIt = SelStart;
      endIt.ForwardLines(noLinesToRead); // to point to the beginning of the next line after the passage we want.
      // set blockWidth according to the longest line in Block:
      for (int i = 0; i < noLinesToRead; i++)
      { if (Block[i].Length > blockWidth) blockWidth = Block[i].Length;
      }
    }
    else // there is a selection:
    { endIt = SelEnd;
      endIt.ForwardLine(); // Now points to the beginning of the line after the selection end.
      noLinesToRead = endIt.Line - startIt.Line;
      if (noLinesToRead <= 1) return "the selection must cross at least one end-of-line marker";
      blockWidth = SelEnd.LineOffset - SelStart.LineOffset;
      if (blockWidth < 1) return "the selection end must be later in a line than the selection start";
    }
    // Either way, get the existing text and convert it to a string array
    string ss, oldtxt = BuffAss.GetText(startIt, endIt, true);
    int insertPoint = SelStart.LineOffset;
    string[] OldText = oldtxt.Split(new char[] {'\n'}, StringSplitOptions.None);
    int oldTextLen = OldText.Length; // If inserting at the end of the file, notionally empty lines must be added at its end.
    // If inserting, there may be more lines in Block than lines in OldText; so extend OldText accordingly:
    if ( (doInsert || doOverwrite)  &&  oldTextLen < Block.Count)
    { List<string>oldie = new List<string>();  oldie.AddRange(OldText);
      for (int i = oldTextLen; i < Block.Count; i++) oldie.Add(String.Empty);
      OldText = oldie.ToArray();
    } 
    // GO THROUGH THE EXTRACTED TEXT:
    for (int i = 0; i < noLinesToRead; i++)
    { ss = OldText[i];
      // Copy:
      if (doCopy)
      { if (i == 0) Block.Clear();
        Block.Add(ss._Extent(insertPoint, blockWidth));
      } 
      // Erase:
      if (doErase)
      { OldText[i] = ss._Extent(0, insertPoint) + ss._Extent(insertPoint + blockWidth);
      }          
      // Insert:
      if (doInsert)
      { OldText[i] = ss._Extent(0, insertPoint) + Block[i] + ss._Extent(insertPoint);
      }
      // Insert:
      if (doOverwrite)
      { OldText[i] = ss._Extent(0, insertPoint) + Block[i] + ss._Extent(insertPoint + blockWidth);
      }
    }
    // REPLACE EXISTING TEXT WITH NEW TEXT (except for operation COPY alone)
    if (doErase || doInsert || doOverwrite)
    { ss = String.Join("\n", OldText);
      Buff.SelectRange(startIt, endIt);
      Buff.Delete(ref startIt, ref endIt);
      BuffAss.Insert(ref startIt, ss);
    }
    return String.Empty;
  }

  // Detects the last word (up to length 80 chars.) preceding the cursor, looking back to the first non-identifier character. Then
  // looks for that word in as many words as have so far been recorded; that is, looks in system keywords and function names
  // (recorded at MM startup), then if 'GO' has been pressed, user fn. names, variable names in any functions. NB - only checks
  // MM lists and arrays, does not search REditAss text.
  // If no find, simply nothing happens. If one find, the word is completed. If more than one find, no text is added, but a context menu
  // appears with options, and if one is clicked, that word replaces the text.
  // If the cursor is in the middle of a text word, the completion word replaces the whole text word.
  protected virtual void OnCodeCompletionActionActivated (object sender, System.EventArgs e)
  {
  // EXTRACT THE TEXT WORD, if possible:
    // Look for the last instance of a non-identifier char. to the left of the cursor:
    int nn, cursor = BuffAss.CursorPosition,  n = cursor - 81; // If n < 0, method ._FromTo(.) below behaves as if n were 0.
    string tt, ss = BuffAss.Text._FromTo(n, cursor-1);   if (ss == "") return; // No text, so no dice.
    tt = ss._Reverse();
    n = tt._IndexOfNoneOf(P.IdentifierChars);  if (n == -1) n = tt.Length; // e.g. if this is the first letter of text
    // Extract the word:
    string word = ss._Extent(ss.Length - n);  if (word == "") return; // There was a space to the left of the cursor.
    int wordLen = word.Length, wordStart = cursor - n;
   // SET UP STRUCTURES required in equipping the PopUp menu:
    List<string> offerings = new List<string>(); // 'offerings' will be the offered replacement words.
    List<Strint> submenus = new List<Strint>(); // .S is the text displayed, .I is the index of its colour in 'colours' below.
    string[] colours = new string[] { "red", "black", "green", "blue", "CornflowerBlue" };
    int clrKeyword = 0, clrSysFn = 1, clrUserFn = 2, clrVarMain = 3, clrVarFn = 4; // indexes for colour names in the above string[].

  // THE HUNT BEGINS...

   // KEYWORDS search:
    Quad quoo;
    for (int i = C.CHand; i < C.CHkwds; i++)
    { quoo = JS.EqualHowFar(word, C.CHname[i], false); // 'false' = check is NOT case-sensitive.
      if (quoo.I >= wordLen-1) // then the two words match right up to the end of 'word':
      { offerings.Add(C.CHname[i]);  submenus.Add(new Strint(clrKeyword, C.CHname[i] + "    (Keyword)")); }
    }
   // SYSTEM FUNCTIONS search:
    n = F.SysFnCnt;
    for (int i = 1; i < n; i++)
    { quoo = JS.EqualHowFar(word, F.SysFn[i].Name, false); // 'false' = check is NOT case-sensitive.
      if (quoo.I >= wordLen-1) // then the two words match right up to the end of 'word':
      { offerings.Add(F.SysFn[i].Name);  submenus.Add(new Strint(clrSysFn, F.SysFn[i].Name + "    (System Function)")); }
    }
   // ASSIGNED VARIABLE NAMES search:  (if there has been a prior run)
    if (V.Vars != null)
    { for (int fn=0; fn < V.Vars.Length; fn++)
      { List<TVar> variety = V.Vars[fn];
        if (fn == 0) tt = "    (in Main Program)";   else tt = "    (in " + P.UserFn[fn].Name + ")";
        n = variety.Count;
        for (int v=0; v < n; v++)
        {
          quoo = JS.EqualHowFar(word, variety[v].Name, false); // 'false' = check is NOT case-sensi39tive.
          if (quoo.I >= wordLen-1) // then the two words match right up to the end of 'word':
          { if (fn == 0) nn = clrVarMain;  else nn = clrVarFn;
            ss = variety[v].Name;
            if (ss.IndexOf("__") == -1) // exclude system variables with "__" somewhere in the name
            { offerings.Add(ss);  
              submenus.Add(new Strint(nn, ss + tt));
            }
          }
        }
      }
    }
    else // V.Vars doesn't yet exist. Check for named constants (omitting duplicated names, like 'NaN', 'true':
    { string[] namedConstants = new string[] { "MAXREAL", "MINREAL", "MAXINT32", "MININT32", "MAXINT64", "MININT64",
                              "POSINF", "NEGINF", "NAN", "PI", "EE", "TRUE", "FALSE" };
      for (int v=0; v < namedConstants.Length; v++)
      { quoo = JS.EqualHowFar(word, namedConstants[v], false); // 'false' = check is NOT case-sensitive.
        if (quoo.I >= wordLen-1) // then the two words match right up to the end of 'word':
        { offerings.Add(namedConstants[v]);
          submenus.Add(new Strint(clrVarMain, namedConstants[v] + "    (in Main Program)"));
        }
      }
    }
   // USER FUNCTIONS search:
    if (P.UserFn != null)
    { for (int i=1; i < P.UserFn.Length; i++)
      { quoo = JS.EqualHowFar(word, P.UserFn[i].Name, false); // 'false' = check is NOT case-sensitive.
        if (quoo.I >= wordLen-1) // then the two words match right up to the end of 'word':
        { ss = P.UserFn[i].Name;   offerings.Add(ss);  submenus.Add(new Strint(clrUserFn, ss + "    (User Function)"));  }
      }
    }
   // POTENTIAL VARIABLE NAMES and USER FUNCTION NAMES search:  (where no run yet, or none since new variables added)
    string theText = BuffAss.Text;
    theText = theText.Insert(wordStart, JS.FirstPrivateChar.ToString()); // bookmark
    ss = theText.Replace(JS.CRLF, "\n"); // in case text pasted in from a MS Windows source
    if (ss.Length > 20 || ss._IndexOfAny(P.IdentifierChars).X >= 0) // i.e. nonempty buffer:
    {
      string[] TheLines = ss.Split(new char[]{'\n'}, StringSplitOptions.None); // A class variable
      C.ProcessDirectiveALLOW(ref TheLines, true); // If an 'ALLOW' line found before other code, it is serviced
                  //  to add its contained characters to P.IdentifierChars & ...1stChar. (The ALLOW line will be removed from TheLines.)
      string AllText = String.Join("\n", TheLines);
      Quad quail = C.ReplaceQuotes(ref AllText);
      if (!quail.B) AllText = String.Join("\n", TheLines); // I.e. if any quote marks are not paired, don't raise an error;
                    // but in that case ignore all quote marks, so that words inside an intended quotation will go into the word list.
      string identifierChars = P.IdentifierChars;
      StringBuilder sb = new StringBuilder(AllText);
      int len = sb.Length;
      int startPtr = 0, endPtr = 0;
      bool insideWord = false;
      for (int ptr = 0; ptr <= len; ptr++)
      { if (insideWord)
        { if (ptr == len || identifierChars.IndexOf(sb[ptr]) == -1)
          { endPtr = ptr-1;  insideWord = false;
            // Extract the word - now complete - and add to MayBeIdentifiers, if not a duplicate:
            tt = sb.ToString(startPtr, endPtr - startPtr + 1);
            if (sb[startPtr] > '9'  &&  offerings.IndexOf(tt) == -1) // i.e. doesn't start with a numeral, and not duplicated
            { quoo = JS.EqualHowFar(word, tt, false); // 'false' = check is NOT case-sensitive.
              if (quoo.I >= wordLen-1) // then the two words match right up to the end of 'word':
              { if (startPtr == 0 || sb[startPtr-1] != JS.FirstPrivateChar)
                { offerings.Add(tt);  submenus.Add(new Strint(clrVarFn, "[" + tt + "]   (possible variable name)")); }
              }
            }
          }
          // else do nothing.
        }
        else // not inside a word:
        { if (ptr < len && identifierChars.IndexOf(sb[ptr]) >= 0)
          { startPtr = ptr;  insideWord = true; }
        }
      }
    }
   //---------------------------------------------------------
   // PREPARE THE BUFFER FOR REPLACEMENT
    int noFinds = offerings.Count;   if (noFinds == 0) return; // No matching text.
    // Work out the length of what has to be replaced, including chars. to the right of the cursor:
    ss = BuffAss.Text._FromTo(cursor, cursor + 80);
    n = ss._IndexOfNoneOf(P.IdentifierChars);  if (n == -1) n = ss.Length;
    int fullwordLen = wordLen + n; // i.e. no contig. ident. chars. to left of cursor + ditto to right of cursor
    // So the stretch to be replaced begins at wordptr (set near the method's start) and extends for fullwordLen chars.

    // Check if all entries are the same, or if there is only one:
    bool no_differences = true;
    for (int i = 1; i < noFinds; i++)
    { if (offerings[i] != offerings[0]) { no_differences = false;  break;  } }
    // If so, don't bother with a PopUp menu; just replace it...
    if (no_differences)
    { JTV.ReplaceTextAt(BuffAss, cursor - wordLen, fullwordLen, offerings[0]);
      return;
    }
  // INVOKE THE POPUP MENU, if more than one find.
    // The following 3 class fields will be used by the OnCodeCompletionItemClick handler to implant the choice.
    Completions = offerings.ToArray();
    CompletionSelStart = cursor - wordLen;
    CompletionSelLength = fullwordLen;
    PopUpMenu("completion", submenus.ToArray(), colours, OnCodeCompletionItemClick);

  }
  // Context menu handler:
  private void OnCodeCompletionItemClick (object o, EventArgs args)
  { string ss = (o as MenuItem).Name;
    int n = ss.IndexOfAny(JS.DigitList);  if (n == -1) return;
    int ndx = ss._Extent(n)._ParseInt(-1);  if (ndx < 0 || ndx >= Completions.Length) return; // latter if clicked on warning to run GO first.
    JTV.ReplaceTextAt(BuffAss, CompletionSelStart, CompletionSelLength, Completions[ndx]);
  }


  protected void OnSpecialCharactersActionActivated (object sender, System.EventArgs e)
  {
    string bodyText = "To <b>insert special characters into your text</b> you can do one of the following:\n";
    bodyText += "<bullet>Copy and paste from the list below, or from some more complete character map;\n";
    bodyText += "<bullet 50,➯>(If using your own map, avoid chars. with a unicode more than 2 bytes long, or you will have problems.)\n";
    bodyText += "<bullet>Select character(s) from the list below, and then click 'ACCEPT'; the selected text will automatically\n";
    bodyText += "         be pasted into the Assignments Window at the cursor.\n";
    bodyText += "<bullet>Use a hot key combination which automatically substitutes for the character to the left of the cursor\n";
    bodyText += "         in your text (you don't need this display for that, except for reference).\n\n";
    bodyText += "The <b>hot key combinations</b> are:\n";
    bodyText += "<bullet 50,➯><# red>Ctrl+Shift+G<# black>, to turn a <# darkslategrey>grey <# black>character into the <# red>red <# black>character above it;\n";
    bodyText += "<bullet 50,➯><# blue>Ctrl+Shift+H<# black>, to turn a <# darkslategrey>grey <# black>character into the <# blue>blue <# black>character above it;\n";
    bodyText += "<bullet 50,➯><# purple>Ctrl+Shift+J<# black>, to turn a <# darkslategrey>grey <# black>character into the <# purple>purple <# black>character above it;\n\n";
    bodyText += "<font DejaVu Sans Mono, Courier_New, 12>";
    bodyText += "<# red >      Α Β Γ Δ Ε Ζ Η Θ Ι Κ Λ Μ Ν Ξ Ο Π Ρ Σ Τ Υ Φ Χ Ψ Ω  α β γ δ ε ζ η θ ι κ λ μ ν ξ ο π ρ σ ς τ υ φ χ ψ ω\n";
    bodyText += "<# grey>      A B G D E Z # 9 I K L M N X O P R S T U F H Y W  a b g d e z @ 0 i k l m n x o p r s c t u f h y w\n\n";
    bodyText += "<# blue>      ∫ ∮ ∑ √ ± ° × ‹ › ≪ ≫ ‒ ‖ ┃ ℜ ℑ ℂ ℤ ℱ ⨍ ∂ ∇ ∞ ∡ ≈ ≠ ≡ ≤ ≥ ← → ↔ ↑ ↓ ¹ ² ³ ⁴ ⁵ ⁶ ⁷ ⁸ ⁹ ⁰ ⁺ ⁻\n";
    bodyText += "<# grey>      i j S / + o x ( ) {  } - \"  | R I C Z F f d D @ A ~ # = < > [ ] _ ^ v 1 2 3 4 5 6 7 8 9 0 p m\n\n";
    bodyText += "<# purple>      ₁ ₂ ₃ ₄ ₅ ₆ ₇ ₈ ₉ ₀ ₊ ₋ ‾ « »\n";
    bodyText +=    "<# grey>      1 2 3 4 5 6 7 8 9 0 p m o { }\n\n\n\n";
    bodyText += "</font><# Black>Some characters to copy, which do not currently have a hot key assignment:\n\n";
    bodyText += "<# black>     ÷ ⁄ ‡ • ‧ … ℛ ℵ ₊ ₋ ↕ ↚ ↛ ⇒ ⇐ ∈ ∡ ∬ ∭ ∮ ∯ ∰ ⨍ ⋃ ⋂ ✔ ✘ ¼ ½ ¾ ⅓ ⅔ à â è ê é ì ò ô ù û ç ä ë ï ö ü ∝\n";
 
    JD.PlaceBox(1100, 625, "last", "last"); // 'last' = future instances of the dialog are placed wherever the user left this instance.
    int btn = JD.Display("CHARACTERS NOT ON THE KEYBOARD", bodyText, true, true, false, "ACCEPT", "CANCEL");
    if (btn == 1  &&  JD.SelectedText != "")
    { BuffAss.InsertAtCursor(JD.SelectedText); }
  }
  private static string GreekLetters  = "αβγδεζηθικλμνξοπρσςτυφχψωΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ";
  private static string GreekLtrCodes = "abgdez@0iklmnxoprsctufhywABGDEZ#9IKLMNXOPRSTUFHYW";
  private static string MathChars     = "∫∮∑√±°×‹›≪≫‒‖┃ℜℑℂℤℱƒ∂∇∞∡≈≠≡≤≥←→↔↑↓¹²³⁴⁵⁶⁷⁸⁹⁰⁺⁻";
  private static string MathCharCodes = "ijS/+ox(){}-\"|RICZFfdD@A~#=<>[]_^v1234567890pm";
  private static string MoreChars     = "₁₂₃₄₅₆₇₈₉₀₊₋‾«»";
  private static string MoreCharCodes = "1234567890pmo{}";


  // Does char. replacements based on the above method ("OnSpecialCharactersActionActivated")
  private void ChangeCharToLeft(int WhichTable)
  {
    int cursorAt = BuffAss.CursorPosition;   if (cursorAt < 1) return;
//    char ch = BuffAss.Text[cursorAt-1];
    char replacer = '\u0000';
    char ch = (GetCharsToLeftOfCursor(1))[0];
    if (WhichTable == 0) // Greek
    { int n = GreekLtrCodes.IndexOf(ch);
      if (n >= 0) replacer = GreekLetters[n];
    }
    else if (WhichTable == 1) // Maths
    { int n = MathCharCodes.IndexOf(ch);
      if (n >= 0) replacer = MathChars[n];
    }
    else // WhichTable = 2, for 'more chars':
    { int n = MoreCharCodes.IndexOf(ch);
      if (n >= 0) replacer = MoreChars[n];
    }
    if (replacer != '\u0000')
    { JTV.SelectText(BuffAss, cursorAt-1, 1);
      BuffAss.DeleteSelection(true, true);
      BuffAss.InsertAtCursor(replacer.ToString());
    }
  }
// Returns the empty string if no char. to left of cursor.
// You can't just read BuffAss.Text[], as it is a BYTE array, and will be out of sync with cursor position
//  if any two-byte UTF8 chars in the foregoing text
  public string GetCharsToLeftOfCursor(int noChars)
  { int cursorOffset = BuffAss.CursorPosition;
    if (cursorOffset == 0 || noChars < 1) return "";
    TextIter thisPosn = BuffAss.StartIter;
    thisPosn.ForwardChars(cursorOffset);
    TextIter lastPosn = thisPosn;
    lastPosn.BackwardChars(noChars); // Will be set to text start if the cursor is less than noChars from the start.
    string result = BuffAss.GetText(lastPosn, thisPosn, true); // 'true' = return hidden characters
    return result;
  }


  protected void OnConformIndentsActionActivated (object sender, System.EventArgs e)
  {
    if (BuffAss.Text.Length == 0) return;
    string header = "CONFORM THE LINE INDENTS";
    string preamble =
      "Line indents in the current file can be forced to be all spaces or all tabs, according to your choices here.\n\n" +
      "Points to note:\n"+
      "   1.  If, say, one tab is to replace two spaces, and the indent has an odd number of spaces, the leftover final space will remain.\n" +
      "   2.  Your choices only affect the current displayed text; they do not influence how future indenting will happen.";
    string[] prompts = new string[] { "Indent with TABS (enter 'T') or spaces (enter 'S'):", "Spaces per tab:",
                                      "Remove all spaces and tabs at ENDS of lines ('Y' or 'N'):" };
    string[] boxtexts = new string[] { "T", "2", "Y" };
    string[] buttontitles = new string[] { "ACCEPT", "CANCEL" };
    string which_indent = "", trim_line_end = "";
    int spaces_per_tab = -1;
    char[] space_tab = new char[] { ' ', '\t' }, eol = new char[] { '\n' };
    while (true)
    { JD.PlaceBox(700, 300, 0.5, 0.5);
      int btn = JD.InputBox(header, preamble, prompts, ref boxtexts, buttontitles);
      if (btn != 1) return; // Cancelled by button or by corner icon.
      for (int i=0; i < 3; i++)
      { boxtexts[i] = (boxtexts[i].Trim() + " ").ToUpper(); } // the added space ensures that boxtexts[i] is never the empty string
      which_indent = boxtexts[0].Substring(0,1);  trim_line_end = boxtexts[2].Substring(0,1);
      spaces_per_tab = boxtexts[1]._ParseInt(-1);
      string errmsg = "";
      if (which_indent != "T" && which_indent != "S") errmsg = "The first box should contain just one of the four letters: 'T', 't', 'S' or 's'.\n\n";
      if (spaces_per_tab < 1 || spaces_per_tab > 20) errmsg += "The second box should contain an integer between 1 and 20.\n\n";
      if (trim_line_end != "Y" && trim_line_end != "N") errmsg += "The last box should contain just one of the four letters: 'Y', 'y', 'N' or 'n'.";
      if (errmsg != "") JD.Msg(errmsg);
      else // satisfactory values:
      { bool isTabIndent = (which_indent == "T"),  doLopSpaces = (trim_line_end == "Y");
        string spacesuit = (' ')._Chain(spaces_per_tab);
        string[] Lines = BuffAss.Text.Split(eol, StringSplitOptions.RemoveEmptyEntries);
        int len = Lines.Length;   string ss;
        for (int j=0; j < len; j++)
        { if (doLopSpaces) ss = Lines[j].TrimEnd(space_tab);  else ss = Lines[j];
          int p = ss._IndexOfNoneOf("\t ");
          if (p > 0)
          { string tt = ss._Extent(0, p); // the whole indent
            if (isTabIndent) tt = tt.Replace(spacesuit, "\t");
            else tt = tt.Replace("\t", spacesuit);
            ss = tt + ss._Extent(p);
          }
          Lines[j] = ss;
        }
        BuffAss.Text = String.Join("\n", Lines);
        break;
      }
      // Replace displayed text with the doctored text:
    }
  }

  protected void OnChangeLetterCaseActionActivated(object sender, EventArgs e)
  { TextIter selPtStart, selPtEnd;
    BuffAss.GetSelectionBounds(out selPtStart, out selPtEnd);
    // SELECTION exists:
    if (selPtStart.Offset != selPtEnd.Offset)
    { string OrigText = BuffAss.GetText(selPtStart, selPtEnd, true);
      string Title = "CHANGE LETTER CASE";
      string Layout = "L|rrr";
      string[] Texts = new string[] {"Click for the desired letter case adjustment:",  
        "ALL LETTERS CAPITALIZED", "all letters lower case", "Letters. Capitalized after. A stop." };
      int btn = JD.MultiBox(Title, ref Layout, ref Texts, "ACCEPT|CANCEL");
      if (btn == 1)
      { int n = Layout.IndexOf('R');
        string Model = "";
        if (n == 2) Model = "A";  else if (n == 3) Model = "a";  else if (n == 4) Model = "Aa.Aa";
        if (Model.Length > 0) // i.e. if a radio button is ON (should never occur that none are on; but just in case...)
        { string NewText = OrigText._CaseMatch(Model);
          BuffAss.DeleteSelection(true, true);
          BuffAss.InsertAtCursor(NewText);
        }
      }
    }
    else // NO SELECTION:
    { JD.Msg("No text selected");
    }
  }

  protected void OnShowUndoStackActivated(object sender, EventArgs e)
  { string header = "CONTENTS OF \"UNDO\" STACK";
    string bodytext = "";
    string divider = "⇒⇒⇒⇒⇒⇒⇒⇒\n";
    if (HistoryStack == null)
    { bodytext = "The history stack is empty.\n";
    }
    else
    { var sz = HistoryStack.Count;
      for (int i=0; i < sz; i++)
      { string ss = HistoryStack[i].OldStrip;
        if (!String.IsNullOrEmpty(ss))
        { if (ss[ss.Length-1] != '\u000A')  ss += "\u000A";
          bodytext += ss + divider;
        }
      }
    }
    JD.Display(header, bodytext, false, true, false, new string[] {"CLOSE"});
    int dummy = 1;
  }

//--------------------------------------------------------
//  SEARCH MENU                                       //find  //search

  protected virtual void OnFindActionActivated (object sender, System.EventArgs e)
  { FindInText(true, null, "", true, true, true, true); // First 'true' = ask via message box. Second, 'null',
      // causes LastSoughtText to be displayed initially in msg box. All later parameters are dummies.
  }

//###### I think entry value of 'replacemt' is unused; check out.
// Returns find(s) as character positions, or NULL if no finds (including where the search has been aborted for any reason).
  public int[] FindInText(bool askViaMsgBox, string soughtText, string replacemt, bool matchCase, bool wholeWord,
               bool fromCursor, bool markAll)
  { LblComments.Text = "";
    // Get pointers (as text iterators) to the whole text, and also to any selection:
    TextIter buffStartIt, buffEndIt, selStartIt, selEndIt, searchStartIt, searchEndIt;
    BuffAss.GetBounds(out buffStartIt, out buffEndIt);
    bool isSelection = BuffAss.GetSelectionBounds(out selStartIt, out selEndIt);
    if (askViaMsgBox)
    // Set up the FIND TEXT message box:
    { replacemt = null; // Null 'replacemt' converts the search box into just a 'find' box.
      bool[] cheques;
      // Show the search box, and so input search parameters:
      JD.PlaceBox(450, 250, 0.6, 0.5);
      if (soughtText == null) soughtText = LastSoughtText;
      int button = JD.SearchBox(ref soughtText, ref replacemt,  isSelection, out cheques);
      if (button == 0 || button == 3) return null; // icon closure, CANCEL button.
      markAll = (button == 2);
      matchCase = cheques[0];   wholeWord = cheques[1];   fromCursor = cheques[2];
    }
    // Set globals, in case a repeat search of the same is triggered ('F3' key):
    LastSoughtText = soughtText;  LastMatchCase = matchCase;  LastWholeWord = wholeWord; // in case F3 used later to find the same text again.
    // Colour the finds. (The next 'ESC' keypress will undo the colouring.)
    if (fromCursor)
    { searchStartIt = selStartIt;
      if (isSelection) searchEndIt = selEndIt;  else searchEndIt = buffEndIt;
    }
    else { searchStartIt = buffStartIt;  searchEndIt = buffEndIt; }
    string searchMe = BuffAss.GetText(searchStartIt, searchEndIt, false);
    string origSoughtText = soughtText; // to preserve it after the next line, for LblComments note.
    if (!matchCase) { soughtText = soughtText.ToUpper();  searchMe = searchMe.ToUpper(); }
    int soughtlen = soughtText.Length;
    if (soughtlen == 0 || searchMe == "") return null;
    int searchOffset = 0;  if (fromCursor) searchOffset = searchStartIt.Offset;
    int noFound = 0;
    int[] Result = null;
    if (markAll)
    { int firstFindPosn = -1;
      int[] findings = JTV.FindTextAndTagIt(BuffAss, soughtText, ref searchMe, 0, searchOffset, find_text, wholeWord, P.IdentifierChars);
      if (findings != null)
      { noFound = findings.Length;
        firstFindPosn = findings[0] + searchOffset;
        // *** If ever the above is rewritten in away that alters BuffAss content, then the following line - repeating an earlier one - must be used:
        // *** BuffAss.GetBounds(out buffStartIt, out buffEndIt);
        TextIter thisIt = buffStartIt;
        thisIt.ForwardChars(firstFindPosn);
        REditAss.ScrollToIter(thisIt, 0.1, true, 0.0, 0.5); // '0.5' to get the line halfway up the screen, if possible.
        // *** If the above fails - as it did in OnReplaceActionActivated(.) - q.v. - you have to use REditAss.ScrollToMark(.) instead.
        BuffAss.PlaceCursor(thisIt); // *** If it is ever found a pest that the cursor moves from its prior position, remark this line out.
      }
      Result = findings;
    }
    else // Just the first find:
    { int find = JTV.FindTextAndTagIt(BuffAss, soughtText, ref searchMe, find_text, 0, searchOffset, wholeWord, false, P.IdentifierChars);
         // the final 'false' --> allow the first find, even if it is right at the start point. (As not for 'search again'.)
      if (find >= 0)
      {
        // *** If ever the above is rewritten in away that alters BuffAss content, then the following line - repeating an earlier one - must be used:
        // *** BuffAss.GetBounds(out buffStartIt, out buffEndIt);
        TextIter thisIt = buffStartIt;
        thisIt.ForwardChars(find + searchOffset);
        REditAss.ScrollToIter(thisIt, 0.1, true, 0.0, 0.5); // '0.5' to get the line halfway up the screen, if possible.
        // *** If the above fails - as it did in OnReplaceActionActivated(.) - q.v. - you have to use REditAss.ScrollToMark(.) instead.
        BuffAss.PlaceCursor(thisIt); // *** If it is ever found a pest that the cursor moves from its prior position, remark this line out.
        noFound = 1;
        Result = new int[] {find};
      }
    }
    // Set the Comments label text:
    LblComments.Text = CommentAfterSearch(origSoughtText, noFound, false, markAll, matchCase, fromCursor, wholeWord);
    return Result;
  }

  protected virtual void OnFindAgainActionActivated (object sender, System.EventArgs e)
  { LblComments.Text = "";
    if (LastSoughtText == "") { LblComments.Text = "No text to find."; return; }
    string searchText = LastSoughtText;
    int searchLen = searchText.Length, searchStart = BuffAss.CursorPosition;
    string searchMe = BuffAss.Text.Substring(searchStart);
    string origSearchText = searchText;
    if (!LastMatchCase) { searchMe = searchMe.ToUpper();  searchText = searchText.ToUpper(); }
    if (searchMe._Extent(0, searchLen) == searchText)
    { searchMe = searchMe._Extent(searchLen);  searchStart += searchLen; }
    int noFound = 0; // for display in LblComments below.
    int find = JTV.FindTextAndTagIt(BuffAss, searchText, ref searchMe, find_text, 0, searchStart, LastWholeWord, true, P.IdentifierChars);
    if (find != -1)
    { noFound = 1; // ensures sensible display in LblComments below.
      TextIter buffStartIt, buffEndIt;      
      BuffAss.GetBounds(out buffStartIt, out buffEndIt);
      TextIter thisIt = buffStartIt;
      thisIt.ForwardChars(searchStart + find);
      REditAss.ScrollToIter(thisIt, 0.1, true, 0.0, 0.5); // '0.5' to get the line halfway up the screen, if possible.
        // *** If the above fails - as it did in OnReplaceActionActivated(.) - q.v. - you have to use REditAss.ScrollToMark(.) instead.
      BuffAss.PlaceCursor(thisIt); // *** If it is ever found a pest that the cursor moves from its prior position, remark this line out.
    }
    LblComments.Text = CommentAfterSearch(origSearchText, noFound, false, false, LastMatchCase, true, LastWholeWord);
  }

  protected virtual void OnReplaceActionActivated (object sender, System.EventArgs e) //replace
  {// Set up the REPLACE TEXT message box:
    string soughtText = "", replacemt = "";
    bool matchCase, wholeWord, fromCursor;
    bool[] cheques;
    // Get pointers (as text iterators) to the whole text, and also to any selection:
    TextIter buffStartIt, buffEndIt, selStartIt, selEndIt;
    BuffAss.GetBounds(out buffStartIt, out buffEndIt);
    bool isSelection = BuffAss.GetSelectionBounds(out selStartIt, out selEndIt);
    // Show the search box, and so input search parameters;:
    int button = JD.SearchBox(ref soughtText, ref replacemt, isSelection, out cheques);
    if (button == 0 || button == 3) return; // icon closure, CANCEL button.
    bool dontAsk = (button == 2); // i.e. replace all, without asking at each one.
    matchCase = cheques[0];   wholeWord = cheques[1];   fromCursor = cheques[2];

    int startPtr, endPtr; // here, endPtr points to one char. past the end of the desired text.
    if (fromCursor)
    { startPtr = selStartIt.Offset;
      if (isSelection) endPtr = selEndIt.Offset;  else endPtr = buffEndIt.Offset;
    }
    else {  startPtr = 0;  endPtr = buffEndIt.Offset; }
    string buffText = BuffAss.Text._Extent(startPtr, endPtr - startPtr);
    string origSoughtText = soughtText; // to preserve it after the next line, for LblComments note.
    if (!matchCase) { soughtText = soughtText.ToUpper();  buffText = buffText.ToUpper(); }
    int searchlen = soughtText.Length,  replacelen = replacemt.Length;
    int incrementLen = replacelen - searchlen;
    if (searchlen == 0 || buffText == "") return;
    int noFound = 0, firstFindPosn = -1;
    // Initially mark all finds with a 'find' tag rather than a 'replaced' tag:
    int[] findings = JTV.FindTextAndTagIt(BuffAss, soughtText, ref buffText, 0, startPtr, find_text, wholeWord, P.IdentifierChars);
    if (findings != null)
    { noFound = findings.Length;
      for (int i=0; i < noFound; i++) findings[i] += startPtr; // Some day we should rewrite FindTextAndTagIt so that this is not necessary...
      if (dontAsk)
      { int[] replacings = new int[noFound]; // After replacements, will give locations of replaced text.
        StringBuilder sb = new StringBuilder(BuffAss.Text); // not 'buffText', which may be all upper case by now.
        for (int i = noFound-1; i >= 0; i--)
        { sb.Remove(findings[i], searchlen);
          if (replacelen > 0) sb.Insert(findings[i], replacemt);
          replacings[i] = findings[i] + i * incrementLen;
        }
        BuffAss.Text = sb.ToString();
        RecolourAllAssignmentsText("remove only", find_text, replaced_text);
        foreach (int thisRepl in replacings) JTV.TagExtent(BuffAss, replaced_text, thisRepl, replacelen);
      }
      else // ask before each substitution
      { int wd = 300, ht = 250, cx = -1, cy = -1;
        int noReplaced = 0;
        JTV.MoveCharPosnIntoView(REditAss, BuffAss, 0, true); // This is part of a Jerry-built way of getting the displayed find
            // off the bottom line of the screen. Note the "+ 100" in the next call to this method, a couple of lines below.
            // That will get the word up a bit from the bottom, but ONLY IF you called the search from earlier in the text. E.g.
            // if you started from the end of text, the word to check would now by 100 chars. ABOVE THE TOP of the screen.
            // Hence I put the cursor at the start first. (By the way, JTV.PlaceCursorAtStart(.) doesn't work; I presume that is
            // because Mono queues Gtk instructions, and the one below overrules it.)
        for (int i=0; i < noFound; i++) // have to check each one:
        { JTV.MoveCharPosnIntoView(REditAss, BuffAss, findings[i] + noReplaced * incrementLen + 100, true); // see comments a coupla lines above.
          JTV.TagExtent(BuffAss, query_text, findings[i] + noReplaced * incrementLen, searchlen);
          if (i == 0) JD.PlaceBox(wd, ht, 0.6, 0.5);
          else JD.PlaceBox(wd, ht, cx, cy);
          int btn = JD.DecideBox("REPLACING TEXT", "Replace this one?", new string[] {"YES", "NO", "CANCEL"} );
          JTV.RemoveTagAt(BuffAss, query_text, findings[i] + noReplaced * incrementLen, searchlen);
          if (btn == 0 || btn == 3) return;
          cx = JD.LastBoxCentre.X; cy = JD.LastBoxCentre.Y;
          if (btn == 1) // YES button:
          { TextIter startIt, endIt;
            JTV.ReplaceTextAt(BuffAss, findings[i] + noReplaced * incrementLen, searchlen, replacemt, out startIt, out endIt);
            noReplaced++;
            BuffAss.ApplyTag(replaced_text, startIt, endIt);
          }
        }
      }
      firstFindPosn = findings[0];
     // Move focus to the first find.
      BuffAss.GetBounds(out buffStartIt, out buffEndIt); // We have altered the buffer, so I think we have to reset the iterators.
      TextIter thisIt = buffStartIt;
      thisIt.ForwardChars(firstFindPosn);
      // For some unknown reason, REditAss.ScrollToIter(.) doesn't work here (I tried+++). Instead you have to create a mark, then scroll to it.
      TextMark tempo = BuffAss.CreateMark(null, thisIt, true);
      REditAss.ScrollToMark(tempo, 0.1, false, 0.0, 0.5); // '0.5' to get the line halfway up the screen, if possible. (In practice, not quite.)
      BuffAss.DeleteMark(tempo);      
      BuffAss.PlaceCursor(thisIt); // *** If it is ever found a pest that the cursor moves from its prior position, remark this line out.
    }
    // Set the Comments label text:
    LblComments.Text = CommentAfterSearch(origSoughtText, noFound, true, false, matchCase, fromCursor, wholeWord);
  }
// Specialized routine intended just for the above events:
  private string CommentAfterSearch(string OrigSoughtText, int NoFound, bool isReplace,
                                                        bool MarkAll, bool MatchCase, bool FromCursor, bool WholeWord)
  { string result = "";
    // Record the conditions, in case of a no-find:
    if (NoFound == 0)
    { string tt = "";
      if (MatchCase) tt += "match case; ";
      if (FromCursor) tt += "from cursor down; ";
      if (WholeWord) tt += "whole word; ";
      if (tt.Length > 0) tt = "\n (Conditions: " + tt._CullEnd(2) + ")";
      result = "Text '" + OrigSoughtText + "' not found" + tt;
    }
    else
    { if (isReplace)
      { if (NoFound == 1) result = "One replacement.";  else result = NoFound.ToString() + " replacements."; }
      else
      { if (MarkAll)
        { if (NoFound == 1) result = "One find.";  else result = NoFound.ToString() + " finds."; }
        else result = "Text found.";
      }
    }
    return result;
  }
//----------------------------------------------------//bkmk //bookmark
  protected void OnListAllFindsMoveToAnyActionActivated (object sender, System.EventArgs e)
  {
    string[] boxtexts = new string[] {JTV.ReadSelectedText(BuffAss)}; // will be the empty string, if no selection.
    string spiel = "Insert the text you want to find.\n\nButtons:\n   'EXACT' = find only if a whole word, and in this exact letter case.\n"
                      + "   'ANY' = find whether a whole word or not, and any letter case.";
    int btn = JD.InputBox("SHOW ALL INSTANCES OF SOME TEXT", spiel, new string[] {""}, ref boxtexts,
                             new string[]{"ANY", "EXACT", "CANCEL"});
    if (btn < 1 || btn > 2) return;
    bool mustbeExact = (btn == 2);
    // Collect all the lines of the window (including any empty ones)
    string target = boxtexts[0]; // method below looks after oddball values.
    string alltext = BuffAss.Text;
    ShowAllOccurrencesOfSomeText(ref alltext, target, mustbeExact, mustbeExact);
  }

  public void ShowAllOccurrencesOfSomeText(ref string TheText, string Target, bool isCaseSensitive, bool mustBeWholeWord)
  { string target = Target.Trim ();   if (target == "" || TheText == "") return;
    string target_upper = target.ToUpper();
    int targetLen = target.Length;
    string[] Lines = TheText.Split(new char[] {'\n'}, StringSplitOptions.None);
    List<string>FoundLines = new List<string>();
    for (int i=0; i < Lines.Length; i++)
    { int[] findings;
      if (!isCaseSensitive)
      { findings = (Lines[i].ToUpper())._IndexesOf(target_upper); }
      else  findings = Lines[i]._IndexesOf(target);
      if (findings != null)
      { bool atleastone = true;
        if (mustBeWholeWord)
        { atleastone = false;
          for (int j=0; j < findings.Length; j++)
          {
            char c1 = ' '; if (findings[j] > 0) c1 = Lines[i][findings[j]-1];
            char c2 = ' '; if (findings[j] + targetLen < Lines[i].Length-1) c2 = Lines[i][findings[j] + targetLen];
            if (P.IdentifierChars.IndexOf(c1) >= 0 ||P.IdentifierChars.IndexOf(c2) >= 0) // identifier char. on one or other side of the find
            { findings[j] = -1; } // invalidates the find.
            else atleastone = true;
          }
        }
        if (atleastone)
        { // Set up a copy of the line, together with line no. and with instances in bold type:
          string intro =  "<# magenta>" + (i+1).ToString() + "<# blue>:  ";   int introLen = intro.Length;
          string ss = intro + Lines[i];
          for (int j=findings.Length-1; j >= 0; j--)
          { int p1 = findings[j]; if (p1 == -1) continue; // find was invalidated above.
            ss = ss.Insert(p1 + introLen + targetLen, "</b>");
            ss = ss.Insert(p1 + introLen, "<b>");
          }
          // Recolour remarks of the one-par type:
          int n = ss.IndexOf(HeaderCue);
          if (n >= 0) ss = ss.Insert(n, "<# " + ClrHeader._ToString(",") + ">");
          else
          { int p = ss.IndexOf(CommentCue);
            if (p >= 0) ss = ss.Insert(p, "<# " + ClrComment._ToString(",") + ">");
          }
          FoundLines.Add(ss);   FoundLines.Add(" ");
        }
      }
    }
    int sz = FoundLines.Count;
    string conditions = "(Conditions: ";
    if (mustBeWholeWord) conditions += "whole word only; ";   else conditions += "any occurrence; ";
    if (isCaseSensitive) conditions += "exact letter case.)";  else conditions += "any letter case.)";
    if (sz == 0) // no finds:
    { JD.Msg ("Could not find the text <b>" + target + "</b>\n\n" + conditions); }
    else // find(s):
    { string[] arr = FoundLines.ToArray();
      string findery = "<in 30><b>OCCURRENCES OF TEXT '" + target + "'      " + conditions;
      findery += "</b>\nTo move focus to a line below, select the line number (left margin) and then click 'ACCEPT'.\n\n";
      findery += String.Join("\u000A", arr);
      JD.PlaceBox(0.9, 0.9, 0.5, 0.5);
      int btn1 = JD.Display ("STUFF", findery, true, true, false, new string[] {"ACCEPT", "CANCEL"});
      if (btn1 == 1)
      { if (JD.SelectedText != "")
        {
          int lineno = JD.SelectedText._ParseInt(-1);
          if (lineno > 0) // remember, 'lineno' is to base 1
          { JTV.PlaceCursorAtLine(REditAss, lineno-1, 0, true); }

        }
      }
    }
  }

  protected virtual void OnInsertBookmarkActionActivated (object sender, System.EventArgs e)
  { BuffAss.InsertAtCursor(SystemBkMk);
    RecolourAllAssignmentsText("leave all");
    JTV.PlaceCursorAt(BuffAss, BuffAss.CursorPosition - 2); // Cursor ends up sandwiched between the remark bounds.
  }

  protected virtual void OnGoToBookmarkActionActivated (object sender, System.EventArgs e)
  { string buffText = BuffAss.Text;
    int find = -1;
    int startPtr = BuffAss.CursorPosition;
    while (true)
    { find = JTV.FindTextAndTagIt(BuffAss, SystemBkMk, ref buffText, null, startPtr, 0, false, true, P.IdentifierChars);
      if (find == -1 || find != startPtr) break;
      startPtr += SystemBkMk.Length;
    }
    // If no find from cursor onwards, search again from the start:
    if (find == -1 && startPtr > 0) find = JTV.FindTextAndTagIt(BuffAss, SystemBkMk, ref buffText, null, 0, 0, false, false, P.IdentifierChars);
    // If a find, get the line halfway up the screen, if possible:
    TextIter buffStartIt, buffEndIt;
    BuffAss.GetBounds(out buffStartIt, out buffEndIt);
    TextIter thisIt = buffStartIt;
    thisIt.ForwardChars(find);
    REditAss.ScrollToIter(thisIt, 0.1, true, 0.0, 0.5); // '0.5' to get the line halfway up the screen, if possible.
    // *** If the above fails - as it did in OnReplaceActionActivated(.) - q.v. - you have to use REditAss.ScrollToMark(.) instead.
    BuffAss.PlaceCursor(thisIt); // *** If it is ever found a pest that the cursor moves from its prior position, remark this line out.
  }

  protected void OnRemoveThisBookmarkActionActivated (object sender, System.EventArgs e)
  {
    if (!(sender is int)) { JD.Msg("Use the hot key version, as you need to specify a particular bookmark."); return; }
    int bkmkIndex = (int) sender;
    string alltext = BuffAss.Text;
    int[] foo = alltext._IndexesOf("/*" + bkmkIndex + "*/");  if (foo == null) return;
    int cursorAt = BuffAss.CursorPosition; // We will adjust this downwards for every removed bookmark which begins below it.
    int topAt = REditAss.VisibleRect.Top;
    for (int i = foo.Length-1; i >= 0; i--)
    { int n = foo[i];
      if (alltext._Extent(n+3, 2) == "*/") // (Does not crash if extent goes beyond end of string.)
      { alltext = alltext.Remove(n, 5);
        if (n < cursorAt) cursorAt -= 5; // stops the cursor being in effect pushed forward by every removed bookmark
      }
    }
    BuffAss.Text = alltext;
    RecolourAllAssignmentsText("remove all"); // all other tags already gone, because I have replaced the buffer; if this is ever
      // rewritten so as not to replace the buffer, then worth using arg. "leave all", which takes longer but leaves all the tags as is.
    while (Application.EventsPending ())   Application.RunIteration(); // Omit this and you will always end up at top of file, cursor prob. offscreen.
    JTV.PlaceLocnAtTopOfView(REditAss, BuffAss, topAt);
    JTV.PlaceCursorAt(BuffAss, cursorAt);
  }

  protected void OnRemoveAllBookmarksActionActivated (object sender, System.EventArgs e)
  {// *** This delegate is also called directly from key handler method "DoSpecialAction(.)", using dummy args.; keep in mind if altering code here.
    string alltext = BuffAss.Text;
    int[] foo = alltext._IndexesOf("/*");  if (foo == null) return;
    int cursorAt = BuffAss.CursorPosition; // We will adjust this downwards for every removed bookmark which begins below it.
    int topAt = REditAss.VisibleRect.Top;
    for (int i = foo.Length-1; i >= 0; i--)
    { int n = foo[i];
      if (alltext._Extent(n+3, 2) == "*/") // (Does not crash if extent goes beyond end of string.)
      { alltext = alltext.Remove(n, 5);
        if (n < cursorAt) cursorAt -= 5; // stops the cursor being in effect pushed forward by every removed bookmark
      }
    }
    BuffAss.Text = alltext;
    RecolourAllAssignmentsText("remove all"); // all other tags already gone, because I have replaced the buffer; if this is ever
      // rewritten so as not to replace the buffer, then worth using arg. "leave all", which takes longer but leaves all the tags as is.
    while (Application.EventsPending ())   Application.RunIteration(); // Omit this and you will always end up at top of file, cursor prob. offscreen.
    JTV.PlaceLocnAtTopOfView(REditAss, BuffAss, topAt);
    JTV.PlaceCursorAt(BuffAss, cursorAt);
  }

  protected virtual void OnNavigateBackwardsActionActivated (object sender, System.EventArgs e)
  { Navigate(false); }

  protected virtual void OnNavigateForwardsActionActivated (object sender, System.EventArgs e)
  { Navigate(true); }

  public void Navigate ( bool Forwards)
  { if (NavigationStack == null) return; // should be impossible, as the stack is initiated to one entry, 0, and never later becomes empty.
    Navigating = true;
    Duo currentLocn = NavigationStack[NavigationPtr];
    int nextPtr;
    Duo nextLocn;
    if (Forwards)
    { if (NavigationPtr >= NavigationStack.Count-1) return;
      nextPtr = NavigationPtr+1;
    }
    else
    { if (NavigationPtr == 0) return;
      nextPtr = NavigationPtr-1;
    }
    nextLocn = NavigationStack[nextPtr];
    if (nextLocn.X != currentLocn.X) // then we have to renew the screenful of text:     
    { JTV.PlaceLocnAtTopOfView(REditAss, BuffAss, nextLocn.X); }
    JTV.PlaceCursorAt(BuffAss, nextLocn.Y);
    NavigationPtr = nextPtr;
  }

  public Strint2 WordOccurrencePtr = new Strint2(-1, -1, "", ""); // If no selection, .SX holds an extracted word and .SY is empty;
  // if there was a selection, .SX holds the selection (no trimming occurs) and .SY is exactly "S". In all cases, .IX holds the top of screen
  // in "buffer coordinates" (whatever they are), and .IY the cursor posn of the start of the word or selection.
  // Current use: From startup, if the cursor is at word "AAA" when cntrl-n is clicked (n = 1 to 9), then WordOccurrencePtr is set to
  // (top of screen,  cursor posn,  "AAA", "") - or (..., "AAA", "S") if there was a selection. Cntrl-n will meanwhile take one to the
  // nth. occurrence of "AAA" in the text (or won't budge, if no such occurrence). If .SY = "S", then the newly accessed
  // instance of "AAA" will again be selected; otherwise not. Suppose .cntrl-n' is again keyed; the current word at the cursor is still
  // AAA, which is the same as WordOccurrencePtr.SX; therefore WordOccurrencePtr is unaltered, and the n'th. occurrence is focussed.
  // As long as AAA remains the content of WordOccurrencePtr.SX, cntrl-0 will always bring the user back to the ORIGINAL screen - using
  // fields .IX and .IY to get there - and then will reset WordOccurrencePtr to the 'empty' value above.
  // If at any stage the focus is changed to e.g. word BBB and then cntrl-n pressed, then because WordOccurrencePtr.SX differs from the new
  // word it will be overwritten by "BBB" and this screen's pointers.

/// <summary>
/// WhichOccurrence must be >= 0.
/// </summary>
  protected void GoToWordOccurrence(int WhichOccurrence)
  { if (WhichOccurrence < 0) return;
    if (WhichOccurrence == 0) // return to the original reference word, and then make WordOccurrencePtr empty:
    { if (WordOccurrencePtr.IX >= 0) // Do nothing, if there is no original occurrence.
      { JTV.PlaceLocnAtTopOfView(REditAss, BuffAss, WordOccurrencePtr.IX);
        JTV.PlaceCursorAt(BuffAss, WordOccurrencePtr.IY);
        WordOccurrencePtr = new Strint2(-1, -1, "", "");
      }
      return;
    }
    // Collect this identifier:
    Strint2 thisWord = WordAtCursor(true);  if (thisWord.IX == -1) return; // No potential legal identifier at the cursor, so do absolutely nothing.
    if (thisWord.SX != WordOccurrencePtr.SX) // Overwrite WordOccurrencePtr, making this the word for subsequent nth-occurrence searches:
    { WordOccurrencePtr = new Strint2(REditAss.VisibleRect.Top, BuffAss.CursorPosition, thisWord.SX, thisWord.SY);
    }
    // Find the occurrence. If a selection, we can simply use "._IndexOfNth(.)"; otherwise not, as we want to exclude instances where
    //   the search text is part of another word (e.g. where search for instances of variable "x" would also find 'x' in "exists").
    string soughtText = thisWord.SX;
    int soughtTextLen = soughtText.Length;
    bool isSelection = (thisWord.SY == "S");
    int pntr = 0;
    if (isSelection) pntr = BuffAss.Text._IndexOfNth(soughtText, WhichOccurrence);
    else // no selection:
    { int cntr = 0;
      while (true)
      { pntr = BuffAss.Text.IndexOf(soughtText, pntr);
        if (pntr == -1) break;
        string ss = BuffAss.Text._FromTo(pntr-1, pntr + soughtTextLen); // one char. each side of the word at pntr
        Strint hint = JS.ExtractIdentifier(ss, 1, true, P.IdentifierChars);
        if (hint.I == 1 && hint.S == soughtText) cntr++;
        if (cntr >= WhichOccurrence) break;
        pntr += soughtTextLen;
      }
    }
    if (pntr >= 0) // If it IS -1, then no cursor or screen action occurs, but WordOccurrencePtr remains set to the same word.
    { JTV.MoveCharPosnIntoView(REditAss, BuffAss, pntr, true);
      if (isSelection) JTV.SelectText(BuffAss, pntr, soughtTextLen);
    }
  }
  
  /// <summary>
  /// <para>First identifies the text to be sought. If no selection, the search text is the adjacent sequence of identifier characters; only
  ///   complete instances of this will be sought (e.g. search text = "sin" would not cause finding of "assinine"). Otherwise the selected text
  ///   (no trimming) is identified, and any instances of this will be sought (e.g. selected "sin" would indeed find "assinine").</para>
  /// <para>Returns: SX = the search text; IX = the index of this occurrence in the whole text; IY = total no. occurrences in the text;
  ///   XX = start of search text; XY = length of text. BX is 'true' for all finds; BY is 'true' if there was a selection. SY is not used.</para>
  /// <para>Error: Only occurs if there was no selection and there were no identifier chars. are adjacent to the cursor. In this case,
  ///   returns: SX = "", IX = 0, IY = 0, XX = -1, XY = -1, BX = false, BY = false.</para>
  /// </summary>
  public Octet WhichOccurrenceIsThis()
  { Strint2 Wursor = WordAtCursor(true); // 'true' = no start digit allowed (only referenced if there is no selection).
    if (Wursor.IX == -1) return new Octet(0, -1.0, false, "", 0, -1.0, false, "");
    bool isSelection = (Wursor.SY == "S"); // only 'true' if there is no selection.
    string theWord = Wursor.SX;  int wordLen = theWord.Length;
    int CntToHere = 1, CntAfterHere = 0;
    string theText = BuffAss.Text;
    int[] earlierOnes = theText._IndexesOf(theWord, 0, Wursor.IX-1);
    if (!isSelection && earlierOnes != null)
    { int len = earlierOnes.Length;
      List<int> wordies = new List<int>(len);
      for (int i = 0; i < len; i++)
      { Strint squint = JS.ExtractIdentifier(theText, earlierOnes[i], true, P.IdentifierChars);
        if (squint.S == theWord) wordies.Add(earlierOnes[i]);
      }
      earlierOnes = wordies.ToArray();
    }
    if (earlierOnes != null) CntToHere += earlierOnes.Length;
    int[] laterOnes = theText._IndexesOf(theWord, Wursor.IY + wordLen);
    if (!isSelection && laterOnes != null)
    { int lenny = laterOnes.Length;
      List<int> hurdygurdies = new List<int>(lenny);
      for (int i = 0; i < lenny; i++)
      { Strint squint = JS.ExtractIdentifier(theText, laterOnes[i], true, P.IdentifierChars);
        if (squint.S == theWord) hurdygurdies.Add(laterOnes[i]);
      }
      laterOnes = hurdygurdies.ToArray();
    }
    if (laterOnes != null) CntAfterHere = laterOnes.Length;
    return new Octet(CntToHere,                (double) Wursor.IX,                  true,        theWord, 
                     CntToHere + CntAfterHere, (double)(Wursor.IY - Wursor.IX + 1), isSelection,   ""     );
  }

  protected void OnWhichOccurrenceOfWordActionActivated (object sender, System.EventArgs e)
  { Octet whichetty = WhichOccurrenceIsThis();
    if (!whichetty.BX) return; // Do nothing. Means there was no selected text, and cursor not adjacent to identifier character(s).
    JD.Msg ("This is the " + whichetty.IX._Ordinal() + " occurrence of '" + whichetty.SX + "'.\nTotal occurrences: " + whichetty.IY.ToString());
    // If there was a selection, then this dialog box display has unselected it, so we have to restore the selection:
    if (whichetty.BY) JTV.SelectText(BuffAss, (int) whichetty.XX, (int) whichetty.XY);
  }

  protected virtual void OnGoToNthActionActivated (object sender, System.EventArgs e)
  {
    string[] boxtext = new string[]{""};
    string preamble = "Set N as 1 to 19, for the 1st. to 19th. occurrence; or as 0, to return to the original cursor position.\n\n" +
                        "(Hot-key combinations: For N = 0 to 9, use Cntrl + N; for N = 10 to 19, use Cntrl + Alt + 0 to 9.)";
    int btn = JD.InputBox("GO TO NTH. OCCURRENCE OF WORD", preamble,
                  new string[]{"Value of N:  (0 to 19)"}, ref boxtext, "ACCEPT", "CANCEL");
    if (btn == 1)
    { int N = boxtext[0]._ParseInt(-1);
      if (N >= 0 && N <= 19) GoToWordOccurrence(N);
    }
  }

  protected void OnGoToThisFunctionSDeclarationActionActivated (object sender, System.EventArgs e)
  { // If the word at the cursor turns out to be a user function name, moves its declaration into view, and ensures that cntrl-0
    //  returns to orig. view (if the cursor is not moved. When the declaration is conjured up, the cursor sits at the start of the fn. name.)
    Strint2 sue = WordAtCursor(true);
    string PFN = sue.SX;   if (PFN  == "") return;
    string[] DeclarationOnly, AndItsRemarks;
    int[] Foo =  FindFunctionDeclarations(PFN, out DeclarationOnly, out AndItsRemarks, false, false);
    if (Foo.Length > 0)
    { // Register the present instance of the function name, so it can be returned to via ctrl-0:
      WordOccurrencePtr.IX = REditAss.VisibleRect.Top;
      WordOccurrencePtr.IY = BuffAss.CursorPosition;
      WordOccurrencePtr.SX = PFN;
      // Now head for it:
      JTV.PlaceCursorAtLine(REditAss, Foo[0], 0, true);
      // Move the cursor till it is at the start of the function name, so that ctrl-0 can return you back to whence you came:
      int ptr = (DeclarationOnly[0]).IndexOf(PFN);
      if (ptr != -1)
      { TextIter titter = BuffAss.GetIterAtLineIndex(Foo[0], ptr);
        BuffAss.PlaceCursor(titter);
      }
    }
  }

  protected virtual void OnFindLineByNumberActionActivated (object sender, System.EventArgs e)
  { int totalLines = BuffAss.Text._CountChar('\n');   if (totalLines < 2) return;
    string ss = "Enter a line number between 1 and " + totalLines.ToString();
    string[] boxText = null;
    int btn = JD.InputBox("GO TO LINE NUMBER", ss, new string[]{""}, ref boxText, new string[]{"ACCEPT", "CANCEL"});
    if (btn == 1)
    { bool success;
      int lineNo = boxText[0]._ParseInt(out success);
      if (success) success = (lineNo >= 1 && lineNo <= totalLines);
      if (!success) JD.Msg(String.Format("'{0}' does not convert into a line no. between 1 and {1}.", boxText[0], totalLines));
      else JTV.PlaceCursorAtLine(REditAss, lineNo-1, 0, true);
    }
  }

  protected virtual void OnShowCurrentLineSNumberActionActivated (object sender, System.EventArgs e)
  { if (!BufferHasLineNos) InsertLineNosIntoBufferText();
  }

  public void InsertLineNosIntoBufferText()
  { int toppie = REditAss.VisibleRect.Top;
    HoldBufferContentsDuringLineNosDisplay = BuffAss.Text; // class string
    string currentText = "\n" + BuffAss.Text;
    string ss;
    currentText = currentText.Replace("\n", "\n££££  ");
    StringBuilder sb = new StringBuilder(currentText);
    int lineNo = 0, maxLineNo = BuffAss.LineCount;
    int noZeroes = 1 + (int) Math.Log10( (double) (maxLineNo + 5) );
    string boo = '0'._Chain(noZeroes);
    for (int i=0; i < sb.Length; i++)
    {
      if (sb[i] == '\n')
      { ss = String.Format ("{0:" + boo + "}", ++lineNo);
        sb.Remove(i+1, 4);  sb.Insert(i+1, ss);
      }
    }
    sb.Remove (0,1); // Remove the '\n' which we inserted at the start, a few lines above
    BuffAss.Text = sb.ToString();
    RecolourAllAssignmentsText("leave all");
    while (Application.EventsPending ())   Application.RunIteration(); // Leave this out and the next line does nothing (see method description).
    JTV.PlaceLocnAtTopOfView(REditAss, BuffAss, toppie);
    BufferHasLineNos = true; // Keep this switch to prevent collision with other processes that set / reset REditAss.Editable.
    ThisWindow.REditAss.ModifyBase(StateType.Normal, ColourAssBackPaused); // Change REditAss background colour for duration of the line no. display.
    REditAss.Editable = false;
    LblAss.Markup = "    ASSIGNMENTS:                             <span foreground=\"red\"><big><b>Press ESC to return to Editing mode</b></big></span>";
  }

  public void RemoveLineNosFromBufferText()
  { 
    int toppie = REditAss.VisibleRect.Top;
    REditAss.Editable = true;
    BuffAss.Text = HoldBufferContentsDuringLineNosDisplay;
    HoldBufferContentsDuringLineNosDisplay = "";
    BufferHasLineNos = false; // Keep this switch to prevent collision with other processes that set / reset REditAss.Editable.
    RecolourAllAssignmentsText("leave all");
    while (Application.EventsPending ())   Application.RunIteration(); // Leave this out and the next line does nothing (see method description).
    JTV.PlaceLocnAtTopOfView(REditAss, BuffAss, toppie);
    ThisWindow.REditAss.ModifyBase(StateType.Normal, ColourAssBack);
    LblAss.Markup = "    ASSIGNMENTS:";
  }

  protected virtual void OnREditAssMoveCursor (object o, Gtk.MoveCursorArgs args)
  {
    // Nothing doing at present...
  }

  protected virtual void OnColourMatchActionsActivated (object sender, System.EventArgs e)
  { 
    if (sender == ColourMatchOfBracketsAction)
    { ColourBrackets('(', ')');
      AutoColourBrackets = 1;
    }
    else if (sender == ColourMatchOfBracesAction)
    { ColourBrackets('{', '}');
      AutoColourBrackets = 2;
    }
    else ColourBrackets('[', ']');
  }

  protected virtual void OnMarkBlockActionsActivated (object sender, System.EventArgs e)
  {
    char OpenChar, CloseChar;
    if (sender == MarkTextBetweenBracesAction) { OpenChar = '{';  CloseChar = '}'; }
    else if (sender == MarkTextBetweenBracketsAction) { OpenChar = '(';  CloseChar = ')'; }
    else { OpenChar = '[';  CloseChar = ']'; }
    // Find the locations of the enclosing clamps:
    int CursorPtr = BuffAss.CursorPosition;
    int openerAt =  JS.OpenerAt(BuffAss.Text, OpenChar, CloseChar, 0, CursorPtr);
    if (openerAt < 0) openerAt = 0;
    int closerAt =  JS.CloserAt(BuffAss.Text, OpenChar, CloseChar, CursorPtr);
    if (closerAt < 0) closerAt = BuffAss.Text.Length-1; 
    // Mark the stretch between clamps, including the clamps themselves, with the mark used for text finds:
    JTV.TagExtent(BuffAss, find_text, openerAt, closerAt - openerAt + 1);
  }

  //------------------------------------------------------------------------------
  //  APPEARANCE MENU                                  //appearance  //app  //appear

  protected virtual void OnIndentSelectedLinesActionActivated (object sender, System.EventArgs e)
  {
    PrefixSelectedLines("\t", false); // 'false' - if no selection, let TAB have its normal action.
  }

  protected virtual void OnUnindentSelectedLinesActionActivated (object sender, System.EventArgs e)
  {
    UnPrefixSelectedLines("\t", false);
  }

  protected virtual void OnRemarkOutSelectedLinesActionActivated (object sender, System.EventArgs e)
  {
    PrefixSelectedLines("//", true); // 'true' - if no selection, remark out the line holding the cursor.
  }

  protected virtual void OnUndoRemarkOutOfSelectedLinesActionActivated (object sender, System.EventArgs e)
  {
    UnPrefixSelectedLines("//", true);
  }

// The use of remarks colouring tags is ON by default.
  protected virtual void OnToggleRemarksColouringSystemActionActivated (object sender, System.EventArgs e)
  { if (UseMarkUpTags)
    { JD.Msg("Turn off usage of markup tags first (menu item 'View | Toggle Markup Tag System')");  return; }
    UseRemarksTags = !UseRemarksTags;
    RecolourAllAssignmentsText("remove all"); // The above class bool is used in this method.
  }
  protected virtual void OnToggleMarkupTagSystemActionActivated (object sender, System.EventArgs e)
  { SwitchMarkupTagSystem(!UseMarkUpTags);
  }

  public void SwitchMarkupTagSystem(bool HonourMarkUpTags)
  {
    if (HonourMarkUpTags == UseMarkUpTags) return;
    UseMarkUpTags = !UseMarkUpTags;

    if (UseMarkUpTags)
    { HoldUseRemarksTags = UseRemarksTags;
      UseRemarksTags = false;
      HoldREditAssText = BuffAss.Text;
      HoldREditAssCursorLocnForMarkUp = BuffAss.CursorPosition; // for access on turning markup tag colouring back off.
      HoldREditAssTopLocnForMarkUp = REditAss.VisibleRect.Top; // ditto.
      HoldUnsavedData = UnsavedData; // This is to stop the insertion and deletion below of the bookmark firing the UnsavedData flag.
      // We need to bookmark the cursor position. We can't use text tags or marks, as they are eliminated by buffer clearing in
      //  method RecolourAll..(.) called below. So we will very temporarily insert a text passage "[&bkmk&]" at the cursor. BUT
      //  we must place that outside any markup tag - perhaps we have just been editing one and have left the cursor there -as
      //  RecolourAll...(.) will fail to recognize it with our bookmark in it.
      int n, ptr = BuffAss.CursorPosition;
      string ss = BuffAss.Text._Extent(ptr, 20); // surely no tag is longer than that?
      n = ss._IndexOf('>');
      if (n >= 0) JTV.PlaceCursorAt(BuffAss, ptr + n + 1); // One char. past '>' (even if it is not part of a tag - so what?)
      BuffAss.InsertAtCursor("[&bkmk&]");
      REditAss.ModifyText(StateType.Normal, JTV.Black);
      RecolourAllAssignmentsText("remove all");
      while (Application.EventsPending ())   Application.RunIteration();
      ptr = BuffAss.Text.IndexOf("[&bkmk&]");
      JTV.SelectText(BuffAss, ptr, 8);
      BuffAss.DeleteSelection(false, true);
      SetUnsavedData(HoldUnsavedData);
      // Before the above the cursor was left at the start of text; so if ptr is more than one screenful below the start of text,
      //  then it will be placed at the very bottom of the screen. So let's put it nearer the centre:
      TextIter twit = BuffAss.StartIter;   twit.ForwardChars(ptr);
      for (int i=0; i < 15; i++) REditAss.ForwardDisplayLine(ref twit); // 15 lines up works fairly well for typical font and screen height.
      ptr = twit.Offset;
      JTV.MoveCharPosnIntoView(REditAss, BuffAss, ptr, true); // Cursor will be at the bottom, but the prior focus is fairly midscreen.
      REditAss.Editable = false;
    }
    else
    { HoldUnsavedData = UnsavedData; // This is to stop the changes below firing the UnsavedData flag.
      // If there is a selection in the formatted display, the unformatted display will have that line fairly well centred.
      //    If no selection, then the unformatted display will be as was before switching to formatted display, irrespective of
      //    any scrolling done in the formatted display.
      TextIter dummy1, dummy2;
      bool isSeln = BuffAss.GetSelectionBounds(out dummy1, out dummy2);
      int cursorLine = -1;
      if (isSeln)cursorLine = JTV.CursorLine(BuffAss, 0).I;
      REditAss.Editable = true;
      REditAss.ModifyText(StateType.Normal, ColourAssText);
      BuffAss.Text = HoldREditAssText;
      UseRemarksTags = HoldUseRemarksTags;
      RecolourAllAssignmentsText("remove all");
      while (Application.EventsPending ())   Application.RunIteration();
      // Now we reposition the display. As mentioned above, if there was a selection in the formatted version, it will be scrolled to:
      if (isSeln) JTV.PlaceCursorAtLine(REditAss, cursorLine, 0, true);
      else
      { JTV.PlaceLocnAtTopOfView(REditAss, BuffAss, HoldREditAssTopLocnForMarkUp);
        JTV.PlaceCursorAt(BuffAss, HoldREditAssCursorLocnForMarkUp);
      }
      HoldREditAssTopLocnForMarkUp = -1;  HoldREditAssCursorLocnForMarkUp = 0;
      SetUnsavedData(HoldUnsavedData);
    }
  }

  protected string[] OrigBoxTextsForColourChanging = null; // Ensures 'original' button below always returns the colour at start-up.
  protected virtual void OnChangeAppearanceColoursActionActivated (object sender, System.EventArgs e)
  {
    string[] subMenuLabels = new string[] {"Assignments Window - Text     ", "Assignments Window - Background", "Results Window - Background",
              "Remarks - one line ('<span color=\"" + JS.ColourToHex("#", ClrComment) + "\">// yak</span>')",
              "Remarks colour - header ('<span color=\"" + JS.ColourToHex("#", ClrHeader) + "\">__ yak</span>')",
              "Remarks - block ('<span color=\"" + JS.ColourToHex("#", ClrIgnore) + "\">/* yak yak */</span>')"};
    PopUpAppearanceMenu(subMenuLabels, OnAppearanceMenuItemClick);
  }
  protected void PopUpAppearanceMenu(string[] SubMenuLabels, EventHandler Adventure)
  { Menu many = new Menu ();
    int len = SubMenuLabels.Length;  if (len == 0) return;
    AccelLabel[] axles = new AccelLabel[len];
    MenuItem[] man = new MenuItem[len];
    for (int i=0; i < len; i++)
    { axles[i] = new AccelLabel("");
      axles[i].UseMarkup = true;
      axles[i].Markup = SubMenuLabels[i];
          // I have found no way to stop justification being central; 'axles[i].Justify = Justification.Left' does nothing, as does Pango gravity.
      man[i] = new MenuItem();  man[i].Name = "Appearance" + i.ToString();
      man[i].Add(axles[i]);
      man[i].Activated += Adventure;
      axles[i].Show();
      man[i].Show();
      many.Append(man[i]);
    }
    many.Popup();
    many.Dispose();
  }
  // Appearance Context Menu handler:
  private void OnAppearanceMenuItemClick (object o, EventArgs args)
  { string ss = (o as MenuItem).Name;
    string[] headings = new string[] {"ASSIGNMENTS WINDOW TEXT", "ASSIGNMENTS WINDOW BACKGROUND", "RESULTS WINDOW BACKGROUND",
                                      "ON-LINE REMARKS COLOUR", "HEADING REMARKS COLOUR", "BLOCK REMARKS COLOUR" };
    string[] hexClr = new string[]
      { JS.ColourToHex("", ClrAssText), JS.ColourToHex("", ClrAssBack), JS.ColourToHex("", ClrResBack),
        JS.ColourToHex("", ClrComment), JS.ColourToHex("", ClrHeader), JS.ColourToHex("", ClrIgnore) };
    // Get the index of the sending menu item within the popup menu:
    int ndx = ss._Extent(10)._ParseInt(-1);  if (ndx == -1) return;
    int btn = 1;
    string inStr = hexClr[ndx];
    while (btn == 1) // If the user clicks the 'SET' button in the dialog, btn returns as 1, and relooping occurs
                     // with inStr set to whatever the user put into the dialog text box before pressing 'SET'.
    { btn = JD.ColourChoiceBox(headings[ndx], ref inStr, "bytes"); }
    if (btn == 2) // the 'FINISH' button.
    {
      byte[] culler = inStr._ToByteArray(",");
      Gdk.Color cooler = new Color(culler[0], culler[1], culler[2]);
      switch (ndx)
      { case 0 :
        { ClrAssText = culler;    ColourAssText  = cooler;
          REditAss.ModifyText(StateType.Normal, ColourAssText);
          break;
        }
        case 1 :
        { ClrAssBack = culler;    ColourAssBack  = cooler;
          REditAss.ModifyBase(StateType.Normal, ColourAssBack);
          break;
        }
        case 2 :
        { ClrResBack = culler;    ColourResBack  = cooler;
          REditRes.ModifyBase(StateType.Normal, ColourResBack);
          break;
        }
        case 3 :
        { ClrComment = culler;    ColourComment  = cooler;
          comment_text.ForegroundGdk = ColourComment;
          break;
        }
        case 4 :
        { ClrHeader = culler;    ColourHeader  = cooler;
          header_text.ForegroundGdk = ColourHeader;
          break;
        }
        case 5 :
        { ClrIgnore = culler;    ColourIgnore  = cooler;
          ignore_text.ForegroundGdk = ColourIgnore;
          break;
        }
        default: break;
      }
    }
  }

  protected virtual void OnChangeAppearanceFontsActionActivated (object sender, System.EventArgs e)
  { int btn = 1;
    string[] prompts = null, boxtexts = null;
    while (btn == 1)
    { string header = "FONTS";
      string preamble = "For a list of available fonts on this machine, click on the 'List' button.";
      prompts = new string[] { "Assignments Window Font Name", "Assignments Window Font Size",
                                         "Results Window Font Name",    "Results Window Font Size" };
      boxtexts = new string[] { FontNameAss, FontPointsAss.ToString(), FontNameRes, FontPointsRes.ToString() };
      btn = JD.InputBox(header, preamble, prompts, ref boxtexts, "LIST", "ACCEPT", "CANCEL");
      if (btn == 1)
      { string errmsg;
        string listing = JTV.ListFontsOnMachine("/tmp/", ProgramPath + "allfonts.txt", out errmsg, true);
        if (errmsg == "")
        {
          JD.Display("FONTS AVAILABLE ON THIS MACHINE", listing, true, false, false, "CLOSE");
        }
        else JD.Msg("Attempt to load fonts failed. Error message: " + errmsg);
      }
    }
    if (btn == 2)
    { boxtexts = boxtexts._Trim();
      if (boxtexts[0] != "") FontNameAss = boxtexts[0];
      double x = boxtexts[1]._ParseDouble(0.0);
      if (x >= 1.0) FontPointsAss = (float) x;
      if (boxtexts[2] != "") FontNameRes = boxtexts[2];
      x = boxtexts[3]._ParseDouble(0.0);
      if (x >= 1.0) FontPointsRes = (float) x;
      // Apply these, even if unchanged:
      Pango.FontDescription REditAssFont =
                  Pango.FontDescription.FromString(FontNameAss + FontSecondOptions + " " + FontPointsAss.ToString());
      REditAss.ModifyFont(REditAssFont);
      Pango.FontDescription REditResFont =
                  Pango.FontDescription.FromString(FontNameRes + FontSecondOptions + " " + FontPointsRes.ToString());
      REditRes.ModifyFont(REditResFont);
    }

  }


  private string HoldFontNameAss = String.Empty; // Only used for the following method
  protected void OnToggleProportionalFontActionActivated (object sender, EventArgs e)
  { if (HoldFontNameAss == String.Empty)
    { HoldFontNameAss = FontNameAss;
      Pango.FontDescription REditAssFont =
                  Pango.FontDescription.FromString("DejaVu Sans Mono,Andale Mono,Liberation Mono,Nimbus Mono L" + " " + FontPointsAss);
      REditAss.ModifyFont(REditAssFont);
    }
    else
    { FontNameAss = HoldFontNameAss;
      HoldFontNameAss = String.Empty;
      Pango.FontDescription REditAssFont =
                  Pango.FontDescription.FromString(FontNameAss + FontSecondOptions + " " + FontPointsAss.ToString());
      REditAss.ModifyFont(REditAssFont);
    }
  }

  protected virtual void OnColourSelectorActionActivated (object sender, System.EventArgs e)
  { int btn = 1;
    string inStr = "";
    while (btn == 1) // If the user clicks the 'SET' button in the dialog, btn returns as 1, and relooping occurs
                     // with inStr set to whatever the user put into the dialog text box before pressing 'SET'.
    { btn = JD.ColourChoiceBox("", ref inStr, "verbose"); }
    if (btn == 2) // the 'FINISH' button.
    { string ss = "Chosen colour:  " + inStr + '\n'; // In this case, inStr has formatted details of the chosen colour.
      JTV.DisplayMarkUpText(REditRes, ref ss, "append");
    }
  }

  //------------------------------------------------------------------------------
  //  SHOW MENU                                            //show

/// <summary>
/// <para>Searches back 500 chars, so a longer name will go unrecognized. Checks for a system fn., if all letters small (or '_');
/// if fails, or if not so, searches for a user fn. in REditAss text - i.e. for "function Foo(..)" - and produces what is between brackets.
/// ** NB: This handler is also raised by menu item OnDisplayFunctionTextActionActivated(.). The second does all that the first does, but in
/// addition displays the text of the user function (if found) in a message box.</para>
/// <para>To allow for the editing of non-MonoMaths C-like programs, this method also works for keywords "public", "private" and "protected";
///   but the search for the keyword "function" occurs first.</para>
/// </summary>
  protected virtual void OnDisplayFunctionArgsActionActivated (object sender, System.EventArgs e)
  { bool showFunctionCode = (sender == FunctionCodeWithCursorAfterAction);
    int startptr, endptr = BuffAss.CursorPosition - 1;
    startptr = endptr - 500;  if (startptr < 0) startptr = 0;
    string FnNm, ss = BuffAss.Text._FromTo(startptr, endptr);   if (ss == "") return;
    int ptr = JS.OpenerAt(ss, '(', ')', 0, ss.Length-1 );  if (ptr == -1) return;
    ss = ss._Reverse();
    ptr = ss.Length - ptr - 1;
    char CR = '\n';
    Octet ox = JS.FindWord(ss, ptr + 1, 999, P.IdentifierChars, "", true);
    if (!ox.BX) return;
    FnNm = ox.SX._Reverse(); // hopefully is a function name.
    int sysfnno = -1, userfnptr = -1;
    string userfndisplaytext = "";
    string alltext = ""; // The whole of BuffAss text.
    for (int i=0; i < F.SysFnCnt; i++) { if (F.SysFn[i].Name == FnNm) { sysfnno = i; break; } }
    if (sysfnno == -1) // look instead for a user fn. name - provided the program has been run once:
    { // check USER FNS. (An earlier version simply searched P.UserFn, if the program had been run once. This fails because
      //   if, as you develop code, you have altered the user fn. since the last run, this method will not have noted the changes.)
      alltext = BuffAss.Text;
      int hop = 9;  // hop = length of the keyword + 1; this is the start value, for keyword 'function'.
      bool seekingFunction = true; // If true, seeking keyword "function"; otherwise, seeking keywords "public" etc.
      string[] struedel = null;
      ptr = 0;
      while (true)
      {
        if (seekingFunction)
        { ptr = alltext.IndexOf("function", ptr);
          if (ptr == -1)
          { seekingFunction = false;  ptr = 0;
            struedel = new string[] {"public", "private", "protected"}; // keep this order, as it is used in calculating 'hop' below.
          }
        }
        if (!seekingFunction)
        { Duo dew = alltext._IndexOfAny(struedel, ptr);
          ptr = dew.X;
          hop = dew.Y + 7;  if (hop == 9) hop = 10; // a 'hop' from first letter of keyword takes focus to first letter beyond following space.
          if (ptr == -1) break;
        }
        // ignore find, if the keyword is remarked out, or no subsequent bracketted section:
        int[] taggery = JTV.TagsApplyingHere(BuffAss, AllCommentTagNames, ptr+1); // Tags applying just after the 1st. letter of the keyword.
        if (taggery != null) { ptr += hop; continue; }
        int opr = alltext.IndexOf('(', ptr+hop);  if (opr == -1) {  ptr += hop; continue; }
        int clr = JS.CloserAt(alltext, '(', ')', opr);  if (clr == -1) {  ptr += hop; continue; } // Possible, if program never yet compiled.
        // Ignore find, if the last word before the opening bracket is not the given function name
        ss = alltext._FromTo(ptr+hop, opr-1);
        ss = ss.Trim(); // Allows internal spaces (for the case of keywords like 'public')
        int n = ss.LastIndexOf(' ');
        if (n != -1)
        { // One more source of error: if any char. other than a lower case Latin letter (for a C-like language) or a white space occurs between
          // the fn. name and this space, it cannot be a function declaration. E.g. consider the code`ss = "Foo function"; <next line> x = Foo(y);`
          //  As is, n points to the space before "Foo(y)", so thinks it has found the function declaration. But the stuff
          //  between the word 'function' and the word 'Foo' necessarily contains punctuation.
          string tt = ss._FromTo(0, n-1)._PurgeRange(0, -1, 'a', 'z')._Purge();
          if (tt.Length > 0) ss = ""; 
          else ss = ss._Extent (n+1); // If there are internal spaces, only look at the word after the last one.
        }
        if (ss != FnNm) { ptr += hop; continue; }
        // function found, and has args.:
        userfnptr = ptr; // Used in later section.
        if (showFunctionCode)
        { opr = alltext.IndexOf('{', ptr); if (opr == -1) { ptr += hop; continue; }
          clr = JS.CloserAt(alltext, '{', '}', opr);  if (clr == -1) {  ptr += hop; continue; }
          userfndisplaytext = alltext._FromTo(ptr, clr);  break;
        }
        else userfndisplaytext = alltext._FromTo(opr+1, clr-1) + CR;  break;
      }
      if (userfndisplaytext == "") return; // all hope of finding a fn. is gone.
    }

   // FUNCTION NAME FOUND, so hunt for details in the file:
    string Header = "<# BLUE><B>" + FnNm + "(.) args:</B>  ";
    if (sysfnno >= 0) // SYSTEM FN.:
    { if (showFunctionCode) // Menu item was "Show | Function Code":
      { StringBuilder outcome;
        bool isHybrid = (F.SysFn[sysfnno].Hybrid == (byte) 2);
        int ptr_start, ptrOp = 0, ptrCl = 0;
        ss = "";
        if (isHybrid) // hybrids are processed inSysFn2
        { Boost boodle = Filing.LoadTextFromFile(SysFn2FilePathName, out outcome); // "outcome" will hold the text of the file
          if (!boodle.B) { JD.Msg("Unable to access source code file '" + SysFn2FilePathName + "'\n\n"); return; }
          ss = outcome.ToString();
          ptr = ss.IndexOf("static void Hybrids");  if (ptr == -1) return; // The Hybrids section begins with this declaration.
          ptr_start = ss.IndexOf("case " + sysfnno.ToString() + ":", ptr);
          if (ptr_start == -1) return; // Not found.
        }
        else
        { string fnm;
          if      (sysfnno <= 200) fnm = SysFn1FilePathName;
          else if (sysfnno <= 400) fnm = SysFn2FilePathName;
          else fnm =                     SysFn3FilePathName;
          Boost boodle = Filing.LoadTextFromFile(fnm, out outcome); // "outcome", a stringbuilder, will hold the text of the file
            if (!boodle.B) { JD.Msg("Unable to access source code file '" + fnm + "'\n\n"); return; }
          ss = outcome.ToString();
          // Find the start of the first line which containst "case <sysfnno>:" (or leave start as is, if no LF in last 200 chars.)
          ptr_start = ss.IndexOf("case " + sysfnno.ToString() + ":");
          if (ptr_start == -1) return; // Not found.
        }
        // Whatever the type of system function, the rest of the code is the same...
        int line_start = ss._LastIndexOf('\n', ptr_start - 200, ptr_start);
        if (line_start == -1) line_start = ptr_start;
        ptrOp = 1 + ss.IndexOf(" {", ptr_start); // If the remarks section contains a brace, it should't have a spaces before it.
        ptrCl = JS.CloserAt(ss, '{', '}', ptrOp);
        if (ptrCl == -1) ptrCl = ptrCl + 1000; // should never happen.
        ss = ss.Substring(line_start, ptrCl - line_start + 1);
        ptr = 0; // Remember, ss is now just the substring of the file which contains this function's data.
        while (true)
        { ptr = ss.IndexOf("//", ptr);  if (ptr == -1) break;
          ss = ss._Insert(ptr, "<# magenta>");
          ptr = ss.IndexOf('\n', ptr);  if (ptr == -1) break;
          ss = ss._Insert(ptr, "<# blue>");
        }
        ss = "<# blue>" + ss;
        JD.PlaceBox(Screen.Width - 200, Screen.Height - 200);
        JD.Display("SOURCE CODE", ss, true, true, false, "CLOSE");
        return;
      }
      else // Menu item was "Display Fn Arg Description":
       // Get the function args. text:
      { string FlNm = ProgramPath + "FnArgs.txt";
        StringBuilder fileText;
        Boost boodle = Filing.LoadTextFromFile(FlNm, out fileText);
        if (!boodle.B) { JD.Msg("Unable to access function arguments file '"+FlNm+"'"); return; }
        string fnArgsTextFileContents = fileText.ToString();
        string TopicHeader = "//" + FnNm + "//";
        ptr = fnArgsTextFileContents.IndexOf(TopicHeader);
        if (ptr >= 0)
        { ptr = fnArgsTextFileContents.IndexOf('\n', ptr); // Move to end of line containing "//FNNAME". Crasho, if none!
          int ptr1 = fnArgsTextFileContents.IndexOf("====", ptr); // Crasho, if none!
          string BodyText = fnArgsTextFileContents._Between(ptr, ptr1);
          BodyText = BodyText.Replace("scalars", "scal*&%ars"); // to avoid next step applying to it
          BodyText = BodyText.Replace("scalar", "<# MAGENTA>scalar<# BLUE>");
          BodyText = BodyText.Replace("scal*&%ars", "<# MAGENTA>scalars<# BLUE>");
          BodyText = BodyText.Replace("arrays", "arr*&%ays"); // to avoid next step applying to it
          BodyText = BodyText.Replace("array", "<# MAGENTA>array<# BLUE>");
          BodyText = BodyText.Replace("arr*&%ays", "<# MAGENTA>arrays<# BLUE>"); // to avoid next step applying to it
          BodyText = BodyText.Replace("matrix", "<# MAGENTA>matrix<# BLUE>");
          BodyText = BodyText.Replace("bool", "<# MAGENTA>bool<# BLUE>");
          BodyText = BodyText.Replace("%", ""); // Keep this till after all replacements involving input strings of length > 1.
          BodyText = BodyText.Replace("{{", "£$%|"); // to avoid the next replacement, so as to allow literal single braces.
          BodyText = BodyText.Replace("}}", "%$£|");
          BodyText = BodyText.Replace("{", "<# BLACK><B>");
          BodyText = BodyText.Replace("}", "</B><# BLUE>");
          BodyText = BodyText.Replace("£$%|", "{"); // undoing the above temporary replacement.
          BodyText = BodyText.Replace("%$£|", "}");
          ss = Header + BodyText;
          JTV.DisplayMarkUpText(REditRes, ref ss, "start");
        }
      }
    }
    else // USER FN. identified:
    {// Now hunt for preceding lines beginning with '//' or with only spaces + '//':
      string preamble = "";
      string tt="", sometxt = alltext._FromTo(userfnptr - 2000, userfnptr-1); // The last thousand chars. before the 'f' of 'function'.
      if (sometxt != "")
      { sometxt = CR + sometxt;
        int p=0, q=0, qq = -1, nopars = sometxt._CountChar(CR); char ch = ' ';
        for (int i=nopars-1; i > 0; i--)
        { p = sometxt._IndexOfNth(CR, i);
          q = -1;
          for (int j=1; j < 10; j++)
          { ch = sometxt[p+j];  if (ch == '/' && sometxt[p+j+1] == '/') { q = p; break; }
            else if (ch != ' ' && ch != '\t') break;
          }
          if (q == -1) break;
          qq = q; // qq will end up as the beginning of the first line starting with '   //'
        }
        if (qq >= 0) { tt = sometxt._Extent(qq+1);  preamble += "<# MAGENTA>" + tt; } // tt is used below
      }
      if (preamble._Last() != '\n') preamble += '\n';
     // Now display differently for the two menu items:
      if (showFunctionCode)
      { // Do a bit of colouring: (Only for '//' remarks).
        int ptrCl = 0;
        while (true)
        { int ptrOp = userfndisplaytext.IndexOf("//", ptrCl);  if (ptrOp == -1) break;
          userfndisplaytext = userfndisplaytext._Insert(ptrOp, "<# magenta>");
          ptrCl = userfndisplaytext.IndexOf('\n', ptrOp);  if (ptrCl == -1) break;
          userfndisplaytext = userfndisplaytext._Insert(ptrCl+1, "<# blue>");
          ptrCl += 8;
        }
        userfndisplaytext = preamble + "<# blue>" + userfndisplaytext;
        JD.PlaceBox(Screen.Width - 200, Screen.Height - 200);
        JD.Display("FUNCTION CODE", userfndisplaytext, true, true, false, "CLOSE");
      }
      else // display args. in Results window:
      { ss = "<# BLUE>" + Header + userfndisplaytext + preamble + "<# blue>---------------\n"; // The definition of the function comes first. (It already ends in CR.)
        JTV.DisplayMarkUpText(REditRes, ref ss, "start");
      }
    }
  }

  protected void OnListAssignedVariablesActionActivated (object sender, System.EventArgs e)
  { if (V.Vars == null)
    { JD.Msg("No variables have been declared yet - run the program first.");  return; }
    // PREPARE THE TEXT OF THE DISPLAY
    int noValuesShown = 20; // i.e. the number of values displayed from the array's data strip
    int noSignificantFigures = 4;   string GString = "G" + noSignificantFigures;
    string headline = "<stops 20, 130, 300, 370>" + 
      "<b>\tFunc\tName\tSize\tValue(s)</b>   (up to " + noValuesShown + " values, each to " + noSignificantFigures + " significant figures)\n";
    string fnlvlstr = "", bodytext = "", ss = "",  longdash = "‒",   constantdash = "—"; // unicodes 8210 (0x2012) and 8212 (0x2014)
    // Display variables of main level first; then, if the current focus is in a function, variables of that function:
    int[] levels;
    int CurrentFnNo = R.CurrentlyRunningUserFnNo;
    if (CurrentFnNo > 0) levels = new int[] {CurrentFnNo, 0};  else levels = new int[] { 0 };
    List<string> FinalDisplayLines = new List<string>();
    for (int i = 0; i < levels.Length; i++)
    { int thisFnLvl = levels[i];
      if (thisFnLvl == 0) fnlvlstr = "Main";  else fnlvlstr = P.UserFn[thisFnLvl].Name;
//####      if (fnlvlstr.Length > 10) fnlvlstr = fnlvlstr.Substring(0, 10) + "…";            
      List<TVar> variety = V.Vars[thisFnLvl];
      List<string> LocalDisplayLines = new List<string>();
      string Clr = "";
      for (int j=0; j < variety.Count; j++)
      { TVar thisVar = variety[j];
        if (thisVar.Use == 1  ||  thisVar.Use == 3  ||  thisVar.Use == 11) // ignore all others
        { 
          string nm = thisVar.Name;
          if (nm._Extent(0, 2) == "__") continue; // We don't want system constants with internal usage only.
//####          if (nm.Length > 17) nm = nm.Substring(0, 17) + "…";            
          ss = "\t" + fnlvlstr + "\t" + nm + '\t';
          // Scalars:
          if (thisVar.Use == 1)      ss += constantdash + "\t" + thisVar.X.ToString("G5");
          else if (thisVar.Use == 3) ss += longdash + "\t" + thisVar.X.ToString("G5");
          else
          { // Add the statement of dimensions:
            StoreItem sitem = R.Store[thisVar.Ptr];
            for (int k = sitem.DimSizes.Length-1; k >= 0;  k--)
            { ss += sitem.DimSizes[k].ToString();
              if (k > 0) ss += 'x';
            }
            ss += "\t";
           // Add the first few data items:
            double[] dudu = sitem.Data._Copy(0, noValuesShown); // If smaller, will just copy the smaller amount of data.
            if (dudu == null)
            { ss += "NULL ARRAY"; }
            else 
            { for (int m = 0; m < dudu.Length; m++)
              { ss += dudu[m].ToString(GString);
                if (m < dudu.Length-1) ss += ", ";
              }
              if (sitem.Data.Length > noValuesShown) ss += "…";
            }
          }
          LocalDisplayLines.Add(ss);
        }
      }
      if (LocalDisplayLines.Count > 1)
      { LocalDisplayLines.Sort();
        FinalDisplayLines.AddRange(LocalDisplayLines);
        // We could not add colour tags before sorting, so they go on now...
        for (int ii = 0; ii < LocalDisplayLines.Count; ii++)
        { ss = LocalDisplayLines[ii];
          bool isConstant = (ss.IndexOf(constantdash) > 0);
          bool isScalar = ( isConstant || ss.IndexOf(longdash) > 0);
          if (thisFnLvl == 0) 
          { if (isScalar) Clr = isConstant ?  "<# green>"  :  "<# blue>";
            else Clr = "<# red>";
          }
          else { Clr = isScalar ? "<# dodgerblue>" : "<# magenta>"; }
          LocalDisplayLines[ii] = Clr + ss;
        }
        bodytext += String.Join("\n", LocalDisplayLines.ToArray()) + "\n";
      }
    }    
    bodytext = "<# black>(If any part of a line is selected at closure, the first occurrence of its variable will be displayed.)\n"
                  + bodytext;
    int displayWidth = 940, displayHeight = Screen.Height - 200,  topMgn = 100,  rightMgn = 100;  
    var startPosition = new Tetro(displayWidth,  displayHeight,  ScreenWidth - displayWidth/2 - rightMgn, displayHeight/2 + topMgn);
    if (LastVarsBoxPosition.X1 < 0)  JD.PlaceBox(startPosition);
    else JD.PlaceBox(LastVarsBoxPosition);
    string[] ButtonNames = (CurrentRunStatus > 0)  ?  new string[]{"SEEK", "ALTER", "CLOSE"}  :  new string[]{"SEEK", "CLOSE"};
    int btn = JD.Display("VALUES OF VARIABLES", headline + bodytext, true, false, false, ButtonNames);
    LastVarsBoxPosition = new Tetro(JD.LastBoxSize.X, JD.LastBoxSize.Y, JD.LastBoxCentre.X, JD.LastBoxCentre.Y);
    if (btn == 0 || btn == ButtonNames.Length || JD.SelectedText == "") return; // User cancelled out of the box, or there is no selection.
    // TRACK DOWN THE VARIABLE, as the user wants either to "seek" the variable (find it in program code) or to "alter" its code.
    // Move focus to the first occurrence of the word in the program text. (Could well be in a remarked 
    //  section and in any function. Good enough for now...)
    int ndx = JD.SelectionLine - 2; // '2' because the first two lines of the display are explanatory, not included in FinalDisplayLines.
    if (ndx >= 0 && ndx < FinalDisplayLines.Count)
    { ss = FinalDisplayLines[ndx]; // Unlike LocalDisplayLines, this string array does not have text tags included.
      int[] tagptrs = ss._IndexesOf('\t');
      if (tagptrs != null && tagptrs.Length > 2)
      { string fnm = ss._Between(tagptrs[0], tagptrs[1]);
        string varnm = ss._Between(tagptrs[1], tagptrs[2]);
        if (btn == 2) // ALTER THE VALUE OF THE VARIABLE 
        {
          int var_no = V.FindVar(CurrentFnNo, varnm);
          if (var_no >= 0) // If -1, var. not identified, so do nothing.
          { 
            bool boo = ChangeValueOfVariable(CurrentFnNo, var_no);
            ss = boo ? "Value successfully changed.\n" : "Failed to change value of '" + varnm + "'";
            JD.Msg(ss);
          }
          return;
        }
        // SEEK THE VARIABLE IN PROGRAM CODE
        int start_ptr = 0; // applies if this is a main program variable.
        if (fnm != "Main")
        // Set the start of the search to the beginning of the function:
        { string[] dummy1 = null, dummy2 = null;
          int[] fnline = FindFunctionDeclarations(fnm, out dummy1, out dummy2, false, false);
          if (fnline.Length > 0)
          { start_ptr = BuffAss.GetIterAtLine(fnline[0]).Offset;
          }
        }
        ss = BuffAss.Text;
        int ptr = JTV.FindTextAndTagIt(BuffAss, varnm, ref ss, find_text, start_ptr, start_ptr, true, false,  P.IdentifierChars);
        if (ptr >= 0)
        { TextIter buffStartIt, buffEndIt;      
          BuffAss.GetBounds(out buffStartIt, out buffEndIt);
          TextIter thisIt = buffStartIt;
          thisIt.ForwardChars(ptr);
          REditAss.ScrollToIter(thisIt, 0.1, true, 0.0, 0.5); // '0.5' to get the line halfway up the screen, if possible.
          // *** If the above fails - as it did in OnReplaceActionActivated(.) - q.v. - you have to use REditAss.ScrollToMark(.) instead.
          BuffAss.PlaceCursor(thisIt); // *** If it is ever found a pest that the cursor moves from its prior position, remark this line out.
          LastSoughtText = varnm;  LastMatchCase = true;   LastWholeWord = true;
        }
      }
    }
    return;
  }

  // For internal use only. No error detection - crash without message if any errors! Returns TRUE if value altered.
  private bool ChangeValueOfVariable(int Fn, int At)
  {
    int var_use = V.GetVarUse(Fn, At);
    if (var_use != 3 && var_use != 11) return false; // No changing constants or system variables or unassigned variables.    
    string[] prompts, boxtexts;
    string toptext;
    int btn;
    bool success;
    // SCALAR:
    if (var_use == 3)
    { var x = V.GetVarValue(Fn, At);
      prompts = new string[]{"Old value:", "New value:"};
      boxtexts = new string[] {x.ToString(), ""};
      toptext = "";
      btn = JD.InputBox("CHANGE VALUE FOR SCALAR VARIABLE", toptext, prompts, ref boxtexts, new string[] {"ACCEPT", "CANCEL"});
      if (btn != 1) return false;
      double y = boxtexts[1]._ParseDouble(out success);
      if (!success) return false;
      V.SetVarValue(Fn, At, y);
      return true;
    }
    else // ARRAY:
    {
      string fnname = V.GetVarName(Fn, At);
      StoreItem sitem = R.Store[V.GetVarPtr(Fn, At)];
      double[] olddata = sitem.Data._Copy();
      int nodims = sitem.DimCnt;
      string ttt;
      toptext = "Array '" + fnname + "' ";
      if (nodims == 1)
      { ttt = "is one-dimensional, of length " + sitem.TotSz.ToString(); }
      else
      { ttt = "has dimensions " + sitem.DimSz[nodims-1].ToString();
        for (int i = nodims-2; i >= 0; i--) ttt += " x " + sitem.DimSz[i].ToString();
        ttt += "; total length " + sitem.TotSz.ToString();
      }
      toptext += "\n\nYou have to supply the ABSOLUTE address within the array, and then enter as many consecutive values as you want changed," +
                 " separated by commas.\n\nIf more values are supplied than can fit, the excess will be ignored./n";
      prompts = new string[]{"Absolute address:", "New value(s):"};
      boxtexts = new string[] {"", ""};     
      btn = JD.InputBox("CHANGE VALUES WITHIN AN ARRAY VARIABLE", "", prompts, ref boxtexts, new string[] {"ACCEPT", "CANCEL"});
      if (btn != 1) return false;
      double x = boxtexts[0]._ParseDouble(out success); 
      if (success)
      { x = Math.Round(x);      
        if (x >= 0.0 && x < (double) sitem.TotSz)
        { int ptr = (int) x; // We now have a valid absolute address for this array.
          double[] newdata = boxtexts[1]._ToDoubleArray(",");
          if (newdata != null && newdata.Length > 0)
          {
            int end_ptr = Math.Min(ptr + newdata.Length - 1, sitem.TotSz - 1);             
            int length_to_copy = end_ptr - ptr + 1;
            Array.Copy(newdata, 0, olddata, ptr, length_to_copy);
            if (olddata.Length == sitem.Data.Length) // inequality should be impossible, but just in case...
            {
              sitem.Data = olddata;
              return true;
            }
          }
        }
      }
    }
    return false;
  }

  protected virtual void OnViewContentsOfSystemListActionActivated (object sender, System.EventArgs e)
  { int fsyslen;
    if (F.Sys == null) fsyslen = -1; else fsyslen = F.Sys.Count;
    if (fsyslen <= 0) { JD.Msg("No lists exist yet.");  return; }
    string[] BoxText = null;
    int btn = JD.InputBox("DISPLAY OF LIST CONTENTS", "List(s) available are indexed from 0 to " + (fsyslen-1).ToString(),
                              new string[] {"List index:"}, ref BoxText,  new string[] {"ACCEPT", "CANCEL"} );
    if (btn != 1) return;
    int n = BoxText[0]._ParseInt(-1);
    if (n >= 0 && n < fsyslen)
    { double[] doodad = F.Sys[n].LIST.ToArray();
      //Create a temporary array, display it, then destroy it:
      int slut = V.GenerateTempStoreRoom(doodad.Length);
      R.Store[slut].Data = doodad;
      DisplayArrayWithOptions(slut, "System List",  n.ToString());
      R.Store[slut].Demolish();  R.StoreUsage[slut] = 0;
    }
  }

  protected virtual void OnListUnusedVariablesActionActivated (object sender, System.EventArgs e)
  { if (V.Vars == null || V.Vars[0].Count == 0)
    { JD.Msg("Run the program first."); return; }
    string UnreachedStr = "<b>VARIABLES NEVER ASSIGNED DURING THE RUN</b>\n\n";
    string UnusedStr    = "<b>VARIABLES ASSIGNED BUT NOT USED DURING THE RUN</b>\n\n";
    // This approach (looking for Use = 0) works in functions as well, because after a function has been used once
    //  and then is exited, its arrays acquire use 10 and its scalars remain at use 3.
    bool foundUnreached = false, foundUnused = false;
    for (int fnlvl = 0; fnlvl < C.UserFnCnt; fnlvl++)
    { List<string> unreached = new List<string>(); // names of variables never assigned
      List<string> unused = new List<string>(); // names of variables assigned but never used
      List<TVar> toby = V.Vars[fnlvl];
      for (int vari = 0; vari < toby.Count; vari++)
      { TVar czar = toby[vari];
        if (czar.Use == 0) unreached.Add(czar.Name);
        else if (!czar.Accessed && czar.Use >= 3) unused.Add(toby[vari].Name); // Don't include system constants and variables
      }
      if (unreached.Count > 0)
      { foundUnreached = true;
        if (fnlvl == 0) UnreachedStr += "<u>MAIN PROGRAM:</u>\n";  else UnreachedStr += "<u>FUNCTION " + P.UserFn[fnlvl].Name + ":</u>\n";
        for (int i=0; i < unreached.Count; i++) UnreachedStr += unreached[i] + "\n";
        UnreachedStr += "\n";
      }
      if (unused.Count > 0)
      { foundUnused = true;
        if (fnlvl == 0) UnusedStr += "<u>MAIN PROGRAM:</u>\n";  else UnusedStr += "<u>FUNCTION " + P.UserFn[fnlvl].Name + ":</u>\n";
        for (int i=0; i < unused.Count; i++) UnusedStr += unused[i] + "\n";
        UnusedStr += "\n";
      }
    }
    if (!foundUnreached) UnreachedStr += "None found.\n\n";
    if (!foundUnused) UnusedStr += "None found.\n";
    JD.Display("UNASSIGNED AND UNUSED VARIABLES", UnreachedStr + UnusedStr, true, true, false, "CLOSE");
  }

  protected void OnListAllFunctionsActionActivated (object sender, System.EventArgs e)
  { string[] DeclarationOnly, AndItsRemarks;
    FindFunctionDeclarations("", out DeclarationOnly, out AndItsRemarks, true, true);
    if (AndItsRemarks.Length > 0)
    { string hdr = "<in 30><b>FUNCTIONS OF THIS PROGRAM</b>\n" +
                   "To move focus to a particular function after the  box closes, highlight its name and click \"SEEK\".\n" +
                   "If the program has been run, the no. of calls to a function is shown in square brackets after its name.\n";
      string uu = hdr + String.Join("\n", AndItsRemarks);
      JD.PlaceBox(0.9, 0.9, 0.5, 0.5);
      int btn = JD.Display("FUNCTIONS OF THIS PROGRAM", uu, true, true, false, "SEEK", "!CLOSE");
      if (btn == 1  &&  JD.SelectedText != "") // then we are to go to the function mentioned in the line holding the highlight:
      { int[] fuddle =  FindFunctionDeclarations(JD.SelectedText, out DeclarationOnly, out AndItsRemarks, true, true);
        if (fuddle.Length > 0)
        { JTV.PlaceCursorAtLine(REditAss, fuddle[0], 0, true);
          // Move the cursor till it is at the start of the function name:
          int ptr = (DeclarationOnly[0]).IndexOf(JD.SelectedText);
          if (ptr != -1)
          { TextIter titter = BuffAss.GetIterAtLineIndex(fuddle[0], ptr);
            BuffAss.PlaceCursor(titter);
          }
        }
      }
    }
  }

//  protected void OnListAllFunctionsActionActivated (object sender, System.EventArgs e)
//  { string[] DeclarationOnly, AndItsRemarks;
//    FindFunctionDeclarations("", out DeclarationOnly, out AndItsRemarks, true, true);
//    if (AndItsRemarks.Length > 0)
//    { string hdr = "<in 30><b>FUNCTIONS OF THIS PROGRAM</b>\n" +
//                   "To move focus to a particular function after the  box closes, highlight its name here before closing.\n" +
//                   "If the program has been run, the no. of calls to a function is shown in square brackets after its name.\n";
//      string uu = hdr + String.Join("\n", AndItsRemarks);
//      JD.PlaceBox(0.9, 0.9, 0.5, 0.5);
//      JD.Display("FUNCTIONS OF THIS PROGRAM", uu, true, true, false, "!CLOSE");
//      if (JD.SelectedText != "") // text was highlighted in the dialog box before it was closed
//      { int[] fuddle =  FindFunctionDeclarations(JD.SelectedText, out DeclarationOnly, out AndItsRemarks, true, true);
//        if (fuddle.Length > 0)
//        { JTV.PlaceCursorAtLine(REditAss, fuddle[0], 0, true);
//          // Move the cursor till it is at the start of the function name:
//          int ptr = (DeclarationOnly[0]).IndexOf(JD.SelectedText);
//          if (ptr != -1)
//          { TextIter titter = BuffAss.GetIterAtLineIndex(fuddle[0], ptr);
//            BuffAss.PlaceCursor(titter);
//          }
//        }
//      }
//    }
//  }
//



  /// <summary>
  /// <para>Returns the line number(s) at which function declaration(s) occur, or an empty array if none.
  /// If ParticularName is specified and found, only the details of that one function are returned; if empty, details of all functions
  /// are returned. (If name specified and not found, the empty int[] is returned.)</para>
  /// <para>OUT arguments: DeclarationsOnly returns with the actual declaration(s) (empty, if no finds). Array 'AndItsRemarks' is larger (usually),
  ///  as each function's declaration is preceded by the remarks that precede it.</para>
  /// <para>'AddFormatting' - if true, colour tags (blue for declaration, and magenta for remarks) are added to 'AndItsRemarks'
  ///  (but not to 'DeclarationOnly').</para>
  /// <para>'AddCallsCount' - if true, and if the program has run, then "[123]" is appended to the function declaration; if 'AddFormatting',
  ///  then this addend will be in green.</para>
  /// </summary>
  public int[] FindFunctionDeclarations(string ParticularFnName, out string[] DeclarationOnly, out string[] AndItsRemarks,
                                                                                               bool AddFormatting, bool AddCallsCount)
  {
    DeclarationOnly = new string[0];  AndItsRemarks = new string[0];
    List<string> DecOnlyList = new List<string>(),  AndRemList = new List<string>();
    List<int>FnLinesList = new List<int>();
    string alltext = BuffAss.Text;   if (alltext.Length < 12) return new int[0]; // No program with fewer chars. could have a function.
    // Extract any segments in block-remarking, though preserve the line feeds contained by the block.
    int extent = 0;
    while (true)
    { int blockOpr = alltext.IndexOf(IgnoreCueOpen);   if (blockOpr == -1) break;
      int blockClr = alltext.IndexOf (IgnoreCueClose);
      if (blockClr == -1) extent = alltext.Length - blockOpr;
      else extent = blockClr - blockOpr + IgnoreCueClose.Length; // this includes both cues in the block
      int[] noLFs = alltext._IndexesOf('\n', blockOpr, blockOpr + extent - 1);
      if (noLFs == null)  alltext = alltext.Remove(blockOpr, extent);
      else alltext = alltext._Scoop(blockOpr, extent, ("\n")._Chain(noLFs.Length));
    }
    string[] Lines = alltext.Split(new char[] {'\n'}, StringSplitOptions.None);
    int noLines = Lines.Length;
    for (int i=0; i < noLines; i++)
    { int ptr = Lines[i].IndexOf("function");
      if (ptr != -1)
      { string thisline = Lines[i]; string ss = thisline._Extent(0, ptr);
        ss = ss.Trim(); // This removes tabs as well as spaces.
        if (ss == "") // then 'function' occurs at the beginning of the line, ignoring white spaces.
        { // We won't do deep parsing here; the worst that will happen is that occasionally a reference to a function will be included.
          int ptr1 = thisline._IndexOf('('); // Only a demented programmer would have the opening '(' on a separate line. Too bad for him.
                // *** Important: ptr1 is used in dealing with 'Lines[i]' below, which assumes that both in 'thisLine' and 'Lines[i]' it
                //       points to the opening bracket. If fiddling with 'thisLine' in the future, keep this in mind.
          if (ptr1 != -1)
          { string fnName = thisline._FromTo(ptr + 8, ptr1-1).Trim();
            // Is it a possible name?
            if (C.LegalName(fnName))
            {// THIS IS A FUNCTION LINE: (well, usually... see caveat above.)
              if (ParticularFnName != ""  &&   fnName != ParticularFnName) continue;
              // If required, retrieve the number of function calls, and convert to a string to append to the function declaration:
              string callsCountSuffix = "";
              if (AddCallsCount && R.UserFnCalls != null)
              { int thisfn = -1;
                for (int ii=1; ii < C.UserFnCnt; ii++) // start at 1, i.e. miss the main pgm
                { if (P.UserFn[ii].Name == fnName) { thisfn = ii; break; } }
                if (thisfn > 0)
                { callsCountSuffix = "    [" + R.UserFnCalls[thisfn].ToString() + "]";
                  if (AddFormatting) callsCountSuffix = "<# green>" + callsCountSuffix;
                }
              }
              int firstDeclnLine = i;
              FnLinesList.Add(firstDeclnLine); // lists the line index of the start of each function.
              // Find the last line of the declaration (or just the first 5 lines of it, if it is longer):
              int lastDeclnLine = i; // value not used, but C# complains if no value provided
              string accum = "";
              for (int j=0; j < 5; j++)
              { if (i+j == noLines) break;
                lastDeclnLine = i+j;
                accum += Lines[lastDeclnLine]; // don't worry about a LF, as 'accum' has just this one purpose
                int n = JS.CloserAt(accum, '(', ')', ptr1);  if (n > 0) break;
              }
              // Find where the remarks start:
              int firstRemLine = i; // will be the case if there are no remarks.
              for (int j = i-1; j >= 0; j--)
              { int n = Lines[j].IndexOf("//");
                if (n == -1) break;
                firstRemLine = j;
              }
              // Pile the lines into the two lists. Remarks first:
              for (int k = firstRemLine; k < firstDeclnLine; k++)
              { if (AddFormatting  &&  k == firstRemLine) AndRemList.Add("<# magenta>");
                AndRemList.Add(Lines[k]);
              }
              // Now add the rest to both lists:
              for (int k = firstDeclnLine;  k <= lastDeclnLine;  k++)
              { ss = "";
                if (AddFormatting  &&  k == firstDeclnLine) ss = "<# blue>";
                ss += Lines[k];
                if (k == lastDeclnLine) ss += callsCountSuffix;
                DecOnlyList.Add (ss);   AndRemList.Add(ss);
              }
              if (ParticularFnName != "") break; // no point in looking for further instances, as have found the given one.
            }
          }
        }
      }
    }
    DeclarationOnly = DecOnlyList.ToArray();
    AndItsRemarks = AndRemList.ToArray();
    return FnLinesList.ToArray();
  }
  

  
  protected virtual void OnShowRunTimeActionActivated (object sender, System.EventArgs e)
  { if (RecordRunTime)
    { RecordRunTime = false;  RunDurationsAction.Label = "Show Run Durations"; }
    else
    { RecordRunTime = true; RunDurationsAction.Label = "Hide Run Durations";
      string ss = "Duration of last run: " + ( RunDuration / 1000.0 ).ToString("G4") + " secs."; // will show '0' if never yet a run.
      REditRes.Buffer.Text += ss;
    }
  }

  protected virtual void OnTotalArrayStorageActionActivated (object sender, System.EventArgs e)
  {
    Strint stint = R.TotalArrayStorageSize();
    JD.Display("ARRAY STORAGE", stint.S, true, false, false, "CLOSE");
  }

  protected virtual void OnShowPnemonicsActionActivated (object sender, System.EventArgs e)
  {
    string msg = "";
    if (Pnemonic != null && Pnemonic.Count > 0)
    { for (int i=0; i < Pnemonic.Count; i++)
      { msg += Pnemonic[i] + "\t-->\t" + PnemonicReplacemt[i] + '\n'; }
    }
    else msg = "No pnemonics available.";
    JD.Display("PNEMONICS", msg, false, false, false, "CLOSE"); // Don't allow markup text, as a couple of pnemonic tags are markup tags too.
  }

  //------------------------------------------------------------------------------
  //  RUN MENU                                            //run

  protected virtual void OnGOActionActivated (object sender, System.EventArgs e)
  { GO();  }

  protected virtual void OnToggleBreakPointActionActivated (object sender, System.EventArgs e)
  { int lineno = BuffAss.Text._CountChar('\n', 0, BuffAss.CursorPosition);
    JTV.TagLine(REditAss, currentbreak_text, lineno, '-', false, false);
    JTV.TagLine(REditAss, breakpoint_text, lineno, '~', false, false); // '~' --> toggle the tagging of the line.
    if (CurrentRunStatus > 0) // You can only reset internal breakpoints here if the program is running. If not running, then internal
                              // break points will only be set when GO is pressed (see 'GO()', where the JTV method below is also called).
    { PgmBreakPoints = JTV.LinesStartingWithThisTag(BuffAss, breakpoint_text, 0, -1); }
  }

  protected virtual void OnRemoveAllBreakPointsActionActivated (object sender, System.EventArgs e)
  { TextIter startIt = BuffAss.StartIter, endIt = BuffAss.EndIter;
    BuffAss.RemoveTag(currentbreak_text, startIt, endIt);
    BuffAss.RemoveTag(breakpoint_text, startIt, endIt);
    if (CurrentRunStatus > 0) // You can only reset internal breakpoints here if the program is running. If not running, then internal
                              // break points will only be set when GO is pressed (see 'GO()', where the JTV method below is also called).
    { PgmBreakPoints = JTV.LinesStartingWithThisTag(BuffAss, breakpoint_text, 0, -1); }
  }

  protected virtual void OnKillCurrentGraphsActionActivated (object sender, System.EventArgs e)
  { KillCurrentGraphs(); }

  public void KillCurrentGraphs()
  { Board.KillBoards(); // Kills all boards and their contained refs. to plots and graphs.
    Plots2D.Clear();
    Plots3D.Clear();
  }


  protected virtual void OnDonTClearResultsWindowAtGOActionActivated (object sender, System.EventArgs e)
  { PreserveResults = !PreserveResults;
    if (PreserveResults) DonTClearResultsWindowAtGOAction.Label = "Clear Results Window at 'GO'";
    else DonTClearResultsWindowAtGOAction.Label = "Don't Clear Results Window at 'GO'";
  }

  protected virtual void OnAbortRunActionActivated (object sender, System.EventArgs e)
  { if (CurrentRunStatus > 0) StopNow = 'A'; // otherwise don't do a lot.
  }



//--------------------------------------------------------
//  EXTRA MENU                                      //extra
  // All submenus, with names "0Action", "1Action", ... raise this handler:
  protected virtual void OnExtraActionActivated (object sender, System.EventArgs e)
  { string name = (sender as Gtk.Action).Name;
    int menuNo = (name._Extent(7))._ParseInt(-1);   if (menuNo < 0 || menuNo >= MaxExtraSubmenus) return;
    ExtraClicked = menuNo;
    return;
  }

  public void InitializeExtraMenus() // Called only once, at startup.
  { ExtraAction.Label = "";  ExtraAction.Visible = false;
    // Yes, I know there are more elegant ways of doing the following, but I doubt if any other way is nearly as fast.
    ExtraSubmenu[0] = this.ActionX0;   ExtraSubmenu[1] = this.ActionX1;   ExtraSubmenu[2] = this.ActionX2;
    ExtraSubmenu[3] = this.ActionX3;   ExtraSubmenu[4] = this.ActionX4;   ExtraSubmenu[5] = this.ActionX5;
    ExtraSubmenu[6] = this.ActionX6;   ExtraSubmenu[7] = this.ActionX7;   ExtraSubmenu[8] = this.ActionX8;
    ExtraSubmenu[9] = this.ActionX9;
    ExtraSubmenu[10] = this.ActionX10;   ExtraSubmenu[11] = this.ActionX11;   ExtraSubmenu[12] = this.ActionX12;
    ExtraSubmenu[13] = this.ActionX13;   ExtraSubmenu[14] = this.ActionX14;   ExtraSubmenu[15] = this.ActionX15;
    ExtraSubmenu[16] = this.ActionX16;   ExtraSubmenu[17] = this.ActionX17;   ExtraSubmenu[18] = this.ActionX18;
    ExtraSubmenu[19] = this.ActionX19;
    for (int i=0; i < MaxExtraSubmenus; i++) ExtraSubmenu[i].Label = ""; // This action is sufficient to make it invisible.
    ExtraClicked = -1;
  }
  public void InactivateExtraMenuSystem()
  { ExtraAction.Label = "";   ExtraAction.Visible = false;
    for (int i=0; i < MaxExtraSubmenus; i++) ExtraSubmenu[i].Label = ""; // This action is sufficient to make it invisible.
    ExtraClicked = -1;
  }
  /// <summary>
  /// <para>Resets Extra menu and submenu titles, leaving all visible.</para>
  /// <para>MainMenuTitle: If null or empty (after trimming), the preexisting menu title is unchanged.</para>
  /// <para>SubMenuTitles: If null or empty, the preexisting submenu set is unchanged. Also if any of its elements is empty
  ///  (after trimming), the whole preexisting submenu set is unchanged. Otherwise the number of strings in SubMenuTitles is the new
  ///  number of submenus, and SubMenuTitles holds their new titles.</para>
  /// </summary>
  public void SetExtraMenuTitles(string MainMenuTitle, string[] SubMenuTitles)
  { string ss;
    if (MainMenuTitle._Length() > 0) { ss = MainMenuTitle.Trim();  if (ss != "") ExtraAction.Label = ss; }
    int len = Math.Min(SubMenuTitles._Length(), MaxExtraSubmenus);
    ExtraAction.Visible = true;
    if (len > 0)
    { string[] trial = new string[len];
      for (int i=0; i < len; i++) { ss = SubMenuTitles[i].Trim();  if (ss == "") return;  trial[i] = ss; }
      for (int i=0; i < len; i++) { ExtraSubmenu[i].Label = trial[i];  ExtraSubmenu[i].Visible = true; }
      for (int i=len; i < MaxExtraSubmenus; i++) ExtraSubmenu[i].Visible = false;
    }
  }
    /// <summary>
    /// IsVisible[0] refers to the main menu item, the rest to its submenus. If SubVisible is null or empty, existing visibilities
    /// still apply. If it is too short, existing visibilities still apply for menus beyond its reach. If longer, excess ignored.
    /// </summary>
    public void SetExtraMenuVisibilities(bool[] IsVisible)
    { int subvis = IsVisible._Length();
      ExtraAction.Visible = IsVisible[0];
      int maxwell = Math.Min(MaxExtraSubmenus, subvis-1);
      for (int i=0; i < maxwell; i++) ExtraSubmenu[i].Visible = IsVisible[i+1];
      return;
    }
    /// <summary>
    /// For the returned array, [0] refers to the main menu item, the rest to its submenus.
    /// </summary>
    public bool[] GetExtraMenuVisibilities()
    { int len = ExtraSubmenu.Length;
      bool[] result = new bool[len+1];
      result[0] = ExtraAction.Visible;
      for (int i=0; i < len; i++) result[i+1] = ExtraSubmenu[i].Visible;
      return result;
    }

/// <summary>
///
/// </summary>
  public Boost[] QuerySubmenus()
  { Boost[] result = new Boost[MaxExtraSubmenus];
    for (int i=0; i < MaxExtraSubmenus; i++)
    { result[i] = new Boost(ExtraSubmenu[i].Visible, ExtraSubmenu[i].Label); }
    return result;
  }
/// <summary>
/// Out-of-range argument returns (false, "").
/// </summary>
  public Boost QuerySubmenu(int Which)
  { if (Which < 0 || Which >= MaxExtraSubmenus) return new Boost(false);
    return new Boost(ExtraSubmenu[Which].Visible, ExtraSubmenu[Which].Label);
  }

  //------------------------------------------------------------------------------
  //  HELP MENU                 //help!
  protected virtual void OnHelpMenusActivated (object sender, System.EventArgs e)
  { string cue = "//" + (sender as Gtk.Action).Label.ToUpper() + "//";
    var theLines = new List<string>();
    Boost boodle = Filing.LoadTextLinesFromFile(ProgramPath + "Help.txt", out theLines);
    if (!boodle.B) { JD.Msg("Unable to load help file. " + boodle.S);  return; }
    // The whole file has been read in, but we only want a part of it.
    int endPtr = -1, startPtr = 1 + theLines.IndexOf(cue);
    if (startPtr == 0) { JD.Msg("Unable to find the topic header '" + cue + "' in the help file");  return; }
    int noLines = theLines.Count;
    int n = startPtr;
    for (int i = n; i < noLines; i++)
    { if (theLines[i] != "" && theLines[i][0] == '/') startPtr++;  else break; }
    for (int i = startPtr+1; i < noLines; i++)
    { if (theLines[i]._Extent(0, 4) == "====") { endPtr = i;  break; }
    }
    if (endPtr == -1) endPtr = noLines;
    if (endPtr < noLines) theLines.RemoveRange(endPtr, noLines - endPtr); // lines above the desired segment go.
    if (startPtr > 0) theLines.RemoveRange(0, startPtr); // lines below the desired segment go.
    string[] stroo = theLines.ToArray();
    string helpFilePortion = String.Join("\n", stroo);

    // Display the stuff:
    JD.PlaceBox(0.6, 0.8, 0.65, 0.4);
    JD.Display("HELP FILE EXTRACT", helpFilePortion, true, true, false, "CLOSE");
  }

  //-----------------------------------------------------------------------------
  //  TECHNICAL MENU                                //technical

  protected virtual void OnVersionActionActivated (object sender, System.EventArgs e)
  { string pgmpathandname = System.IO.Path.GetFullPath(Environment.GetCommandLineArgs()[0]);
    FileInfo gen = new System.IO.FileInfo(pgmpathandname); // no checks, as was tested in F.RunSysFn(.).
    DateTime dt = gen.LastWriteTime;
    string stroo = MainWindowTitle + " was created on " + dt.ToLongDateString() + "  at  " + dt.ToShortTimeString();
    JD.Msg(stroo);
  }

  protected virtual void OnInternalDataActionActivated (object sender, System.EventArgs e)
  { if (R.Assts == null) JD.Msg("No assignments have been processed, so there is no data to display.");
    else R.Show("AVLSQ");
  }

  //------------------------------------------------------------------------------
  //  EXPERIMENTAL MENU                 //experimental
  void OnDoIt1ActionActivated (object sender, System.EventArgs e)
  { AddTextToREditRes("\u000A", "append");
    if (HistoryStack == null)
    { AddTextToREditRes("The history stack is empty", "append");
    }
    else
    { var sz = HistoryStack.Count;
      for (int i=0; i < sz; i++)
      { string ss = HistoryStack[i].OldStrip;
        if (!String.IsNullOrEmpty(ss))
        { if (ss[ss.Length-1] != '\u000A')  ss += "\u000A";
          AddTextToREditRes(ss, "append");
        }
      }
    }

    int dummy = 1;
  }

  protected virtual void OnDoIt2ActionActivated (object sender, System.EventArgs e)
  {


  }


//
// ==== NON-MENU COMPONENT EVENTS ===========================
// --------- WINDOW BUTTONS:                              //button  //btn

// THE GO BUTTON
  protected virtual void OnBtnGOClicked (object sender, System.EventArgs e)
  { GO();
  }


  protected virtual void OnBtnClearResClicked (object sender, System.EventArgs e)
  { BuffRes.Clear();   REditAss.GrabFocus();
  }

  protected virtual void OnBtnAbortClicked (object sender, System.EventArgs e)
  { if (CurrentRunStatus > 0) StopNow = 'A'; // otherwise don't do a lot.
  }

//-------------------------------------------------------------
// ---------- EVENT BOX underlaying RESULTS LABEL:

  protected virtual void OnEventbox1ButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
  { int X, Y;
    eboxLblRes.GetPointer(out X, out Y);
    ButtonDownY = Y;
  }

  protected virtual void OnEventbox1ButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
  { int X, Y, YChange = 0;
    eboxLblRes.GetPointer(out X, out Y);
    YChange = ButtonDownY - Y;
    vbox12.HeightRequest -= YChange; // vbox12 holds REditAss and the label BELOW it (not the label above it).
    vbox13.HeightRequest += YChange; // vbox13 holds REditRes and the buttons and label below it.
  }


//-----------------------------------------------------------------
// ------- UNIVERSAL KEY EVENTS                            //key
/// <summary>
/// <para>KeySnooper is fired by both the KeyPress and the KeyRelease events, distinguishable by eve.Type.</para>
/// <para>There isn't any documentation for the returned integer. By trial and error I have found out that a return
/// of 0 allows normal handling of the key to proceed, and of 1 (or apparently anything else) prevents same.</para>
/// <para>As to actual key values, see the non-XML notes at the top of this method.</para>
/// </summary>
// SHORT HOMILY ON KEY VALUES:
// Mono documentation is bl. useless wrt key values. Hunt for a listing of them online by hunting for the C header
// "gdkkeysyms.h" for same (at March 2013, saved in folder "~/Documents/Computing Documentation/GTK et al/GTK and GDK/" .
// If not, here is where it is as of March 2013: https://git.gnome.org/browse/gtk+/plain/gdk/gdkkeysyms.h
// <EventKey object>.KeyValue is an unsigned 32-bit integer ('uint'), encoding a 4-digit hex no. ( = what is listed in the C header).
// Some principles apparent from this list, and by experimentation using the remarked-out bit below:
// 1. For every key on the keyboard that bears two symbols, there are two KeyValues, the unshifted and the shifted version.
// 2. For every key that has only one symbol, there is only one KeyValue, irrespective of which helper key you press.
// 3. If you press a shifted key (e.g. capital A), the key snooper will first detect the shift key itself (0xffe1) and
//     then the 'A' key (0x0041); you cannot avoid that shift key value.
// 3.5  If the caps lock is on, unshifted A key returns 0x0041 (capital A), shifted A key 0x0061 (small A).
// 4. Other helper keys ctrl and alt do not change the final key value; e.g. for cntrl-shift-alt A, the non-helper key value
//     will still be as for just shift-A, i.e. 0x0041.
// 5. the 'Alt Gr' key (0xfe03) acts as a shift key also, for funny characters. Most of these start with 0x00, though a couple
//     start with 0xFE; I think all such are themselves modifier keys (e.g. typing AltGr-'=', and then a letter like 'c', will give
//     you a cedilla under the c).
// 6. In general, if something starts with "0x0", it is almost certainly a printable key; if with "0xE" or higher, almost certainly
//     a helper key, cursor-moving key, or anything but a printable key. Good enough for a prima-facie filter, but not absolute truth.
// 7. (int) <EventKey object>.State: loads of values, but all we need care about here are: nil = 0; shift = 1; caps-lock-on = 2;
//      cntrl = 4; alt = 8. All combinations of these keys: add them together. If caps lock is off, combo values are:
//      CS=5; CA=12; CSA=13; SA=9. If caps lock is on, add 2 to all of these.
//     AltGr has state 128.
// 8. NB!!! The value of 'rawkey' below is deceptively different to 'keyvalue'. E.g. for 'G', keyvalue is 71 (0x47) but rawkey is 42.
// Q: Why use raw key codes at all? A: because it is the only simple way to find which physical key was pressed; the cooked key value9
//     for two chars. served by the same physical key will be different. I can't think of a way to get around this which is
//     not very merky when you start to think how to institute it. If this proves a real problem when using other people's computers,
//     then will just have to use a merky method.
  public int KeySnooper(Widget widge, Gdk.EventKey eve)
  {/// KEEP THE FOLLOWING, for eliciting key values:
   //       this.Title = "Key value: 0x" + eve.KeyValue.ToString("X4") + ";  Key state: " + ((int)eve.State).ToString();
   //       return 0; // use '0' to stop the key from doing anything at all, or '1' to allow it to have its usual effect.
    int keyvalue = (int) eve.KeyValue;  int keytime = (int) eve.Time;
    int rawkey = (int) eve.HardwareKeycode;
//ThisWindow.AdjustText("LblComments", rawkey.ToString(), 'W');// *** Keep for showing raw key codes; no need to remark-out any other code.
    bool isKeyDown = (eve.Type == EventType.KeyPress);
    bool Cdown = false, Adown = false, Sdown = false, NoneDown = true;
    if (isKeyDown)
    { WhatKeyIsDown = keyvalue;
      if (WhatKeyIsDown != KeyQueue[0])
      {
        for (int i = KeyQueueLength-1; i > 0; i--) KeyQueue[i] = KeyQueue[i-1];
        KeyQueue[0] = WhatKeyIsDown;
        LastKeyCombo[0] = 0;
      }
     // TIMEOUTS:
      if (keytime - LastKeyDownTime > KeysDownTimeOut) // Time out! User took too long e.g. between prefix key and its follower.
      { PrefixKey = new Boost(false); }
      LastKeyDownTime = keytime;
      Cdown = ((eve.State & ModifierType.ControlMask) == ModifierType.ControlMask);
      Adown = ((eve.State & ModifierType.Mod1Mask) == ModifierType.Mod1Mask);
      Sdown = ((eve.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
      NoneDown = (!Cdown && !Adown && !Sdown);
      Cdown = ((eve.State & ModifierType.ControlMask) == ModifierType.ControlMask);
      Adown = ((eve.State & ModifierType.Mod1Mask) == ModifierType.Mod1Mask);
      Sdown = ((eve.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
      NoneDown = (!Cdown && !Adown && !Sdown);
    }
    else
    { WhatKeyIsDown = 0;
//######vvvvvvvvvvvvvv
      if (KeyQueue[0] != 0)
      {
        for (int i = KeyQueueLength-1; i > 0; i--)
        { KeyQueue[i] = KeyQueue[i-1];
          LastKeyCombo[i] = KeyQueue[i]; 
        }
        KeyQueue[0] = 0;
        LastKeyCombo[0] = 1;
      }
//#####^^^^^^^^^^^^^^
    }
  // Key presses relating to Board objects:
    if (!REditAss.HasFocus && !REditRes.HasFocus) // then maybe the board is focussed? Hope so. (But it could be a form button...)
    { if (isKeyDown)
      { int whichboard = Board.FocussedBoard();
        if (whichboard > 0)
        { Board.RegisterKeyPress(whichboard, keyvalue, keytime); // Stores it on a stack, and also stimulates response if appropriate.
          // When a graph is in focus, the general rule is that key presses should fall through to the inbuilt system handlers, once registered
          //  with Board. This is achieved by 'return 0' here. If we definitely don't want the key handled by the system handlers, we would
          //  use 'return 1'. [At the time of writing, there are none such.] There is a third category: those which we want handled not only by
          //  the focussed graph but also by one of the key combinations programmed in below. These will fall through to the next section (no 'return').
          // if (...) return 1; *** Put something here if wanting keys not to be handled by system handlers after being available to the graph.
          if ( (keyvalue == 103 && Cdown && Adown) // ctrl-alt-G (kill all graphs) // *** Even though this is a main menu hot key, it won't operate
                                              //  because the main window is not in focus; hence we cannot just return 0 and expect the menu
                                              //  hot key handler to take over the keypress. (Many other keypresses will still work, though.)
            || (keyvalue == 0xFFC2 && Cdown) ) // ctrl-F5 (stop program). This one doesn't need a separate handler below because it gets caught
                                               //  by the F5 handler below. As it then enters GO(.) with CurrentRunStatus nonzero, GO(.) aborts the run.
          { // do nothing - just fall through to the next block of code
          }
          else return 0; // Keys not in the above list are handled by the system handlers.
        }
      }
      else return 0; // No interaction with keypresses where neither REditAss nor a graphing board is focussed.
    }
  // ------------------------------------------------------------------------
  // ACTIONS FOR KEY-DOWN: (ALL HANDLED ACTIONS RETURN immediately; the final 'return 0' is only for keys not triggering an action.
    if (isKeyDown)
    {// FUNCTION KEYS
      if (keyvalue >= 0xFFBE && keyvalue <= 0xFFC9) // a function key, F1 to F12:
      {// F1: Hints text and local function header search
        if      (keyvalue == 0xFFBE) { F1KeyHandler();  return 1; } // No further processing of the key, thanks.
       // F2: *** Currently the hot-key for menu item "Appearance | Toggle Markup..". Note that whatever key is chosen for that,
       //          you have to allow for it specifically in OnREditAssKeyReleaseEvent(.), to block a further destructive recolour.
//        else if (keyvalue == 0xFFBF) { F2KeyHandler();  return 1; } // No further processing of the key, thanks.
       // F5: Go.
      }
    // RECORD A COMPOUND KEY:
      if ((Cdown || Adown) && UsableAsAffixKey(rawkey)) // helper keys are down, and a printable char. is now being keyed:
      { string ss = ""; // Possible values will be "C","A","CA","CS","AS", "CAS" (but not just "S").
        if (Cdown) ss += 'C';   if (Adown) ss += 'A';  if (Sdown) ss += 'S';
        if (ss.Length > 0) // then maybe this is a special key combination
        { ss += '_' + rawkey.ToString();
          int actionCode;
          bool success = SpecialKeys.TryGetValue(ss, out actionCode); // *** BREAKPOINT HERE (next line), TO WORK OUT A RAW KEY VALUE
          if (success)  // Note that standard Gtk system keys like "Ctrl-F", "Ctrl-S" have been left out of SpecialKeys; these will return 
                        // success = false, so that 'return 0' 5 lines below will occur, allowing them to carry out their Gtk system function.
          { if (actionCode == -1) PrefixKey = new Boost(true, ss);
            else { DoSpecialAction(actionCode);  return 1; }
          }
        }
        return 0; // Allow normal system handling, if no return above. (Indeed, a prefix key may both trigger a system action
                  // and also be a prefix key, though I am not sure why you would want this to happen.)
      }
     // RECOGNIZE KEY FOLLOWING A PREFIX KEY:
      else if (NoneDown && UsableAsAffixKey(rawkey)) // then helper keys are down, and a printable char. is now being keyed:
      { if (PrefixKey.B)
        { PrefixKey.S += '_' + rawkey.ToString();
          int actionCode;
          bool success = SpecialKeys.TryGetValue(PrefixKey.S, out actionCode);
          PrefixKey = new Boost(false); // Whether recognized or not, reset it.
          if (success) { DoSpecialAction(actionCode);  return 1; }
        }
        PrefixKey = new Boost(false);
        return 0;
      }
     // RESPONSE TO TAB KEY: (keyvalue is 0xff09 for plain tab, 0xfe20 for shift-tab; cntrl and alt have no effect.)
      if (keyvalue == 0xff09 && isKeyDown) // TAB: Indent selected lines, if there is a selection; otherwise let system handle the key.
      { if (PrefixSelectedLines("\t", false)) return 1; // Prefixing occurred, so block the key from further actions.
        else return 0; // allow the key to do its usual thing.
      }
      else if (keyvalue == 0xfe20 && isKeyDown) // SHIFT-TAB Unindent selected lines, if there is a selection; otherwise let system handle the key.
      { if (UnPrefixSelectedLines("\t", false)) return 1; // Prefix removal was at least attempted, so block the key from further actions.
        else return 0; // No focus, or no selected text, so allow the key to do its usual thing.
      }
    }
  // ------------------------------------------------------------------------
  // ACTIONS FOR KEY-UP:
    else
    {

    }
  // ------------------------------------------------------------------------
    return 0;
  }

  private static int[] BannedAffixKeyCodes = new int[] {22, 23, 36, 37, 49, 50, 62, 63, 64, 66}; // These are
  // 'raw' hardware keycodes which are in the range 10 to 76 but not available as either (a) part of a single-key
  // PREFIX control code (like 'Cntrl-Alt-<char>') or as (b)the 2nd. (SUFFIX) of a two-key control code
  // (like 'Cntrl-C, <char>'). Note that all values below 10 and above 76 are banned.
/// <summary>
/// Returns TRUE if the 'raw' hardware key code is suitable to be either part of a standalone control code (e.g. Cntrl-n)
/// or as part of a suffix - the second of a double-key code (e.g. Cntrl-A, n).
/// </summary>
  private bool UsableAsAffixKey(int HardwareKeyCode)
  { if (HardwareKeyCode < 10 || HardwareKeyCode > 76) return false;
    return (BannedAffixKeyCodes._IndexOf(HardwareKeyCode) == -1);
  }
/// <summary>
/// Done once at startup. *** Later versions may allow the user to tinker with special keys, in which case the INI file would
/// have to have a reference available for such changes.
/// </summary>
  private void SetUpSpecialKeys()
  {// **** NB: When adding new keypresses, realize that for better or for worse, the combinations below use RAW KEY NOS., which
   //  relate to keyboard position, not logic key codes. Therefore the remarked-out start to 'KeySnooper(.)' will not help when you
   //  want to find out a raw key no. Instead, search for the line in KeySnooper which has this exact wording in its remark:
   // "BREAKPOINT HERE"; put a breakpoint there, and then run the pgm and key your key in a combo with either or both of Cntl and Alt.
   //  Then when operation stops at the breakpoint, read the value of rawkey (occurs two lines above the breakpoint line).
   //  This has the advantage also that you can check whether your keypress is unused, by seeing what subsequently happens.
    SpecialKeys = new Dictionary<string, int>();
    // ONE-PRESS COMBINATIONS: (dictionary int value below 10000)
    SpecialKeys.Add("C_57", 100); // Ctrl-N - expand pnemonic to left of cursor.
    SpecialKeys.Add("CS_57", 110); // Ctrl-shift-N - make selection to next LF, then proceed as for Ctrl-N above.
    SpecialKeys.Add("CA_42", 120); // Ctrl-Alt-G - close down all graphs. (Is recorded as a menu hot key, but if used while a graph is in
                                   //   focus, the hot key will not work - see remark in KeySnooper(.) re same. So key combination must go here.)
    // Move-to-Bookmark keys, cntrl-shift-0 to -9:
    SpecialKeys.Add("CS_19", 200); // Out of the loop below because rawkey for 0 is above that for 9, but I want indexes to rise from 0 to 9.
    for (int i=1; i <= 9; i++)
    { SpecialKeys.Add("CS_" + (i+9).ToString(), 200 + i); } // ctrl-Shift-1 to 9 (returning 201 to 209)
    // Move-to-Word-Instance keys, cntrl-0 to -9: (N = 1 to 9: go to Nth. occurrence; N = 0: go to occurrence where search started.)
    SpecialKeys.Add("C_19", 220); // Out of the loop below for the same reason as mentioned above.
    for (int i=1; i <= 9; i++)
    { SpecialKeys.Add("C_" + (i+9).ToString(), 220 + i); } // ctrl-1 to 9 (returning 221 to 229)
    // Move-to-Word-Instance keys, occurrences 10 to 19: cntrl-Alt-0 to -9:
    SpecialKeys.Add("CA_19", 230); // Out of the loop below for the same reason as mentioned above.
    for (int i=1; i <= 9; i++)
    { SpecialKeys.Add("CA_" + (i+9).ToString(), 230 + i); } // ctrl-1 to 9 (returning 230 to 239)
    SpecialKeys.Add("CS_42", 300); // Ctrl-Shift-G - Turn char. to the left into a Greek letter
    SpecialKeys.Add("CS_43", 301); // Ctrl-Shift-H - Turn char. to the left into a Maths character
    SpecialKeys.Add("CS_44", 302); // Ctrl-Shift-J - Turn char. to the left into a Maths character

   // TWO-PRESS COMBINATIONS - You have to register the primary key combo in (1) below, then deal with the second key in (2) below.
   // (1) REGISTER THE PREFIX: (dictionary int value -1) [NB - for a 2-key pair, the prefix must go here AND the combo in next block below]
    SpecialKeys.Add("C_56", -1); // Ctrl-B - a prefix key (for bookmarking with a small block remark)
    SpecialKeys.Add("CS_53", -1); // ctrl-shift-X - a prefix key (for removing all instances of a particular bookmark)
    SpecialKeys.Add("CS_33", -1); // Ctrl-Shift-P - a prefix key (for toggling pausability of 'pause(0 to 9)'.

   // (2) THE FULL TWO-PRESS COMBINATIONS - PREFIX + SUFFIX: (dictionary int value 10,000+)
    // Go to bookmark 0 to 9:
    SpecialKeys.Add("C_56_19", 10000); // ctrl-B then 0 (returning 10000)
    for (int i=1; i <= 9; i++) { SpecialKeys.Add("C_56_" + (i+9).ToString(), 10000 + i); } // ctrl-B then 1 to 9 (returning 10001 to 10009)
    SpecialKeys.Add("C_56_20", 10050); // ctrl-B then '-' (eliminates all bookmarks)
    SpecialKeys.Add("CS_53_19", 10060); // ctrl-shift-X then 0 (returning 10060)
    for (int i=1; i <= 9; i++) { SpecialKeys.Add("CS_53_" + (i+9).ToString(), 10060 + i); } // ctrl-shift-X then 1 to 9 (returning 10061 to 10069)
    SpecialKeys.Add("CS_33_19", 10100); // ctrl-shift-P then 0 (returning 10100)
    for (int i=1; i <= 9; i++) { SpecialKeys.Add("CS_33_" + (i+9).ToString(), 10100 + i); } // ctrl-shift-P then 1 to 9 (returning 10101 to 10109)
  }

  private void DoSpecialAction(int actionCode)
  {
   // ONE-KEY CODES (i.e. Control &/or Alt &/or Shift + printable key together)
    if (actionCode == 100) ExpandPnemonic(false); // false = don't force selection
    else if (actionCode == 110) ExpandPnemonic(true); // true = if no selection, force one to the next LF (or end of text).
    else if (actionCode == 120) KillCurrentGraphs();
    else if (actionCode >= 200 && actionCode <= 209)
    { string texto = "/*" + (actionCode - 200).ToString() + "*/";
      string buffText = BuffAss.Text;
      int find = -1;
      int startPtr = BuffAss.CursorPosition;
      // Search from cursor to the end; if no find, search from the beginning, thus looping the search around.
      find =                 JTV.FindTextAndTagIt(BuffAss, texto, ref buffText, null, startPtr, 0, false, true, P.IdentifierChars);
      if (find == -1) find = JTV.FindTextAndTagIt(BuffAss, texto, ref buffText, null, 0,        0, false, false, P.IdentifierChars);
      // If a find, get the line halfway up the screen, if possible:
      if (find >= 0)
      { TextIter buffStartIt, buffEndIt;
        BuffAss.GetBounds(out buffStartIt, out buffEndIt);
        TextIter thisIt = buffStartIt;
        thisIt.ForwardChars(find);
        REditAss.ScrollToIter(thisIt, 0.1, true, 0.0, 0.5); // '0.5' to get the line halfway up the screen, if possible.
        // *** If the above fails - as it did in OnReplaceActionActivated(.) - q.v. - you have to use REditAss.ScrollToMark(.) instead.
        BuffAss.PlaceCursor(thisIt); // *** If it is ever found a pest that the cursor moves from its prior position, remark this line out.
      }
    }
    else if (actionCode >= 220 && actionCode <= 239) GoToWordOccurrence(actionCode - 220);
    else if (actionCode >= 300 && actionCode <= 302) ChangeCharToLeft(actionCode - 300);

// TWO-KEY CODES:
    else if (actionCode >= 10000 && actionCode <= 10009) // Insert bookmark:
    { BuffAss.InsertAtCursor("/*" + (actionCode - 10000).ToString() + "*/");
      RecolourAllAssignmentsText("leave all");
    }
    else if (actionCode == 10050)
    { OnRemoveAllBookmarksActionActivated (actionCode, System.EventArgs.Empty); } // 1st. arg is a dummy, to fit delegate arg. "object sender".
    else if (actionCode >= 10060 && actionCode <= 10069) // Insert bookmark:
    { OnRemoveThisBookmarkActionActivated (actionCode - 10060, System.EventArgs.Empty); }
    else if (actionCode >= 10100 && actionCode <= 10109) // Toggle pausability:
    { F.CanPause[actionCode - 10100] = !F.CanPause[actionCode - 10100]; }
  }

  /// <summary>
  /// If DoWhat is '?' (or anything else except 'V' or 'I'), returns visibility status of the given menu
  ///  ('MenuName' being case-sensitive, must have been trimmed): +1 if visible, 0 if not, -1 if the name was not identified (error).
  ///   If DoWhat is 'V', the menu is made visible, and the return is +1 (or -1, for error).
  ///  If DoWhat is 'I', the menu is made invisible, and the return is 0 (or -1, for error).
  /// SPECIAL CASE: If MenuName is exactly "ALL", and DoWhat is 'V' or 'I', then that action is applied to all menus.
  /// All menus are reset to full visibility at the end of the run.
  /// </summary>
  public int MenuVisibility(string MenuName, char DoWhat)
  {
    string[] mainMenuNames = new string[]
          {"File", "Reload", "Edit", "Search", "Appearance", "Show", "Run", "Help", "Technical", "Experimental"};
    Gtk.Action[] acts = new Gtk.Action[] { FileAction, ReloadAction, EditAction, SearchAction, AppearanceAction,
                                    ShowAction, RunAction, HelpAction, TechnicalAction, ExperimentalAction };
    if (MenuName == "ALL")
    { if (DoWhat == 'V')
      { for (int i=0; i < acts.Length; i++) acts[i].Visible = true;
        return 1;
      }
      else if (DoWhat == 'I')
      { for (int i=0; i < acts.Length; i++) acts[i].Visible = false;
        return 0;
      } // If DoWhat is anything else, focus falls through into the next section, where 'ALL' raises an error.
    }
    int thisfind = -1;
    for (int i=0; i < acts.Length; i++)
    { if (MenuName == mainMenuNames[i])
      { thisfind = i;  break;  }
    }
    if (thisfind == -1) return -1;
    if (DoWhat == 'V') { acts[thisfind].Visible = true;  return 1; }
    else if (DoWhat == 'I') { acts[thisfind].Visible = false; return 0; }
    else { if (acts[thisfind].Visible) return 1; else return 0; }
  }


/// <summary>
/// <para>Returns in .SX the 'word' at the cursor, and in .SY an indicator of there being a selection (if so, .SY = "S"; if not, .SY is empty "").
///   .IX and .IY point to the first and last characters of the identified 'word' in BuffAss. As to the identifying of a 'word':</para>
/// <para>Case 1: If there is a selection, then the 'word' is simply whatever is selected, no matter what that is.</para>
/// <para>Case 2: If there is no selection, a 'word' is defined as a continuum consisting only of A..Z, a..z, 0..9, '_'; moreover, if
///   the boolean argument is 'true', the first char. cannot be a numeral. The word will be found if the cursor is inside it, or is
///   touching its start or its end.</para>
/// <para>Where no word is found (only possible if there is no selection), then .IX and .IY both = -1, and .SX is "".</para>
/// </summary>
  public Strint2 WordAtCursor(bool NoStartDigit)
  {// Collect the text:
    Strint2 result;
    string txt = "";
    TextIter selPtStart, selPtEnd;
    BuffAss.GetSelectionBounds(out selPtStart, out selPtEnd);
    // SELECTION exists:
    if (selPtStart.Offset != selPtEnd.Offset)
    { result = new Strint2 (selPtStart.Offset, selPtEnd.Offset-1,  BuffAss.GetText(selPtStart, selPtEnd, true), "S"); }
    else // NO SELECTION:
    {// Get stuff 40 chars. to left and 40 chars. to right:
      int mgn = 40, selptr = selPtStart.Offset, alteredselptr = 40;
      TextIter excerptStart = selPtStart, excerptEnd = selPtStart;
      if (selptr <= mgn) { excerptStart = BuffAss.StartIter;  alteredselptr = selptr; }
      else excerptStart.BackwardChars(mgn);
      if (selptr + mgn >= BuffAss.EndIter.Offset) excerptEnd = BuffAss.EndIter;
      else excerptEnd.ForwardChars(mgn);
      txt = BuffAss.GetText(excerptStart, excerptEnd, true); // 'true' = return hidden characters
      // Now go back to the first identifier-type char:
      StringBuilder stew = new StringBuilder(txt);
      string[] strudel = BuffAss.Text.Split(new char[] {'\u000D', '\u000A'} );
      C.ProcessDirectiveALLOW(ref strudel, true);
      string idchars = P.IdentifierChars;
      int newstart = 0; // value to apply (a) if the loop below is never entered (selptr = 0), or (b) if its 'if' condition
                        // is never met (valid chars. all the way to the start).
      for (int i = alteredselptr-1; i >= 0; i--)
      { if (idchars.IndexOf(stew[i]) == -1) { newstart = i+1;  break; } }
      int stewlen_1 = stew.Length - 1, newend = stewlen_1; // value to apply in sitns. analagous to (a) and (b) for newstart.
      for (int i = alteredselptr; i <= stewlen_1; i++)
      { if (idchars.IndexOf(stew[i]) == -1)
        { newend = i-1;  break; }
      }
      string ss = stew.ToString();
      ss = ss._FromTo(newstart, newend);
      if (ss == "") return new Strint2(-1, -1, "", "");
      int ptr = excerptStart.Offset + newstart;
      result = new Strint2(ptr, ptr + ss.Length - 1, ss, "");
      if (NoStartDigit && JS.Digits.IndexOf(ss[0]) >= 0) // If NoStartDigit was set, then e.g. "5AB" would be disallowed.
      { result = new Strint2(-1, -1, "", ""); }
    }
    return result;
  }

  private void F1KeyHandler()
  {// Collect the text:
    Strint2 striddle = WordAtCursor(true);
    string stroo = striddle.SX;  if (stroo == "") return;
    int wordFirstPtr = striddle.IX;
  // KEY WORD covered in "Hints.txt"?
    string strooloo = stroo.ToLower();
    if (stroo == strooloo) // If there are any capital letters, then it can't be a keyword.
    { string hint = RetrieveHint(strooloo);
      if (hint != "")
      { if (LastF1BoxPosition.X1 < 0)  JD.PlaceBox(900, 400, Screen.Width - 475, 25); // Top right corner is 25 pixels each way from screen's t.r.cnr.
        else JD.PlaceBox(LastF1BoxPosition.X1, LastF1BoxPosition.X2, LastF1BoxPosition.X3, LastF1BoxPosition.X4);
        JD.Display("HINT FOR '" + stroo + "'", hint, true, true, false, "CLOSE");
        LastF1BoxPosition = new Tetro(JD.LastBoxSize.X, JD.LastBoxSize.Y, JD.LastBoxCentre.X, JD.LastBoxCentre.Y);
        return;
      }
    }
   // VARIABLE of user's program?
    if (P.UserFn == null) return; // User hasn't run the program yet, so no dice.
    int noFns = P.UserFn.Length;  if (noFns == 0) return; // User hasn't run the program yet, so no dice.
    if (noFns > 1) // Set OpenerPtr and CloserPtr for all functions, every time (as the user may have added text, offsetting previous values)
    // (With a large neuroscience program with loads of functions, searches took around 1/3 sec. to display, so the code below is not a time problem.)
    { for (int i = 1; i < noFns; i++)
      { TFn funky = P.UserFn[i];
        int pOpenerLineStart = 1 + BuffAss.Text._IndexOfNth("\n", funky.LFBeforeOpener); 
        int nn = funky.LFBeforeCloser - funky.LFBeforeOpener;
        int pCloserLineStart;
        if (nn == 0) pCloserLineStart = pOpenerLineStart;
        else pCloserLineStart = 1 + BuffAss.Text._IndexOfNth("\n", nn,  pOpenerLineStart);
        int pp = BuffAss.Text._IndexOf('{', pOpenerLineStart);
        int qq = BuffAss.Text._IndexOf('}', pCloserLineStart);
        if (pp > 0 && qq > pp)  { funky.OpenerPtr = pp;   funky.CloserPtr = qq;  }
        else 
        { JD.Msg("Unable to use variable value display via F1 key, as there is a problem with user function data - call programmer.");
          return;
        }
      }
    }
    // LOCATE THE FUNCTION which holds this word:
    int thisFnNo = 0;
    for (int i = 1; i < noFns; i++) // If the cursor is not in any function at all, then thisFnNo will remain indicating the main pgm.
    { TFn funko = P.UserFn[i];
      if (wordFirstPtr > funko.OpenerPtr  &&  wordFirstPtr < funko.CloserPtr) // Then this is our function!
      { thisFnNo = i;  break; }
    }
    // LOOK FOR THE VARIABLE within this function:
    int hereitis = V.FindVar(thisFnNo, stroo);
    if (hereitis == -1) // then maybe it is a constant, that has been mentioned in function "thisFnNo"...
    { int whereisit = V.FindVar(0, stroo);
      if (whereisit > 0)
      {  byte use = V.GetVarUse(0, whereisit);
        if (use == 1) // then this is a constant, so should go through to the stuff below, as if it were a main program variable:
        { thisFnNo = 0;  
          hereitis = whereisit;
        }
      }
    }
    if (hereitis >= 0)
    { TFn funny = P.UserFn[thisFnNo];
      byte use = V.GetVarUse(thisFnNo, hereitis);
      bool isConstant = (use == 1);
      string header = "<b>" + stroo + "</b>";
      if (isConstant) header += " (global constant)";
      else if (thisFnNo == 0) header += " (in main program)";
      else header += " (in function '" + funny.Name + "')";
      if (use < 10) // a scalar:
      { if (use == 0)  header += ": Unassigned scalar";
        else // Normally just display the value; but if there are system lists, and if the value is integral and within range,
             // have a second button offering a display of the list.
        { header += " = ";
          double val = V.GetVarValue(thisFnNo, hereitis);
          if (val == Math.Round(val) && F.Sys != null && val >= 0.0 && val < (double) F.Sys.Count)
          { int valInt = (int) val;
            int btn = JD.DecideBox("", header + val.ToString(), "SYS LIST", "CLOSE");
            if (btn == 1)
            { List<double> diddle = F.Sys[valInt].LIST;
              if (diddle.Count == 0)
              { JD.Msg("System List " + valInt.ToString() + " is empty."); }
              else
              { double[] doodad = F.Sys[valInt].LIST.ToArray();
                //Create a temporary array, display it, then destroy it:
                int slut = V.GenerateTempStoreRoom(doodad.Length);
                R.Store[slut].Data = doodad;
                DisplayArrayWithOptions(slut, "System List",  valInt.ToString());
                R.Store[slut].Demolish();     R.StoreUsage[slut] = 0;
              }
            }
          }
          else
          { header += V.GetVarValue(thisFnNo, hereitis).ToString();
            header += "\n\n";
            JD.Msg(header);
          }
        }
      }
      else // array:
      { if (use != 11) JD.Msg(header + ":\nUnassigned, as function " + funny.Name + " is not currently running.");
        else
        { string varName = V.GetVarName(thisFnNo, hereitis);
          int slot = (int) V.GetVarPtr(thisFnNo, hereitis);
          DisplayArrayWithOptions(slot, "Array", varName);
        }
      }
    }
  }
  // Services the above routine, and also the display of lists.
  // 'VarType' is text for display; typically it would be "Array" or "System List"; it will be followed by a space, then the VarName.
  // 'VarName' should be the array name ("MyArr") or the list no. ("2").
  // NB!! No error checks!!
  public void DisplayArrayWithOptions(int Slot, string VarType, string VarName)
  { double wide = 0.55, high = 0.8, centreX = 0.67, centreY = 0.5; // Initial box dimensions and placement.
    string origHeader = VarType + " <b>" + VarName + "</b>";
    string header = origHeader;
    string captured = ""; // addends to the header
    StoreItem stem = R.Store[Slot];
    string CharsOrNumbers; // Can have 3 values: "N" - numbers; "C" - chars., ignoring formatting tags; "CF" - chars, respecting formatting tags.
    if (stem.IsChars) CharsOrNumbers = "CF"; else CharsOrNumbers = "N";
    double[] data = stem.Data;
    int nodims = stem.DimCnt, datalen = data.Length;
    int[] dimsz = stem.DimSizes; // length is exactly the no. of dimensions
    int[] lowLimits = new int[nodims], highLimits = dimsz._Copy(); // highlimits[i] has to be 1 beyond the last entry of the dimension.
    bool showReducedSizeWarning = false; // Will cause a warning to be added to the heading of the data display.
    int maxDataBolus = 10000; // **** THIS PARAMETER CAN BE ALTERED; it is a reasonable compromise between display time and amt. data to show.
    string ss;
    if (datalen > maxDataBolus)
    { string tt, uu, vv;  if (nodims == 1) tt = "a subrange of values"; else tt = "subranges of values";
      ss = "It may take a long time to prepare the whole structure of " + datalen.ToString() + " elements for display.\n" +
           "You can choose " + tt + " to display, to avoid this.\n" +
           "To ignore this warning and display all values, click 'THE LOT'.";
      // Prepare the labels for text boxes, initially as a string using '|' as delimiter for separate texts:
      if      (nodims == 1)
      { uu = (datalen-1).ToString();  vv = "First element (0 to " + uu + "):|Last element (0 to " + uu + "):"; }
      else if (nodims == 2)
      { uu = (dimsz[1]-1).ToString();   vv =  "First row (0 to " + uu + "):|Last row (0 to " + uu.ToString() + "):|";
        uu = (dimsz[0]-1).ToString();   vv += "First column (0 to " + uu + "):|Last column (0 to " + uu + "):";
      }
      else // more than 2 dimensions:
      { uu = (dimsz[nodims-1]-1).ToString();
                vv =  "Outer dimension start (0 to " + uu + "):|Outer dimension end (0 to " + uu.ToString() + "):|";
        for (int i = nodims-2; i > 0; i--)
        { uu = (dimsz[i]-1).ToString();
                vv +=  "Next dimension start (0 to " + uu + "):|Next dimension end (0 to " + uu.ToString() + "):|";
        }
        uu = (dimsz[0]-1).ToString();
                vv +=  "Inner dimension start (0 to " + uu + "):|Inner dimension end (0 to " + uu.ToString() + "):";
      }
      string[] prompts = vv.Split(new char[] {'|'});
      // Initial box texts are offered for 1 and 2-dim structures only:
      string[] answers = null;
      if (nodims == 1) { answers = new string[2];  answers[0] = "0";   answers[1] = (maxDataBolus-1).ToString(); }
      else if (nodims == 2)
      { answers = new string[4];
        int maxRowsToShow = 20; // **** THIS PARAMETER CAN BE ALTERED; it is the suggested max. no. rows offered to the user.
        answers[0] = answers[2] = "0";
        if (dimsz[1] <= maxRowsToShow)
        { answers[1] = (dimsz[1]-1).ToString();  answers[3] = ( maxDataBolus / dimsz[1] - 1).ToString(); }
        else
        { answers[1] = (maxRowsToShow-1).ToString();
          int n = maxDataBolus / maxRowsToShow;   if (n > dimsz[0]) n = dimsz[0];
          answers[3] = (n - 1).ToString();
        }
      } // If nodims > 2, then 'answers' remains NULL.
      int p, q, baton = JD.InputBox("LARGE STRUCTURE", ss, prompts, ref answers, new string[] {"THE LOT", "SMALLER", "CANCEL"});
      if (baton == 0 || baton == 3) return; // Don't continue with display, as user cancelled it.
      answers = answers._Purge(); // all array strings will have had blanks removed.
      if (baton == 2) // (If it is 1, don't do anything - display the lot.)
      { for (int dim = 0; dim < nodims; dim++)
        { p = answers[2*(nodims - dim - 1)]._ParseInt(-1);    if (p >= 0 && p < dimsz[dim]) lowLimits[dim] = p; // else it stays as 0.
          p = lowLimits[dim];
          q = answers[2*(nodims - dim - 1)+1]._ParseInt(-1);
          if (q < p ) highLimits[dim] = p+1; // if q is below p, just display the one row p.
          else if (q < dimsz[dim]) highLimits[dim] = q+1; // if q is beyond the dimension size, highLimits[dim] remains as the dim. size.
          // Will the displayed array be only a segment of the whole array?
          showReducedSizeWarning = ( showReducedSizeWarning || lowLimits[dim] > 0 || highLimits[dim] < dimsz[dim] );
        }
      }
    }

   // DISPLAY
    string ttt;
    if (nodims == 1) ttt = "  one-dimensional;  length " + dimsz[0].ToString();
    else
    { ttt = "  dims.  " + dimsz[nodims-1].ToString();
      for (int i = nodims-2; i >= 0; i--) ttt += " x " + dimsz[i].ToString();
    }
    header += " -- " + ttt;
    if (showReducedSizeWarning) header += "  (portion only)";
    // Max/Min addend to the header:
    string ss1, tt1;
    double x, MaxVal = double.MinValue, MinVal = double.MaxValue;
    int MaxAt = 0, MinAt = 0;
    for (int i=0; i < datalen; i++)
    { x = data[i];
      if (x > MaxVal) { MaxVal = x;  MaxAt = i; }
      if (x < MinVal) { MinVal = x;  MinAt = i; }
    }
    ss1 = "[" + F.CoordsOfAbsAddress(dimsz, nodims, MaxAt, true)._ToString(", ") + "]";
    tt1 = "[" + F.CoordsOfAbsAddress(dimsz, nodims, MinAt, true)._ToString(", ") + "]";
    header += "  <# magenta>(Min: " + tt1 + " = " + MinVal.ToString() + ";  Max: " + ss1 + " = " + MaxVal.ToString() + ")";
    // DISPLAY PARAMETERS ALTERABLE BY THE USER: (The prefix 'Run' = fixed for this pgm run, unless altered by user; prefix 'Orig' = machine defaults.)
    string StringFmt = RunStringFmt;
    string Delimiter = RunDelimiter;
    int TabStep = RunTabStep;
    bool ShowColumnNos = RunShowColumnNos;
    bool formatTagsApply = true;
    bool aschars;
    while (true) // Loop to handle button-clicks on the display.
    {
      if (CharsOrNumbers == "C") { aschars = true;  formatTagsApply = false; }
      else if (CharsOrNumbers == "CF") { aschars = true;  formatTagsApply = true; }
      else { aschars = false;  formatTagsApply = true; }
      Boost boo = V.StoreroomDataFormatted(data, dimsz, lowLimits, highLimits, aschars, formatTagsApply, true, ShowColumnNos,
                                                                              StringFmt, Delimiter, "", CharsOrNumbers, TabStep.ToString());
      JD.PlaceBox(wide, high, centreX, centreY);
      int btn = JD.Display("VARIABLE CONTENT", header + captured + "\n\n" + boo.S, formatTagsApply, false, false, "HELP", "CAPTURE", "FORMAT", "@ A 65",
                              "COMMAS", "TABS", "COL No.", "!CLOSE"); // The '!' in the last title causes that button to receive focus.
      if (btn == 0 || btn == 8) break; // user cancelled, by CLOSE button or by corner icon.
      if (btn == 1) // HELP
      { wide = JD.LastBoxSize.X;   high = JD.LastBoxSize.Y;   centreX = JD.LastBoxCentre.X;   centreY = JD.LastBoxCentre.Y;
          // These values will be used in the JD.PlaceBox(.) call just above, at the next loop.
        ss = "<b><# blue>BUTTONS OF THE ARRAY DISPLAY WINDOW:</b>\n<left 30><in -30>" +
             "<b><# black>CAPTURE</b> -- saves the array in a form that can be accessed from another <i>MonoMaths</i> instance using function 'captured()'.\n" +
             "<b>FORMAT</b> -- the formatting instructions recognized by C#. Examples available in the message box that opens.\n" +
             "<b>@ A 65</b> -- toggles display of: Chars. (interpreting format tags); Chars. (not interpreting format tags); Numbers.\n" +
             "<b>COMMAS</b> -- the default: column values are separated by ', '. Used to cancel 'TABS' (next button).\n" +
             "<b>TABS</b> -- column values separated by tabs. Starting at step 50, successive clicks increase tab step by 50.\n" +
             "<b>COL.No.</b> -- show / hide the lowest dimension index for each value." +
             "\n\n<left 0><in 0><b><# BLUE>DEFAULT SETTINGS FOR FUTURE ARRAY DISPLAYS:</b>\n<left 30><in -30>" +
             "<# black>To use current settings, click 'Remember'; to return to start-up defaults, click 'Forget'.";
        int nutherBtn = JD.Display("HELP FOR DISPLAY OF ARRAYS", ss, true, true, false, "REMEMBER", "FORGET", "CLOSE");
        if (nutherBtn == 1)
        { RunStringFmt = StringFmt;  RunDelimiter = Delimiter;   RunTabStep = TabStep;  RunShowColumnNos = ShowColumnNos; }
        else if (nutherBtn == 2)
        { RunStringFmt = OrigStringFmt;  RunDelimiter = OrigDelimiter;
                                                              RunTabStep = OrigTabStep;  RunShowColumnNos = OrigShowColumnNos; }
      }
      else if (btn == 2) // 'CAPTURE' was clicked. Save the variable in a special file, "Captured Array.txt", in program directory:
      { 
        string descripn = VarName + " in " + PgmFilePath + PgmFileName;
        Boost roost = F.SaveVariable(Slot, 0.0, CapturedArrayPathName, 'O', "identified data", descripn);
        if (roost.B) captured = "   <# red>**captured**";
        else JD.Msg("Failed to capture array: " + roost.S);
      }
      else if (btn == 3) // FORMAT:
      { // We have to save the display box's size and posn here, as the input box below will reset them to its values.
        wide = JD.LastBoxSize.X;   high = JD.LastBoxSize.Y;   centreX = JD.LastBoxCentre.X;   centreY = JD.LastBoxCentre.Y;
          // These values will be used in the JD.PlaceBox(.) call 30 or so lines above, at the next loop.
        const string examples = "<b>SOME USEFUL FORMAT STRINGS</b>\nIn the following, 'n' stands for an integer between 0 and 15.\n\n" +
            "<left 30><in -30>" +
            "<b>En</b> -- Exponential format, n digits.\n" +
            "<b>Fn</b> -- Forced to have n decimal places (if nec. with trailing zeroes).\n" +
            "<b>Nn</b> -- Same, but with thousands comma inserted.\n" +
            "<b>Gn</b> -- n digits in either decimal or exp. form, whichever is shorter.\n" +
            "<b>Pn</b> -- x 100, force n decimal places, and append '%'.\n" +
            "<b>Cn</b> -- currency sign + number with thousands comma, n decimal places.\n";
        while (true)
        { string[] str_fmt = new string[] { StringFmt };
          int butt = JD.InputBox("FORMAT THE DISPLAY", "Provide a string recognized by Mono for formatting of type double:", new string[]{""},
                              ref str_fmt, new string[] {"EXAMPLES", "DEFAULT", "ACCEPT", "CANCEL"});
          if      (butt == 1) JD.Display("", examples, true, true, false, "CLOSE");

          else if (butt == 2) { StringFmt = OrigStringFmt; break; }
          else if (butt == 3) { StringFmt = str_fmt[0]; break; }
          else break; // cancel
        }
      }
      else if (btn == 4) // force chars. or numbers. 'F' = chars., obey format tags; 'C' = chars., ignore tags; 'N' = numbers. 
      { if (CharsOrNumbers == "N") CharsOrNumbers = "CF";
        else if (CharsOrNumbers == "CF") CharsOrNumbers = "C";
        else if (CharsOrNumbers == "C") CharsOrNumbers = "N";
      }
      else if (btn == 5) Delimiter = ""; // commas between column entries
      else if (btn == 6) // tabs, increasing with each btn. press, but cancelled by the above:
      { if (Delimiter == "") TabStep = OrigTabStep;  else TabStep += OrigTabStep;
        Delimiter = JS.TabS;
      }
      else if (btn == 7) ShowColumnNos = !ShowColumnNos;
    }
  }
/// <summary>
/// If file not accessible, the first time this is detected there will be an error message, but subsequently no error message will occur.
/// Even if the user inserted the hints file into the expected place, it would remain inaccessible until the program started again.
/// </summary>
  public static string RetrieveHint(string Topic)
  { string hintsTextFileContents = "";
    if (!FailedRetrievalOfHintsText)
    { string FlNm = ProgramPath + "Hints.txt";
      StringBuilder outcome;
      Boost boodle = Filing.LoadTextFromFile(FlNm, out outcome);
      if (!boodle.B) { JD.Msg("Unable to access hints file '"+FlNm+"'");  FailedRetrievalOfHintsText = true;  return ""; }
      hintsTextFileContents = outcome.ToString();
    }
    // Find the header for this topic:
    int startPtr = hintsTextFileContents.IndexOf("//" + Topic.Trim().ToUpper() + "//");
    if (startPtr == -1) return "";
    // Look for the first line that does not begin with '//':
    int ptr = startPtr;
    while (true)
    { ptr = hintsTextFileContents.IndexOf('\n', ptr);
      if (ptr == -1) return ""; // something went horribly wrong.
      if (hintsTextFileContents._Extent(ptr+1, 2) == "//") ptr += 2;
      else { startPtr = ptr + 1;  break; }
    }
    // Return the extract:
    int beyondEndPtr = hintsTextFileContents.IndexOf("====", startPtr);  if (beyondEndPtr == -1) beyondEndPtr = hintsTextFileContents.Length;
    return hintsTextFileContents.Substring(startPtr, beyondEndPtr - startPtr);
  }

/// <summary>
/// <para>Load pnemonics and their meanings. If file found returns (TRUE, ""); if file name is empty, returns (FALSE, "");
/// if file name nonempty but file could not be loaded, returns (FALSE, error message).</para>
/// <para>If file found but data is partly or wholly unusable, no error returns; simply no (or not all) intended pnemonics are set.</para>
/// </summary>
  private Boost LoadPnemonics()
  { Boost result = new Boost(false);
    List<string> pnemonicStuff;
    Boost outcome = Filing.LoadTextLinesFromFile(PnemonicFName, out pnemonicStuff);
    if (!outcome.B) { result.S = outcome.S;  return result; }
    string[] pneumonia = pnemonicStuff.ToArray();
    ExtractRemarks(ref pneumonia); // removes explanatory comments from the file strings.
    // Translate the pnemonics:
    if (Pnemonic == null) { Pnemonic = new List<string>();   PnemonicReplacemt = new List<string>(); }
    else { Pnemonic.Clear(); PnemonicReplacemt.Clear(); }
    foreach (string ln in pneumonia)
    { if (ln == "") continue;
      int eq = ln.IndexOf('=');  if (eq == -1) continue;
      Pnemonic.Add(ln._Extent(0, eq).Trim());   PnemonicReplacemt.Add(ln._Extent(eq+1).Trim());
    }
    result.B = true; return result;
  }

/// <summary>
/// <para>Replaces the pnemonic, if recognized, by its listed replacement. Otherwise does nothing. The beginning of the selection
///  - or the insertion point, if none - must be immediately at the end of the pnemonic (no space); anything to the right is
///  ignored. If the pnemonic has no '|', then the pnemonic replacement simply goes to the left of the insertion point or selection edge,
///  and the cursor ends up at its right end; if there is a '|', then (a) if no selection, the cursor ends up there; (b) if a selection,
///  the selected text goes where the '|' is, and the cursor ends up at its right end.</para>
/// <para>The argument ForceSelection, if true AND there is no existing selection, first forces a selection from the cursor to
///  either the next LF or to end of text, whichever comes first. Thereafter operation is as above, EXCEPT that if there is no '|',
///  the cursor goes to the LF. Note that if ForceSelection is TRUE but there is already a selection, ForceSelection is ignored.</para>
/// </summary>
  private void ExpandPnemonic(bool ForceSelection)
  { if (Pnemonic == null || Pnemonic.Count == 0) return;
    // Collect the last 20 chars behind the cursor:
    int HowFarBack = 20; // how far back to look for the pnemonic
    TextIter selPtStart, selPtEnd;
    BuffAss.GetSelectionBounds(out selPtStart, out selPtEnd);
    int n=0, soft = selPtStart.Offset;
    if (soft == 0) return; // No text to the left of the insertion point.
    n = HowFarBack;  if (soft < HowFarBack) n = soft; // cursor is close to the start of text
    TextIter excerptStart = selPtStart;
    excerptStart.BackwardChars(n);
    string stroo = BuffAss.GetText(excerptStart, selPtStart, true); // 'true' = return hidden characters
    string oorst = stroo._Reverse(),  pnemonic = "";
    int ptr = oorst._IndexOfNoneOf(P.IdentifierChars + ",%");
    if (ptr == -1) pnemonic = stroo; // The unlikely event that the pnemonic is the only text in the Ass. Window.
    else pnemonic = stroo._Extent(stroo.Length - ptr); // won't crash with impossible args., but will return empty string.
    if (pnemonic == "") return;
  // Find the replacement for the pnemonic:
    // First look for '%'; if found, dissect off the variables list:
    string[] varnames = null;
    int p = pnemonic.IndexOf('%'); // The '%' only occurs in user's code, not in the list Pnemonic.
    if (p >= 0)
    { string varstr = pnemonic._Extent(p+1);   pnemonic = pnemonic._Extent(0, p);
      varnames = varstr.Split(new char[] {','} );
    }
    int indx = Pnemonic.IndexOf(pnemonic); if (indx == -1) return;
    // We have identified the pnemonic.
    // Deal with the possibility of a selection. *** This block must be left till after the last 'return', as the cost of a
    //    pnemonic mistake should not be the loss of text (which this block would produce, as removed selected text is only
    //    reinserted in the last line of the method).
    bool isSelection = (soft != selPtEnd.Offset);
    string selecnText = "";
    if (isSelection)
    { selecnText = BuffAss.GetText(selPtStart, selPtEnd, true); // 'true' = return hidden characters
      JTV.OverwriteSelectedText(BuffAss, "", false); // 'false' = don't select the newly implanted text
      ForceSelection = false;
    }
    else if (ForceSelection) // and no selection has been made
    { TextIter LFIter = selPtStart;
      bool foundIt = LFIter.ForwardFindChar(equals_linefeed, BuffAss.EndIter); // 'equals_linefeed is a delegate, immediately below this method.
      if (!foundIt) LFIter = BuffAss.EndIter;
      selecnText = BuffAss.GetText(selPtStart, LFIter, true); // 'true' = return hidden characters
      BuffAss.Delete(ref selPtStart, ref LFIter);
      isSelection = true; // A virtual selection, as Gtk was never asked to actually select the text.
    }
    // Now there is no selection, and any selected text has been removed - but is stored, and will be replaced later.
    string repo = PnemonicReplacemt[indx]; // We have to massage this a bit first...
    if (varnames != null)
    { for (int i=0; i < varnames.Length; i++)
      { repo = repo.Replace("<"+i.ToString()+">", varnames[i]); }
    }
    string privvy = JS.FirstPrivateChar.ToString();
    repo = repo.Replace("<#>", privvy);
    repo = repo.Replace("<CR>", "\n");
    repo = repo.Replace("<T>", "\t");
    repo = repo.Replace("<_>", " "); // used to insert tabs at start or end, as PnemonicReplacemt strings have all been trimmed.
    // If an indent is required, get its dimensions by counting tabs as far back as the '\n' (if any):
    if (repo.IndexOf("<IN>") >= 0)
    { string indent = "\n";
      p = oorst.IndexOf('\n'); if (p == -1) p = oorst.Length;
      int tabsfound = oorst._CountChar('\t', 0, p);
      if (tabsfound > 0) { indent += ('\t')._Chain(tabsfound); }
      repo = repo.Replace("<IN>", indent);
    }
    // If special placement of the insertion point is required:
    int cursoradjust = 0;
    p = repo.IndexOf("<^>");
    if (p >= 0)
    { repo = repo.Replace("<^>", "");
      cursoradjust = repo.Length - p;
    }
    repo = repo.Replace(privvy, ""); // Final replacement - the dummy that stops this method from interpreting as a symbol what you meant literally.

    // Replace pnemonic with replacement:
    JTV.ReplaceTextAt(BuffAss, soft - pnemonic.Length, pnemonic.Length, repo);
    if (cursoradjust > 0) JTV.PlaceCursorAt(BuffAss, BuffAss.CursorPosition - cursoradjust);
    // Finally, if there was a selection, we have to reinsert the selected text:
    if (isSelection) BuffAss.InsertAtCursor(selecnText);
    if (ForceSelection) // then the cursor has to go back to the LF:
    { 
      BuffAss.GetSelectionBounds(out selPtStart, out selPtEnd); // There was no selection, but we need the cursor's location.
      TextIter LFIter = selPtStart;
      bool foundIter = LFIter.ForwardFindChar(equals_linefeed, BuffAss.EndIter);
      if (!foundIter) LFIter = BuffAss.EndIter;
      JTV.PlaceCursorAt(BuffAss, LFIter.Offset);
    }

  }
  // DELEGATE used by TextIter method 'ForwardFindChar' in the above method.
  public bool equals_linefeed (char ch)
  { return (ch == '\u000A'); }

///// <summary>
///// Replaces the pnemonic, if recognized, by its listed replacement. Otherwise does nothing. The insertion point must be
///// immediately at the end of the pnemonic (no space), and there must not be a selection.
///// </summary>
//  private void ExpandPnemonic()
//  { if (Pnemonic == null || Pnemonic.Count == 0) return;
//    // Collect the last 20 chars behind the cursor:
//    int HowFarBack = 20; // how far back to look for the pnemonic
//    TextIter selPtStart, selPtEnd;
//    BuffAss.GetSelectionBounds(out selPtStart, out selPtEnd);
//    int n=0, soft = selPtStart.Offset;
//    if (soft != selPtEnd.Offset) return; // A selection, so do nothing.
//    if (soft == 0) return; // No text to the left of the insertion point.
//    n = HowFarBack;  if (soft < HowFarBack) n = soft; // cursor is close to the start of text
//    TextIter excerptStart = selPtStart;
//    excerptStart.BackwardChars(n);
//    string stroo = BuffAss.GetText(excerptStart, selPtStart, true); // 'true' = return hidden characters
//    string oorst = stroo._Reverse(),  pnemonic = "";
//    int ptr = oorst._IndexOfNoneOf(P.IdentifierChars + ",%");
//    if (ptr == -1) pnemonic = stroo; // The unlikely event that the pnemonic is the only text in the Ass. Window.
//    else pnemonic = stroo._Extent(stroo.Length - ptr); // won't crash with impossible args., but will return empty string.
//    if (pnemonic == "") return;
//  // Find the replacement for the pnemonic:
//    // First look for '%'; if found, dissect off the variables list:
//    string[] varnames = null;
//    int p = pnemonic.IndexOf('%'); // The '%' only occurs in user's code, not in the list Pnemonic.
//    if (p >= 0)
//    { string varstr = pnemonic._Extent(p+1);   pnemonic = pnemonic._Extent(0, p);
//      varnames = varstr.Split(new char[] {','} );
//    }
//    int indx = Pnemonic.IndexOf(pnemonic); if (indx == -1) return;
//    string repo = PnemonicReplacemt[indx]; // We have to massage this a bit first...
//    if (varnames != null)
//    { for (int i=0; i < varnames.Length; i++)
//      { repo = repo.Replace("<"+i.ToString()+">", varnames[i]); }
//    }
//    repo = repo.Replace("<#>", "");
//    repo = repo.Replace("<#>", "");
//    repo = repo.Replace("<CR>", "\n");
//    repo = repo.Replace("<T>", "\t");
//    repo = repo.Replace("<_>", " "); // used to insert tabs at start or end, as PnemonicReplacemt strings have all been trimmed.
//    // If an indent is required, get its dimensions by counting tabs as far back as the '\n' (if any):
//    if (repo.IndexOf("<IN>") >= 0)
//    { string indent = "\n";
//      p = oorst.IndexOf('\n'); if (p == -1) p = oorst.Length;
//      int tabsfound = oorst._CountChar('\t', 0, p);
//      if (tabsfound > 0) { indent += ('\t')._Chain(tabsfound); }
//      repo = repo.Replace("<IN>", indent);
//    }
//    // If special placement of the insertion point is required:
//    int cursoradjust = 0;
//    p = repo.IndexOf("<^>");
//    if (p >= 0)
//    { repo = repo.Replace("<^>", "");
//      cursoradjust = repo.Length - p;
//    }
//    // Replace pnemonic with replacement:
//    JTV.ReplaceTextAt(BuffAss, soft - pnemonic.Length, pnemonic.Length, repo);
//    if (cursoradjust > 0) JTV.PlaceCursorAt(BuffAss, BuffAss.CursorPosition - cursoradjust);
//  }




//---------------------------------------------------------------
// ------- TEXT VIEW EVENTS                                   //textview //tv

  protected virtual void OnREditAssKeyPressEvent (object o, Gtk.KeyPressEventArgs args)
  { } // As far as I can elicit, this event is never raised!

  // See KeySnooper(.) for spiel on values of .KeyValue. That method also has the bookmark "//" + "key".
  protected virtual void OnREditAssKeyReleaseEvent (object o, Gtk.KeyReleaseEventArgs args) //key
  { uint keyval = args.Event.KeyValue;
   // ESC key, including CNTRL-ESC:       //esc
    if (keyval == 0xFF1B)
    {
      AutoColourBrackets = 0;
      // CTRL-ESC removes ALL temporary tags, including breakpoint tags:
      if (args.Event.State == ModifierType.ControlMask) RecolourAllAssignmentsText("remove all");
     // Plain ESC removes removes all temporary tags EXCEPT breakpoint tags:
      else RecolourAllAssignmentsText("remove except", breakpoint_text, currentbreak_text);
      LblComments.Text = "";
      // If this is keyed during a run, breakpoint tags will go but breakpoints will remain set as is; so these must be cancelled...
      if (CurrentRunStatus > 0) PgmBreakPoints = JTV.LinesStartingWithThisTag(BuffAss, breakpoint_text, 0, -1);
      if (BufferHasLineNos) RemoveLineNosFromBufferText();
    }
    // Problem - any keypress as hot key for menu item "Appearance | Toggle markup..." will arrive here after nicely recolouring Ass window.
    // At present the assigned key is F2 - key value 0xFFBF. If we don't do the following, all those lovely markup tags will have achieved nothing.
    else if (keyval != 0xFFBF) RecolourAllAssignmentsText("leave all");
  }

  protected virtual void OnREditAssFocusOutEvent (object o, Gtk.FocusOutEventArgs args)
  {
  }
  protected virtual void OnREditAssFocusInEvent (object o, Gtk.FocusInEventArgs args)
  { LastKeyDownTime = 0;  PrefixKey = new Boost(false);
  }


//---------------------------------------------------------------
// ------- TEXT BUFFER EVENTS
  protected virtual void OnBuffAssChangedEvent(object o, System.EventArgs args)
  { if (BlockTextChangeEvents) return; // Occurs during load and during recolouring.
    SetUnsavedData(true);
    if (HistoryStack == null)
    { Undoing = false;  Redoing = false;  HistoryPtr = 0;
      HistoryStack = new List<ChangeRec>();
      HistoryStack.Add(new ChangeRec()); // blank record.
    }
    if (AutoColourBrackets > 0)
    { if (AutoColourBrackets == 1) ColourBrackets('(',')');
      else ColourBrackets('{','}');
    }
    UpdateHistoryStack();
    UpdateNavigationStack();
  }

//   THE ACTION -- 'GO'                              //go  //GO
// ---------------------------------------------------------

  // Method that handles the Btn GO click or the kbd. firing of the same action:
  private void GO()
  {// If error or pause line has been coloured by the query_text tag, remove it:
    TextIter startIt = BuffAss.StartIter, endIt = BuffAss.EndIter;
    BuffAss.RemoveTag(query_text, startIt, endIt);
    BuffAss.RemoveTag(currentbreak_text, startIt, endIt);
    // Deal with possibility that we are in the holding loop in HoldingPattern():
    if (CurrentRunStatus == 1)
    { StopNow = 'G'; return; }
    else if (CurrentRunStatus >= 3) { ReadjustAfterInterrupt(); return; } // the 'return' will take operation back to
        // 'HoldingPattern()', which has been endlessly looping in a 'DoEvents()' loop. But now that loop will be exited,
        //   as CurrentRunStatus will no longer be 3, and operation will return to the calling code in Runner.Run(..).
   // Test for empty assignments window: (Test 1 saves time doing test 2, as in vast majority of cases there will be program text there.)
    if (BuffAss.Text.Length < 1 || BuffAss.Text._Extent(0, 100)._Purge() == "") return;
   // OK, there's something there...
    BeforeGOPtr = new Duo(REditAss.VisibleRect.Top, REditAss.Buffer.CursorPosition); // bookmark the screen as is just before the run.
    R.AssInstance = 0; // 'R.RunAssignment(.)' to start from zero with every run, irrespective of setting of PlayItAgainSam below.
    bool PlayItAgainSam = true; // used in preference to recursion for running a new pgm. from an old one.
    bool UseREditAss = true; // only false for particular system function(s) which run pgms. without reference to REditAss.
    while (PlayItAgainSam)
    { string oopsie = "", ss;
      bool RecordParsedAssts = true; // **** TRUE ONLY IF you need it for trouble-
                                     // shooting. Otherwise unnecessary use of memory.
      F.IOError = false;  F.IOMessage = "";  F.IOFilePath = ""; // used by I/O system fns. These are the default settings.
      // Graphing params:
      if (KillOldGraphs) { Plots2D.Clear();  Plots3D.Clear();  Board.KillBoards(); }
      F.ResetStaticParams("A");
      Board.LastKilledBoard = new Duo(0,0); // only reset when MiniMaths first lights up (in J_Plot unit), and here. (Only done for neatness; not really needed.)
      R.KillData(); // Remove data from the last run (if any).
      // But the above can't clean out MainForm structures, so...:
      R.LoopTestTime = R.OrigLoopTestTime; // Can be reset during the run to a lower value, but must always start thus.
      // Prepare the results window for this run.
      if (UseREditAss) // If not, RawLines already set (see lower down)
      { if (PreserveResults) JTV.AppendText(BuffRes, "---------------------\n", true);
        else BuffRes.Clear();
        // Get the text and conform it:
        ss = BuffAss.Text.Replace(JS.CRLF, "\n"); // in case text pasted in from a MS Windows source
        RawLines = ss.Split(new char[]{'\n'}, StringSplitOptions.None); // A class variable
      }
      PgmBreakPoints = JTV.LinesStartingWithThisTag(BuffAss, breakpoint_text, 0, -1);
      ExtractRemarks(ref RawLines);
      Quad quack = C.Conform(RawLines);
      if (!quack.B) oopsie = "Pre-parsing failed ";
      // Parse the conformed text:
      else
      { quack = P.Parse(RecordParsedAssts);
        if (!quack.B) oopsie = "Parsing failed ";
        // Run the program
        else
        { R.StuffupSite = -1; // will hold line of runtime error, if any; but
          // is used locally there; the req'd locn. to be read here is
          // instead the value returned in quack.I.
          StopNow = ' '; // if set to anything else during a loop execution, bombs the pgm.
          CurrentRunStatus = 1;
          PgmFilePath = CurrentPath;   PgmFileName = CurrentName; // Never reset elsewhere.
          this.Title = ClearTextAddend(this.Title) + RunStatusText[1];
          F.PreserveSysLists = false; // If set to true, it kept the lists alive in R.KillDate(); now its term in office has expired.
          bool success;
          BtnAbort.ModifyFg(StateType.Normal, "DeepPink"._ParseColour(Black, true, out success));
                                                             // Pink button text: indicates active btn. state.
          SetLabelDefaultTexts("ARC");
          StartTime = DateTime.Now;
          //=====================
          // RUN
          //=====================
          BtnGO.Label = "FREEZE";
          quack = R.Run(0); // Run main pgm. (fn. no. 0)
          //=====================
          // RUN OVER
          //=====================
          TimeSpan period = DateTime.Now - StartTime;
          RunDuration = period.TotalMilliseconds;
          MenuVisibility("ALL", 'V'); // Just in case the user made some main menus invisible.
          BtnGO.Label = "GO!";
          SetLabelDefaultTexts("AR"); // LblAss and LblRes; leave LblComments as is.
          CurrentRunStatus = 0; this.Title = ClearTextAddend(this.Title);
          BtnAbort.ModifyFg(StateType.Normal, "gray"._ParseColour(Black, true, out success)); // Inactive indicator. No need to disable - clicking it won't do a lot.
          if (BlockFileSave != null) BlockFileSave.Clear();
          // Before checking 'quack' for error or for re-runs, display any material accumulated for the Results Window:
          if (UseREditAss) // i.e. this is a program visible in the Assts Window, not hidden from view.
          { // Prepare string for display:
            int ln = BuffRes.Text.Length; 
            ss = "";
            if (ln > 0 && BuffRes.Text[ln - 1] >= ' ') ss = "\n";
            ss += R.DisplayMainPgmUserScalars();
            if (DisplayScalarValuesAfterTheRun)
            { JTV.DisplayMarkUpText(REditRes, ref ss, "append");  }
            if (RecordRunTime)
            { ss = "Duration of run: " +  ( RunDuration / 1000.0 ).ToString("G4") + " secs.\n";
              JTV.AppendText(BuffRes, ss, true);
            }
          }
          // Now handle errors / reruns recorded in 'quack':
          if (!quack.B) // EITHER an error arose at runtime OR a new run has been requested during existing run:
          { if (quack.I < -1) // then NOT an error:
            { if (quack.I <= -100) // then abortion at a breakpoint has occurred:
              { int n = - (100 + quack.I);
                JTV.PlaceCursorAtLine(REditAss, n, 0, true);
                oopsie = "Program aborted at a set break point (par. " + (n+1).ToString() + ")"; // i.e. line no. to base 1
              }
              else
              { UseREditAss = (quack.I != -30 && quack.I != -40);
               // Deal with form title:
                if (quack.I == -10 || quack.I == -11 || quack.I == -40) // look for a file name to display at top of MainForm:
                { int n = quack.S.IndexOf(JS.FirstPrivateChar);
                  if (n > 0)
                  {// read off the file name, and strip it from quack.S:
                    string filename = quack.S._Extent(0, n); quack.S = quack.S._Extent(n + 1);
                    string[] stroo = filename._ParseFileName(CurrentPath);
                    CurrentPath = stroo[0];  CurrentName = stroo[1];
                    this.Title = CurrentName + "    (folder:  " + CurrentPath + ")"; // Set MainForm's title to the new file name.
                  }
                }
                else // either altered pgm. or completely new text. Either way, don't display the former file name.
                { CurrentName = "";  this.Title = ""; }
               // Fill REditAss with new code, unless 'quiet' option taken up:
                if (UseREditAss)
                { BuffAss.Text = quack.S;
                  RecolourAllAssignmentsText("remove all");
                  if (quack.I == -10 || quack.I == -11) JTV.PlaceCursorAtStart(BuffAss);
                  else JTV.PlaceCursorAt(BuffAss, SelStore.X);
                  REditAss.GrabFocus();
                }
                else RawLines = quack.S.Split('\n'); // RawLines goes straight to the parser, not to REditAss.
               // RERUN (or not...)
                if (quack.I != -11 && quack.I != -21) // which are load-only options
                { continue; } // with PlayItAgainSam still TRUE.   **** NB: RE-RUNS END THE LOOP HERE!
              }
            }
            else if (ExitPlus.IX != 0) // Set by system fn 'exit_plus'. After the exit, invokes menu item "File | New" or "Load".
            { if (ExitPlus.IX == 1) // Mimick "File | New" (but no warning re overwriting unsaved text):
              { KillCurrentGraphs();
                ClearREditAss();
                R.KillData();
              }
              else if (ExitPlus.IX == 2) // Mimick "File | Load" (but no warning re overwriting unsaved text):
              { Boost roost = LoadProgramAndResetParams(ExitPlus.SX, ExitPlus.SY, false);
                if (!roost.B)
                { if (roost.S == "") oopsie = "User cancelled file dialog of function 'exit_plus'";
                  else { oopsie = "Failed to load file '" + ExitPlus.SX + ExitPlus.SY + "';  " + roost.S; }
                }
                else // successful file load, so if required put the display in formatted text mode:
                { if (ExitPlus.IY == 1) SwitchMarkupTagSystem(true); }
              }
              ExitPlus = new Strint2(0); // Whatever was ExitPlus.IX, always reset ExitPlus.
            }
            else oopsie = "Runtime interruption "; // 'tis an error...
          }
        }
      }
      if (oopsie != "")
      {// Display the error message in REditRes:
        quack.S = Demythologize(quack.S);
        ss = "<# blue><b>" + oopsie;
        if (quack.I > -100) // i.e. not a break point
        { if (quack.I > -1) ss += "in par. " + (quack.I + 1).ToString();
            ss += ":  " + quack.S;
        }
        ss += ".</b><# black>\n";
        JTV.DisplayMarkUpText(REditRes, ref ss, "append");
        // Highlight the offending line in REditAss:
        if (quack.I > -1)
        { JTV.TagLine(REditAss, query_text, quack.I, '+', true, true);
          REditAss.GrabFocus();
        }
        if (quack.X == 10.0 || quack.X == 20.0)
        { if (quack.X == 10.0) ss = "'(  )'";  else ss = "'{  }'"; // *** if changing, also change args to ColourBrackets(.) below.
          int n = JD.DecideBox("BRACKETS MISMATCH", "Somewhere brackets " + ss + " don't match. Do a colour check?", "COLOUR", "CANCEL");
          if (n == 1) ColourBrackets( ss[1], ss[4] );
        }
// START HERE: Rewrite 'ColourBrackets'.
        return; // SIDESTEPS  the SHUTDOWN TEST below, so that error crashes and 'crash(.)' crashes do not trigger MiniMaths instance closure.
      }
      PlayItAgainSam = false; // if got here, then no re-run desired. Re-runs use CONTINUE above, so jump over this.
    } // END OF 'while (PlayItAgainSam)' loop
    if (ShutDown) { ShutDown = false; CloseThisWindow(); } // The function 'closedown(.)' is the only thing that evokes this.
                    // *** It is not redundant to reset ShutDown. Reason: CloseThisWindow() doesn't always work, and the instance may persist.
    REditAss.GrabFocus();
    return;
  }
//===========================================
//  METHODS TYING IN WITH 'GO'
// ------------------------------------------

// This is called (from 'GO' handler) when user's program is about to be resumed after interruption:
  public void ReadjustAfterInterrupt()
  { // MAKE MENU CHANGES:

    // ADJUST THE FORM HEADING:
    this.Title = ClearTextAddend(this.Title) + RunStatusText[1];
    CurrentRunStatus = 1;
    // Now the HoldingPattern() will be released, and return to R.Run(..) will occur.
  }

// Trim the header of the main form, if it has some suffix in sq. bkts.
  public static string ClearTextAddend(string intext)
  {// Only trim if the last char. in the string is ']'
    if (intext._Last() == ']')// && intext != Unnamed)
    { int n = intext.IndexOf('[');
      if (n > -1)
      { string result = intext._Extent(0, n);
        return result.TrimEnd(); // there were blanks before the '['.
    } }
    return intext; // if not trimmed.
  }

/// <summary>
/// Extract all remarks of both types (block and one-line remarks). No lines will be eliminated, but lines
/// will return trimmed, so that lines containing only spaces will be reduced to empty lines.
/// </summary>
  public void ExtractRemarks(ref string[] TxtLn)
  {// EXTRACT BLOCKS FIRST. Unpaired RemOpener/RemCloser pairs will be treated
   //  as ordinary text (and so will stimulate an error in parse time).
    int openX = -1, openY = -1, closeX, pp, qq=0;
    for (int i = 0; i < TxtLn.Length; i++)
    { while (true) // remove blocks wholly within this one line:
      { pp = TxtLn[i].IndexOf(IgnoreCueOpen);
        if (pp > -1) qq = TxtLn[i].IndexOf(IgnoreCueClose,pp);
        if (pp>=0 && qq>pp)
        { TxtLn[i] = TxtLn[i]._ScoopTo(pp, qq + IgnoreCueClose.Length-1);}
        else break;
      }
   // Now there can only be: (1) no blocks; (2) the end of a block;  (3) the
   //  beginning of a block; or (4) both, but with non-remark text between them.
     // If the end of a block is in this line, delete the block:
      closeX = TxtLn[i].IndexOf(IgnoreCueClose);
      if (openX!=-1 && closeX!=-1)//we have a valid block.
      {//delete within first and last lines of the block:
        TxtLn[openY] = TxtLn[openY]._Extent(0,openX);
        TxtLn[i] = TxtLn[i]._Extent(closeX + IgnoreCueClose.Length);
       // Remove all text from any lines in between these two:
        for (int j = openY+1; j < i; j++)TxtLn[j]="";
       // Reset the opener markers:
        openX = -1; openY = -1;
      }
     // If an opener is in the block, and no preexisting opener is current, record it:
      pp = TxtLn[i].IndexOf(IgnoreCueOpen);
      if (pp != -1 && openX == -1) {openX = pp;  openY = i;}
    }

// EXTRACT ONE-LINE REMARKS, and TRIM LINES in the process:
    for (int i = 0; i < TxtLn.Length; i++)
    { pp = TxtLn[i]._IndexOfAny(new string[]{HeaderCue, CommentCue} ).X; // Find first of either type of one-liner.
      if (pp!=-1) TxtLn[i] = TxtLn[i]._Extent(0, pp); // Remove any one-line remark.
      TxtLn[i] = TxtLn[i].Trim(); // In any case, trim the line.
    }
//Keep for testing:    JD.Msg(TxtLn._ToString("_/_"));
  }
  
/// <summary>
/// Brackets of the given type are coloured according to their nesting order. All other bracket-colouring tags still in place
///  from some previous call are removed; other tags (e.g. 'find' tags) are left as is.
/// </summary>
  public void ColourBrackets(char Opener, char Closer)
  {// Develop a list of all bracket positions outside of remarks, with their hierarchical scores:
    List<Duo> brackets = new List<Duo>();
    int bktlevel = 0; // Outermost brackets should have bktlevel = 1.
    bool inblockrem = false, inparrem = false;
    string chstr = "", lastchstr;
    string opener = Opener.ToString(),  closer = Closer.ToString();
    TextIter titter = BuffAss.StartIter;
    while (true) // iterating through a string this way avoids offset where a UFC8 is way above range, like "𝕀" ('\uD835').
    { lastchstr = chstr;
      chstr = titter.Char;
      if (inblockrem) { if (chstr == "/" && lastchstr == "*") inblockrem = false; }
      else if (inparrem) { if (chstr == "\n") inparrem = false; }
      else // active text (i.e. not remarked out):
      { if (chstr == "*" && lastchstr == "/") inblockrem = true;
        else if (chstr == "/" && lastchstr == "/") inparrem = true;
        else if (chstr == opener)
        { bktlevel++;  brackets.Add(new Duo(titter.Offset, bktlevel)); }
        else if (chstr == closer)
        { brackets.Add(new Duo(titter.Offset, bktlevel));  bktlevel--; }
      }
      if (!titter.ForwardChar()) break;
    }
    int bktcnt = brackets.Count;
    if (bktcnt == 0)  return;
    // Check if BuffAss has the necessary tags in its table; if not (because this is the first pass), set them here.
    string tagPrefix = "bktclrtag_";
    string[] clrnames = new string[] {"black", "chocolate", "red", "orange", "yellow", "green", "deepskyblue", "magenta", "grey", "white"}; // resistor code
    int noClrs = clrnames.Length;
    string[] tagnames = new string[noClrs];
    for (int i=0; i < noClrs; i++) tagnames[i] = tagPrefix + i.ToString();
    if (BuffAss.TagTable.Lookup(tagPrefix + (noClrs-1).ToString()) == null) // the last tag
    { JTV.InstallColourTags(BuffAss, null, clrnames, tagPrefix); } // The tags will be highlight colours, not text colours.
    // Remove any existing bracket colour tags:
    BlockTextChangeEvents = true;
    TextIter startIt = BuffAss.StartIter, endIt = BuffAss.EndIter;
    for (int i=0; i < noClrs; i++)
    { BuffAss.RemoveTag(tagnames[i], startIt, endIt); }
    // Now apply the new bracket colour tags: 
    for (int j=0; j < bktcnt; j++)
    { int tagindex = brackets[j].Y-1;
      if (tagindex < 0) tagindex = noClrs + tagindex - 1; // --> unmatched closer looks a very different colour to 1st. &  2nd. level '{'.
      JTV.TagExtent(BuffAss, tagnames[tagindex % noClrs], brackets[j].X, 1); // last two: 'from' pointer, extent.
    }
    BlockTextChangeEvents = false;
  }

// Translate internal constants like "__L1" into numbers.
  public static string Demythologize(string myth)
  { string result = myth, nm = "", newnm = "";
    int startptr = 0, ptr = 0, varno = -1;    int[] arr = null;
    while (true)
    { ptr = result.IndexOf("__", startptr);
      if (ptr == -1) break;
      arr = JS.FindInteger(result, true, ptr+3, ptr+8); // 'true' = ignore initial minus
      if (arr[0] == 1)
      { nm = result._FromTo(ptr, arr[2]-1) + arr[1].ToString();
        varno = V.FindVar(0, nm);
        if (varno >= 0) 
        { newnm = V.GetVarValue(0, varno).ToString();
          result = result._Extent(0, ptr) + newnm + result._Extent(arr[3]+1);
          startptr = arr[2]-3; // first char. of replacement
        }
        else startptr = ptr + 3;
      }
      else startptr = ptr + 3;
    }
  return result;
  }

//    GENERAL METHODS (i.e. other than event handlers)
//---------------------------------------------------------
// INI FILE PROCESSING                      //ini

/// <summary>
/// <para>Extracts parameters from the image of the disk INI file, applies limited checks, and if these are satisfactory
///  replaces the corresponding entries in INR[]. If the check fails for a parameter, the corresponding INR[] entry
///  remains unchanged.</para>
/// <para>The only checks are </para>
/// RETURNED IF NO PROBLEMS: .B TRUE, .I = 0, .S empty.
/// RETURNED IF SOME PROBLEMS: .B TRUE, .I = 1, .S = list of problems, delimited by '\n'.
/// RETURNED IF TOTAL FAILURE (no file, or not even one INR value could be set): .B FALSE, .I = 2, .S = explanation.
/// There is no check for completeness of information in INI; any fields that the INI file could set but did not are ignored.
/// </summary>
  private Quad Load_INIFile_AndFill_INR_FromItsData()
  { Quad result = new Quad(true);
    bool success,  atLeastOneSuccess = false;
    List<string> INIFileImg;
    Boost outcome = Filing.LoadTextLinesFromFile(ProgramPath+INIFileName, out INIFileImg);
    if (!outcome.B) { result.S = outcome.S;  result.B = false;  result.I = 2;  return result; }
    int ptr, INILen = INIFileImg.Count,  INRLen = INR.Length;
    string ININame="", INIPredicate="", INRName="";
    char typoe=' ';
    INIRecord rec = new INIRecord();
    // Outer Loop: Go through INI strings.
    for (int i = 0; i < INILen; i++)
    { ININame = INIFileImg[i];
      ptr = ININame.IndexOf('=');  if (ptr < 1) continue; // ignore any INI file line without a '=', or without at least one char. before '='.
      INIPredicate = ININame._Extent(ptr+1).Trim(); // remove any spaces after the '=', and any terminal spaces.
      ININame = ININame._Extent(0, ptr).Trim();
     // Inner Loop: For given INI file entry, check it against all INR entries:
      for (int j = 0; j < INRLen; j++) //... search INR for where that parameter should go.
      { INRName = INR[j].Nm;  if (INRName == "") continue; // would be a currently unused INR[] slot.
        if (ININame == INRName) // Hoorah! A match!
        { rec.CopyFrom(INR[j]);
          typoe = rec.Tp;
          if      (typoe == 'X') rec.X = INIPredicate._ParseDouble(out success);
          else if (typoe == 'I') rec.I = INIPredicate._ParseInt(out success);
          else if (typoe == 'S')
          { if (INIPredicate._Extent(0, 2) == "~/")
            { INIPredicate = HomePath + INIPredicate._Extent(2); }
            rec.S = INIPredicate;
            success = true;
          }
          else if (typoe == 'L') 
          { //if (INIPredicate._Extent(0, 2) != "0x") INIPredicate = "0x" + INIPredicate;
            rec.Clr = INIPredicate._ParseColour(new Color(0,0,0), true, out success);
          }
          else success = false; // unrecognized typoe; should never occur.
          // If all well, record in INR:
          if (success) {  INR[j].CopyFrom(rec);   atLeastOneSuccess = true; }
          // Otherwise kindly let the user know what went wrong, but don't raise an error:
          else { result.I = 1;  result.S += "  " + INIFileImg[i] + "\n"; }
        }
      }
    }
    if (!atLeastOneSuccess)
    { result.B = false; result.I = 2;  result.S = "PROBLEMS WITH 'INI' FILE: Parsing failed with all lines"; }
    else if (result.I == 1)
    { result.S = "PROBLEMS ARISING WHEN PARSING THE 'INI' FILE:  Parsing failed with line(s):\n\n" + result.S +
              "\nMiniMaths will still run, but will use default value(s) for the above parameter(s).";
    }
    return result;
  }

/// <summary>
/// <para>Collects all of the parameters that are referenced in the INI file and slots them into INR[], the elements of this array
/// being of type INIRecord. That type has 3 fields: string 'Nm' (the parameter name);  char 'Tp' (parameter type - 'X'(real),
/// 'I'(int), 'L' (colour as three-byte hexadecimal), 'S' (string)); and Quad 'Val' (different uses for different 'Tp' values).</para>
/// <para>'RecentFilesOnly' -- if TRUE, only recent filenames are packed into the existing INR[], no other elements being accessed.</para>
/// </summary>
  private void PackParametersIntoINR(bool RecentFilesOnly)
  {// ANY CHANGES TO ALLOCATIONS MUST BE REFLECTED IN SetParametersFromINR() below.
    if (!RecentFilesOnly)
    { INR[0] = new INIRecord("RelativeHt_AssWindow", RelHtAss); // In each case, the INIRecord constructor detects what type of variable
      INR[1] = new INIRecord("CurrentPath", CurrentPath);       //   is being handed to it, and sets the appropriate field accordingly.
      INR[2] = new INIRecord("CurrentFnPath", CurrentFnPath);
      INR[3] = new INIRecord("CurrentDataPath", CurrentDataPath);
      INR[4] = new INIRecord("PnemonicsFileName", PnemonicFName);

      INR[10] = new INIRecord("FontNameAss", FontNameAss);
      INR[11] = new INIRecord("FontNameRes", FontNameRes);
      INR[12] = new INIRecord("FontPointsAss", FontPointsAss);
      INR[13] = new INIRecord("FontPointsRes", FontPointsRes);
      INR[14] = new INIRecord("ArrayDisplayFormatString", ArrayDisplayFormatStr);

      INR[20] = new INIRecord("ColourAssBack", ClrAssBack);
      INR[21] = new INIRecord("ColourAssBackPaused", ClrAssBackPaused);
      INR[22] = new INIRecord("ColourResBack", ClrResBack);
      INR[23] = new INIRecord("ColourAssText", ClrAssText);
      INR[24] = new INIRecord("ColourComment", ClrComment);
      INR[25] = new INIRecord("ColourHeader", ClrHeader);
      INR[26] = new INIRecord("ColourIgnore", ClrIgnore);
      INR[27] = new INIRecord("ColourPausedAt", ClrPausedAt);
      INR[28] = new INIRecord("ColourFinds", ClrFinds);
      INR[29] = new INIRecord("ColourReplaced", ClrReplaced);
      INR[30] = new INIRecord("ColourQuery", ClrQuery);
      INR[31] = new INIRecord("ColourBreakpoint", ClrBreakpoint);
      INR[32] = new INIRecord("ColourCurrentBreak", ClrCurrentBreak);

     // *** Currently INR's size is 52, = up to 40 general slots + top 12 reserved for recent files (below).
     //     The no. general slots is easily adjusted (start at definition of INR near top of this file); no. recent files is set in iron.
    }

    // ONLY RECENT FILES entries can go from here till the end of the file.
    int firstRecentFileSlot = SizeOfINR - NoRecentFileSlots;
    string ss;
    for (int i=0; i < NoRecentFileSlots; i++)
    { ss = i.ToString();  if (ss.Length < 2) ss = "0" + ss;
      ss = "RecentFile" + ss;
      INR[firstRecentFileSlot + i] = new INIRecord(ss, RecentFiles[i]); // *** If adding more fields, will need to adjust field SizeOfINR.
    }
  }

/// <summary>
/// <para>This is only used to set parameters after a prior call to LoadIntoINR(.). That method loads text lines and then does partial
/// parsing of these, but in particular does not parse colours, leaving them in their original string form. The results of its work
/// are stored in INR[], records with data and string fields. Here the parsing job is completed, being done on these INR records,
/// and - where parsing is successful - sets the actual parameters.</para>
/// <para>If there are parsing failures, the return will be (FALSE, error messages delineated by '\n'). If no errors, returns (TRUE, "").</para>
/// </summary>

  private void SetParametersFromINR()
  { RelHtAss =        INR[0].X;
    CurrentPath =     INR[1].S;
    CurrentFnPath =   INR[2].S;
    CurrentDataPath = INR[3].S;
    PnemonicFName =   INR[4].S;  if (PnemonicFName.IndexOf('/') == -1) PnemonicFName = ProgramPath + PnemonicFName;

    if (INR[10].S != "")  FontNameAss = INR[10].S;
    if (INR[10].S != "")  FontNameRes = INR[11].S;
    FontPointsAss = (float) INR[12].X;
    FontPointsRes = (float) INR[13].X;
    ArrayDisplayFormatStr = INR[14].S;

    ClrAssBack =            INR[20].Clr._ToByteArray();
    ClrAssBackPaused =      INR[21].Clr._ToByteArray();
    ClrResBack =            INR[22].Clr._ToByteArray();
    ClrAssText =            INR[23].Clr._ToByteArray();
    ClrComment =            INR[24].Clr._ToByteArray();
    ClrHeader  =            INR[25].Clr._ToByteArray();
    ClrIgnore  =            INR[26].Clr._ToByteArray();
    ClrPausedAt =           INR[27].Clr._ToByteArray();
    ClrFinds   =            INR[28].Clr._ToByteArray();
    ClrReplaced =           INR[29].Clr._ToByteArray();
    ClrQuery   =            INR[30].Clr._ToByteArray();
    ClrBreakpoint =         INR[31].Clr._ToByteArray();
    ClrCurrentBreak =       INR[32].Clr._ToByteArray();

    // Recent Files:
    int file0 = SizeOfINR - NoRecentFileSlots;
    for (int i=0; i < NoRecentFileSlots; i++)
    { RecentFiles[i]= INR[file0+i].S;
      if (RecentFiles[i] != "") NoRecentFiles = i+1; }
   // Leave it to calling code to actually implement these parameters, with the single exception of
   //  applying the parameter 'RelHtAss' by resizing the form:
 }

  public void AdjustRecentFilesList(string PathPlusName)
  { bool Found = false;
    for (int i = 0; i < NoRecentFiles; i++)
    // If the current file name is already in RecentFiles[], just shift it to the top, and exit:
    { if (PathPlusName == RecentFiles[i])
      { for (int j = i; j > 0; j--) RecentFiles[j] = RecentFiles[j - 1];
        RecentFiles[0] = PathPlusName; Found = true; break;
      }
    }
    // If not, add it to the beginning of the array, losing anything at the end of the array:
    if (!Found)
    { for (int i = NoRecentFileSlots - 1; i > 0; i--)
      { RecentFiles[i] = RecentFiles[i - 1]; }
      RecentFiles[0] = PathPlusName;
      if (NoRecentFiles < NoRecentFileSlots) NoRecentFiles++;
    }
    PackParametersIntoINR(true); // true = only adjusts the recent files list in INR.
    SaveINISettings(ProgramPath + INIFileName);
    UpdateReloadMenuTexts();
  }

  private void UpdateReloadMenuTexts()
  { // Menu labels automatically ignore the underscore, so you have to fake it with a lookalike character:
    char underscore = '_',  replacemt = 'ˍ'; // hex unicode 2CD; decimal 717.
    ReloadAction0.Label  = RecentFiles[0].Replace(underscore, replacemt);
    ReloadAction1.Label  = RecentFiles[1].Replace(underscore, replacemt);
    ReloadAction2.Label  = RecentFiles[2].Replace(underscore, replacemt);
    ReloadAction3.Label  = RecentFiles[3].Replace(underscore, replacemt);
    ReloadAction4.Label  = RecentFiles[4].Replace(underscore, replacemt);
    ReloadAction5.Label  = RecentFiles[5].Replace(underscore, replacemt);
    ReloadAction6.Label  = RecentFiles[6].Replace(underscore, replacemt);
    ReloadAction7.Label  = RecentFiles[7].Replace(underscore, replacemt);
    ReloadAction8.Label  = RecentFiles[8].Replace(underscore, replacemt);
    ReloadAction9.Label  = RecentFiles[9].Replace(underscore, replacemt);
    ReloadAction10.Label = RecentFiles[10].Replace(underscore, replacemt);
    ReloadAction11.Label = RecentFiles[11].Replace(underscore, replacemt);
  }

  private bool SaveINISettings (string INIpathfilename)
  { char c;   string ss;
    int homelen = HomePath.Length;
    string[] INISettings = new string[SizeOfINR];
    for (int i = 0; i < SizeOfINR; i++)
    { ss = INR[i].Nm;  if (ss == "") continue; // an INR field which does not yet have a use.
      INISettings[i] = ss + "=";
      c = INR[i].Tp;
      if (c == 'S')
      { ss = INR[i].S;
        if (ss._Extent (0, homelen) == HomePath) ss = "~/" + ss._Extent (homelen); // convert all home dir. intros to "~/", for portability.
        INISettings[i] += ss;
      }
      else if (c == 'X')INISettings[i] += INR[i].X.ToString();
      else if (c == 'L')INISettings[i] += INR[i].Clr._ToString("[R=%R, G=%G, B=%B]"); // = fmt. of MS .NET .ToString() output.
      else if (c == 'I')INISettings[i] += INR[i].I.ToString();
    }
    string INISettingsString = String.Join("\n", INISettings);
    Boost toost = Filing.SaveTextToFile(INIpathfilename, INISettingsString, false);
    if (!toost.B) JD.Msg("SAVING OF INI FILE FAILED\n\nSystem message:\n\n" + toost.S);
    return toost.B;
  }
/// <summary>
/// DimensionType values: 2 = search Plots2D; 3 = search Plots3D; anything else - e.g. 23 - = search both.
/// The method would remove all of any duplicated references (though these should never occur).
/// PlotIDs may be null or empty, and entries may be zero (or negative).
/// </summary>
  public static void RemovePlots(int DimensionType, params int[] PlotIDs)
  { if (PlotIDs._NorE()) return;
    if (DimensionType != 3)
    { for (int i=0; i < PlotIDs.Length; i++)
      { if (PlotIDs[i] <= 0) continue;
        for (int j=0; j < Plots2D.Count; j++)
        { if (Plots2D[j].ArtID == PlotIDs[i]) Plots2D.RemoveAt(j);
        }
      }
    }
    if (DimensionType != 2)
    { for (int i=0; i < PlotIDs.Length; i++)
      { if (PlotIDs[i] <= 0) continue;
        for (int j=0; j < Plots3D.Count; j++)
        { if (Plots3D[j].ArtID == PlotIDs[i]) Plots3D.RemoveAt(j);
        }
      }
    }
  }
/// <summary>
/// DimensionType values: 2 = search Plots2D; 3 = search Plots3D; anything else - e.g. 23 - = search both.
/// Returns a Plot[] array, the members of which should be dereferenced as soon as possible by calling code.
/// If no plots identified, or if PlotIDs is empty, returns a nonnull but empty array.
/// </summary>
  public static Plot[] GetPlotPointers(int DimensionType, params int[] PlotIDs)
  { if (PlotIDs.Length == 0) return new Plot[0];
    List<Plot> plotshot = new List<Plot>();
    if (DimensionType != 3)
    { for (int i=0; i < PlotIDs.Length; i++)
      { for (int j=0; j < Plots2D.Count; j++)
        { if (Plots2D[j].ArtID == PlotIDs[i]) plotshot.Add(Plots2D[j]);
        }
      }
    }
    if (DimensionType != 2)
    { for (int i=0; i < PlotIDs.Length; i++)
      { for (int j=0; j < Plots3D.Count; j++)
        { if (Plots3D[j].ArtID == PlotIDs[i]) plotshot.Add(Plots3D[j]);
        }
      }
    }
    return plotshot.ToArray();
  }



//----------------------------------------------------------

// I/O-SERVICING METHODS                                        //io
/// <summary>
/// <para>Try to load a file into the REditAss window; return { TRUE, ""} if successful;
/// otherwise { FALSE, error message }.</para>
/// <para>If 'forceDialog' a dialog is forced; otherwise a dialog only occurs if (a) PathAndName is empty (after trimming);
///  or (b) PathAndName is only a path (as detected by its ending in '/').</para>
/// <para>If PathAndName is empty, the dialog will open in the CurrentPath directory. Otherwise in the directory of the
///  path given in PathAndName.</para>
/// <para>If ClearBufferFirst is TRUE, not only does (successfully) loaded text replace what is in REDitAss, but also
///  CurrentPath and CurrentPathAndName are reset. If it is FALSE, text is loaded at the current insertion point (without
///  deleting any selection) and CurrentPath etc. are not adjusted.</para>
/// </summary>
  public Boost LoadFileIntoREditAss(bool forceDialog, ref string ThisPath, ref string ThisName, bool ClearBufferFirst)
  {
    if (ThisPath == "" || ThisName == "")
    { forceDialog = true;
      if (ThisPath == "") ThisPath = CurrentPath;
    }
    if (forceDialog)
    { Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog("Choose the file to open", this,
           FileChooserAction.Open, "Cancel",ResponseType.Cancel, "Open",ResponseType.Accept);
      fc.SetCurrentFolder(ThisPath);
      int outcome = fc.Run();
      string fname = fc.Filename;
      fc.Destroy(); // Without this, the FileChooserDialog window won't get closed.
      if (outcome != (int) ResponseType.Accept) return new Boost(false, "");
      string[] stroo = Filing.Parse(fname);
      if (stroo == null) return new Boost(false, ""); // user cancelled using empty text
      ThisPath = stroo[0];  ThisName = stroo[1];
    }
    StringBuilder sb;   string fpname = ThisPath+ThisName;
    Boost result = Filing.LoadTextFromFile(fpname, out sb);
    if (result.B)
    { if (ClearBufferFirst)
      { ClearREditAss(); // This does lots of actions, including setting colouring to UseRemarksTags and resetting history stack.
        BuffAss.Text = sb.ToString();
      }
      else JTV.InsertTextAtCursor(BuffAss, sb.ToString()); // just insert at the cursor:
      AdjustRecentFilesList(fpname);
    }
    return result;
  }

/// <summary>
/// <para>Save 'TextToSave'. If TheName is empty (after trimming), a dialog is forced. (If ThePath is valid, it defines the
///   opening directory; otherwise CurrentPath is used as the opening directory.)</para>
/// <para>If the given file exists already and a dialog was NOT invoked, a warning about overwriting is displayed.</para>
/// <para>ThePath and TheName returns with the actual path and file name of a successful save.</para>
/// <para>ForcedExtension: ignored, if PathAndName supplied. Otherwise (a) ForcedExtension empty: no extension added; (b) nonempty:
///   text is appended to the file name UNLESS it already contains a '.'. If PathAndName ends in '.', the '.' will be removed and no
///   extension will be added.</para>
/// <para>Any initial '.' in ForcedExtension is ignored.</para>
/// <para>Note that this method checks string list BlockFileSave, which names files banned from saving, +/- error messages.</para>
/// </summary>
  public Boost SaveWithOptions(string TextToSave, ref string ThePath, ref string TheName, string ForcedExtension)
  { ThePath = ThePath.Trim();
    TheName = TheName.Trim();
    string pathname = "";
    if (ThePath == "" || TheName == "")
    { Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog("Choose or supply file name for saving", null,
           FileChooserAction.Save, "Cancel",ResponseType.Cancel, "Save",ResponseType.Accept);
      if (ThePath == "") fc.SetCurrentFolder(CurrentPath); else fc.SetCurrentFolder(ThePath);
      int outcome = fc.Run();
      if (outcome == (int) ResponseType.Accept) pathname = fc.Filename.Trim();
      fc.Destroy(); // Without this, the FileChooserDialog window won't get closed.
      if (outcome != (int) ResponseType.Accept || pathname == "") return new Boost(false, ""); // latter: user cancelled using empty text
      // Apply extension, if relevant:
      if (ForcedExtension != "")
      { int p = pathname.LastIndexOf('.'), q = pathname.Length;
        if (p != -1 && p == q-1) pathname = pathname.Substring(0, q-1); // file name ended in '.', so remove it and
                                                                              //  don't add an extension.
        else if (p == -1) // add the extension:
        { if (ForcedExtension[0] == '.') pathname += ForcedExtension;
          else pathname += '.' + ForcedExtension;
        }
      } // *** If nec., allow later for the '.' of hidden directories containing '/.', like "/.etc".
    }
    else pathname = ThePath + TheName;
    // Check that this is not a blocked file name:
    if (BlockFileSave != null)
    { for (int i=0; i < BlockFileSave.Count; i++)
      { string ss = BlockFileSave[i], yak = "", fnm;
        int n = ss._IndexOf('#'); // if present, stuff before '#' is a (full) file name, stuff after it is a message. Otherwise all is the file name.
        if (n >= 0) fnm = ss._Extent(0, n);  else fnm = ss;
        bool isOk = true;
        if (ss.Length > 0  &&  ss[0] == '!')
        { isOk = (pathname.IndexOf(fnm._Extent(1)) == -1); }
        else isOk = (pathname  !=  fnm);
        if (!isOk)
        { if (n == -1) yak = "The file name\n   <b>" + pathname +
                                  "</b>\n has been blocked from within the current user program, by the use of function 'blockfile'.";
          else yak = ss._Extent(n+1); // stuff after '#'
          return new Boost(false, yak);
        }
      }
    }
    Boost boo =  Filing.SaveTextToFile(pathname, TextToSave, false);
    if (boo.B)
    { string[] stree = Filing.Parse(pathname);
      ThePath = stree[0];   TheName = stree[1];
    }
    return boo;
  }
/// <summary>
///
/// </summary>
  public static string[] ParseFileName(string PFName, string CurrentDir)
  { string[] result = new string[2];
    string pfName = PFName.Trim();
    int len = pfName.Length;  if (len == 0) return result;
    // Replace any Microsoft hangovers using backslashes:
    pfName = pfName.Replace(@"\\", "/");    pfName = pfName.Replace(@"\", "/");
    // Split into path and name:
    int n = pfName.LastIndexOf('/');
    if (n == -1) { result[1] = pfName;  return result; } // PFName was all filename.
    string path = pfName._Extent(0, n+1); // Path name with a terminal '/'
    if (path.Length >= 2 && path._Extent(0, 2) == "./")
    { string curr = CurrentDir._ForceLast('/');
      path = curr + path._Extent(2);
    }
    result[0] = path;   result[1] = pfName._Extent(n+1);
    return result;
  }

//----------------------------------------------------------
// TEXT-SERVICING METHODS                                        //text

  private Rectangle LocateViewPoint()
  { return REditAss.VisibleRect; // Bounds of the visible section of the buffer, in buffer coordinates.
  }
/// <summary>
/// Sets an internal bookmark (named "BkMk1") to the buffer location which is currently at the top left corner
///  of the text view. Used in conjunction with method 'ResumeViewPoint()'.
/// </summary>
  private void MarkViewPoint()
  { Rectangle roo = REditAss.VisibleRect; // Bounds of the visible section of the buffer, in buffer coordinates.
    TextIter titter = REditAss.GetIterAtLocation(roo.Left, roo.Top); // Marks char. at the top left of the visible section.
    BkMk1 = BuffAss.CreateMark("BkMk1", titter, true); // Cements the position with a bookmark. ('titter' will be
                                      // demolished at the next change to any of the text, hence the need for a bookmark.)
  }
/// <summary>
/// Marks the given buffer location (X, Y) with the supplied text mark.
/// </summary>
  private void MarkLocation(out TextMark Tarque, string TextMarkName, int X, int Y)
  { TextIter titter = REditAss.GetIterAtLocation(X, Y);
    Tarque = BuffAss.CreateMark(TextMarkName, titter, true);
  }
/// <summary>
/// If 'MarkViewPoint()' has been called (setting BkMk1), this scrolls the buffer up or down until bookmark BkMk1
/// is again at the top left of the text view. Having done so, it deletes BkMk1. (If BkMk1 had not been created,
/// or had been deleted, this method simply does nothing.)
/// </summary>
  private void ResumeViewPoint()
  { if (BkMk1 == null) return;
    REditAss.ScrollToMark( BkMk1, 0.0, true, 0.0, 0.0); // Last 3 args. --> Put the mark at top left corner of visibility.
//    BuffAss.DeleteMark(BkMk1);
  }

/// <summary>
/// <para>Used either to indent selected lines (Prefix = "\t") or to 'remark-out' selected lines (Prefix e.g. = "//").</para>
/// <para>Operates on selected text in whichever TextView has the focus.</para>
/// <para>If AllowUnselected is TRUE, and there is no selection, then the par. holding the cursor is selected, and operation proceeds.</para>
/// <para>Returns TRUE if the required action was successful, FALSE if either no TextView had focus or (!AllowUnselected &&
///   no text selected).</para>
/// </summary>
  private bool PrefixSelectedLines(string Prefix, bool AllowUnselected)
  { TextBuffer buff;
    if (WhichTextViewHasFocus(out buff) == 0) return false; // No TextView object has focus.
    bool wasSelection = buff.HasSelection;
    if (!wasSelection)
    { if (AllowUnselected) JTV.SelectParContainingCursor(buff);
      else return false; // If no selection, take no action.
    }
    string InText = JTV.ReadSelectedText(buff);
    if (InText == "") return false;
    // Now there definitely is a selection:
    int inlen = InText.Length;
    char[] coo = InText.ToCharArray();
    StringBuilder sb = new StringBuilder(2*inlen);
    sb.Append(Prefix);
    for (int i=0; i < inlen-1; i++)
    { sb.Append(coo[i]);
      if (coo[i] == '\n') sb.Append(Prefix);
    }
    sb.Append(coo[inlen-1]);
    TextIter startIt, endIt;
    buff.GetSelectionBounds(out startIt, out endIt); // At this stage startIt and endIt will coincide.
    int cursorOffset = startIt.Offset;
    bool boo = buff.DeleteSelection(true, true);  if (!boo) return false; // do nothing.
    buff.InsertAtCursor(sb.ToString());
    RecolourAllAssignmentsText("leave all");
    // Now recreate the selection, if there was one at the start:
    if (wasSelection) JTV.SelectText(buff, cursorOffset, sb.Length);
    return true;
  }
/// <summary>
/// <para>Used either to UNindent selected lines (Prefix = "\t") or to remove a remark symbol (e.g. "//") from the
/// front of selected lines.</para>
/// <para>Operates on selected text in whichever TextView has the focus. If text not selected, either returns doing nothing
///  (AllowUnselected FALSE) or else selects the line with the cursor and then proceeds as for the case with preselected lines.</para>
/// <para>Returns TRUE if the action went ahead.</para>
/// </summary>
  private bool UnPrefixSelectedLines(string Prefix, bool AllowUnselected)
  { TextBuffer buff;
    if (WhichTextViewHasFocus(out buff) == 0) return false; // No TextView object has focus.
    bool wasSelection = buff.HasSelection;
    if (!wasSelection)
    { if (AllowUnselected) JTV.SelectParContainingCursor(buff);
      else return false; // If no selection, take no action.
    }
    string InText = JTV.ReadSelectedText(buff);  if (InText == "") return false; // If no selection, allow the normal operation of tabbing.
    int prelen = Prefix.Length;
    if (InText.Substring(0, prelen) == Prefix) InText = InText.Substring(prelen); // remove any starting prefix.
    InText = InText.Replace("\n" + Prefix, "\n");
    TextIter startIt, endIt;
    buff.GetSelectionBounds(out startIt, out endIt); // At this stage startIt and endIt will coincide.
    int cursorOffset = startIt.Offset;
    bool boo = buff.DeleteSelection(true, true);  if (!boo) return false; // do nothing.
    buff.InsertAtCursor(InText);
    RecolourAllAssignmentsText("leave all");
    // Now recreate the selection, if there was one at the start:
    if (wasSelection) JTV.SelectText(buff, cursorOffset, InText.Length);
    return true;
   }

// In what follows, 'segment' refers to text being copied (for copy fn.) or to text being replaced (for replacing fun.).
// If the task is COPYING, Replacemt is ignored. Otherwise it replaces the text segment.
// If FromCue is "", it is ignored, and FromPtr is accessed; otherwise FromPtr is ignored - EXCEPT in that if FromPtr is 0, FromCue itself
// is excluded from the segment; but if it has any other value, FromCue is included. All the same applies to ToCue and ToPtr.
  public Quad REditAssTextOpn(char DoWhat, string Replacemt, string FromCue, int FromPtr, string ToCue, int ToPtr)
  { Quad result = new Quad(false);
    TextIter startIt, endIt;
    BuffAss.GetSelectionBounds(out startIt, out endIt);
    int startptr=-1, endptr=-1;
    if (FromCue == "") { startptr = FromPtr; if (startptr < 0) { result.S = "start ptr. cannot be negative"; return result; } }
    else
    { startptr = BuffAss.Text.IndexOf(FromCue);
      if (startptr == -1){ result.S = "start cue not found"; return result; }
      if (FromPtr == 0)  startptr += FromCue.Length; // --> startptr points to the first char. past FromCue.
    }
    if (ToCue == "") { endptr = ToPtr; if (endptr < 0) { result.S = "end ptr. cannot be negative"; return result; } }
    else
    { endptr = BuffAss.Text.IndexOf(ToCue, startptr);
      if (endptr == -1) { result.S = "end cue not found"; return result; }
      if (ToPtr != 0) endptr += ToCue.Length;
      endptr--; // points to last char. to be copied.
    }
    if (DoWhat == 'C') result.S = BuffAss.Text._FromTo(startptr, endptr);
    else if (DoWhat == 'R') result.S = BuffAss.Text._ScoopTo(startptr, endptr, Replacemt);
    else { result.S = "program error: wrong 'DoWhat' argument in MainWindow object's REditAssTextOpn(.)"; return result; }
    BuffAss.SelectRange(startIt, endIt);

    REditAss.GrabFocus();
    result.B = true;  return result;
  }

  public string CopyOfREditAssText()
  {
     return REditAss.Buffer.Text;
  }

// Replacing function, called from system functions unit. Replaces a segment of REditAss text with string Replacemt.
//  If FromCue is "", it is ignored, and FromPtr is accessed;
//  otherwise FromPtr is ignored - EXCEPT in that if FromPtr is 0, FromCue itself is excluded from the segment; but
//  if it has any other value, FromCue is included. All the same applies to ToCue and ToPtr.
  public Quad GraftREditAssText(string Replacemt, string FromCue, int FromPtr, string ToCue, int ToPtr)
  { Quad result = new Quad(false);
    TextIter startIt, endIt;
    BuffAss.GetSelectionBounds(out startIt, out endIt);
    int startptr=-1, endptr=-1;
    if (FromCue == "") { startptr = FromPtr; if (startptr < 0) { result.S = "start ptr. cannot be negative"; return result; } }
    else
    { startptr = BuffAss.Text.IndexOf(FromCue);
      if (startptr == -1){ result.S = "start cue not found"; return result; }
      if (FromPtr == 0)  startptr += FromCue.Length; // --> startptr points to the first char. past FromCue.
    }
    if (ToCue == "") { endptr = ToPtr; if (endptr < 0) { result.S = "end ptr. cannot be negative"; return result; } }
    else
    { endptr = BuffAss.Text.IndexOf(ToCue, startptr);
      if (endptr == -1) { result.S = "end cue not found"; return result; }
      if (ToPtr != 0) endptr += ToCue.Length;
      endptr--; // points to last char. to be copied.
    }
    result.S = BuffAss.Text._ScoopTo(startptr, endptr, Replacemt);
    BuffAss.SelectRange(startIt, endIt);
    REditAss.GrabFocus();
    result.B = true;  return result;
  }


// Intended to service SysFns functions. Act = "copy" --> .S holds text between cues (and argmt. Replacement is ignored); Act = "replace" -->
// .S holds the WHOLE of REditAss text after 'replace' has replaced whatever previously lay between the cues. (Cues remain in situ.)
// If FromCue = "", search begins at the start of REditAss text; if ToCue = "", it continues to the end of REditAss text.
  public Quad ProcessREditAssText(string Act, string FromCue, string ToCue, string Replacement)
  { Quad result = new Quad(false);
    TextIter startIt, endIt;
    BuffAss.GetSelectionBounds(out startIt, out endIt);
    SelStore.X = startIt.Offset;  SelStore.Y = endIt.Offset; // global to unit; allows calling code to access prior values.
    int startptr = -1, endptr = -1;
    if (FromCue == "") startptr = 0;
    else
    { startptr = BuffAss.Text.IndexOf(FromCue);
      if (startptr == -1){ result.S = "start cue not found"; return result; }
      startptr += FromCue.Length; // startptr now points to the first char. past FromCue.
    }
    if (ToCue == "") endptr = BuffAss.Text.Length - 1;
    else
    { endptr = BuffAss.Text.IndexOf(ToCue, startptr);
      if (endptr == -1){ result.S = "end cue not found"; return result; }
      endptr--;
    }
    // The text has been located, so do something with it:
    if (Act == "copy")
    { result.S = BuffAss.Text._FromTo(startptr, endptr);
      BuffAss.SelectRange(startIt, endIt);
      REditAss.GrabFocus();
      result.B = true; return result;
    }
    else if (Act == "replace")
    { result.S = BuffAss.Text._Extent(0, startptr) + Replacement + BuffAss.Text._Extent(endptr + 1);
      result.B = true; return result;
    }
    else { result.S = "faulty argument 'Act'";  return result; }
  }
/// <summary>
/// Focusses the window. If WindowID = 'A' (case-sensitive), the Asst. Window, and recoloring is invoked; if 'R', the Results Window;
///   anything else --> no effect. For 'A', recolouring is invoked.
/// </summary>
  public void FocusWindow(char WindowID)
  { if      (WindowID == 'A') { REditAss.GrabFocus(); RecolourAllAssignmentsText("leave all"); }
    else if (WindowID == 'R') REditRes.GrabFocus();
  }
/// <summary>
/// WindowID must be 'A' or 'R' (case-sensitive), or -1 is returned (the sign of error).
/// If WhereTo is less than 0, no movement of the cursor occurs. If WhereTo is beyond the end of text, it goes to the last character of text.
/// In any case, the cursor position after any changes is returned.
/// </summary>
  public int CursorPosition(char windowID, int WhereTo)
  { TextBuffer Buff;
    if (windowID == 'A') Buff = BuffAss;  else if (windowID == 'R') Buff = BuffRes;  else return -1;
    if (WhereTo >= 0)
    { int textLen = Buff.Text.Length;
      if (WhereTo > textLen) WhereTo = textLen;
      JTV.PlaceCursorAt(Buff, WhereTo);
    }
    return Buff.CursorPosition;
  }

/// <summary>
/// Designed for use outside of the window, as REditAss is then inaccessible. Place cursor in the line numbered LineNo (0 being the first line),
/// at char. offset OffsetInLine within that line (0 being the start).
/// OffsetInLine may be negative. Optionally scroll to it, hopefully with the line at midscreen level.
/// </summary>
  public void PlaceCursorAtAssWindowLine(int LineNo_base0, int OffsetInLine, bool andScrollToIt)
  { JTV.PlaceCursorAtLine(REditAss, LineNo_base0, OffsetInLine, andScrollToIt);
  }

/// <summary>
/// Clears the window. If WindowID = 'A' (case-sensitive), the Asst. Window; if 'R', the Results Window; anything else --> no effect.
/// </summary>
  public void ClearWindow(char WindowID)
  { if      (WindowID == 'A') BuffAss.Clear();
    else if (WindowID == 'R') BuffRes.Clear();
  }
/// <summary>
/// Returns all text in the window. If WindowID = 'A' (case-sensitive), the Asst. Window; if 'R', the Results Window; anything else returns "".
/// </summary>
  public string ReadWindow(char WindowID)
  { if      (WindowID == 'A') return BuffAss.Text;
    else if (WindowID == 'R') return BuffRes.Text;
    else return "";
  }
/// <summary>
/// <para>Writes TheText to the window.  If WindowID = 'A' (case-sensitive), to the Asst. Window; if 'R', to the Results Window;
/// anything else --> no effect.</para>
/// <para>'Where' (case-sens.) values: "fill" = replace any current text with this text; "cursor" = at current cursor position
///   (overwriting any selected text);
/// "start" = at start (moving existing text up);  "append" = append (i.e. at end of existing text); "1234" = start at
///  the specific character position 1234 (adjusted back to text extreme, if outside of existing text). Any other
///  value aborts the method.</para>
///  <para>If 'Formatted' TRUE, formatting tags are recognized (as at the bottom of file JTextView.cs); otherwise not.</para>
///  <para>If 'LeaveSelected' present and [0] is TRUE AND new text has been inserted, then that text will be selected.</para>
/// </summary>
  public void WriteWindow(char WindowID, string TheText, string Where, bool Formatted, params bool[] LeaveSelected)
  { TextView tv = null;
    if      (WindowID == 'A') tv = REditAss;
    else if (WindowID == 'R') tv = REditRes;
    else return;
    bool leaveSelected = (LeaveSelected.Length > 0  && LeaveSelected[0]); 
    // FORMATTING TAGS HANDLED:
    if (Formatted)
    { JTV.DisplayMarkUpText(tv, ref TheText, Where);
    }
    else // NO FORMATTING IS HANDLED:
      // Where to put the stuff?
    { TextBuffer Buff = tv.Buffer;
      int bufftextlen = Buff.Text.Length, buffOffset = 0;
      TextIter PutItHere;  bool clearBuff = false;
      if (Where == "cursor") // This option needs special treatment:
      { buffOffset = Buff.CursorPosition; 
        JTV.OverwriteSelectedText(Buff, TheText, leaveSelected);
                 // Get rid of any existing selection before inserting the replacement
      }
      else // all the rest have a common last action:
      {
        if      (Where == "fill" || bufftextlen == 0) { clearBuff = true;  PutItHere = Buff.StartIter; }
        else if (Where == "start")  PutItHere = Buff.StartIter;
        else if (Where == "append") { buffOffset = bufftextlen;  PutItHere = Buff.EndIter; }
        else
        { buffOffset = Where._ParseInt(-1);  if (buffOffset < 0) return;
          if (buffOffset >= bufftextlen) { buffOffset = bufftextlen;  PutItHere = Buff.EndIter; }
          else PutItHere = Buff.GetIterAtOffset(buffOffset);
        }
        // So... put it there.
        if (clearBuff) // then this must be "fill":
        { Buff.Text = TheText;
          if (leaveSelected) Buff.SelectRange(Buff.StartIter, Buff.EndIter);
        }
        else // "start" or a numerical offset or "append". Leave buffer text as is,
             //   and just add this new text into it at the desired position:
        { int selStartOffset = PutItHere.Offset; // this and the next one only used if text to stay selected.
          int selEndOffset = selStartOffset + TheText.Length;
          Buff.PlaceCursor(PutItHere);
          Buff.InsertAtCursor(TheText);
          // Where requested, have the added text selected:
          if (leaveSelected)
          { TextIter foo = Buff.StartIter;   foo.ForwardChars(selStartOffset);
            TextIter bar = Buff.StartIter;   bar.ForwardChars(selEndOffset);
            Buff.SelectRange(foo, bar);
          }
        }
      }
    }
  }


//------------------------------------------------------------
// HISTORY STACK -- UNDO / REDO                          //history  //undo


// The present strategy: Every change produces a stack record EXCEPT the typing (or otherwise adding) of successive
//  characters, une a une, said characters not including linefeed, tab, or exotica above the tilda.
//  *** It might be polite to offer the user a choice of this and char-by-char records. If so, increase the present back-tracking
//  limit 'HistoryStackMaxSize' to something more generous.
// Another strategy: If you undo some levels, then make any change, then undo again, redo's will only take you back to the
//  change just made; earlier levels are lost forever.
  private void UpdateHistoryStack()
  { if (Undoing || Redoing) return; // as stack manipulations handled separately by Undo() and Redo().
    ChangeRec changes = ChangeRec.ChangeInfo(REditAss, LastAssText, BuffAss.Text);
    if (changes.OldStrip != "" ||  changes.NewStrip != "")
    { // Remove any records ABOVE the history ptr; if there are any, it means that there has been a (nett) undoing, and
      //  now we are adding new corrections/additions; therefore we throw away all that we have backtracked from.
      while (HistoryStack.Count > HistoryPtr + 1) HistoryStack.RemoveAt(HistoryStack.Count - 1);
      // Set the cue from its default (-1) to the cursor posn, as a flag to indicate that just one printable symbol was added at the prior cursor posn.
      if (changes.OldStrip.Length == 0 &&  changes.NewStrip.Length == 1) changes.Cue =changes.CursorPos;
      int hcnt = HistoryStack.Count;    bool makeANewStackItem = true;
      LastAssText = BuffAss.Text;
      if (HistoryStack[hcnt-1].Cue >= 0 && changes.Cue == HistoryStack[hcnt-1].Cue + 1) // then this 'change' AND the last
              // added one (i.e. top of the stack) are in response to one char. being added at the same spot:
      { char ch = changes.NewStrip[0];
        if (ch >= ' ' && ch <= '\u007e') makeANewStackItem = false; // from space to tilda - includes all letters, numbers, normal punctuation.
      }
      if (makeANewStackItem) // put 'changes' into the stack:
      { HistoryStack.Add(changes);
        if (HistoryStack.Count == HistoryStackMaxSize) HistoryStack.RemoveAt(0);
        HistoryPtr = HistoryStack.Count - 1; // points at latest entry.
      }
      else // Just modify the latest stack item:
      { HistoryStack[hcnt-1].BackLastEqualNew++;    HistoryStack[hcnt-1].NewStrip += changes.NewStrip;
        HistoryStack[hcnt-1].Cue++;   HistoryStack[hcnt-1].CursorPos++;
      }
    }
  }

// Fired by method OnBuffAssChangedEvent(.). If the view has moved (as indicated by change of the coord. of the top of the view)
//  the navigation stack is updated.
  private void UpdateNavigationStack()
  { if (Navigating) { Navigating = false; return; }
    Duo thisLocn = new Duo( REditAss.VisibleRect.Top, BuffAss.CursorPosition);
    int stackSize = NavigationStack.Count;
    Duo topLocn = NavigationStack[stackSize -1];
    if (thisLocn.X != topLocn.X)
    { // then there is enough change to justify pushing the current location onto the stack:
      if (stackSize == NavigationStackMaxSize) NavigationStack.RemoveAt(0);
      NavigationStack.Add(thisLocn);
      NavigationPtr = NavigationStack.Count-1;
    }
  }

  private void Undo()
  { if (HistoryPtr == 0) return; // also true if HistoryStack is NULl or History stack has one or no entries.
    Undoing = true;
    // Retrieve record and update the stack pointer:
    ChangeRec change = HistoryStack[HistoryPtr];
    // Undo the change:
    string ss = BuffAss.Text._Extent(0, change.FrontLastEqual + 1);
    ss += change.OldStrip;
    ss += BuffAss.Text._Extent(change.BackLastEqualNew);
    BuffAss.Clear();
    BuffAss.Text = ss;
    LastAssText = ss;
    HistoryPtr--;
    RecolourAllAssignmentsText("remove all");
    Undoing = false;
    // Flush pending events to keep the GUI reponsive. (Without this, attempts to return the cursor to its earlier
    // position of change.VisibleTop are doomed to failure; ResetVisiblePartOfBuffer() is always done before the old
    // buffer is swept away, so that it has no impact on the new buffer.
    while (Application.EventsPending ())   Application.RunIteration();
    JTV.PlaceLocnAtTopOfView(REditAss, BuffAss, change.VisibleTop);
    JTV.PlaceCursorAt(BuffAss, change.CursorPos);
  }

  private void Redo()
  { if (HistoryPtr == HistoryStack.Count - 1) return; // also true if HistoryStack is NULl or History stack has one or no entries.
    Redoing = true;
    // Retrieve record and update the stack pointer:
    ChangeRec change = HistoryStack[HistoryPtr+1];
    // Redo effect of previous Undo:
    string ss = BuffAss.Text._Extent(0, change.FrontLastEqual + 1);
    ss += change.NewStrip;
    ss += BuffAss.Text._Extent(change.BackLastEqualOld);
    BuffAss.Clear();
    BuffAss.Text = ss;
    RecolourAllAssignmentsText("remove all");
    ResumeViewPoint(); // back to the original viewing position in the buffer.
    LastAssText = ss;
    HistoryPtr++;
    Redoing = false;
    while (Application.EventsPending ())   Application.RunIteration();
    JTV.PlaceLocnAtTopOfView(REditAss, BuffAss, change.VisibleTop);
    JTV.PlaceCursorAt(BuffAss, change.CursorPos);
  }
/// <summary>
/// <para>Gives access to text of labels from code outside that of the window instance.</para>
/// <para>WhichLabel: 'A' = LblAss; 'R' = LblRes; 'C' = lblComments; 'T' = main window title. (Note that future file I/O processes
///   will overwrite this setting of the title.)</para>
/// <para>DoWhat: 'R' --> read only (return the current value; NewText ignored); 'W' = write only (return empty string);
///  'D' = write the default text of this label; 'E' = write default text followed by NewText. Anything else does nothing but return "".</para>
/// </summary>
  public string AdjustLabelText(char WhichLabel, string NewText, char DoWhat)
  { string result = "";
    // Main window title option:
    if   (WhichLabel == 'T')
    { if      (DoWhat == 'R') return ThisWindow.Title;
      else if (DoWhat == 'W') { ThisWindow.Title = NewText;  return result; }
    }
    // Remaining options are all for Gtk.labels:
    Gtk.Label thisLabel = null;
    if      (WhichLabel == 'A') thisLabel = LblAss;
    else if (WhichLabel == 'R') thisLabel = LblRes;
    else if (WhichLabel == 'C') thisLabel = LblComments;
    else return result;
    // For all labels...
    if      (DoWhat == 'R') result = thisLabel.Text;
    else if (DoWhat == 'W') thisLabel.Markup = JTV.PrepareEscapedTextForPango(NewText); // Allows Pango formatting tags.
    else if (DoWhat == 'D' || DoWhat == 'E')
    { string ss = "";
      if      (WhichLabel == 'A') ss = LblAssDefaultText;
      else if (WhichLabel == 'R') ss = LblResDefaultText;
      else if (WhichLabel == 'C') ss = " ";
      if (DoWhat == 'E') ss += NewText;
      thisLabel.Markup = ss;
    }
    return result;
  }


/// <summary>
/// <para>Displays text with markup tags as defined at the end of source code file 'jTextView.cs'.</para>
/// <para>'Where' case-sens. values: "fill" = replace any current text with this text; "cursor" = at current cursor position;
/// "start" = at start (moving existing text up);  "append" = append (i.e. at end of existing text); "1234" = start at
///  the specific character position 1234 (adjusted back to text extreme, if outside of existing text). Any other
///  value aborts the method. NB! If old text is to remain, (a) tags applying at the insertion position will apply to the
///  new text (e.g. if you are appending text, and the old text has tags that go to the end of the buffer, then they will
///  also apply to the end of the added text). (b) tags have priority, so that if the old text has a higher priority tag
///  applying at the position where you are inserting new text, the old tag will overrule any tags of the same type
///  applying to part of the new text. BEST POLICY: Make sure no tags are still in operation for the final char. before the
///  insertion point.</para>
/// </summary>
  public void AddTextToREditRes(string FormattedText, string Where)
  { JTV.DisplayMarkUpText(REditRes, ref FormattedText, "append");
  }

//------------------------------------------------------------

//ꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏ
// OPERATIONS HEADQUARTERS                                 //operations   //hq
//ꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏ

// HOLDING LOOP DURING INTERRUPTS:
  public static char CheckStopNow()
  { while (Application.EventsPending ())   Application.RunIteration();
    return StopNow;
  }

/// <summary>
/// <para>This is called in three cases where the program run is to be suspended but be capable of resuming: (1) where the 'GO' ( = 'FREEZE')
/// button was clicked (PauseIndex -1); (2) where system function 'pause(n)' has struck (PauseIndex = n, range 0 to 9);
/// (3) at a set break point (PauseIndex = 100 + the line no. of the break point).</para>
/// <para>RETURNS true if 'abort' button click ended the run, otherwise false.</para>
/// </summary>
  public static bool HoldingPattern(int PauseIndex)
  {// ADJUST THE WINDOW HEADING:
    CurrentRunStatus = 4 + PauseIndex; // will be 3 for a loop hold, 4 to 13 for pause indices 0 to 9, 104 for a breakpoint.
    ThisWindow.REditAss.GrabFocus();
    ThisWindow.BtnGO.Label = "RESUME"; // Go button text.
    string ss;  int lineno, breakptLineNo = -1;
    if (CurrentRunStatus >= 3)
    { if (CurrentRunStatus == 3) ss = RunStatusText[CurrentRunStatus];
      else if (CurrentRunStatus >= 104) // a BREAK POINT, index = 104 plus the line no.:
      { ss = RunStatusText[5]; // R.S.T. holds suffixes for the title of the window. This one is "[BREAK POINT]".
        breakptLineNo = lineno = CurrentRunStatus - 104;
        JTV.PlaceCursorAtLine(ThisWindow.REditAss, lineno, 0, true); // scroll to the line, displaying it centrally if possible.
        JTV.TagLine(ThisWindow.REditAss, currentbreak_text, lineno, '+', false, false);
          // We don't do this with 'pause' below, because with 'pause' you may sometimes not want focus to move from its present spot.
      }
      else // = 4 plus index of PAUSE:
      { ss = (RunStatusText[4]).Replace("#", (CurrentRunStatus-4).ToString() ); // suffix "[PAUSE #]" - here we replace '#' with the number.
        lineno = R.FlowLine[R.ThisFn][R.ThisBubble].Ref;
        JTV.TagLine(ThisWindow.REditAss, query_text, lineno, '+', true, true);
      }
      ThisWindow.Title = ClearTextAddend(ThisWindow.Title) + ss;
      while (CurrentRunStatus >= 3)
      {
        ThisWindow.REditAss.ModifyBase(StateType.Normal, ColourAssBackPaused); // Change REditAss background colour for duration of the hold.
        while (Application.EventsPending ())   Application.RunIteration();
        ThisWindow.REditAss.ModifyBase(StateType.Normal, ColourAssBack);
        if (StopNow == 'A') // User has chosen to abort the program.
        { ThisWindow.REditAss.GrabFocus();
          if (CurrentRunStatus < 104) // exclude break points, as cursor is already where it should be.
          { JTV.PlaceCursorAtLine(ThisWindow.BuffAss, R.FlowLine[R.ThisFn][R.ThisBubble].Ref, 0); }
          return true;
        }
        if (CurrentRunStatus < 3)
        { if (breakptLineNo >= 0) JTV.TagLine(ThisWindow.REditAss, currentbreak_text, breakptLineNo, '-', false, false);
          ThisWindow.BtnGO.Label = "FREEZE"; // i.e. hold is over, so GO button text --> its normal value while a program is running.
        }
      }
    }
    return false;
  }

//ꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏ
// GDK-LEVEL ROUTINES                                 //gdk   //gtk
//ꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏ
// (A) FIELDS FOR INTERNAL USE, specific to the display system:
//   *** NB - Don't assign values to any fields that are / may later be set by the INI file, as values are assigned before INI is accessed.
//            Assign such fields in method DisplaySystemInitializing() below.
//  (A1) STATIC FIELDS:
  public static string FontSecondOptions; // To be added to desired font name, in case desired font not on the system.
  public static Pango.FontDescription REditAssFont, REditResFont;
  public static TextTag // Don't worry about the order of defining; it is the order of insertion into Taggery that sets the hierarchy.
    find_text = new TextTag("find_text"),      replaced_text = new TextTag("replaced_text"), ignore_text = new TextTag("ignore_text"),
    header_text = new TextTag("header_text"),  comment_text = new TextTag("comment_text"),
    query_text = new TextTag("query_text"),    oops_text = new TextTag("oops_text"),
    breakpoint_text = new TextTag("breakpoint_text"),    currentbreak_text = new TextTag("currentbreak_text"),
    bold_text = new TextTag("bold_text");

  public static TextTagTable Taggery = new TextTagTable(); // Both REditAss and REditRes use this.
  public static string[] AllCommentTagNames = new string[] { "ignore_text", "header_text", "comment_text" };
  public static Color ColourAssBack, ColourAssBackPaused, ColourResBack, ColourAssText;
  public static Color ColourComment, ColourHeader,  ColourIgnore;
  public static Color ColourFinds, ColourReplaced, ColourQuery, ColourBreakpoint, ColourCurrentBreak;
//  (A2) NONSTATIC FIELDS:
  public TextBuffer BuffAss, BuffRes; // Text buffers of REditAss and REditRes.

/// <summary>
/// Called once, from MainWindow's 'build()' method.
/// </summary>
  public void DisplaySystemInitializing()         //tags
  {
    ColourAssBack  = new Color(ClrAssBack[0],  ClrAssBack[1],  ClrAssBack[2]);
    ColourAssBackPaused = new Color(ClrAssBackPaused[0],  ClrAssBackPaused[1],  ClrAssBackPaused[2]);
    ColourResBack  = new Color(ClrResBack[0],  ClrResBack[1],  ClrResBack[2]);
    ColourAssText  = new Color(ClrAssText[0],  ClrAssText[1],  ClrAssText[2]);
    ColourComment  = new Color(ClrComment[0],  ClrComment[1],  ClrComment[2]);
    ColourHeader   = new Color(ClrHeader[0],   ClrHeader[1],   ClrHeader[2]);
    ColourIgnore   = new Color(ClrIgnore[0],   ClrIgnore[1],   ClrIgnore[2]);
    ColourFinds    = new Color(ClrFinds[0],    ClrFinds[1],    ClrFinds[2]);
    ColourReplaced = new Color(ClrReplaced[0], ClrReplaced[1], ClrReplaced[2]);
    ColourQuery    = new Color(ClrQuery[0],    ClrQuery[1],    ClrQuery[2]);
    ColourBreakpoint = new Color(ClrBreakpoint[0],    ClrBreakpoint[1],    ClrBreakpoint[2]);
    ColourCurrentBreak = new Color(ClrCurrentBreak[0],    ClrCurrentBreak[1],    ClrCurrentBreak[2]);
    FontSecondOptions = ",DejaVu Sans Condensed,Arial";
    REditAssFont = Pango.FontDescription.FromString(FontNameAss + FontSecondOptions + " " + FontPointsAss.ToString());
    REditResFont = Pango.FontDescription.FromString(FontNameRes + FontSecondOptions + " " + FontPointsRes.ToString());
    REditAss.ModifyBase(StateType.Normal, ColourAssBack); // Background colour of REditAss
    REditRes.ModifyBase(StateType.Normal, ColourResBack); // Background colour of REditRes
    REditAss.ModifyText(StateType.Normal, ColourAssText); // Default text colour in REditAss
    REditAss.ModifyFont(REditAssFont); // Default font in REditAss
    REditRes.ModifyFont(REditResFont); // Default font in REditAss
    // Order of adding to Taggery is vital, as later-added tag displays over earlier-added tags where there is overlap.
    Taggery.Add(breakpoint_text); // Should be overwritten by current break and by comments and find especially.
    Taggery.Add(currentbreak_text); // Should be overwritten by comments and find especially.
    Taggery.Add(comment_text);
    Taggery.Add(header_text);
    Taggery.Add(ignore_text);
    // The above are all overwritten by...
    Taggery.Add(find_text);
    Taggery.Add(replaced_text);
    Taggery.Add(query_text); // query tag must overwrite replaced tag must overwrite find tag.
    Taggery.Add(bold_text); // Text made bold by the remarks cue should stay bold, even with above tags applied.
    comment_text.ForegroundGdk = ColourComment;
    header_text.ForegroundGdk = ColourHeader;
    ignore_text.ForegroundGdk = ColourIgnore;
    find_text.BackgroundGdk = ColourFinds; // changes colour of background behind text.
    replaced_text.BackgroundGdk = ColourReplaced;
    query_text.BackgroundGdk = ColourQuery;
    breakpoint_text.BackgroundGdk = ColourBreakpoint;
    currentbreak_text.BackgroundGdk = ColourCurrentBreak;
    bold_text.Weight = Pango.Weight.Bold;
    BuffAss = new TextBuffer(Taggery);    REditAss.Buffer = BuffAss;
    BuffRes = new TextBuffer(Taggery);    REditRes.Buffer = BuffRes;
    this.BuffAss.Changed += new System.EventHandler(this.OnBuffAssChangedEvent);
    REditAss.LeftMargin = 20;   REditRes.LeftMargin = REditAss.LeftMargin;
  }

// The following stuff is entirely dedicated to 'RecolourAllAssignmentsText(.)', which immediately follows:
  public List<TextTag> TagsToGo = new List<TextTag>();
  public List<TextTag> TagsToStay = new List<TextTag>();
  public void TTTForeach(TextTag tag)
  { if (TagsToStay.IndexOf(tag) == -1) TagsToGo.Add(tag); }


  public bool MarkUpTagsNowON = false; // A flag used only by the following method, as a value that persists between its calls.
/// <summary>
/// <para>Recolours all of the text in the REditAss window, and additionally may remove other tags.</para>
/// <para>'OtherTagsAction' -- "leave all" - remove no other tags; "remove all" - removes all other tags (any third arg. is then ignored);
/// "remove except" - removes all other tags EXCEPT those listed in TagBag; "remove only" removes ONLY those listed in TagBag.</para>
/// <para>Error detection is not bothered with, so be sure to get the arguments right.</para>
/// </summary>
  public void RecolourAllAssignmentsText(string OtherTagsAction, params TextTag[] TagBag)
  { if (BuffAss.Text.Length == 0) return;
    if (UseMarkUpTags) // After calling code has just switched on UseMarkUpTags, this method must be entered; but not after that first time.
    { if (MarkUpTagsNowON) return; }
    else MarkUpTagsNowON = false; // Once out of markup tagging, this is reset, ready for the start of the next session of markup tagging.
    bool holdvalue = BlockTextChangeEvents;
    BlockTextChangeEvents = true;
    TextIter startIt = BuffAss.StartIter, endIt = BuffAss.EndIter;
    if (OtherTagsAction == "remove all") BuffAss.RemoveAllTags(startIt, endIt);
    else
    {// In all other situations the text colouring tags have to be removed:   //bkmk "//removetag"
      BuffAss.RemoveTag(comment_text, startIt, endIt);
      BuffAss.RemoveTag(header_text, startIt, endIt);
      BuffAss.RemoveTag(bold_text, startIt, endIt);
      BuffAss.RemoveTag(ignore_text, startIt, endIt);
      if (OtherTagsAction != "leave all")
      { TagsToGo.Clear();
        if (OtherTagsAction == "remove only") TagsToGo.AddRange(TagBag);
        else // "remove except":
        { TagsToStay.Clear();
          TagsToStay.AddRange(TagBag);
          Gtk.TextTagTableForeach Foodle = new Gtk.TextTagTableForeach(TTTForeach); // See just above this method for 'Foodle'.
          Taggery.Foreach(Foodle); // This will fill TagsToGo, being all extant tags except those in TagsToStay.
        }
        // TagsToGo is now valid:
        int tagsCnt = TagsToGo.Count;
        for (int tag=0; tag < tagsCnt; tag++)
        { BuffAss.RemoveTag(TagsToGo[tag], startIt, endIt);
        }
      }
    }
   // REACT ON CURRENT COLOURING SYSTEM:
    if (UseRemarksTags)
    { // **** Reinsert text-cued tags:  (If you are typing at the end of the whole text and are inside a cue - e.g. typing after "//" at the end
      //  of all text - you will see the default text colour for a fraction of a second after typing each letter. This does not occur if there is even
      //  one character beyond the insertion point. This is a quirk of Gdk; if a tag is defined up to the last char. in the buffer, then
      //  the next char. typed does not extend the tag to cover itself. No doubt one could program around this, but at present I have no urge to do so.
      ColourOneLiners(BuffAss, CommentCue, comment_text, startIt, endIt);
      ColourOneLiners(BuffAss, HeaderCue, header_text, startIt, endIt);
      ColourIgnoredText(BuffAss, startIt, endIt);
    }
    else if (UseMarkUpTags) // Will only be accessed if UseRemarksTags is FALSE.
    { string ss = BuffAss.Text;
      JTV.DisplayMarkUpText(REditAss, ref ss, "fill");
      MarkUpTagsNowON = true; // Causes future calls to this method to be ignored until markup display mode is cancelled.
    }
    BlockTextChangeEvents = holdvalue;
  }

  public void ColourOneLiners(TextBuffer thisBuffer, string CueStr, TextTag tagg, TextIter fromIt, TextIter toIt)
  { TextIter t1 = fromIt, m1, m2;
    bool found = true;
    int cueLen = CueStr.Length;
    while (found)
    { found = t1.ForwardSearch(CueStr, TextSearchFlags.TextOnly, out m1, out m2, toIt);
      if (found)
      {
        m2 = m1; // Can't use m2 from the above, as it may well be at the line end (if user has just typed the cue at the end of the line),
                 // so that the code below would put m2 at the end of the next line of the user's program.
        m2.ForwardToLineEnd();
        m2.ForwardChar(); // You have to cover the (first) line-ending code, otherwise any text you
                          //  add to the end of an existing remark reverts to default text colour.
        // DEAL WITH QUALIFIERS JUST AFTER THE CUE:
//        int T1 = t1.Offset; // *** If any deleting is ever required in the following in the future, it will be necessary to unremark
            // this step, to preserve the memory of t1, because 'Delete(.)' below would null all TextIter objects,
            // so that we would have to recreate t1 before the next 'while' loop, or --> crasho.
        TextIter p1 = m1;  p1.ForwardChars(cueLen); // to point to the character straight after the cue
        if (p1.Char == "!") // Qualifier used immediately after the cue string, to enforce bold text for the foregoing line.
        { TextIter p2 = m1;
          p2.BackwardLine();  p2.ForwardLine(); // I could not get a single instruction to find the start of the line of cue string.
          thisBuffer.ApplyTag(bold_text, p2, m1);
          // If adding similar thingos, note that you also have to adjust code which removes tags - search for bkmk "//" foll'd by "removetag".
        }
        thisBuffer.ApplyTag(tagg, m1, m2);
        t1 = m2;
      }
    }
  }

  /// <summary>
  /// Colours the ignored text, IF it takes the form "/* ... */" BUT NOT  "/*\ ... \*/". With the latter, the enclosed text
  ///  will still be excluded from the parsed program, but there will be no ignored-text tag applied. The other remarks tags,
  ///  if present, will therefore take on their usual colours.
  /// </summary>
  public void ColourIgnoredText(TextBuffer thisBuffer, TextIter fromIt,  TextIter toIt)
  { TextIter opener = fromIt, closer, m1, m2, m3;
    bool found = true;
    while (found)
    { found = opener.ForwardSearch(IgnoreCueOpen, TextSearchFlags.TextOnly, out m1, out m2, toIt);
      if (found)
      { opener = m1;
        found = m2.ForwardSearch(IgnoreCueClose, TextSearchFlags.TextOnly, out m1, out m3, toIt);
        if (found)
        { closer = m3; // 'closer' is set to just past the end of the IgnoreCueClose string.
          TextIter code1 = opener, code2 = closer; // We are looking for the combination "/*\ ... \*/", which means: don't recolour.
          code1.ForwardChars(2);  code2.BackwardChars(3);
          string ch1 = code1.Char, ch2 = code2.Char;

          if (ch1 == @"\" && ch2 == @"\") // Then this is a block which, though not part of program code, is NOT to be recoloured.
          { code1.ForwardChar();
            thisBuffer.ApplyTag(ignore_text, opener, code1); // Colour the 3-code opener, though not the blocked-out text
            thisBuffer.ApplyTag(ignore_text, code2, closer); // Same, for the closer.
          }
          else thisBuffer.ApplyTag(ignore_text, opener, closer); // Colour the bl. lot.
          opener = closer;
        }
      }
    }
  }
/// <summary>
/// If the focus is in REditAss, returns 1; if in REditRes, 2; if in neither (e.g. a button focussed), returns 0.
/// WhichBuffer returns pointing to the buffer of the focussed TextView, or if none focussed, is null.
/// </summary>
  private int WhichTextViewHasFocus(out TextBuffer WhichBuffer)
  { WhichBuffer = null; int result = 0;
    if      (REditAss.HasFocus) { WhichBuffer = BuffAss;  result = 1; }
    else if (REditRes.HasFocus) { WhichBuffer = BuffRes;  result = 2; }
    return result;
  }

/// <summary>
/// Fires only in two situations that I have so far identified: (1) a triple-click on the vertical scroll bar; (2) a single click on the
///   tiny bit of space between the edge of REditAss and the vertical scroll bar. Not much use, as far as I can see.
/// </summary>
  protected virtual void OnEboxAssButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
  {
   // JD.Msg("Button press event"); // *** Un-remark this, if you want to experiment.
  }

/// <summary>
/// Fires only in one situation that I have so far identified: when you click on SELECTED text in REditAss, whether that text was selected
///   by a double-click or by other means.
/// Data assignment to ButtonReleaseData:
///   [0] = button clicked (0 if none since user run startup OR if reset by fn. 'btnrelease()'; left = 1; middle = 2; right = 3.
///          NB: function 'btnrelease' resets ONLY [0]; all other values carry over from the last click (or are 0, if no clicks this run).
///   [1] = time of this click in msecs. since start of 1 AD.
///   [2] = time in msecs. since last click, or same as [1] if this is the first click this run.
///   [3], [4] currently unused, reserved for future timing entries.
///   [5] = CNTRL down, [6] = ALT down, [7] = SHIFT down (*** doesn't happen, as Mono's TextView hijacks shift + mouse click.)
///   [8] to [10] unused; reserved for other modifier keys detection in the future, if nec.
///   [11, 12] = (X, Y) of click relative to the top of this window.
///   [13, 14] = (X, Y) of click relative to the top of the screen.
/// </summary>
  protected virtual void OnEboxAssButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
  {// USE: args.Event.   and see what comes after you enter the '.'
   // JD.Msg("Button release event"); // *** Un-remark this, if you want to experiment.
    ButtonReleaseData[0] = (double) args.Event.Button; // 0 = no button press since startup or last service; is RESET to 0 by R.KillData()
                                                    //     and by system fn. "btnrelease()". 1 = left; 2 = middle; 3 = right.
    double x = ButtonReleaseData[1];
    ButtonReleaseData[1]  = (double) JS.Tempus('L',false); // Millisecs since 1 AD for this press.
    ButtonReleaseData[2] = ButtonReleaseData[1] - x; // Millisecs since last release event, or since 1 AD if none yet.
    // [3, 4] currently unused.
    // [5+] are HELPER KEY DETECTORS:
    if ( (args.Event.State & ModifierType.ControlMask) == ModifierType.ControlMask) x = 1.0; else x = 0.0;
    ButtonReleaseData[5] = x; // CONTROL is down.
    if ( (args.Event.State & ModifierType.Mod1Mask)    == ModifierType.Mod1Mask)    x = 1.0; else x = 0.0;
    ButtonReleaseData[6] = x; // ALTERNATE is down.
    if ( (args.Event.State & ModifierType.ShiftMask)   == ModifierType.ShiftMask)   x = 1.0; else x = 0.0;
    ButtonReleaseData[7] = x; // SHIFT is down. *** Currently always 0, as Mono's TextView hijacks shift + mouseclicks.
    // [8 to 10] currently unused, and reserved for other helper keys in the future (e.g. 'AltGr'). Omitted for now, as they
    //   are not guaranteed to be the same on all computers. (Even Mod1Mask is not  guaranteed to be 'Alt', but nearly always is.)
    ButtonReleaseData[11] = args.Event.X; // posn. relative to top of window
    ButtonReleaseData[12] = args.Event.Y;
    ButtonReleaseData[13] = args.Event.XRoot; // posn. relative to top of screen
    ButtonReleaseData[14] = args.Event.YRoot;
  }

  protected void PopUpMenu(string GroupPrefix, Strint[] SubMenuLabels, string[] Palette, EventHandler Eventful)
  {
    Menu gentlemenu = new Menu ();
    int len = SubMenuLabels.Length;  if (len == 0) return;
    AccelLabel[] axles = new AccelLabel[len];
    MenuItem[] men = new MenuItem[len];
    for (int i=0; i < len; i++)
    { axles[i] = new AccelLabel("");
      axles[i].UseMarkup = true;
      axles[i].Markup = "<span color=\"" + Palette[SubMenuLabels[i].I] + "\">" + SubMenuLabels[i].S + "</span>";
          // I have found no way to stop justification being central; 'axles[i].Justify = Justification.Left' does nothing, as does Pango gravity.
      men[i] = new MenuItem();  men[i].Name = GroupPrefix + i.ToString();
      men[i].Add(axles[i]);
      men[i].Activated += Eventful;
      axles[i].Show();
      men[i].Show();
      gentlemenu.Append(men[i]);
    }
    gentlemenu.Popup();
    gentlemenu.Dispose();
  }

  protected virtual void OnGoToScreenPosnBeforeLastRunActionActivated (object sender, System.EventArgs e)
  { if (BeforeGOPtr.X != -1) // i.e. if there has yet been a program run
    { JTV.PlaceLocnAtTopOfView(REditAss, BuffAss, BeforeGOPtr.X);
      JTV.PlaceCursorAt(BuffAss, BeforeGOPtr.Y);
    }
  }


    protected void OnREditAssPopulatePopup (object o, Gtk.PopulatePopupArgs args)
    {
      F1KeyHandler();
    }


    protected void OnShowSequenceOfFunctionCallsActionActivated (object sender, System.EventArgs e)
    {
      JD.PlaceBox(575, 475);
      string header = "MOST RECENT FUNCTION CALLS";
      string topPart = "<in 30><b>MOST RECENT FUNCTION CALLS</b>\n(up to the last " + R.UserFnLogSize.ToString() +"; call no. in square brackets)\n\n";
      string table = "", thisLine, calledFnName, callingFnName;
      foreach (object O in R.UserFnLog)
      { Trio kew = (Trio) O;
        int calledFnNo = kew.X, assNo = kew.Y, callNo = kew.Z;
        int callingFnNo = R.Assts[assNo][0].Fn;
        int lineRef = -1, bubbleNo = -1; // If the loop below ever fails, '-1' will appear in the message box.
        TBubble[] hubble = R.FlowLine[callingFnNo];
        for (int i=0; i < hubble.Length; i++)
        { if (hubble[i].Ass == assNo) { bubbleNo =  i;  lineRef = hubble[i].Ref; break; } }
        int lineNo = 1 + lineRef;
        calledFnName = P.UserFn[calledFnNo].Name;
        callingFnName = P.UserFn[callingFnNo].Name;
        thisLine = "<# skyblue>[" + callNo.ToString() + "]\\t<# blue>" + callingFnName + " <# black>called <# magenta>" + calledFnName +
            "<# black> at line " + lineNo.ToString() + " <# grey>(Asst. " + assNo.ToString() + ", Bubble " + bubbleNo + ")\n";
        table += thisLine; // this reverses the natural 'foreach' order, to have most recent calls displayed at the top.
      }
      int btn = JD.Display(header, topPart + table, true, false, false, "LENGTHEN", "CLOSE");
      if (btn == 1)
      { topPart = "Enter the new number of most recent function calls to be logged.\n" +
                  "The new value will only apply after the next run of this program, but then it will persist " +
                  "till you close this instance of MonoMaths.";
        string[] prompts = new string[] {"No. calls to be logged:"};
        string[] boxTexts = new string[] { R.UserFnLogSize.ToString() };
        int n = JD.InputBox("CHANGE NO. FUNCTION CALLS DISPLAYED", topPart, prompts, ref boxTexts, "ACCEPT", "CANCEL");
        if (n == 1)
        { int p = (boxTexts[0])._ParseInt(-1);
          if (p < 1) JD.Msg("The entry must be greater than zero, so no change was made.");
          else { R.UserFnLogSize = p; JD.Msg ("From the next program run, the log will hold up to " + p.ToString() + " function calls."); }
        }
      }
    }

  /// <summary>
  /// Order of argument elements: New left, new top, new width, new height. Negative value = retain old value for this dimension.
  /// For this overload, values > 0 and ≤ = 1 are relative to Screen dimension; beyond that, taken as pixels (rounded). Negatives stay negative.
  /// </summary>
  public Tetro ChangeWindowLocation(double[] NewData)
  { int[] IntData = new int[4];
    for (int i=0; i < 4; i++)
    { double x = NewData[i];
      if (x < 0) IntData[i] = -1;
      else if (x > 1.0) IntData[i] = Convert.ToInt32(x);
      else
      { if (i % 2 == 0) IntData[i] = Convert.ToInt32( x * (double) Screen.Width);
        else            IntData[i] = Convert.ToInt32( x * (double) Screen.Height);
      }
    }
    return ChangeWindowLocation(IntData[0], IntData[1],  IntData[2],  IntData[3]);
  }

/// <summary>
/// Returns .X1 = new left, .X2 = new top, .X3 = new width, .X4 = new height.
/// <para>If a dimension is negative, the existing value continues to apply (and is returned). Therefore if all are negative,
///  this method simply returns the current data without changing anything.</para>
/// <para>Minimum width and height are defined in the method; *** these can be reset if found undesirable.</para>
/// </summary>
  public Tetro ChangeWindowLocation(int NewLeft, int NewTop, int NewWidth, int NewHeight)
  {
    int FinalLeft, FinalTop, FinalWidth, FinalHeight; // Returned values
    // Retrieve existing values:
    int oldLeft, oldTop, oldWidth, oldHeight;
    int minWidth = 50, minHeight = 50; //####
    ThisWindow.GetSize(out oldWidth, out oldHeight);
    ThisWindow.GetPosition(out oldLeft, out oldTop);
    FinalLeft = oldLeft;  FinalTop = oldTop;  FinalWidth = oldWidth;  FinalHeight = oldHeight;
    // Set size first:
    if (NewWidth >= 0) // if negative, leave the above setting for FinalWidth
    { if (NewWidth < minWidth)  FinalWidth = minWidth;   else if (NewWidth > Screen.Width) FinalWidth = Screen.Width;
      else FinalWidth = NewWidth;
    }
    if (NewHeight >= 0) // if negative, leave the above setting for FinalHeight
    { if (NewHeight < minHeight) FinalHeight = minHeight;  else if (NewHeight > Screen.Height) FinalHeight = Screen.Height;
      else FinalHeight = NewHeight;
    }
    if (NewWidth >= 0 || NewHeight >=0) // no point in the call below, if the code calling this did not want to change dimensions.
    ThisWindow.Resize(FinalWidth, FinalHeight);
    // Now, position:
    if (NewLeft >= 0) // if negative, leave the above setting for FinalLeft
    { if (NewLeft > Screen.Width - FinalWidth) FinalLeft = Screen.Width - FinalWidth; else FinalLeft = NewLeft; }
    if (NewTop >= 0) // if negative, leave the above setting for FinalTop
    { if (NewTop > Screen.Height - FinalHeight) FinalTop = Screen.Height - FinalHeight; else FinalTop = NewTop; }
    if (NewTop >= 0 || NewTop >=0) // no point in the call below, if the code calling this did not want to change location.
    { ThisWindow.Move(FinalLeft, FinalTop); }
    return new Tetro(FinalLeft, FinalTop, FinalWidth, FinalHeight);
  }

/// <summary>
/// <para>WhichWindow: 'A' or 'R'. NewSize: Neg or 0 = no change, just return existing HeightRequests.(Ditto, if WhichWindow invalid.)
///  NewSize &#60; 20: Results unpredictable; usable results with some window sizes, unusable with others (weird effects). Use trial and error.
///  NewSize &#62; sum of existing HeightRequests less 20: corrected down to this value (for which Asst. Window is just a crack).</para>
/// <para>Always returns final values of height requests: .X = Asst Window height, .Y = Results Window height.
///  (Who knows whether their real heights are as requested?)</para>
/// <para>NB: This method DOES NOT CHANGE the actual size of the MonoMaths window: see method ChangeWindowLocation for that.</para>
/// </summary>
  public Duo Repartition(char WhichWindow, int NewSize)
  { int assHt = vbox12.HeightRequest,  resHt = vbox13.HeightRequest, bothHts = assHt + resHt;
    Duo result = new Duo(assHt, resHt);
    if (NewSize > 0)
    { if (NewSize > assHt + resHt - 20) NewSize = assHt + resHt - 20;
      // vbox13 holds REditRes and the buttons and label below it.
      // vbox12 holds REditAss and the label BELOW it (not the label above it).
      if (WhichWindow == 'A')
      { vbox12.HeightRequest = NewSize;
        vbox13.HeightRequest = bothHts - NewSize;
        result = new Duo (vbox12.HeightRequest, vbox13.HeightRequest);
      }
      else if (WhichWindow == 'R')
      { vbox13.HeightRequest = NewSize;
        vbox12.HeightRequest = bothHts - NewSize;
        result = new Duo (vbox12.HeightRequest, vbox13.HeightRequest);
      }
    }
    return result;
  }

}//☰☰☰☰☰  END OF CLASS 'MainWindow'  ☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰☰

public struct INIRecord // Don't use direct assignments "objectA = objectB"; instead use "objectA.CopyFrom(objectB)"
{ public string Nm; // What goes on LHS of "=" stmt on the disk file.
  public char Tp; // Type: 'X' for real; 'I' for int; 'L' for colour as four-byte hexadecimal; 'S' for string.
  public double X;
  public int I;
  public string S;
  public Color Clr; // If type is 'L', this will be a valid colour; otherwise Color.Black.
  public bool Valid; // When the INI file is parsed, correctly formatted INI file entries set Valid to TRUE; o'wise to FALSE.
  //  In the case of 'H', value goes to .I.
  public INIRecord(string Name, double DoubleValue)
  { Nm = Name;  Tp = 'X';   X = DoubleValue;  I = 0;        Clr = Color.Zero;  S = "";    Valid = true; }
  public INIRecord(string Name, int IntValue)
  { Nm = Name;  Tp = 'I';   X = 0.0;          I = IntValue; Clr = Color.Zero;  S = "";    Valid = true; }
  public INIRecord(string Name, Color Colour)
  { Nm = Name;  Tp = 'L';   X = 0.0;          I = 0;        Clr = Colour;      S = "";    Valid = true; }
  public INIRecord(string Name, byte[] RGB)
  { Nm = Name;  Tp = 'L';   X = 0.0;          I = 0;        Clr = new Color(RGB[0],RGB[1],RGB[2]); S = "";   Valid = true; }
  public INIRecord(string Name, string StringValue)
  { Nm = Name;  Tp = 'S';   X = 0.0;          I = 0;        Clr = Color.Zero;  S = StringValue;  Valid = true; }
  public void CopyFrom(INIRecord Model)
  { Nm = Model.Nm;  Tp = Model.Tp;   X = Model.X;   I = Model.I;  Clr = Model.Clr;  S = Model.S;  Valid = true; }
  public override string ToString()
  { return String.Format("Nm = '{0}';  Tp = '{1}';  X = {2];  I = {3};  Clr = {4}; S = '{5}'; Valid = {6}", Nm, Tp, X, I, Clr, S, Valid); }
}
/// <summary>
/// When two strings - call them 'oldstr' and 'newstr' - are compared, FrontLastEqual records the last char. from 0
///  which is the same for the two strings; working back from their tops, the last char. at which they are equal
///  is at BackLastEqualOld (in oldstr) and BackLastEqualNew (in newstr). That part of oldstr which lies inside FrontLastEqual
///  and BackLastEqualOld is stored in OldStrip; and analogously for newstr. Note that the location of changes may very well
///  not coincide with the location of whatever has just been done to text; for example, if oldstr = "AAABCDCDCD" and the last "CD"
///  is removed, this process will compare "AAABCDCDCD" and "AAABCDCD", and so will give the same result as if we had removed
///  either the second last or the third last "CD" pair.
/// </summary>
public class ChangeRec
{ public int FrontLastEqual, BackLastEqualOld, BackLastEqualNew;
  public string OldStrip, NewStrip;
  public int VisibleTop; // the .Top field of the visible rectangle, in buffer coords., after the change.
  public int CursorPos;
  public int Cue; // Can have different uses a/c needs of the application using it. Default is -1. (If used to store a
                  //  text pointer, then 0 would be a significant value.)
  public ChangeRec()
  { FrontLastEqual = -1;  BackLastEqualOld = 0;  BackLastEqualNew = 0;
    OldStrip = String.Empty;  NewStrip = String.Empty;  VisibleTop = 0;   CursorPos = 0;  Cue = -1;
  }
  public override string  ToString()
  { string ss = "FrontLastEqual = {0};  BackLastEqualOld = {1};  BackLastEqualNew = {2}; " +
                  "VisibleTop = {3};  CursorPos = {4};  Cue = {5}.\nOldStrip: \'{6}\'\nNewStrip:\'{7}\'\n";
    return String.Format(ss,  FrontLastEqual, BackLastEqualOld, BackLastEqualNew, VisibleTop, CursorPos, Cue, OldStrip, NewStrip);
  }

  public static ChangeRec ChangeInfo(Gtk.TextView TV, string OldStr, string NewStr)
  { ChangeRec result = new ChangeRec();
    Octet ox = JS.StrDifference(OldStr, NewStr);
    result.FrontLastEqual = ox.IX;
    result.BackLastEqualOld = (int) ox.XX;   result.BackLastEqualNew = (int) ox.XY;
    result.OldStrip = ox.SX;                 result.NewStrip = ox.SY;
    result.VisibleTop = TV.VisibleRect.Top;
    result.CursorPos = TV.Buffer.CursorPosition;
    return result;
  }
}

} // END OF NAMESPACE MonoMaths

/*
SOME GLEANED WISDOM RE TEXT VIEW MANIPULATION:  (Examples use 'tv' as the name of some TextView object.)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BACKGROUND COLOUR:  tv.ModifyBase(StateType.Normal, myColour);
DEFAULT TEXT COLOUR:  tv.ModifyText(StateType.Normal, thyColour);
DEFAULT FONT: Two steps:
  (1) public Pango.FontDescription myFont = Pango.FontDescription.FromString("Arial 12");
         (The above e.g. as a class variable; then in some method...)
  (2) tv.ModifyFont(myFont);
LEFT MARGIN: Use REditAss.LeftMargin as a read-write property.

HIGHLIGHT COLOUR:   This trivial example assumes there is enough text, and highlights from char. 7 to char. 13:
    TextBuffer buff = tv.Buffer;
    TextTag taggy = new TextTag("greenHighlighting");
    taggy.BackgroundGdk = new Color(0, 255, 0);
    buff.TagTable.Add(taggy);
    TextIter twitter1 = buff.GetIterAtOffset(7);
    TextIter twitter2 = buff.GetIterAtOffset(14); // one past the end, in the usual C fashion of iteration.
    buff.ApplyTag(taggy, twitter1, twitter2);

MARKUP TAGS FOR LABEL TEXTS: (You have to set the label's 'UseMarkUp' property first (can also do through IDE), then apply text as '.Markup = "..";'.
The quick stuff:
   <b>     Bold
   <big>   Makes font relatively larger
   <i>     Italic
   <s>     Strikethrough
   <sub>   Subscript
   <sup>   Superscript
   <small> Makes font relatively smaller
   <tt>    Monospace font
   <u>     Underline
More longwinded stuff (sorry for the untidy layout; it was pasted from DevHelp's "Text Attribute markup"):
A simple example of a marked-up string might be: "<span foreground="blue" size="x-large">Blue text</span> is <i>cool</i>!"
<span> attributes:
font[1], font_desc
A font description string, such as "Sans Italic 12". See pango_font_description_from_string() for a description of the format of the string representation . Note that any other span attributes will override this description. So if you have "Sans Italic" and also a style="normal" attribute, you will get Sans normal, not italic.
font_family, face
A font family name
font_size[1], size
Font size in 1024ths of a point, or one of the absolute sizes 'xx-small', 'x-small', 'small', 'medium', 'large', 'x-large', 'xx-large', or one of the relative sizes 'smaller' or 'larger'. If you want to specify a absolute size, it's usually easier to take advantage of the ability to specify a partial font description using 'font'; you can use font='12.5' rather than size='12800'.
font_style[1], style
One of 'normal', 'oblique', 'italic'
font_weight[1], weight
One of 'ultralight', 'light', 'normal', 'bold', 'ultrabold', 'heavy', or a numeric weight
font_variant[1], variant
One of 'normal' or 'smallcaps'
font_stretch[1], stretch
One of 'ultracondensed', 'extracondensed', 'condensed', 'semicondensed', 'normal', 'semiexpanded', 'expanded', 'extraexpanded', 'ultraexpanded'
foreground, fgcolor[1], color
An RGB color specification such as '#00FF00' or a color name such as 'red' [All the names are in a famous internet file called "X11rgb.txt";
I have stored both it and colour html version of it in the computer documentation file for Gtk.]
background, bgcolor[1]
An RGB color specification such as '#00FF00' or a color name such as 'red'
underline
One of 'none', 'single', 'double', 'low', 'error'
underline_color
The color of underlines; an RGB color specification such as '#00FF00' or a color name such as 'red'
rise
Vertical displacement, in 10000ths of an em. Can be negative for subscript, positive for superscript.
strikethrough
'true' or 'false' whether to strike through the text
strikethrough_color
The color of strikethrough lines; an RGB color specification such as '#00FF00' or a color name such as 'red'
fallback
'true' or 'false' whether to enable fallback. If disabled, then characters will only be used from the closest matching font on the system. No fallback will be done to other fonts on the system that might contain the characters in the text. Fallback is enabled by default. Most applications should not disable fallback.
lang
A language code, indicating the text language
letter_spacing
Inter-letter spacing in 1024ths of a point.
gravity
One of 'south', 'east', 'north', 'west', 'auto'.
gravity_hint
One of 'natural', 'strong', 'line'.


*/
