using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Gdk;

namespace JLib
{ // A library of utilities for widescale use in programs written by me. Spread over several files,
  //  each containing a single "sublibrary".
  /// <summary>
  /// Fies are B (boolean) and Tag (object). In a typical usage for error announcements, if B is TRUE then Tag is left at the default NULL;
  /// if FALSE, then Tag is set to some object containing error data.
  /// </summary>
  public struct BoolTag
  { public bool B;  public object Tag;
    public BoolTag(bool b) { B = b;  Tag = null; }
    public BoolTag(bool b, object obj) { B = b;  Tag = obj; }
    public override string ToString(){ return string.Format ("{0}, '{1}'", B, Tag); }
  }
  public struct Boost
  { public bool B;   public string S;  
    public Boost(bool b) { B = b;  S = ""; }
    public Boost(bool b, string s) { B = b;  S = s; }
    public override string ToString(){ return string.Format ("{0}, '{1}'", B, S); }
  }
/// <summary>
/// Fields:  int I, string S. Constructor args: (), or (I, S).
/// </summary>
  public struct Strint
  { public int I;   public string S;  
    public Strint(int Int, string Str) { I = Int;  S = Str; }
    public override string ToString(){ return string.Format ("{0}, '{1}'", I, S); }
  }
/// <summary>
/// Fields:  int IX, IY;  string SX, SY. Constructor args: (dummy integer) --> (IX = IY = 0, SX = SY = ""); or (IX, IY, SX, SY).
/// </summary>
  public struct Strint2
  { public int IX, IY;   public string SX, SY;
    public Strint2(int dummy) { IX = 0; IY = 0;   SX = ""; SY = ""; }
    public Strint2(int IntX, int IntY, string StrX, string StrY) { IX = IntX; IY = IntY;   SX = StrX; SY = StrY; }
    public override string ToString(){return String.Format("IX: {0};  IY: {1};  SX: '{2}';  SY: '{3}'", IX, IY, SX, SY); }
    public static Strint2 Copy(Strint2 Model)
    { Strint2 result = new Strint2(0);
      result.IX = Model.IX;  result.IY = Model.IY;  result.SX = Model.SX;  result.SY = Model.SY;
      return result;
    }
  }
/// <summary>
/// Fields:  double X, string S. Constructor args: (), or (X, S).
/// </summary>
  public struct Strub
  { public double X;   public string S;
    public Strub(double Dub, string Str) { X = Dub;  S = Str; }
    public override string ToString(){return X.ToString() + ", '" + S +"'";}
  }
/// <summary>
/// Fields:  double X, Y;  string SX, SY. Constructor args: (dummy integer) --> (X = Y = 0.0, SX = SY = ""); or (X, Y, SX, SY).
/// </summary>
  public struct Strub2
  { public double X, Y;   public string SX, SY;
    public Strub2(int dummy) { X = 0.0; Y = 0.0;   SX = ""; SY = ""; }
    public Strub2(double doubleX, double doubleY, string StrX, string StrY)
    { X = doubleX; Y = doubleY;   SX = StrX; SY = StrY; }
    public override string ToString(){return String.Format("X: {0};  Y: {1};  SX: '{2}';  SY: '{3}'", X, Y, SX, SY); }
    public static Strub2 Copy(Strub2 Model)
    { Strub2 result = new Strub2(0);
      result.X = Model.X;  result.Y = Model.Y;  result.SX = Model.SX;  result.SY = Model.SY;
      return result;
    }
  }
/// <summary>
/// Fields (integers): X, Y. Constructor args: (), or (X, Y).
/// </summary>
  public struct Duo
  { public int X;  public int Y;
    public Duo(int xx, int yy) {X = xx; Y = yy;}
    public override string ToString(){return "("+X.ToString()+", "+Y.ToString()+")";}
  }
/// <summary>
/// Fields (double): X, Y. Constructor args: (), or (X, Y).
/// </summary>
  public struct Duplex
  { public double X;  public double Y;
    public Duplex(double dummy) {X = 0.0; Y = 0.0;}
    public Duplex(double xx, double yy) {X = xx; Y = yy;}
    public override string ToString() { return "("+X.ToString()+", "+Y.ToString()+")";}
    public string ToString(string fmtString) { return "("+X.ToString(fmtString)+", "+Y.ToString(fmtString)+")";}
  }
/// <summary>
/// Fields (integers): X, Y, Z. Constructor args: (), or (X, Y, Z).
/// </summary>
  public struct Trio
  { public int X, Y, Z;
    public Trio(int xx, int yy, int zz) {X = xx; Y = yy; Z = zz;}
    public override string ToString()
    { return String.Format("X: {0}; Y: {1}; Z: {2}", X, Y, Z); }
  }
  public struct Triox
  { public int X, Y, Z;
    public double XX;
    public Triox(int x, int y, int z, double xx) {X = x; Y = y; Z = z; XX = xx; }
    public override string ToString()
    { return String.Format("X: {0}; Y: {1}; Z: {2}; XX: {3}", X, Y, Z, XX); }
  }
  public struct Triplex
  { public double X, Y, Z;
    public Triplex(double xx, double yy, double zz) { X = xx; Y = yy; Z = zz; }
    public override string ToString() { return "X: "+X.ToString()+";  Y: "+Y.ToString()+";  Z: "+Z.ToString(); }
    public string ToString(string fmtString) { return "X: "+X.ToString(fmtString)+";  Y: "+Y.ToString(fmtString)+";  Z: "+Z.ToString(fmtString); }  
  }
  public struct Trilobite
  { public byte X, Y, Z;
    public Trilobite( byte x, byte y, byte z) { X = x; Y = y; Z = z; }
    public Trilobite( int x, int y, int z) { X = Convert.ToByte(x); Y = Convert.ToByte(y); Z = Convert.ToByte(z); }
    public Trilobite( Trio tree) { X = Convert.ToByte(tree.X); Y = Convert.ToByte(tree.Y); Z = Convert.ToByte(tree.Z); }
    public override string ToString() { return "X: "+X.ToString()+";  Y: "+Y.ToString()+";  Z: "+Z.ToString(); }
  }



  /// <summary> Fields: integers X1, X2, X3, X4. Constructors: (1) empty, (2) Tetro(x1, x2, x3, x4).</summary>
  public struct Tetro
  { public int X1, X2, X3, X4;
    public Tetro(int x1, int x2, int x3, int x4) {X1 = x1; X2 = x2; X3 = x3; X4 = x4; }
    public override string ToString()
    { return String.Format("X1: {0}; X2: {1}; X3: {2}; X4: {3}", X1, X2, X3, X4); }
  }
/// <summary> Fields: doubles .X1,  .X2,  .X3,  .X4. </summary>
  public struct Tetrex
  { public double X1, X2, X3, X4;
    public Tetrex(double x1, double x2, double x3, double x4) {X1 = x1; X2 = x2; X3 = x3; X4 = x4; }
    public override string ToString()
    { return String.Format("X1: {0}; X2: {1}; X3: {2}; X4: {3}", X1, X2, X3, X4); }
  }
/// <summary> Fields: integer .I, double .X. </summary>
  public struct PairIX
  { public int I;  public double X;
    public PairIX(int ii, double xx) {I = ii; X = xx;}
    public override string ToString() { return "I: "+I.ToString()+", X: "+X.ToString();}
    public string ToString(string fmtString) { return "I: "+I.ToString()+", X: "+X.ToString(fmtString);}
  }
  public struct TRect // Height and Width can't be set from outside. If using this to store screen coords, then 'Tp' will be
  // smaller than 'Bm'. The only consequence is that .H will return as negative, as will Clo.Y and Chi.Y. Simply change the sign.
  // 
  { private int Lt; private int Tp; private int Rt; private int Bm;
    private int Ht; private int Wd; // NB: If adding methods, make sure Ht and Wd
                                    // are always automatically updated.
    public TRect(int dummy){Lt=0; Tp=0; Rt=0; Bm=0; Ht=0; Wd=0;}
    public TRect(int left, int top, int right, int btm)
    {Lt=left; Tp=top; Rt=right;  Bm=btm; Ht = Tp-Bm;  Wd = Rt-Lt; }
    public int L {get {return Lt;}  set {Lt = value; Wd = Rt-Lt;} }
    public int T {get {return Tp;}  set {Tp = value; Ht = Tp-Bm;} }
    public int R {get {return Rt;}  set {Rt = value; Wd = Rt-Lt;} }
    public int B {get {return Bm;}  set {Bm = value; Ht = Tp-Bm;} }
    public int H {get {return Ht; } }
    public int W {get {return Wd; } }
    public int Max { get { int max = Lt; if (Rt > max) max = Rt; if (Tp > max) max = Tp; if (Bm > max) max = Bm; return max; } }
    public int Min { get { int min = Lt; if (Rt < min) min = Rt; if (Tp < min) min = Tp; if (Bm < min) min = Bm; return min; } }
    // Centre: Where Ht is even, Clo puts centre closer to Bm, but Chi puts it
      //  towards Tp. (Wd even: centre always goes towards Lt.)
    public Duo Clo {get{Duo result;
            result.X = Rt - Wd/2; result.Y = Tp - Ht/2; return result; }}
    public Duo Chi {get{Duo result;
            result.X = Rt - Wd/2; result.Y = Bm + Ht/2; return result; }}
    public override string ToString()
    { return String.Format("L: {0}; T: {1}; R: {2}; B: {3}; W: {4}; H: {5}", Lt, Tp, Rt, Bm, Wd, Ht); }
  }
  public struct TDRect // Height and Width can't be set from outside.
  { private double Lt; private double Tp; private double Rt; private double Bm;
    private double Ht; private double Wd;// NB: If adding methods, make sure Ht
                                    // and Wd are always automatically updated.
    public TDRect(int dummy){Lt=0.0; Tp=0.0; Rt=0.0; Bm=0.0; Ht=0.0; Wd=0.0;}
    public TDRect(double left, double top, double right, double btm)
    {Tp=top; Lt=left; Bm=btm; Rt=right;  Ht = Tp-Bm;  Wd = Rt-Lt; }
    public double L {get {return Lt;}  set {Lt = value; Wd = Rt-Lt;} }
    public double T {get {return Tp;}  set {Tp = value; Ht = Tp-Bm;} }
    public double R {get {return Rt;}  set {Rt = value; Wd = Rt-Lt;} }
    public double B {get {return Bm;}  set {Bm = value; Ht = Tp-Bm;} }
    public double H {get {return Ht; } }
    public double W {get {return Wd; } }
    public Duplex C {get{Duplex result;
            result.X = Lt + Wd/2; result.Y = Bm + Ht/2; return result; }}
    public double Max { get { double max = Lt; if (Rt > max) max = Rt; if (Tp > max) max = Tp; if (Bm > max) max = Bm; return max; } }
    public double Min { get { double min = Lt; if (Rt < min) min = Rt; if (Tp < min) min = Tp; if (Bm < min) min = Bm; return min; } }
    public override string ToString()
    { return "L: "+Lt.ToString()+"; T: "+Tp.ToString()+"; R: "+Rt.ToString()+
           "; B: "+Bm.ToString()+"; W: "+Wd.ToString()+"; H: "+Ht.ToString();
    }
    public string ToString(string fmtString)
    { return "L: "+Lt.ToString(fmtString)+"; T: "+Tp.ToString(fmtString)+"; R: "+Rt.ToString(fmtString)+
           "; B: "+Bm.ToString(fmtString)+"; W: "+Wd.ToString(fmtString)+"; H: "+Ht.ToString(fmtString);
    }
  }
  public struct Quad
  { public int I;  public double X;  public bool B;  public string S;
    public Quad(bool b) { I = 0; X = 0.0; B = b; S = ""; } // use in place of default (empty) constructor if you want S not to be set to NULL.
    public Quad(int i, double x, bool b, string s) { I = i; X = x; B = b; S = s; }
    public override string ToString()
    { return "I: " + I.ToString() + "; X: " + X.ToString() + "; B: " + B.ToString() + "; S: '" + S + "'"; }
      public string ToString(string fmtString)
    { return "I: " + I.ToString() + "; X: " + X.ToString(fmtString) + "; B: " + B.ToString() + "; S: '" + S + "'"; }
  }
/// <summary>Fields: integers .IX, .IY; bool .B;  string .S.</summary>
  public struct Quid
  { public int IX, IY;  public bool B;  public string S;
    public Quid(bool b) { IX = 0; IY = 0; B = b; S = ""; } // use in place of default (empty) constructor if you want S not to be set to NULL.
    public Quid(int ix, int iy, bool b, string s) { IX = ix; IY = iy; B = b; S = s; }
    public override string ToString()
    { return String.Format("IX: {0}; IY: {1}; B: {2}; S: '{3}'",  IX, IY, B, S); }
  }
/// <summary>Fields: integers .IX, .IY; doubles .XX, .XY; bools .BX, .BY; strings .SX, .SY.</summary>
  public struct Octet
  { public int IX;   public double XX;  public bool BX;  public string SX;
    public int IY;   public double XY;  public bool BY;  public string SY;
    public Octet(int Dummy) // Use in place of the default (empty) constructor where it is nec. to avoid strings set to NULL
    { IX=0; IY=0; XX=0.0; XY=0.0; BX = false; BY = false; SX = ""; SY = ""; }
    public Octet(int i, double x, bool b, string s) { IX=i; IY=i; XX=x; XY=x; BX = b; BY = b; SX = s; SY = s; }
    public Octet(int ix, double xx, bool bx, string sx, int iy, double xy, bool by, string sy)
    { IX=ix; IY=iy; XX=xx; XY=xy; BX = bx; BY = by; SX = sx; SY = sy; }
    public override string ToString()
    { string ss = "IX: " + IX.ToString() + "; XX: " + XX.ToString() + "; BX: " + BX.ToString() + "; SX: '" + SX + "'\n";
            ss += "IY: " + IY.ToString() + "; XY: " + XY.ToString() + "; BY: " + BY.ToString() + "; SY: '" + SY + "'";
      return ss;
    }
    public string ToString(string fmtString)
    { string ss = "IX: " + IX.ToString() + "; XX: " + XX.ToString(fmtString) + "; BX: " + BX.ToString() + "; SX: '" + SX + "'\n";
            ss += "IY: " + IY.ToString() + "; XY: " + XY.ToString(fmtString) + "; BY: " + BY.ToString() + "; SY: '" + SY + "'";
      return ss;
    }
  }
  public struct Twelver
  { public int IX;   public double XX;  public bool BX;  public string SX;
    public int IY;   public double XY;  public bool BY;  public string SY;
    public int IZ;   public double XZ;  public bool BZ;  public string SZ;
    public Twelver(int Dummy) // nec. in place of default (empty) constructor, if you want to avoid strings being set to NULL
    { IX=0; IY=0; IZ=0; XX=0.0; XY=0.0; XZ=0.0; BX=false; BY=false; BZ=false;
      SX = ""; SY = ""; SZ = ""; }
    public Twelver(int i, double x, bool b, string s) 
    { IX = i; IY = i; IZ = i; XX = x; XY = x; XZ = x; BX = b; BY = b; BZ = b; SX = s; SY = s; SZ = s; }
    public Twelver(int ix, double xx, bool bx, string sx, int iy, double xy, bool by, string sy, int iz, double xz, bool bz, string sz) 
    { IX = ix; IY = iy; IZ = iz; XX = xx; XY = xy; XZ = xz; BX = bx; BY = by; BZ = bz; SX = sx; SY = sy; SZ = sz; }
    public override string ToString()
    { string ss = "IX: " + IX.ToString() + "; XX: " + XX.ToString() + "; BX: " + BX.ToString() + "; SX: '" + SX + "'\n";
            ss += "IY: " + IY.ToString() + "; XY: " + XY.ToString() + "; BY: " + BY.ToString() + "; SY: '" + SY + "'\n";
            ss += "IZ: " + IZ.ToString() + "; XZ: " + XZ.ToString() + "; BZ: " + BZ.ToString() + "; SZ: '" + SZ + "'";
      return ss;
    }
    public string ToString(string fmtString)
    { string ss = "IX: " + IX.ToString() + "; XX: " + XX.ToString(fmtString) + "; BX: " + BX.ToString() + "; SX: '" + SX + "'\n";
            ss += "IY: " + IY.ToString() + "; XY: " + XY.ToString(fmtString) + "; BY: " + BY.ToString() + "; SY: '" + SY + "'\n";
            ss += "IZ: " + IZ.ToString() + "; XZ: " + XZ.ToString(fmtString) + "; BZ: " + BZ.ToString() + "; SZ: '" + SZ + "'";
      return ss;
    }  
  }

//==========================================================//js

// General points:
// 1. No check is ever made for certain error-throwing fn. arguments which would never be
//   perpetrated by the intended user of this namespace (which is me). In particular:
//    (1) No test for a null string or null array;
//    (2) No test for integer arguments being very close to max. / min. possible values
//         (so that adding a very small no. --> overflow / underflow).
// 2. StringBuilder versions tend to have the following notation: (1) SB is a prefix ("SBSnip") if it
//    returns an SB rather than a string. (It may operate on either a string or an SB, though.)
//    (2) SB is a suffix if the return is as for the string version, but it operates on an SB.
// 3. StringBuilder arguments may be empty (but not null) just as corresp. string args may be "" (but not null).
//    (For that matter, char[] arguments can also be empty = "char[] coo = new char[0]" is valid - but not null.)
public class JS
{
// PRIVATE FIELDS
  private static DateTime jsDateTime;
  private static long baseticks;
// PUBLIC FIELDS
  public static char[] DigitList = {'0','1','2','3','4','5','6','7','8','9'}; // MUST stay in this order, as some user's code relies on it.
  public static char DecimalPoint = '.'; // A program for european use might reset this.
  public static string EngSmallLtrs = "abcdefghijklmnopqrstuvwxyz";
  public static string EngCapitals = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
  public static string EngLetters = EngSmallLtrs + EngCapitals;
  public static string Digits = "0123456789";
  public static string HexDigits = "0123456789abcdefABCDEF";
  public static string Identifier1stChar = EngLetters + "_";
  public static string IdentifierChars = Identifier1stChar + Digits;
  public static char[] WhiteSpaces = {'\u0009',   '\u000A',    '\u000B',    '\u000C',      '\u000D', '\u0020'};
                                      // tab(\t)  new line(\n)  vert tab(\v)  form feed(\f)  CR(\r)    space
  public static char TabC = '\u0009';
  public static string TabS = TabC.ToString();
  public static string CRLF = "\u000D\u000A"; // MS Windows text files terminate lines in this (Unix, Linux: just \r, = CR.)
    //Unicode reserves values between these two for one's own private use:
  public static char FirstPrivateChar = '\ue000';  public static int FirstPrivateCharNo = 0xE000;
  public static char LastPrivateChar = '\uf8ff';
  public static string UnlikelyStr = "\ue000\uf8ff"; // the above two concatenated.
  public static string MonthStrShort = "/JAN/FEB/MAR/APR/MAY/JUN/JUL/AUG/SEP/OCT/NOV/DEC/";
          // Note that the month no. (1 to 12) is (ptr + 3)/4, with no remainder. Or if the test string
          //  has '/' prefixed to it, month no. = (ptr+4)/4.
  public static string MonthStr = "/January///February//March/////April/////May///////June//////July//////August///" + 
                                  "/September/October///November//December//";
  public static int[] DaysInMonth = {0,31,28,31,30,31,30,31,31,30,31,30,31}; // for month nos. 1 to 12; for non-leap years.
  public static string MonthStrLo = MonthStr.ToLower();
          // Gap between starts of month names is 10 chars.

  // PRIVATE DUMMY CONSTRUCTOR, to prevent some galah from creating an instance of JS.
  private JS(){}

  // STATIC CONSTRUCTOR which sets fields for this class.
  static JS()
  { jsDateTime = DateTime.Now; // Will be set to 0 ticks, until a method sets it.
    baseticks = jsDateTime.Ticks;
  }

//=======================================================================
//1. BASIC STRING MANIPULATIONS
//-----------------------------------------------------------------------

// Group the chars. of the string into groups of GpSize, separated by Spacer. If the
//  string length was not a multiple of GpSize, either (1) the string will be returned
//  with the last group smaller than the rest (determined by Padder = ""), or (2) the
//  last group will be padded out to GpSize using Padder[0]. (Padder as 'char' would
//  require the user to input something like '\u0000' each time for option 1, which
//  would be a pain.)
// E.g. ("abcdefghi", 4, " - ", "0") --> "abcd - efgh - i000".
  public static string Grouped(string InStr, int GpSize, string Spacer, string Padder)
  { if (InStr == "" || Spacer == "" || GpSize < 1) return InStr;
    char padder = '#'; // Don't worry, this dummy value will never be used!
    if (Padder.Length > 0) padder = Padder[0];
    StringBuilder result = new StringBuilder();
    char[] instr = InStr.ToCharArray();
    char[] spacer = Spacer.ToCharArray();
    for (int i=0; i<instr.Length; i += GpSize)
    { if (i>0) { for (int j=0; j<spacer.Length; j++) result.Append(spacer[j]); }
      for (int j=0; j<GpSize; j++)
      { if (i+j < instr.Length) result.Append(instr[i+j]);
        else if (Padder != "") result.Append(padder);
    } }
    return result.ToString();
  }

/// <summary>
/// <para>Prepare a quote, truncated (at word breaks, if possible) if necessary. Quote marks are optionally added. </para>
/// <para>KeepAllBefore and KeepNothingAfter define the shortest and longest allowed lengths of the returned string. </para>
/// <para>BreakChars: characters in addition to the space char. which are valid for word breaks. To accept the default set 
/// "-,:;.?!", set BreakChars to "default" (case-insens.).</para>
/// <para>QuoteMark: This will be placed at both ends of the returned string (use the empty string, if none wanted).</para>
/// <para>'Etc' is what goes on the end if the string was truncated. Single char. '…' (unicode 8230 decimal) is a good one.</para>
/// <para>EXAMPLE OF USE: ShortQuote("The cat sat on the mat", 5, 10, "", "'", "...") --> " 'The cat...' ".</para>
/// <para>No errors are raised.</para>
/// </summary>
  public static string ShortQuote(string InStr, int KeepAllBefore, int KeepNothingAfter,
                    string BreakChars, string QuoteMark, string Etc)
{ if (KeepAllBefore > KeepNothingAfter || KeepNothingAfter >= InStr.Length-1
                                                    || InStr == "") return InStr;
  if (BreakChars.ToLower() == "default") BreakChars = "-,:;.?!";
  char[] instr = InStr.ToCharArray();
  int chopafter = KeepNothingAfter;
  for (int i = KeepAllBefore; i <= KeepNothingAfter; i++)
  { if (instr[i] == ' '){chopafter = i-1; break;}//keep all to, but not incl., the space.
    if (BreakChars.IndexOf(instr[i]) > -1){chopafter = i;  break;}//retain the breaker.
  }
  string ss = InStr._Extent(0, chopafter+1);
  ss = ss._Extent(0, KeepNothingAfter+1 - Etc.Length);
  ss += Etc;
  return QuoteMark + ss + QuoteMark;
}
// Demarcate all chars. in string; e.g. for Demarcator of " ,", "123" --> "1, 2, 3".
// If < 2 chars. in InStr, simply returns InStr.
  public static string DemarcateStr(string InStr, string Demarcator)
{ int inlen = InStr.Length;  if (inlen < 2) return InStr;
  char[] instr = InStr.ToCharArray();
  StringBuilder result = new StringBuilder();
  for (int i=0; i<inlen-1; i++){result.Append(instr[i]);  result.Append(Demarcator);}
  result.Append(instr[inlen-1]);
  return result.ToString();
}

//=======================================================================
//2. SEARCHES -- NOT using Regex (see final section for Regex searches)
//-----------------------------------------------------------------------
/// <summary>
/// Find the lowest and highest characters in the input string. If empty, returns (#0,#0, 'F'). Otherwise(low char, high char, 'T').
/// </summary>
  public static char[] LowestHighestChar(string InStr)
  { if (InStr == "") return new [] {'\u0000', '\u0000', 'F'}; 
    char ch, lowch = InStr[0], hich = lowch;
    for (int i=1; i < InStr.Length; i++)
    { ch = InStr[i];  if (ch < lowch) lowch = ch;  else if (ch > hich) hich = ch; }
    return new [] { lowch, hich, 'T'};
  }

// Find the first HowMany characters, starting at BaseChar, not present in InStr.
//  If 3nd. argument missing, starts at  \u0001. In the extremely unlikely
//  event that all chars. are present in the string, char.MaxValue is returned.
  public static char[] LowestAbsentChars(string InStr, int HowMany,
                                                   params char[] BaseChar)
  { char[] result = new char[HowMany];
    if (BaseChar.Length == 0) result[0] = '\u0001'; else result[0] = BaseChar[0];
    for (int i = 0; i < HowMany; i++)
    { if (i>0)
      { result[i] = result[i-1]; if (result[i]< char.MaxValue) result[i]++; }
      while (result[i] < char.MaxValue)
      { if (InStr.IndexOf(result[i]) == -1) break;  result[i]++; }
    }
    return result;
  }
// Search within a window in InStr (i.e. between and incl. StartPtr and EndPtr) for
//   Target, but only record a find if a condition is satisfied. The condition is that
//   the character immediately before and the one immediately after an instance of
//   Target must NOT be in the char[] list BadNeighbours. Also, realize that if a
//   candidate occurs at either the start or the end of the window, chars. that would
//   otherwise disqualify the candidate but which are just outside the window will have
//   no effect; so make sure your window is properly chosen.
// Example: you are doing maths parsing, and need to detect the math symbol 'tan' . Your
//   parsing code is faced with "x = tan(stan + tango);". You want the first instance
//   of 'tan' to be seen as a fn. name, but the next two as parts of rather perverse
//   variable names. If BadNeighbours consists of all chars. allowed in identifiers,
//   then the 's' in 'stan' negates the second instance, and the 'g' in 'tango' negates
//   the third.
// Input params: [0] = StartPtr (default: 0); [1] = EndPtr (default: last char.; if
//   given value exceeds last char, corrected to last char.); [2] = case type (0, the
//   default, = case-sensitive search; any other value, e.g. 1, = case-insens. search.)
//   NB: If case-insensitive search, be aware that InStr and Target will be made UPPER
//   case, while BadNeighbours will be unchanged; so BadNeighbours had better have the
//   upper case version of all chars. to be detected.
// RETURNED: .X and .Y provide start and end pointers of the find (and .X is therefore
//   nonneg.). If no find, .X is -1. If an error (= silly argument), .X is <-1 (key below).
//   .Y only has meaning if there was a find; use .X for testing.  
// If either InStr or Target are empty strings, a 'no find' result is returned.

  public static Duo FindIf(string InStr, string Target, char[] BadNeighbours,
                                                   params int[] Start_End_CaseKey)
  { Duo result = new Duo(-1,-1);
    int inlen = InStr.Length, tarlen = Target.Length;   if (inlen == 0 || tarlen == 0) return result;
  // Get, and check, parameters:
    int parlen = Start_End_CaseKey.Length;  int StartPtr, EndPtr;  bool CaseSensitive;
    if (parlen == 0)StartPtr = 0; else StartPtr = Start_End_CaseKey[0];
    if (parlen <= 1)EndPtr = inlen-1;
    else {EndPtr = Start_End_CaseKey[1]; if (EndPtr >= inlen)EndPtr = inlen-1;}
    if (parlen <= 2 || Start_End_CaseKey[2] == 0) CaseSensitive = true;
    else CaseSensitive = false;
    // Get silly args. out of the way:
    if (StartPtr < 0){result.X = -2; return result;}
    else if (StartPtr > EndPtr){result.X = -3; return result;}
    // Deal with empty arg. strings:
    if (InStr == "" || Target == "") {result.X = -1; return result;}
    // Go searching..
    if (!CaseSensitive)
    { InStr = InStr.ToUpper();  Target = Target.ToUpper(); }
    int ptr = StartPtr, nextptr;
    char cleft, cright;  bool qualifies;
    result.X = -1; // default - no finds.
    while (ptr < inlen)
    { nextptr = InStr.IndexOf(Target, ptr);
      // No (or no further) finds:
      if (nextptr == -1 || nextptr+tarlen > EndPtr+1) break;
      // Found one, so check for eligibility:
      qualifies = false; // It is disqualified till proven qualified.
      if (nextptr == StartPtr) cleft = (char) 0; else cleft = InStr[nextptr-1];
      if (Array.IndexOf(BadNeighbours, cleft) == -1)// then left char. does not disqualify, so
                                                     //  so check char. at right end:
      { if (nextptr + tarlen == EndPtr+1) cright = (char) 0;
        else cright = InStr[nextptr + tarlen];
        if (Array.IndexOf(BadNeighbours, cright) == -1) qualifies = true;
      }
      // Deal with InStr according to whether or not Target qualifies:
      if (qualifies)
      { result.X = nextptr;  result.Y = nextptr + tarlen - 1; break; }
      else ptr = nextptr+1; // A bummer, so restart search after that first char.
    }
    return result; 
  }
// Throughout InStr, replace substring Target with Replacement, but only if a condition
//   is satisfied. The condition is that the character immediately before and the one
//   immediately after an instance of Target must NOT be in the char[] list BadNeighbours.
// Example: you are doing maths parsing, and need to replace 'tan' with a token. Your
//   parsing code is faced with "x = tan(stan + tango);". You want the first instance
//   of 'tan' to be parsed as a fn. name, but the next two as parts of rather perverse
//   variable names. If BadNeighbours consists of all chars. allowed in identifiers,
//   then the 's' in 'stan' negates the second instance, and the 'g' in 'tango' negates
//   the third. Note: no char. before/after Target counts the same as an allowed char. there.
// BadNeighbours can be NULL (in which case this fn. replaces ALL occurrences).
// RETURNED: The string with replacements; in the out array Stats, [0] = no. replacements;
//   [1] = old string length; [2] = new string length; [3] --> start of first replacemt
//   (-1, if none); [4] --> ONE PAST last replacemt (-1 if none; string length, if last
//   replacement was at the end of the string).
// The search is ALWAYS CASE-MATCHED.
  public static string SubstituteIf(string InStr, string Target, string Replacement,
                                 char[] BadNeighbours, out int[] Stats)
  { int inlen = InStr.Length;
    Stats = new [] {0, inlen, inlen, -1, -1}; //'out' arg, so must fully initialize.
    if (InStr == "" || Target == "") return InStr;
    int lent = Target.Length;
    int ptr = 0, nextptr;  StringBuilder newstr = new StringBuilder();
    char cleft, cright;  bool qualifies;
    char[] inchar = InStr.ToCharArray();  bool isfirst = true;
    while (ptr < inlen)
    { // All of InStr up to ptr-1 has been copied to newstr already (with Target replacemt).
      nextptr = InStr.IndexOf(Target, ptr);
      // No (or no further) finds:
      if (nextptr == -1)
      { newstr.Append(InStr._Extent(ptr, inlen)); break; } // append rest of string
      // Found one, so check for eligibility:
      if (BadNeighbours == null) qualifies = true;
      else
      { qualifies = false; // It is disqualified till proven qualified.
        if (nextptr == 0) cleft = (char) 0; else cleft = inchar[nextptr-1];
        if (Array.IndexOf(BadNeighbours, cleft) == -1)// then left char. does not disqualify, so
                                                       //  so check char. at right end:
        { if (nextptr + lent == inlen) cright = (char) 0; else cright = inchar[nextptr+lent];
          if (Array.IndexOf(BadNeighbours, cright) == -1) qualifies = true;
      } }
      // Deal with InStr according to whether or not Target qualifies:
      if (qualifies)
      { if (isfirst){isfirst = false; Stats[3] = nextptr; } // update Stats[]
        newstr.Append(InStr._FromTo(ptr, nextptr-1)); newstr.Append(Replacement);
        Stats[0]++;   Stats[4] = newstr.Length;   ptr = nextptr + lent; }
      else // a bummer:
      { newstr.Append(InStr._FromTo(ptr, nextptr)); // include first char. of found Target
        ptr = nextptr+1; } // restart search after that first char.
    }
    Stats[1] = inlen;  Stats[2] = newstr.Length;
    return newstr.ToString();
  }
// Return a string made up of all the chars. in InStr which are also in HitList. Every
//  instance of a char. will be returned; if HitList contains 'a' and there are five
//  'a's in InStr, there will be five instances of 'a' in the returned string.
// The out parameter CleanedString is InStr with all HitList chars. removed.
// Note the simpler overload below.
  static public string CharsInList(string InStr, char[] HitList, out string CleanedString)
  { CleanedString = InStr;  if (InStr == "") return InStr;
    StringBuilder cleaned = new StringBuilder();
    StringBuilder result = new StringBuilder();
    char[] instr = InStr.ToCharArray();
    foreach (char ch in instr)
    { if (Array.IndexOf(HitList, ch) > -1) result.Append(ch);
      else cleaned.Append(ch); }
    CleanedString = cleaned.ToString();
    return result.ToString();
  }
// Return a string made up of all the chars. in InStr which are NOT in HitList. Every
//  instance of such a char. will be returned; if HitList omits 'a' and there are five
//  'a's in InStr, there will be five instances of 'a' in the returned string.
// The out parameter CleanedString is InStr with all non-HitList chars. removed.
// Note the simpler overload below.
  static public string CharsNotInList(string InStr, char[] HitList,
                                                             out string CleanedString)
  { CleanedString = "";  if (InStr == "") return InStr;
    StringBuilder cleaned = new StringBuilder();
    StringBuilder result = new StringBuilder();
    char[] instr = InStr.ToCharArray();
    foreach (char ch in instr)
    { if (Array.IndexOf(HitList, ch) == -1) result.Append(ch);
      else cleaned.Append(ch); }
    CleanedString = cleaned.ToString();
    return result.ToString();
  }
// Simple overload of above. Returns a count of instances NOT in HitList chars.
  static public int CharsNotInList(string InStr, char[] HitList)
  { int result = 0;
    if (InStr != "")
    { char[] instr = InStr.ToCharArray();
      foreach (char ch in instr){if (Array.IndexOf(HitList, ch)== -1) result++;}
    }
    return result;
  }
// Detect runs of the same character, if the character is in HitList. For example, if
//   InStr is "ppqrr" and HitList is "pq", "pp" is detected but not "pq" or "rr".
// If HitList is the empty string, repetitions of any character at all are detected.
// The returned string is InStr after removal of repetitions; Rejects contains all
//   removed chars. Example: InStr = "pppqqrs", HitList = "pqr", --> returned: "pqrs";
//   Rejects = "ppq". If none detected, Rejects is "" and InStr is returned intact.
  public static string DetectRunsOf(string InStr, string HitList, out string Rejects)
  { Rejects = "";  if (InStr == "") return InStr;
    StringBuilder rejects = new StringBuilder();
    StringBuilder result = new StringBuilder();
    char[] instr = InStr.ToCharArray();   int inlen = InStr.Length;
    result.Append(instr[0]); // The first char. always gets returned.
    for (int i = 1; i < inlen; i++)
    { if (instr[i] == instr[i-1] && (HitList == "" || HitList.IndexOf(instr[i]) > -1) )
      { rejects.Append(instr[i]); }
      else result.Append(instr[i]);
    }
    Rejects = rejects.ToString();
    return result.ToString();
  }
/// <summary>
/// <para>Finds the last element i such that Str1[i] = Str2[i]. Imagine that all strings have a hypothetical
/// equal character at element -1; that is, (virtual) Str1[-1] = (virtual) Str2[-1]. This causes '-1' to be returned
/// if either or both strings are empty, or if Str1[0] is not equal to Str2[0].</para>
/// <para>RETURNED FIELDS: .I as above;  .B TRUE if the two strings are exactly equal (including both being empty);
/// .X = Str1.Length - Str2.Length, irrespective of whether any partial matching occurs;
/// .S is the shared first portion of the strings (empty, if none). (If 'CaseSensitive' is FALSE, .S will be all capitals.)</para>
/// </summary>
  public static Quad EqualHowFar(string Str1, string Str2, bool CaseSensitive)
  { Quad quid = new Quad(false);
    int len1 = Str1.Length,  len2 = Str2.Length,  commonlen;
    quid.X = (double) (len1 - len2);
    if (len1 == 0 || len2 == 0)
    { quid.I = -1;  quid.B = (Str1 == Str2);   return quid; }
    // Both strings have content:
    if (!CaseSensitive){Str1 = Str1.ToUpper();   Str2 = Str2.ToUpper();}
    char[] str1 = Str1.ToCharArray();  char[] str2 = Str2.ToCharArray();
    if (len1 < len2) commonlen = len1; else commonlen = len2;
    quid.B = true;
    for (int i = 0; i < commonlen; i++) // commonlen is at least 1
    { if (str1[i] != str2[i])
      { quid.B = false;  quid.I = i-1;  quid.S = Str1._Extent(0, i); break; }
    }
    if (quid.B) // then the whole commonlen of both strings is equal:
    { quid.I = commonlen-1;  quid.B = (len1 == len2);
      if (len1 <= len2) quid.S = Str1; else quid.S = Str2; }
    return quid;
  }
// Return the kernel substrings resulting from stripping two strings of all left-end and all right-end characters that match.
// Along with these kernels, return pointers.
//  Examples: "ABCD/ABXYD" --> .IX = .IY = 1 ( = the last front-end matching char.; .XX = 3 (the last
//   char. from the back end of Str1 that is matched), .XY = 4 (do. for Str2), .SX = "C" (content of Str1 not in Str2),
//   .SY = "XY" (content of Str2 not in Str1). Note that in ALL cases, .IY redundantly = .IX.
//   "ABCDE/AXCYE" --> .IX = 0, .XX = 4, .XY = 4, .SX = "BCD", .SY = "XCY". (Note: chars. matching internally to the
//     extracted strings - in this case, 'C' is in both - are ignored.)
//   "ABC/DEFG" --> .IX = -1, .XX = 3, .XY = 4, .SX = "ABC", .SY = "DEFG". (Note: no starting match --> .IX = .IY = -1;
//     no match at the end --> .XX (or .XY) = length of Str1 (or Str2).
//   "AB/<empty string>" --> .IX = -1, .XX = 2, .XY = 0, .SX = "AB", .SY empty.
//   Both empty --> .IX = -1, .XX = 0, .XY = 0, .SX and .SY empty.
//   "ABCDCDCD/ABCD" --> .IX = 3, .XX = 8, .XY = 4, .SX = "CDCD", .SY empty. (The back pointer can't go to / past the front pointer.)
//   "ABCDCDCDEF/ABCDEF" --> .IX = 3, .XX = 8, .XY = 4, .SX = "CDCD", .SY empty. 
// As a bonus, .BX /.BY is TRUE if .SX/.SY is NOT empty.
  public static Octet StrDifference(string Str1, string Str2)
  { Octet result = new Octet(0);
    int FrontLastEqual = -1, BackLastEqual1 = 0, BackLastEqual2 = 0;
   // Deal with all cases where one or both strings are empty:
    if (Str1.Length == 0)
    { if (Str2.Length > 0) { BackLastEqual2 = Str2.Length;  result.SY = Str2; } } // otherwise leave defaults (as both strings empty)
    else if (Str2.Length == 0) { BackLastEqual1 = Str1.Length; result.SX = Str1; }
    else // both input strings are non-empty:
    { StringBuilder sb1= new StringBuilder(Str1);
      StringBuilder sb2 = new StringBuilder(Str2);
      int len1 = sb1.Length, len2 = sb2.Length;
      int minlen = len1; if (len2 < len1) minlen = len2;
      // Search from the front (i.e. from [0]) till the first unequal character.
      for (int i = 0; i < minlen; i++)
      { if (sb1[i] == sb2[i]) FrontLastEqual = i; else break; }
      // Search from the back
      for (int i = 0; i < minlen; i++)
      { if (i > minlen - FrontLastEqual - 2) break; // don't allow back pointer to overlie / pass front pointer
        if (sb1[len1 - i - 1] == sb2[len2 - i - 1])
        { BackLastEqual1 = len1 - i - 1; BackLastEqual2 = len2 - i - 1; }
        else break;
      }
      if (BackLastEqual1 == 0) BackLastEqual1 = len1; // default, only valid as return for empty string, which would never get here.
      if (BackLastEqual2 == 0) BackLastEqual2 = len2;
      // Set the texts:
      int start = FrontLastEqual + 1, extent1 = BackLastEqual1 - start, extent2 = BackLastEqual2 - start;
      if (extent1 > 0) result.SX = sb1.ToString(start, extent1);
      if (extent2 > 0) result.SY = sb2.ToString(start, extent2);
    }
    result.IX = FrontLastEqual;  result.IY = FrontLastEqual;  result.XX = (double)BackLastEqual1;  result.XY = (double)BackLastEqual2;
    result.BX = (!String.IsNullOrEmpty(result.SX));   result.BY = (!String.IsNullOrEmpty(result.SY));
    return result;
  }


/// <summary>
/// <para>On defining a 'word' as any contiguous collection of admissible chars., find
///  the next one in the window of InStr between the pointers. E.g. if admissible
///  chars. are only "a..z", it will find "cat" in "123cat?!".</para>
/// <para>ValidWordChars contains all admissible chars; but if this is "", ALL chars. not
///  in Delimiters will be taken as valid. Delimiters is not even accessed if
///  ValidWordChars has content, so can be left as "". However it is an error for
///  both to be "".</para>
/// <para>If CaseSensitive is FALSE, InStr is changed to upper case; but ValidWordChars
///  or Delimiters are left as is; therefore they should contain the upper case versions.</para>
/// <para>RETURNED: .BX TRUE if a word found, and no error. If true, the word is in .SX (it
///  will be in capitals, if CaseSensitive FALSE); and start and end ptrs. in .IX, .IY.
///  Errors only occur with silly args. An error returns .BY false; it is otherwise
///  true in all cases (find or no find). If an error, message is in .SY.</para>
/// <para>If EndPtr is > end of string, it is adjusted down to end of string.</para>
/// </summary>
  public static Octet FindWord(string InStr, int StartPtr, int EndPtr,
                   string ValidWordChars, string Delimiters, bool CaseSensitive)
  { Octet result = new Octet(0);
    // Deal with silly args.
    if (StartPtr < 0){result.SY = "negative StartPtr"; return result;}
    if (EndPtr >= InStr.Length) EndPtr = InStr.Length-1;
    if (StartPtr > EndPtr){result.SY = "StartPtr > EndPtr"; return result;}
    if (ValidWordChars._Length()< 1 &&  Delimiters._Length() < 1)  { result.SY = "character lists both empty"; return result; }
    // Errors eliminated.
    result.BY = true; // whatever the outcome below, it will not be an error.
    if (InStr == "") return result;
    if (!CaseSensitive)InStr = InStr.ToUpper();
    char[] inchar = InStr.ToCharArray(StartPtr, EndPtr-StartPtr+1);
    StringBuilder sb = new StringBuilder();
    int firstpos = -1;
    if (ValidWordChars != "")// then we use ValidWordChars as our criterion:
    { for (int i = 0; i < inchar.Length; i++)
      { if (ValidWordChars.IndexOf(inchar[i]) >= 0)
        { sb.Append(inchar[i]); if (firstpos == -1) firstpos = i;  }
        else if (firstpos != -1) break; // as word collection is finished.
    } }
    else // we use Delimiters instead as the criterion:
    { for (int i = 0; i < inchar.Length; i++)
      { if (Delimiters.IndexOf(inchar[i]) == -1)
        { sb.Append(inchar[i]); if (firstpos == -1) firstpos = i;  }
        else if (firstpos != -1) break; // as word collection is finished.
    } }
    if (sb.Length == 0) return result; // no find.
    result.BX = true;  result.SX = sb.ToString();
    result.IX = StartPtr + firstpos;  result.IY = result.IX + sb.Length - 1;
    return result;
  }
// Suppose you want to find the set "CAT" in that order, but with any number (or none)
// of lower-case letters or white spaces in between. E.g. you will accept "CAT" itself,
// or "C A  T" or "CxxxxAyyyT", but not "CxABxT" or "CTA" (wrong order). Then this is
// the fn. for you. Sequence goes in as "CAT"; set CaseSensitive how you wish.
// The hard one is IgnoreThese, as you have to write it in RegEx speak.
// RULES FOR IgnoreThese: (1) The string must start and end with square brackets.
//  (2) The dash has a special meaning: "[a-m]" means all letters 'a' to 'm' inclusive.
//    Therefore if you want to insert a dash, it has to follow a backslash: "[a\-m]"
//    would designate only the three chars. 'a','-','m'.
//  (3) Characters that MUST be preceded by a backslash are: . $ ^ { [ ( | ) * + ? \
//    To get it right, the best way of assigning is with the use of '@' before quotes:
//    IgnoreThese = @"[a\-m]";   (Otherwise you have to double the backslashes.)
//  (3.5) You can actually use these symbols without backslashes with Regex meaning, as
//    whatever you enter is simply passed as is to the Regex object, to go between each
//    character in Sequence. If you know the lingo, use it to make the fn. more powerful!
//  (4) You can use '\s' (small s) for white spaces, and a few others such (delve in
//    to Regex literature if wanting a list).
// RETURNED: .BY FALSE if any error; explanation in .SY. .BX TRUE if a find; in which
// case, .SX is the found string (complete with inbetween letters), .IX is its first
// char. posn. in InStr, and .IY its last.
  public static Octet FindInterruptedSequence(string InStr, string Sequence,
                                    string IgnoreThese, bool CaseSensitive)
  { Octet result = new Octet(0);
    // Deal with silly arguments.
    if (InStr == ""){result.BY = true;  return result;} // = No finds. Not an error.
    if (Sequence == ""){result.SY = "empty Sequence"; return result;} // error.
    if (IgnoreThese == "" || IgnoreThese[0] != '['
         || IgnoreThese[IgnoreThese.Length-1] != ']')
    { result.SY = "IgnoreThese is empty or unbracketted"; return result; }
    // Arguments OK, apart from the runtime test of IgnoreThese by Regex / Match.
    char[] seq = Sequence.ToCharArray();
    StringBuilder sb = new StringBuilder();
   sb.Append(seq[0]);
    for (int i = 1; i < seq.Length; i++)
    { sb.Append(IgnoreThese);  sb.Append("*");
      sb.Append(seq[i]);
    }
    string pattern =  sb.ToString();
    // Compile the pattern as a regex:
    Regex rx;
    try
    { if (CaseSensitive) rx = new Regex(pattern, RegexOptions.None);
      else               rx = new Regex(pattern, RegexOptions.IgnoreCase);
      // Find the first match for the pattern:
      Match macho = rx.Match(InStr);
      if (macho.Success)
      { result.BX = true;         result.SX = macho.Value;
        result.IX = macho.Index;  result.IY = result.IX + macho.Length - 1;
      }
      result.BY = true; // to get here, no crash in the regex mechanics.
    }
    catch
    { result.SY = "Regex searcher rejected IgnoreThese = '"+IgnoreThese+"'"; }
    return result;
  }

/// <summary>
/// There is a virtual cursor at At within Txt. If there is a contiguous sequence of identifier characters either around it
/// or touching it (before or after), then that sequence is returned, together with the pointer to its first character within Txt. If
/// no such sequence, .S = "" and .I = -1.
/// </summary>
  public static Strint ExtractIdentifier(string Txt, int At, bool NoInitialNumeral, string Identifier_chars)
  {
    StringBuilder stew = new StringBuilder(Txt);
    int stewlen_1 = stew.Length - 1;
    if (At < 0 || At > stewlen_1) return new Strint(-1, "");
    // Search backwards for the start of a sequence of identifier chars.
    int idStart = 0; // value to apply (a) if the loop below is never entered (At = 0), or (b) if its 'if' condition
                      // is never met (valid chars. all the way to the start).
    for (int i = At-1; i >= 0; i--)
    { if (Identifier_chars.IndexOf(stew[i]) == -1) { idStart = i+1;  break; } }
    // Search forwards towards the end likewise:
    int idEnd = stewlen_1; // value to apply in sitns. analagous to (a) and (b) for newstart.
    for (int i = At; i <= stewlen_1; i++)
    { if (Identifier_chars.IndexOf(stew[i]) == -1)
      { idEnd = i-1;  break; }
    }
    if (idEnd < idStart) return new Strint(-1, "");
    if (NoInitialNumeral && stew[idStart] < 'A') return new Strint(-1, ""); // only possible if the start is a numeral.
    return new Strint( idStart, stew.ToString(idStart, idEnd - idStart + 1) );
  }


// Work backwards from (BeforeThis-1), looking for SoughtStr, which must start
//  no earlier in InStr than FromHereOn. If found, return its start posn;
//  otherwise, return -1 (or < -1 for erratic argument).
// IgnoreWhat values: 0 = must immediately precede; 1 = ignore intervening blanks
//  and white spaces; 2 = ignore any chars.
  public static int CheckForPrecedingString(string InStr, string SoughtStr,
       int BeforeThis, int FromHereOn, int IgnoreWhat, bool CaseSensitive)
  {
    int inlen = InStr.Length;  if (inlen == 0) return -2;
    int slen = SoughtStr.Length; if (slen == 0) return -3;
    if (BeforeThis<0||IgnoreWhat<0||FromHereOn<0||IgnoreWhat>2) return -4;
    int lastposs = BeforeThis - slen; // last poss. posn. for SoughtStr
    if (lastposs < FromHereOn) return -1;
    // Deal with case where SoughtStr must immediately precede BeforeThis:
    if (IgnoreWhat == 0)
    { if (lastposs >= FromHereOn && InStr._Extent(lastposs,slen)==SoughtStr)
                                                                return lastposs;
      else return -1; }
    // Deal with other cases:
    string RevIn = InStr._FromTo(FromHereOn, BeforeThis-1)._Reverse();
    string RevS = SoughtStr._Reverse();
    if (!CaseSensitive){RevIn = RevIn.ToUpper();  RevS = RevS.ToUpper();}
    int q, p = RevIn.IndexOf(RevS);
    if (p == -1) return -1; // failed search.
    q = BeforeThis-p-slen;
    if (IgnoreWhat == 2) return q;// intervening chars. irrelevant.
    string ss = RevIn._Extent(0, p);//intervening chars.
    if (ss._Purge() == "") return q; else return -1;
  }

  /// <summary>
  /// <para>inStr may enter untrimmed, as it will always be trimmed before testing; if approved, its trimmed version will be returned in .S.</para>
  /// <para>inStr is checked for the following criteria: (a) it is not null, and after trimming, it is not empty; (b) that all chars. lie 
  /// between 'lowestAllowed' and 'highestAllowed'; (c) that within this range, none are specifically mentioned in 'excludeThese'; 
  /// and (d) that inStr does not start with a char. in 'nonStarters' (even though it is allowable elsewhere in the name).</para>
  /// <para>Each of the last two string arguments may be null.</para>
  /// <para>RETURNED if SUCCESSFUL: .B TRUE, .S is the TRIMMED name.</para>
  /// <para>RETURNED if ERROR: .B FALSE, .S is the error message, which always takes the form: "input text.." followed by a verb.</para>
  /// </summary>
  public static Boost CheckTitle(string inStr, char lowestAllowed, char highestAllowed, string excludeThese, string nonStarters)
  { Boost result = new Boost(false);
    if (String.IsNullOrEmpty(inStr)) { result.S = "input text is either null or empty"; return result; }
    string name = inStr.Trim(); if (name == ""){ result.S = "input text contains no chars. apart from white spaces"; return result; }
    string ss;
    name = name._PurgeExceptRange(0, -1, lowestAllowed, highestAllowed, out ss);
    if (ss != "") { result.S = "input text contains illegal char(s): '" + ss + "'"; return result; }
    if (!String.IsNullOrEmpty(nonStarters) )
    { if (nonStarters.IndexOf(name[0]) != -1){ result.S = "input text begins with illegal character '" + name[0] + "'"; return result; } }

    if (!String.IsNullOrEmpty(excludeThese) )
    { name = name._Purge(0, -1, out ss, excludeThese.ToCharArray());
      if (ss != "") { result.S = "input text contains unallowed char(s): '" + ss + "'"; return result; }
    }
    result.S = name;  result.B = true;  
    return result;
  }  


// OVERLOADS for removing string duplicates from arrays and lists:
  public static string[] RemoveDuplicates(string[] InArr)
  { List<string> inlist = new List<string>(InArr.Length);
    inlist.AddRange(InArr);
    List<string> outlist = RemoveDuplicates(inlist, 0);
    string[] result = new string[outlist.Count]; outlist.CopyTo(result); return result;
  }
  public static string[] RemoveDuplicates(List<string> InList)
  { List<string> outlist = RemoveDuplicates(InList, 0);
    string[] result = new string[outlist.Count]; outlist.CopyTo(result); return result;
  }
  public static List<string> RemoveDuplicates(List<string> InList, int dummy)
  { if (InList == null || InList.Count == 0) return null;
    List<string> result = new List<string>(InList.Count);
    result.AddRange(InList);
    int ptr = 0; 
    while (ptr < result.Count-1) // work forward through the list, to the 2nd. last element:
    { string ss = result[ptr];
      for (int i = result.Count-1; i > ptr; i--)
      { if (result[i] == ss) result.RemoveAt(i); }
      ptr++;
    }
    return result;
  }


//=======================================================================
//3. DATA <--> STRING CONVERSIONS and ARRAY CONVERSIONS
//-----------------------------------------------------------------------
// Convert a hexadecimal no. into a string of bits. Returns "?" if HexStr not properly formatted as a hex no., or if too big
//  for int type. (Case-insensitive; leading and trailing blanks ignored; initial "0x" or "0X" tolerated but not required.)
// GroupSize: if 0, the returned string has no leading zeroes and no internal delimiters. If > 0, the no. chars. is a multiple
//  of GroupSize, including if necessary left-padding zeroes. If Delimiter is the empty string, no delimiting occurs;
//  otherwise the delimiter occurs between groups. (If GroupSize is 0, Delimiter is ignored.)

  public static string HexStrToBits(string HexStr, int GroupSize, string Delimiter)
  { bool WentOk;     HexStr.Trim();    HexStr.ToUpper();
    HexStr._ParseHex(out WentOk); // We discard the return value, as we only want to raise the OUT argument.
    if (!WentOk) return "?";
    // HexStr is correctly formatted, and in capitals only:
    char[] hexch = HexStr.ToCharArray();
    string result = "";
    for (int i=0; i<hexch.Length; i++)
    { switch (hexch[i])
      { case '0': result += "0000"; break;      case '1': result += "0001"; break;
        case '2': result += "0010"; break;      case '3': result += "0011"; break;
        case '4': result += "0100"; break;      case '5': result += "0101"; break;
        case '6': result += "0110"; break;      case '7': result += "0111"; break;
        case '8': result += "1000"; break;      case '9': result += "1001"; break;
        case 'A': result += "1010"; break;      case 'B': result += "1011"; break;
        case 'C': result += "1100"; break;      case 'D': result += "1101"; break;
        case 'E': result += "1110"; break;      case 'F': result += "1111"; break;
      }
    }
    // Remove leading digits:
    int n = result.IndexOf('1');
    if (n == -1) result = "0";
    else if (n > 0) result = result.Substring(n);
    if (GroupSize > 0)
    { // Pad it with leading digits to group size:
      n = result.Length % GroupSize;
      if (n > 0) result = ('0')._Chain(GroupSize - n) + result;
      // Divide it up into groups:
      if (Delimiter != "")
      { string ss = "";
        int len = result.Length, ptr = 0;
        while (true)
        { ss += result.Substring(ptr, GroupSize);
          ptr += GroupSize;
          if (ptr >= len) break;
          ss += Delimiter;
        }
        result = ss;
      }
    }
    return result;
  }

  public static string JoinStrList(List<string> InList, string Delim, bool TrimItems)
  { string result = "";
    if (InList == null || InList.Count == 0) return result;
    string[] inarr = new string[InList.Count];
    InList.CopyTo(inarr);
    if (TrimItems)
    { for (int i=0; i<inarr.Length; i++) inarr[i] = inarr[i].Trim(); }
    return String.Join(Delim, inarr);    
  }
// One or more strings can function as delimiters. Delimiter at start / end will result in an empty
//  string at start / end. Two adjacent delimiters --> empty string between them.
//  If more than one delimiter string, and delimiter stretches in InStr consequently overlap,
//  realize that C# will work through the whole string[] Delim for each char. in InStr.
  public static List<string> SplitStrList(string InStr, bool TrimSubStrings, params string[] Delims)
  { string[] stroo = InStr.Split(Delims, StringSplitOptions.None);
    List<string> result = new List<string>();
    result.AddRange(stroo);
    if (TrimSubStrings)
    { for (int i=0; i<result.Count; i++) result[i] = result[i].Trim(); }
    return result;
  }
  
  
// Used to provide no. representations for a column. The result string is therefore of
//  fixed length, and always has the decimal point (even if not shown) in the same place.
//  Numbers are always returned strictly in decimal format, never in scientific notation.
// StrLength -- Must be long enough for every exigency, including having neg. sign. If
//  the trial string turns out to be too long, this method returns a string "***.."
//  of length StrLength. Also, its value must be at least 3.
// DecPosn -- The string position at which the decimal point must always go. (If the
//  setting of NoTrailZeroes causes the dec. pt. to disappear, it is still notionally
//  at this position.)
// NoTrailZeros -- (A) POS. OR ZERO: exact length of decimal part of number, using either
//  truncation (rounded) or padding with zeros to get this length. (B) ANY NEG. VALUE:
//  No trailing zeros; truncation only occurs if space between dec. pt. and string end
//  would otherwise be exceeded. If no nonzero dec. portion, no dec. pt. is displayed.
// ERRORS: A string of '?', +/- spaces, is returned, which has length StrLength (unless
//  StrLength is < 3, when it has length 3). Error type determines the exact pattern.
  public static string JustifyNo(double No, int StrLength, int DecPosn, int NoTrailZeros,
                                  bool ThousandsSepr)
  { if (StrLength < 3) return " ??";
    if (DecPosn < 1 || DecPosn >= StrLength) return ('?')._Chain(StrLength);
    string fmttr, result;
    int digitroom = StrLength - DecPosn - 1;
    if (NoTrailZeros > digitroom)
    { result = ("? ")._Chain(StrLength); result.PadRight(StrLength); return result; }
  // First get the number into string form:
    int notrail0s = NoTrailZeros;
    if (NoTrailZeros < 0) notrail0s = digitroom; // trail zeros will be excavated later.
    fmttr = "F" + notrail0s;
    result = No.ToString(fmttr);
    // Finalize the bit between dec. pt. and right end of string:
    result += (' ')._Chain(digitroom - notrail0s);
    if (result.Length > StrLength) return ('*')._Chain(StrLength);
    // Pad the string with leading blanks to the right length:
    result = result.PadLeft(StrLength);
    // If required, lop off trailing zeros, incl. dec. pt. if mantissa is zero:
    if (NoTrailZeros < 0)// then always a Dec.Pt., with only spaces or '0' beyond it:
    { StringBuilder sb = new StringBuilder(result);
      for (int i = result.Length-1; i >= 0; i--)
      { if (sb[i] == '0') sb[i] = ' ';
        else if (sb[i] == DecimalPoint){sb[i] = ' '; break;}
        else if (sb[i] != ' ') break;
      }
      result = sb.ToString();
    }
    return result;
  }
// A set of overloads for presenting a ragged array as a string. (I don't know how
//  to use Array for a ragged array.)
  public static string RaggedToStr(int[][] InArr, string ColumnDelim, string RowDelim)
  { string result = "";  if (InArr == null || InArr.Length == 0) return "";
    int rows = InArr.GetLength(0); 
    for (int i = 0; i < rows; i++)
    { for (int j = 0; j < InArr[i].Length; j++)
      { result+= (InArr[i][j]).ToString(); if(j<InArr[i].Length-1) result+= ColumnDelim;}
      if (i < rows-1) result += RowDelim; }
    return result;
  }
  public static string RaggedToStr(double[][] InArr, string ColumnDelim, string RowDelim)
  { string result = "";  if (InArr == null || InArr.Length == 0) return "";
    int rows = InArr.GetLength(0); 
    for (int i = 0; i < rows; i++)
    { for (int j = 0; j < InArr[i].Length; j++)
      { result+= (InArr[i][j]).ToString(); if(j<InArr[i].Length-1) result+= ColumnDelim;}
      if (i < rows-1) result += RowDelim; }
    return result;
  }
// Formatted type-double overload. If format string is faulty, returns error message.
  public static string RaggedToStr(double[][] InArr, string ColumnDelim, string RowDelim,
                                       string FormatStr)
  { string result = "";  if (InArr == null || InArr.Length == 0) return "";
   // Test the format string
    double dummy = 1.23;  string ss = "";
    try { ss = dummy.ToString(FormatStr); }
    catch { dummy = 0.0; } // Some errors don't raise the FormatException class, but
                           // instead simply return your FormatStr; hence the next line:
    if (dummy == 0.0 || ss == FormatStr) return "faulty format string";
    int rows = InArr.GetLength(0); 
    for (int i = 0; i < rows; i++)
    { for (int j = 0; j < InArr[i].Length; j++)
      { result+= (InArr[i][j]).ToString(FormatStr); 
        if(j<InArr[i].Length-1) result+= ColumnDelim; }
        if (i < rows-1) result += RowDelim; }
    return result;
  }

// Converts a one-dimensional array into a matrix. Obviously the array size must be
//  a multiple of NoRows. Error resets Success and returns null.
  public static double[,] DubArrToMx(double[] Arr, int NoRows, out bool Success)
  { if (NoRows < 1 || Arr.Length % NoRows != 0){Success = false; return null;}
    int NoCols = Arr.Length / NoRows;
    double[,] result = new double [NoRows, NoCols];
    int cnt = 0;
    for (int i=0; i<NoRows; i++){for (int j=0; j<NoCols; j++) {result[i,j]=Arr[cnt]; cnt++;}}
    Success = true;  return result;
  }
// Converts a matrix into a one-dimensional array.
  public static double[] MxToDubArr(double[,] Mx)
  { double[] result = new double [Mx.GetLength(0) * Mx.GetLength(1)];
    int cnt = 0;
    foreach (double xx in Mx) {result[cnt] = xx; cnt++;}
    return result;
  }
//=======================================================================
//4. PARSING METHODS           //pars    //nest
//-----------------------------------------------------------------------
/// <summary>
/// Check a string for correct bracketting. If the given brackets were thus: Opener = '(', Closer = ')' : 
/// then this would be correct: "(A((B(C)D)E)F)G". If the no. of openers != no. closers, brackets are "unmatched"; 
/// another error occurs if brackets are "crossed" - "A)B(C". Any chars. can be nominated 'brackets'. (They can even
/// both be the same character, in which case this mthod becomes simply a check for an even no. of instances of that char.)
/// RETURNED: (A) NO ERROR FOUND: This includes the case where there were no brackets at all (not an error state). .B is TRUE;
/// .I is the highest level of nesting in the string: 0 for "ABC", 2 for e.g. "A(B(C))" or "((A))B((C))". (Empty string --> -1.)
///  (If Opener = Closer, .I always = 0 as nesting level is ambiguous). (B) ERROR FOUND: .B FALSE, error message in .S.
/// </summary>
  public static Quad BracketsCheck(ref string InStr, char Opener, char Closer, params int[] FromTo)
  { Quad result = new Quad(false);  int strlen = InStr.Length;
    if (InStr == "") { result.I = -1; result.B = true; return result; }
    int FromPtr = 0, ToPtr = strlen-1;
    if (FromTo.Length >= 1) FromPtr = FromTo[0];   if (FromTo.Length >= 2) ToPtr = FromTo[1];  
    // Arg. errors and correction:
    if (ToPtr >= strlen) ToPtr = strlen-1;
    if (FromPtr < 0 || FromPtr >= InStr.Length || FromPtr > ToPtr) { result.S = "impossible arguments"; return result; }       
    char[] instr = InStr.ToCharArray(FromPtr, ToPtr - FromPtr + 1);   int openers = 0,  closers = 0;
    foreach (char ch in instr)
    { if (ch == Opener) openers++; else if (ch == Closer) closers++; }
    if (Opener == Closer)
    { if (openers % 2 == 0){ result.B = true; result.I = 0; return result; }
      else {result.S = "odd number of brackets"; return result; }
    }
  // Different opening and closing clamps:
    if (openers > closers){result.S="unmatched opener(s) present"; return result;}
    else if (openers < closers)
    { result.S = "unmatched closer(s) present"; return result; }
    // Openers and Closers match in numbers. Do they cross? And what is the
    //   highest nesting level?
    int lvl = 0, maxlvl = 0;
    foreach(char ch in instr)
    { if (ch == Opener) lvl++;  else if (ch == Closer) lvl--;
      if (lvl < 0){result.S = "improper order of nested brackets"; return result;}
      if (lvl > maxlvl) maxlvl = lvl;
    }
    result.B = true;  result.I = maxlvl;  return result;
  }
///<summary>
/// Overload for use where 'brackets' are longer than one character:
///</summary>
  public static Quad BracketsCheck(ref string InStr, string Opener, string Closer)
  { char[] chrs = LowestAbsentChars(InStr, 2);
    string op = chrs[0].ToString(), clos = chrs[1].ToString();
    string NewStr = InStr.Replace(Opener, op);
    NewStr = NewStr.Replace(Closer, clos);
    return BracketsCheck(ref NewStr, chrs[0], chrs[1]);
  }

/// <summary>
/// Given a reference point - the character at ReferencePtr - find the opener which starts the nest level current at that point.
/// If 'ReferencePtr' points to a closer, it will find the corresponding opener.
/// If to an opener, it returns that opener. If no find, returns -1. All of InStr prior to StartPtr is ignored.
/// Examples: "01[3[5]7]9": ReferencePtr = 9 returns -1; ReferencePtr = 8 or 7 returns 2; ReferencePtr = 2 returns 2.
/// Silly values of the pointer args. return -1 (no find), EXCEPT that if ReferencePtr is greater than the end of InStr it is
/// corrected to the last character of InStr.
/// </summary>
  public static int OpenerAt(string InStr, char Opener, char Closer, int StartPtr, int ReferencePtr)
  { if (InStr == "") return -1;
    if (ReferencePtr >= InStr.Length) ReferencePtr = InStr.Length-1;
    if (StartPtr < 0 || ReferencePtr < 0 || StartPtr > ReferencePtr) return -1;
    // valid start and end pointers:
    char[] instr = InStr.ToCharArray(StartPtr, ReferencePtr-StartPtr+1);
    int lvl = 1, result = -1, instrLast = instr.Length-1;
    for (int i = instrLast; i>= 0; i--)
    { if (instr[i] == Closer){ if (i != instrLast) lvl++; }
      else if (instr[i] == Opener)
      { lvl--;   if (lvl == 0) {result = StartPtr+i; break;} }
    }
    return result;
  }
/// <summary>
/// <para>Find the closer ending the current nest level. If 'From' points to an opener, it will find the corresponding closer. 
/// If to a closer, returns that closer. If no find, returns -1.</para>
/// <para>Examples: "01[3[5]7]9": start at 0 --> -1; start at 2 or 3 --> 8; start at 8 --> 8.</para>
/// <para>FromTo: silly values return -1 (no find), EXCEPT that if TO is either absent or greater than the end of InStr it is corrected 
/// to the end of InStr.</para>
/// </summary>
  public static int CloserAt(string InStr, char Opener, char Closer, params int[] FromTo)
  { int lastch = InStr.Length-1;
    if (lastch < 1) return -1;
    int lvl = 1;  int startptr = 0, endptr = lastch;  int result = -1;
    if (FromTo.Length > 0)startptr = FromTo[0];
    if (FromTo.Length > 1)endptr = FromTo[1];
    if (endptr > lastch) endptr = lastch;
    if (startptr<0 || endptr<0 || startptr>endptr) return result;
    // valid start and end pointers:
    char[] instr = InStr.ToCharArray(startptr, endptr-startptr+1);
    for (int i = 0; i < instr.Length; i++)
    { if (instr[i] == Opener){ if (i != 0) lvl++; }
      else if (instr[i] == Closer)
      { lvl--; if (lvl==0){result = startptr+i; break;} }
    }
    return result;
  }
// Overload for StringBuilder -
  public static int CloserAt(ref StringBuilder InSbr, char Opener, char Closer, params int[] FromTo)
  { int lastch = InSbr.Length-1;
    if (lastch < 1) return -1;
    int lvl = 1;  int startptr = 0, endptr = lastch;  int result = -1;
    if (FromTo.Length > 0)startptr = FromTo[0];
    if (FromTo.Length > 1)endptr = FromTo[1];
    if (endptr > lastch) endptr = lastch;
    if (startptr<0 || endptr<0 || startptr>endptr) return result;
    // valid start and end pointers:
    for (int i = startptr; i <= endptr; i++)
    { if (InSbr[i] == Opener){ if (i != startptr) lvl++; }
      else if (InSbr[i] == Closer)
      { lvl--; if (lvl==0){result = i; break;} }
    }
    return result;
  }
///<summary>
/// Returns a RAGGED ARRAY of type Duo[][], holding all information about all levels of bracketting within the defined SUBstring.
/// The nth. returned row holds positions of all openers (.X) and closers (.Y) at level n (where level 0 is the level before any brackets WITHIN
/// the substring - no note is taken of nesting levels preceding the substring).
/// NULL is returned if the string is empty; if 'from' and 'to' pointers cross; or if somewhere in the SUBstring, at whatever level, 
/// brackets are MISMATCHED (even though over the whole string they may be matched), or out of order (closer precedes opener).
/// The final number of rows (for non-null return) will always be TopLevel + 1. Note that InStr is a REF argument;
/// it will not be altered here. Re LEVEL 0: (a) if none - "(...)" - returns empty, non-null row 0. Otherwise each Duo in the row
/// represents the first and last non-bracket char. of a level 0 run. (All other levels are non-empty, and all pointers are to brackets.)
///</summary>
  public static Duo[][] OpenersAndClosers(ref string InStr, char Opener, char Closer, out int TopLevel, params int[] FromTo)
  { TopLevel = 0; // have to set the 'out' parameter first up.
    if (String.IsNullOrEmpty(InStr) ) return null;
    int inlen = InStr.Length, startptr = 0, endptr = inlen-1; 
    if (FromTo.Length > 0) startptr = FromTo[0];
    if (FromTo.Length > 1) endptr = FromTo[1];
    if (startptr < 0) startptr = 0;
    if (endptr >= inlen) endptr = inlen-1;
    if (startptr >= endptr) return null;
   // Confirm proper nesting, and find highest level:
    Quad quee = BracketsCheck(ref InStr, '(', ')', startptr, endptr);
    if (!quee.B) return null;
    TopLevel = quee.I;
   // Convert the SUBstring to a char. array for all further operations
    char[] inchars = InStr.ToCharArray(startptr, endptr - startptr + 1);
    int chlen = inchars.Length;
   // Create the required array of lists, and return the pointers. 
    int nolists = TopLevel + 1;
    List<Duo>[] outlists = new List<Duo>[nolists];
    for (int i=0; i < nolists; i++) outlists[i] = new List<Duo>();
    char ch;  int n=0, lvl = 0;  Duo d;
   // Detect all the brackets: (It is VITAL for this part that brackets match and never cross; hence the call to ClampsCheck(.) above.)
    for (int i = 0; i < chlen; i++)
    { ch = inchars[i];
      if (ch == Opener)
      { lvl++; outlists[lvl].Add(new Duo(startptr+i, -1)); 
        if (lvl == 1 && i != 0) 
        { n = outlists[0].Count-1;
          d = outlists[0][n]; d.Y = startptr+i-1; outlists[0][n] = d; // Closes "..AA(..", A at level 0. (Stretch opened as below.)
        }
      }
      else if (ch == Closer)
      { n = outlists[lvl].Count-1;
        d = outlists[lvl][n]; d.Y = startptr+i; outlists[lvl][n] = d; // "..AA).."
        lvl--;
      }
      else if (lvl == 0)
      { if (i == 0 || inchars[i-1] == ')') outlists[0].Add(new Duo(startptr+i, -1)); // Opens level 0 stretch for "AA.." or ")AAA..".
      }  
    }
    if (inchars[chlen-1] != Closer)
    { n = outlists[0].Count-1;
      d = outlists[0][n]; d.Y = startptr+chlen-1; outlists[0][n] = d; // substring did not end on a closer.
    }
    // Convert to a ragged array:
    Duo[][] result = new Duo[nolists][];
    for (int i=0; i < nolists; i++) result[i] = outlists[i].ToArray(); 
    return result;
  }

///<summary>
/// Returns an ARRAY of type Duo[], each Duo object pointing to an opener (.X) and corresponding closer (.Y) at the given level 
/// (level 0 being the level before any brackets WITHIN the substring - no note is taken of nesting levels preceding the substring. If the
/// first char. of the substring is an opening bracket, this is taken as being at level 1).
/// NULL return represents an error state: null input string; 'from' and 'to' pointers cross; or somewhere in the SUBstring, at whatever level, 
/// brackets are MISMATCHED (even though over the whole string they may be matched) or out of order (closer precedes opener).
/// On the other hand, a nonnull but EMPTY array is returned if the substring is empty, or if the given level does not occur in the substring.
/// Note that InStr is a REF argument; it will not be altered here. Re LEVEL 0: (a) if none - "(...)" - returns an empty, non-null array. 
/// Otherwise each Duo in the array represents the first and last NONBRACKET CHAR. of a level 0 run. For all higher levels, the Duo
/// objects point to brackets.
///</summary>
  public static Duo[] OpenersAndClosers(ref string InStr, int AtNestLevel, char Opener, char Closer, params int[] FromTo)
  { if (InStr == null) return null;
    if (InStr == "") return new Duo[]{ }; // empty
    int inlen = InStr.Length, startptr = 0, endptr = inlen-1; 
    if (FromTo.Length > 0) startptr = FromTo[0];
    if (FromTo.Length > 1) endptr = FromTo[1];
    if (startptr < 0) startptr = 0;
    if (endptr >= inlen) endptr = inlen-1;
    if (startptr >= endptr) return null;
   // Confirm proper nesting, and find highest level:
    Quad quee = BracketsCheck(ref InStr, '(', ')', startptr, endptr);
    if (!quee.B) return null;
    if (AtNestLevel < 0 || AtNestLevel > quee.I) return new Duo[]{ }; // empty, as the required nesting level is not present.
   // Convert the SUBstring to a char. array for all further operations
    char[] inchars = InStr.ToCharArray(startptr, endptr - startptr + 1);
    int chlen = inchars.Length;
   // Create the required array of lists, and return the pointers. 
    List<Duo> outlist = new List<Duo>();
    char ch;  int n=0, lvl = 0;  Duo d;
   // Detect all the brackets: (It is VITAL for this part that brackets match and never cross; hence the call to ClampsCheck(.) above.)
    for (int i = 0; i < chlen; i++)
    { ch = inchars[i];
      if (ch == Opener)
      { lvl++;  
        if (lvl == AtNestLevel)  outlist.Add(new Duo(startptr+i, -1)); // with properly matched bkts, never reached if AtNestLevel is 0.
        else if (AtNestLevel == 0 && lvl == 1 && i != 0) 
        { n = outlist.Count-1;
          d = outlist[n]; d.Y = startptr+i-1; outlist[n] = d; // Closes "..AA(..", A at level 0. (Stretch opened as below.)
        }
      }
      else if (ch == Closer)
      { if (lvl == AtNestLevel)
        { n = outlist.Count-1;
          d = outlist[n]; d.Y = startptr+i; outlist[n] = d; // "..AA).."
        }
        lvl--;
      }
      else if (lvl == 0 && AtNestLevel == 0)
      { if (i == 0 || inchars[i-1] == ')') outlist.Add(new Duo(startptr+i, -1)); // Opens level 0 stretch for "AA.." or ")AAA..".
      }  
    }
    // For AtNestLevel 0, more cleaning up to do:
    if (AtNestLevel == 0 && inchars[chlen-1] != Closer)
    { n = outlist.Count-1;
      d = outlist[n]; d.Y = startptr+chlen-1; outlist[n] = d; // substring did not end on a closer.
    }
    return outlist.ToArray();
  }

// Returns an int array of size 6, giving nesting data relative to a char. posn.
//   Errors --> [0] is -2 or less. (see codes at end of heading), and other places
//   indeterminate. If no errors, [0] is >= -1, and coding is as follows:
// [0]: Posn. of first Opener at or to left of AtPos, after skipping over any higher-
//   level nestings; e.g. for "A(B(C)D..", if AtPos pointed to D, [0] would give the
//   position of bracket after A; if AtPos pointed to bracket before C, [0] would return
//   as AtPos. If no such bkt to left, [0] returns as -1.
// [1]: Likewise gives the posn. of first Closer at or to right of AtPos, hopping over
//   higher nestings. If no such bkt to right, returns as length of InStr.
// [2]: 0 = AtPos is not at any bkt; 1 = at Opener; 2 = at Closer.
// [3]: highest relative nesting level inside secn. between [0] and [1]. E.g. for
//   "A(B(C)D)", AtPos at B (or bkt to its left) --> [3] = 1. AtPos at C --> [3] = 2.
// [4] = length of enclosed extent; equals [1] - [0] - 1.
// [5]: If GetAbsolNestLevel is TRUE, this gives the nesting level of the enclosed extent
//   (between [0] and [1]) relative to the WHOLE string. If FALSE, simply returns -1.
//   To save time, should be false for very long strings where this value is irrelevant.
// Opener and Closer may be the same.
// NB - there is NO PRIOR CHECK for correct nesting, so run ClampsCheck first for the whole
//   text, before multiple calls of this for parts of the texts. Many errors would otherwise
//   go undetected. Where errors are detected, they are coded thus: -2 = empty InStr;
//   -3 = silly AtPos; -4 = unmatched or crossed bkts;
  public static int[] NestLevel(string InStr, char Opener, char Closer, int AtPos,
                                  bool GetAbsolNestLevel)
  { int[] result = new int[6]; // C# automatically sets all result[i] to 0 - I rely on this.
    int inlen = InStr.Length;
    if (inlen==0) {result[0] = -2; return result;} // Empty string error.
    if (AtPos < 0 || AtPos >= inlen) {result[0] = -3; return result;} // Silly arg. error.
    char[] instr = InStr.ToCharArray();
   // If Opener = Closer, we have to find a new Closer, and substitute it for every
   //  second instance of a clamp.
   if (Opener == Closer)
   { if (InStr._CountChar(Opener) % 2 == 1){result[0] = -4;  return result;}//must be even no.
     bool chfound;
     for (char ch = '\u0001'; ch < '\uFFFF'; ch++)
     { chfound = false;
       for (int i=0; i<inlen; i++) if (instr[i]==ch){chfound = true; break;}
       if (!chfound){Closer = ch; break; }
     } // Closer is now different to Opener, and does not occur elsewhere in InStr.
     int clampcount = 0;
// *** Algorithms for assiging Closer: Suppose the clamp is '|'; for explanation purposes,
//  suppose we replace this with '(' (when it is taken as an opener) and ')' (when as
//  a closer). Then algorithm 1 - used here - converts "A|B|C|D|E" to "A(B)C(D)E". Note
//  that a nesting level higher than 1 can never occur with this approach. Algorithm 2
//  assumes a 'wedding cake' model: "A|B|C|D|E" --> "A(B(C)D)E". Currently only Algo. 1
//  is supported here; but if Algo. 2 becomes nec., insert it as an alternative, and so
//  add a Boolean argument to the method, to determine which algorithm to use.
     for (int i=0; i<inlen; i++) // Turn every second clamp into a closer:
     { if (instr[i]==Opener)
       { clampcount++; if (clampcount%2==0) instr[i] = Closer; }
     }
   }
  // In all cases, Opener and Closer are now different.
    // Search backwards for opener:
    if (instr[AtPos] == Opener) {result[0] = AtPos;  result[2] = 1;}
    else
    { int minlvl = 0, lvl = 0;  result[0] = -1; // default for case where no opener found.
      for (int i=AtPos; i>=0; i--)
      { if (instr[i] == Opener && lvl == 0){result[0] = i; break;}
        if (instr[i]==Closer && i!=AtPos) lvl--; else if (instr[i] == Opener) lvl++;
        if (lvl < minlvl) minlvl = lvl; // lvl can only ever be 0 or negative.
      }
      if (lvl != 0){result[0] = -4; return result;} // ')' unmatched from start of InStr.
      result[3] = -minlvl; // highest enclosed relative nesting found so far.
    }
    // Search forwards for closer:
    if (instr[AtPos] == Closer) {result[1] = AtPos;  result[2] = 2;}
    else
    { int maxlvl = 0, lvl = 0;  result[1] = inlen;//default for case where no opener found.
      for (int i=AtPos; i<inlen; i++)
      { if (instr[i] == Closer && lvl == 0){result[1] = i; break;}
        if (instr[i]==Opener && i!=AtPos) lvl++;  else if (instr[i] == Closer) lvl--;
        if (lvl > maxlvl) maxlvl = lvl; // lvl can only ever be 0 or positive.
      }
      if (lvl != 0){result[0] = -4; return result;} // '(' unmatched to end of InStr.
      if (maxlvl > result[3]) result[3] = maxlvl; // highest enclosed nesting.
    }
   // Get length of text between found brackets ( = length of InStr, if no brackets):
    result[4] = result[1] - result[0] - 1;
   // Get absolute nesting of enclosed text, if required:
    if (GetAbsolNestLevel)
    { for (int i = 0; i <= result[0]; i++) // from start of InStr to & incl. clamp at [0]:
      { if (instr[i] == Opener) result[5]++; else if (instr[i] == Closer) result[5]--;
        if (result[5] < 0) {result[0] = -4; return result;} // crossed clamps.
    } }
    return result;
  }
// TWO OVERLOADS, one for CHAR clamps and one (more expensive) for STRING clamps:
// (1) CHAR CLAMPS VERSION: Return a string which overwrites every character in
//  nestings of level ClampLevel and higher (including its clamps) with Blotter.
//  For example, given "abc(de(fg))h", with blotter '*', ClampLevel 0 would -->
//  all '*', 1 would --> "abc*********h"; 2 would --> "abc(de****)h".
// If you want to actually remove the overwritten chars., run EradicateChar(.)
// on the result.
  public static string BlotOut(string InStr, char Opener, char Closer,
                                                 char Blotter, int ClampLevel)
  { if (ClampLevel < 0 || InStr == "") return InStr;
    char[] instr = InStr.ToCharArray();
    StringBuilder sbldr = new StringBuilder();
    char ch;  int level = 0;
    for (int i = 0; i < instr.Length; i++)
    { ch = instr[i];
      if (ch == Opener) level++;
      if (level >= ClampLevel) sbldr.Append(Blotter); else sbldr.Append(ch);
      if (ch == Closer) level--;
    }
    return sbldr.ToString();
  }
// (2) STRING CLAMPS VERSION: // Replace everything between (and including)
//  strings Opener and Closer within the given string, replacing each char. in the
//  extent with char Blotter. If an Opener found but no subsequent Closer, or if
//  Closer = "", all from Opener to end of string is blotted out. If Closer before
//  first Opener, it is ignored. (Note: No ClampLevel as in the above overload.)
// If you want to actually remove these bits in parentheses, run EradicateChar().) on result.
  public static string BlotOut(string InStr, string Opener, string Closer, char Blotter)
  { if (InStr=="" || Opener=="") return InStr;
    StringBuilder instr = new StringBuilder(InStr);
    // As StringBuilder does not have its own IndexOf method, we will do searching on
    //  the original string, while installing changes into the StringBuilder object.
    int opptr = 0, clptr;
    while (true)
    { opptr = InStr.IndexOf(Opener, opptr);  if (opptr == -1) break;
      // Opener found. If Closer is empty string, simply blot out the rest of the string.
      if (Closer == ""){for (int i=opptr; i<InStr.Length; i++) instr[i] = Blotter;  break;}
      // Otherwise hunt for the Closer:
      clptr = InStr.IndexOf(Closer, opptr + Opener.Length);
      // If none found, blot out the rest of the string.
      if (clptr == -1){for (int i=opptr; i<InStr.Length; i++) instr[i] = Blotter;  break;}
      // Blot out from Opener to Closer inclusive, and update opptr:
      for (int i=opptr; i<clptr + Closer.Length; i++) instr[i] = Blotter;
      opptr = clptr + Closer.Length;  if (opptr >= InStr.Length) break;
    }
    return instr.ToString();
  }
// Return all between clamps of the next level. For example, given a(b)c(d(e)f)g,
// startptr of either 0 or 1 (=at opening clamp) would return array [ b, d(e)f ].
// Omitted FromTo elements --> obvious defaults. Returns NULL if no finds / errors.
// Unmatched clamps: as long as nestlevel goes below orig. level there is no problem;
// e.g. "a(b)c(d)e)f)" will still return [b, d]; but "a(b)c(d" will return ["b", ""].
  public static string[] ClampInclusions(string InStr, char Opener, char Closer,
                                                      params int[] FromTo)
  { int startptr, endptr;
    if (FromTo.Length > 1)endptr = FromTo[1]; else endptr = InStr.Length-1;
    if (FromTo.Length > 0)startptr = FromTo[0]; else startptr = 0;
    if (startptr > endptr || InStr=="") return null;
    if (InStr.IndexOf(Opener)==-1) return null; // no brackets at all.

    char[] instr = InStr.ToCharArray(startptr, endptr-startptr+1);
    char[] delim = LowestAbsentChars(InStr, 1, 'z');// guaranteed not in InStr
    StringBuilder sb = new StringBuilder();
    int nestlvl = 0;  char ch;
    for (int i = 0; i < instr.Length; i++)
    { ch = instr[i];
      if (ch == Opener){nestlvl++; if (nestlvl > 1) sb.Append(ch); }
      else if (ch == Closer)
      { nestlvl--;
        if (nestlvl > 0)sb.Append(ch);
        else if (nestlvl == 0) sb.Append(delim[0]);
      }
      else if (nestlvl > 0) sb.Append(ch);
    }
    string ss = sb.ToString();
    ss = ss._CullEnd(1); // remove the final delimiter. No harm, if ss empty.
    return ss.Split(delim);
  }

// Return index of the next occurrence of char. Target WHICH IS at the same nest level as the char. at the start. 
//  If there is an opener at the start, the nest level just inside it is the one for the search. 
//  (If InStr begins with a closer, nothing happens. Don't do it.)
// The search stops at the end of the segment at this level; i.e. it does not cross into any subsequent segments at the same level.
// The params arg., if there, gives limits to the search.
  public static int FindAtLevel(string InStr, char Opener, char Closer,  char Target, params int[] FirstLast)
  { int inlen = InStr.Length,  startptr = 0,  endptr = inlen-1,  paramlen = FirstLast.Length;
    if (paramlen > 0) startptr = FirstLast[0];  if (paramlen > 1) endptr = FirstLast[1];
    if (startptr < 0) startptr = 0;   if (endptr >= inlen) endptr = inlen-1;
    if (endptr < startptr) return -1; // No crash; simply, no finds. Includes an empty input string.
    char[] instr = InStr.ToCharArray(startptr, endptr-startptr+1);
    if (instr[0] == ')') return -1; // see warning at top.
    int level = 0; // the level of the search.
    if (instr[0] == '(') level = -1; // see notes at top re bkt. at the start
    char ch;
    for (int i = 0; i < instr.Length; i++)
    { ch = instr[i];
      if (level == 0 && ch == Target) return startptr + i;
      if (ch == Opener) level++;  else if (ch == Closer) level--;
      if (level== -1) return -1; // Don't look in subsequent segments that are at the same level.
    } 
    return -1;
  }

// Replace every occurrence of char. Target with char. Replacemt, BUT ONLY IF
//  Target is at the same nest level as the char. at the start. If there is an
//  opener at the start, the nest level just inside it is the one for
//  replacements. (If string begins with a closer, nothing happens. Don't do it.)
  public static string ReplaceAtLevel(string InStr,
                   char Opener, char Closer,  char Target, string Replacemt)
  { char[] instr = InStr.ToCharArray();
    if (instr[0] == ')') return InStr; // see warning at top.
    int level = 0; // the level of replacements.
    if (instr[0] == '(') level = -1; // see notes at top re bkt. at the start
    StringBuilder sb = new StringBuilder();  char ch;
    for (int i = 0; i < instr.Length; i++)
    { ch = instr[i];
      if (level == 0 && ch == Target) sb.Append(Replacemt);
      else
      { if (ch == Opener) level++;  else if (ch == Closer) level--;
        sb.Append(ch); // whatever it is (as it can't be Target at level 0).
    } }
    return sb.ToString();
  }
// Given a list of strings, a search string LHS, and a character PivotalChar (e.g. '='),
//  look for the combination 'lhs' + char. All before '=' is trimmed before comparison is made.
//  if found, return .B true, .I is the line no., and .S all that follows '=' (trimmed); otherwise, 
//  .B false (and .S empty - which it may also be with a find, if nil follows '=').
//  Best-guess values are used for StartPos and EndPos if they are silly. (If crossed, no find.)
  public static Quad FindRHS( List<string> Lines, string LHS, char PivotalChar, int StartPos, int EndPos,
   bool CaseSensitive) //################ CaseSensitive not yet programmed into use.
  { Quad result = new Quad(false);
    if (Lines == null || Lines.Count == 0 || LHS == "") return result;
    if (StartPos < 0) StartPos = 0;  if (EndPos >= Lines.Count) EndPos = Lines.Count-1;
    int n, piv;
    for (int ln=StartPos; ln <= EndPos; ln++)
    { piv = Lines[ln].IndexOf(PivotalChar);
      if (piv > 0) // char. found, and is not the first char.
      { n = Lines[ln].IndexOf(LHS); // do this step to avoid creating loads of strings below in a long search.
        if (n >= 0) // LHS is somewhere in the line:
        { if (Lines[ln]._Extent(0, piv).Trim() == LHS.Trim())
          { result.B = true;    result.I = ln;
            result.S = (Lines[ln]._Extent(piv+1)).Trim() ; break;
          }
    } } }   
    return result;      
  }
// Overload of the above, where the whole of Lines is to be searched:  
  public static Quad FindRHS( List<string> Lines, string LHS, char PivotalChar, bool CaseSensitive)
  { return FindRHS(Lines, LHS, PivotalChar, 0, int.MaxValue, CaseSensitive);   
  }

/// <summary>
/// <para>Find an INTEGER in InStr between (and including) optional pointers StartPtr and EndPtr. (If EndPtr is absent or is too big, 
/// it is corrected to point to the end of InStr). If IgnoreInitialMinus, only digits will be sought, and not also a leading '-'.</para>
/// <para>RETURNED: An array of length 4. [0] indicates success: 1 for success, 0 for failure,
///  -1 if number found but it is out of the int range; -2 if args. ridiculous (except for abovementioned correction).
///  [1] is the integer (indeterminate, if not successful find); [2] and [3] point to the first and last chars. of the no. 
///  (Both are -1, if no digits are found. But in the case of an out-of-range error, they will correctly delineate the digit substring.)</para>
/// <para>The fn. will start collecting on finding the first digit, and finish collecting after the last contiguous digit.</para>
/// <para>NB - if a neg. sign or digit precedes StartPtr, it will not be taken into account.</para>
/// </summary>
  public static int[] FindInteger(string InStr, bool IgnoreInitialMinus, params int[] StartEndPtr)
  { int[] result = {0, 0, -1, -1}; // failure values as default.
    if (String.IsNullOrEmpty(InStr)) return result;
    int inlen = InStr.Length;
    int StartPtr = 0; if (StartEndPtr.Length > 0) StartPtr = StartEndPtr[0];
    int EndPtr = inlen; if (StartEndPtr.Length > 1) EndPtr = StartEndPtr[1];
    // Corrections:
    if (EndPtr >= inlen)EndPtr = inlen-1;
    if (StartPtr < 0 || EndPtr < StartPtr){result[0] = -2; return result;}
    // Acceptable arguments, so continue.
    char[] instr = InStr.ToCharArray(StartPtr, EndPtr-StartPtr+1);
    StringBuilder num = new StringBuilder();
    bool digitfound = false;   bool ispositive = true;
    for (int i = 0; i < instr.Length; i++)
    { if (char.IsDigit(instr[i])) // i.e. is 0 to 9
      { if (!digitfound)
        { digitfound = true; result[2] = i + StartPtr;
          if (!IgnoreInitialMinus && i > 0 && instr[i-1]=='-') ispositive = false; 
        }
        num.Append(instr[i]);
      }
      else if (digitfound){result[3] = i+StartPtr-1; break;}
    }
    if (digitfound)
    { if (result[3] == -1) result[3] = EndPtr; // last char. a digit, so
                 // the setting of result[3] in the above loop was never triggered.
      // Take negative sign into account:
      if (!ispositive){num.Insert(0, "-"); result[2]--;} // decrement start ptr.
      // Conversion, with check for out-of-range:
      string ss = num.ToString();
      try { result[1] = Int32.Parse(ss); } // If no. is out of range, error thrown.
      catch { result[0] = -1;  return result; }
      result[0] = 1;
    }
    return result;
  }
// Find a NUMBER (as type double) in InStr between (and including) StartPtr and EndPtr.
//  (If EndPtr is too big, it is corrected to point to the end of InStr). Examples:
//  "a-1..23bb"-->-1; "a-1.23bb"-->-1.23; "aa1.23Ebb"-->1.23;"aa1.23E++2bb"-->1.23;
//     "aa1.23E+2bb"-->123 (i.e. 1.23E2). (1E2 and 1E+2 both valid. Also, 1e2 valid.)
//  If IgnoreInitialMinus is TRUE, then "a-1E-2" --> 1e-2 (.IX pointing to '1')..
//  If NotAfterThese is nonempty, all chars. in [0] are noted, and no digit can begin
//   a number if it is immediately preceded by one of them, EVEN IF it is the
//   character just before the window begins (at StartPtr-1). (E.g. used to avoid
//   identifying a numeral inside a variable name. Note that unless NotAfterThese
//   is empty, numerals themselves should normally be in NotAfterThese, as otherwise
//   e.g. in a variable name "X12", the X negates the '1' but the '2' would be accepted.
// RETURNED: An Octet. .BX TRUE if a candidate number found; .BY is TRUE only if both:
//  (1) .BX is TRUE; and (2) no error was thrown in converting that no. to a double.
//  Whenever .BY is FALSE there will be an error message in .SY.
//  If .BX TRUE (irrespective of .BY), then .IX and .IY point to the first and last chars.
//  of the number, and .SX is its string form. If the no. is also valid (.BY TRUE),
//  .XX is the number, and the part of InStr that produced it is in .SX.
  public static Octet FindNumber(string InStr, int StartPtr, int EndPtr,
                         bool IgnoreInitialMinus, params string[] NotAfterThese)
  { Octet result = new Octet(0);
    // Deal with argument irregularities:
    if (EndPtr >= InStr.Length)EndPtr = InStr.Length-1; // simply adjust down an
                                                        //  oversized EndPtr.
    if (StartPtr < 0){result.SY = "negative StartPtr";  return result;}
    if (EndPtr < StartPtr){result.SY = "EndPtr < StartPtr";  return result;}
    string badguys = "";
    if (NotAfterThese.Length > 0) badguys = NotAfterThese[0];
    // Acceptable arguments, so set up structures.
    // Find the first digit in the region of interest ('window') within InStr:
    do
    { result.IX = InStr.IndexOfAny(DigitList, StartPtr, EndPtr-StartPtr+1);//Look for 0..9.
      if (result.IX == -1){result.BY = true; return result;} // No error, but no find also.
      if (result.IX==0 || badguys=="") break; // this find will do - no risk of negation.
      else if (badguys.IndexOf(InStr[result.IX-1]) == -1) break; //preceding char. does not negate
      // Find negated, because a negating character preceded it.
      StartPtr = result.IX + 1;
      if (StartPtr > EndPtr){result.BY = true; return result;} // No error, but no find also.
    } while (true);
    // There is definitely a number in this part of InStr, so catch all of InStr from
    //   this digit to the end ofthe window:
    char[] instr = InStr.ToCharArray(result.IX, EndPtr - result.IX + 1);
    StringBuilder number = new StringBuilder(); // No. to be returned.
    // If a neg. sign immediately precedes (and is in the window), insert it into number:
    if (!IgnoreInitialMinus && result.IX > StartPtr && InStr[result.IX-1] == '-')
    { number.Append('-'); result.IX--; }
    // Accumulate the whole-no. part of this number:
    int noend = 0;// Progressively increased pointer to end of no. (Dummy starting value
      // req'd. by compiler, but value is always set at least once after the 'if' below.)
    for (int i = 0; i < instr.Length; i++) // instr[0] guaranteed to be a digit.
    { if (char.IsDigit(instr[i])){number.Append(instr[i]); noend = i;} else break; }
    // Look for a decimal part:
    bool endnow = (noend >= instr.Length-2);//TRUE if there are less than 2 chars. left.
    if (!endnow)
    { if (instr[noend+1]!=DecimalPoint || !char.IsDigit(instr[noend+2])) endnow = true; }
    if (!endnow) // then above part no. is followed by a decimal portion:
    { noend++;  number.Append(instr[noend]); // add in the decimal point
      for (int i = noend+1; i < instr.Length; i++)
      { if (char.IsDigit(instr[i])){number.Append(instr[i]); noend = i;} else break; }
    }
    // We now have a complete no. in the form (e.g.) "123" or "123.45". We now must
    //  look for an exponential part. ('endnow' is reused, ignoring its prev. use.)
    endnow =  (noend >= instr.Length-2); // TRUE if there are less than 2 chars. left.
    if (!endnow && char.ToUpper(instr[noend+1]) != 'E') endnow = true;
    if (!endnow) // We have reached e.g. "12.34E" - is the 'E' an exp'l indicator?
    { int possnoend = noend+1; // possible future value of noend points to the 'E'.
      if (instr[noend+2] == '+' || instr[noend+2] == '-') possnoend++;
      // possnoend now points either to 'E' (no sign) or to the +/- of "E+"/"E-".
      if (possnoend==instr.Length-1 || !char.IsDigit(instr[possnoend+1])) endnow = true;
          // i.e. End now, if either the string ended prematurely, or no digit follows.
      if (!endnow) // then we have "E[+/-]" followed by at least one digit:
      { for (int i = noend+1; i <= possnoend; i++) number.Append(instr[i]);//add in "E[+]"
        for (int i = possnoend+1; i < instr.Length; i++)
        { if (char.IsDigit(instr[i])){number.Append(instr[i]); noend = i;} else break; }
      }
    } // All paths should now have correctly filled number and correctly set noend:
    result.BX = true; // a candidate no. found.
    result.SX = number.ToString();
    result.IY = result.IX + number.Length - 1;
    // Is it a valid no.?
    try { result.XX = double.Parse(result.SX);  result.BY = true; }
    catch (OverflowException)
    { result.BY = false;  result.SY = "overflow"; }
    catch {result.SY = "no. format error";}
    return result;
  }
// Find "true" or "false" (irrespective of case) in the window of InStr set by the
//  pointers. Any English letter, or digit, or '_', will nullify a possible find
//  (so that none of these will qualify: "falsehood", "true1", "_false").
// Returned: .B TRUE if a boolean found. If .B is TRUE, then .S is the find, in
//  capitals ("TRUE" or "FALSE"), and .I is where it starts. If .B is FALSE, then
//  if .S is empty there has been no error; otherwise .S holds an error message.
  public static Quad FindBoolean(string InStr, int StartPtr, int EndPtr)
  { Quad result = new Quad(false);
    int FPos = InStr.Length, TPos = FPos; // i.e. both beyond end of string: no-find vals.
    char[] idchars = IdentifierChars.ToCharArray();
    Duo pt = FindIf(InStr, "TRUE", idchars, StartPtr, EndPtr, 1);
    if (pt.X < -1)
    { result.S = "argument error (code: " + pt.X.ToString()+")"; return result; }
    else if (pt.X >= 0) TPos = pt.X;//TPos now points to a find, or else = InStr.Length.
     // No need to check for error the second time, as the same args. are passed:
    pt = FindIf(InStr, "FALSE", idchars, StartPtr, EndPtr, 1);
    if (pt.X >= 0) FPos = pt.X; // FPos now points to a find, or else = InStr.Length.
    if (TPos == FPos) return result; // no find.
    result.B = true;
    if (TPos < FPos){result.S = "TRUE";  result.I = TPos; }
    else {result.S = "FALSE";  result.I = FPos; }
    return result;
  }
// Wordwraps a string.
// INPUT: InStr will be searched backwards from char. (if any) at MaxLength, looking for
//  a natural break point; it will look back only to the char. at FirstBreakPoint; if none
//  found by then, the line simply breaks after MaxLength-1, the next line beginning
//  with an inserted hyphen. Counter is simply incremented by the fn. and returned in
//  .IX; it is useful in some repetitive uses of this fn., to keep count of lines produced.
//  Breakers is optional. If omitted, non-white-space breakers will be '-' and '/'.
//  Otherwise they will only be what you insert here. E.g. if you were using nos. in
//  exp. form ("1e-2") a lot, you might want to exclude '-', to keep no. parts together.
// RETURNED: .SX is what fits on the line; .SY is what is left over. (If the break is
//  at a white space, that space is lost; .SY begins after this space, but any earlier
//  neighbouring spaces survive.)
//  .BX is TRUE if there is a carryover (.SY nonempty). .IX is Counter+1. If a line break
//  control char. caused the break (see below), .IY is (int) <that char.>; otherwise, 0.
// If a '\n' or '\l' is found anywhere in the line from its start onwards, the line
//  breaks there, irrespective of FirstBreakPoint. (This is always checked for first.)
// ERROR: .BY is TRUE in every situation except when there is a silly argument; then
//  it is FALSE, and .SY holds the error message.
// BREAKPOINTS: any white space; and any char in Breakers (default as above). If the line
//  breaks at a line-breaking control char., any following control char (or space) will
//  not go into .SY.
  public static Octet WordWrap(string InStr, int MaxLength, int FirstBreakPoint,
                                                int Counter, params char[] Breakers)
  { Octet result = new Octet(0);
    // Trivial case - InStr fits into MaxLength:
    if (InStr.Length <= MaxLength)
    { result.SX = InStr; result.BY = true; result.IX = Counter+1; return result; }
    // Deal with errors:
    if (FirstBreakPoint < 2){result.SY = "FirstBreakPoint must be > 1"; return result;}
    if (FirstBreakPoint >= MaxLength)
    { result.SY = "FirstBreakPoint too large"; return result; }
    // No more errors possible.
    result.BY = true; result.IX = Counter+1;
    if (Breakers.Length == 0) Breakers = new [] {'-','/'}; // non-whitespace breakers.
    int len = MaxLength+1; if (len > InStr.Length) len = InStr.Length;
    char[] charr = InStr.ToCharArray(0, len); // no need to copy InStr further.
    // Check for line breakers:
    int breakpt = -1;
    for (int i = 0; i < charr.Length; i++)// look for line-breaking control chars.:
    { if (char.IsWhiteSpace(charr[i]) && !char.IsSeparator(charr[i]))
      { breakpt=i;  result.IY = (int) charr[i];   break; }
    }
    if (breakpt != -1) // deal with the premature break caused by the control char.:
    { result.SX = InStr._Extent(0, breakpt);
      // More than one line-breaker may follow, or a space. If so, trim it off the carry:
      int n = breakpt+1;
      if (n < charr.Length && char.IsWhiteSpace(charr[breakpt+1]))n = breakpt+2;
      result.SY = InStr._Extent(n, InStr.Length);
      result.BX = (result.SY != "");
      return result;
    }
    // There are now no line-break control chars. within MaxLength.
    // Nothing to do, if InStr already fits the line:
    if (InStr.Length <= MaxLength){result.SX = InStr; return result;}
    result.BX = true; // there is always a carryover now, and importantly, the length
                      // of charr is always at least MaxLength+1.
    breakpt = MaxLength; // default, if no breakpoint found.
    char ch = ' '; // dummy assignment.
    for (int i = MaxLength; i >= FirstBreakPoint; i--)
    { ch = charr[i];
      if (char.IsWhiteSpace(ch)){ breakpt = i; break; }
      else if (i < MaxLength && Array.IndexOf(Breakers, ch) >= 0)
      { breakpt = i+1; break; }
    }
    result.SX = InStr._Extent(0, breakpt);
    if (breakpt < MaxLength && char.IsWhiteSpace(ch)) breakpt++;
    result.SY = InStr._Extent(breakpt, InStr.Length);
    return result;
  }
  // Input a statement like "5:2-4,6;8-10" - one colon, one integer (called
  //  the 'type') before colon, list of integers ('instances') after colon
  //  delimited only by ',' or ';' (no distinction).
  //  Instances and types may be negative. Only allowed nondigits are
  //   ':', ',', ';', '-'. Spaces irrelevant (removed up front).
  // REF ARG: Repository is a matrix. if order_TypeInst, the order of indices
  //  is [Type, Inst]; otherwise it is [Inst, Type]. The corresponding array slot
  //  for an instance listed in InStr is incremented.
  // Dimensions of Repository MUST be (MaxType-MinType+1), (MaxInst.-MinInst.+1)
  //  or else an undignified pgm. crash will occur (no testing done here).
  // Indices will be offset by the two minima.
  // RETURNED: .B and .S respond to errors in the usual way.
  public static Quad ParseColonList(string InStr, int MinType, int MaxType,
    int MinInstance, int MaxInstance, bool order_TypeInst, ref int[,] Repository)
  { string instr = InStr._Purge();  Quad result = new Quad(false);  bool boo;
    int colon = instr.IndexOf(':');
    if (colon == -1){result.S = "no colon found"; return result;}
    int typ = instr._Extent(0, colon)._ParseInt(out boo);
    if (!boo){result.S = "cannot identify integer before colon"; return result;}
    if (typ < MinType || typ > MaxType)
    { result.S = "out-of-range no. before ':'"; return result;}
    string[] instances = instr._Extent(colon+1).Split(new [] {',', ';'});
    int dash, lo, hi=0;
    for (int i = 0; i < instances.Length; i++)
    { dash = instances[i].IndexOf('-',1);//ignore a leading '-' (must be a minus).
      if (dash == -1) dash = instances[i].Length;
      lo = instances[i]._Extent(0, dash)._ParseInt(out boo);
      if (boo)
      { if (dash == instances[i].Length) hi = lo;
        else hi = instances[i]._Extent(dash+1)._ParseInt(out boo); }
      if (!boo)
      { result.S = "cannot identify instance: '"+instances[i]+"'";return result;}
      if (lo < MinInstance || hi > MaxInstance)
      { result.S = "out-of-range instance: '"+instances[i]+"'";return result;}
      if (lo > hi)
      { result.S = "instance order wrong: '"+instances[i]+"'";return result;}
      for (int j = lo; j <= hi; j++)
      { if (order_TypeInst) Repository[typ - MinType, j - MinInstance]++;
        else                Repository[j - MinInstance, typ - MinType]++;
      }
    }
    result.B = true; return result;
  }
  // Input a statement like "2-4,6;8-10" - integers delimited either by ',' or by ';' (no distinction made).
  // Integers may be negative. Spaces irrelevant (removed up front).
  // REF ARG: if e.g. MinInstance is 0 and the integer 6 is in the list,
  //  Repository[6] will be incremented (its prior value not being referenced).
  // Indices will be offset by MinInstance. (e.g. if 1, above increments Rep.[7].)
  // Size of Repository MUST be (MaxInstance-MinInstance+1), or else crasho.
  // RETURNED: .B and .S respond to errors in the usual way.
  public static Boost ParseDelimitedList(string InStr, int MinInstance,
                                   int MaxInstance, ref int[] Repository)
  { string instr = InStr._Purge();  Boost result = new Boost(false);  bool boo;
    string[] instances = instr.Split(new [] {',', ';'});
    int dash, lo, hi=0;
    for (int i = 0; i < instances.Length; i++)
    { dash = instances[i].IndexOf('-',1);//ignore a leading '-' (must be a minus).
      if (dash == -1) dash = instances[i].Length;
      lo = instances[i]._Extent(0, dash)._ParseInt(out boo);
      if (boo)
      { if (dash == instances[i].Length) hi = lo;
        else hi = instances[i]._Extent(dash+1)._ParseInt(out boo); }
      if (!boo)
      { result.S = "cannot identify instance: '"+instances[i]+"'";return result;}
      if (lo < MinInstance || hi > MaxInstance)
      { result.S = "out-of-range instance: '"+instances[i]+"'";return result;}
      if (lo > hi)
      { result.S = "instance order wrong: '"+instances[i]+"'";return result;}
      for (int j = lo; j <= hi; j++) Repository[j - MinInstance]++;
    }
    result.B = true; return result;
  }
// The reverse of the above. Starting with bool array {T T F T T T F}, end up
//  with either "0,1,3,4,5" (AllowDashes = false) or "0-1,3-5".
  public static string BuildDelimitedList(bool[] bools, bool AllowDashes)
  { string result = "";
    bool lastT = false, dashadded = false; 
    for (int i=0; i<bools.Length; i++)
    { if (!AllowDashes)
      { if (bools[i]) result += i.ToString() + ','; }
      else
      { if (bools[i]) // we have 'T':
        { if (lastT) // 'T' is 2nd. or higher, in a sequence of 'T's:          
          { if (!dashadded){ result += "-"; dashadded = true;}}// do nil, if dash already there.
          else { result += i.ToString(); dashadded = false;}// 'T' after 'F' (or at start).
        }
        else // we have 'F':
        { if (lastT) // 'F' after 'T':
          { if (dashadded)result += (i-1).ToString();
            result += ","; }
          // do nothing if 'F' after 'F'.
      } } 
      lastT = bools[i];  
    }
    // if result ends in '-' or ',', there is unfinished business...
    if (result.Length > 0)
    { char c = result[result.Length-1]; 
      if (c=='-') result += (bools.Length-1).ToString(); 
      else if (c==',') result = result._CullEnd(1); 
    }
    return result;  
  }
// Overlay of above. In arr[], 0 is 'false' and anything else is 'true'.
  public static string BuildDelimitedList(int[] arr, bool AllowDashes)
  { bool[] arrb = new bool[arr.Length];
    for (int i=0; i<arr.Length; i++) if (arr[i] != 0) arrb[i] = true;
    return BuildDelimitedList(arrb, AllowDashes);
  }
//date  //time
// Try to translate a date, given all sorts of funny inputs. If fails, sets Success to FALSE 
//   (and returns 'DateTime.MinValue' as the date).  
// You may omit the year (if Yr_Mth_DMY has a default year), or both the year and the month (if Yr_Mth-DMY
//   has both). (Caveat for numerals-only forms: '1/2' will have year added on the end, so cannot be in
//   the form <year no.>/<month no.>'. '1' (standalone) will be taken as the day in all cases.
// INSTR: Characters that are not English letters or numerals are all ignored, except that they all act
//   as delimiters between valid substrings. A change of type (numeral <--> letter) also acts as a delimiter.
//   Hence all of: '2Jan05', '2 Jan 05', '2///Jan***05%%' will be accepted to provide the date 2 Jan. 2005.
//   (a) Month in numerals: taken as day-month-year unless indicated otherwise (see below). '2/1/05' --> 2 Jan 2005.
//   (b) Month in letters: Month or day may come first; but year is always last. Month names: Case is
//        not significant. Only the first 3 letters are checked against standard month names. If less than 
//        3 letters present, the first match applies; e.g. '2J05' returns month January, not June or July.
// Yr_Mth_DMY: 
//    [0] = Year to insert, if InStr omits the year. (If absent or invalid, omitted year raises an error.)
//    [1] = Month to insert, if InStr omits BOTH the year and the month. (If absent or invalid, omitted 
//            month raises an error.) With [0] and [1], the same formatting rules apply as for the corresp.
//            entries of InStr.
//    [2] = Order for interpreting numbers-only versions, like "1/2/34". Only three case-sensitive combinations
//            are allowed, with obvious meaning: 'DMY', 'MDY', 'YMD'. The default is 'DMY'.  
//     If you want to specify [2] but not[0] and [1], leave [0] and [1] as empty string (or as any nonsense). 
// Further formatting notes:
//   Day: Allowed to be followed by 'st','nd','rd','th' etc. - e.g. 5th.July 03' --> 5 July 2003.
//   Year: Must lie between 1900 and 2099, or between 0 and 99 ( in which case 2000 will be added on). 
  public static DateTime GetDate(string InStr, out bool Success, params string[] Yr_Mth_DMY)
  { Success = false; if (InStr=="") return DateTime.MinValue;
    char[] inchar = InStr.ToUpper().ToCharArray();   
    char ch, type = ' ', lasttype = '/';
    int omits = Yr_Mth_DMY.Length;  
    string omittedyear="", omittedmonth="", DMY="";
    List<string> substr = new List<string>();
    if (omits >= 1) omittedyear = Yr_Mth_DMY[0];  if (omittedyear == "") omittedyear = " ";
    if (omits >= 2) omittedmonth = Yr_Mth_DMY[1]; if (omittedmonth == "") omittedmonth = " ";
      // the above 2 must not be empty strings, or crasho later on.
    if (omits >= 3) DMY = Yr_Mth_DMY[2]; 
    string str = "", types = "", ordinal = "ST-ND-RD-TH"; // as in e.g. "1st."
  // Divide the string into substrings of contiguous letters or numerals:
    for (int ptr=0; ptr < inchar.Length; ptr++)
    { ch = inchar[ptr];
     // Get type of character:
      if (ch >= '0' && ch <= '9') type = '1'; // numeric character
      else if (ch >= 'A' && ch <= 'Z') type = 'A'; // letter
      else type = '/';  // non-alphanumeric character
      // Build strings accordingly:
      if (type == lasttype)
      { if (type == '1' || ( type == 'A' && str.Length < 3) ) str += ch; }//only take first 3 ltrs. of a word.
      else // type and lasttype are different:
      { if (lasttype != '/')
        { if (lasttype == '1' || ordinal.IndexOf(str) == -1)//number, or a word that is not 'st', 'nd'...
          { substr.Add(str);  types += lasttype; }
        }
        if (type == '/') str = ""; else str = ch.ToString(); 
      }  
      lasttype = type;
    }
    if (str != "") { substr.Add(str); types += type; }
    int typlen = types.Length;   
      // Remove the disallowed types that have less than three characters:
    if (typlen == 0 || types == "A") return DateTime.MinValue;
    if (typlen + omits < 3) return DateTime.MinValue; // must be able to pad out types with < 3 substrings.

     // Pad out short types to the full length:
    if (typlen == 1) // add on the month:
    { substr.Add(omittedmonth.ToUpper());  
      if (omittedmonth[0] <= '9') types += '1'; else types += 'A';  
      typlen++; // Will now be processed by the next 'IF':
    }
    if (typlen == 2) // add on the year:
    { substr.Add(omittedyear);  
      if (omittedyear[0] <= '9') types += '1'; else types += 'A';
      typlen++;
    }
    // All are now full-length types.
    // Remove disallowed types:
    if (types[2] == 'A' || types == "AA1") return DateTime.MinValue;
    // Only three types are left - '111', '1A1', 'A11'.
  // Get definitive day / month / year strings set up:
    string day="", month="", year="";   
    if (types == "111")
    { if      (DMY == "YMD") {day = substr[2]; month = substr[1]; year = substr[0]; }
      else if (DMY == "MDY") {day = substr[1]; month = substr[0]; year = substr[2]; }
      else                   {day = substr[0]; month = substr[1]; year = substr[2]; }//default = "DMY"
    }
    else if (types[0] == '1')  {day = substr[0]; month = substr[1]; year = substr[2]; }
    else                       {day = substr[1]; month = substr[0]; year = substr[2]; }
                 // 'types' not adjusted for these swaps, as it is never used again.
  //-----------------------------------------------------------
  // Enumerate the three strings:
    int dy=-1, mth=-1, yr=-1;
  // Enumerate the day:
    dy = day._ParseInt(0); // leave error-checking to the DateTime constructor, called below.
  // Enumerate the month:
    // Only 'month' can still be a string; so try to convert it to a number:
    if (month[0] >= 'A')
    { mth = ( ( MonthStrShort.IndexOf("/" + month) ) + 4) / 4; } // = 0, if name not found.
    else mth = month._ParseInt(0); // leave error-checking to the DateTime constructor, called below.
    // Enumerate the year, in full four-digit form, only allowing the range 1900 to 2099:
    yr = year._ParseInt(-1);
    if (yr < 0 || yr > 2099) return DateTime.MinValue;
    if (yr > 99 && yr < 1900) return DateTime.MinValue;
    if (yr < 100) yr += 2000;
   // The Grand Test...
    DateTime result;
    try { result = new DateTime(yr, mth, dy);  Success = true; }
    catch { result = DateTime.MinValue; }
    return result;
  }

// Try to translate a time, given all sorts of funny inputs. If succeeds, sets
//  .B to TRUE and supplies the time string in .S, and the time as an integer in .I
//  (.I always being in 24 hour format).
// If fails, sets .B to FALSE, and .I to an error number (.S empty).
// MODEL will be trimmed, but internal blanks will be left in place. Model
//  must contain either three or four '%' place-holders, and have the format:
//  [prefix] [%]% [infix] %% [suffix] [AM <or> am <or> PM <or> pm]. Strings prefix, 
//  infix and suffix may be anything (except no '%' allowed), and will be faithfully 
//  reproduced as is (incl. any blanks; except that Model is trimmed before parsing). 
//  If only three '%', no leading zero will be added for times with single-digit hours,
//  IF the time is to be in AM/PM format. (In 24 hr. format there will always be four digits.)
//  'AM' or 'PM' at the end signify to use 12-hour format. The case of the am/pm 
//  qualifier will be that given in Model.
// Examples of InStr: These are correct and equivalent: 0700, 700, 7.00, 7, 7a, 7AM.
//  As are these: 1900, 19, 19.00, 7P, 7 pm. These are equivalent: 1910, 19.10,
//  1910 hrs, 19h 10m, 19 hrs 10 mins, 19, 7.10 pm.  One or two numerals only: taken as whole hours.
// If a time is in the format 7 or 7.00, it is taken as AM unless 'P' or 'PM' follows.
// Midnight as input: 0 and 2400 are taken as midnight. 12 AM and 12 PM are not officially
//  defined, but this function regards input 12 PM as midday, and 12 AM as midnight.
// With all these inputs, everything is ignored except digits and any final 'a','am','p','pm' 
//  (case-insensitive). So input "The7cat0sat0on the mat" would register as 7 AM.
  public static Quad TimeConform (string InStr, string Model)
  { Quad result = new Quad(false);   if (InStr=="") { result.I = 100; return result; }
   // DISCERN THE TIME    
    string ss, instr = InStr._Purge().ToUpper(); // All caps., with blanks removed (and any other chars #0 to #32).
   // Extract all digits:
    string digs, undigs = JS.CharsNotInList(instr, Digits.ToCharArray(), out digs);
   // Convert the digits to four-digit form: 
    if (digs.Length == 0 || digs.Length > 4) { result.I = 200; return result; }
    if (digs.Length <= 2) digs += "00"; // '7' --> '700', '19' --> '1900'.
    if (digs._Last(2)._ParseInt(60) >= 60) { result.I = 300; return result; }
    int time = digs._ParseInt(2400);
    if (time == 2400) time = 0; else if (time > 2400) { result.I = 400; return result; }
    // if could be interpreted as AM, check 'undigs' for 'A' or 'AM' as final char(s):
    char ampm = ' ';  
    if (undigs._Last() == 'P' || undigs._Last(2) == "PM") ampm = 'p';
    else if (undigs._Last() == 'A' || undigs._Last(2) == "AM") ampm = 'a';
    if (time >= 1200 && time < 1300 && ampm == 'a') time -= 1200;
    else if (time < 1200 && ampm == 'p') time += 1200;
    result.I = time;
    // PARSE THE MODEL
    string model = Model.Trim();    if (model == "") { result.I = 500; return result; }
    int n = model._CountChar('%');   if (n < 3 || n > 4) { result.I = 600; return result; }
    char[] choo = model.ToCharArray();
    int[] Xpos = {-1,-1,-1,-1};
    int Xcnt = 4-n; // 0, if 4x'%', 1 if 3x'%'.
    for (int i=0; i<choo.Length; i++)
    { char c = choo[i];
      if (c == '%')
      { if (Xcnt < 4 ) Xpos[Xcnt] = i; 
        Xcnt++;
    } }  
    if (n==3) Xpos[0] = Xpos[1];
    if (Xpos[1] > Xpos[0]+1 || Xpos[3] > Xpos[2]+1) { result.I = 700; return result; }    
    string prefix = model._Extent(0, Xpos[0]),  suffix = model._Extent(Xpos[3]+1);
    string infix = model._FromTo(Xpos[1]+1, Xpos[2]-1);
    char fmt12hrs = ' '; // for 12-hour format, will be 'M' or 'm', a/c user's case.
    ss = suffix._Last(2).ToUpper();   if (ss != "") ss = ss.ToUpper(); 
    if (ss == "AM" || ss == "PM")
    { fmt12hrs = suffix._Last();  suffix = suffix._CullEnd(2); }
   // CONFORM THE TIME 
    if (fmt12hrs != ' ')
    { if (time < 1200) ss = "am";  else ss = "pm";
      if (fmt12hrs == 'M') ss = ss.ToUpper(); 
      suffix += ss;
    }
    if (ss != "") // 12-hour format: 
    { if (time < 100) time += 1200; // e.g. 0039 hrs. --> 12.39 AM.
      else if (time >= 1300) time -= 1200; // eg. 1301 hrs. --> 1.01 PM.
    }
    string timestr = time.ToString();
      // If 24 hr format, pad always to four digits:
    if (fmt12hrs == ' ') while (timestr.Length < 4) timestr = '0' + timestr;
    else // 12 hr format: guaranteed to have at least 3 digits, from earlier code.
    { if (Xpos[1] > Xpos[0] && timestr.Length == 3) timestr = '0' + timestr; }
   // BUILD THE OUTPUT STRING
    result.S = prefix + timestr._Extent(0, timestr.Length-2) + infix + timestr._Last(2) + suffix;
    result.B = true;   return result;
  }
// Develop a unique nonzero positive integer - not a random number, but one which is directly derived from system time,
//  with quantum of 1 millisec and with a cycle of length Int32.MaxValue * 1 millisec, i.e. about 24.86 days. Intended
//  to provide numbers guaranteed to be different during one program run, and where calls on this function never occur
//  as close together as 2 millisecs. If you want uniqueness closer than that, best is to have some class have a private
//  static field NextID, and for its static constructor to set that to e.g. JS.UniqueInteger() / 2 + 1, and then have 
//  this incremented with the creation of each new class object. (Presumably the static constructor won't be called more
//  frequently than every 2 milliseconds!
  public static int UniqueInteger()
  { int result;
    do
    { result = (int) ( JS.Tempus('L', false) & (long) 0x7fffffff ); // a zero or positive number, 0 to Int32.MaxValue
    } while ( result == 0); // Every 24.86 days this algorithm would return 0.
    return result;
  }
// This overload gives a LONG integer result, which has the advantage of a shorter quantum (1 microsecond) and
//  no cycling until 290 more centuries have passed.
  public static long UniqueLongInteger()
  { return JS.Tempus('C', false);
  }

/// <summary>
/// <para>This overload allows for standalone numerals (like '21'). If the string can be parsed to an integer, then DefMonth and DefYear
/// will be used to generate the full date (in which case they must be valid). If not, DefMonth is ignored and InStr is passed aong with 
/// DefYear to the original overloaded method.</para>
/// </summary>
  public static DateTime DateGuess(string InStr, int DefMonth, int DefYear, out Boost Outcome)
  { int day = InStr._ParseInt(-1);
    if (day == -1) return DateGuess(InStr, DefYear, out Outcome); // usually means there are month letters there as well.
    // else parses as a pure number:
    if (DefMonth < 1 || DefMonth > 12) { Outcome = new Boost(false, "default month no. is outside range 1 to 12");  return DateTime.MinValue; }
    int p = (DefMonth-1)*10,  q = MonthStr.IndexOf('/', p+1);
    InStr = InStr._Purge() + ' ' + MonthStr._Between(p, q); 
    return DateGuess(InStr, DefYear, out Outcome);
  }
/// <summary>
/// <para>Guess the date, given a string in which the month is expressed in letters (not a month no.), and the year (if present) is at
/// the end. Examples of allowed inputs (for 8th. June, 2003): "8ju", "ju8", "8ju03", ju8 03" (no day name allowed).</para>
/// <para>If no year is given, the default 'DefYear' will be used. (If this is negative, then an error is returned if no year is supplied,
/// or if the supplied year has less than 4 digits. If DateTime.MinValue is being used by calling code, 0 is the value to use to get "0001", 
/// as -1 would just give "1".)
/// If the year component of InStr has less than 4 digits, missing digits will be supplied by DefYear; e.g. if DefYear is 1987, 
/// "8jun3" --> year 1983, "8jun03" --> year 1903.</para>
/// <para>POINTS:</para>
/// <para>-- The letters must be the first char(s) of a true month name - not an abbrevn. (E.g. not "jn".) Where there is ambiguity 
/// (e.g. for "j") the earliest month of the year that would qualify is taken.</para>
/// <para>-- If the order is 'day-month-year', then the month component, being letters, is a sufficient delimiter between the two numbers, so
/// no further delimiters (spaces or punctuation marks) are needed; though if present, they are tolerated.</para>
/// <para>-- If the order is 'month-day-year', then some delimiting (even just a space) must exist between the two numbers.</para>
/// <para>-- Non-alphanumeric chars. are allowed between fields or at the end, but NOT at the start of InStr.</para>
/// <para>ERRORS return DateTime.MinValue; Outcome.B is FALSE and Outcome.S is the error message.</para>
/// </summary>
  public static DateTime DateGuess(string InStr, int DefYear, out Boost Outcome)
  { Outcome = new Boost(false);
    InStr = InStr.ToLower();
    int p, errNo = 0;
    int dayStart, dayEnd, mthStart, mthEnd, yearStart = -1;
    int day=0, month=0, year = 0;
   // Identify the segments:
    dayStart = InStr._IndexOfAny(Digits).X;   if (dayStart == -1) { errNo = 10; goto Oopsie; }
    dayEnd = InStr._IndexOfNoneOf(DigitList, dayStart) - 1;
    if (dayEnd == -2) dayEnd = InStr.Length-1;
    if (dayStart == 0) // then the order is Day - Month [ - year]:
    { mthStart = dayEnd+1; // may be a punctuation mark or space, but the 'month' section will sort that out.
      p = InStr._IndexOfAny(DigitList, mthStart).X;
      if (p == -1) mthEnd = InStr.Length-1; // leave yearStart as -1.
      else { mthEnd = p-1;  yearStart = p; }
    }  
    else // the order is Month - Day [ - year]:
    { mthStart = 0;  mthEnd = dayStart-1;
      if (dayEnd < InStr.Length-1) yearStart = dayEnd+1;  // otherwise leave yearStart as -1.
    }  
   // Collect the segments:
    string dayStr, mthStr, yearStr;
    dayStr = InStr._FromTo(dayStart, dayEnd);
    mthStr = InStr._FromTo(mthStart, mthEnd);
    mthStr = mthStr._PurgeExceptRange(0, -1, 'a', 'z'); // we converted all chars. to lower case at the start
    if (mthStr == "") { errNo = 20; goto Oopsie; }
    if (yearStart == -1) yearStr = DefYear.ToString();
    else yearStr = InStr._Extent(yearStart);
    yearStr = yearStr._PurgeExceptRange(0, -1, '0','9');
    p = yearStr.Length;
    if (p < 4)
    { if (DefYear < 0) { errNo = 30; goto Oopsie; } // If DefYear is negative, then a four-digit year must be in the date.
      string ss = DefYear.ToString();
      while (ss.Length < 4) ss = '0' + ss;
      yearStr = ss._Extent(0, 4-p) + yearStr;
    }
   // Turn them into numbers:
    day = dayStr._ParseInt(0);
    if (day < 1 || day > 31) { errNo = 40; goto Oopsie; } // Doesn't pick up e.g. Feb 31st.
    p = MonthStrLo.IndexOf("/" + mthStr); // mthStr must contain at least one letter.
    if (p == -1) { errNo = 50; goto Oopsie; }
    month = 1 + p / 10;
    year = yearStr._ParseInt(0); // no problem if yearStr is empty (in which case 0 is returned).
    if (year < 1 || year > 9999) { errNo = 60; goto Oopsie; } // these are the years contained in DateTime.MinValue and DateTime.MaxValue
   // TRY TO CONVERT the day, month, year numbers into a DateTime object:
    DateTime result = DateTime.MinValue; // dummy value; the bl. compiler protests if omitted.
    try
    { result = new DateTime(year, month, day); 
      Outcome.B = true;
    }
    catch
    { errNo = 100; } // the only values failing the prior tests would be days 29 to 31 beyond end of given month in given year.
    if (errNo != 0) goto Oopsie;
  // SUCCESS: 
    return result;
  // FAILURE:
  Oopsie:
    switch (errNo)
    { case 10:  Outcome.S = "no digits found"; break;
      case 20:  Outcome.S = "no letters found, so month component not identified"; break;
      case 30:  Outcome.S = "year component required but not found"; break;
      case 40:  Outcome.S = "day no. outside range 1 to 31"; break;
      case 50:  Outcome.S = "month name (or its abbrevn.) not identified"; break;
      case 60:  Outcome.S = "year must lie between 0 and 9999"; break;
      case 100: 
        p = (month-1)*10;  int q = MonthStr.IndexOf('/', p+1);
        Outcome.S = MonthStr._Between(p, q) + " has no day " + day.ToString(); 
        if (month == 2 && day == 29) Outcome.S += " in year " + year.ToString();
        break;
    }
    return DateTime.MinValue;  
  }

// Translate DateStr as interpreted using Model. Model and DateStr must have the same length.
// Model must be some combination of: "yy" or "yyyy"; "m" or "mm"; "d" or "dd" (case-insensitive).
// In the case of months and days, a single letter means that no leading zero is nec. present for 1 to 9.
// (But it is allowed.) Some silly errors (e.g. "yyy" in model) not detected, but return nonsense.
// Other chars. may be in Model, but corresp. DateStr chars. will be ignored. E.g. - Model = "mm/dd/yyyy"
// with DateStr "11/02/1999" is interpreted as 2nd. November, 1999, as with "m/d/yy" modelling "11/2/99". 
// (If the model is "yy", the year is taken as being 20xx.) 
// RETURNED: out DMY: [0] = day, [1] = month, [2] = year (e.g. 2003, not 3). If error, [0] and [1] are zero, 
//  and [2] has the error number as a negative number. The DateTime object is as interpreted, unless
//  error detected, in which case it returns as Now.
// DATE LIMITS: Year must lie between 1 AD and 2099 AD. (Changing the latter limit is no problem; but
//  changing the former will introduce programming problems below.)
  public static DateTime ParseDateStr(string DateStr, string Model, out int[] DMY)
  { DMY = new int[3];    DateTime result = DateTime.Now;     
    if (Model == "" || DateStr == "") { DMY[2] = -10; return result; }
    Model.ToLower(); 
    if (DateStr.Length < Model.Length) { DMY[2] = -20; return result; }// extra length in Model is ignored.
        // (if 'm' and/or 'd' single, and represent 2-digit nos. in DateStr, DateStr will always be longer.)
    // Deal with single 'm' and single 'd' at this stage:
    int DPtr = Model.IndexOf('d'), MPtr = Model.IndexOf('m'); 
    if (DPtr==-1 || MPtr==-1) { DMY[2] = -30; return result; }
    string[] dormstr = new string[2];  int[] dorm = new int[2];
    // Make adjustments from end of string towards front:
    if (DPtr < MPtr) { dorm[0] = DPtr;  dorm[1] = MPtr;  dormstr[0] = "d";  dormstr[1] = "m"; } 
    else { dorm[0] = MPtr;  dorm[1] = DPtr;  dormstr[0] = "m";  dormstr[1] = "d"; }
    for (int i=0; i<2; i++)
    { if (Model.IndexOf(dormstr[i]+dormstr[i]) == -1) // a single 'd' or 'm' in Model:
      { Model = Model.Insert(dorm[i], dormstr[i]); // to preserve length for next loop, if both 'm' and 'd' single
        if (i==0) dorm[1]++; // adjust next ptr. for this string enlargement.
        if (dorm[i] == DateStr.Length-1 || JS.Digits.IndexOf(DateStr[dorm[i]+1]) == -1)
        { DateStr = DateStr.Insert(dorm[i], "0"); // DateStr.Length guaranteed  >= Model.Length.
    } } }
    // Now 'm' and 'd' have been replaced by 'mm' and 'dd', and in DataStr corresponding 'n' by '0n'.
    string YStr="", MStr="", DStr="";  int ptr, n, Y,M,D;
    // Develop years string:
    ptr = Model.IndexOf("yyyy");      
    if (ptr >= 0) YStr = DateStr._Extent(ptr, 4);
    else { ptr = Model.IndexOf("yy"); if (ptr >= 0) YStr = "20" + DateStr._Extent(ptr, 2); }
    // Develop months and days strings:
    DPtr = Model.IndexOf('d');  MPtr = Model.IndexOf('m');//must repeat, as above may have changed them.
    MStr = DateStr._Extent(MPtr, 2); DStr = DateStr._Extent(DPtr, 2); 
    // Convert to integers, detecting errors in the process:
    bool noproblems;  int errno = 0;
    Y = YStr._ParseInt(out noproblems); if (!noproblems) errno = -100; 
    M = MStr._ParseInt(out noproblems); if (!noproblems) errno = -110; 
    D = DStr._ParseInt(out noproblems); if (!noproblems) errno = -120; 
    if (errno == 0) 
    { if (Y <= 0 || Y > 2099) errno = -200;
      else if (M < 1 || M > 12) errno = -210;
      else if (D < 1 || D > 31) errno = -220;
    }
    if (errno == 0) // check that the given month's no. of days is not exceeded:
    { n = DateTime.DaysInMonth(Y,M);  if (D > n) errno = -300; }       
    if (errno == 0)
    { DMY[0] = D;  DMY[1] = M;  DMY[2] = Y;  result = new DateTime(Y, M, D); }
    else DMY[2] = errno;
    return result;       
  } 
  /// <summary>
  /// Given any date, returns an integer of the 6-to-9-digit form [Y[Y[Y]]]YMMDDN, the last - N - being the day no. (0 for Sunday,
  /// 6 for Saturday). E.g. Saturday 7th. November, 2009 ( = today) --> 200911076; DateTime.MinValue --> 000101011 (apparently it 
  /// was a Monday).
  /// </summary>
  public static int DateToInteger(DateTime DT)
  { int yr = DT.Year, mth = DT.Month, dy = DT.Day, dno = (int) DT.DayOfWeek;
    return yr * 100000 + mth * 1000 + dy * 10 + dno;
  }
  /// <summary>
  /// Requires an integer representation of a date, taking the 6-to-9-digit form [Y[Y[Y[Y]]]]MMDDX, the last ('X') being irrelevant. (It is there
  /// for consistency with the inverse method, 'DateToInteger(.)'.) E.g. Saturday 7th. November, 2009 would be represented by 20091107X; 
  /// DateTime.MinValue by 00010101X. This method returns the DateTime object, if successful, or else DateTime.MinValue (with Outcome FALSE).
  /// </summary>
  public static DateTime DateFromInteger(int Int, out bool Outcome)
  { Outcome = false;
    if (Int < 101011) return DateTime.MinValue;
    int yr = Int / 100000, Remains = Int % 100000;
    int mth = Remains / 1000; Remains = Remains % 1000;
    int day = Remains / 10;
    DateTime result;
    try { result = new DateTime(yr, mth, day);  Outcome = true; }
    catch { result = DateTime.MinValue; }
    return result;
  }

  /// <summary>
  /// <para>PathName and FileName will be concatenated, and then directory delimiters of the 4 different allowed types ("\", "\\", "/", "//")
  /// will all be replaced by the argument Delimiter. One of PathName and FileName may be empty, as long as the nonempty
  /// partner has the full path and name. If PathName does not end in a delimiter, one will be added on (unless FileName is empty).</para>
  /// <para>Delimiter: the delimiter that will divide between subdirectories in the returned object; the usual choices would be "\", "\\" or "/".
  /// <para>CheckValidity: If TRUE, the file name and path will be checked for valid syntax - BUT there is no check for the existence
  /// of the drive, path or file name on the host computer.</para>
  /// <para>RETURNED: If CheckValidity is FALSE, returned .B is always TRUE, and .S holds the full path and file name with delimiters conformed. 
  /// If CheckValidity is TRUE and the name tests as having valid syntax, the same is returned. If the test fails, .B is FALSE and .S holds 
  /// the error message.</para>
  /// <para>USE THE OVERLOAD IF you want to get hold of the System.IO.FileInfo instance created internally during this method,
  /// for example to find out if the file actually exists (as this method only tests for name syntax).</para>
  /// </summary>
  public static Boost FileNameConform(string PathName, string FileName, string Delimiter, bool CheckValidity)
  { FileInfo FileInform = null;
    return FileNameConform(PathName, FileName, Delimiter, CheckValidity, ref FileInform);
  }  
  /// <summary>
  /// <para>PathName and FileName will be concatenated, and then directory delimiters of the 4 different allowed types ("\", "\\", "/", "//")
  /// will all be replaced by the argument Delimiter. One of PathName and FileName may be empty, as long as the nonempty
  /// partner has the full path and name. If PathName does not end in a delimiter, one will be added on (unless FileName is empty).</para>
  /// <para>Delimiter: the delimiter that will divide between subdirectories in the returned object; the usual choices would be "\", "\\" or "/".
  /// <para>CheckValidity: If TRUE, the file name and path will be checked for valid syntax - BUT there is no check for the existence
  /// of the drive, path or file name on the host computer.</para>
  /// <para>FileInform: Usually would enter with the value NULL, but will return as a valid System.IO.FileInfo object.</para>
  /// <para>RETURNED: If CheckValidity is FALSE, returned .B is always TRUE, and .S holds the full path and file name with delimiters conformed. 
  /// If CheckValidity is TRUE and the name tests as having valid syntax, the same is returned. If the test fails, .B is FALSE and .S holds 
  /// the error message.</para>
  /// <para>Use the returned FileInform if you want e.g. to test whether this file actually exists. USE THE OVERLOAD IF you don't need the 
  /// System.IO.FileInfo instance 'FileInform'.</para>
  /// </summary>
  public static Boost FileNameConform(string PathName, string FileName, string Delimiter, bool CheckValidity, ref FileInfo FileInform )
  { Boost result = new Boost(true);
    // Standardize subdirectory delimiters to the supplied one:
    char tempDelim = JS.FirstPrivateChar; 
    string tempDelimStr = tempDelim.ToString();
    // Joint path name and file name:
    string pathName = PathName.Trim(), fileName = FileName.Trim();
    string fullName = pathName;
    if (pathName != "" && fileName != "" && (@"\/:").IndexOf(fullName[fullName.Length-1]) == -1)
    { fullName += tempDelim; } // No added delimiter if either string is empty, or if there is one already at the end of the path name.
    // join PathName to FileName, and do the substitutions:
    fullName += fileName;
    fullName = fullName.Replace("//", tempDelimStr);     fullName = fullName.Replace('/', tempDelim);
    fullName = fullName.Replace(@"\\", tempDelimStr);    fullName = fullName.Replace(@"\", tempDelimStr);
    fullName = fullName.Replace(tempDelimStr+tempDelimStr, tempDelimStr); // would mainly occur if path name ended in a delimiter and
       // file name began with a delimiter.
    fullName = fullName.Replace(tempDelimStr, Delimiter);
    if (CheckValidity)
    { result.B = false;
      try
      { FileInform = new FileInfo(fullName); 
        result.B = true;
      }
      catch (Exception e) 
      { result.S = e.Message; } // Gives the .NET message for the 6 possible exceptions raised by the FileInfo creation.
    }
    // Set up the return:
    if (result.B) result.S = fullName; // otherwise leave the error message in .S
    return result;
  }

// InArr should contain only the values in Allowed. If any others, Default is substituted.
// NB - No trimming or cleaning occurs, either of InArr elements or of Allowed elements!
// RETURNED: .B TRUE if no errors; otherwise .S is a comman-delimited list of error entries (base 0) -
//  e.g. "0, 2, 3-6, 20" - no preceding text. .I is error indicator. No error --> 0; argument errors 
//  --> negative values: -1 = empty InArr, -2 = empty Allowed. Values not in allowed --> no. of such.
  public static Quad ConformStrArray(ref string[] InArr, string[] Allowed, string Default)
  { Quad result = new Quad(false);  
    if (InArr == null || InArr.Length == 0){ result.I = -1; return result; }
    if (Allowed == null || Allowed.Length == 0) { result.I = -2; return result; }
    bool gottim;   var unravished = new bool[InArr.Length];
    for (int i=0; i < InArr.Length; i++)
    { gottim = false;
      for (int j=0; j < Allowed.Length; j++) { if (InArr[i] == Allowed[j]) { gottim = true; break; } }
      if (!gottim) { InArr[i] = Default; unravished[i] = true;  result.I++; }
    }
    result.B = (result.I == 0);
    if (!result.B)
    { result.S = BuildDelimitedList(unravished, true); }// "TTTFT" --> "0-2,4". 'true' = allow dashes.
    return result;    
  }
  
// Returns -1 if no find, -2 if arguments faulty (though excessive To ptr allowed), otherwise the find locn.
  public static int SBIndexOf(ref StringBuilder sb, char searchchar, params int[] FromTo)
  { int sblen = sb.Length, FromPtr, ToPtr;  if (sblen == 0) return -2;
    if (FromTo.Length >= 2) ToPtr = FromTo[1]; else ToPtr = sblen - 1;
    if (FromTo.Length >= 1) FromPtr = FromTo[0]; else FromPtr = 0;
    if (ToPtr >= sblen) ToPtr = sblen-1 ;
    if (FromPtr < 0 || FromPtr > ToPtr) return -2;
    for (int i=FromPtr; i <= ToPtr; i++){ if (sb[i] == searchchar) return i; }
    return -1;
  }

// Returns in .X: -1 if no find, -2 if parameters faulty (though excessive To ptr allowed), otherwise the find locn.
// Returns in .Y, if a find: the index in SEARCHCHARS (NOT in sb) of the found char. (otherwise -1).
  public static Duo SBIndexOfAny(ref StringBuilder sb, char[] searchchars, params int[] FromTo)
  { Duo result = new Duo(-1, -1);
    int sblen = sb.Length, sclen = searchchars.Length, FromPtr, ToPtr;  if (sblen == 0) { result.X = -2; return result; }
    if (FromTo.Length >= 2) ToPtr = FromTo[1]; else ToPtr = sblen - 1;
    if (FromTo.Length >= 1) FromPtr = FromTo[0]; else FromPtr = 0;
    if (ToPtr >= sblen) ToPtr = sblen-1 ;
    if (FromPtr < 0 || FromPtr > ToPtr) { result.X = -2; return result; }
    char ch;
    for (int i=FromPtr; i <= ToPtr; i++)
    { ch = sb[i];
      for (int j=0; j < sclen; j++) { if (ch == searchchars[j]) { result.X = i;  result.Y = j;  return result; } }
    }  
    return result; // which would be (-1,-1) - no find.
  }

//==================================================================
//5. METHODS NOT PRIMARILY INVOLVING STRINGS
//------------------------------------------------------------------
 /// <summary>
 /// <para>Given a length (of array or string), and two values - either 'from' and 'to' pointers, or 'from' pointer and 'extent' -
 /// RETURNS valid ('from', 'to', 'extent'), OR an indicator of error: (-1, -1, -1).</para>
 /// ARGUMENTS: 'Limits' can be null or empty, single-valued or multi-valued. The first value is always the 'from' pointer;
 /// the second is 'extent' if 'isFromExtent' is TRUE, or is the 'to' pointer if 'isFromExtent' is FALSE.
 //  Third or higher elements in 'Limits' are ignored.
 /// <para>RETURN FOR VALID VALUES: ('from', 'to', 'extent'). </para>
 /// RETURN FOR SPECIAL SITUATIONS: (1) InLen &lt;= 0: returns (-1,-1,-1). (2) Limits NULL or EMPTY: returns (0, InLen-1, InLen).  
 /// (3) Limits has a SINGLE VALUE, 'from': (from, Inlen-1, InLen - from). (4) 'from' &lt;0: corrected to 0. 
 /// (5) 'From' >= InLen: returns (-1,-1,-1). (6) 'to'/'extent' go beyond end of string: corrected to end of string. 
 /// (7) Corrected 'to' &lt; 'from', OR 'extent' &lt; 1: returns (-1,-1,-1).
 /// </summary>
  public static Trio SegmentData(int InLen, bool isFromExtent, params int[] Limits)
  { if (InLen <= 0) return new Trio(-1, -1, -1);
    if (Limits._NorE()) return new Trio(0, InLen-1, InLen);
    int fromptr = Limits[0];   if (fromptr < 0) fromptr = 0; 
    else if (fromptr >= InLen) return new Trio(-1, -1, -1);
    int extent = InLen - fromptr; // always >= 1
    if (Limits.Length > 1) 
    { int extent0 = extent; // always >= 1
      extent = Limits[1]; if (!isFromExtent) extent -= fromptr - 1; 
      if (extent <= 0) return new Trio(-1, -1, -1);
      if (extent > extent0) extent = extent0;
    }
    int toptr = fromptr + extent - 1;
    return new Trio(fromptr, toptr, extent);
  } 
// Complement a boolean - useful occasionally for bools with horribly long names.
// Note that you must use 'ref' before the bool in calling this, and that not only is
// the bool complemented in situ but also the value is returned by this method.
  public static bool Complement(ref bool TheBool)
  { TheBool = !TheBool;  return TheBool;
  }
///<summary>
///Return the current system time as one of: minutes, seconds, millisecs, microsecs.
/// Arg. case unimportant, but a wrong char. returns -1.  The awful char. arg name is to
/// remind one of meanings of MSLC when the class hint pops up. One of M,S,L,C required.
/// If SinceLastCall is FALSE, the absolute time is returned; if TRUE, time since the
/// last call of Tempus(..) baseticks is returned. (Exception: the first-ever call, if
/// has SinceLastCall TRUE, will retrieve the time since the first use of any method
/// in the whole class JS). Class JS field baseticks is reset by every call to this method.
/// NB!! If more than one part of your program is using this fn., a 'race condition' may
/// result if you use with 'SinceLastCall' TRUE.
///</summary>
  public static long Tempus(char MSLC_min_sec_milli_micro, bool SinceLastCall)
  { jsDateTime = DateTime.Now;
    long ticks = jsDateTime.Ticks, tocks;
    if (SinceLastCall) tocks = ticks - baseticks;  else tocks = ticks;
    baseticks = ticks; // In all cases, JS class field baseticks is always reset.
    switch (MSLC_min_sec_milli_micro)
    { case 'M':  case 'm': return tocks / 600000000L; // minutes since 1 AD.
      case 'S':  case 's': return tocks / 10000000L; // seconds since 1 AD.
      case 'L':  case 'l': return tocks / 10000L; // milliseconds since 1 AD.
      case 'C':  case 'c': return tocks / 10L; // microseconds since 1 AD.
      default: return -1L;
    }
  }

// ====== COLOUR-HANDLING FUNCTIONS =========

  public static string[] StandardColourNames = null;
  public static Dictionary<string, Trilobite> ColourDic = null;

  public static byte[] RR_StandardClr = null,  GG_StandardClr = null, BB_StandardClr = null;
  public static void InitializeColourNames()
  { if (StandardColourNames != null) return;
    StandardColourNames = new []
    { "ALICEBLUE","ANTIQUEWHITE","AQUA","AQUAMARINE","AZURE","BEIGE","BISQUE","BLACK","BLANCHEDALMOND","BLUE","BLUEVIOLET","BROWN",
      "BURLYWOOD","CADETBLUE","CHARTREUSE","CHOCOLATE","CORAL","CORNFLOWERBLUE","CORNSILK","CRIMSON","CYAN","DARKBLUE","DARKCYAN",
      "DARKGOLDENROD","DARKGRAY","DARKGREEN","DARKKHAKI","DARKMAGENTA","DARKOLIVEGREEN","DARKORANGE","DARKORCHID","DARKRED","DARKSALMON",
      "DARKSEAGREEN","DARKSLATEBLUE","DARKSLATEGRAY","DARKTURQUOISE","DARKVIOLET","DEEPPINK","DEEPSKYBLUE","DIMGRAY","DODGERBLUE",
      "FIREBRICK","FLORALWHITE","FORESTGREEN","FUCHSIA","GAINSBORO","GHOSTWHITE","GOLD","GOLDENROD","GRAY","GREEN","GREENYELLOW",
      "HONEYDEW","HOTPINK","INDIANRED","INDIGO","IVORY","KHAKI","LAVENDER","LAVENDERBLUSH","LAWNGREEN","LEMONCHIFFON","LIGHTBLUE",
      "LIGHTCORAL","LIGHTCYAN","LIGHTGOLDENRODYELLOW","LIGHTGRAY","LIGHTGREEN","LIGHTPINK","LIGHTSALMON","LIGHTSEAGREEN","LIGHTSKYBLUE",
      "LIGHTSLATEGRAY","LIGHTSTEELBLUE","LIGHTYELLOW","LIME","LIMEGREEN","LINEN","MAGENTA","MAROON","MEDIUMAQUAMARINE","MEDIUMBLUE",
      "MEDIUMORCHID","MEDIUMPURPLE","MEDIUMSEAGREEN","MEDIUMSLATEBLUE","MEDIUMSPRINGGREEN","MEDIUMTURQUOISE","MEDIUMVIOLETRED",
      "MIDNIGHTBLUE","MINTCREAM","MISTYROSE","MOCCASIN","NAVAJOWHITE","NAVY","OLDLACE","OLIVE","OLIVEDRAB","ORANGE","ORANGERED",
      "ORCHID","PALEGOLDENROD","PALEGREEN","PALETURQUOISE","PALEVIOLETRED","PAPAYAWHIP","PEACHBUFF","PERU","PINK","PLUM","POWDERBLUE",
      "PURPLE","RED","ROSYBROWN","ROYALBLUE","SADDLEBROWN","SALMON","SANDYBROWN","SEAGREEN","SEASHELL","SIENNA","SILVER","SKYBLUE",
      "SLATEBLUE","SLATEGRAY","SNOW","SPRINGGREEN","STEELBLUE","TAN","TEAL","THISTLE","TOMATO","TRANSPARENT","TURQUOISE","VIOLET",
      "WHEAT","WHITE","WHITESMOKE","YELLOW","YELLOWGREEN"
    }; // Note that none of these contain the letter combination 'grey', so to allow for non-American users, a dictionary-searching
       //  routine could safely change any substring 'grey' in a test name to 'gray' before consulting the dictionary.
    RR_StandardClr = new byte[]
    { 240,250,0,127,240,245,255,0,255,0,138,165,222,95,127,210,255,100,255,220,0,0,0,184,169,0,189,139,85,255,153,139,233,143,72,47,
      0,148,255,0,105,30,178,255,34,255,220,248,255,218,128,0,173,240,255,205,75,255,240,230,255,124,255,173,240,224,250,211,144,
      255,255,32,135,119,176,255,0,50,250,255,128,102,0,186,147,60,123,0,72,199,25,245,255,255,255,0,253,128,107,255,255,218,238,
      152,175,219,255,255,205,255,221,176,128,255,188,65,139,250,244,46,255,160,192,135,106,112,255,0,70,210,0,216,255,255,64,238,
      245,255,245,255,154
    };
    GG_StandardClr = new byte[]
    { 248,235,255,255,255,245,228,0,235,0,43,42,184,158,255,105,127,149,248,20,255,0,139,134,169,100,183,0,107,140,50,0,150,188,61,
      79,206,0,20,191,105,144,34,250,139,0,220,248,215,165,128,128,255,255,105,92,0,255,230,230,240,252,250,216,128,255,250,211,
      238,182,160,178,206,136,196,255,255,205,240,0,0,205,0,85,112,179,104,250,209,21,25,255,228,228,222,0,245,128,142,165,69,112,
      232,251,238,112,239,218,133,192,160,224,0,0,143,105,69,128,164,139,245,82,192,206,90,128,250,255,130,180,128,191,99,255,224,
      130,222,255,245,255,205
    };
    BB_StandardClr = new byte[]
    { 255,215,255,212,255,220,196,0,205,255,226,42,135,160,0,30,80,237,220,60,255,139,139,11,169,0,107,139,47,0,204,0,122,139,139,
      79,209,211,147,255,105,255,34,240,34,255,220,255,0,32,128,0,47,240,180,92,130,240,140,250,245,0,205,230,128,255,210,211,144,
      193,122,170,250,153,222,224,0,50,230,255,0,170,205,211,219,113,238,154,204,133,112,250,225,181,173,128,230,0,35,0,0,214,170,
      152,238,147,213,185,63,203,221,230,128,0,143,225,19,114,96,87,238,45,192,235,205,144,250,127,180,140,128,216,71,255,208,238,
      179,255,245,0,50
    };
    int noColours = StandardColourNames.Length;
    if (RR_StandardClr.Length != noColours || GG_StandardClr.Length != noColours || BB_StandardClr.Length != noColours)
    { throw new Exception("Failure in class JS: Arrays in method 'InitializeColourNames()' are of unequal length."); }
    ColourDic = new Dictionary<string, Trilobite>();
    for (int i=0; i < noColours; i++)
    { ColourDic.Add(StandardColourNames[i], new Trilobite(RR_StandardClr[i], GG_StandardClr[i], BB_StandardClr[i]) ); }
  }
  /// <summary>
  /// If the bytes supplied correspond to one of the colours with standard names stored in this unit, the name will be
  /// returned as a lower-case string with no internal spaces. Otherwise the empty string is returned. 
  /// </summary>
  public static string RetrieveColourName(byte R, byte G, byte B)
  { InitializeColourNames();
    int[] RRfinds = RR_StandardClr._FindAll(R);
    int noFinds = RRfinds.Length;
    string result = "";
    for (int i=0; i < noFinds; i++)
    { int n = RRfinds[i];
      if (G == GG_StandardClr[n] && B == BB_StandardClr[n]) { result = StandardColourNames[n].ToLower();  break; }
    }
    return result;
  }

  public static Color NewColor(int R, int G, int B)
  { return new Color( (byte)R, (byte)G, (byte)B ); }

  /// <summary>
  /// Given an integer representing an RGB colour, returns that colour.
  /// </summary>
  public static Color NewColor(int IntValue)
  { if (IntValue < 0) IntValue += Int32.MinValue; // This in effect adds 0x8000-0000 to the hex value, with no carry,
                                                  //  so that e.g. -1 (0xFFFF-FFFF) converts to 0x7FFF-FFFF, a positive number.
    int R, Gresid, G, Bresid, B;
    B = IntValue % 256;  Bresid = IntValue / 256;
    G = Bresid % 256;    Gresid = Bresid / 256;
    R = Gresid % 256;
    return new Color( (byte)R, (byte)G, (byte)B );
  }
  public static Color NewColor(string ColourName, out bool Success)
  { InitializeColourNames();
    Trilobite clrBytes;
    ColourName = ColourName.ToUpper();
    ColourName = ColourName.Replace("GREY", "GRAY");
    Success = ColourDic.TryGetValue(ColourName, out clrBytes);
    if (Success) return new Color(clrBytes.X, clrBytes.Y, clrBytes.Z);
    else return Color.Zero;
  }
/// <summary>
/// Convert a colour expressed as a byte array to a colour expressed as six hex digits (capitalized) following 'Prefix' (which may be empty).
/// </summary>
  public static string ColourToHex(string Prefix, byte[] RGB)
  { string result = Prefix;
    for (int i=0; i < 3; i++) result += RGB[i].ToString("X2");
    return result;
  }
/// <summary>
/// Convert a colour expressed as an integer array to a colour expressed as six hex digits (capitalized) following 'Prefix' (which may be empty).
/// </summary>
  public static string ColourToHex(string Prefix, int[] RGB)
  { string result = Prefix;
    for (int i=0; i < 3; i++) result += (RGB[i] % 256).ToString("X2");
    return result;
  }
/// <summary>
/// Convert a Gdk.Color object to a colour expressed as six hex digits (capitalized) following 'Prefix' (which may be empty).
/// </summary>
  public static string ColourToHex(string Prefix, Color Tint)
  { string result = Prefix;
    byte R = (byte) (Tint.Red / 256),   G = (byte) (Tint.Green / 256),   B = (byte) (Tint.Blue / 256);
    result += R.ToString("X2") + G.ToString("X2") + B.ToString("X2");
    return result;
  }



/// <summary>
/// Develops a System.Drawing.Color colour from hue, saturation and luminosity values. All three
/// values must be in the range 0 to 1, or you get back black.
/// </summary>
  public static System.Drawing.Color SDColorFromHSL(double hue, double sat, double lum)
  { byte red, green, blue;
    HSLtoRGB(hue, sat, lum,  out red, out green, out blue);
    return System.Drawing.Color.FromArgb((int) red, (int) green, (int) blue);
  }
/// <summary>
/// Develops a Gdk.Color colour from hue, saturation and luminosity values. All three
/// values must be in the range 0 to 1, or you get back black.
/// </summary>
  public static Color ColorFromHSL(double hue, double sat, double lum)
  { byte red, green, blue;
    HSLtoRGB(hue, sat, lum,  out red, out green, out blue);
    return new Color(red, green, blue);
  }
/// <summary>
/// Void. Develops Hue, Saturation and Luminosity values as OUT args., from either a Gdk.Color or a System.Drawing.Color object.
/// If 'Colour' is neither of these, black is returned (all OUT args. = 0).
/// </summary>
  public static void HSL(object Colour, out double hue, out double sat, out double lum)
  { int R = 0, G = 0, B = 0; // Default of black, if object not identified.
    if (Colour is Color)
    { var clr = (Color) Colour;
      R = clr.Red / 256;   G = clr.Green / 256;   B = clr.Blue / 256;
    }
    else if (Colour is System.Drawing.Color)
    { System.Drawing.Color clr = (System.Drawing.Color) Colour;
      R = (int) clr.R;   G = (int) clr.G;   B = (int) clr.B;
    }
    RGBtoHSL(R, G, B, out hue, out sat, out lum);
  }


/// <summary>
/// Void; returns RGB values via the OUT arguments. The first three arguments should be in the range 0 to 1; if any is out of range,
/// it is trimmed back to the nearer of the two limits.
/// </summary>
  public static void HSLtoRGB(double hue, double sat, double lum,  out byte red, out byte green, out byte blue)
  {
    if (hue < 0.0) hue = 0.0;  else if (hue > 1.0) hue = 1.0;
    if (sat < 0.0) sat = 0.0;  else if (sat > 1.0) sat = 1.0;
    if (lum < 0.0) lum = 0.0;  else if (lum > 1.0) lum = 1.0;
    double v;
    double redx, greenx, bluex;
    redx = lum;  greenx = lum;  bluex = lum; // default to gray
    v = (lum <= 0.5) ? (lum * (1.0 + sat)) : (lum + sat - lum * sat);
    if (v > 0)
    { double m, sv, fract, vsf, mid1, mid2;
      int sextant;
      m = lum + lum - v;
      sv = (v - m) / v;
      if (hue == 1.0) hue = 0.0; // because of raparound = hue 1.0 is hue 0.0.
      hue *= 6.0; 
      sextant = (int)hue;
      fract = hue - sextant;
      vsf = v * sv * fract;
      mid1 = m + vsf;   mid2 = v - vsf;
      switch (sextant)
      { case 0:  redx = v;     greenx = mid1;   bluex = m;     break;
        case 1:  redx = mid2;  greenx = v;      bluex = m;     break;
        case 2:  redx = m;     greenx = v;      bluex = mid1;  break;
        case 3:  redx = m;     greenx = mid2;   bluex = v;     break;
        case 4:  redx = mid1;  greenx = m;      bluex = v;     break;
        case 5:  redx = v;     greenx = m;      bluex = mid2;  break;
      }
    }
    // Convert.ToByte(double) below rounds the double value first:
    red = Convert.ToByte(redx * 255.0);    green = Convert.ToByte(greenx * 255.0);    blue = Convert.ToByte(bluex * 255.0);
  }
  /// <summary>
  /// Converts RGB values into Hue, Saturation and Luminescence values.
  /// </summary>
  public static void RGBtoHSL(byte red, byte green, byte blue, out double hue, out double sat, out double lum)
  {
    RGBtoHSL( (double) red, (double) green, (double) blue, out hue, out sat, out lum);
  }
  /// <summary>
  /// Converts RGB values into Hue, Saturation and Luminescence values. If any of input values red, green and blue
  /// are out of range (0 to 255), they are corrected (to 0 or 255).
  /// </summary>
  public static void RGBtoHSL(int red, int green, int blue, out double hue, out double sat, out double lum)
  {
    RGBtoHSL( (double) red, (double) green, (double) blue, out hue, out sat, out lum);
  }
  /// <summary>
  /// Converts RGB values into Hue, Saturation and Luminescence values. If any of input values red, green and blue
  /// are out of range (0 to 255), they are corrected (to 0 or 255).
  /// </summary>
  public static void RGBtoHSL(double red, double green, double blue, out double hue, out double sat, out double lum)
  { hue = sat = lum = 0.0; // default to black
    double redx = red / 255.0,  greenx = green / 255.0,  bluex = blue / 255.0;
    if (redx < 0.0) redx = 0.0;  if (greenx < 0.0) greenx = 0.0;  if (bluex < 0.0) bluex = 0.0;
    if (redx > 1.0) redx = 1.0;  if (greenx > 1.0) greenx = 1.0;  if (bluex > 1.0) bluex = 1.0;
    double v, m, vm,    r2, g2, b2;
    v = Math.Max(redx, greenx);  v = Math.Max(v, bluex); // Maximum of the three colours
    m = Math.Min(redx, greenx);  m = Math.Min(m, bluex); // Minimum of the three colours
    lum = (m + v) / 2.0;   if (lum <= 0.0) return; // Grey (because red = green = blue).
    vm = v - m;
    sat = vm;
    if (sat > 0.0) { sat /= (lum <= 0.5) ? (v + m) : (2.0 - v - m); }
    else return;
    r2 = (v - redx) / vm;    g2 = (v - greenx) / vm;    b2 = (v - bluex) / vm;
    if (redx == v) { hue = (greenx == m ? 5.0 + b2 : 1.0 - g2); }
    else if (greenx == v) { hue = (bluex == m ? 1.0 + r2 : 3.0 - b2); }
    else { hue = (redx == m ? 3.0 + g2 : 5.0 - r2); }
    hue /= 6.0;
  }

// -------------------------

/// <summary>
/// <para>Play a .wav file. If SoundPlayer can't find or can't handle the file, then the beep will occur if
///  and only if the second arg. is not empty (and sensible). Default duration is 500 msec (if no B.F.D.[1]).</para> 
/// <para>There is a third params arg. If [2] is nonzero, '.Play()' is used instead of '.PlaySync()', the default. PlaySync
///  makes your program wait till the sound is finished; but on the other hand it allows successive sounds to be queued
///  in successive calls to the fn. 'Play()' allows your program to continue while the sound is occurring, but if you
///  try to call this fn. again during that sound it will swamp the earlier sound, so sounds can't be queued.</para>
/// </summary>
	public static void PlayWavFile(string PathAndName, params int[] BeepFreqDurn)
	{	int freq = -1, durn = 500;   bool beepo = false,  play_asynch = false;
	  if (BeepFreqDurn.Length > 0) freq = BeepFreqDurn[0];
    if (BeepFreqDurn.Length > 1) durn = BeepFreqDurn[1];
    if (BeepFreqDurn.Length > 2) play_asynch = (BeepFreqDurn[2] != 0);
    if (PathAndName == "") beepo = true;
    else
    { using (System.Media.SoundPlayer squawk = new System.Media.SoundPlayer())
  		{	try
	  		{ squawk.SoundLocation = PathAndName;
		  		squawk.LoadTimeout = 10000;
          if (play_asynch) squawk.Play(); 
		  		else squawk.PlaySync(); 
		  	} 
			  catch
			  { beepo = true; }
      }
		}
    if (beepo && freq >= 37 && freq <= 32767 && durn > 0) Console.Beep(freq, durn); // freq. limits come from the .NET specs; not my idea.
  }
// Play the 'WRONG!' .wav file, if (as is usual) it happens to be in "C:/Windows/Media/":
	public static void Wrong()
	{ PlayWavFile("C:/Windows/Media/Windows Error.wav"); }

//sort
// SORTING FUNCTIONS - TWO WITH KEYS, TWO WITHOUT
// ==============================================
  // Arr is the array to be sorted on the key; ArrKey is the key. ArrKey may be either of type double or type int.
  // StartEnd, if supplied, are the first and last elements subject to sorting.
  // No error detection. Out of range values for StartEnd are corrected; crossed values return the original unsorted array.
  // Note that BOTH ARRAYS are sorted, being implicitly REF arguments; the function returns nothing.
  public static void SortByKey(double[] Arr, Array ArrKey, bool Ascending, params int[] StartEnd)
  { int startptr = 0, endptr = Arr.Length-1;
    if (StartEnd.Length > 0)startptr = StartEnd[0];
    if (StartEnd.Length > 1)endptr = StartEnd[1];
    if (startptr < 0) startptr = 0;
    if (endptr >= Arr.Length) endptr = Arr.Length-1;
    if (startptr >= endptr) return; // Don't do no sorting if pointers cross, OR if only one element is in scope.
    if (!Ascending) // descending sort needs instance of the class 'CompareDouble',
            // which is written lower down in this unit, in the same namespace.
    { IComparer com;
      if (ArrKey == null)
      { com = new DescendDouble();
        Array.Sort(Arr, startptr, endptr-startptr+1, com); }
      else
      { if (ArrKey.GetValue(0) is Int32) com = new DescendInt();
        else com = new DescendDouble();
        Array.Sort(ArrKey, Arr, startptr, endptr-startptr+1, com);
      }
    }
    else
    { if (ArrKey == null)
      { Array.Sort(Arr, startptr, endptr-startptr+1); }
      else Array.Sort(ArrKey, Arr, startptr, endptr-startptr+1);
    }
  }

  // Simply calls the above, without a key.
  public static void Sort(double[] Arr, bool Ascending, params int[] StartEnd)
  { SortByKey(Arr, null, Ascending,  StartEnd); 
  }

  // The integer[] version of the above. Again, ArrKey can be either int[] or double[]. All else is also the same.
  public static void SortByKey(int[] Arr, Array ArrKey, bool Ascending, params int[] StartEnd)
  { int startptr = 0, endptr = Arr.Length-1;
    if (StartEnd.Length > 0)startptr = StartEnd[0];
    if (StartEnd.Length > 1)endptr = StartEnd[1];
    if (startptr < 0) startptr = 0;
    if (endptr >= Arr.Length) endptr = Arr.Length-1;
    if (startptr >= endptr) return; // Don't do no sorting if pointers cross, OR if only one element is in scope.
    if (!Ascending) // descending sort needs instance of the class 'CompareDouble',
            // which is written lower down in this unit, in the same namespace.
    { IComparer com;
      if (ArrKey == null)
      { com = new DescendInt();
        Array.Sort(Arr, startptr, endptr-startptr+1, com); }
      else
      { if (ArrKey.GetValue(0) is Int32) com = new DescendInt();
        else com = new DescendDouble();
        Array.Sort(ArrKey, Arr, startptr, endptr-startptr+1, com);
      }
    }
    if (!Ascending) // descending sort needs instance of the class 'CompareDouble',
            // which is written lower down in this unit, in the same namespace.
    { IComparer com;
      if (ArrKey == null)
      { com = new DescendInt();
        Array.Sort(Arr, startptr, endptr-startptr+1, com); }
      else
      { if (ArrKey.GetValue(0) is Int32) com = new DescendInt();
        else com = new DescendDouble();
        Array.Sort(ArrKey, Arr, startptr, endptr-startptr+1, com);
      }
    }
    else
    { if (ArrKey == null)
      { Array.Sort(Arr, startptr, endptr-startptr+1); }
      else Array.Sort(ArrKey, Arr, startptr, endptr-startptr+1);
    }
  }
  // Simply calls the above, without a key.
  public static void Sort(int[] Arr, bool Ascending, params int[] StartEnd)
  { SortByKey(Arr, null, Ascending,  StartEnd); 
  }

// Suppose you were planning to insert the value Candidate into a sorted array. RefArray.
//   This method locates the slot where Candidate should be inserted so as to maintain
//   sorted order. If less than / greater than all elements, returns as 0 / RefArray.Length.
// NB: The array must be SORTED, and in ASCENDING ORDER (so take care with neg. arrays).
// RefArray may currently be of type byte, int, double or string. Add others as required.
// Candidate: enter as is (no typecast needed to 'object').
// TypeName is entered as "byte", "int", "double" or "string" (case-sensitive).
// RETURNED: a Duo object, of which .X is the slot mentioned above, or else an error
//   code, namely: -1 = TypeName not recognized; -2 = System raised the error.
//   The returned .Y field gives the no. of contiguous occurrences of Candidate at and
//   below position .X; = 0 if Candidate does not tie with an element.
// Note that there is no check for sorting. Results will be misleading if the array is not
//   sorted or if it is sorted in descending order; but there will be no error indicator.
  public static Duo FindSlot(Array RefArray, object Candidate, string TypeName )
  { int cnt = 0;
    Duo result = new Duo(RefArray.Length, 0);
    if (result.X == 0) return result; // for empty array, candidate should always --> [0].
    try
    { if (TypeName == "byte")
      { byte cand = (byte) Candidate;
        foreach (byte bb in RefArray)
        { if (cand == bb) result.Y++;
          else if (cand < bb){result.X = cnt; break; } cnt++; }
      }
      else if (TypeName == "int")
      { int cand = (int) Candidate;
        foreach (int ii in RefArray)
        { if (cand == ii) result.Y++;
          else if (cand < ii){result.X = cnt; break; } cnt++; }
      }
      else if (TypeName == "double")
      { double cand = (double) Candidate;
        foreach (double dd in RefArray)
        { if (cand == dd) result.Y++;
          else if (cand < dd){result.X = cnt; break; } cnt++; }
      }
      else if (TypeName == "string")
      { string cand = (string) Candidate;
        foreach (string ss in RefArray)
        { int f = String.Compare(cand, ss);
          if (f == 0) result.Y++;
          else if (f < 0){result.X = cnt; break;}   cnt++; }
      }
      else result = new Duo(-1,0); // TypeName not handled in this method.
    }
    catch { result = new Duo(-2,0); }
    return result;
  }
// Overload for double arrays, if you want to regard values of Candidate +/- Leeway
//   as "equal" to Candidate. Meaning of .X and .Y as in the original version, except that
//   no error codes are needed, as not much can go wrong.
// Leeway: Must be positive (or zero), or you will get silly results.
//   NB! Because of the mysterious inner workings of "Math.Abs(.)", there is NO GUARANTEE
//   that a difference between x and y of exactly Leeway will cause them to be regarded
//   as "equal". Therefore be sure to make Leeway the tiniest tad beyond the real leeway.
//   Also I am not sure that I would trust Math.Abs(.) for Leeway = exactly 0.0; but
//   you wouldn't be using this overload in that case, would you?
  public static Duo FindSlot(double[] RefArray, double Candidate, double Leeway )
  { if (RefArray.Length == 0) return new Duo(0,0);
    Duo result = new Duo(RefArray.Length, 0);   int cnt = 0;
    foreach (double dd in RefArray)
    { if (Math.Abs(Candidate - dd) <= Leeway) result.Y++; // see caution note in header.
      else if (Candidate < dd){result.X = cnt; break; } cnt++; }
    return result;
  }
// Given an integer array, increment it as if it were a number to base NumberBase.
//  Omitted fields of NumberBase[] duplicate the last, so if all elements are to
//  the same number base, simply send that scalar no. as the argument.
// NumberBase should never be omitted, even though it is a params arg.
// FromLowestElement: if TRUE, [0] is incremented till carry, then [1], etc.
//  otherwise starts from the other end. (Nec. e.g. for working through a matrix
//  array; 'Array.GetValue(Array, int[])' expects cols. to be last, not first.)
// RETURNED: the carry of the highest element. If 1, overrunning occurred, and
//  the array is back to a series of zeroes.
  public static int IncmtArray(ref int[] Arr, bool FromLowestElement,
                                 params int[] NumberBase)
  { int result = 1;  int n,p, len = Arr.Length, NBLast = NumberBase.Length-1;
    for (int i = 0; i < len; i++)
    { if (FromLowestElement) n = i; else n = len-1-i;
      Arr[n]++;
      if (n > NBLast) p = NBLast; else p = n;
      if (Arr[n] < NumberBase[p]) { result = 0;  break; }
      else Arr[n] = 0;
    }
    return result;
  }
  // Returns -1 if no find, or else the first byte in Within such that
  // from there on it matches LookFor.
  public static int FindInByteArray(byte[] Within, byte[] LookFor)
  { int result = -1;
    int wlen = Within.Length,  flen = LookFor.Length;
    if (flen==0 || wlen < flen) return result;
    for (int i=0; i <= wlen-flen; i++) 
    { result = i;
      for (int j=0; j < flen; j++)
      { if (Within[i+j] != LookFor[j]) { result = -1; break; } }
      if (result == i) break;    
    }
    return result;  
  }
  // Returns -1 if no find, or else the first element in Within such that from there on it matches LookFor. 
  // NB: The 'TO' pointer (FromTo[1]) is the last element at which to check for a find; it is NOT the last element
  //  which may be examined, and the last find may straddle it.
  public static int FindInDubArray(ref double[] Within, ref double[] LookFor, params int[] FromTo)
  { int result = -1;
    int wlen = Within.Length, flen = LookFor.Length;
    if (flen==0 || wlen < flen) return result;
    int fromptr = 0, toptr = wlen-flen;
    if (FromTo.Length > 1 && FromTo[1] < toptr) toptr = FromTo[1]; //no check for negative value.
    if (FromTo.Length > 0) fromptr = FromTo[0]; // no check for neg. value. Allow crossed pointers.
    for (int i=fromptr; i <= toptr; i++) 
    { result = i;
      for (int j=0; j < flen; j++)
      { if (Within[i+j] != LookFor[j]) { result = -1; break; } }
      if (result == i) break;    
    }
    return result;  
  }
  // Returns -1 if no find, or else the first byte in Within such that
  // from there on it matches LookFor. NB: The 'TO' pointer (FromTo[1]) is the last char.
  //  at which to check for a find; it is NOT the last char. which may be examined, and
  //  the last find may straddle it.
  public static int FindInCharArray(char[] Within, char[] LookFor, params int[] FromTo)
  { int result = -1;
    int wlen = Within.Length, flen = LookFor.Length;
    if (flen==0 || wlen < flen) return result;
    int fromptr = 0, toptr = wlen-flen;
    if (FromTo.Length > 1 && FromTo[1] < toptr) toptr = FromTo[1]; //no check for negative value.
    if (FromTo.Length > 0) fromptr = FromTo[0]; // no check for neg. value. Allow crossed pointers.
    
    for (int i=fromptr; i <= toptr; i++) 
    { result = i;
      for (int j=0; j < flen; j++)
      { if (Within[i+j] != LookFor[j]) { result = -1; break; } }
      if (result == i) break;    
    }
    return result;  
  }
  // Overload, for looking for single character(s). If AnyOfThese is NULL, JustOneChar is sought; if
  //  non-null, JustOneChar is ignored. (Someextra arg. is nec. to distinguish this from previous overload, so why not this?)
  public static int FindInCharArray(char[] Within, char JustOneChar, char[] AnyOfThese, params int[] FromTo)  
  { int result = -1;
    int wlen = Within.Length;   if (wlen == 0) return result;
    int fromptr = 0, toptr = wlen-1;
    if (FromTo.Length > 1 && FromTo[1] < toptr) toptr = FromTo[1]; //no check for negative value.
    if (FromTo.Length > 0) fromptr = FromTo[0]; // no check for neg. value. Allow crossed pointers.
    int anylen = 0;  if (AnyOfThese != null) anylen = AnyOfThese.Length;
    for (int i=fromptr; i <= toptr; i++) 
    { if (anylen == 0) // Use JustOneChar:
      { if (Within[i] == JustOneChar) return i; }
      else // Use AnyOfThese:
      { for (int j=0; j < anylen; j++)
        { if (Within[i] == AnyOfThese[j]) return i; }
      }
    }
    return result;  
  }
  // Search stringbuilder for target. Returns -1 if no find.
  public static int FindInStringBuilder(ref StringBuilder Within, string LookFor, params int[] FromTo)
  { char[] target = LookFor.ToCharArray();  
    int targlen = LookFor.Length, sblen = Within.Length;
    int startptr = 0, endptr = Int32.MaxValue, result = -1;
    if (FromTo.Length > 0) startptr = FromTo[0]; 
    if (FromTo.Length > 1) endptr = FromTo[1];
    if (endptr >= sblen) endptr = sblen - 1;
    while (startptr <= endptr - targlen + 1)
    { result = startptr;
      for (int i=0; i<targlen; i++)
      { if (Within[startptr+i] != target[i]){result = -1; break;} }
      if (result != -1) break; 
      startptr++; 
    }   
    return result;  
  }

  /// <summary>
  /// ValueDataBase must be a set of values, some or all of which are referenced by the expression.
  /// Expression has all spaces, tabs and par. marks stripped from it; after that, it mus consist ONLY
  ///   of three element types: (1) references to elements of ValueDataBase,
  ///   in the form of "{n}", where n is the string representation of a valid index in ValueDataBase;
  ///   (2) operation signs, the 5 allowed ones being in the set "+-*/^" (the last being the power index sign);
  ///   (3) brackets '(', ')', with any level of nesting; but brackets must match.
  /// If there is no error, the solution for the expression is returned, and ErrMsg is set to "";
  ///   if an error, 0 is returned, and the ErrMsg is appropriately filled.
  /// Hierarchy: at any one bracket nesting level, operations are processed in the reverse order of their
  ///   occurrence in the above string ("^" first, "+" last).
  /// </summary>
  public static double SolveExpression(string Expression, double[] ValueDataBase, out string ErrMsg)
  { ErrMsg = "";
    if (String.IsNullOrEmpty(Expression)) { ErrMsg = "empty expression"; return 0.0; }
    Expression = Expression.Trim(); // This would crash if Expression was null, hence the need to split these two error tests.
    if (Expression == "") { ErrMsg = "no data in expression"; return 0.0; }
    string OpSigns = " +-*/^"; // in ascending hierarchical order. As spaces were eliminated by calling code, the first dummy
                                // element will never be accessed. It is needed so that the op code for '+' is 1, not 0 (the null opn).
    int BktIncrement = OpSigns.Length; // If inside n open brackets '(', an operation will have n * BktIncrement added to its
                                       //   index in OpSigns, the result being implanted in array Opns (created later).
    // Remove all internal spaces:
    while (true)
    { int p = Expression.IndexOfAny(new [] {'\n', '\t', ' '});
      if (p == -1) break;
      Expression = Expression.Remove(p, 1);
    }
    int[] brace_oprs = Expression._IndexesOf('{');
    int[] brace_clrs = Expression._IndexesOf('}');
    if (brace_oprs == null || brace_clrs == null) { ErrMsg = "there is no '{' and/or no '}'"; return 0.0; }
    int noValueRefs = brace_oprs.Length;
    if (noValueRefs != brace_clrs.Length) { ErrMsg = "braces '{' and '}' are not matched";  return 0.0; }
    // Deal with +,- signs at start of expression or after an opening bracket, both being legal; do so by inserting a dummy zero.
    int len = Expression.Length; // just for the next step, as the length will alter.
    string stroo = noValueRefs.ToString(); // only used if we have to add a final zero
    char c = Expression[0];
    if (c == '+' || c == '-') Expression = "{" + stroo + "}" + Expression; // in a minute, vDataBase[value of stroo] will exist and will be zero.
    Expression = Expression.Replace("(-", "({" + stroo + "}-");
    Expression = Expression.Replace("(+", "({" + stroo + "}+");
    double[] valueDBase; // This will replace the arg. ValueDataBase, as the array may be altered inside this function (by appending zero).
    if (Expression.Length != len)
    { noValueRefs++;
      valueDBase = new double[noValueRefs];
      ValueDataBase.CopyTo(valueDBase, 0); // The last element of valueDBase is not overwritten, so remains zero.
    }
    else valueDBase = ValueDataBase; // A pointer copy, but we promise not to alter this array again.
   // DEVELOP TWO ARRAYS FROM EXPRESSION, ONE OF VALUES AND ONE OF OPERATIONS (incorporating bracketting levels):
    double[] Values = new double[noValueRefs];
    int[] Opns = new int[noValueRefs];
    char[] expo = Expression.ToCharArray();
    int expoLen = expo.Length;
    int DBaseLen = valueDBase.Length;
    int ndxValues = 0,  ndxOpns = 0;
    bool inavalue = false;
    int n, bktlevel = 0;
    string ss = ""; // will accumulate the string between braces
    for (int i=0; i < expoLen; i++)
    { char ch = expo[i];
      if (ch == '{')
      { if (inavalue) { ErrMsg = "an '{' is out of place";  return 0.0; }
        inavalue = true;  ss = "";
      }
      else if (ch == '}')
      { if (!inavalue) { ErrMsg = "an '}' is out of place";  return 0.0; }
        n = ss._ParseInt(-1);
        if (n < 0) { ErrMsg = "the data base reference '{" + ss + "}' is invalid";  return 0.0; }
        if (n >= DBaseLen) { ErrMsg = "the data base index '{" + ss + "}' is out of range";  return 0.0; }
        Values[ndxValues] = valueDBase[n];  ndxValues++; // from earlier checks, no risk of ndxValues being out of range.
        inavalue = false;
      }
      else if (inavalue) ss += ch.ToString();
      else if (ch == '(') bktlevel += BktIncrement;
      else if (ch == ')')
      { bktlevel -= BktIncrement;
        if (bktlevel < 0) { ErrMsg = "there are more ')' than '('";  return 0.0; }
      }
      else
      { n = OpSigns.IndexOf(ch);
        if (n == -1) { ErrMsg = "unrecognized operation '" + ch.ToString() + "'";  return 0.0; }
        if (ndxOpns >= noValueRefs) { ErrMsg = "there are operation sign(s) not associated with variables";  return 0.0; }
        Opns[ndxOpns] = bktlevel + n;
        ndxOpns++;
      }
    }
    if (bktlevel != 0) { ErrMsg = "there are more '(' than ')'";  return 0.0; }
    if (ndxOpns != ndxValues-1) { ErrMsg = "there are not enough operation signs connecting variables";  return 0.0; }
    Opns[ndxOpns] = 0; // the null operation, at the end.
    // SOLVE THE EXPRESSION, USING THESE TWO NEW ARRAYS
    if (Opns.Length == 1) return Values[0]; // Just a value supplied, with no signs, so nothing more to do.
    int[] topop;
    int op;  double x = 0.0, y = 0.0;
    while (true)
    {
      topop = Opns._Extremum(0, -1, '+'); // 'topup' will have 4 values; we use [0] - the max value in Opns - and [1] - its 1st. index there.
      if (topop[0] == 0) break; // Nothing left to do.
      n = topop[1];
      op = Opns[n] % BktIncrement; // back to being an index in OpSigns.
      x = Values[n];  y = Values[n+1];
      int errno = 0;
      switch (op)
      { case 1: x += y;  break;
        case 2: x -= y;  break;
        case 3: x *= y;  break;
        case 4: x /= y;  break; // If y is zero, will simply return double.PositiveInfinity or double.NegativeInfinity
        case 5: // power index
          try
          { x = Math.Pow(x, y); }
          catch { errno = 1; }
          break;
      }
      if (errno != 0)
      { ErrMsg = "power operation is improper (e.g. a negative value with a fractional index)";  return 0.0; }
      // Overwrite the dead operator and its operand:
      for (int j = n; j < noValueRefs-1; j++)
      { Opns[j] = Opns[j+1];  Values[j] = Values[j+1]; }
      Values[n] = x;
    }
    return x;
  }





//=======================================================================
//6. REGEX SEARCHES
//-----------------------------------------------------------------------
// MULTI-FIND / REPLACE REGEX MASTER FN.:                   //reg
// ======================================
// [BASE VERSION for various overloads (so don't panic at the no. of arguments!)]
// DO A REGEX SEARCH (and optionally replace finds) without the hoo-ha of having to
//   create Regex and Match objects. Regex syntax not dealt with here; ya either knows
//   it or ya don't. Note that all finds are returned; if you only want a set no. (e.g.
//   the first two), process what you want and ignore the rest.
// ARGUMENTS: Some are obvious. Pattern must be exactly what is to be submitted to the
//   Regex constructor. The first two booleans set corresponding RegexOptions fields
//   in C#'s Regex class.
// Arguments for replacements: (1) You don't want to replace the finds: Simply set
//   DoReplacements to FALSE, and put 'null' in for MatchEvaluator. (You still have
//   to define a dummy string for Replaced, as it is an 'out' parameter.)
//   (2) You want to replace all finds with the same word: DoReplacements TRUE,
//   ReplaceWith is that word, MatchEvaluator is null. (3) You want to do a smart
//   replacement: DoReplacements TRUE; MatchEvaluator set to delegate fn. in calling
//   code; ReplaceWith irrelevant. [** See end of file for notes on such delegates.]
// RETURNED: a MatchCollection object. You get at it thus: "MatchCollection Macho =
//   <this fn.>;". Deal with it as with any array: "foreach (Match m in Macho){..}"
//   (Main fields of the MatchColl. object are simple: m[i].Value is the string
//   returned; it started at m[i].Index, and had extent m[i].Length.  No. finds is
//   m[i].Count. If you did replacing, then 'out Replaced' = InStr as rewritten
//   with replacements.
// ERROR CODE: 0 = no errors;  1 = silly args. (so Regex not called); 2 = error thrown
//   by classes Regex or MatchCollection. If errors, the fn. returns NULL.
  public static MatchCollection RegEx(string InStr, int StartPos, int EndPos,
         string Pattern, bool CaseSensitive, bool SearchBackwards,
         bool DoReplacements, string ReplaceWith,  MatchEvaluator MEv,
         out string Replaced, out int ErrorCode)
  { // Deal with silly arguments first:
    Replaced = "";
    if (EndPos >= InStr.Length) EndPos = InStr.Length-1; // reset EndPos, if too large.
    if (StartPos<0 || StartPos>EndPos){ErrorCode = 1; return null;}
    // Respond to pointers:
    if (StartPos > 0  || EndPos < InStr.Length-1) InStr = InStr._FromTo(StartPos,EndPos);
    // Try the search
    try
    { // Set the options in accordance with arguments:
      RegexOptions ro = new RegexOptions();
      if (!CaseSensitive) ro |= RegexOptions.IgnoreCase;
      if (SearchBackwards) ro |= RegexOptions.RightToLeft;
      // Get search going:
      Regex r = new Regex(Pattern, ro);
      MatchCollection  Macho = r.Matches(InStr);
      if (DoReplacements)
      { if (MEv == null)  Replaced = r.Replace(InStr, ReplaceWith);
        else Replaced = r.Replace(InStr, MEv);
      }
      ErrorCode = 0;  return Macho; // If no error, go home.
    }
    catch { ErrorCode = 2; }
    return null;
  }
// MULTI-FIND REGEX OVERLOADS:
// ===========================
// 1. NO REPLACEMENT; INCLUDE POINTERS:
  public static MatchCollection RegEx(string InStr, int StartPos, int EndPos,
         string Pattern, bool CaseSensitive, bool SearchBackwards, out int ErrorCode)
  { string Dummy;
    return RegEx(InStr, StartPos, EndPos, Pattern, CaseSensitive, SearchBackwards,
         false, "",  null, out Dummy, out ErrorCode);
  }
// 2. NO REPLACEMENT; NO POINTERS (i.e. whole-string search):
  public static MatchCollection RegEx(string InStr, string Pattern,
            bool CaseSensitive, bool SearchBackwards, out int ErrorCode)
  { string Dummy;
    return RegEx(InStr, 0, InStr.Length-1, Pattern, CaseSensitive, SearchBackwards,
         false, "",  null, out Dummy, out ErrorCode);
  }
// 3. SIMPLE REPLACEMENT (no replacement delegate fn.); INCLUDE POINTERS.
  public static MatchCollection RegEx(string InStr, int StartPos, int EndPos,
         string Pattern, bool CaseSensitive, bool SearchBackwards,
         string ReplaceWith, out string Replaced, out int ErrorCode)
  { return RegEx(InStr, StartPos, EndPos, Pattern, CaseSensitive, SearchBackwards,
                        true, ReplaceWith,  null, out Replaced, out ErrorCode);
  }

// 4. SIMPLE REPLACEMENT (no replacement delegate fn.); NO POINTERS (whole-string search)
  public static MatchCollection RegEx(string InStr, string Pattern,
         bool CaseSensitive, bool SearchBackwards,
         string ReplaceWith, out string Replaced, out int ErrorCode)
  { return RegEx(InStr, 0, InStr.Length-1, Pattern, CaseSensitive, SearchBackwards,
                        true, ReplaceWith,  null, out Replaced, out ErrorCode);
  }
// 5. DELEGATE REPLACEMENT; INCLUDE POINTERS.
  public static MatchCollection RegEx(string InStr, int StartPos, int EndPos,
         string Pattern, bool CaseSensitive, bool SearchBackwards,
         MatchEvaluator MEv, out string Replaced, out int ErrorCode)
  { return RegEx(InStr, StartPos, EndPos, Pattern, CaseSensitive, SearchBackwards,
         true, "",  MEv, out Replaced, out ErrorCode);
  }

// 6. DELEGATE REPLACEMENT; NO POINTERS (whole string only).
  public static MatchCollection RegEx(string InStr, string Pattern,
         bool CaseSensitive, bool SearchBackwards,
         MatchEvaluator MEv, out string Replaced, out int ErrorCode)
  { return RegEx(InStr, 0, InStr.Length-1, Pattern, CaseSensitive, SearchBackwards,
         true, "",  MEv, out Replaced, out ErrorCode);
  }


// SINGLE-FIND (NO REPLACE) REGEX MASTER FN.: (No single-find version with replacing.)
// ==========================================
// [BASE VERSION for various overloads (so don't panic at the no. of arguments!)]
// DO A REGEX SEARCH for a single instance, without the hoo-ha of having to
//   create Regex and Match objects. Regex syntax not dealt with here; ya either knows
//   it or ya don't. Note that all finds are returned; if you only want a set no. (e.g.
//   the first two), process what you want and ignore the rest.
// ARGUMENTS: Some are obvious. Pattern must be exactly what is to be submitted to the
//   Regex constructor. The first two booleans set corresponding RegexOptions fields
//   in C#'s Regex class.
// RETURNED: a Match object. You get at it thus: "Match Macho = <this fn.>;".
//   The Main fields of the Match object are simple: m.Value is the string returned;
//   it started at m.Index, and had extent m.Length.
// ERROR CODE: 0 = no errors;  1 = silly args. (so Regex not called); 2 = error thrown
//   by classes Regex or MatchCollection. If errors, the fn. returns NULL.
  public static Match RegExSingle(string InStr, int StartPos, int EndPos,
         string Pattern, bool CaseSensitive, bool SearchBackwards, out int ErrorCode)
  { // Deal with silly arguments first:
    if (EndPos >= InStr.Length) EndPos = InStr.Length-1; // reset EndPos, if too large.
    if (StartPos<0 || StartPos>EndPos){ErrorCode = 1; return null;}
    // Respond to pointers:
    if (StartPos > 0  || EndPos < InStr.Length-1) InStr = InStr._FromTo(StartPos,EndPos);
    // Try the search
    try
    { // Set the options in accordance with arguments:
      RegexOptions ro = new RegexOptions();
      if (!CaseSensitive) ro |= RegexOptions.IgnoreCase;
      if (SearchBackwards) ro |= RegexOptions.RightToLeft;
      // Get search going:
      Regex r = new Regex(Pattern, ro);
      Match  Macho = r.Match(InStr);
      ErrorCode = 0;  return Macho; // If no error, go home.
    }
    catch { ErrorCode = 2; }
    return null;
  }
// SINGLE-FIND REGEX OVERLOAD:
// ===========================
// NO POINTERS (i.e. search whole string):
  public static Match RegExSingle(string InStr, string Pattern,
                          bool CaseSensitive, bool SearchBackwards, out int ErrorCode)
  { return RegExSingle(InStr, 0, InStr.Length-1, Pattern, CaseSensitive, SearchBackwards, out ErrorCode);
  }

//==================================================================================
//  I/O
//==================================================================================
// If no command line arguments for the process, set 'Arguments' to "".
/// <summary>
/// <para>Runs whatever is specified by FilePathAndName, that object receiving the arguments as if written
///  on a terminal command line. Arguments should be the empty string, if none, or else have a space
///  between individual arguments.</para>
/// <para>You cannot run e.g. Bash commands which contain non-argument structures; e.g. RunProcess("ls", "> /foo/goo.txt")
///  will not work, because '>' will be interpreted as an argument for 'ls' (in this case), not as a redirection sign.
///  To get over this, write a temporary Bash script, save it somewhere, and then run that script using this method.</para>
/// <para>If WaitMsecs.. is zero or negative, no wait occurs, and the returned exit code is simply 0. If longer, then
///  the method will wait up to this time; if the process ends within that time, the returned exit code will be whatever the
///  process has returned; if it has not ended, the return exit code will be 999.</para>
/// </summary>
  public static Quid RunProcess(string FilePathAndName, string Arguments, int WaitMsecsForTermination)
  { // These are the Win32 error code for file not found or access denied.
    int ERROR_FILE_NOT_FOUND = 2;
    int ERROR_ACCESS_DENIED = 5;
    Quid result = new Quid(true);
    System.Diagnostics.Process prosaic = new System.Diagnostics.Process();
    try
    { // Get the path that stores user documents.
      prosaic.StartInfo.FileName = FilePathAndName;
      if (Arguments != "") prosaic.StartInfo.Arguments = Arguments;
      prosaic.Start();
      if (WaitMsecsForTermination > 0)
      { result.IX = 999;
        DateTime startTime = DateTime.Now;
        while (true)
        { TimeSpan period = DateTime.Now - startTime;
          if (period.TotalMilliseconds >= WaitMsecsForTermination) break; // leaving exit code as 999
          if (prosaic.HasExited) { result.IX = prosaic.ExitCode;  break; }
        }
      }
    }
    catch (System.ComponentModel.Win32Exception e)
    { if (e.NativeErrorCode == ERROR_FILE_NOT_FOUND)
      { result.S = e.Message;  result.B = false;
      }
      else if (e.NativeErrorCode == ERROR_ACCESS_DENIED)
      { // Note that if your word processor might generate exceptions
        // such as this, which are handled first.
        result.S = e.Message;  result.B = false;
      }
    }
    if (!result.B) result.S = "Attempt to run '" + FilePathAndName + "' failed:  " + result.S;
    return result;
  }




} // End of class JS

//==================================================================================
//  OTHER CLASSES
//==================================================================================

// COMPARERS, available for any C# sorting functions (e.g. Array.Sort(..)):
// ========================================================================
// DESCENDING SORTS:
public class DescendDouble : IComparer
{ public int Compare( Object x, Object y )
  { double xx = (double) x, yy = (double) y;
    if (yy > xx) return 1; else if (yy < xx) return -1; else return 0;
  }
}
public class DescendInt : IComparer
{ public int Compare( Object x, Object y )
  { int xx = (int) x, yy = (int) y;
    if (yy > xx) return 1; else if (yy < xx) return -1; else return 0;
  }
}

public class DescendStr_CaseInsens : IComparer  // If the same letter but different case, 'A' precedes 'a'.
{ int IComparer.Compare( Object x, Object y )
  { return( StringComparer.CurrentCulture.Compare( y, x ) ); }
}

public class DescendStr_CaseSens : IComparer  //#########NOT WORKING
{ int IComparer.Compare( Object x, Object y )
  { return StringComparer.Ordinal.Compare((string)y, (string)x ); }
}
// ASCENDING STRING SORTS: No need for delegates. E.g. "Array.Sort(StrArr);" sorts ignoring case, except
//   that if two strings of same letters in different case, then (e.g.) 'a...' precedes 'A...'. For a sort
//   sensitive to case, use "Array.Sort(StrArr, StringComparer.Ordinal);".
//===========================================================================
public class Filing
{
  private Filing(){}
  /// <summary>
  /// <para>Returns an array of length 3: [0] = path (forced always to begin and end with '/'); [1] = filename without path;
  /// [2] = file extension part of [1] (less the '.'). If no '.', [2] is null. If name ends in '.', [2] is empty.</para>
  /// <para>If inStr is empty after trimming, NULL is returned instead.</para>
  /// </summary>
  public static string[] Parse(string inStr)
  { inStr = inStr.Trim();  if (inStr == "") return null;
    if (inStr[0] != '/') inStr = '/' + inStr;
    int lastSlash = inStr.LastIndexOf('/');
    string[] result = new string[3];
    result[0] = inStr._FromTo(0, lastSlash); // for something in the root directory, this will be just '/'.
    result[1] = inStr._Extent(lastSlash+1);
    int lastStop = inStr.LastIndexOf('.');
    if (lastStop == -1) result[2] = null;
    else result[2] = inStr._Extent(lastStop+1);
    return result;
  }
      
/// <summary>
/// <para>Try to load a file into the 'out' argument StringBuilder object. Return { TRUE, ""}
/// if successful. If unsuccessful, returns { FALSE, error message }, and FileText is an empty
/// (but non-null) StringBuilder object.</para>
/// </summary>
  public static Boost LoadTextFromFile(string ThisPathAndName, out StringBuilder FileText)
  { FileText = new StringBuilder();
    bool wentWell = false;
    string systemMessage = "";
    StreamReader sr = null; // Outside because the statement 'sr = new..' can raise an error (e.g. wrong file name), and
                          //  'finally' won't have heard of 'sr' in that case.
    try // *** THIS METHOD, using File.OpenRead, ALLOWS YOU TO OPEN A READ-ONLY FILE, so be slow to change it, as usual methods won't do so.
    { sr = new StreamReader(File.OpenRead(ThisPathAndName)); // the System.IO.File class only has static methods, so no closure required. 
      sr.BaseStream.Seek(0, SeekOrigin.Begin);
      while (sr.Peek() > -1) FileText.AppendLine(sr.ReadLine()); // While not at the end of the file, write to standard output.
      wentWell = true;
    }
    catch (Exception e) { systemMessage = e.Message; }
    finally { if (sr != null) sr.Close(); } // If you don't close sr, you get a 'sharing violation' message when you try to save it with same name.
    if (wentWell) return new Boost(true);
    else return new Boost(false, "System message:\n\n" + systemMessage);
  }
/// <summary>
/// <para>Try to load a file into the 'out' argument List &lt;string> object. Return { TRUE, ""}
/// if successful. If unsuccessful, returns { FALSE, error message }, and FileStrings is an empty
/// (but non-null) string list.</para>
/// </summary>
  public static Boost LoadTextLinesFromFile(string ThisPathAndName, out List<string> FileStrings)
  { FileStrings = new List<string>();
    bool wentWell = false;
    string systemMessage = "";
    FileStream fs = null; // Outside because the statement 'fs = new..' can raise an error (e.g. wrong file name), and
                          //  'finally' won't have heard of 'fs' in that case. (No such problem for 'sr' below.)
    try
    { fs = new FileStream(ThisPathAndName, FileMode.Open);
      { StreamReader sr = new StreamReader(fs);
        try
        { sr.BaseStream.Seek(0, SeekOrigin.Begin);
          while (sr.Peek() > -1) FileStrings.Add(sr.ReadLine()); // While not at the end of the file, write to standard output.
          wentWell = true;
        }
        catch (Exception e) { systemMessage = e.Message; }
        finally { if (sr != null) sr.Close(); }
      }
    }
    catch (Exception e) { systemMessage = e.Message; }
    finally { if (fs != null) fs.Close(); }
    if (wentWell) return new Boost(true);
    else return new Boost(false, "System message:\n\n" + systemMessage);
  }
/// <summary>
/// 'AppendIfFileExists': if there is no such file, this is ignored, and a new file is created. If there already
///  is a file with the given file name, then TRUE causes new data to be appened to it, FALSE causes all old data
///  to be erased before new data is written.
/// </summary>
  public static Boost SaveTextToFile(string ThisPathAndName, string TextToSave, bool AppendIfFileExists)
  { bool wentWell = false;
    string systemMessage = "";
    FileStream fs = null; // Outside because the statement 'fs = new..' can raise an error (e.g. wrong file name), and
                          //  'finally' won't have heard of 'fs' in that case. (No such problem for 'sr' below.)
    FileMode fm;  if (AppendIfFileExists) fm = FileMode.Append;  else fm = FileMode.Create;
    try
    { fs = new FileStream(ThisPathAndName, fm, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);
      try
      { sw.Write(TextToSave);
        wentWell = true;
      }
      catch (Exception e) { systemMessage = e.Message; }
      finally { if (sw != null) sw.Close(); }

    }
    catch (Exception e) { systemMessage = e.Message; }
    finally { if (fs != null) fs.Close(); }
    if (wentWell) return new Boost(true);
    else return new Boost(false, "System message:\n\n" + systemMessage);
  }


} // END OF CLASS Filing



} // End of namespace J_Lib

/*
//fo
   NOTES ON FORMAT STRINGS which go into "number.ToString("<format string>"):
   ~~~~~~~~~~~~~~~~~~~~~~~~
   The following is far from exhaustive. It is taken from a list in 'C# Cookbook'
   (O'Reilly series), 2nd. edn, page 65+.  NOT case-sensitive. Nos. after the
   letters are optional. You can't use with .ToString(..) all the things you can
   with e.g. Console.WriteLine(..).

   E3 - exp'l notation, with 3 dec. places  (1.200E+3).
   F3 - fixed point, with 3. dec. places (123.400)
   G5 - whatever format is shortest, with 5 significant digits.
   X4 - hexadecimal version (integer types only), 4 digits forced. (This one IS
          case-sensitive; X --> capital A to F; x --> a to f.


   NOTES ON DELEGATE FUNCTIONS FOR THE RegEx(..) METHOD ABOVE:    //reg
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   You are currently writing your code inside some class (or else you are not using C#).
   Suppose that that class was called 'ThisClass'.

   1. Somewhere within it (e.g. at its end, where it is easy to find), write the
      replacement method. It must be of type 'public string', and it must have just
      one argument, of type 'Match':
           public string LetsReplaceIt(Match macho)
           { return (macho.Value).ToUpper(); } // replace finds with upper case versions.

   2. In the code that plans to call RegEx(..), insert these two lines:
           Class boo = new ThisClass();
           MatchEvaluator myEvaluator = new MatchEvaluator(boo.LetsReplaceIt);
      The key word here is MatchEvaluator. The rest is your choice.
                        _____________________________________

*/


