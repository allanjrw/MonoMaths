using System;
using System.Text;
//using System.Drawing;
//using System.Collections;
using System.Collections.Generic;
using Gdk;
using Gtk;
using Pango;
using GLib;

namespace JLib
{ // A library of utilities for widescale use in programs written by me. Spread over several files,

public class JD
{
  private JD(){} // THE CLASS CANNOT BE INSTANTIATED.

/// <summary>
/// <para>Sets dimensions for the next message box (other than that of method Msg(.) ); usually used in conjunction with field BoxCentre. Fields:
///  .X = Width in pixels, .Y = Height in pixels. Message box code ignores BoxSize if .X &lt;= 0.</para>
/// <para>A call to ANY box will reset .X and .Y to 0, whether or not their values were used for the box in question. To preserve the
///  current box's closing value ready for the next box instance, calling code should read LastBoxSize, which is automatically set by boxes when they close.</para>
/// <para>Why don't you just use method PlaceBox(.)? It sizes and centres in one go, and has inbuilt protection against silly arguments.</para>
/// </summary>
  public static Duo BoxSize = new Duo(-1, -1);
/// <summary>
/// <para>Sets screen position of the centre of the next message box (other than that of method Msg(.) ); usually used in conjunction
///  with field BoxSize. Fields are the pixel coordinates .X and .Y (.Y as usual being measured from the top of the screen).</para>
/// <para>A call to ANY box will reset .X and .Y to 0, whether or not their values were used for the box in question. To preserve the
///  current box's closing value ready for the next box instance, calling code should read LastBoxSize, which is automatically set by boxes when they close.</para>
/// <para>Why don't you just use method PlaceBox(.)? It sizes and centres in one go, and has inbuilt protection against silly arguments.</para>
/// </summary>
  public static Duo BoxCentre = new Duo(-1, -1); // The 'ignore' value is (-1, -1).
  public static Duo LastBoxSize = new Duo(-1, -1); // Always set by each box created below, except for boxes created using 'Msg(.)'.
  public static Duo LastBoxCentre = new Duo(-1, -1); // as above.
  public static Gdk.Color DefBackColour = new Gdk.Color(242, 255, 244); // The user's code can alter this.
  // Used to record cursor posn and selected text in method Display(.) (*** and maybe in other methods too, at a later date).
  public static string SelectedText = ""; // returns empty, of course, if no text selected.
  public static int SelectionPoint; // start of the selection, if any; otherwise the cursor position (which defaults to the end of text).
  public static int SelectionLine; // line of the selection, if any; otherwise -1.

  // STRUCTURE FOR HOLDING LINKS
  public struct Linkage
  { public IntPtr Handle; // Should hold the handle of the particular box feeding
    public int StartPtr, Extent; // first character and length of the displayable text in the link
    public string To; // 'buffer' (for text in the same TextBuffer as the linking tag); 'thisfile'
      // (for text in the same file, stored in RAM, but not nec. in the TextBuffer); '/home/../filename.txt'
      // (sufficient addressing info for detecting some other file on the same computer or network).
    public string BkMk; // if the sought bookmark tag is "<? blah>", this string would be "blah".
  // Constructor:
    public Linkage(IntPtr Handle_, int StartPtr_, int Extent_, string To_, string BkMk_)
    { Handle = Handle_;  StartPtr = StartPtr_;  Extent = Extent_;  To = To_;  BkMk = BkMk_; }
    public override string ToString ()
    { return string.Format ("Handle: {0};  StartPtr: {1};  Extent: {2};  To: '{3}';  BkMk: '{4}'",
                                                                          Handle, StartPtr, Extent, To, BkMk);
    }
    public override bool Equals (object obj)
      { if (obj == null || !(obj is Linkage)) return false;
        Linkage that = (Linkage) obj;
        if (Handle == that.Handle && StartPtr == that.StartPtr && Extent == that.Extent
              && To == that.To && BkMk == that.BkMk)  return true;
        return false;
      }
    public override int GetHashCode () // Have to override this when overriding Equals(.).
      { return ToString().GetHashCode();
      }
    public Linkage Copy()
    { Linkage result = new Linkage();
      result.Handle = Handle;  result.StartPtr = StartPtr;  result.Extent = Extent;  result.To = To;  result.BkMk = BkMk;
      return result;
    }
  }

  //LINK TANK:
  public static List<Linkage> Linkages = new List<Linkage>();

//================================================================================
//   METHODS other than box-generating ones
//================================================================================

/// <summary>
/// <para>Sets box size and location either in terms of fraction of screen extent (in which case the dimension
///  must be greater than 0 but not greater than 1) or in terms of a number of pixels (in which case it must exceed 1).</para>
/// <para>Fields will be valid (and so not set to -1) if both args. for each pair obey these conditions:
///  (a) the object is either of type double or type int; (b) its value is greater than zero.</para>
/// <para>Any of the four arguments may be replaced by a string (any string - e.g. "last"), in which case the setting
///  from any last message box (except the simple 'JD.Msg(.)' dialog box) will be reused; or defaults, if there was
///  no last message box. If the user had resized or moved that last message box, the final size and position
///  are what will be reused.</para>
/// <para>Corrections: (a) If (after conversion to pixels) width or height exceeds screen extreme it is set
///  to that extreme; (b) If (as pixels) width or height is less than 100 pixels, it is reset to 100 pixels.
///  (c) If either centre dimension would be too close to a margin (given PixelsWide and PixelsHigh), it is
///  adjusted to put the box at that margin.</para>
/// <para>In case of error in either size dimension, box size is set to the 'ignore' value (-1, -1). Similarly
///  for error in either argument for the box centre.</para>
/// </summary>
  public static void PlaceBox(object PixelsWide, object PixelsHigh, object CentralX, object CentralY)
  { Screen scream = Screen.Default;
    int screenW = scream.Width,  screenH = scream.Height;
   // BoxSize:
    Duo trialBoxSize;
    if (PixelsWide is string) trialBoxSize.X = LastBoxSize.X;
    else trialBoxSize.X = PixFromObject(PixelsWide, screenW, -1);
    if (PixelsHigh is string) trialBoxSize.Y = LastBoxSize.Y;
    else trialBoxSize.Y = PixFromObject(PixelsHigh, screenH, -1);
    if (trialBoxSize.X != -1 && trialBoxSize.Y != -1) BoxSize = trialBoxSize;
    else BoxSize = new Duo(-1, -1); // In all cases, error or not, a call to this method resets BoxSize.
   // BoxCentre:
    Duo trialBoxCentre;
    if (CentralX is string) trialBoxCentre.X = LastBoxCentre.X;
    else trialBoxCentre.X = PixFromObject(CentralX, screenW, screenW/2);
    if (CentralY is string) trialBoxCentre.Y = LastBoxCentre.Y;
    else trialBoxCentre.Y = PixFromObject(CentralY, screenH, screenH/2);
     BoxCentre = trialBoxCentre;
    if (BoxSize.X != -1) // Adjust centre, if there are valid box sizes, and the centre is too close to a margin:
    { int halfWd = BoxSize.X/2, halfHt = BoxSize.Y/2;
      if (BoxCentre.X < halfWd) BoxCentre.X = halfWd;
      else if (BoxCentre.X > screenW - halfWd) BoxCentre.X = screenW - halfWd;
      if (BoxCentre.Y < halfHt) BoxCentre.Y = halfHt;
      else if (BoxCentre.Y > screenH - halfHt) BoxCentre.Y = screenH - halfHt;
    }
  }

/// <summary>
/// <para>Sets box size either in terms of fraction of screen extent (in which case the dimension
///  must be greater than 0 but not greater than 1) or in terms of a number of pixels (in which case it must exceed 1).</para>
/// <para>Fields will be valid (and so not set to -1) if both args. obey these conditions:
///  (a) the object is either of type double or type int; (b) its value is greater than zero.</para>
/// <para>Either argument may be replaced by a string (any string - e.g. "last"), in which case the setting
///  from any last message box (except the simple 'JD.Msg(.)' dialog box) will be reused; or defaults, if there was
///  no last message box. If the user had resized or moved that last message box, the final size will be reused.</para>
/// <para>Corrections: (a) If (after conversion to pixels) width or height exceeds screen extreme it is set
///  to that extreme; (b) If (as pixels) width or height is less than 100 pixels, it is reset to 100 pixels.</para>
/// <para>NEGATIVE ARGS: If either arg. is negative, BoxSize is reset to the default (-1, -1).</para>
/// </summary>
  public static void PlaceBox(object PixelsWide, object PixelsHigh)
  { PlaceBox(PixelsWide, PixelsHigh, 0.5, 0.5); }

/// <summary>
/// All applies as for the four-argument version, for the four fields X1 ... X4  of Placement.
/// </summary>
  public static void PlaceBox(Tetro Placement)
  { PlaceBox(Placement.X1, Placement.X2, Placement.X3, Placement.X4); }


// Used for the above:
  private static int PixFromObject(object Pix, int availableExtent, int SwapForNegValue)
  { int result;  double testval;
    if (Pix is double)
    { testval = (double) Pix;
      if (testval <= 1.0) testval *= availableExtent;
      result = (int) testval;
    }
    else if (Pix is int)
    { result = (int) Pix;
      if (result == 1) result = availableExtent;
    }
    else return -1;
    // Get the pixel value within limits:
    if (result < 0) result = SwapForNegValue;
    else if (result > availableExtent) result = availableExtent;
    return result;
  }

  /// <summary>
  /// A small number of JTextView tags can be converted to Pango tags. Those that can, are. The rest are left as is.
  /// </summary>
  public static string ConformTagsToPangoTags(string Str)
  {
    Str = Str.Replace("<B>", "<b>");    Str = Str.Replace("</B>", "</b>"); // Pango tags are case-sensitive.
    Str = Str.Replace("<I>", "<i>");    Str = Str.Replace("</I>", "</i>");
    Str = Str.Replace("<U>", "<u>");    Str = Str.Replace("</U>", "</u>");
    Str = Str.Replace("<^>", "<sup>");  Str = Str.Replace("</^>", "</sup>");
    Str = Str.Replace("<v>", "<sub>");  Str = Str.Replace("</v>", "</sub>");
    return Str;
  }

//================================================================================
//   METHODS generating BOXES                                           //box
//================================================================================

/// <summary>
/// <para>Simple message dialog; displays the message + a 'close' button. The dialog box
/// swells to fit the text, though its width limit is 450 pixels, and there is no scroll bar if
/// the text is too much for the screen height (so that you cannot see the end of a long text).</para>
/// <para>Note that simple Pango markup tags apply (but NOT the tags of library unit JTextView.cs).
/// These are "&lt;X&gt;, where 'X' is: b (bold); i (italic); u (underline); sup (superscript); sub (subscript);
/// small (make font smaller); big (make font larger); tt Monospace font (i.e. fixed char. length). They all MUST
/// be matched by closing tags.</para>
/// <para>NB -- For some reason you can't use braces '{..}' - they raise an error. So they are replaced here with \u2774 and \u2775,
/// which are slightly smaller bold-print versions: ❴❵</para>
/// </summary>
  public static void Msg(string Message)
  { Msg("", Message); }
/// <summary>
/// <para>Simple message dialog; displays a box heading, the message + a 'close' button. The dialog box
/// swells to fit the text, though its width limit is 450 pixels, and there is no scroll bar if
/// the text is too much for the screen height (so that you cannot see the end of a long text).</para>
/// <para>Note that simple Pango markup tags apply (but NOT the tags of library unit JTextView.cs).
/// These are "&lt;X&gt;, where 'X' is: b (bold); i (italic); u (underline); sup (superscript); sub (subscript);
/// small (make font smaller); big (make font larger); tt Monospace font (i.e. fixed char. length). They all MUST
/// be matched by closing tags.</para>
/// <para>To make life easier, tags of JTextView.cs are converted to Pango versions for bold, italic, underline, super/subscript.</para>
/// <para>NB -- For some reason you can't use braces '{..}' - they raise an error. So they are replaced here with \u2774 and \u2775,
/// which are slightly smaller bold-print versions: ❴❵</para>
/// </summary>
  public static void Msg(string Heading, string Message)
  { Message = Message.Replace('{', '\u2774');   Message = Message.Replace('}', '\u2775');
    Message = ConformTagsToPangoTags(Message);
    MessageDialog md = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Other,  ButtonsType.Close, Message);
    md.Title = Heading;
    md.Run();
    md.Destroy();
  }

/// <summary>
/// <para>Message box will have just body text and a set of buttons (at least one), as many as have titles
/// supplied in ButtonTitles. Buttons are placed FROM LEFT TO RIGHT in the order supplied, and return
/// numbers from 1 upwards in the order supplied. (Corner icon closure returns 0.)</para>
/// <para>Note that simple Pango markup tags apply for BodyText (but NOT the tags of library unit JTextView.cs).
/// These are "&lt;X&gt;, where 'X' is: b (bold); i (italic); u (underline); sup (superscript); sub (subscript);
/// small (make font smaller); big (make font larger); tt Monospace font (i.e. fixed char. length). They all MUST
/// be matched by closing tags.</para>
/// </summary>
  public static int DecideBox(string Header, string BodyText, params string[] ButtonTitles)
  { return DecideBox(Header, BodyText, true, ButtonTitles);
  }
/// <summary>
/// <para>Message box will have just body text and a set of buttons (at least one), as many as have titles
/// supplied in ButtonTitles. Buttons are placed FROM LEFT TO RIGHT in the order supplied, and return
/// numbers from 1 upwards in the order supplied. (Corner icon closure returns 0.)</para>
/// <para>Pango markup text tags in BodyText are valid for this overload IF ParseMarkupTags is TRUE. Tags are:
/// "&lt;X&gt;, where 'X' is: b (bold); i (italic); u (underline); sup (superscript); sub (subscript);
/// small (make font smaller); big (make font larger); tt Monospace font (i.e. fixed char. length). They all MUST
/// be matched by closing tags.</para>
/// <para>To make life easier, tags of JTextView.cs are converted to Pango versions for bold, italic, underline, super/subscript.</para>
/// </summary>
  public static int DecideBox(string Header, string BodyText, bool ParseMarkupTags, params string[] ButtonTitles)
  { int noBtns = ButtonTitles.Length;  if (noBtns == 0) throw new Exception("Message boxes must have at least one button");
    int winLeft, winTop, winWidth, winHeight;
    object[] oo = new object[2*noBtns];
    for (int i=0; i < noBtns; i++) { oo[2*i] = ButtonTitles[i];  oo[2*i+1] = i+1; }
    Dialog dialog = new Dialog(Header, null, DialogFlags.Modal, oo);
    dialog.AllowGrow = true;
    if (BoxSize.X != -1) { winWidth = BoxSize.X;   winHeight = BoxSize.Y; }
    else                 { winWidth = 500;   winHeight = 300; }
    dialog.SetDefaultSize(winWidth, winHeight);
    if (BoxCentre.X == -1) dialog.WindowPosition = WindowPosition.Center;
    else
    { dialog.GetPosition(out winLeft,out winTop);
      dialog.Move(BoxCentre.X - winWidth/2, BoxCentre.Y - winHeight/2);
    }
    dialog.Title = Header;
    Label lulu = new Label(); // Warning! If you use the string-arg. form of the constructor,
                                      // the .UseMarkup field (+ others) will not be accessible!
    lulu.Visible = true;
    lulu.UseMarkup = ParseMarkupTags;
    if (ParseMarkupTags)
    { BodyText = ConformTagsToPangoTags(BodyText);
      BodyText = JTV.PrepareEscapedTextForPango(BodyText); // Substrings between '«' and '»' will be escaped using GLib.MarkUp.EscapeText(.).
      lulu.Markup = "\n" + BodyText + '\n';
    }
    else lulu.Text = "\n" + BodyText + '\n';
    lulu.Wrap = true;
    dialog.VBox.Add(lulu);
    dialog.Modal = true;
    int response = dialog.Run ();
    // Before destroying the box, record its final position and size (e.g. after moved/resized by user) for posterity.
    dialog.GetPosition(out winLeft,out winTop);
    dialog.GetSize(out winWidth, out winHeight);
    LastBoxSize.X = winWidth;  LastBoxSize.Y = winHeight;
    LastBoxCentre.X = winLeft + winWidth/2;  LastBoxCentre.Y = winTop + winHeight/2;
    dialog.Destroy();
    BoxSize = new Duo(-1, -1);  BoxCentre = new Duo(-1, -1);
    if (response < 1) response = 0; // Covers 'ResponseType.DeleteEvent' (evals. to -4) for corner icon closure.
    return response;
  }

/// <summary>
/// <para>public static int MultiBox(string Title, ref string Layout, ref string[] Texts, string Buttons)</para>
/// <para>'Layout':  A string of combinations of letters, one letter per widget, delimited by character '|'. Each delimited substring
///  represents a separate horizontal level of the dialog box; '|' moves vertically down to the next horizontal level.</para>
/// <para>Allowed letters are: 'L'(label with wrap), 'l'(label without wrap); 'T' or 't'(text box); 'V'(textview editable), 'v'
///  (textview uneditable), 'W' and 'w' (as for 'V' and 'v', but responding to tags);  'X'(check box checked at start),
///  'x' (check box unchecked at start); 'R' (radio button, checked at start),
///  'r' (radio button, unchecked at start).
///   For radio buttons, if all 'r' then the first is checked; if more than one 'R', only the last 'R' widget is checked.</para>
/// <para>'Texts': A string array, one string per widget of whatever sort. There must be exactly as many strings
///   as there are letters in Layout. Note that there is no reference to which horizontal layer the widget is in.</para>
/// <para>RETURN values: The input Layout is reproduced, incorporating any button changes ('X' ↔ 'x', 'R' ↔ 'r'). 'Texts' returns as its
///   input value, except that strings corresponding to text boxes reflect the final text in these boxes.</para>
/// <para>'Buttons': Button texts delimited by '|'; buttons numbered from left to right, leftmost being 1. Trimmed in the method.</para>
/// <para>SIZING: You cannot set box position without also setting box size. But having set box size, you can also set its position.
///   You do this as usual, using a prior call to JD.PlaceBox(.).</para>
/// <para>RETURNED: Button no. (1+, starting from leftmost, = first in 'Buttons'); 0 for corner icon closure.</para>
/// <para>If no errors, Texts returns with as many delimited substrings (same delimiter, '|') as Layout. For 'T' or 't', the final 
///    text of the text box; for 'L' or 'l', just '#' (invariable); for 'X' or 'x' or 'R' or 'r', '1' for active, '0' otherwise.</para>
/// <para>RETURNED for ERRORS: a negative value. Codes: -1000 = no printable chars. in 'Layout'; -1001 = unrecognized char. in 'Layout';
///   -1002 = no buttons; -1003 = array Texts has length not equal to no. of widgets.</para>
/// </summary>
    public static int MultiBox(string Title, ref string LayOut, ref string[] Texts, string Buttons, params Gtk.Window[] SendingWindow)
  {
    // Some preliminary settings:
    char[] DelimArray = { '|' }; // used as arg. to method Split.
    // Sort out the buttons first, as they are needed in the dialog box's constructor:
    String[] buttonery = Buttons.Split(DelimArray, StringSplitOptions.RemoveEmptyEntries);
    int noBtns = buttonery.Length;
    if (noBtns < 1)
      return -1002;
    object[] oo = new object[2 * noBtns]; // as required in 'Dialog' constructor
    for (int i = 0; i < noBtns; i++)
    {
      oo[2 * i] = buttonery[i].Trim();
      oo[2 * i + 1] = i + 1;
    }
    // Call the dialog box constructor, and optionally give it a set size:
    Dialog dialog = new Dialog(Title, null, DialogFlags.Modal, oo); // *** Acts modally whatever you put as dialog flag; not sure why it is there.
                        // (I even tried using the sending window instead of 'null', and setting DialofFlags to DestroyWithParent; no effect.)
    int winLeft, winTop, winWidth, winHeight;
    // You can alter size without affecting central placement; but you can alter placement unless size is also set.
    if (BoxSize.X != -1) // If not, leave Gtk to work out its sizing and centering.
    {
      winWidth = BoxSize.X;
      winHeight = BoxSize.Y;
      dialog.SetDefaultSize(winWidth, winHeight);
      if (BoxCentre.X == -1)
        dialog.WindowPosition = WindowPosition.Center;
      else
      {
        dialog.GetPosition(out winLeft, out winTop);
        dialog.Move(BoxCentre.X - winWidth / 2, BoxCentre.Y - winHeight / 2);
      }
    }
    // Add in the basic container, into which all of the required widgets will go:
    VBox V1 = new VBox(false, 5);
    V1.Name = "V1";   // V1.Spacing = 0;//#############6;
    dialog.VBox.Add(V1);
    // Prepare to set aside one HBox for each delimited segment within LayOut:
    LayOut = LayOut._PurgeRange(0, -1, '\u0000', '\u0020'); // Remove all chars. with unicodes 0 to 32.
    string LayOutCaps = LayOut.ToUpper();
    if (LayOut == "")
    {
      dialog.Destroy();
      return -1000;
    }
    int LayoutLen = LayOut.Length;
    string[] Layabout = LayOut.Split(DelimArray);
    int noHLevels = Layabout.Length;
    int noWidgets = LayoutLen - noHLevels + 1;
    if (Texts.Length != noWidgets)
    {
      dialog.Destroy();
      return -1003;
    }
    HBox[] HBoxes = new HBox[noHLevels];
    // These two lists run in parallel, so that we don't stuff up type casting when referencing the list of Widgets:
    List<Widget> Witchetty = new List<Widget>();
    List<char> Grub = new List<char>(); // Same letters (capitals) as used in LayOut.
    VButtonBox ButterBox = null; // for check buttons only
    VButtonBox RadioBox = null; // for radio buttons only
    RadioButton firstradio = null, nextradio = null;
    int wdgtCntr = 0, textCntr = 0; 
    for (int HLvl = 0; HLvl < noHLevels; HLvl++)
    { 
      HBoxes[HLvl] = new HBox(false, 4);//################# was just ()
      HBoxes[HLvl].Name = "HLevel" + HLvl.ToString();
      HBoxes[HLvl].Spacing = 5;
      V1.Add(HBoxes[HLvl]);
      // Stick in the components for this HBox:
      char lastCH = ' ';
      for (int wdgt = 0; wdgt < Layabout[HLvl].Length; wdgt++)
      {
        char ch = Layabout[HLvl][wdgt], CH = char.ToUpper(ch);
        if (ButterBox != null && CH != 'X')
        {
          HBoxes[HLvl].Add(ButterBox);
          ButterBox = null;
        }
        if (RadioBox != null && CH != 'R')
        {
          HBoxes[HLvl].Add(RadioBox);
          RadioBox = null;
          firstradio = null;
          nextradio = null;
        }
        if (CH == 'L') // Label
        {
          var lbl = new Label();
          lbl.Visible = true;
          string ss = ConformTagsToPangoTags(Texts[wdgtCntr]);
          ss = JTV.PrepareEscapedTextForPango(ss); // Substrings between '«' and '»' will be escaped using GLib.MarkUp.EscapeText(.).
          lbl.Markup = ss;
          lbl.Wrap = (ch == 'L');
          HBoxes[HLvl].Add(lbl);
          Witchetty.Add(lbl);
          Grub.Add(CH);
          textCntr++;
        }
        else if (CH == 'T') // Text box
        {
          var entry = new Entry("Widget " + HLvl + "/" + wdgt);
          entry.IsEditable = true;
          entry.Visibility = true;
          entry.Text = Texts[wdgtCntr];
          entry.Activated += JD.OnEntryBoxActionActivated;
          HBoxes[HLvl].Add(entry);
          Witchetty.Add(entry);
          Grub.Add(CH);
          textCntr++;
        }
        else if (CH == 'V' || CH == 'W') // TextView object
        { Gtk.ScrolledWindow scrump = new Gtk.ScrolledWindow();
          HBoxes[HLvl].Add(scrump);
          scrump.ShadowType = ShadowType.Out;
          var TV = new TextView();
          TextBuffer buff = TV.Buffer;
          TV.Editable = (ch == 'V' || ch == 'W');
          TV.WrapMode = Gtk.WrapMode.Word;
          TV.ModifyBase(StateType.Normal, DefBackColour);
          TV.AcceptsTab = false;
          TV.Visible = true;
          if (CH == 'W')
          { string sss = Texts[wdgtCntr];
            JTV.DisplayMarkUpText(TV, ref sss, "fill");
          }
          else buff.Text = Texts[wdgtCntr];
        //###        entry.Activated += JD.OnEntryBoxActionActivated;
          scrump.Add (TV);
          Witchetty.Add(TV);   Grub.Add(CH);
          textCntr++;
        }
        else if (CH == 'X') // Check box
        { // We don't use a VButtonBox because boxes are independent wrt whether or not they are ticked, unlike radio buttons.
          var cheque = new CheckButton(Texts[wdgtCntr]);
          cheque.Active = (ch == 'X');
          HBoxes[HLvl].Add (cheque);
          Witchetty.Add(cheque);   Grub.Add(CH);
        }
        else if (CH == 'R') // Radio box
        { if (lastCH != 'R')
          { firstradio = new Gtk.RadioButton(Texts[wdgtCntr]); // This time the container is needed, as box ticking is interactive across boxes.
            firstradio.Active = true; // may be negated by a later radio box in the group
            RadioBox = new VButtonBox(); // Otherwise we go on using RadioBox's present incarnation
            RadioBox.Add(firstradio);
            Witchetty.Add(firstradio); Grub.Add(CH);
          }
          else
          { nextradio = new RadioButton(firstradio, Texts[wdgtCntr]);
            nextradio.Active = (ch == 'R'); // the last one with ch of 'R' will win the check.
            RadioBox.Add(nextradio);
            Witchetty.Add(nextradio);   Grub.Add(ch);
          }
        }
        else return -1001;
        lastCH = CH;        
        wdgtCntr++;
      } // end of looping through this one horizontal level
      if (ButterBox != null) { HBoxes[HLvl].Add(ButterBox);  ButterBox = null; }
      if (RadioBox != null)  { HBoxes[HLvl].Add(RadioBox);   RadioBox = null; }
    } // end of looping through all horizontal levels
    // ----- SHOW THE BOX --------------
    // *** No need for  "dialog.Modal = true;", as the constructor used has set the Modal flag already (see above).
    dialog.VBox.ShowAll(); // Omit this and you don't see nuthin but a blank VBox.
    int response = dialog.Run ();
    // ----- DEAL WITH THE RESPONSE ---------
    int wdgtNo = 0;
    string tt;
    bool isActive = false;
    for (int i=0; i < LayoutLen; i++)
    { char CH = LayOutCaps[i];
      if (CH == '|') continue; // wdgtNo will not be updated.
      if (CH == 'T') Texts[wdgtNo] = ((Gtk.Entry) Witchetty[wdgtNo]).Text;
      else if (CH == 'V' || CH == 'W') Texts[wdgtNo] = ((Gtk.TextView) Witchetty[wdgtNo]).Buffer.Text;
      else if (CH == 'X')
      { isActive = ((Gtk.CheckButton) Witchetty[wdgtNo]).Active;
        if (isActive) tt = "X";  else tt = "x";
        LayOut = LayOut._Scoop(i, 1, tt);
      }
      else if (CH == 'R')
      { isActive = ((Gtk.RadioButton) Witchetty[wdgtNo]).Active;
        if (isActive) tt = "R";  else tt = "r";
        LayOut = LayOut._Scoop(i, 1, tt);
      }
      wdgtNo++;
    }
    // Before destroying the box, record its final position and size (e.g. after moved/resized by user) for posterity.
    dialog.GetPosition(out winLeft,out winTop);
    dialog.GetSize(out winWidth, out winHeight);
    LastBoxSize.X = winWidth;  LastBoxSize.Y = winHeight;
    LastBoxCentre.X = winLeft + winWidth/2;  LastBoxCentre.Y = winTop + winHeight/2;;
    dialog.Destroy();
    BoxSize = new Duo(-1, -1);  BoxCentre = new Duo(-1, -1);
    if (response < 1) response = 0; // Covers 'ResponseType.DeleteEvent' (evals. to -4) for corner icon closure.
    return response;
  }


/// <summary>
/// <para>Doubles as a FIND box (in which case ReplaceText must be NULL) and as a REPLACE box.</para>
/// <para>REF arg(s). return with user's text box entry.</para>
/// <para>'isSelection': if TRUE, then the text of the third check box will be different. No other difference occurs.</para>
/// <para>Return value, FIND box: 0 = closed by cnr icon; 1 = Find Next btn; 2 = Mark All btn; 3 = Cancel btn.</para>
/// <para>Return value, REPLACE box: 0 = closed by cnr icon; 1 = Check Each btn; 2 = All At Once btn;
///   3 = Cancel btn.</para>
/// <para>OUT arg CheckedStates, both box types: [0] = Match case; [1] = Match whole word; [2] =
///   only from cursor position down OR only within selection.</para>
/// <para>Effect of ENTER keyed while text box focussed: (a) for FIND dialog, sends focus to leftmost button ("FIND NEXT");
///   (b) for REPLACE dialog, top box --> next box --> middle button ("ALL AT ONCE")</para>
/// </summary>
  public static int SearchBox(ref string FindText, ref string ReplaceText, bool isSelection, out bool[] CheckedStates)
  { bool isFindOnly = (ReplaceText == null);
    int winLeft, winTop, winWidth, winHeight;
    CheckedStates = new bool[3];
    object[] oo;
    if (isFindOnly) oo = new object[]{"FIND NEXT", 1,  "MARK ALL", 2,    "CANCEL", 3};
    else            oo = new object[]{"CHECK EACH", 1, "ALL AT ONCE", 2, "CANCEL", 3};
   string title;
    if (isFindOnly) title = "FIND TEXT"; else title = "REPLACE TEXT";
    Dialog dialog = new Dialog(title, null, DialogFlags.Modal, oo);
    HoldData.ThisDialog = dialog;
    HoldData.NoButtons = 3;
    if (BoxSize.X != -1) { winWidth = BoxSize.X;   winHeight = BoxSize.Y; }
    else                 { winWidth = 500;   winHeight = 300; };
    dialog.SetDefaultSize(winWidth, winHeight);
    if (BoxCentre.X == -1) dialog.WindowPosition = WindowPosition.Center;
    else
    { dialog.GetPosition(out winLeft,out winTop);
      dialog.Move(BoxCentre.X - winWidth/2, BoxCentre.Y - winHeight/2);
    }
  // Top row - the 'Search For' label and text box:
    VBox V1 = new VBox();   V1.Name = "V1";    V1.Spacing = 6;
    dialog.VBox.Add(V1);
    HBox H1 = new HBox();   H1.Name = "H1";    H1.Spacing = 6;
    V1.Add(H1);
    Gtk.Label lblFor = new Gtk.Label();
    lblFor.Visible = true;
    lblFor.Text = "SEARCH FOR:  ";
    lblFor.Wrap = false;
    H1.Add(lblFor);
    Entry entryFor = new Entry (FindText);
    entryFor.IsEditable = true;
    entryFor.Visibility = true;
    entryFor.Name = "entry0"; // Needed in the event handler...
    entryFor.Activated += JD.OnEntryBoxActionActivated;
    H1.Add (entryFor);
  // Middle row - the 'Replace With' label and text box:
    Entry entryWith = null; // Even if this is just a 'find' box, you still have to assign it, or compiler complains.
    if (!isFindOnly)
    { HBox H2 = new HBox();   H2.Name = "H2";    H2.Spacing = 6;
      V1.Add(H2);
      Gtk.Label lblWith = new Gtk.Label();
      lblWith.Visible = true;
      lblWith.Text = "REPLACE WITH:";
      lblWith.Wrap = false;
      H2.Add(lblWith);
      entryWith = new Entry (ReplaceText);
      entryWith.IsEditable = true;
      entryWith.Visibility = true;
      entryWith.Name = "entry1"; // Needed in the event handler...
      entryWith.Activated += JD.OnEntryBoxActionActivated;
      H2.Add (entryWith);
    }
    HoldData.EntryBoxes = new Entry[] { entryFor, entryWith };
    if (isFindOnly) HoldData.DoAtExit = new string[] {"1"};
    else  HoldData.DoAtExit = new string[] {"101", "2"};
  // Bottom row - blank label on left, radio buttons on right:
    HBox H3 = new HBox();   H3.Name = "H3";    H3.Spacing = 6;
    V1.Add(H3);
    Gtk.Label lblBtm = new Gtk.Label();
    lblBtm.Visible = true;
    lblBtm.Wrap = true;
    lblBtm.Text = "(\'\\n\' and \'\\t\' allowed)\n\n\n\n\n\n";
    H3.Add(lblBtm);
    VButtonBox Vchqs = new VButtonBox();
    Gtk.CheckButton Cheque1 = new Gtk.CheckButton("Match Case");
    Vchqs.Add(Cheque1);
    Gtk.CheckButton Cheque2 = new Gtk.CheckButton("Whole Word");
    Vchqs.Add(Cheque2);
    Gtk.CheckButton Cheque3 = null;
    if (isSelection) Cheque3 = new Gtk.CheckButton("Only within Selected Text");
    else             Cheque3 = new Gtk.CheckButton("Only from Here Down");
    Vchqs.Add(Cheque3);
    H3.Add(Vchqs);
    dialog.VBox.ShowAll(); // Omit this and you don't see nuthin but a blank VBox.
    dialog.Modal = true;
    int response = dialog.Run ();
    FindText = entryFor.Text;
    // Allow for entry of '\n' to mean a par. mark, '\t' to mean a tab, and '\\' to stand for literal '\':
    FindText = FindText.Replace(@"\\", "$%^%$^%");
    FindText = FindText.Replace(@"\n", "\n");
    FindText = FindText.Replace(@"\t", "\t");
    FindText = FindText.Replace("$%^%$^%", @"\");
    if (!isFindOnly)
    { ReplaceText = entryWith.Text;
    // Allow for entry of '\n' to mean a par. mark, '\t' to mean a tab, and '\\' to stand for literal '\':
      ReplaceText = ReplaceText.Replace(@"\\", "$%^%$^%");
      ReplaceText = ReplaceText.Replace(@"\n", "\n");
      ReplaceText = ReplaceText.Replace(@"\t", "\t");
      ReplaceText = ReplaceText.Replace("$%^%$^%", @"\");
    }
    CheckedStates[0] = Cheque1.Active;
    CheckedStates[1] = Cheque2.Active;
    CheckedStates[2] = Cheque3.Active;
    // Before destroying the box, record its final position and size (e.g. after moved/resized by user) for posterity.
    dialog.GetPosition(out winLeft,out winTop);
    dialog.GetSize(out winWidth, out winHeight);
    LastBoxSize.X = winWidth;  LastBoxSize.Y = winHeight;
    LastBoxCentre.X = winLeft + winWidth/2;  LastBoxCentre.Y = winTop + winHeight/2;
    // Get rid of the pointers to dialog objects before destroying the dialog:
    JD.HoldData.ThisDialog = null;
    JD.HoldData.EntryBoxes = null;
    JD.HoldData.NoButtons = 0;
    JD.HoldData.DoAtExit = null;
    dialog.Destroy();
    BoxSize = new Duo(-1, -1);  BoxCentre = new Duo(-1, -1);
    if (response < 1) response = 0; // Covers 'ResponseType.DeleteEvent' (evals. to -4) for corner icon closure.
    return response;
  }
  
  
/// <summary>
/// <para>The message box will have just body text and a set of buttons (at least one), as many as have titles
/// supplied in ButtonTitles. Buttons are placed FROM LEFT TO RIGHT in the order supplied, and return
/// numbers from 1 upwards in the order supplied. (Corner icon closure returns 0.) All button clicks close the message box.</para>
/// <para>Normally focus starts in the text area (with a cursor visible there). If you want a button focussed, prefix its name with '!'.</para>
/// <para>If you want keying 'Enter' to close the box, focus a button as just explained; then pressing 'Enter' will activate that button.</para>
/// <para>All the tags in file jTextView.cs apply (instead of Pango markup text tags). To avail of them, set
/// ParseMarkupTabs to TRUE.</para>
/// <para>Remember that to change position and size from the default you have to set JD.BoxCentre and JD.BoxSize before calling this.</para>
/// </summary>
  public static int Display(string Header, string BodyText, bool ParseMarkupTabs, bool AllowWrap, bool AllowEdit, params string[] ButtonTitles)
  { int winLeft, winTop, winWidth, winHeight;
    int noBtns = ButtonTitles.Length;  if (noBtns == 0) throw new Exception("Message boxes must have at least one button");
    object[] ooh = new object[2*noBtns];
    int focusButton = -1;
    for (int i=0; i < noBtns; i++)
    { string ss = ButtonTitles[i];
      if (ss.Length > 0 && ss[0] == '!') // then this button is to be focussed:
      { ss = ss._Extent(1);   focusButton = noBtns - i -1; } // buttons are stored in 'dialog' in reverse order.
      ooh[2*i] = ss;  ooh[2*i+1] = i+1;
    }
    Dialog dialog = new Dialog(Header, null, DialogFlags.Modal, ooh);
    if (BoxSize.X != -1) { winWidth = BoxSize.X;   winHeight = BoxSize.Y; }
    else                 { winWidth = 500;   winHeight = 300; };
    dialog.SetDefaultSize(winWidth, winHeight);
    if (BoxCentre.X == -1) dialog.WindowPosition = WindowPosition.Center;
    else
    { dialog.GetPosition(out winLeft,out winTop);
      dialog.Move(BoxCentre.X - winWidth/2, BoxCentre.Y - winHeight/2);
    }
   // Top part - the TextView:
    VBox V1 = new VBox();   V1.Name = "V1";    V1.Spacing = 6;
    dialog.VBox.Add(V1);

    Gtk.ScrolledWindow scrump = new Gtk.ScrolledWindow();
    V1.Add(scrump);

    TextView TV = new TextView();
    TextBuffer Buff = TV.Buffer;
    TV.Editable = AllowEdit;
    if (ParseMarkupTabs) JTV.DisplayMarkUpText(TV, ref BodyText, "fill");
    else Buff.Text = BodyText;
    TV.ModifyBase(StateType.Normal, DefBackColour);
    if (AllowWrap) TV.WrapMode = Gtk.WrapMode.Word;  else TV.WrapMode = Gtk.WrapMode.None;
    TV.Visible = true;
    scrump.Add(TV);
    // If a button is to receive focus, go for it:
    if (focusButton >= 0) ( (Gtk.Button) dialog.ActionArea.Children[focusButton]).GrabFocus();
    SelectedText = ""; // public class variable, which will capture whatever is selected at the time of dialog closure.
    dialog.VBox.ShowAll(); // Omit this and you don't see nuthin but a blank VBox.
    dialog.Modal = true;
    int response = dialog.Run ();
    // Before destroying the box, record its final position and size (e.g. after moved/resized by user) for posterity.
    SelectedText = JTV.ReadSelectedText(Buff, out SelectionPoint);
    SelectionPoint = Buff.CursorPosition;
    SelectionLine = (SelectedText == "") ? -1  : (JTV.CursorLine(Buff, 0)).I;
    dialog.GetPosition(out winLeft,out winTop);
    dialog.GetSize(out winWidth, out winHeight);
    LastBoxSize.X = winWidth;  LastBoxSize.Y = winHeight;
    LastBoxCentre.X = winLeft + winWidth/2;  LastBoxCentre.Y = winTop + winHeight/2;;
    dialog.Destroy();
    BoxSize = new Duo(-1, -1);  BoxCentre = new Duo(-1, -1);
    if (response < 1) response = 0; // Covers 'ResponseType.DeleteEvent' (evals. to -4) for corner icon closure.
    return response;
  }

// *** MAYBE SOME DAY: Somehow try to make left edges of entry boxes line up, despite label text variations.
// *** Also, get right edges further away from the container's right margin.
/// <summary>
/// <para>Displays any number of input text lines (but at least one), each line consisting of a label on the left and an input
///  text box (one-liner, not wrapped) on the right. In addition there is a top label for explanatory notes, above the
///  first line.The REF argument returns with the user's text box entries.</para>
/// <para>For each line i (base 0), Prompts[i] is the prompt to the left of the text entry box and BoxTexts[i] is the text to
///  display in the entry box at startup. BoxTexts may be NULL or of any length; only nonnull nonempty elements within range
///  will be used.</para>
/// <para>Note that simple Pango markup tags apply for Preamble and Prompts (but NOT the tags of library unit JTextView.cs).
///  These include "&lt;X&gt;, where 'X' is: b (bold); i (italic); u (underline); sup (superscript); sub (subscript);
///  small (make font smaller); big (make font larger); tt Monospace font (i.e. fixed char. length). They all MUST
///  be matched by closing tags.</para>
/// <para>ButtonTitlesEtc: (a) Holds button titles, one per string; and (b) may hold one more string which instructs the method
///  what to do when ENTER is keyed in a focussed text box. There must always be at least one button title; but (b) is optional.
///  Re (a): they are placed FROM LEFT TO RIGHT in the order supplied, and return numbers from 1 upwards in the order supplied
///  (Corner icon closure returns 0.)
///  Re (b): the string must start with exactly "#:" (or it will be regarded as another button text). What follows is one or more
///  integers delimited by commas. There should be one value for every text box. Values should be: 0 for 'do nothing' (which
///  is what normally happens by default in all text boxes). -1 = close the dialog and return 100 + text box index (to base 0).
///  Values 1, 2, ... send the focus to buttons 1, 2, ... numbered as mentioned above. (When that button is focussed, a further
///  'Enter' will activate that button; keep in mind that users can easily accidentally evoke an extra unintended 'Enter').
///  Values 100 + text box index (to base 0) send focus to that text box. ERRORS in this string don't crash, but revert
///  to the default: do nothing when ENTER keyed.</para>
/// <para>Sadly, many errors crash the program, so calling code must do the bullet-proofing.</para>
/// </summary>
  public static int InputBox(string Header, string Preamble, string[] Prompts, ref string[] BoxTexts, params string[] ButtonTitlesEtc)
  {
   int winLeft, winTop, winWidth, winHeight;
    // Set up the buttons:
    string ss = "", jumpTo = "";
    int noButtons = ButtonTitlesEtc.Length;
    if (noButtons > 0)
    { ss =  ButtonTitlesEtc[noButtons-1];
      if (ss.Length > 2  &&  ss._Extent (0,2) == "#:")
      { jumpTo = ss._Extent (2);  ButtonTitlesEtc[noButtons-1] = "";  noButtons--; }
    }
    bool useHoldData = (jumpTo != "");
    if (noButtons < 1) throw new Exception("JD.InputBox(.) - there must be at least one button title");
    object[] oo = new object[2 * noButtons];
    for (int i=0; i < noButtons; i++) { oo[2*i] = ButtonTitlesEtc[i];  oo[2*i+1] = i+1; }
    // Define the Dialog and size and place it:
    Dialog dialog = new Dialog(Header, null, DialogFlags.Modal, oo);
    // Set up first of HoldData's static fields - for use by the Action delegate for entry boxes.
    if (useHoldData)
    { JD.HoldData.ThisDialog = dialog;   JD.HoldData.NoButtons = noButtons;
      JD.HoldData.DoAtExit = jumpTo.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
    }
    if (BoxSize.X != -1) { winWidth = BoxSize.X;   winHeight = BoxSize.Y; }
    else                 { winWidth = 500;   winHeight = 300; };
    dialog.SetDefaultSize(winWidth, winHeight);
    if (BoxCentre.X == -1) dialog.WindowPosition = WindowPosition.Center;
    else
    { dialog.GetPosition(out winLeft,out winTop);
      dialog.Move(BoxCentre.X - winWidth/2, BoxCentre.Y - winHeight/2);
    }
   // Set up the preamble container and label:
    VBox Vtop = new VBox();  Vtop.Name = "Vtop";  Vtop.Spacing = 6;
    dialog.VBox.Add(Vtop);
    Gtk.Label lblPreamble = new Gtk.Label();   lblPreamble.Visible = true;   lblPreamble.Wrap = true;
    Preamble = ConformTagsToPangoTags(Preamble);
    Preamble = JTV.PrepareEscapedTextForPango(Preamble); // Substrings between '«' and '»' will be escaped using GLib.MarkUp.EscapeText(.).
    lblPreamble.Markup = Preamble; // Sets the label text, after Pango parses it for markup tags.
    lblPreamble.WidthRequest = winWidth - 50;
    Vtop.Add(lblPreamble);
   // Set up the label-plus-entry-box line containers and widgets:
    int noLines = Prompts.Length;
    if (noLines == 0) { throw new Exception("JD.InputBox(.) - String array Prompts has no entries"); }
    int noBoxTexts = BoxTexts._Length(); // null --> -1.
    Entry[] entries = new Entry[noLines];
    string tt="";
    VBox vox = null;  HBox hox = null;  Gtk.Label lab = null;  Entry ant = null;
    for (int line = 0; line < noLines; line++)
    { ss = line.ToString();
      vox = new VBox();  vox.Name = "vox" + ss;  vox.Spacing = 6;
      Vtop.Add(vox); // NB - You have to add successive VBoxes to the first VBox, NOT to dialog itself.
      hox = new HBox();  hox.Name = "hox" + ss;  hox.Spacing = 6;
      vox.Add(hox);
      // The prompt label, on the left:
      lab = new Gtk.Label();   lab.Visible = true;   lab.Wrap = false;
      ss = ConformTagsToPangoTags(Prompts[line]);
      ss = JTV.PrepareEscapedTextForPango(ss); // Substrings between '«' and '»' will be escaped using GLib.MarkUp.EscapeText(.).
      lab.Markup = ss; // Sets the label text, after Pango parses it for markup tags.
      hox.Add(lab);
      // The text box, on the right:
      tt = "";  if (line < noBoxTexts) tt = BoxTexts[line]; // entry box text at startup
      ant = new Entry(tt);  ant.IsEditable = true;
      if (useHoldData) // then set up a delegate to handle ENTER in text boxes:
      {
        ant.Activated += JD.OnEntryBoxActionActivated;
        ant.Name = "entry" + line.ToString();
      }
      entries[line] = ant;
      hox.Add(ant);
    }
    if (useHoldData) HoldData.EntryBoxes = entries;

  // SHOW THE DIALOG AND WAIT FOR A RESPONSE:
    dialog.VBox.ShowAll(); // Omit this and you don't see nuthin but a blank VBox.
    dialog.Modal = true;
    int response = dialog.Run();
  // RESPONSE HAS BEEN MADE:
    // collect the entry box contents:
    BoxTexts = new string[noLines];
    for (int i=0; i < noLines; i++) BoxTexts[i] = entries[i].Text;
    // Before destroying the box, record its final position and size (e.g. after moved/resized by user) for posterity.
    dialog.GetPosition(out winLeft,out winTop);
    dialog.GetSize(out winWidth, out winHeight);
    LastBoxSize.X = winWidth;  LastBoxSize.Y = winHeight;
    LastBoxCentre.X = winLeft + winWidth/2;  LastBoxCentre.Y = winTop + winHeight/2;

    // Get rid of the external pointers to this dialog and its components:
    if (useHoldData)
    { JD.HoldData.ThisDialog = null;
      JD.HoldData.EntryBoxes = null;
      JD.HoldData.NoButtons = 0;
      JD.HoldData.DoAtExit = null;
    }
    dialog.Destroy();
    BoxSize = new Duo(-1, -1);  BoxCentre = new Duo(-1, -1);
    if (response < 1) response = 0; // Covers 'ResponseType.DeleteEvent' (evals. to -4) for corner icon closure.
    return response;
  }

/// <summary>
/// There are two action buttons, 'SET' and 'FINISH'. To set a start colour you enter the description into the text box
///  and click 'SET'. Calling code MUST have this method in a loop, only exited if returned value is not 1 ('SET' button).
///  Then in the next looping, the text box colour will be applied to the colour button on the form. 'FINISH' returns
///  the final colour selection as a hex string (6 digits), and as three integers, and as a colour name (if a search reveals
///  that this colour has a name), and finally provides text which is formatted to display in the returned colour, using markup tags.
///  Acceptable inputs (case-insensitive, space-insensitive) for the SET textbox: "7FFFD4", "0x7fffd4", "aquamarine", "127, 255, 212".
///  For this example, the output would be: "Chosen colour:  7FFFD4    (127, 255, 212)    'aquamarine'.     Text example: ABCDEFG",
///  with 'ABCDEFG' enclosed in colour markup tags.
/// 'Heading': If this enters as an empty string, it is converted to 'COLOUR SELECTION'.
/// 'DescriptionFormat': Determines what is returned as ClrDescription: "hex" for "FF0080"; "bytes" for "255, 0, 128"; "verbose" for a whole sentence.
/// </summary>
  public static int ColourChoiceBox(string Heading, ref string ClrDescription,  string DescriptionFormat)
  {
    string[] ButtonTitles = {"SET","FINISH", "CANCEL" };
    int winLeft, winTop, winWidth, winHeight;
    // Set up the buttons:
    int noButtons = ButtonTitles.Length;
    object[] oo = new object[2 * noButtons];
    for (int i=0; i < noButtons; i++) { oo[2*i] = ButtonTitles[i];  oo[2*i+1] = i+1; }
    // Define the Dialog and size and place it:
    if (Heading == "") Heading = "COLOUR  SELECTION";
    Dialog dialog = new Dialog(Heading, null, DialogFlags.Modal, oo);
    if (BoxSize.X != -1) { winWidth = BoxSize.X;   winHeight = BoxSize.Y; }
    else                 { winWidth = 500;   winHeight = 300; }
    dialog.SetDefaultSize(winWidth, winHeight);
    if (BoxCentre.X == -1) dialog.WindowPosition = WindowPosition.Center;
    else
    { dialog.GetPosition(out winLeft,out winTop);
      dialog.Move(BoxCentre.X - winWidth/2, BoxCentre.Y - winHeight/2);
    }
   // Set up the preamble container and label:
    VBox Vtop = new VBox();  Vtop.Name = "Vtop";  Vtop.Spacing = 6;
    dialog.VBox.Add(Vtop);
    Label lblPreamble = new Label();   lblPreamble.Visible = true;   lblPreamble.Wrap = true;
    string Preamble = "To <b>choose a colour</b>, click on the large coloured button below.\n\nTo set a <b>starting colour</b>, enter the colour " +
        "description into the box and then click the 'SET' button.\n\n<b>Allowed colour descriptions</b>: hexadecimal RGB ('ff0088'); " +
        "three integers R,G,B ('255, 0, 136'); or a recognized name ('deep sky blue').";
    lblPreamble.Markup = Preamble; // Sets the label text, after Pango parses it for markup tags.
    Vtop.Add(lblPreamble);
   // Set up the label-plus-entry-box line container and widget:
    //#####VBox vox = null;  HBox hox = null;  Gtk.Label lab = null;
    string Prompt = "Input colour description:";
    VBox vox = new VBox();  vox.Name = "vox";  vox.Spacing = 6;
    Vtop.Add(vox); // NB - You have to add successive VBoxes to the first VBox, NOT to dialog itself.
    HBox hox = new HBox();  hox.Name = "hox";  hox.Spacing = 6;
    vox.Add(hox);
    // The prompt label, on the left:
    var lab = new Label();   lab.Visible = true;   lab.Wrap = false;
    string ss = ConformTagsToPangoTags(Prompt);
    lab.Markup = ss; // Sets the label text, after Pango parses it for markup tags.
    hox.Add(lab);
    // The text box, on the right:
    Entry ant = new Entry(ClrDescription);  ant.IsEditable = true;
    hox.Add(ant);
    var Clutton = new ColorButton();
    string clrStr = ClrDescription._Purge();
    if (clrStr != "")
    { bool success;
      var thisClr = clrStr._ParseColour(JTV.Black, false, out success);
      if (success) Clutton.Color = thisClr;
      else ant.Text = "Cannot translate: " + ClrDescription;
    }
    Vtop.Add(Clutton);
//    public static Gdk.Color _ParseColour( this string inStr, Gdk.Color failureColour, bool force_0x_for_Hex, out bool success)

  // SHOW THE DIALOG AND WAIT FOR A RESPONSE:
    dialog.VBox.ShowAll(); // Omit this and you don't see nuthin but a blank VBox.
    dialog.Modal = true;
    int response = dialog.Run();

  // RESPONSE HAS BEEN MADE:
    // collect the entry box contents:
    // Before destroying the box, record its final position and size (e.g. after moved/resized by user) for posterity.
    dialog.GetPosition(out winLeft,out winTop);
    dialog.GetSize(out winWidth, out winHeight);
    LastBoxSize.X = winWidth;  LastBoxSize.Y = winHeight;
    LastBoxCentre.X = winLeft + winWidth/2;  LastBoxCentre.Y = winTop + winHeight/2;
    if (response == 1) ClrDescription = ant.Text; // The user's colour setting, ready for re-entrance.
    else if (response == 2)
    { Gdk.Color clr = Clutton.Color;
      string hex="", bytes="", clrName = "";
      int R = clr.Red / 256,  G = clr.Green / 256,  B = clr.Blue / 256;
      if (DescriptionFormat != "hex")
      { bytes = R + ", " + G + ", " + B;
        if (DescriptionFormat == "verbose") clrName = JS.RetrieveColourName( Convert.ToByte(R), Convert.ToByte(G), Convert.ToByte(B));
      }
      if (DescriptionFormat != "bytes")
      { hex = R.ToString("X2") + G.ToString("X2") + B.ToString("X2"); }
      if (DescriptionFormat == "hex") ClrDescription = hex;
      else if (DescriptionFormat == "bytes") ClrDescription = bytes;
      else if (DescriptionFormat == "verbose")
      { ClrDescription = "Chosen colour:  " + hex + "    (" + bytes + ")";
        if (clrName != "") ClrDescription += "    '" + clrName + "'";
        ClrDescription += ".     Text example: <# 0x" + hex + ">ABCDEFG<# black>";
      }
      else ClrDescription = "unrecognized argument";
    }
    else response = 0; // Covers 'ResponseType.DeleteEvent' (evals. to -4) for corner icon closure.
    dialog.Destroy();
    BoxSize = new Duo(-1, -1);  BoxCentre = new Duo(-1, -1);
    return response;
  }

// ===== DELEGATE ============

  protected class HoldData : JD
  {
    private HoldData(){} // THE CLASS CANNOT BE INSTANTIATED.
    // *** IF ADDING FIELDS, be sure that each new one is nulled, along with the old ones, before the target dialog box is destroyed.
    public static Dialog ThisDialog;
    public static Entry[] EntryBoxes = null;
    public static string[] DoAtExit = null;
    public static int NoButtons = 0;
  }

  // Called only from JD.InputBox(.), and only when ENTER is keyed while focus is in one of its text boxes.
  protected static void OnEntryBoxActionActivated (object sender, EventArgs e)
  {
    if (HoldData.ThisDialog != null)
    {
      int noboxes = HoldData.EntryBoxes.Length;
      string ss = (sender as Entry).Name;
      int thisboxno = ss._Extent(5)._ParseInt(-1);
      if (thisboxno < 0  ||  thisboxno >= noboxes  || HoldData.DoAtExit.Length <= thisboxno) return;
      // This box no. is valid, and there is an entry in DoAtExit corresponding to it:
      int nextthingo = HoldData.DoAtExit[thisboxno]._ParseInt(999);
      // 0 = do nothing (defaults to dialog box's system, which is to do nothing).
      if (nextthingo == 0) return;
      // -1 or any negative value = close the box, with response of 100 + textbox number (e.g. '100' for the top box).
      if (nextthingo < 0) { HoldData.ThisDialog.Respond(100 + thisboxno);  return; }
      // 1, 2, 3, ... = if there is a button with this number, focus it; otherwise do nothing.
      if (nextthingo <= HoldData.NoButtons) // the user's no. for button 0 is '1'.
      { int nextbuttonno = HoldData.NoButtons - nextthingo;
        HoldData.ThisDialog.ActionArea.Children[nextbuttonno].GrabFocus();   return;
      }
      // 100, 101, 102, ... = if there is a text box numbered (n - 100), go to it; otherwise do nothing.
      int nextboxno = nextthingo - 100;
      if (nextboxno >= 0 && nextboxno < noboxes)
      { HoldData.EntryBoxes[nextboxno].GrabFocus();  return;  }
    }

  }

} // END OF CLASS JD


} // END OF NAMESPACE JLib















