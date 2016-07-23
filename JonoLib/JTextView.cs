using System;
using System.Text;
//using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using Gdk;
using Gtk;
using Pango;
using GLib;

namespace JLib
{ // A library of utilities for widescale use in programs written by me. Spread over several files

public class JTV // TEXTVIEW STATIC METHODS (and related)
{
  private JTV(){} // THE CLASS CANNOT BE INSTANTIATED.

  public static Gdk.Color Black = new Gdk.Color(0,0,0);
  public static Gdk.Color Brown = new Gdk.Color(0xA5, 0x2A, 0x2A);
  public static Gdk.Color Red = new Gdk.Color(0xFF, 0, 0);
  public static Gdk.Color Orange = new Gdk.Color(0xFF, 0xA5, 0);
  public static Gdk.Color Yellow = new Gdk.Color(0xFF, 0xFF, 0);
  public static Gdk.Color Green = new Gdk.Color(0, 0x80, 0);
  public static Gdk.Color Blue = new Gdk.Color(0, 0, 0xFF);
  public static Gdk.Color Magenta = new Gdk.Color(0xFF, 0, 0xFF);
  public static Gdk.Color Grey = new Gdk.Color(0x80, 0x80, 0x80),    Gray = Grey;
  public static Gdk.Color White = new Gdk.Color(0xFF, 0xFF, 0xFF);

  private static List<IntPtr> EducatedBuffs = new List<IntPtr>(); // Will hold the names of good Buffs (a 'good' TextBuffer
          // being one that has been filled up, if nec., with markup TextTags by method IncorporateMarkupTagsIntoTextTable(.)).
// MarkUp Tag Replacement String Codes. Note that the spaces in the codes (a) act as delimiters, (b) ensure exact match between
//  symbol and its encodement where either includes multichar. units, and (c) make the coding humanly readible.
//  String pairs MUST have the same length.
  public static string GreekSymbols = "α β γ δ ε ζ η θ ι κ λ μ ν ξ ο π ρ σ ς τ υ φ χ ψ ω  Α Β Γ Δ Ε Ζ Η Θ Ι Κ Λ Μ Ν Ξ Ο Π Ρ Σ Τ Υ Φ Χ Ψ Ω";
  public static string GreekCodings = "a b g d e z @ 0 i k l m n x o p r s c t u f h y w  A B G D E Z # 9 I K L M N X O P R S T U F H Y W";
  public static string MathSymbols =  "∫ ∮ ∑ √ ± ° × ‹ › ≪ ≫ ‒ ‖ ℛ ℱ ∂ ∇ ∞ ∡ ≈ ≠ ≡ ≤ ≥";
  public static string MathCodings =  "i j S / + o x ( ) { } - | R F d D 8 A ~ # = < >";
  // These are used in handling the use of "\<" and "\>" to imply literal '<' and '>'. They are assigned in IncorporateMarkupTagsIntoTextTable(.)
  public static char LessThanChr, GreaterThanChr, DoubleQuoteChr;
  public static string LessThanStr, GreaterThanStr, DoubleQuoteStr;
  public static int UniqueNo = 1; // incremented with every use; use to give diff. names for variables. Actual value has no significance.
 // Default paragraph settings
  public static int DefLeftMargin = 0, DefRightMargin = 0, DefIndent = 0, DefPixelsAbove = 0, DefPixelsBelow = 0,
              DefBulletGap = 2; // no. nonbreaking spaces in a bulletted par. from the bullet to the first char. of the par.
  public static string DefBullet = "\u26ab"; // a filled circle. *** Changed from the earlier larger dot: "\u25cf".

// STATIC CONSTRUCTOR for whatever of the above needs some code for its initialization:
  static JTV()
  { LessThanChr = JS.LastPrivateChar;
    GreaterThanChr = LessThanChr;   GreaterThanChr++;  DoubleQuoteChr = GreaterThanChr;  DoubleQuoteChr++;
    LessThanStr = LessThanChr.ToString();    GreaterThanStr = GreaterThanChr.ToString();
    DoubleQuoteStr = DoubleQuoteChr.ToString();
  }

// ===================================================
// SCREEN                                      //screen
// ---------------------------------------------------
/// <summary>
/// Return the screen size in pixels: .X is width, .Y is height.
/// </summary>
  public static Duo ScreenSizeInPixels()
  { Screen scream = Screen.Default;
    return new Duo(scream.Width, scream.Height);
  }


// ===================================================
// CURSOR AND SELECTION                      //cursor  //selection
// ---------------------------------------------------
/// <summary>
/// Gives the line of text in the buffer which currently holds the cursor. 'FirstLineNo' would usually be either
/// 0 (for the line no. as computed internally) or 1 (intuitively more acceptable).
/// Returns .I = no. of current paragraph (to base 0), .S = that number as a string.
/// </summary>
  public static Strint CursorLine (TextBuffer Buff, int FirstLineNo)
  { int charNo = Buff.CursorPosition;
    TextIter itty, dummy;
    Buff.GetBounds(out itty, out dummy);
    itty.Offset = charNo;
    int lineNo = itty.Line + FirstLineNo;
    return new Strint(lineNo, lineNo.ToString());
  }


/// <summary>
/// Place cursor at given character position.
/// </summary>
  public static void PlaceCursorAt(TextBuffer Buff, int Posn)
  { TextIter cursIt = Buff.GetIterAtOffset(Posn);
    Buff.PlaceCursor(cursIt);
  }

/// <summary>
/// Place cursor in the line numbered LineNo (0 being the first line), at char. offset OffsetInLine within that line (0 being the start).
/// OffsetInLine may be negative.
/// </summary>
  public static void PlaceCursorAtLine(TextBuffer Buff, int LineNo, int OffsetInLine)
  { TextIter It = Buff.GetIterAtLine(LineNo);
    if (OffsetInLine != 0) It.ForwardChars(OffsetInLine);
    Buff.PlaceCursor(It);
  }
/// <summary>
/// Place cursor in the line numbered LineNo (0 being the first line), at char. offset OffsetInLine within that line (0 being the start).
/// OffsetInLine may be negative. Optionally scroll to it, hopefully with the line at midscreen level.
/// </summary>
  public static void PlaceCursorAtLine(TextView TV, int LineNo, int OffsetInLine, bool andScrollToIt)
  { TextBuffer Buff = TV.Buffer;
    TextIter thisIt = Buff.GetIterAtLine(LineNo);
    if (OffsetInLine != 0) thisIt.ForwardChars(OffsetInLine);
    if (andScrollToIt) TV.ScrollToIter(thisIt, 0.1, true, 0.0, 0.5); // '0.5' to get the line halfway up the screen, if possible.
                                 // This instruction must precede the next line, or else the cursor is stranded at pre-scroll posn.
    Buff.PlaceCursor(thisIt);
  }

/// <summary>
/// Place cursor at the end of text.
/// </summary>
  public static void PlaceCursorAtEnd(TextBuffer Buff)
  { Buff.PlaceCursor(Buff.EndIter);
  }

/// <summary>
/// Place cursor at the start of text.
/// </summary>
  public static void PlaceCursorAtStart(TextBuffer Buff)
  { Buff.PlaceCursor(Buff.StartIter);
  }

  public static void SelectParContainingCursor(TextBuffer Buff)
  { TextIter parStartIt, parEndIt;
    int cursorOffset = Buff.CursorPosition;
    parStartIt = Buff.StartIter;
    parStartIt.ForwardChars(cursorOffset);
    int lineInset = parStartIt.LineOffset;
    if (lineInset > 0) parStartIt.BackwardChars(lineInset);
    parEndIt = parStartIt;
    parEndIt.ForwardLine();
    Buff.SelectRange(parStartIt, parEndIt);
  }


/// <summary>
/// Appends text to the buffer. If 'CursorToEnd', the cursor ends up at the end of the appended text;
/// otherwise it is left where it is.
/// </summary>
  public static void AppendText(TextBuffer Buff, string TextToAppend, bool CursorToEnd)
  { TextIter putIt = Buff.EndIter;
    if (CursorToEnd)
    { Buff.PlaceCursor(putIt);
      Buff.InsertAtCursor(TextToAppend);
    }
    else
    { Buff.Insert(ref putIt, TextToAppend);
    }
  }
/// <summary>
/// Inserts text into the buffer. If 'CursorToEnd', the cursor ends up at the end of the inserted text;
/// otherwise it is left where it is.
/// </summary>
  public static void InsertText(TextBuffer Buff, int Posn, string TextToInsert, bool CursorToEnd)
  { TextIter putIt = Buff.GetIterAtOffset(Posn);
    if (CursorToEnd)
    { Buff.PlaceCursor(putIt);
      Buff.InsertAtCursor(TextToInsert);
    }
    else
    { Buff.Insert(ref putIt, TextToInsert);
    }
  }
/// <summary>
/// Inserts text into the buffer. Really you should just directly use "Buff.InsertAtCursor(.)", but if you prefer
/// to call this method, go ahead.
/// </summary>
  public static void InsertTextAtCursor(TextBuffer Buff, string TextToInsert)
  { Buff.InsertAtCursor(TextToInsert);
  }
/// <summary>
/// Selects text.
/// </summary>
  public static void SelectText(TextBuffer Buff, int SelStart, int SelLength)
  { TextIter startIt = Buff.GetIterAtOffset(SelStart);
    TextIter endIt = Buff.GetIterAtOffset(SelStart + SelLength);
    Buff.SelectRange(startIt, endIt);
  }
/// <summary>
/// Returns the currently selected text, without altering the selection. If none selected, returns the empty string.
/// </summary>
  public static string ReadSelectedText(TextBuffer Buff)
  { int dummy;
    return ReadSelectedText(Buff, out dummy); // 'true' = return hidden characters
  }
/// <summary>
/// Returns the currently selected text, without altering the selection; and sets SelStart to the start of the selection,
///  or to the cursor position if no text selected. Returns the empty string if no text is selected.
/// </summary>
  public static string ReadSelectedText(TextBuffer Buff, out int SelStart)
  { TextIter startIt, endIt;
    SelStart = 0;
    if (! (Buff.GetSelectionBounds(out startIt, out endIt)))
    { SelStart = Buff.CursorPosition;   return ""; } // No text selected.
    SelStart = startIt.Offset;
    return Buff.GetText(startIt, endIt, true); // 'true' = return hidden characters
  }

/// <summary>
/// Replaces selected buffer text with NewText. To delete selected text only, set NewText to the empty string.
/// If there is no selection, NewText simply goes in at the cursor.
/// </summary>
  public static void OverwriteSelectedText(TextBuffer Buff, string NewText)
  { TextIter startIt, endIt;
    if (Buff.GetSelectionBounds(out startIt, out endIt)) // then text is selected, so delete it:
    { Buff.Delete(ref startIt, ref endIt); }
    // Either way, startIt is now set to where the new text is to go.
    if (NewText != "") Buff.Insert(ref startIt, NewText);
  }

// ===================================================
// TEXT TAGGING                                   //tag
// ---------------------------------------------------

/// <summary>
/// <para>Applies the given tag to the given extent. The tag must exist in the TextBuffer's TagTable, or else nothing
/// happens (no error raised).</para>
/// <para>If Extent is beyond the end it is adjusted back (by Mono, not me) to the end. Also does not crash if
/// FromPtr is too big; simply nothing happens.</para>
/// </summary>
  public static void TagExtent(TextBuffer Buff, TextTag ThisTag, int FromPtr, int Extent)
  { TextIter StartIt = Buff.GetIterAtOffset(FromPtr);
    TextIter EndIt = Buff.GetIterAtOffset(FromPtr + Extent);
    Buff.ApplyTag(ThisTag, StartIt, EndIt);
  }
/// <summary>
/// <para>Applies the given tag to the given extent. The tag with the given name must exist in the TextBuffer's TagTable,
/// or else nothing happens (no error raised).</para>
/// <para>If Extent is beyond the end it is adjusted back (by Mono, not me) to the end. Also does not crash if
/// FromPtr is too big; simply nothing happens.</para>
/// </summary>
  public static void TagExtent(TextBuffer Buff, string TagName, int FromPtr, int Extent)
  { TextTag toodle = Buff.TagTable.Lookup(TagName);
    if (toodle != null) TagExtent(Buff, toodle, FromPtr, Extent);
  }
/// <summary>
/// <para>Applies the given tag to all chars. between StartPtr and EndPtr inclusive. The tag must exist in the
/// TextBuffer's TagTable, or else nothing happens (no error raised).</para>
/// <para>If EndPtr is beyond the end it is adjusted back (by Mono, not me) to the end. Also does not crash if
/// StartPtr is too big; simply nothing happens.</para>
/// </summary>
  public static void TagFromTo(TextBuffer Buff, TextTag ThisTag, int StartPtr, int EndPtr)
  { TagExtent(Buff, ThisTag, StartPtr, EndPtr - StartPtr + 1);
  }
/// <summary>
/// <para>Applies the given tag to all chars. between StartPtr and EndPtr inclusive. A tag with this name
/// must exist in the TextBuffer's TagTable, or else nothing happens (no error raised).</para>
/// <para>If EndPtr is beyond the end it is adjusted back (by Mono, not me) to the end. Also does not crash if
/// StartPtr is too big; simply nothing happens.</para>
/// </summary>
  public static void TagFromTo(TextBuffer Buff, string TagName, int StartPtr, int EndPtr)
  { TagExtent(Buff, TagName, StartPtr, EndPtr - StartPtr + 1);
  }
/// <summary>
/// <para>Either tags or untags a numbered line of text. 'DoWhat': '+' = insert tag; '-' = remove tag; '~' = toggle tag;
///  '0' [or anything else, in fact] = do nothing, but return the current state - tagged or not. (NB - where DoWhat is '~' or '0', the method
/// only regards a tag as already 'on' if it is active at the beginning of the indicated line.)</para>
/// <para>If andScrollToIt is true, finishes by placing the line fairly centrally in the display. (True, even if DoWhat is '0';
///  in fact, this method could be used for no other purpose than to move a particular line into view and centred on the screen.)</para>
/// <para>'AndPutCursorThere' is only accessed (and used) if andScrollToIt is true.</para>
/// <para>RETURNS a Trio structure; field .X is 1 ('true') if, after the method has run, the line is now tagged; otherwise 0 ('false').
///  Field .Y is the offset of the start iter of this line, .Z of the next line.</para>
/// </summary>
  public static Trio TagLine(TextView TV, TextTag tagg, int LineNo, char DoWhat, bool andScrollToIt, bool AndPutCursorThere )
  { TextBuffer thisBuffer = TV.Buffer;
    TextIter thisLineIt = thisBuffer.GetIterAtLine(LineNo);
    TextIter nextLineIt;
    if (LineNo == thisBuffer.LineCount-1) nextLineIt = thisBuffer.EndIter; // the line is the last line of the buffer
    else nextLineIt = thisBuffer.GetIterAtLine(LineNo+1);
    bool isTagged = thisLineIt.HasTag(tagg); // if Dowhat is not '+', '-' or '~', this will simply be returned at the end.
    if (DoWhat == '~') // toggle the tagging:
    { if (isTagged) DoWhat = '-';  else DoWhat = '+'; } // 'BeginsTag(.)' would fail where prev. + this line had been tagged together.
    if      (DoWhat == '-') { thisBuffer.RemoveTag(tagg, thisLineIt, nextLineIt);  isTagged = false; }
    else if (DoWhat == '+') { thisBuffer.ApplyTag(tagg, thisLineIt, nextLineIt);  isTagged = true; }
    if (andScrollToIt)
    { TV.ScrollToIter(thisLineIt, 0.1, true, 0.0, 0.5);
      if (AndPutCursorThere) thisBuffer.PlaceCursor(thisLineIt);
    }
    return new Trio( (isTagged ? 1 : 0), thisLineIt.Offset, nextLineIt.Offset);
  }

/// <summary>
/// Find all lines for which the given tag is active at the START of the line. If 'LastLine' is negative or too large, it is taken as
///  the last line of the text. RETURNED: line nos., or an empty but non-null array if none found.
///  (The form of the return was chosen to be a List{int} rather than an int[] because th9e List's .IndexOf(.)
///   property will often be needed on the returned data.)
/// </summary>
  public static List<int> LinesStartingWithThisTag(TextBuffer Buff, TextTag ThisTag, int FirstLine, int LastLine)
  { if (LastLine < 0 || LastLine >= Buff.LineCount) LastLine = Buff.LineCount-1;
    List<int> tagbag = new List<int>();
    TextIter lineStartIt;
    for (int i = FirstLine; i <= LastLine; i++)
    { lineStartIt = Buff.GetIterAtLine(i);
      if (lineStartIt.HasTag(ThisTag)) tagbag.Add(i); // 'BeginsTag(.)' would fail where prev. + this line had been t9agged together.
    }
    return tagbag;
  }

/// <summary>
/// No error detection code. [Effects of faulty args. not yet tested.]
/// </summary>
 public static void RemoveTagAt(TextBuffer Buff, TextTag ThisTag, int FromPtr, int Extent)
  { TextIter startIt = Buff.GetIterAtOffset(FromPtr);
    TextIter endIt = Buff.GetIterAtOffset(FromPtr + Extent);
    Buff.RemoveTag(ThisTag, startIt, endIt);
  }
/// <summary>
/// No error detection code. [Effects of faulty args. not yet tested.]
/// </summary>
  public static void RemoveTagAt(TextBuffer Buff, string TagName, int FromPtr, int Extent)
  { TextTag toodle = Buff.TagTable.Lookup(TagName);
    if (toodle != null) RemoveTagAt(Buff, toodle, FromPtr, Extent);
  }

/// <summary>
/// <para>TextColours and HighlightColours hold colour descriptions that can be parsed by string extension "_ParseColour(.)". If you only
/// want tags to colour the foreground, set HighlightColours to NULL; and vice versa, if you only want tags to colour the background.
/// If you want a mixture - some colouring foreground, some background, some both - then (a) BOTH arrays must have the same length,
/// and (b) if for tag i you want a foreground colour but not a background colour, set HighLightColours[i] to the empty string "" (or to NULL);
/// and vice versa. NB - EITHER one array is null and the other nonnull, OR both are nonnull and of the same length.</para>
/// <para>The text tags will be given names according to the pattern [TagNamePrefix + index in the array(s)].</para>
/// <para>ANY ERRORS --> return of (FALSE, error message), and no tags are added to Buff.</para>
/// </summary>
  public static Boost InstallColourTags(TextBuffer Buff, string[] TextColours, string[] HighlightColours,  string TagNamePrefix)
  { Boost result = new Boost(false);
    // Try to develop the colours and the tag names:
    int noClrs;
    if (TextColours == null) noClrs = HighlightColours.Length; // This will crash the program if calling code has made both NULL!
    else
    { noClrs = TextColours.Length;
      if (HighlightColours != null  &&  HighlightColours.Length != noClrs)
      { result.S = "if both TextColours and HighlightColours are non-null, they must have the same length";  return result; }
    }
    string[] tagnames = new string[noClrs];
    Gdk.Color[] foreClr = new Gdk.Color[noClrs],  backClr = new Gdk.Color[noClrs];
    bool[] fore = new bool [noClrs],  back = new bool[noClrs]; // fore[i] is TRUE if tag no. i is to have a forecolour set; analogously with back[i].
    Gdk.Color dud = Gdk.Color.Zero;
    bool success = true;
    for (int i=0; i < noClrs; i++)
    { fore[i] = (TextColours != null && !string.IsNullOrEmpty(TextColours[i]) );
      if (fore[i]) foreClr[i] = TextColours[i]._ParseColour(out success);   else  foreClr[i] = dud;
      if (!success)
      { result.S = "cannot parse TextColours[" + i.ToString() + "] ( = '" + TextColours[i] + "') to obtain a colour"; return result; }
      back[i] = (HighlightColours != null && !string.IsNullOrEmpty(HighlightColours[i]) );
      if (back[i]) backClr[i] = HighlightColours[i]._ParseColour(out success);   else  backClr[i] = dud;
      if (!success)
      { result.S = "cannot parse HighlightColours[" + i.ToString() + "] ( = '" + HighlightColours[i] + "') to obtain a colour"; return result; }
      if (!fore[i] && !back[i])
      { result.S = "Neither a text colour nor a highlight colour is given for array index " + i.ToString(); return result; }
      tagnames[i] = TagNamePrefix + i.ToString();
    }
    // Build and install the tags: (Kept out of the above loop because we only want to install tags into Buff if all errors have been dealt with.)
    for (int i=0; i < noClrs; i++)
    { if (Buff.TagTable.Lookup(tagnames[i]) == null) // We only do the stuff below IF a tag of that name is not already in Buff's tag table.
                                                 // (Too bad if it is already there but does something else.)
      { TextTag tictoc = new TextTag(tagnames[i]);
        if (fore[i]) tictoc.ForegroundGdk = foreClr[i];
        if (back[i]) tictoc.BackgroundGdk = backClr[i];
        Buff.TagTable.Add(tictoc);
      }
    }
    result.B = true;  return result;
  }

// ===================================================
// TEXT VIEWPOINT                              //view
// ---------------------------------------------------

/// <summary>
/// <para>TopLocation is a Y coordinate (pixels) within Buff, relative to the start of its text. The aim is to set the
/// visible display in TV such that this location is right at the top of the visible display. If TopLocation is
/// negative, nothing happens.</para>
/// <para>Usually you would have the value for arg. TopLocation handy from a previous call to [TextView instance].VisibleRect.Top.</para>
/// <para>NB! Just before you call this, you need to flush pending events; without this, attempts to return the cursor to its earlier
/// position are doomed to failure, as Mono (like .NET) will always do this method BEFORE the pending events are carried out, even though
/// your method places this after such events. So do this first:</para>
/// <para>"while (Application.EventsPending ())   Application.RunIteration();"</para>
/// </summary>
  public static void PlaceLocnAtTopOfView(TextView TV, TextBuffer Buff, int TopLocation)
  { if (TopLocation < 0) return;
    TextIter titter = TV.GetIterAtLocation(0, TopLocation);
    TextMark toplocn_mark = Buff.CreateMark(null, titter, true);
    try
    { TV.ScrollToMark(toplocn_mark, 0.0, true, 0.0, 0.0);  // Last 3 args. --> Put the mark at top left corner of visibility.
      TV.PlaceCursorOnscreen(); // Puts the cursor somewhere in the visible region - will be the top or the bottom.
    }
    catch
    { JD.Msg("Something went wrong in trying to reset the visible part of the page!"); }
//    Buff.DeleteMark(toplocn_mark);
  }

/// <summary>
/// <para>Move the given character position into view with the least amount of scrolling possible, so that if it
/// starts out of view it will end up either at the top or the bottom line of the view.</para>
/// <para>If AndPutCursorThere is FALSE the cursor stays where it is, so that if the user then keys in a character
///  the display will fly back to wherever the cursor is.</para>
/// </summary>
  public static void MoveCharPosnIntoView(TextView TV, TextBuffer Buff, int CharPosn, bool AndPutCursorThere)
  { TextIter buffStartIt, startIt, endIt;
    Buff.GetBounds(out buffStartIt, out endIt); // default setting of iters to start and end of all text.
    if (CharPosn > endIt.Offset) CharPosn = endIt.Offset-1;
    startIt = buffStartIt;  startIt.Offset = CharPosn;
    TextMark tmk = Buff.CreateMark("tmk", startIt, true); // the text mark now marks the first find.
    TV.ScrollMarkOnscreen(tmk); // *** If it is preferable to try to centre the first find in the
      // visible area, use REditAss.ScrollToIter(.) instead, which allows specifying of where it is displayed.
      // Later: I tried this, but it ignores the inset, so that the lines are hard up against the left of the screen, with displaced horiz. scrollbar.
    Buff.DeleteMark(tmk);
    tmk.Dispose();
    if (AndPutCursorThere) Buff.PlaceCursor(startIt); // Whatever the previous selection length, it will be zero here.
  }


// ===================================================
// TEXT OPERATION (other than font changes)       //text
// ---------------------------------------------------

/// <summary>
/// <para>Overload for a SINGLE FIND. Returns the character position of the find, or -1 if none. If IgnoreTargetAtStart
/// is TRUE, ignors an occurrence right at FromPtr. If WholeWordOnly is TRUE, the presence of an identifier character
/// (letter, numeral or '_') on either side of the word disqualifies the find. If Tagg is NON-NULL and exists in Buff's
/// tag table, the method also tags the find. If tagging is required, then BufferOffset is referenced; it is the
/// offset of the beginning of TextToSearch from the start of Buff.</para>
/// <para>Note that TextToSearch is a REF argument, for economy reasons; but it is not altered by this method.</para>
/// <para>There is no provision for case-insensitive search; for this, calling code must first convert Target
/// and TextToSearch to the same case.</para>
/// <para>Set 'IdentifierChars' to the empty string, if you are content to use JS.IdentifierChars to distinguish words.</para>
/// </summary>
  public static int FindTextAndTagIt(TextBuffer Buff, string Target, ref string TextToSearch, TextTag Tagg, int FromPtr,
                                           int BufferOffset, bool wholeWordOnly, bool IgnoreTargetAtStart,  string IdentifierChars)
//  Note that the order of args. is a bit different to that in the overload, so as to produce a different method signature.
  { int targetLen = Target.Length;
    int find = -1, origFromPtr = FromPtr;
    if (IdentifierChars == "") IdentifierChars = JS.IdentifierChars;
    while (true) // Outer loop, to detect a find at locn. 0 when IgnoreTargetAtStart is TRUE
    {
      while (true) // Inner loop, to handle whole word checks where wholeWordOnly is TRUE:
      { find = TextToSearch._IndexOf(Target, FromPtr); // Using the extension form of IndexOf, so that oversized FromPtr does not crash.
int dummy=1;//############
if (find == -1) break;
        if (!wholeWordOnly || TextToSearch._IsWholeWord(find, targetLen, ref IdentifierChars)) break; // whole word found.
        FromPtr++; // the find was not of a whole word, so try again.
      }
      if (find != origFromPtr || !IgnoreTargetAtStart) break;
      // Only arrive here if there was a find, and it was at locn 0, and IgnoreTargetAtStart is TRUE:
      FromPtr += targetLen;
    }
    // Tag the find(s) if desired:
    if (find >= 0 && Tagg != null) JTV.TagExtent(Buff, Tagg, BufferOffset + find, targetLen);
    return find;
  }

/// <summary>
/// <para>Overload for MULTIPLE FINDS. Returns an array of character positions where text was found; or NULL if none.
/// If Tagg is NON-NULL and exists in Buff's tag table, the method also tags all the finds. If tagging is required,
/// then BufferOffset is referenced; it is the offset of the beginning of TextToSearch from the start of Buff.</para>
/// <para>Note that TextToSearch is a REF argument, for economy reasons; but it is not altered by this method.</para>
/// <para>There is no provision for case-insensitive search; for this, calling code must first convert Target
/// and TextToSearch to the same case.</para>
/// <para>Set 'IdentifierChars' to the empty string, if you are content to use JS.IdentifierChars to distinguish words.</para>
/// </summary>
  public static int[] FindTextAndTagIt (TextBuffer Buff, string Target, ref string TextToSearch, int FromPtr,
                                                       int BufferOffset, TextTag Tagg, bool wholeWordOnly, string IdentifierChars)
//  Note that the order of args. is a bit different to that in the overload, so as to produce a different method signature.
 { int searchlen = Target.Length;
    int[] finds = TextToSearch._IndexesOf(Target, FromPtr);
    int noFound = finds._Length(); // -1, if null.
    if (IdentifierChars == "") IdentifierChars = JS.IdentifierChars;
    if (wholeWordOnly && noFound > 0) // Remove finds which don't qualify:
    { StringBuilder sb = new StringBuilder(TextToSearch);
      List<int> findsLst = new List<int>(finds.Length);
      for (int i=0; i < noFound; i++)
      { if (sb._IsWholeWord(finds[i], searchlen, ref IdentifierChars) ) findsLst.Add(finds[i]); }
      finds = findsLst.ToArray();  noFound = finds.Length;
      if (noFound == 0) { finds = null;  noFound = -1; }
    }
    // Either way, tag the find(s) if desired:
    if (Tagg != null && noFound > 0)
    { foreach (int thisFind in finds)
      { JTV.TagExtent(Buff, Tagg, BufferOffset + thisFind, searchlen); noFound++; }
    }
    return finds;
  }

/// <summary>
/// This overload is used where calling code needs text iterators set at the start and end of the replaced text.
/// </summary>
  public static bool ReplaceTextAt(TextBuffer Buff, int StartPtr, int Extent, string NewText, out TextIter StartIt, out TextIter EndIt)
  { TextIter buffStartIt, buffEndIt;
    Buff.GetBounds(out buffStartIt, out buffEndIt); // default setting of iters to start and end of all text.
    if (StartPtr < 0 || StartPtr + Extent > buffEndIt.Offset)
    { StartIt = buffStartIt;  EndIt = buffEndIt;  return false; } // Do nothing, if arguments impossible.
    StartIt = buffStartIt;  StartIt.Offset = StartPtr;
    EndIt = buffStartIt;  EndIt.Offset = StartPtr + Extent;
    Buff.Delete(ref StartIt, ref EndIt); // NB!! There is a bug in Mono here; startIt and endIt seem to end up with the same
            // pointer. Anyway, you have to redefine startIt (3 lines below) or startIt is regarded as NULL (which is not allowed
            // for a structure, so the system crashes as soon as it is referenced).
    Buff.Insert(ref EndIt, NewText); // NB! the text iter will end up at the END of the inserted word...
    StartIt = EndIt; // See comment above re bug, requiring this line.
    StartIt.BackwardChars(NewText.Length); // ...hence StartIt has to backtrack.
    return true;
  }
/// <summary>
/// This overload is used where calling code has no need of text iterators.
/// </summary>
  public static bool ReplaceTextAt(TextBuffer Buff, int StartPtr, int Extent, string NewText)
  { TextIter startIt, endIt;
    return ReplaceTextAt(Buff, StartPtr, Extent, NewText, out startIt, out endIt);
  }

/// <summary>
/// Copies selected text from the text buffer. If 'AndCut' is true, also cuts the selected text.
/// </summary>
  public static void CopyToClipboard(TextBuffer Buff, bool AndCut)
  { Clipboard board = Clipboard.Get(Gdk.Selection.Clipboard);
    if (AndCut) Buff.CutClipboard (board, true);
      else Buff.CopyClipboard (board);
  }
/// <summary>
/// Pastes text from the clipboard into Buff.
/// </summary>
  public static void PasteFromClipboard(TextBuffer Buff)
  { Clipboard board = Clipboard.Get(Gdk.Selection.Clipboard);
    Buff.PasteClipboard(board);
  }

// ===================================================
// FONT OPERATIONS                              //font
// ---------------------------------------------------

  /// <summary>
  /// <para>ScriptPath is where a script 'aaa.sh' will be placed and chmod'd to be runnable (DON'T put it in a place requiring admin. privileges);
  /// DumpPathName is the path and name of the file in which to dump the font listing. In both cases the file will overwrite any preexisting
  ///  file of the same name without warning.</para>
  /// <para>NO ERROR: ErrorMsg is the empty string, and the list of fonts (as stored in the file DumpPathName) is returned. ERROR:
  ///  ErrorMsg tells why, and the returned value is the empty string.</para>
  /// </summary>
  public static string ListFontsOnMachine(string ScriptPath, string DumpPathName, out string ErrorMsg, bool UsingFormatTags)
  {
    ErrorMsg = "";
    string script = "#!/bin/bash\nfc-list | sort | less > '" + DumpPathName + "'";
    if (ScriptPath[ScriptPath.Length-1] != '/') ScriptPath += '/';
    string scriptfilename = ScriptPath + "aaa.sh";
    try
    { System.IO.StreamWriter w;
      w = System.IO.File.CreateText(scriptfilename);
      w.Write(script); // Don't ever use .WriteLine, as the 'B' option in particular requires that we don't add a final par. mark.
      w.Flush();   w.Close();
    }
    catch
    { ErrorMsg = "could not save font-reading script to '" + scriptfilename + "'";  return ""; }
    Quid quo = JS.RunProcess("chmod", "777 " + scriptfilename, 10000);
    quo = JS.RunProcess(scriptfilename, "", 10000);
    if (!quo.B) { ErrorMsg = quo.S; return ""; }
    string wholetext = "<# blue>";
    try
    { string ss;
      using (System.IO.StreamReader sr = new System.IO.StreamReader(DumpPathName))
      { while ((ss = sr.ReadLine()) != null)
        { wholetext += ss + '\n'; }
      }
    }
    catch
    { ErrorMsg = "could not read file'" + DumpPathName + "'"; return ""; }
    // Success.
    if (UsingFormatTags)
    { wholetext = wholetext.Replace(":", "  <# magenta>");
      wholetext = wholetext.Replace("\n", "<# blue>\n");
    }
    return wholetext;
  }

  private static double PangoScalingFactor = 0.0; // Intended exclusively for the method below, which sets it at its first use.

/// <summary>
/// Where a dimension may be either in points or in PangoScale units, this returns its value definitively in points, to the nearest integer.
/// The input must be either of type int or of type double; anything else would crash, as there is no error-handling here.
/// </summary>
  public static int ToPointsInt(int theSize)
  { return Convert.ToInt32(ToPointsDouble(theSize));
  }
/// <summary>
/// Where a dimension may be either in points or in PangoScale units, this returns its value definitively in points, as type double.
/// The input must be either of type int or of type double; anything else would crash, as there is no error-handling here.
/// </summary>
  public static double ToPointsDouble(object TheSize)
  { double thesize;
    if (TheSize is int) thesize = (double) ( (int) TheSize); else thesize = (double) TheSize;
    if (PangoScalingFactor == 0.0) PangoScalingFactor = Pango.Scale.PangoScale;
    if (thesize >= PangoScalingFactor || thesize <= -PangoScalingFactor)
    { return thesize / PangoScalingFactor; } // should now be in points.
    return thesize;
  }


/// <summary>
/// <para>Fields:</para>
/// <para>Family (one or more font names, comma-delimited; spaces inside a name are significant);</para>
/// <para>Points (font size in points);</para>
/// <para>FamSize ( = Family + space + point size);</para>
/// <para>PointsEven (font size that would apply if super- or subscripting were not invoked);</para>
/// <para>LevelType ('^' for superscript, 'v' for subscript, '-' for even script, 'x' where user wants to set
/// level and point size independently of the internal algorithm);</para>
/// <para>Level (in points, the distance of the character base from the normal line level; can be negative).</para>
/// <para>CONSTRUCTOR with args Family and PointsEven: Other settings are: Points = PointsEven, LevelType = '-',
/// Level = 0; and FamSize as above.</para>
/// </summary>
  public class FontLog
  { public string Family; // one or more font names (delimiter = comma), with internal spaces of each font name significant.
    public string FamSize; // Family + a space + string version of the size in points.
    public int Points; // char. size in points.
    public int PointsEven; // char. size in points for even script (i.e. non-superscripted, non-subscripted text).
    public char LevelType; // '^' (superscript), 'v' (subscript), '-' (even script), 'x' (none: user is not using the system
                           // algorithm to compute point sizes and level values.
    public int Level; // in points, the level above or (if neg.) below the baseline for placement of characters in the line.
    // CONSTRUCTOR:
    private FontLog(){}
    public FontLog(string Family_, int PointsEven_)
    { Family = Family_;  PointsEven = PointsEven_;
      Points = PointsEven;  LevelType = '-';  Level = 0;
      FamSize = Family + ' ' + Points.ToString();
    }
    // STATIC METHODS:
    public static FontLog Copy(FontLog Model)
    { FontLog result = new FontLog(Model.Family, Model.PointsEven);
      result.FamSize = Model.FamSize;
      result.Points = Model.Points;
      result.Level = Model.Level;
      result.LevelType = Model.LevelType;
      return result;
    }
    /// <summary>
    /// Assumes that Fount fields Family and PointsEven are valid, and works on the basis of LvlType (ignored if not
    /// one of "^, v, -").
    /// </summary>
    public static void AdjustForLevelType(ref FontLog Fount, char LvlType)
    { if (LvlType != '-' && LvlType != '^' && LvlType != 'v') return; // No action if LvlType improper.
     // Set fields Points and Level:
      if (LvlType == '-') { Fount.Points = Fount.PointsEven;  Fount.Level = 0; }
      else
      { Fount.Points = (int) Math.Round( (double) (Fount.PointsEven + 32) / 6.0); // super- and subscript sizes are the same.
        if (LvlType == '^')
        { Fount.Level = (int) Math.Round( (double) (3 * Fount.PointsEven - 6) / 4.0); }
        else // subscript:
        { Fount.Level = -3; } // one size appears to fit all.
      }
     // Set the remaining fields:
      Fount.FamSize = Fount.Family + ' ' + Fount.Points.ToString();
      Fount.LevelType = LvlType;
    }
  } // END OF class FontLog
/// <summary>
/// <para>Name format: "z_[Points]_[PointsEven]_[Level]_[LevelType]_[first entry in Family, with spaces replaced by '_']".</para>
/// <para>LevelType values are coded from {^, v, -, x} to {p, b, e, x}, to ensure alphanumeric chars. only in the name.</para>
/// </summary>
  public static string SuitableTagName(FontLog Fount)
  { string result = "z_"; // All tags generated from markup tags have this prefix, to avoid ambiguity with any other tags present.
    result += Fount.Points.ToString() + '_' + Fount.PointsEven.ToString() + '_' + Fount.Level.ToString() + '_';
    char c = Fount.LevelType;   string levelStr = "";
    if      (c == '^') levelStr = "UP";   else if (c == 'v') levelStr = "DOWN";
    else if (c == '-') levelStr = "EVEN";  else               levelStr = "NONE";
    result += levelStr + '_'; // NB: Only the first letter is ever checked; keep in mind if levelStr options are being changed.
    int n = Fount.Family.IndexOf(',');
    if (n == -1) result += Fount.Family;   else result += Fount.Family.Substring(0, n);
    result = result.Replace(' ', '_'); // No spaces, please.
    return result;
  }
/// <summary>
/// <para>Note that tag names only hold a single font name, so that the FontLog field 'Family' that is returned
/// will not correspond to the actual tag's 'Family' field if that field has multiple entries.</para>
/// <para>If name-parsing fails, NULL is returned, with ErrorMsg set; otherwise a usable FontLog object is returned,
/// and ErrorMsg is empty.</para>
/// </summary>
  public static FontLog ParseTagName(string TagName, out string ErrorMsg)
  { ErrorMsg = "";
    if (TagName._CountChar('_') < 5) { ErrorMsg = "too few underscores"; return null; }
    if (TagName._Extent(0, 2) != "z_") { ErrorMsg = "introductory 'z_' not found"; return null; }
    string[] taggery = TagName.Split(new char[] {'_'}, 6); // We only want taggery to have 6 elements.
    int points=0, pointsEven=0, level=0;  bool success;
    points = taggery[1]._ParseInt(out success);
    if (!success)   { ErrorMsg = "failed to parse the 'Points' element"; return null; }
    pointsEven = taggery[2]._ParseInt(out success);
    if (!success)   { ErrorMsg = "failed to parse the 'PointsEven' element"; return null; }
    level = taggery[3]._ParseInt(out success);
    if (!success)   { ErrorMsg = "failed to parse the 'Level' element"; return null; }
    // Check for sensible values:
    if (points < 1)     { ErrorMsg = "the 'Points' element is 0 or negative"; return null; }
    if (pointsEven < 1 ){ ErrorMsg = "the 'PointsEven' element is 0 or negative"; return null; }
    // From here on down parsing cannot fail.
    char c = taggery[4][0], ltyp;
    if (c == 'U') ltyp = '^';  else if (c == 'D') ltyp = 'v';  else if (c == 'E') ltyp = '-';  else ltyp = 'x';
    // Build the return object:
    FontLog result = new FontLog(taggery[5], pointsEven);
    result.Points = points;  result.Level = level;  result.LevelType = ltyp;
    return result;
  }

/// <summary>
/// Returns with fields Family, Points, FamilySize set directly from the TextView's default attributes; and with
/// PointsEven = Points, LevelType = '-', Level = 0.
/// </summary>
  public static FontLog DefaultFontData(TextView TV)
  { TextAttributes tatters = TV.DefaultAttributes;
    FontLog result = new FontLog(tatters.Font.Family, ToPointsInt(tatters.Font.Size) );
    tatters.Dispose();
    return result;
  }
/// <summary>
/// <para>Returns an int[] containing indexes in TagNames of finds, or NULL if no finds. This method is not much bl. use if you didn't give the tags
///  names when you were creating them.</para>
/// <para>No adjusting of TagNames elements, which should if necessary be conformed to the exact tag names before the call.</para>
/// </summary>
  public static int[] TagsApplyingHere(TextBuffer Buff, string[] TagNames, int CharPosn)
  { TextIter tex = Buff.GetIterAtOffset(CharPosn);
    TextTag[] taggery = tex.Tags;  if (taggery._NorE()) return null;
    List<int> finds = new List<int>();
    for (int i = 0; i < taggery.Length; i++)
    { string ss = taggery[i].Name;
      for (int j = 0; j < TagNames.Length; j++)
      { if (ss == TagNames[j]) { finds.Add(j);  break; } }
    }
    if (finds.Count == 0) return null;  else return finds.ToArray();
  }

/// <summary>
/// <para>Given a text location (in this overload as a TextIter object), develop a FontLog object that characterizes
/// the font there.</para>
/// <para>In all cases, fields Points, Family, FamSize and Level will be set directly from the TextTag applying at the location.
/// The rules for the other two fields (LevelType, PointsEven) vary: (a) If the TextTag's name parses with ParseTagName(.),
/// these fields are copied from the parsed name; (b) if that name does not parse (e.g. is a tag in Buff's tag table not
/// generated by method DisplayMarkUpText), LevelType is set to 0 and PointsEven to Points.</para>
/// </summary>
  public static FontLog LocalFontData(TextView TV, TextBuffer Buff, TextIter titter)
  { FontLog result = DefaultFontData(TV); // If no font-changing tags overlay this iterator, this is what will be returned.
    TextTag[] taggery = titter.Tags;
    int tagFind = -1;
    for (int i=taggery.Length-1; i >= 0; i--) // We only want the last font-changing tag, as it has priority.
    { if (taggery[i].Family != null) {tagFind = i; break; } }
    if (tagFind >= 0)
    { result.Points = ToPointsInt(taggery[tagFind].FontDesc.Size);
      result.Family = taggery[tagFind].Family;
      result.FamSize = result.Family + ' ' + result.Points.ToString();
      result.Level = ToPointsInt(taggery[tagFind].Rise);
      string errmsg = "";
      FontLog fromname = ParseTagName(taggery[tagFind].Name, out errmsg);
      // Is the tag name parsable, and if so, does it produce a LevelType other than 'x'?
      if (fromname == null) // then the name is not parsable - it may well be a preexisting font tag in Buff's tag table:
      { result.PointsEven = result.Points;
        result.LevelType = '0'; // The font can be super-subscripted, but is assumed to be in even script.
      }
      else
      { result.PointsEven = fromname.Points;
        result.LevelType = fromname.LevelType;
      }
    }
    return result;
  }
/// <summary>
/// <para>Given a text location (in this overload as a character offset), develop a FontLog object that characterizes
/// the font there.</para>
/// <para>In all cases, fields Points, Family, FamSize and Level will be set directly from the TextTag applying at the location.
/// The rules for the other two fields (LevelType, PointsEven) vary: (a) If the TextTag's name parses with ParseTagName(.),
/// these fields are copied from the parsed name; (b) if that name does not parse (e.g. is a tag in Buff's tag table not
/// generated by method DisplayMarkUpText), LevelType is set to 0 and PointsEven to Points.</para>
/// </summary>
  public static FontLog LocalFontData(TextView TV, TextBuffer Buff, int CharOffset)
  { TextIter tex = Buff.GetIterAtOffset(CharOffset);
    return LocalFontData(TV, Buff, tex);
  }
/// <summary>
/// <para>Sets the font and the level for the given font TextTag. The name of Tagg would have been built from
/// FontData before this method was called, so that there is no conflict between the data in the name and the
/// relevant fields of the Tagg.</para>
/// </summary>
  public static void BuildFontDataIntoTag(FontLog FontData, ref TextTag Tagg)
  { Tagg.FontDesc = Pango.FontDescription.FromString(FontData.FamSize);
    Tagg.Rise = (int) ( (double) FontData.Level * Pango.Scale.PangoScale);
  }

/// <summary>
/// Given FontData, compute what should be the point size for 'level 0' text (i.e. neither super- nor subscripted text).
/// The level in FontData (field .IY) will have its sign taken ('+', 0 or '-') but its value will be ignored.
/// </summary>
  private static int GetLevel0PointSize(Strint2 FontData)
  { int result = FontData.IX, level = FontData.IY;
    if (level != 0) result += 5;
    return result;
  }
/// <summary>
/// Given Level0FontData, compute what should be the point size for text which is superscript (in which case NewLevel enters
/// as any positive integer - value ignored), subscript (NewLevel enters as negative), or level (NewLevel enters as 0).
/// The level in FontData (field .IY) is ignored, being assumed to be 0 (for level script). REF arg. NewLevel returns with
/// a valid integer value.
/// </summary>
  private static int ComputeLevelPointSize(int Level0PointSize, ref int NewLevel)
  { int result = Level0PointSize;
    if (NewLevel < 0) NewLevel = -3; else NewLevel = 3; // By experimentation I found that one size fits all.
    return result;
  }

/// <summary>
/// <para>Apply a font to an extent of text. 'FamilyList' is either a single font name ("Arial") or a series of comma-separated
/// font names ("Comic Sans MS, Arial, FreeMono") in order of preference.</para>
/// <para>In the process a tag name will be internally generated and added to Buff's TagTable. If this method is called
/// on different occasions for different portions of text but with the same font details, then the previously generated
/// tag will be reused. The format of the tag name will be e.g. "Comic_Sans_MS_12" (for size 12), where the font name
/// used is the name of the first font in your list (whether or not it is in fact the font that is applied).</para>
/// <para>'Level' raises or (if neg.) lowers the baseline of the text relative to neighbouring characters, as is required
/// for super- and subscripts. This method is exactly the same as "SetFontOfExtent(.)" if Level = 0.</para>
/// <para>A caveat: When such a tag name is found in the tag table, there is no way for this method to know
/// whether it had created it or whether some unrelated code had created it; it will be uncritically applied.</para>
/// <para>Another caveat: Tags have a pecking order; later tags prevail over earlier ones. Normally you wouldn't overlap
/// tagged extents of text, but if you do, you have to be aware of this.</para>
/// </summary>
  public static void SetFontAndLevelOfExtent(TextView TV, TextBuffer Buff, int StartPtr, int Extent, string FamilyList,
                        int PointSize, int Level)
  {// Give the tag a name.
    string[] stroo = FamilyList.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
    if (stroo == null || stroo.Length == 0) return; // No Family list, no dice.
    string stree = stroo[0].Replace(' ', '_') + '_' + PointSize.ToString();
    if      (Level > 0) stree += '_' + Level.ToString();
    else if (Level < 0) stree += "__" + Level.ToString(); // If Level = 0, no attachment added to stree.
   // Look it up in the tag table; if not there, give it attributes and then add it in.
    TextTag tigger = Buff.TagTable.Lookup(stree);
    if (tigger == null)
    { tigger = new TextTag(stree);
      tigger.FontDesc = Pango.FontDescription.FromString(FamilyList + ' ' + PointSize.ToString());
      tigger.Rise = (int) ( (double) Level * Pango.Scale.PangoScale);
      Buff.TagTable.Add(tigger);
    }
   // Now apply the tag to the given extent of text
    TextIter startIt = Buff.GetIterAtOffset(StartPtr);
    TextIter endIt = Buff.GetIterAtOffset(StartPtr + Extent);
    Buff.ApplyTag(tigger, startIt, endIt);
  }
/// <summary>
/// <para>Apply a font to an extent of text. 'FamilyList' is either a single font name ("Arial") or a series of comma-separated
/// font names ("Comic Sans MS, Arial, FreeMono") in order of preference.</para>
/// <para>In the process a tag name will be internally generated and added to Buff's TagTable. If this method is called
/// on different occasions for different portions of text but with the same font details, then the previously generated
/// tag will be reused. The format of the tag name will be e.g. "Comic_Sans_MS_12" (for size 12), where the font name
/// used is the name of the first font in your list (whether or not it is in fact the font that is applied).</para>
/// <para>A caveat: When such a tag name is found in the tag table, there is no way for this method to know
/// whether it had created it or whether some unrelated code had created it; it will be uncritically applied.</para>
/// <para>Another caveat: Tags have a pecking order; later tags prevail over earlier ones. Normally you wouldn't overlap
/// tagged extents of text, but if you do, you have to be aware of this.</para>
/// </summary>
  public static void SetFontOfExtent(TextView TV, TextBuffer Buff, int StartPtr, int Extent, string FamilyList, int PointSize)
  { SetFontAndLevelOfExtent(TV, Buff, StartPtr, Extent, FamilyList, PointSize, 0);
  }
/// <summary>
/// Get the width and height of a specific character 'TestChar' for the stated font, which must be one of the fonts
/// available to TV.
/// </summary>
  public static Duo GetCharacterWidthHeight(TextView TV, FontDescription font_desc, char TestChar)
  { Pango.Layout layout = TV.CreatePangoLayout(TestChar.ToString()); // Only a copy of TV's layout.
    layout.FontDescription = font_desc;
    int width, height;
    layout.GetPixelSize(out width, out height); // the pixel size of the string of spaces created above.
    layout.Dispose();
    return new Duo(width, height);
  }
/// <summary>
/// <para>Get the width and height of a specific character 'TestChar' for either a particular font (which must be one available
/// to TV) or for the default font of TV (by setting FontNameSize to "" - or to anything but a valid value).</para>
/// <para>FontNameSize should take the form: [one or more (comma delimited) font names][space][integer]; e.g.
/// "Courier New 12" or "Courier New, Arial 12". Variations from this have the following effects (by example):</para>
/// <para>-- "Courier New": TV's default font size will be applied to this font.</para>
/// <para>-- "12": TV's default font will be used, along with this size.</para>
/// <para>-- "" or "gobbledygook": TV's default font and size will be used.</para>
/// </summary>
  public static Duo GetCharacterWidthHeight(TextView TV, string FontNameSize, char TestChar)
  { Pango.Layout layout = TV.CreatePangoLayout(TestChar.ToString()); // Only a copy of TV's layout.
    layout.FontDescription = FontDescription.FromString(FontNameSize);
    int width, height;
    layout.GetPixelSize(out width, out height); // the pixel size of the string of spaces created above.
    layout.Dispose();
    return new Duo(width, height);
  }
/// <summary>
/// <para>For this overload, the size unit for the array 'TabStops' is NOT pixels but widths of the sample character
/// 'SizingChar'. If the font is a proportional ('monospaced') font, then character choice is irrelevant; but if it is
/// a 'true type' font, then choose your width carefully. E.g. use a wide character like capital 'W', so that you can
/// (almost) guarantee that if the gap between two successive tab stops is N, then N-1 chars. will always fit between
/// the two stops.</para>
/// <para>Note that values in TabStops are absolute positions, not differences; therefore values should be ascending.
/// No error is raised if they are not so, but you won't get the tab stops that you might have intended.</para>
/// </summary>
  public static void SetUpTabStops(TextView TV, ref Pango.FontDescription font_desc, char SizingChar,
                                                                                                  params int[] TabStops)
  { Pango.Layout layout = TV.CreatePangoLayout(SizingChar.ToString());// create_pango_layout(tab_string)
    layout.FontDescription = font_desc;
    int noStops = TabStops.Length;  if (noStops == 0) return;
    int width, height;
    layout.GetPixelSize(out width, out height); // the pixel size of the string of spaces created above.
    Pango.TabArray tab_array = new Pango.TabArray(0, true); // 'true' = use pixels as the unit.
            // The 'array' in fact is expansible, so that the initial size is irrelevant; I use 0, as the time used up by
            // Pango in periodically incrementing the size of the array would be extremely trivial in an application.
    // Re these settings: the final arg. is an absolute position, so must increase as new tabs are added. (It won't crash
    // if they decrease, but you will simply get different stops to what you intended.) The tab array has a virtual
    // extension to infinity, in which all tab settings after your last will increment by whatever was the difference
    // between your last and second last tab settings. (If you only ever made one setting, the virtual 'second last'
    // setting is 0.)
    for (int i=0; i < TabStops.Length; i++)
    { tab_array.SetTab(i, Pango.TabAlign.Left, width * TabStops[i]);
    }
    TV.Tabs = tab_array.Copy();
    tab_array.Dispose();
  }
/// <summary>
/// <para>For this overload, the size unit for the array 'TabStops' is pixels. Note that values in TabStops are absolute
/// positions, not differences; therefore values should be ascending. No error is raised if they are not so, but you
/// won't get the tab stops that you might have intended.</para>
/// <para>If you only want to set a new tab interval for all tabs, just enter a single value in TabStops.</para>
/// </summary>
  public static void SetUpTabStops(TextView TV, params int[] TabStops)
  { int noStops = TabStops.Length;  if (noStops == 0) return;
    Pango.TabArray tab_array = new Pango.TabArray(0, true); // 'true' = use pixels as the unit.
            // The 'array' in fact is expansible, so that the initial size is irrelevant; I use 0, as the time used up by
            // Pango in periodically incrementing the size of the array would be extremely trivial in an application.
    // Re these settings: the final arg. is an absolute position, so must increase as new tabs are added. (It won't crash
    // if they decrease, but you will simply get different stops to what you intended.) The tab array has a virtual
    // extension to infinity, in which all tab settings after your last will increment by whatever was the difference
    // between your last and second last tab settings. (If you only ever made one setting, the virtual 'second last'
    // setting is 0.)
    for (int i=0; i < TabStops.Length; i++)
    { tab_array.SetTab(i, Pango.TabAlign.Left, TabStops[i]);
    }
    TV.Tabs = tab_array.Copy();
    tab_array.Dispose();
  }

/// <summary>
///
/// </summary>
  public static void SetUpTabStopsInATag(ref TextTag Tagg, params int[] TabStops)
  { int noStops = TabStops.Length;  if (noStops == 0) return;
    Pango.TabArray tab_array = new Pango.TabArray(0, true); // 'true' = use pixels as the unit.
            // The 'array' in fact is expansible, so that the initial size is irrelevant; I use 0, as the time used up by
            // Pango in periodically incrementing the size of the array would be extremely trivial in an application.
    // Re these settings: the final arg. is an absolute position, so must increase as new tabs are added. (It won't crash
    // if they decrease, but you will simply get different stops to what you intended.) The tab array has a virtual
    // extension to infinity, in which all tab settings after your last will increment by whatever was the difference
    // between your last and second last tab settings. (If you only ever made one setting, the virtual 'second last'
    // setting is 0.)
    for (int i=0; i < TabStops.Length; i++)
    { tab_array.SetTab(i, Pango.TabAlign.Left, TabStops[i]);
    }
    Tagg.Tabs = tab_array.Copy();
    tab_array.Dispose();
  }

  public static void SetUpTabStopsInATag(TextView TV, ref Pango.FontDescription font_desc, ref TextTag Tagg,
                                                                                char SizingChar, params int[] TabStops)
  { Pango.Layout layout = TV.CreatePangoLayout(SizingChar.ToString());
    layout.FontDescription = font_desc;
    int noStops = TabStops.Length;  if (noStops == 0) return;
    int width, height;
    layout.GetPixelSize(out width, out height); // the pixel size of the string of spaces created above.
    Pango.TabArray tab_array = new Pango.TabArray(0, true); // 'true' = use pixels as the unit.
            // The 'array' in fact is expansible, so that the initial size is irrelevant; I use 0, as the time used up by
            // Pango in periodically incrementing the size of the array would be extremely trivial in an application.
    // Re these settings: the final arg. is an absolute position, so must increase as new tabs are added. (It won't crash
    // if they decrease, but you will simply get different stops to what you intended.) The tab array has a virtual
    // extension to infinity, in which all tab settings after your last will increment by whatever was the difference
    // between your last and second last tab settings. (If you only ever made one setting, the virtual 'second last'
    // setting is 0.)
    for (int i=0; i < TabStops.Length; i++)
    { tab_array.SetTab(i, Pango.TabAlign.Left, TabStops[i]);
    }
    Tagg.Tabs = tab_array.Copy();
    tab_array.Dispose();
  }





// ===================================================
// TEXT MARKUP                              //markup //fmt //format
// ---------------------------------------------------

/// <summary>
/// Checks that the tags necessary for markup text are in Buff; if not, they are added. There is no
/// test for what the tags do; if Buff has a tag with the same name but differently defined, then
/// the markup text will be formatted according to Buff's original tag.
/// </summary>
  public static void IncorporateMarkupTagsIntoTextTable(TextBuffer Buff)
  {// If the buffer has been this way before, don't trouble it any further:
    if (EducatedBuffs.IndexOf(Buff.Handle) >= 0) return;
    EducatedBuffs.Add(Buff.Handle);
    TextTagTable table = Buff.TagTable;
    if (table.Lookup("bold") == null)
    { TextTag bold = new TextTag("bold");  bold.Weight = Weight.Bold;  table.Add(bold); }
    if (table.Lookup("italic") == null)
    { TextTag italic = new TextTag("italic");  italic.Style = Pango.Style.Italic;  table.Add(italic); }
    if (table.Lookup("underline") == null)
    { TextTag underline = new TextTag("underline");  underline.Underline = Pango.Underline.Single;  table.Add(underline); }
    if (table.Lookup("doubleunderline") == null)
    { TextTag doubleunderline = new TextTag("doubleunderline");
                                    doubleunderline.Underline = Pango.Underline.Double;  table.Add(doubleunderline); }
    if (table.Lookup("strikethrough") == null)
    { TextTag strikethrough = new TextTag("strikethrough");
                                    strikethrough.Strikethrough = true;  table.Add(strikethrough); }
    if (table.Lookup("locktext") == null)
    { TextTag locktext = new TextTag("locktext");  locktext.Editable = false;  table.Add(locktext); }
    if (table.Lookup("justification_left") == null)
    { TextTag justification_left = new TextTag("justification_left");
                                    justification_left.Justification = Justification.Left;  table.Add(justification_left); }
    if (table.Lookup("justification_centre") == null)
    { TextTag justification_centre = new TextTag("justification_centre");
                              justification_centre.Justification = Justification.Center;  table.Add(justification_centre); }
    if (table.Lookup("justification_right") == null)
    { TextTag justification_right = new TextTag("justification_right");
                                 justification_right.Justification = Justification.Right;  table.Add(justification_right); }
    if (table.Lookup("justification_fill") == null)
    { TextTag justification_fill = new TextTag("justification_fill");
                                    justification_fill.Justification = Justification.Fill;  table.Add(justification_fill); }
    // COLOURS and SUPER/SUBSCRIPTS are NOT assigned here; they are added as required within DisplayMarkUpText(.).
  }

  public static Dictionary<string, int> MarkUpTags = null;
  public static Dictionary<int, string> MarkUpTagNames = null;

  public static void PopulateMarkUpTags()
  {
    if (MarkUpTags != null) return;
    MarkUpTags = new Dictionary<string, int>();
    MarkUpTagNames = new Dictionary<int, string>();
   // NULL TAG, used as a bookmark for searching markup code; the tag will simply be removed, having no effect.
    MarkUpTagNames.Add(10, "null");            MarkUpTags.Add("!!", 10);

   // PAIRED TAGS that do not alter text:  Codes are >= 100 and < 500; opener codes must be even, closers 1 higher (and so odd).
    MarkUpTagNames.Add(100, "bold");            MarkUpTags.Add("b", 100);     MarkUpTags.Add("/b", 101);
    MarkUpTagNames.Add(110, "italic");          MarkUpTags.Add("i", 110);     MarkUpTags.Add("/i", 111);
    MarkUpTagNames.Add(120, "underline");       MarkUpTags.Add("u", 120);     MarkUpTags.Add("/u", 121);
    MarkUpTagNames.Add(130, "doubleunderline"); MarkUpTags.Add("uu",130);     MarkUpTags.Add("/uu",131);
    MarkUpTagNames.Add(140, "strikethrough");   MarkUpTags.Add("x", 140);     MarkUpTags.Add("/x", 141);
    MarkUpTagNames.Add(300, "locktext");        MarkUpTags.Add("lock", 300);  MarkUpTags.Add("/lock",301);

  // FONT-ALTERING TAGS:  Codes are 1000 to < 2000.
    MarkUpTagNames.Add(1000, "textcolour");     MarkUpTags.Add("#", 1000);    MarkUpTags.Add("/#", 1001);
    MarkUpTagNames.Add(1100, "backcolour");     MarkUpTags.Add("~", 1100);    MarkUpTags.Add("/~", 1101);
    MarkUpTagNames.Add(1200, "font");           MarkUpTags.Add("font", 1200); MarkUpTags.Add("/font", 1201);
    MarkUpTagNames.Add(1500, "superscript");    MarkUpTags.Add("^", 1500);    MarkUpTags.Add("/^", 1501);
    MarkUpTagNames.Add(1510, "subscript");      MarkUpTags.Add("v", 1510);    MarkUpTags.Add("/v", 1511);

  // TAGS WITH OPTIONAL CLOSER, being negated by that closer, or by another instance of the opener,
  //   or applying to end of text. If they have suffixes, they should be case-insensitive. Codes are 2000 to < 5000.
    MarkUpTagNames.Add(2000, "stops");          MarkUpTags.Add("stops", 2000); MarkUpTags.Add("/stops", 2001); // tab stops
    MarkUpTagNames.Add(2100, "just");           MarkUpTags.Add("just",  2100); MarkUpTags.Add("/just", 2101); // par. justificn.
    MarkUpTagNames.Add(2200, "indent");         MarkUpTags.Add("in",    2200); MarkUpTags.Add("/in", 2201); // par. 1st. line indent.
    MarkUpTagNames.Add(2300, "margin_left");    MarkUpTags.Add("left",  2300);
    MarkUpTagNames.Add(2310, "margin_right");   MarkUpTags.Add("right", 2310);


  // MISCELLANEOUS.   Codes are 5000 to <10000.
    MarkUpTagNames.Add(5000, "bgdclr");         MarkUpTags.Add("bgdclr", 5000); // background colour for the text view
    MarkUpTagNames.Add(5500, "link");           MarkUpTags.Add("goto", 5500);  MarkUpTags.Add("/goto", 5501);
    MarkUpTagNames.Add(9000, "par");            MarkUpTags.Add("par",  9000); // multiple settings applying to a single paragraph.
    MarkUpTagNames.Add(9050, "bullet");         MarkUpTags.Add("bullet",  9050); // specific such multiple settings for a single bulleted paragraph.


  // TAGS THAT INSERT CHARS. INTO BUFFER TEXT and nothing else: Codes are > 10000. (Nothing is higher.)
  //  (a) No suffix: (10000 to < 11000)
    MarkUpTagNames.Add(10000, "linefeed");      MarkUpTags.Add(@"\n", 10000);
    MarkUpTagNames.Add(10005, "tab");           MarkUpTags.Add(@"\t", 10005);
  //  (b) Suffix required: (11000 to infinity)
    MarkUpTagNames.Add(11000, "greek");         MarkUpTags.Add("|", 11000);
    MarkUpTagNames.Add(11010, "maths");         MarkUpTags.Add("*", 11010);
    MarkUpTagNames.Add(11100, "unicode");       MarkUpTags.Add("&", 11100);
  }

/// <summary>
/// <para>Displays text with markup tags as defined at the end of this source code file ('jTextView.cs').</para>
/// <para>'Where' case-sens. values: "fill" = replace any current text with this text; "cursor" = at current cursor position;
/// "start" = at start (moving existing text up);  "append" = append (i.e. at end of existing text); "1234" = start at
///  the specific character position 1234 (adjusted back to text extreme, if outside of existing text). Any other
///  value aborts the method. NB! If old text is to remain, (a) tags applying at the insertion position will apply to the
///  new text (e.g. if you are appending text, and the old text has tags that go to the end of the buffer, then they will
///  also apply to the end of the added text). (b) tags have priority, so that if the old text has a higher priority tag
///  applying at the position where you are inserting new text, the old tag will overrule any tags of the same type
///  applying to part of the new text. BEST POLICY: Make sure no tags are still in operation for the final char. before the
///  insertion point.</para>
/// <para>'EXTRAS' -- special settings:</para>
/// <para> Extras[0] is for LINKAGES. If no linking required, set it to NULL. Otherwise it must be an IntPtr,
/// the handle of the calling text window, to be stored in JD.Linkages. NB!! Calling code has the
/// responsibility of clearing (but not disposing of) JD.Linkages after they cease to be relevant.</para>
/// <para>Extras[1] is a BOOL: true (the DEFAULT) = CONVERT "\n" and "\t" to linefeed and tab, and "\\" to "\".</para>
/// </summary>
  public static void DisplayMarkUpText(TextView TV, ref string InText, string Where,  params object[] Extras) //display //markup
  { TextBuffer Buff = TV.Buffer;
    PopulateMarkUpTags(); // Does nothing, if dictionary MarkUpTags has already been populated.
    IncorporateMarkupTagsIntoTextTable(Buff); // Does nothing, if the req'd text tags are already in Buff's tag table.
    List<Quid> bitter = new List<Quid>(); // A list of what will one day be TextIter offsets. .IX is the tag's code,
                                          // .IY the future TextIter location, .S the suffix, .B unused (always TRUE).
    FontLog LatestFontData = DefaultFontData(TV);
    string ss;
   // Set up for linkages:
    List <JD.Linkage> linkery = null; // If used, will eventually end up tacked onto the end of JD.Linkages.
        // The NULLness of 'linkery' is used by code in lieu of a boolean flag, to indicate that linkages are to be ignored.
    IntPtr callingHandle = IntPtr.Zero; // Handle of calling window, used where linkages are to be parsed.
   // Deal with 'Extras' argument:
    bool convertBackSlashCodes = true;
    if (Extras != null) // Necessary because a single 'null' entered by the user is meant by him to imply that Extras[0] should not
                        //  be set; but Mono takes that null as meaning that Extras itself is null. (No problem if anything else follows 'null'.)
    {// Extras[0] -- Linkages:
      if (Extras.Length > 0 && Extras[0] != null && Extras[0] is IntPtr)
      { callingHandle = (IntPtr) Extras[0];
        linkery = new List<JD.Linkage>();
      }
      // Extras[1] -- Backslash handling:
      if (Extras.Length > 1 && Extras[1] != null && Extras[1] is bool) convertBackSlashCodes = (bool) Extras[1];
    }

    // Where to put the stuff?
    int bufftextlen = Buff.Text.Length, buffOffset = 0, inserttextlen = 0;
    TextIter PutItHere;  bool clearBuff = false;
    if      (Where == "fill" || bufftextlen == 0) { clearBuff = true;  PutItHere = Buff.StartIter; }
    else if (Where == "start")  PutItHere = Buff.StartIter;
    else if (Where == "cursor") { buffOffset = Buff.CursorPosition;  PutItHere = Buff.GetIterAtOffset(buffOffset); }
    else if (Where == "append") { buffOffset = bufftextlen;  PutItHere = Buff.EndIter; }
    else
    { buffOffset = Where._ParseInt(-1);  if (buffOffset < 0) return;
      if (buffOffset >= bufftextlen) { buffOffset = bufftextlen;  PutItHere = Buff.EndIter; }
      else PutItHere = Buff.GetIterAtOffset(buffOffset);
    }

  // MAKE PRE-PROCESSING SUBSTITUTIONS:
   // (1) BACKSLASH CODES: When the user enters a backslash it is displayed literally as a backslash. Without the
   //
    string inTxt = InText;
    if (convertBackSlashCodes)
    { inTxt = inTxt.Replace(@"\\", GreaterThanStr); // i.e. with something not expected in the rest of text.
      inTxt = inTxt.Replace(@"\n", "\n");
      inTxt = inTxt.Replace(@"\t", "\t");
      inTxt = inTxt.Replace(GreaterThanStr, @"\");
    }

   // (2) SWAPS FOR INTERNAL PURPOSES: Replace '\<' and '\>' up front, with characters "never" used:
    inTxt = inTxt.Replace(@"\<", LessThanStr);
    inTxt = inTxt.Replace(JS.CRLF, "\r"); // In case this string came from a MS Windows text file.
    inTxt = inTxt.Replace(@"\>", GreaterThanStr);
    inTxt = inTxt.Replace("\\\"", DoubleQuoteStr);

   // (3) REPLACEMENT LINES:   ("#VAR [target] [replacement]\n")
    int startPtr = 0, endPtr = 0, targetPtr = 0, replacemtPtr = 0, endofLinePtr = 0;
    while (startPtr < inTxt.Length)
    { startPtr = inTxt.IndexOf("#VAR ", startPtr);  if (startPtr == -1) break;
      targetPtr = startPtr + 5;
      replacemtPtr = 1 + inTxt.IndexOf(' ', targetPtr);
      endofLinePtr = inTxt.IndexOf('\n', replacemtPtr) - 1; if (endofLinePtr == -2) endofLinePtr = inTxt.Length-1;
      string target = inTxt._FromTo(targetPtr, replacemtPtr - 2);
      if (target != "") // then capture the whole markup tag, and excise it from inTxt:
      { string replacemt = inTxt._FromTo(replacemtPtr, endofLinePtr);
        // Allow for character substitutions:
        string[] substituteTags = new string[] {"<|", "<*", "<&"};
        Duo[] subTags = replacemt._IndexesOfAny(substituteTags);
        if (subTags != null)
        { for (int i = subTags.Length-1; i >= 0; i--)
          { int starttagptr = subTags[i].X, endtagptr = replacemt.IndexOf('>', starttagptr);
            if (endtagptr == -1) continue;
            string prefix_ = replacemt._Extent(starttagptr+1, 1);
            string suffix_ = replacemt._Between(starttagptr + 1, endtagptr).TrimStart();
            ss = DecodeTextInsertion(prefix_, suffix_);
            replacemt = replacemt._ScoopTo(starttagptr, endtagptr, ss);
          }
        }
        // Remove the whole line first:
        inTxt = inTxt.Remove(startPtr, endofLinePtr - startPtr + 2); // '2' to include the '\n'
        // Do the substitutions:
        inTxt = inTxt.Replace(target, replacemt);
      }
      startPtr++;
    }

  // USE STRINGBUILDERS from here down:

    int inLen = inTxt.Length;
    StringBuilder sbIn = new StringBuilder(inTxt);
    StringBuilder dump = new StringBuilder(inLen); // Will accumulate the text which is to go to Buff.

  // COLLECT AND REMOVE TAGS, storing their data in list 'bitter':
    int dumpStartPtr = 0; // The char. at which the next piece of sbIn to go to the dump commences.
    int tagOpenerAt = -1; // only > -1 if focus follows '<' and while '>' is awaited or being tested.
    bool spaceFound = false; // detects the first space, if any, in a tag. Later spaces all ignored.
    bool recognized = false; // returned by the dictionary lookup for valid tags.
    string dicKey = ""; int dicVal = -1;
    char ch; string prefix = "", suffix = "";
    bool keepSpaces = false; // Only true between double-quotes in a tag
    for (int ptr=0; ptr < inLen; ptr++)
    { ch = sbIn[ptr];
      if      (ch == '<')
      { if (tagOpenerAt >= 0) // then this '<' follows a previous unclosed '<'; the earlier one thereofre was not a tag opener:
        { for (int i = tagOpenerAt; i < ptr; i++) dump.Append(sbIn[i]);
          dumpStartPtr = ptr+1;
        }
        tagOpenerAt = ptr;  prefix = "";  suffix = "";  spaceFound = false;  keepSpaces = false;
        for (int i = dumpStartPtr; i < ptr; i++) dump.Append(sbIn[i]);
      }
      else if (ch == '>' && tagOpenerAt >= 0)
      { dicKey = prefix.ToLower();
        recognized = MarkUpTags.TryGetValue(dicKey, out dicVal);
        // TAG RECOGNIZED:
        if (recognized)
        {// If it involves ADDING TEXT, do it here, not in the next section:
          if (dicVal >= 10000)
          { ss = DecodeTextInsertion(prefix, suffix);
            if (ss != "") dump.Append(ss);
          }
         // If it does NOT involve ADDING TEXT, then store the locn and process the tag in the next section:
          else
          { if (dicVal >= 1500 && dicVal <= 1511) // Reframe superscript etc. as font markup tags
            { prefix = "font";  suffix = "same,^,";
              if (dicVal == 1500) suffix += '1'; // superscript
              else if (dicVal == 1510) suffix += "-1"; // superscript
              else suffix += '0'; // 1501, 1511: both cancel both super and subscript.
              // It will now be dealt with under fonts (1200, 1500, 1510)
            }

          // Store the tag details
            if (dicVal != 10) // we don't add in NULL tags (they are simply removed from the text)
            { bitter.Add(new Quid(dicVal, dump.Length + buffOffset, true, suffix)); }
          }
        }
        else // unrecognized tag, so simply pack everything into the dump:
        { for (int i = tagOpenerAt; i <= ptr; i++) dump.Append(sbIn[i]);
        }
        dumpStartPtr = ptr+1;
        tagOpenerAt = -1; // no need to clear prefix etc., as they are reset at the next '<'.
      }
      else if (tagOpenerAt >= 0)
      { if (ch == ' ')
        { if (spaceFound && keepSpaces) suffix += ch; // otherwise the space is not put into dump.
          spaceFound = true; // later spaces will be cleaned off, unless inside quote marks.
        }
        else if (ch == '\"') keepSpaces = !keepSpaces;
        else if (spaceFound) suffix += ch;
        else prefix += ch;
      }
    }
    // Add on any undumped material at the end:
    for (int i = dumpStartPtr; i < inLen; i++) dump.Append(sbIn[i]);

  // PUT THE UNFORMATTED but markup-tag-free TEXT INTO THE BUFFER:     ####### ALLOW FOR APPENDING
    string outText = dump.ToString().Replace(GreaterThanChr, '>'); // Undo the replacement at the start (orig. "\<" now --> literal "<". etc.)
    inserttextlen = outText.Length;
    outText = outText.Replace(DoubleQuoteChr, '\"');
    if (clearBuff) Buff.Text = outText.Replace(LessThanChr, '<');
    else // Leave buffer text as is, and just add this new text into it at the desired position:
    { Buff.PlaceCursor(PutItHere);
      Buff.InsertAtCursor(outText.Replace(LessThanChr, '<') );
    }
  // ------------------------------------------------------------
  // APPLY TEXT TAGS
    int code;
    bool success;
    Quid[] Twitter = bitter.ToArray();  int twitLen = Twitter.Length;
    for (int twit = 0; twit < twitLen; twit++)
    { code = Twitter[twit].IX; // the code of the markup tag
      if (code <= 0) continue; // Earlier tags negate their later closing tags by making them negative.
      // FONT:
      else if (code >= 1200 && code <= 1511)
      { string tagName;
        FontLog ThisFontData = FontLog.Copy(LatestFontData);
        if (MarkUpTagNames.TryGetValue(code, out tagName)) // should never fail.
        // PARSE THE FONT MARKUP TAG:
        { bool isSuperSubScript = false;
          string[] stroo = Twitter[twit].S.Split(new char[] {','}); // all that comes after 'font'.
          int strooLen = stroo.Length;   if (strooLen < 2) continue; // faulty font tag, so ignore it.
          int strooFamLen = strooLen-1; // No. of items in stroo which are font names.
          if (stroo[strooLen-2]._IndexOfNoneOf("0123456789?^") == -1) strooFamLen--;
          // Develop the Family field:
          ss = (String.Join(",", stroo, 0, strooFamLen).Replace('_', ' ') );
          if (ss.ToLower() != "same") ThisFontData.Family = ss;
          // Deal with cases where no level is supplied:
          if (strooFamLen == strooLen - 1)
          { ss = stroo[strooLen-1];
            if (ss != "?") // if ss IS "?", then only Family and FamSize change; the first was done above, the second comes later.
            { int n = ss._ParseInt(out success);
              if (!success) continue; // faulty font tag, so ignore it.
              // We have a specific font size, which --> PointsEven; but level info. from the prior font will decide Points.
              ThisFontData.PointsEven = n;
              FontLog.AdjustForLevelType(ref ThisFontData, ThisFontData.LevelType); // This sets Points.
            }
          }
          else // there is both a font size field and a level field:
          { int n = stroo[strooLen-1]._ParseInt(out success);
            if (!success) continue; // Where there is a level field, it MUST be an integer.
            ss = stroo[strooLen-2]; // the font size field
            if (ss == "?") // then we need do nothing but change the level data. If the prior font was in super- or subscript,
                           // the size that will be copied is PointsEven, not Points.
            { ThisFontData.LevelType = '-';  ThisFontData.Level = n;
              ThisFontData.Points = ThisFontData.PointsEven;
            }
            else if (ss == "^") // a super-/subscript is being applied:
            { isSuperSubScript = true;
              char c = '-';  if (n > 0) c = '^'; else if (n < 0) c = 'v';
              if (c != '-')
              { FontLog.AdjustForLevelType(ref ThisFontData, c);  }
            }
            else // Has to have a numerically explicit font size:
            { int p = ss._ParseInt(out success);
              if (!success) continue; // Where there is a level field, it MUST be an integer.
              ThisFontData.LevelType = '-';  ThisFontData.Level = n;
              ThisFontData.Points = ThisFontData.PointsEven = p;
            }
          }
          // We can now finally set FamSize:
          ThisFontData.FamSize = ThisFontData.Family + ' ' + ThisFontData.Points.ToString();
          // Build a tag name for it:
          tagName = SuitableTagName(ThisFontData);
          // See if the buffer tag table already has a tag of this name:
          TextTag diddle = Buff.TagTable.Lookup(tagName);
          if (diddle == null)
          { diddle = new TextTag(tagName);
            BuildFontDataIntoTag(ThisFontData, ref diddle);
            Buff.TagTable.Add(diddle);
          }
          if (isSuperSubScript) TagToNextTerminatorOrEnd(Buff, ref Twitter, twit, 1500, 1510, diddle);
          else TagToSameOrEnd(Buff, ref Twitter, twit, diddle);
          LatestFontData = FontLog.Copy(ThisFontData);
        }
      }
     // STRICTLY PAIRED TAGS
      else if (code >= 100 && code < 500) // then a paired tag which does not alter text:
      { TagIfCloserFound(Buff, ref Twitter, twit);
      }
      else if (code == 1000 || code == 1100) // TEXT COLOUR / HIGHLIGHT COLOUR
      {// Identify the colour:
        Gdk.Color thisClr = (Twitter[twit].S)._ParseColour(Gdk.Color.Zero, true, out success);
        if (success) // then we have to add a tag, unless it is already there:
        { string TagName;
          if (MarkUpTagNames.TryGetValue(code, out TagName))
          { string[] stroodle = thisClr._ToStringArray(); // Prefer not to use e.g. thisClr.Red.ToString(), as 'Red' etc return 4-byte nos.
            TagName += '_' + stroodle[0] + stroodle[1] + stroodle[2];
            TextTag tiddle = Buff.TagTable.Lookup(TagName);
            if (tiddle == null)
            { tiddle = new TextTag(TagName);
              if (code == 1000) tiddle.ForegroundGdk = thisClr;  else tiddle.BackgroundGdk = thisClr;
              Buff.TagTable.Add(tiddle);
            }
            TagToSameOrEnd(Buff, ref Twitter, twit, tiddle);
          }
        }
      }
      else if (code >= 2000 && code < 5000) // unpaired or optionally paired tabs which may have a (case-insensitive) suffix
      { string TagName, twuffix = Twitter[twit].S.ToLower();
        int intArg = 0;
        if (MarkUpTagNames.TryGetValue(code, out TagName))
        { // Derive the appropriate TextTag name from the suffix stored in Twitter:
          if (code == 2000 || code == 2500) // tab stop settings / bulletted pars.
          { TagName += "_" + twuffix.GetHashCode().ToString(); // the hash code is an integer. Let's hope it is unique.
          }
          else if (code == 2100) // justification (of whatever type)
          { char c = twuffix[0];  TagName = "justification_";
            if      (c == 'f') TagName += "fill";   else if (c == 'c') TagName += "centre";
            else if (c == 'r') TagName += "right";  else               TagName += "left"; // error default is LEFT.
          }
          else if (code >= 2200 && code <= 2310) // par. indent, left and right par. margins.
          { if (twuffix == "") intArg = 20; // default for no-argument tag "<in>".
            else intArg = twuffix._ParseInt(0); // Faulty argument results in intArg of 0.
            TagName += "_";  if (intArg < 0) TagName += '_';
            TagName += intArg.ToString();
          }
          else if (code == 2300 || code == 2310) // indent of first line of paragraph
          { if (twuffix == "") intArg = 20; // default for no-argument tag "<in>".
            else intArg = twuffix._ParseInt(0); if (intArg < 0) intArg = 0; // Faulty argument results in an indent of 0.
            TagName += "_" + intArg.ToString();
          }
         // TextTag's name is set, so look it up in the TagTable and apply it.
          TextTag tiddle = Buff.TagTable.Lookup(TagName);
          success = true; // needed, as there can still be a reason to ignore this tag, if it is not in the TagTable...
         // If not found, then a new one has to be generated:
          if (tiddle == null) // can only be so where tags were not predefined (because they need an enumerating qualifier)
          { tiddle = new TextTag(TagName);
            if (code == 2000) // tab stops
            { string[] stopit = twuffix.Split(new string[] {","}, StringSplitOptions.RemoveEmptyEntries);
              Quad outcome;
              int[] stopthem = stopit._ParseIntArray(out outcome);
              if (outcome.B)
              { SetUpTabStopsInATag(ref tiddle, stopthem);
                Buff.TagTable.Add(tiddle);
              }
            }
            else if (code == 2200) // indents
            { tiddle.Indent = intArg;  Buff.TagTable.Add(tiddle); }
            else if (code == 2300) // left margin
            { tiddle.AccumulativeMargin = false; tiddle.LeftMargin = intArg;  Buff.TagTable.Add(tiddle); }
            else if (code == 2310) // right margin
            { tiddle.AccumulativeMargin = false; tiddle.RightMargin = intArg;  Buff.TagTable.Add(tiddle); }

            else success = false; // Arises where the TextTag was PREDEFINED AND ADDED TO Buff.TagTable in the
                // first section of this method, so that there is no excuse for it not being found by ".LookUp(.) above.
          }
         // APPLY THE TAG
          if (success) TagToSameOrEnd(Buff, ref Twitter, twit, tiddle);
        }
      }
      else if (code == 5000) // TextView background colour
      { Gdk.Color cool = Twitter[twit].S._ParseColour(Gdk.Color.Zero, true, out success);
        if (success) TV.ModifyBase(StateType.Normal, cool);
      }
      else if (code == 5500) // Linkage
      { if (linkery != null) // i.e. if we are expected to store links, and not just ignore them
        { // NB! We are NOT tagging the linkage text at all! We are simply recording the linkage data for calling code.
          startPtr = Twitter[twit].IY;   endPtr = -1;
          for (int i = twit+1; i < twitLen; i++)
          { if (Twitter[i].IX == 5501) { endPtr = Twitter[i].IY;  Twitter[i].IX *= -1; break; } }
          if (endPtr > startPtr) // then we have a valid '<goto ..>..</goto>' extent:
          { ss = Twitter[twit].S; string[]
            stroo = ss.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
            if (stroo.Length == 2) // valid, if there are just two nonempty comma-separated parts to the tag's suffix
            { linkery.Add(new JD.Linkage(callingHandle, startPtr, endPtr-startPtr, stroo[0], stroo[1]));
            }
          }
        }
      }
      // *** Best to keep the following code as the last to filter through Twitter, as it adds extra text
      //  to the buffer, requiring incrementing of all values of Twitter[twit+k].IY (k > 0) and in linkery.
      else if (code == 9000 || code == 9050) // Multiple settings for a single paragraph, the 2nd. being a specific bulleted paragraph.
      { string TagName, twuffix = Twitter[twit].S.ToLower();
        if (MarkUpTagNames.TryGetValue(code, out TagName))
        {
          int leftmg, rightmg, indent, pixabove, pixbelow, bullgap;
          string pellet;
          if (code == 9050) // The specific bulleted paragraph:
          { leftmg = 30;  rightmg = 0;  indent = -20;  pixabove = 5;  pixbelow = 5;  pellet = DefBullet;  bullgap = 2;
            ss = Twitter[twit].S; // if nonempty, is a suffix. Allowed: e.g. <bullet 50> and <bullet 50, #>
            if (ss != "")
            { int nn = ss.IndexOf(',');  if (nn == -1) nn = ss.Length; // the integer of the suffix
              int pp = ss._Extent(0, nn)._ParseInt(-1); if (pp >= 0) leftmg = pp;
              if (nn < ss.Length) // then there is a comma, with something (or nothing) after it, for the bullet
              { pellet = ss._Extent(nn+1);
                if (pellet == "") pellet = " "; // in case for some bizarre reason no bullet symbol is wanted, but other behaviour to be the same.
              }
            }
          }
          else // the user-designed custom paragraph:
          { string[] parity = twuffix.Split(new string[] {","}, StringSplitOptions.RemoveEmptyEntries);
            int parlen = parity.Length;
          // The following settings on the left are the system defaults.
            leftmg = DefLeftMargin;    if (parlen > 0) leftmg   = parity[0]._ParseInt(leftmg);
            rightmg = DefRightMargin;  if (parlen > 1) rightmg  = parity[1]._ParseInt(rightmg);
            indent = DefIndent;        if (parlen > 2) indent   = parity[2]._ParseInt(indent);
            pixabove = DefPixelsAbove; if (parlen > 3) pixabove = parity[3]._ParseInt(pixabove);
            pixbelow = DefPixelsBelow; if (parlen > 4) pixbelow = parity[4]._ParseInt(pixbelow);
            pellet = "";  bullgap = 0; // defaults in case of no bulletting.
            if (parlen > 5)
            { if (parity[5] == "?") pellet = DefBullet;  else pellet = parity[5];
              bullgap = DefBulletGap;    if (parlen > 6) bullgap  = parity[6]._ParseInt(bullgap);
            }
          }
        // Derive the appropriate TextTag name from the suffix stored in Twitter:
          TagName += string.Format("_{0}_{1}_{2}_{3}_{4}_{5}_{6}", leftmg, rightmg, indent, pixabove, pixbelow,
                                                    ('_' + pellet).GetHashCode(), bullgap); // '_' because pellet may be empty.
          TextTag tiddle = Buff.TagTable.Lookup(TagName);
          if (tiddle == null)
          { tiddle = new TextTag(TagName);
            tiddle.AccumulativeMargin = false; tiddle.LeftMargin = leftmg;  tiddle.RightMargin = rightmg;
            tiddle.Indent = indent;   tiddle.PixelsAboveLines = pixabove;   tiddle.PixelsBelowLines = pixbelow;
            Buff.TagTable.Add(tiddle);
          }
          // Insert the bullet before doing the formatting:
          if (pellet != "")
          { ss = pellet + ('\u00a0')._Chain(bullgap);
            int bullPtr = Twitter[twit].IY;
            TextIter bullIt = Buff.GetIterAtOffset(bullPtr);
            Buff.Insert(ref bullIt, ss);
            // Now push all elements in Twitter (beyond twit) up by the length of ss:
            int insertLen = ss.Length;
            for (int i=twit+1; i < twitLen; i++) Twitter[i].IY += insertLen;
            // and likewise massage linkery:
            if (linkery != null)
            { for (int i=0; i < linkery.Count; i++)
              { if (linkery[i].StartPtr > bullPtr)
                { JD.Linkage lulu =  linkery[i].Copy();
                  lulu.StartPtr += insertLen;
                  linkery[i] = lulu.Copy();
                }
              }
            }
          }
          // Apply the tag. Most curiously, I have found by trial and error that the tag only works if it covers
          // the '\n' of the PREVIOUS paragraph + at least the first char. of this paragraph. I therefore set the
          // extent-defining iterators accordingly, two char. positions apart.
          int n = Twitter[twit].IY-1;  if (n < 0) n = 0; // re condition: The above rule doesn't seem to apply at start of text.
          TextIter startIt = Buff.GetIterAtOffset(n); // just before the previous '\n'.
          TextIter endIt = Buff.GetIterAtOffset(n+2);  // just after the 1st. char. of the above-inserted bullet;
                                                     // or if no bullet, just after the first char. of the text.
          Buff.ApplyTag(TagName, startIt, endIt);
        }
      }
    }
    // If linkery set, attach it to JD.Linkages, but in doing so avoid duplication (which would occur if, while the
    // box remained open, the same text was repeatedly redisplayed by successive calls to this method):
    if (linkery != null && linkery.Count > 0)
    { int slink = linkery.Count, sLinky = JD.Linkages.Count;
      // If an entry in JD.Linkages is beyond buffOffset, we have to increment its start pointer by bufftextlen:
      for (int i=0; i < sLinky; i++)
      { if (JD.Linkages[i].StartPtr > buffOffset)
        { JD.Linkage clunk = JD.Linkages[i].Copy();
          clunk.StartPtr += inserttextlen;
          JD.Linkages[i] = clunk.Copy();
        }
      }
      // Don't add a new link, if it duplicates an old one (from a previous instance of the same form).
      for (int i=0; i < slink; i++)
      { if (JD.Linkages.IndexOf(linkery[i]) == -1) JD.Linkages.Add(linkery[i]);
      }
    }
  }


// ---------------------------------------------
//  METHODS DEDICATED TO 'DisplayMarkUpText(.)':
// ---------------------------------------------

// Special routine for the above method.
/// <summary>
/// This method is intended for tags which MUST be paired, and which do not have a suffix.
/// Enters pointing to either an opening tag or to an unpaired closing tag. The unpaired tag is ignored (though it
/// will not appear in the buffer text). If an opening tag, then the closer is sought; if found, the extent is tagged,
/// and the closer in Twitter is so marked that it will not be detected again later. If this is an unpaired opening tag,
/// then it will have no effect, although removed from the buffer text. (In traditional HTML, an unpaired opening tag
/// results in the operation of the tag occurring to the end of text; here, it simply does nothing)
/// </summary>
  public static void TagIfCloserFound(TextBuffer Buff, ref Quid[] Twitter, int TwitterIndex)
  { int twitLen = Twitter.Length;
    int openerCode = Twitter[TwitterIndex].IX,  closerCode = 1 + openerCode;
    if (openerCode % 2 == 1) return; // Nothing doing, if this is an orphaned closer tag.
    string TagName;
    if (!MarkUpTagNames.TryGetValue(openerCode, out TagName)) return; // highly unlikely programmer's failure.
    TextIter startIt, endIt;
    for (int i=TwitterIndex+1; i < twitLen; i++)
    { if (Twitter[i].IX == closerCode)
      { startIt = Buff.GetIterAtOffset(Twitter[TwitterIndex].IY);
        endIt   = Buff.GetIterAtOffset(Twitter[i].IY);
//TextTag tiggle = Buff.TagTable.Lookup("locktext");
//        Buff.ApplyTag(tiggle, startIt, endIt);
        Buff.ApplyTag(TagName, startIt, endIt);
        Twitter[i].IX = -closerCode; // Make the closing tag negative, so it will not again be accessed.
        break;
      }
    }
  }
/// <summary>
/// The code of the opener tag is not tested, but all subsequent tags that can negate it must have codes that
/// lie in the range LowTerminator to HighTerminator inclusive, and any closing tags must lie between LowTerminator+1
/// and HighTerminator+1.
/// </summary>
  public static void TagToNextTerminatorOrEnd(TextBuffer Buff, ref Quid[] Twitter, int TwitterIndex, int LowTerminator,
                                                                                               int HighTerminator, TextTag Tagg)
  { int twitLen = Twitter.Length;
    int thisCode = Twitter[TwitterIndex].IX;
    if (thisCode %2 == 1) return; // the entry code is an orphan negator, so ignore it.
    TextIter startIt = Buff.GetIterAtOffset(Twitter[TwitterIndex].IY),  endIt = startIt; // dummy assignment, for compiler.
    bool found = false;
    for (int i=TwitterIndex+1; i < twitLen; i++)
    { int codeine = Twitter[i].IX;
      if (codeine >= LowTerminator && codeine <= HighTerminator + 1) // i.e. inclusive of terminators
      { endIt = Buff.GetIterAtOffset(Twitter[i].IY);
        if (codeine % 2 == 1) Twitter[i].IX = -codeine; // stop this negator from ever being an entry code to this method.
        found = true;  break;
      }
    }
    if (!found) // then this tag applies to the end of text:
    { TextIter dumbo;
      Buff.GetBounds(out dumbo, out endIt);
    }
    Buff.ApplyTag(Tagg, startIt, endIt);  found = true;
  }

// Another special routine for the above method.
/// <summary>
/// TextTags using this method either do not have a closing tag or only have an optional one. The TextTag will extend
/// from the present location EITHER to that of the next tag of the SAME type OR - if a closer exists - to that closer,
/// whichever comes first. An unpaired closer is ignored, but an unpaired opener acts as if there is a virtual closer tag
/// at the end of the text.
/// </summary>
  public static void TagToSameOrEnd(TextBuffer Buff, ref Quid[] Twitter, int TwitterIndex, TextTag Tagg)
  { int twitLen = Twitter.Length;
    int openerCode = Twitter[TwitterIndex].IX,  closerCode = 1 + openerCode;
    if (openerCode % 2 == 1) return; // Nothing doing, if this is an orphaned closer tag.
    TextIter startIt = Buff.GetIterAtOffset(Twitter[TwitterIndex].IY),  endIt = startIt; // dummy assignment, for compiler.
    bool found = false;
    for (int i=TwitterIndex+1; i < twitLen; i++)
    { int codeine = Twitter[i].IX;
      if (codeine == openerCode || codeine == closerCode)
      { endIt = Buff.GetIterAtOffset(Twitter[i].IY);
        if (codeine == closerCode) Twitter[i].IX = -codeine; // Make the closing tag negative, so it will not again be accessed.
        found = true;  break;
      }
    }
    if (!found) // then this tag applies to the end of text:
    { TextIter dumbo;
      Buff.GetBounds(out dumbo, out endIt);
    }
    Buff.ApplyTag(Tagg, startIt, endIt);  found = true;
  }

/// <summary>
/// <para>TextTags using this method have an optional closing tag. The TextTag will extend from the present location
///  EITHER to the closer OF THE SAME NESTING LEVEL as the opener, or if none, to the end of text.</para>
/// <para>Note that inner nested sections will have overlapping tags, the last applied having precedence.</para>
/// </summary>
  public static void TagWithNesting(TextBuffer Buff, ref Quid[] Twitter, int TwitterIndex, TextTag Tagg, string IntroText)
  { int twitLen = Twitter.Length;
    int openerCode = Twitter[TwitterIndex].IX,  closerCode = 1 + openerCode;
    if (openerCode % 2 == 1) return; // Nothing doing, if this is an orphaned closer tag.
    TextIter startIt = Buff.GetIterAtOffset(Twitter[TwitterIndex].IY),
             endIt   = Buff.EndIter; // default, if no closer of the same level found.
    int nestlevel = 1;
    for (int i=TwitterIndex+1; i < twitLen; i++)
    { int codeine = Twitter[i].IX;
      if (codeine == openerCode) nestlevel++;
      else if (codeine == closerCode)
      { nestlevel--;
        if (nestlevel == 0)
        { endIt = Buff.GetIterAtOffset(Twitter[i].IY);
          Twitter[i].IX = -codeine; // Make the closing tag negative, so it will not again be accessed.
          break;
        }
      }
    }
    Buff.ApplyTag(Tagg, startIt, endIt);
    if (IntroText != "")
    { Buff.Insert(ref startIt, IntroText);
      // Sadly, because we have added text we will now have to adjust all the unaccessed members of Twitter:
      for (int i = TwitterIndex+1; i < twitLen; i++) { Twitter[i].IY += IntroText.Length; }
    }
  }

  public static string DecodeTextInsertion(string prefix, string suffix)
  { string result = "";
    if      (prefix == @"\n") result = "\n";
    else if (prefix == @"\t") result = "\t";
    else if (prefix == "|") // Greek letter
    { int n = GreekCodings.IndexOf(suffix);
      if (n >= 0) result = GreekSymbols.Substring(n, 1);
    }
    else if (prefix == "*") // Maths symbol
    { suffix = suffix.Replace(GreaterThanChr, '>');    suffix = suffix.Replace(LessThanChr, '<');
      int n = MathCodings.IndexOf(suffix);
      if (n >= 0) result = MathSymbols.Substring(n, 1); // *** Change, if a code can evoke a string of length >= 2.
    }
    else if (prefix == "&")
    { bool success;
      int n = suffix._ParseInt(out success); // picks up the decimal suffixes
      if (!success) n = suffix._ParseHex(out success); // picks up the hex suffixes
      if (success && n >= 0 && n <= 0xffff) // then we have a unicodable value:
      { result = Convert.ToChar(n).ToString(); }
      else result = "[?]";
    }
    return result;
  }

//=============================================
//    MISCELLANEOUS
//---------------------------------------------

/// <summary>
/// This function comes with the big proviso that it was built from my particular UK-keyboard laptop, and may very well not be general.
/// It certainly would not exactly apply to anything but a UK keyboard layout. Use at your own risk!
/// Printable keys return as is; control keys of various sorts are named; other keys are returned as '#' + the unicode value.
/// If no key has been pressed, the return is "null".
/// </summary>
  public static string KeyDenomination(int KeyValue)
  {
    if (KeyValue > 32 && KeyValue < 128) return Convert.ToChar(KeyValue).ToString();
    switch (KeyValue)
    { case 0:     return "null";
      case 32:    return "Space";
      case 163:   return "£"; // The only symbol on my keyboard which isn't in the range below 128.
      case 65027: return "AltGr";
      case 65056: return "ShiftTab";
      case 65288: return "Backspace";
      case 65289: return "Tab"; // Cntrl Tab and Alt Tab give the same result.
      case 65293: return "Enter";
      case 65307: return "Escape";
      case 65360: case 65429: return "Home"; // The second values for the following set are all for number pad duplicate keys.
      case 65361: case 65430: return "Left";
      case 65362: case 65431: return "Up";
      case 65363: case 65432: return "Right";
      case 65364: case 65433: return "Down";
      case 65365: case 65434: return "PageUp";
      case 65366: case 65435: return "PageDown";
      case 65367: case 65436: return "End";
      case 65379: case 65438: return "Insert";
      case 65505: case 65506: return "Shift";
      case 65507: case 65508: return "Cntrl";
      case 65509: return "CapsLock";
      case 65511: case 65512: case 65513: case 65514: return "Alt";
      case 65515: return "Windows";
      case 65535: case 65439: return "Delete";
      case 65407: return "NumLock";
      case 65437: return "Pad Centre"; // the '5' key of the pad when Numbers Lock is not on.
      case 65421: return "Pad Enter";
      case 65450: return "Pad *";
      case 65451: return "Pad +";
      case 65453: return "Pad -";
      case 65455: return "Pad /";
      default:
      { if      (KeyValue >= 65470 && KeyValue <= 65481) return "F" + (KeyValue - 65469).ToString();
        else if (KeyValue >= 65456 && KeyValue <= 65465) return "Pad " + (KeyValue - 65456).ToString();
        return "#" + KeyValue.ToString();
      }
    }
  }



} // END OF CLASS JTV

} // END OF NAMESPACE JLib

//ꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏ
//              MARKUP FORMATTING RULES
//ꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏꝏ
/*
Note that ONLY the first word in a tag - the keyword - is guaranteed to be case-insensitive (excluding '\n', '\t').

SUBSTITUTIONS THROUGHOUT TEXT:
  Substitution statements should come at the start of text (though they would be correctly handled if found elsewhere).
  The format:  "#VAR [target substring] [replacement substring]\n", which should constitute ONE SINGLE AND COMPLETE PARAGRAPH.
    (In actual fact it would work if '#' were not the start of the paragraph, but the final paragraph mark is essential.)
    Note that both substrings are case-sensitive; that the two delimiters are spaces; that any space after the second
    delimiting space will be taken as being part of the replacement substring.
  E.g. suppose you have a long list of font families which you want to affix to every font tag (as you expect your
    text to be used on different machines and operating systems, perhaps). Your line could be:
    "#VAR %fonts Comic Sans MS, Arial, Verdana, Sans\n"; then your font tags can be like this: "<font %fonts, 12>".
    Or you are writing a maths text in which π keeps popping up. You could write "<| p>" wherever you intend π (see
    at end of these notes); but it would look nicer if you wrote PI everywhere instead. Your line would be:
    "#VAR PI π\n", OR "#VAR PI <| p>" (since substitution tags - unlike all other tags - can be used in substitution lines).
  Warning! replacement is done uncritically at the start of parsing; so if, in the last example, you had some heading
    "PIVOTAL CONDENSATION", it would become "πVOTAL CONDENSATION". If any such risk, use a safe prefix like '%'.

NULL TAG: "<!! AnyText>" - has no effect, and is simply removed from the text. Its purpose is to be a bookmark for searching
          purposes in the raw markup text, e.g. to facilitate periodic updating of the text at specific points.

PAIRED TAGS THAT DO NOT ALTER TEXT: (Unpaired tags are removed from the text, but have no effect, as e.g. in "A<B>B" or "A</B>B".)
  BOLD             <b> ... </b>
  ITALIC           <i> ... </i>
  UNDERLINE        <u> ... </u>
  DOUBLE UNDERLINE <uu> ... </uu>
  STRIKETHROUGH    <x> ... </x>
  LOCK TEXT        <lock> ... </lock>

TAB STOPS: "<stops 50, 100, 200, 220> ... <stops 20, 40, 60, 80>"  OR  "<stops 50, 100, 200, 220> ... </stops>".
    Tab stops apply till (a) a new setting of stops; or (b) the closer tag; or (c) to the end of text.

PARAGRAPH JUSTIFICATION: "<just yak>" where only the first letter of 'yak' (case-insensitive) is checked, and must be one of:
    'l'(eft - the default, so only useful to negate another justification), 'c'(entre), 'r'(ight), 'f'(ill - both margins
    flush). Setting applies till (a) next setting, (b) "</just>", or (c) end of text. See notes below re POSITIONING TAGS...

PARAGRAPH INDENTING: the indenting of the FIRST LINE of the paragraph relative to the margin set for the paragraph; can be
  negative (for hanging paragraphs). Formats:
  <in 30> ... </in>  OR <in 30> ... <in 20>  OR <in 30> ... [applies to end of text]. (The no. is a pixel count.)
  <in> ... </in> OR etc. - "<in>" is interpreted exactly as if it were "<in 20>". (But don't expect "<in><in>" to give you 40!)
  <in 50> - sets an indent to the given absolute value (in pixels)
  <in +30> OR <in -30> - sets an indent relative to the indent (pixels) set by the last TextTag.
  <in> -  Equivalent to <in +20>, where 20 is the program's default.
  <out> - Equivalent to <in -20> likewise.
      If the absolute tab position calculated for the tag would be < 0 it is corrected to 0 (e.g. if two '<out>'s follow
      a single '<in>').
   See notes below re POSITIONING TAGS...

PARAGRAPH MARGIN SETTING:
  LEFT MARGIN: <left 30>, negated by a new left margin setting or else applies to end of text. There is no closer tag.
  RIGHT MARGIN: <right 30>, negated as above. Note that left and right margins are distinguished by the sign of the value;
      they are absolute pixel values relative to the TextView's margin, not to the prior tag's margin.
      As with Indenting, you can use either without an argument: "<left>", "<right>"; the default is the same - 20 pixels.
   See notes below re POSITIONING TAGS...

POSITIONING TAGS THAT AFFECT EXTENTS OF PARAGRAPHS, as above: The rules are as follows. For 'closer' tag, read also any
    tag which is an opener of the same type: e.g. the second of "<in 30> ... <in 20>".
  (1) The opener must be the first entity in the first par. to which it is to apply, OR the last in the previous
     par. (In fact, it can be anywhere in the previous par. beyond the first char; but that would be a bit daft...)
  (2) The closer must be either at the end of the last par. to which it is to apply or at the start of the first par.
     beyond that. (In fact it can be anywhere beyond the first char. of the last par. to which it is to apply, or
     beyond the first after the opener if it is in that par.; but that would be a bit daft...)
  (3) Here is a reasonable strategy. Suppose that A and C are sequences of unformatted pars., and B is a sequence of
     paragraphs (b1, b2, ... bN) sandwiched between them, to which the formatting is to apply. Then (a) if A,B,C are to
     be separated by empty pars., put the opener and closer tags in the empty pars. before and after B.
     (b) If no such separating empty pars., put the opener at the start of b1 and the closer at the end of bN.

PARAGRAPH - MULTIPLE SETTINGS IN ONE TAB, applying to JUST THIS PARAGRAPH:
  Unlike the above settings, this tag has to be reapplied to the beginning of each paragraph for which it will apply;
    consequently there is no closer tag, as the first instance of '\n' is in effect the closer tag.
  This system would usually be used in connection with a "#VAR" definition, as the tag is verbose, though powerful. Its
    main projected use would be in bulletting, or in quotation paragraphs.
  Format: <par [left margin], [right margin], [1st. line indent], [pixels above], [pixels below],
         [bullet (one or more chars.)], [no. nonbreaking spaces after bullet]>
    Defaults apply to any args. left off the end (so that e.g. the argumentless '<par>' outwardly does nothing, as it just
     internally resets the existing settings). If you want defaults for any entries earlier than the last entry, use a '?'.
     (This prevents you from using a single '?' as a bullet, but why would you?)
  For bulletting, you might well insert the defn. "#VAR bull par 30, 0, -20, 5, 5, ?, 2\n" at the top of the text, and then
    introduce each paragraph to be so bulletted with the tag "<bull>".
  There is a CUSTOM BULLETING TAG, "<bullet>", equivalent indeed to "<par 30, 0, -20, 5, 5, ?, 2>" as above. Take it or leave it.

  POSITIONING: The only safe place to put this tag is the intuitively right place: at the very beginning of the paragraph.

COLOURS:
  TEXT COLOUR: <# 0xRRGGBB> ... <# 0xRRGGBB>  OR <# 0xRRGGBB> ... </#>; i.e. an optional closing tag.

  HIGHLIGHT COLOUR: <~ 0xRRGGBB> ... <~ 0xRRGGBB>  OR <~ 0xRRGGBB> ... </~>.
     For both there are other valid formats: (1) a name recognized by .NET (see Microsoft stuff, or the file "Colour Names.doc"
     in the C# documentation file of the computer used to write this code. Or just try your luck. (2) Three bytes -
     "<# 255,0,128>";  and (3) one integer - "<# -234567>".  (Other formats: see Gdk.Color extension '_ParseColour(.)' in JExtensions.cs for details.)

  BACK COLOUR FOR THE WHOLE TEXT VIEW:  <bgdclr 0xRRGGBB> (or other formats as above). Should only be set once, preferably
      at the start of text; but if multiple, the last one wins.

FONT CHANGES:
  FONT: <font Courier_New, 12> ... <font Arial, 14>  OR  <font Courier_New, 12> ... </font>
        Note: (1) Use underlines where there are spaces in the font name (as spaces are parsed out); OR put the name(s)
          with spaces into double quotes: <font "Comic Sans MS", "Courier New", Arial 12>.
          (2) There MUST be a name. If you just want the existing font with a different size, use <font same, 12> (where
          'same' is a keyword). (3) There MUST be a size (except in the closer tag), or the whole statement is ignored.
        You can also list several fonts (in order of preference), separated by commas: <font myfont1, myfont2, myfont3, 10>.
        You can also specify a level value (in pts.), the height or depth (neg. value) of the char. above the base line.
        Do this by appending it to the font size, after another comma: <font myfont1, myfont2, 6, -3>. (The superscript and
          subscript tags are actually translated internally into virtual instances of such.)
  FONT SIZE: Same, but use the keyword 'same' to mean "Don't change the font, only the size +/- level": "<font same, 6, -3>".
  CHANGE FONT, USE SAME SIZE: Substitute '?' for the size: "<font Arial, ?>" or (for level change), "<font Arial, ?, 5>".
  NB! Within a segment of altered font, BOLD and ITALICS will not work. There may be a
        workaround for this, but I have not had time to check it out.

  SUPER / SUBSCRIPTING: Superscript = "<^>", subcript = "<V>". Each is cancelled by either of </^> and </v>. Note that under
       the bonnet, the segment of text involved is being assigned a new font (same font family, but with different parameter settings).
       Hence, as for other segments assigned a different font, BOLD and ITALICS will not work within that segment.

TEXT SUBSTITIONS: (The substitution occurs once only, the whole markup tag being replaced by the substituted text)
  LITERAL '<'   \<    (Distinguishes a literal '<' from a tag opener.)
  LITERAL '>'   \>    (Distinguishes a literal '>' from a tag closer.)
  LITERAL '"'   \"    (Distinguishes a literal double-quotes mark; only needed within specialized tags (e.g. <VAR ..>.)
    Warning: If, in setting the input to the TextView, you were writing a C# literal string "Buff.Text = "..." including
      these characters - rather than simply importing say a disk file - then you would have to code "..\<..." as "..\\<..",
      and "..\".." as "..\\\"..".

  LINEFEED      \n  (this one is CASE-SENSITIVE)
  TAB           \t  (this one is CASE-SENSITIVE)
  GREEK LETTER  <| a>, where 'a' is any of the following code characters: (NB - note the space between '|' and 'a')
              Code char.:   a b g d e z @ 0 i k l m n x o p r s c t u f h y w  A B G D E Z # 9 I K L M N X O P R S T U F H Y W
              Substitution: α β γ δ ε ζ η θ ι κ λ μ ν ξ ο π ρ σ ς τ υ φ χ ψ ω  Α Β Γ Δ Ε Ζ Η Θ Ι Κ Λ Μ Ν Ξ Ο Π Ρ Σ Τ Υ Φ Χ Ψ Ω
  MATHS SYMBOL  <* i>, where 'i' is any of the following code characters: (NB - note the space between '*' and 'i')
              Code char.:   i j S / + o x ( ) { } - |  R F d D 8 A ~ # = < >
              Substitution: ∫ ∮ ∑ √ ± ° × ‹ › ≪ ≫ ‒  ‖ ℛ ℱ  ∂ ∇ ∞ ∡ ≈ ≠ ≡ ≤ ≥
  UNICODE CHAR <& 0x41>  OR <& 65> (both examples producing the char. 'A'). Note the space after '&'.

LINKAGES:
  Format: <goto [space] [address] , [bookmark]>, where 'bookmark' in the targetted text must be stored as "<? bookmark>"
  Example: '<goto "/home/fred/Documents/Blah.txt", foodle>Click here for info on 'How to Foodle'</goto>', which would
              be targetting the tag "<? foodle>" in the indicated text.
      'address' has two special settings:
          -- (1) "buffer", intended to indicate to calling code that the target is in the same TextBuffer text,
                so that what is required is scrolling to it within the same instance of the box;
          -- (2) "text", intended to indicate to calling code that the target is in the same file (stored in memory),
                but not nec. in the TextBuffer at present; an overlapping box might be used to display it.
          -- (3) anything else is taken as a path and file name sufficient for obtaining the file; it is not in any
                way checked in DisplayMarkUpText(.), so calling code had better have ways of dealing with bad addresses.
                REMEMBER TO USE double quote marks to preserve spaces in a file name: <goto "/home/../hello there.txt", foo>.
  The closer tag is vital; if absent, then the opener tag does nothing.
  NB! Linkage offsets for the <goto..>..</goto> segment are set in stone by DisplayMarkUpText(.); if the calling box's
    TextView allows editing to happen, and the user inserts text before the link, then these offsets will be out of synch.
    Either the calling box doesn't allow editing or else it has to adjust the linkage offsets for every change of text.
  USING OTHER TAGS WITH LINKAGE TEXT: This is fine - e.g. to underline and change the colour of the visible text; but
    the vital rule is - PUT ALL OTHER TAGS OUTSIDE OF THE "" REGION. Nothing but visible text must lie between 'goto' tags.
    Example of proper use:   "<# blue><u><goto ..>Click here!</goto></u><# black>"
    If you put them inside, the pointers to the tagged region will be somewhat displaced from what they should be.
    Exception: special char. tags (like "<& 00A0>" or "<| p>"), which may be part of the visible text segment.







*/
