using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq; // NB: If setting up future class libraries modelled on this, 
// couple this line with menu call "Project | Add Reference | .NET | System.Core" (to invoke the system core DLL).
using System.Text;
// ONE-CLASS UNIT FOR EXTENSION METHODS FOR STANDARD C# / .NET TYPES
// Class JX is contained directly in namespace J_Lib, so 'using J_Lib;' is sufficient heading in calling code.
// NAMING CONVENTION: All extensions to standard types are prefixed by '_', followed by a capital and then small letters.
using Gdk;
using Gtk;

namespace JLib
{
  public static class JX
  { 
    public static Strub[] SpecialDoubles = new Strub[]
    { new Strub(double.MaxValue, "1.79769313486232E+308"),
      new Strub(double.MinValue, "-1.79769313486232E+308"),
      new Strub(double.PositiveInfinity, "Infinity"),
      new Strub(double.NegativeInfinity, "-Infinity"),  new Strub(double.NaN, "NaN")
    }; // Why MaxValue and MinValue strings? Because the values which Mono outputs for these are as above; but the values used
       // internally have a slightly nonexponential part - ending in "...48623157" - so that attempts to parse these output values
       // crash Mono's 'double.Parse(.)' method.



 // ++++ BOOL EXTENSIONS ++++           //char
    public static int _ToInt(this bool Bool) { if (Bool) return 1;  else return 0; }

    public static double _ToDub(this bool Bool) { if (Bool) return 1.0;  else return 0.0; }
/// <summary>
/// Only valid values for 'Model' are: "T", "t", "TRUE", "True", "true". Any other values (or no argument) have the same effect as "True".
/// </summary>
    public static string _ToString(this bool Bool, params string[] Model)
    { string muddle = "";
      if (Model.Length > 0) muddle = Model[0];
      string ss;
      if      (muddle == "T")    { if (Bool) ss = "T";     else ss = "F"; }
      else if (muddle == "t")    { if (Bool) ss = "t";     else ss = "f"; }
      else if (muddle == "TRUE") { if (Bool) ss = "TRUE";  else ss = "FALSE"; }
      else if (muddle == "true") { if (Bool) ss = "true";  else ss = "false"; }
      else                       { if (Bool) ss = "True";  else ss = "False"; }
      return ss._CaseMatch(muddle);
    }

 // ++++ CHAR EXTENSIONS ++++           //char

    /// <summary>
    /// Chr._Chain(extent) -- returns a string consisting of repetitions of character Chr up to string length 'extent'.
    /// No errors raised; if extent is less than 1, an empty string is returned. See also Str._Chain(extent).
    /// </summary>
    public static string _Chain(this char Chr, int extent)
    { if (extent <= 0) return String.Empty;
      StringBuilder sb = new StringBuilder(extent); // guaranteed large enough at creation.
      sb.Append(Chr, extent);
      return sb.ToString();
    }

 // ++++ STRING EXTENSIONS ++++            //str
 
 // Strings 1: GET INFORMATION, change nothing:

// ##########*** Rewrite string-searchers to allow for case-insensitive searches, which can done by either:
// ##########*** (1) "if (thisFindStr.ToUpper() == targetString.toUpper())..."   OR
// ##########*** (2) "if (thisFindStr.Equals(targetString, StringComparison.InvariantCultureIgnoreCase))..."
// ##########*** Maybe time the two, and use (1) if there is little difference for search strings of various sizes.

    /// <summary>
    /// Returns -1 for a null string, otherwise returns the same as system string property ".Length".
    /// </summary>
    public static int _Length(this string inStr)
    { if (inStr == null) return -1;  else return inStr.Length; }
    /// <summary>
    /// ss._Last([N]) -- returns last N chars. If N too high: returns whole string.
    /// N zero or neg.: returns empty string. 'ss' empty or null: returns empty string. No errors raised in any situation.
    /// </summary>
    public static string _Last(this string inStr, int noChars)
    { if (string.IsNullOrEmpty(inStr)) return string.Empty;
      int len = inStr.Length; // guaranteed > 0
      if (noChars >= len) return inStr;    if (noChars < 1) return string.Empty;
      // Remaining counts are guaranteed to define a substring of inStr.
      return inStr.Substring(len-noChars, noChars);
    }
    /// <summary>
    /// <para>ss._Last(defChar) -- returns the last character of ss, as a character. If 'ss' is empty or null, returns defChar.</para>
    /// </summary>
    public static char _Last(this string inStr, char defChar)
    { if (string.IsNullOrEmpty(inStr)) return defChar;
      return inStr[inStr.Length-1];
    }
    /// <summary>
    /// ss._Last() -- returns the last character of ss. If 'ss' is empty or null, returns '\u0000'. (If you want the char.
    /// returned as a string, use instead the overloaded method "ss._Last(1)".)
    /// </summary>
    public static char _Last(this string inStr)
    { if (string.IsNullOrEmpty(inStr)) return '\u0000';
      return inStr[inStr.Length-1];
    }
    /// <summary>
    /// ss._CountChar(chars_array [,P [,Q ] ]) returns the no. instances of any chars. in chars_array in string ss between (and including)
    /// ss[P] and ss[Q]. Case-sensitive. No errors raised; adjusts out-ofrange args. as sensibly as possible. Returns 0 for null or empty string.
    /// Duplicate chars. in chars_array are ignored.
    /// </summary>
    public static int _CountChar(this string inStr, char[] CharsToCount, params int[] FromTo)
    { if (inStr == null || CharsToCount == null) return 0;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return 0; // all errors return -1 for trio.X
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      char[] inChars = inStr.ToCharArray(fromPtr, extent);
      int findlen = CharsToCount.Length;
      int cnt = 0;  char ch;
      for (int i=0; i < extent; i++) 
      { ch = inChars[i];
        for (int j=0; j < findlen; j++)
        { if (ch == CharsToCount[j]) { cnt++; break; } }
      }
      return cnt;
    }
    /// <summary>
    /// ss._CountChar(str [,P [,Q ] ]) returns the no. instances  in 'ss' of any chars. listed in string 'str', searching between (and including)
    /// ss[P] and ss[Q]. Case-sensitive. No errors raised; adjusts out-ofrange args. as sensibly as possible. Returns 0 for null or empty string.
    /// Duplicate chars. in 'str' are ignored. Example: ("aabc")._CountChar("ab") returns 3.
    /// </summary>
    public static int _CountChar(this string inStr, string CharsToCount, params int[] FromTo)
    { if (string.IsNullOrEmpty(CharsToCount)) return 0;
      return _CountChar(inStr, CharsToCount.ToCharArray(), FromTo);
    }
    /// <summary>
    /// ss._CountChar(chr [,P [,Q ] ]) returns the no. instances of character 'chr' in string ss between (and including)
    /// ss[P] and ss[Q]. Case-sensitive. No errors raised; adjusts out-ofrange args. as sensibly as possible. Returns 0 for null or empty string.
    /// </summary>
    public static int _CountChar(this string inStr, char CharToCount, params int[] FromTo)
    { return _CountChar(inStr, new char[] {CharToCount}, FromTo);
    }
    /// <summary>
    /// ss._CountStr(substr [,P [,Q ] ]) returns the no. instances of substring 'substr' in string ss which are complete between (and including)
    /// ss[P] and ss[Q]. Case-sensitive. No errors raised; adjusts out-ofrange args. as sensibly as possible. Returns 0 for null or empty string.
    /// Repeated chars.: if ss is "aaaa", ss._CountStr("aa") returns 2, ss._CountStr("aaa") returns 1.
    /// </summary>
    public static int _CountStr(this string inStr, string target, params int[] FromTo)
    { if (inStr == null || target == null) return 0;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y;
      if (fromPtr < 0) return 0;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      int ptr = fromPtr, result = 0, targetlen = target.Length;
      while (true)
      { ptr = inStr.IndexOf(target, ptr);  
        if (ptr == -1 || ptr > toPtr + 1 - targetlen) break; // latter: target found, but it extends beyond toPtr.
        result++;  
        ptr += targetlen;  if (ptr > toPtr + 1 - targetlen) break; // test nec. to avoid crashing .IndexOf(.) in next loop
      }
      return result;
    }
    /// <summary>
    /// Str._CountPlus(chr [, P [, Q ] ]) detects no. instances of 'chr' between Str[P] and Str[Q] inclusive.
    /// RETURNED: .X = no. instances; .Y, .Z point to first and last instances (-1, if none). Case-sensitive. No errors raised; faulty
    /// arguments adjusted for as best as possible. Null or empty string returns .X = 0.
    /// </summary>
    public static Trio _CountPlus(this string inStr, char target, params int[] FromTo)
    { if (string.IsNullOrEmpty(inStr)) return new Trio(0, -1, -1);
      return inStr.ToCharArray()._CountPlus(target, FromTo);
    }
   /// <summary>
   /// <para>The extent itself is not checked; but if on either side of it there are no word-negating chars., then TRUE
   ///  is returned. FALSE is returned if (a) word-negating chars. occur at either end, or (b) if args. faulty.</para>
   /// <para>A StringBuilder extension with the same name exists, more suited to repeated calls over the same text.</para>
   /// </summary>
    public static bool _IsWholeWord(this string inStr, int startPtr, int extent, ref string WordNegators)
    { int inLen = inStr.Length; if (extent < 1 || startPtr < 0 || startPtr + extent > inLen) return false;
      bool beginsOk = false, endsOk = false;
      beginsOk = (startPtr == 0 || WordNegators.IndexOf(inStr[startPtr-1]) == -1);
      if (beginsOk)
      { endsOk = (startPtr + extent == inLen || WordNegators.IndexOf(inStr[startPtr + extent]) == -1); }
      return beginsOk && endsOk;
    }

//   /// <summary>
//   /// <para>The extent itself is not checked; but if on either side of it there are no word-negating chars., then TRUE
//   ///  is returned. FALSE is returned if (a) word-negating chars. occur at either end, or (b) if args. faulty.</para>
//   /// <para>A StringBuilder extension with the same name exists, more suited to repeated calls over the same text.</para>
//   /// </summary>
//    public static bool _IsWholeWord1(this string inStr, int startPtr, int extent, ref string WordNegators)
//    { int inLen = inStr.Length; if (extent < 1 || startPtr < 0 || startPtr + extent > inLen) return false;
//      bool beginsOk = false, endsOk = false;
//      if (startPtr == 0) beginsOK = true;
//      else
//      {
//
//      beginsOk = (startPtr == 0 || WordNegators.IndexOf(inStr[startPtr-1]) == -1);
//      if (beginsOk)
//      { endsOk = (startPtr + extent == inLen || WordNegators.IndexOf(inStr[startPtr + extent]) == -1); }
//      return beginsOk && endsOk;
//    }



    /// <summary>
    /// Str._IndexOf(Chr [, fromPtr [, toPtr ] ]) -- returns address of first instance of character Chr in Str, or -1 if none.
    /// <para>If fromPtr and toPtr are present, they define inclusive limits of the search. 
    /// Improper fromPtr and toPtr corrected as best possible (e.g. negative fromPtr --> fromPtr = 0; oversize toPtr --> end of string).</para>
    /// <para>Null or empty 'Str' or crossed pointers cause a return of -1.</para>
    /// <para>This method is used in preference to .NET's 'Str.IndexOf(Chr [, ...] )' IF (a) a null Str must not cause a crash;
    /// and (b) out-of-range 'from' pointer must not cause a crash.</para>
    /// NB - the equivalent of 'toPtr' for .NET's '.IndexOf(.)' method
    /// is an extent, not a 'to' pointer as here.
    /// </summary>
    public static int _IndexOf(this string inStr, char target, params int[] FromTo)
    { if (inStr == null) return -1;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      return inStr.IndexOf(target, fromPtr, extent);
    }
     /// <summary>
    /// Str._IndexOf(Chr [, fromPtr [, toPtr ] ]) -- returns address of first instance of string 'target', or -1 if none.
    /// <para>If fromPtr and toPtr are present, they define inclusive limits of the search.
    /// Improper fromPtr and toPtr corrected as best possible (e.g. negative fromPtr --> fromPtr = 0; oversize toPtr --> end of string).</para>
    /// <para>Null or empty 'Str' or crossed pointers cause a return of -1.</para>
    /// <para>This method is used in preference to .NET's 'Str.IndexOf(target [, ...] )' IF (a) a null Str must not cause a crash;
    /// and (b) out-of-range 'from' pointer must not cause a crash.</para>
    /// NB - the equivalent of 'toPtr' for .NET's '.IndexOf(.)' method
    /// is an extent, not a 'to' pointer as here.
    /// </summary>
    public static int _IndexOf(this string inStr, string target, params int[] FromTo)
    { if (inStr == null) return -1;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      return inStr.IndexOf(target, fromPtr, extent);
    }
   /// <summary>
    /// Str._IndexesOf(target [, P [, Q ] ]) -- returns an integer array of pointers to all instances of character 'target'; or NULL, if none. 
    /// If P and Q are present, they define inclusive limits of the search. Improper P and Q corrected as best possible.
    /// Null or empty 'Str'or crossed pointers cause a return of NULL. No finds also returns NULL.
    /// </summary>
    public static int[] _IndexesOf(this string inStr, char target, params int[] FromTo)
    { return inStr._IndexesOf(target.ToString(), FromTo);
    }
    /// <summary>
    /// Str._IndexesOf(Str1 [, P [, Q ] ]) -- returns an integer array of pointers to all instances of string 'Str1'; or NULL, if none. 
    /// If P and Q are present, they define inclusive limits of the search. Improper P and Q corrected as best possible.
    /// Null or empty 'Str' or 'Str1' or crossed pointers cause a return of NULL. No finds also returns NULL.
    /// </summary>
    public static int[] _IndexesOf(this string inStr, string target, params int[] FromTo)
    { if (string.IsNullOrEmpty(inStr) || string.IsNullOrEmpty(target)) return null;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y;
      if (fromPtr < 0) return  null;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      int ptr = fromPtr;
      List<int> finds = new List<int>();
      int tarlen = target.Length;
      while (true)
      { ptr = inStr.IndexOf(target, ptr);
        if (ptr == -1 || ptr > toPtr - tarlen + 1 ) break;
        finds.Add(ptr);  
        ptr += tarlen;
      }
      if (finds.Count == 0) return null; else return finds.ToArray();
    }
    /// <summary>
    /// Str._IndexOfNth(target, N [, P [, Q ] ]) -- returns an integer pointer to the Nth. instance of 'target' 
    /// (using N = 1 for the first instance); or -1, if no Nth. instance found.
    /// If P and Q are present, they define inclusive limits of the search. (Improper P and Q corrected as best possible.)
    /// Null or empty 'Str' or 'target' or crossed P and Q also cause a return of -1, as does a zero or negative N.
    /// Duplicated chars. in 'target' and Str: If Str = "AAAAAA", then 'Str._IndexOfNth("AAA", 1)' returns 0, 
    /// 'Str._IndexOfNth("AAA", 2)' returns 3.
    /// </summary>
    public static int _IndexOfNth(this string inStr, string target, int Nth, params int[] FromTo)
    { if (Nth < 1) return -1;
      int[] allfinds = inStr._IndexesOf(target, FromTo);
      if (allfinds == null || allfinds.Length < Nth) return -1;
      return allfinds[Nth-1];    
    }
    /// <summary>
    /// Str._IndexOfNth(target, N [, P [, Q ] ]) -- returns an integer pointer to the Nth. instance of 'target' 
    /// (using N = 1 for the first instance); or -1, if no Nth. instance found.
    /// If P and Q are present, they define inclusive limits of the search. (Improper P and Q corrected as best possible.)
    /// Null or empty 'Str' or crossed P and Q also cause a return of -1, as does a zero or negative N.
    /// </summary>
    public static int _IndexOfNth(this string inStr, char target, int Nth, params int[] FromTo)
    { if (Nth < 1) return -1;
      int[] allfinds = inStr._IndexesOf(target, FromTo);
      if (allfinds == null || allfinds.Length < Nth) return -1;
      return allfinds[Nth-1];    
    }
    /// <summary>
    /// Str._IndexOfAny(target [, P [, Q ] ]) -- returns a Duo pointer to the first instance of any character in 'target'.
    /// If P and Q are present, they define inclusive limits of the search. (Improper P and Q corrected as best possible.)
    /// <para>RETURNED, if a find: .X = location in inStr; .Y = index in 'target' of the character found.</para>
    /// If no find, RETURNS (-1, -1). Null or empty 'Str' or 'target' or crossed pointers also cause a return of (-1, -1).
    /// </summary>
    public static Duo _IndexOfAny(this string inStr, char[] target, params int[] FromTo)
    { Duo result = new Duo(-1,-1);
      if (string.IsNullOrEmpty(inStr) || target == null) return result;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  result;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      int ptr = inStr.IndexOfAny(target, fromPtr, extent);
      if (ptr == -1) return result;
      char ch = inStr[ptr];
      for (int i=0; i < target.Length; i++) { if (target[i] == ch) { result.X = ptr;  result.Y = i;   break; } }
      return result;
    }
    /// <summary>
    /// Str._IndexOfAny(targets [, P [, Q ] ]) -- returns a Duo pointer to the first instance of any character in 'targets'.
    /// If P and Q are present, they define inclusive limits of the search. (Improper P and Q corrected as best possible.)
    /// <para>RETURNED, if a find: .X = location in inStr; .Y = index in 'targets' of the character found.</para>
    /// If no find, RETURNS (-1, -1). Null or empty 'Str' or 'targets' or crossed pointers also cause a return of (-1, -1).
    /// </summary>
    public static Duo _IndexOfAny(this string inStr, string targets, params int[] FromTo)
    { Duo result = new Duo(-1,-1);
      if (string.IsNullOrEmpty(targets)) return result;
      return inStr._IndexOfAny(targets.ToCharArray(), FromTo);
    }
    /// <summary>
    /// Str._IndexOfAny(targets [, P [, Q ] ]) -- returns a Duo pointer to the first instance of any string in 'targets'.
    /// If P and Q are present, they define inclusive limits of the search. (Improper P and Q corrected as best possible.)
    /// <para>RETURNED, if a find: .X = location in inStr; .Y = index in string[] 'targets' of the string found.</para>
    /// If no find, RETURNS (-1, -1). Null or empty 'Str' or 'targets' or crossed pointers also cause a return of (-1, -1).
    /// </summary>
    public static Duo _IndexOfAny(this string inStr, string[] targets, params int[] FromTo)
    { Duo result = new Duo(-1,-1);
      if (string.IsNullOrEmpty(inStr) || targets == null) return result;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y;
      if (fromPtr < 0) return  result;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      // Find targets in inStr, replacing each with chigh in a char[] copy of the string:
      int ptr;

      for (int i=0; i < targets.Length; i++)
      { if (String.IsNullOrEmpty(targets[i])) continue; // check for "" nec. because C#'s .IndexOf("") method returns 0.
        ptr = inStr.IndexOf(targets[i], fromPtr);
        if (ptr > toPtr - targets[i].Length + 1) ptr = -1;
        if (ptr != -1 && (result.X == -1 || ptr < result.X) ) { result.X = ptr;  result.Y = i; }
      }
      return result;
    }
    /// <summary>
    /// Str._IndexesOfAny(targets [, P [, Q ] ]) -- returns a Duo array of pointers to all instances of any string 
    /// in 'targets'; or NULL, if none. 
    /// If P and Q are present, they define inclusive limits of the search. Improper P and Q corrected as best possible.
    /// RETURNED array 'result', if a find: result[i].X = first char. of ith. find; result[i].Y = index in 'targets' of found string.
    ///  If no find, NULL is returned.
    /// Duplicate or overlapping strings in targets: only the first met with is found. E.g. for Str = "ABCD" and search array = 
    /// {"ABC", "BC"}), only targets[0] - "ABC" - will be found by this method.
    /// Null or empty 'targets' or crossed pointers cause a return of NULL. Null or empty 'targets[i]' are ignored.
    /// </summary>
    public static Duo[] _IndexesOfAny(this string inStr, string[] targets, params int[] FromTo)
    { if (string.IsNullOrEmpty(inStr) || targets == null) return null;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y;
      if (fromPtr < 0) return  null;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      // Find a character not present in targets:
      char chigh = JS.FirstPrivateChar;
      bool ch_present;  int n=0;
      while (true)
      { ch_present = false;
        for (int i=0; i < targets.Length; i++)
        { if (String.IsNullOrEmpty(targets[i])) continue;
          n = targets[i].IndexOf(chigh);  
          if (n >= 0) { ch_present = true;  chigh++;  break; } 
        }
        if (!ch_present) break; 
      }       
      // Find targets in inStr, replacing each with chigh in a char[] copy of the string:
      char[] inChar = inStr.ToCharArray();
      List<Duo> finds = new List<Duo>();
      int ptr, tarlen;
      for (int i=0; i < targets.Length; i++)
      { string target = targets[i];  
        if (String.IsNullOrEmpty(target)) continue; // check for "" nec. because C#'s .IndexOf("") method returns 0.
        tarlen = target.Length;   ptr = fromPtr;
        while (true)
        { ptr = inStr.IndexOf(target, ptr);
          if (ptr == -1 || ptr > toPtr - tarlen + 1 ) break;
          // A find. Check if it has already been 'taken' in inChar:
          bool taken = false;
          for (int j=ptr; j < ptr+tarlen; j++) { if (inChar[j] != target[j-ptr]) { taken = true; break; } } // find negated - 
                                     // found for another target earlier.
          if (!taken) 
          { finds.Add(new Duo(ptr, i) );  
            for (int j=ptr; j < ptr+tarlen; j++) inChar[j] = chigh;
          }
          ptr += tarlen;
        }
      }  
      if (finds.Count == 0) return null; else return finds.ToArray();
    }
    /// <summary>
    /// Str._IndexOfNot(chr [, P [, Q ] ] ) -- returns location of first character not = 'chr'; or -1, if no such character found.
    /// If P and Q are present, they define inclusive limits of the search. Improper P and Q corrected as best possible.
    /// Null or empty Str or crossed pointers returns -1.
    /// </summary>
    public static int _IndexOfNot(this string inStr, char chr, params int[] FromTo)
    { if (string.IsNullOrEmpty(inStr)) return -1;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      char[] inChars = inStr.ToCharArray(fromPtr, extent);
      int inlen = inChars.Length,  result = -1;
      for (int i=0; i < inlen; i++) { if (inChars[i] != chr) { result = fromPtr + i; break; }  }
      return result;
    }      
    /// <summary>
    /// Str._IndexOfNot(checkList [, P [, Q ] ] ) -- returns location of first character not in checkList'; or -1, if no such character found.
    /// If P and Q are present, they define inclusive limits of the search. Improper P and Q corrected as best possible.
    /// Null or empty Str or crossed pointers returns -1.
    /// </summary>
    public static int _IndexOfNoneOf(this string inStr, char[] checkList, params int[] FromTo)
    { if (checkList == null || checkList.Length == 0) return -1;
      StringBuilder sb = new StringBuilder(0);   sb.Append(checkList);
      return inStr._IndexOfNoneOf(sb.ToString(), FromTo);
    }
    /// <summary>
    /// Str._IndexOfNot(checkList [, P [, Q ] ] ) -- returns location of first character not in checkList'; or -1, if no such character found.
    /// If P and Q are present, they define inclusive limits of the search. Improper P and Q corrected as best possible.
    /// Null or empty Str or crossed pointers returns -1.
    /// </summary>
    public static int _IndexOfNoneOf(this string inStr, string checkList, params int[] FromTo)
    { if (string.IsNullOrEmpty(inStr) || string.IsNullOrEmpty(checkList) ) return -1;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      char[] inChars = inStr.ToCharArray(fromPtr, extent);
      int inlen = inChars.Length;
      for (int i=0; i < inlen; i++)
      { if (checkList.IndexOf(inChars[i]) == -1) return fromPtr + i; }
      return -1;
    }
    /// <summary>
    /// Str._LastIndexOf(chr, [P, [, Q] ] ) -- returns location of last instance of 'chr' between Str[P] and Str[Q] inclusive (or
    /// to the end of Str, if Q missing); returns -1, if no such character found. Null or empty Str returns -1. NB: Neither in
    /// C# 2005 nor in C# 2008 does method '.LastIndexOf(chr, P [, Q])' work properly; the arguments do not behave according
    /// to .NET documentation. (Method '.LastIndexOf(chr)' is satisfactory, so I have used it in coding this method.)
    /// </summary>
    public static int _LastIndexOf(this string inStr, char chr, params int[] FromTo)
    { if (inStr == null) return -1;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      // We can't use any integer arguments for C#'s '.LastIndexOf(.)', because they just don't work. So we have to
      //  dissect out the string and apply the simple verion of '.LastIndexOf(.)' to it:
      string stroo = inStr._Extent(fromPtr, extent); // From the above, guaranteed to be nonempty.
      int ptr = stroo.LastIndexOf(chr);
      if (ptr == -1) return ptr; else return ptr + fromPtr;
    }
    /// <summary>
    /// Str._LastIndexOfAny(chrArr, [P, [, Q] ] ) -- returns location of last instance of any character in 'chrArr' 
    /// between Str[P] and Str[Q] inclusive (or to the end of Str, if Q missing); returns -1, if no such character found. 
    /// Null or empty Str returns -1. NB: As with '.LastIndexOf(.)', the C# method '.LastIndexOfAny(.)' does not work properly 
    /// when integer arguments are supplied. (Method '.LastIndexOfAny(chrArr)' is satisfactory, so I have used it in coding this method.)
    /// </summary>
    public static int _LastIndexOfAny(this string inStr, char[] chrArr, params int[] FromTo)
    { if (inStr == null) return -1;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      // We can't use any integer arguments for C#'s '.LastIndexOfAny(.)', because they just don't work. So we have to
      //  dissect out the string and apply the simple verion of '.LastIndexOf(.)' to it:
      string stroo = inStr._Extent(fromPtr, extent); // From the above, guaranteed to be nonempty.
      int ptr = stroo.LastIndexOfAny(chrArr);
      if (ptr == -1) return ptr; else return ptr + fromPtr;
    }
    /// <summary>
    /// Str._LastIndexOfAny(Str1, [P, [, Q] ] ) -- returns location of last instance of any character in 'Str1' 
    /// between Str[P] and Str[Q] inclusive (or to the end of Str, if Q missing); returns -1, if no such character found. 
    /// Null or empty Str or Str1 returns -1.
    /// </summary>
    public static int _LastIndexOfAny(this string inStr, string chrStr, params int[] FromTo)
    { if (String.IsNullOrEmpty(chrStr)) return -1;
      char[] chars = chrStr.ToCharArray();
      return inStr._LastIndexOfAny(chars, FromTo);
    }      
    /// <summary>
    /// Str._FirstBeyondAll(chr [, P [, Q ] ] ) -- Given an extent of Str (or the whole of Str, if inclusive limits P and Q
    /// are not supplied), find the first character beyond every instance of 'chr'. There are THREE POSSIBLE RETURNS:
    /// (a) If 'chr' is not found at all, P is returned (or 0, if no P supplied). (b) If the string ends in 'chr', i.e. there is NO
    /// character beyond all 'chr' instances, then -1 is returned. (c) If the last occurrence of 'chr' is found, and is not
    /// at the end of the extent, then the pointer of the first character after that last 'chr' is returned.
    /// (Null 'Str' and an empty extent both return -1 also.) HINT, to save internal computing time and memory: 
    /// If 'Str' is very long, specify a 'from' pointer P which is as close as possible to the 'to' pointer Q (which defaults 
    /// to the end of 'Str', if absent); this is because this method copies the specified extent and then operates only on that.
    /// </summary>
    public static int _FirstBeyondAll(this string inStr, char chr, params int[] FromTo)
    { if (inStr == null) return -1;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      string testStr = inStr._Extent(fromPtr, extent)._Reverse(); // the extent, reversed.
      int find = testStr.IndexOf(chr);
      if (find == -1) return fromPtr;
      else if (find == 0) return -1; // 'chr' is the last character of the extent
      else return fromPtr + extent - find;
    }
    /// <summary>
    /// Str._FirstBeyondAllIn(checkList [, P [, Q ] ] ) -- Given an extent of Str (or the whole of Str, if inclusive limits P and Q
    /// are not supplied), find the first character beyond every instance of a member of checkList. There are THREE POSSIBLE RETURNS:
    /// (a) If no char. in checkList is found, P is returned (or 0, if no P supplied). (b) If the string ends in a member of checkList, 
    /// i.e. there is NO character beyond such as are in checkList, then -1 is returned. (c) In all other cases,
    /// the position of the first character after the last instance of any member of checkList is returned.
    /// Null 'Str' and an empty extent both return -1 also.) HINT, to save internal computing time and memory: 
    /// If 'Str' is very long, specify a 'from' pointer P which is as close as possible to the 'to' pointer Q (which defaults 
    /// to the end of 'Str', if absent); this is because this method copies the specified extent and then operates only on that.
    /// </summary>
    public static int _FirstBeyondAllIn(this string inStr, char[] checkList, params int[] FromTo)
    { if (string.IsNullOrEmpty(inStr) || checkList == null || checkList.Length == 0) return -1;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      string testStr = inStr._Extent(fromPtr, extent)._Reverse(); // the extent, reversed.
      int find = testStr.IndexOfAny(checkList);
      if (find == -1) return fromPtr;
      else if (find == 0) return -1; // 'chr' is the last character of the extent
      else return fromPtr + extent - find;
    }
    /// <summary>
    /// Str._FirstBeyondAllIn(checkList [, P [, Q ] ] ) -- Given an extent of Str (or the whole of Str, if inclusive limits P and Q
    /// are not supplied), find the first character beyond every instance of a member of checkList. There are THREE POSSIBLE RETURNS:
    /// (a) If no char. in checkList is found, P is returned (or 0, if no P supplied). (b) If the string ends in a member of checkList, 
    /// i.e. there is NO character beyond such as are in checkList, then -1 is returned. (c) In all other cases,
    /// the position of the first character after the last instance of any member of checkList is returned.
    /// Null 'Str' and an empty extent both return -1 also.) HINT, to save internal computing time and memory: 
    /// If 'Str' is very long, specify a 'from' pointer P which is as close as possible to the 'to' pointer Q (which defaults 
    /// to the end of 'Str', if absent); this is because this method copies the specified extent and then operates only on that.
    /// </summary>
    public static int _FirstBeyondAllIn(this string inStr, string checkList, params int[] FromTo)
    { if (string.IsNullOrEmpty(checkList) ) return -1;
      return inStr._FirstBeyondAllIn(checkList.ToCharArray(), FromTo);
    }
    /// <summary>
    /// Str._FirstFromEndNot(chr [, P [, Q ] ] ) -- Given an extent of Str (or the whole of Str, if inclusive limits P and Q
    /// are not supplied), search backwards from the end till a character not equal to 'chr' is found, and return its position.
    /// If the string consists only of 'chr', -1 is returned. Null / empty 'Str' also returns -1. HINT, to save internal computing time and memory: 
    /// If 'Str' is very long, specify a 'from' pointer P which is as close as possible to the 'to' pointer Q (which defaults 
    /// to the end of 'Str', if absent); this is because this method copies the specified extent and then operates only on that.
    /// </summary>
    public static int _FirstFromEndNot(this string inStr, char chr, params int[] FromTo)
    { if (inStr == null) return -1;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      char[] chars = inStr.ToCharArray(fromPtr, extent);
      int result = -1;
      for (int i = chars.Length-1; i >= 0; i--)
      { if (chars[i] != chr) { result = fromPtr + i; break; } }
      return result;
    }
    /// <summary>
    /// <para>Str._FirstFromEndNotIn(checkList [, From [, To ] ] ) -- Given an extent of Str (or the whole of Str, if inclusive limits From and To
    /// are not supplied), search backwards from the end till a character not in checkList is found, and return its position.</para>
    /// <para>RETURNED if NO FIND: If the string consists only of members of checkList, -1 is returned. Null / empty 'Str' also returns -1. </para>
    /// <para>HINT, to save internal computing time and memory: If 'Str' is very long, specify a 'from' pointer P which is as close as possible 
    /// to the 'to' pointer Q (which defaults to the end of 'Str', if absent); this is because this method copies the specified extent 
    /// and then operates only on that.</para>
    /// </summary>
    public static int _FirstFromEndNotIn(this string inStr, char[] checkList, params int[] FromTo)
    { string checkStr = "";
      if (checkList != null && checkList.Length > 0)
      { StringBuilder sb = new StringBuilder();   sb.Append(checkList);    checkStr = sb.ToString(); }
      return inStr._FirstFromEndNotIn(checkStr, FromTo);
    }
    /// <summary>
    /// <para>Str._FirstFromEndNotIn(checkList [, From [, To ] ] ) -- Given an extent of Str (or the whole of Str, if inclusive limits From and To
    /// are not supplied), search backwards from the end till a character not in checkList is found, and return its position.</para>
    /// <para>RETURNED if NO FIND: If the string consists only of members of checkList, -1 is returned. Null / empty 'Str' also returns -1. </para>
    /// <para>HINT, to save internal computing time and memory: If 'Str' is very long, specify a 'from' pointer P which is as close as possible 
    /// to the 'to' pointer Q (which defaults to the end of 'Str', if absent); this is because this method copies the specified extent 
    /// and then operates only on that.</para>
    /// </summary>
    public static int _FirstFromEndNotIn(this string inStr, string checkList, params int[] FromTo)
    { if (inStr == null) return -1;
      Trio trio = JS.SegmentData(inStr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y, extent = trio.Z;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      if (String.IsNullOrEmpty(checkList) ) return toPtr;
      char[] chars = inStr.ToCharArray(fromPtr, extent);
      int result = -1;
      for (int i = chars.Length-1; i >= 0; i--)
      { if ( checkList.IndexOf(chars[i]) == -1) { result = fromPtr + i; break; } }
      return result;
    }

 // Strings 2: BLOCK OPERATIONS - NONSELECTIVE:

    /// <summary>
    /// ss._Extent(N [,X]) returns the substring of ss which starts at char. no. N and has length X (default: to end of string). 
    /// No errors raised in any situation. Out of range args. adjusted as sensibly as possible. Null ss returns empty string.
    /// </summary>
    public static string _Extent(this string inStr, int startPtr, params int[] noChars)
    { if (string.IsNullOrEmpty(inStr)) return string.Empty;
      int len = inStr.Length; // guaranteed > 0
      if (startPtr < 0) startPtr = 0;   if (startPtr >= len) return string.Empty;
      int extent = len; // no problem if (as usually so) startPtr + extent is now beyond end of inStr
      if (noChars.Length > 0) extent = noChars[0]; 
      if (extent <= 0) return string.Empty;
      if (startPtr + extent > len) extent = len - startPtr;
      return inStr.Substring(startPtr, extent); 
    }   
    /// <summary>
    /// ss._FromTo(N,P) returns the substring of ss which starts at char. no. N and ends at char. no. P. No errors raised in any situation.
    /// Out of range args. adjusted as sensibly as possible. Null ss returns empty string.
    /// </summary>
    public static string _FromTo(this string inStr, int fromPtr, int toPtr)
    { if (fromPtr < 0) fromPtr = 0; // All other argument messups are (hopefully) covered by ._Extent(.). 
      return _Extent(inStr, fromPtr, toPtr - fromPtr + 1); // leave handling of faulty inStr or params. to the called method.
    }
    /// <summary>
    /// Returns the substring of ss which starts one char. AFTER fromPtr and ends once char. BEFORE toPtr. No errors raised in any situation.
    /// Args. out of range will not raise errors but will produce unreliable results. Null ss returns empty string.
    /// </summary>
    public static string _Between(this string inStr, int fromPtr, int toPtr)
    { return _Extent(inStr, fromPtr+1, toPtr - fromPtr - 1); // leave handling of faulty inStr or params. to the called method.
    }
    /// <summary>
    /// Str._Scoop(fromPtr, extent [, fillStr ]) -- return a copy of Str omitting the segment which starts at Str[fromPtr]
    /// and extends for 'extent' chars. No error states; improper arguments corrected. If present, 'fillStr' is always inserted 
    /// (e.g. if 'Str' is null, fillStr is returned). This method acts as an 'insert(.)' instruction if 'extent' is 0.
    /// </summary>
    /// <summary>
    /// Str._Scoop(fromPtr, extent [, fillStr ]) -- return a copy of Str omitting the segment which starts at Str[fromPtr]
    /// and extends for 'extent' chars. No error states; improper arguments corrected. If present, 'fillStr' is always inserted 
    /// (e.g. if 'Str' is null, fillStr is returned). This method acts as an 'insert(.)' instruction if 'extent' is 0.
    /// </summary>
    public static string _Scoop(this string inStr, int fromPtr, int extent, params string[] fillStr)
    { string filler = "";  if (fillStr.Length > 0) filler = fillStr[0];
      if (string.IsNullOrEmpty(inStr)) return filler;
      if (fromPtr < 0) fromPtr = 0;  if (extent < 0) extent = 0;
      return inStr._Extent(0, fromPtr) + filler + inStr._Extent(fromPtr + extent);
    }
    /// <summary>
    /// Str._ScoopTo(fromPtr, toPtr [, fillStr ]) -- return a copy of Str omitting the segment from Str[fromPtr] to Str[toPtr] inclusive.
    /// No error states; improper arguments corrected. If present, 'fillStr' is always inserted (e.g. if 'Str' is null,
    /// fillStr is returned).
    /// </summary>
    public static string _ScoopTo(this string inStr, int fromPtr, int toPtr, params string[] fillStr)
    { return inStr._Scoop(fromPtr, toPtr - fromPtr + 1, fillStr);
    }
    /// <summary>
    /// Str._Insert(atPtr, fillStr) -- inserts 'fillStr' at 'atPtr'. If 'atPtr' zero or negative, adds to the start; if at or beyond Str.Length,
    /// adds to the end. If Str is empty or null, returns fillStr. Null or empty fillStr: return is simply Str. No errors ever raised.
    /// </summary>
    public static string _Insert(this string inStr, int atPtr, string fillStr)
    { string result = inStr;
      if (result == null) result = String.Empty;
      if (String.IsNullOrEmpty(fillStr)) return result;
      if (atPtr < 1) result = fillStr + result; // this and next condition not logically nec., but they save a bit of handling time.
      else if (atPtr >= result.Length) result += fillStr;
      else result = result._Extent(0, atPtr) + fillStr + result._Extent(atPtr);  
      return result;
    }
    /// <summary>
    /// Str._OverwriteTo(char withChar ,fromPtr, toPtr) -- Overwrites all of the string between limits (inclusive)with the char. 'withChar'.
    /// No error states; does its best with wrong arguments or null / empty string.
    /// </summary>
    public static string _Overwrite(this string inStr, char withChar, int fromPtr, int toPtr)
    { if (string.IsNullOrEmpty(inStr)) return "";
      if (fromPtr < 0) fromPtr = 0;
      int inlen = inStr.Length;
      if (toPtr >= inlen) toPtr = inlen-1;
      int extent = toPtr - fromPtr + 1;
      if (extent < 1) return inStr;
      return inStr._Extent(0, fromPtr) + withChar._Chain(extent) + inStr._Extent(toPtr+1);
    }
    /// <summary>
    /// Str._Overwrite(char withChar ,fromPtr [, extent ])) -- Overwrite part of a string with the char. 'withChar'.
    /// No error states; does its best with wrong arguments or null / empty string.
    /// </summary>
    public static string _Overwrite(this string inStr, char withChar, int fromPtr, params int[] extent)
    { if (string.IsNullOrEmpty(inStr)) return "";
      int howmany = inStr.Length; if (extent.Length > 0) howmany = extent[0];
      if (howmany < 1) return inStr; // no overwriting
      if (fromPtr < 0) fromPtr = 0;
      int toPtr = fromPtr + howmany - 1;
      if (toPtr >= inStr.Length) toPtr = inStr.Length-1;
      return inStr._Extent(0, fromPtr) + withChar._Chain(howmany) + inStr._Extent(toPtr+1);
    }
    /// <summary>
    /// Remove a single character from the end of Str. Returns the empty string if Str is null or empty. No errors raised in any situation.
    /// </summary>
    public static string _CullEnd(this string inStr)
    { return inStr._Extent(0, inStr.Length - 1);
    }
    /// <summary>
    /// Remove 'count' chars. from the end of Str. Returns empty string if Str is null or if its length
    /// is less than or equal to 'count'. No errors raised in any situation.
    /// </summary>
    public static string _CullEnd(this string inStr, int count)
    { 
      return inStr._Extent(0, inStr.Length - count);
    }
   // No 'Str._CullFirst(p)' analogue, as 'Str._Extent(p)' achieves this result with less typing.

    /// <summary>
    /// Str._Chain(extent) -- returns a string consisting of repetitions of Str up to string length 'extent'. If extent
    /// is not an exact multiple of Str.Length, the last repetition will be truncated. No errors raised; impossible args. return "".
    /// See also Chr._Chain(extent), where 'Chr' is a charactoer.
    /// </summary>
    public static string _Chain(this string inStr, int extent)
    { if (string.IsNullOrEmpty(inStr) || extent <= 0) return String.Empty;
      int inlen = inStr.Length;
      StringBuilder sb = new StringBuilder(extent + inlen); // guaranteed large enough at creation.
      while (sb.Length < extent) sb.Append(inStr);
      return sb.ToString(0, extent);
    }
    /// <summary>
    /// Str._PadTo(minLen, padChar, padEnd ) -- If Str has length less than minLen, the returned copy is padded to that length. 
    /// (If longer, Str returned unchanged.) The padding character padChar goes either at the beginning or end of Str, according
    /// to the boolean padEnd. Null or empty Str allowed. minLen below 1 returns an empty string.
    /// </summary>
    public static string _PadTo(this string inStr, int minLen, char padChar, bool padEnd)
    { string result = inStr;  if (inStr == null || minLen < 1) result = String.Empty;
      int inlen = result.Length;
      if (inlen >= minLen) return inStr;
      string ss = padChar._Chain(minLen - inlen); 
      if (padEnd) result += ss;  else result = ss + result; 
      return result;
    }
    /// <summary>
    /// <para>Str._FixedLength(extent, padStr [, padEnd [, truncateEnd ] ] ) -- returns a string of length 'extent', by padding
    /// or truncating 'Str' as necessary. 'padEnd', if 'true' (the default), pads onto the end of Str; if 'false', at the start.
    /// 'truncateEnd' truncates likewise at the end (the default) or the start.</para> 
    /// <para>No errors raised; empty / null padStr defaults to the SPACE character.</para>
    /// <para>For more elaborate treatment (adding e.g. "..." as sign of truncation, and optional enclosure in e.g. quotes), see
    /// method JS.ShortQuote(.)</para>
    /// </summary>
    public static string _FixedLength(this string inStr, int extent, string padStr, params bool[] Flags)
    { if (extent <= 0) return String.Empty;
      string result = inStr;  if (inStr == null) result = String.Empty;
      string padder = padStr; if (string.IsNullOrEmpty(padStr)) padder = " "; 
      bool padEnd = true, truncateEnd = true;
      if (Flags.Length > 0) padEnd = Flags[0];
      if (Flags.Length > 1) truncateEnd = Flags[1];
      int inlen = result.Length;
      if (inlen < extent) 
      { string ss = padder._Chain(extent - inlen);
        if (padEnd) result += ss;  else result = ss + result;
      }
      else if (inlen > extent)
      { if (truncateEnd) result = result.Substring(0, extent);
        else result = result.Substring(inlen - extent);
      }
      return result;
    }
    /// <summary>
    /// <para>Over some range within a string, inserts 'insertThis' between all characters within the range.</para>
    /// <para>Suppose inStr = "12ABC34", startPtr = 2 and extent = 3, and insertThis is " - "; the output string will be "12A - B - C34".</para>
    /// <para>Out-of-range startPtr and extent are adjusted as possible. In particular, if extent is &lt;= 0, or too large,
    /// it is adjusted to reach to the end of the string.</para>
    /// <para>No errors are raised. Null, empty or single-char. strings are returned as is.</para>
    /// </summary>
    public static string _Interleave(this string inStr, int startPtr, int extent, string insertThis)
    { if (String.IsNullOrEmpty(inStr) ) return inStr;
      int inlen = inStr.Length;
      if (startPtr > inlen-2 || extent == 0) return inStr;
      else if (startPtr < 0) startPtr = 0;
      if (extent < 0) extent = inlen;
      if (startPtr + extent >= inlen) extent = inlen - startPtr;
      StringBuilder sb = new StringBuilder();
      char[] inChar = inStr.ToCharArray(startPtr, extent);
      if (startPtr > 0) sb.Append(inStr._Extent(0, startPtr));
      for (int i=0; i < extent; i++)
      { sb.Append(inChar[i]);
        if (i < extent-1) sb.Append(insertThis);
      }
      string ss = inStr._Extent(startPtr+extent);
      if (ss != "") sb.Append(ss);
      return sb.ToString();
    }
    /// <summary>
    /// Returns a string which consists of noTimes repetitions of inStr. No errors raised; if inStr is null or empty or noTimes is zero
    /// or negative, the empty string is returned.
    /// </summary>
    public static string _Repeat(this string inStr, int noTimes)
    { if (String.IsNullOrEmpty(inStr) || noTimes < 1 ) return "";
      StringBuilder sb = new StringBuilder(noTimes * inStr.Length);
      for (int i=0; i < noTimes; i++) sb.Append(inStr);
      return sb.ToString();
    }
 
 // Strings 3: BLOCK OPERATIONS - SELECTIVE:

    /// <summary>
    /// Str._ForceLast(chr) -- if Str ends in char 'chr' already, this simply returns a copy of Str. If not,
    /// it returns Str + chr. A null or empty string returns with content chr.
    /// </summary>
    public static string _ForceLast(this string inStr, char chr)
    { if (string.IsNullOrEmpty(inStr)) return chr.ToString();
      string result = inStr;
      if (inStr[inStr.Length-1] != chr) result += chr;
      return result;
    }

    /// <summary>
    /// Str._CullEndTill(charArr [, maxCull] ) -- remove terminal chars. till one in charArr found (which leave). If 'maxCull' present,
    /// will not check / remove more than 'maxCull' characters.
    /// </summary>
    public static string _CullEndTill(this string inStr, char[] charArray, params int[] maxCull)
    { if (string.IsNullOrEmpty(inStr)) return String.Empty;
      int inlen = inStr.Length,  maxcull = inlen;
      if (maxCull.Length > 0) maxcull = maxCull[0];
      if (maxcull < 1) return inStr;   
      if (maxcull > inlen) maxcull = inlen;
      // inStr has at least one element, and maxcull lies between 1 and inlen:
      int keepextent = inlen - maxcull; // a default value.
      int last = inStr.LastIndexOfAny(charArray);
      if (last >= keepextent) keepextent = last + 1;
      return inStr._Extent(0, keepextent);
    }
    /// <summary>
    /// Str._CullEndTill(chr [, maxCull] ) -- remove terminal chars. till 'chr' found (which leave). If 'maxCull' present,
    /// will not check / remove more than 'maxCull' characters.
    /// </summary>
    public static string _CullEndTill(this string inStr, char chr, params int[] maxCull)
    { return inStr._CullEndTill(new char[] {chr}, maxCull); 

    }
    /// <summary>
    /// <para>Intended for use before a 'Split' operation, where it is planned to create an array of fixed size starting from a given 
    /// delimited string.</para>
    /// <para>If there are more than noDelims instances of delim in inStr, amputation will occur starting at the (noDelims+1)th. delim. 
    /// If there are less, padding will occur with sequences of [delim][padder] until the correct number is reached. (If inStr is empty, 
    /// an extra padder will be added first.)</para>
    /// <para>No errors are raised. Funny args.: noDelims &lt; 0 --> return of untouched inStr, as does null or empty delim. Zero noDelims
    /// amputates from the first delimiter found, if any. Empty (or null) padder is allowed. Null or empty inStr --> 
    /// sequence [padder][delim]...[delim][padder] with (noDelims+1) instances of padder.</para>
    /// </summary>
    public static string _FixedNoDelims(this string inStr, string delim, int noDelims, string padder)
    { if (noDelims < 0 || String.IsNullOrEmpty(delim) ) return inStr;
      if (padder == null) padder = "";
      if (String.IsNullOrEmpty(inStr) ) inStr = padder;
      int[] delimery = inStr._IndexesOf(delim);
      if (delimery == null) return inStr + (delim + padder)._Repeat(noDelims); // no delimiters found in inStr.
      int noDelimsFound = delimery.Length;
      if (noDelimsFound <= noDelims) return inStr + (delim + padder)._Repeat(noDelims - noDelimsFound); // if equal, latter part returns ""
      else return inStr._Extent(0, delimery[noDelims]);
    }

    /// <summary>
    /// Starting from the end of inStr, remove terminal chars. as long as they are in charArray; stop the search as soon as
    /// one not in charArray is found. If 'maxCull' is present, will not check / remove more than 'maxCull' characters.
    /// </summary>
    public static string _CullEndTillNot(this string inStr, char[] charArray, params int[] maxCull)
    { if (string.IsNullOrEmpty(inStr)) return String.Empty;
      int inlen = inStr.Length,  maxcull = inlen;
      if (maxCull.Length > 0) maxcull = maxCull[0];
      if (maxcull < 1) return inStr;   
      if (maxcull > inlen) maxcull = inlen;
      // inStr has at least one element, and maxcull lies between 1 and inlen:
      int keepextent = inlen - maxcull; // a default value.
      int last = inStr._FirstFromEndNotIn(charArray);
      if (last >= keepextent) keepextent = last + 1;
      return inStr._Extent(0, keepextent);
    }
    /// <summary>
    /// Starting from the end of inStr, remove the terminal char. as long as it is 'chr'; stop the search as soon as any other char. found.
    /// If 'maxCull' is present, will not check / remove more than 'maxCull' characters.
    /// </summary>
    public static string _CullEndTillNot(this string inStr, char chr, params int[] maxCull)
    { return inStr._CullEndTillNot(new char[] {chr}, maxCull); 
    }

 // Strings 4: DIFFUSE ALTERATIONS:

    /// <summary>
    /// Str._CaseMatch(model) -- returns Str with case altered in accordance with the formatting cue 'model'. The elements 
    /// of 'model' can only be 'A', 'a', '_' (underline, not dash) and '.' 
    /// Recognized instances of 'model': "A" --> all capitals; "a" --> all letters small; "Aa" --> Str[0] capitalized, all other
    /// chars. small; "Aa_Aa" --> separate words all start with a capital, rest of word being small; "Aa.Aa" puts capitals only
    /// after punctuation marks ('?','!','.') and at start; rest are small. Any other 'model' causes return of unchanged Str.
    /// </summary>
    public static string _CaseMatch(this string inStr, string model)
    { if (string.IsNullOrEmpty(inStr)) return String.Empty;
      if (string.IsNullOrEmpty(model) || model.IndexOf('#') != -1) return inStr; // we reserve '#' for what follows.
      string stroo = "#A######a######Aa#####Aa_Aa##Aa.Aa#";
      int modelno = stroo.IndexOf('#' + model + '#');
      if (modelno == -1) return inStr;
      modelno /= 7;
      switch (modelno)
      { case 0: return inStr.ToUpper(); // "A"
        case 1: return inStr.ToLower(); // "a"
        case 2: // "Aa"
        { return char.ToUpper(inStr[0]) + inStr._Extent(1).ToLower(); } // ("").ToLower() does not raise an error.
        case 3: // "Aa_Aa" - every word has an initial capital letter.
        { char[] charr = inStr.ToLower().ToCharArray();  int len = charr.Length;
          char ch, prev = ' '; // i.e. something not in 'nullifiers' below.
          string nullifiers = JS.IdentifierChars + '-'; // all letters + digits + '-' + '_' negate capitals.
          for (int i=0; i < len; i++)
          { ch = charr[i];
            if (nullifiers.IndexOf(prev) == -1) charr[i] = char.ToUpper(ch);
            prev = ch;
          }
          return charr._ToString();
        }    
        case 4: // "Aa.Aa" - every word after key punctuation marks has an initial capital letter.
        { char[] charr = inStr.ToLower().ToCharArray();  int len = charr.Length;
          char ch, prevnonblank = '.'; // i.e. something in 'capitalizers' below.
          string capitalizers = "?!.";
          for (int i=0; i < len; i++)
          { ch = charr[i];
            if (ch == ' ') continue;
            if (capitalizers.IndexOf(prevnonblank) >= 0) charr[i] = char.ToUpper(ch);
            prevnonblank = ch;
          }
          return charr._ToString();
        }    
        default: return inStr;
      }  
    }
    /// <summary>
    /// Str._Reverse() -- returns the string in reversed order. A null string returns the empty string. (Note that for arrays
    /// there is a .NET method "Array.Reverse(arr)". )
    /// </summary>
    public static string _Reverse(this string inStr)// For arrays, there is already a system method: "Array.Reverse(Arr1);"
    { if (string.IsNullOrEmpty(inStr)) return String.Empty;
      char[] charr = inStr.ToCharArray();     
      Array.Reverse(charr); // A looping method (feeding sb from charr, looping backwards) took 50% longer.
      StringBuilder sb = new StringBuilder(charr.Length);  sb.Append(charr);
      return sb.ToString();
    }
    /// <summary>
    /// Str._Purge(chr1 [, chr2 [, chr3.. ] ] ) -- returns Str with all instances of character(s) chr1, chr2, chr3,... removed.
    /// Case-sensitive. Str._Purge() removes all characters from U+0000 to U+0032 inclusive. Null and empty strings tolerated.
    /// (For white spaces, use 'JS.WhiteSpaces' as argument.)
    /// </summary>
    public static string _Purge(this string inStr, params char[] Unwanted)
    { if (string.IsNullOrEmpty(inStr)) return String.Empty;
      char[] chars = inStr.ToCharArray();
      StringBuilder sbldr = new StringBuilder();
      int unlen = Unwanted.Length;
      if (unlen == 0)
      { foreach (char ch in chars) { if (ch > '\u0020') sbldr.Append(ch); } }
      else
      { bool foundone;
        foreach (char ch in chars)
        { foundone = false;
          foreach (char ch1 in Unwanted) 
          { if (ch == ch1) { foundone = true; break; } }
          if (!foundone) sbldr.Append(ch);
        }
      }  
      return sbldr.ToString(); // OK if sbldr is empty.
    }
    /// <summary>
    /// <para>Str._Purge(fromPtr, toPtr, chr1 [, chr2 [, chr3.. ] ] ) -- returns Str with all instances of character(s) chr1, chr2, chr3,... 
    /// removed at and between the given limits. Case-sensitive. Str._Purge() and Str.Purge(fromPtr, toPtr) remove all characters from 
    /// U+0000 to U+0032 inclusive. Null and empty strings tolerated. Impossible pointers corrected; also, negative toPtr is corrected 
    /// to point to the end of inStr. (For white spaces, use 'JS.WhiteSpaces' as argument.)</para>
    /// </summary>
    public static string _Purge(this string inStr, int fromPtr, int toPtr, params char[] Unwanted)
    { string Purged = "";
      return inStr._Purge(fromPtr, toPtr, out Purged, false, Unwanted);
    }  
    /// <summary>
    /// <para>Str._Purge(fromPtr, toPtr, chr1 [, chr2 [, chr3.. ] ] ) -- returns Str with all instances of character(s) chr1, chr2, chr3,... 
    /// removed at and between the given limits. Case-sensitive. Str._Purge() and Str.Purge(fromPtr, toPtr) remove all characters from 
    /// U+0000 to U+0032 inclusive. Null and empty strings tolerated. Impossible pointers corrected; also, negative toPtr is corrected 
    /// to point to the end of inStr. (For white spaces, use 'JS.WhiteSpaces' as argument.)</para>
    /// <para>In this overload, purged characters are listed in 'out Purged' (without duplication).</para>
    /// </summary>
    public static string _Purge(this string inStr, int fromPtr, int toPtr, out string Purged, params char[] Unwanted)
    { return inStr._Purge(fromPtr, toPtr, out Purged, true, Unwanted);
    }  
     // PRIVATE method used by above two:
    private static string _Purge(this string inStr, int fromPtr, int toPtr, out string Purged, bool usePurged, params char[] Unwanted )
    { Purged = "";
      if (string.IsNullOrEmpty(inStr)) return String.Empty;
      if (fromPtr < 0) fromPtr = 0;  
      if (toPtr < 0 || toPtr >= inStr.Length) toPtr = inStr.Length-1;
      if (fromPtr > toPtr) return String.Empty;
      // Now the pointers will always correctly define a finite piece of the string:
      StringBuilder retained = new StringBuilder();
      if (fromPtr > 0) retained.Append(inStr._Extent(0, fromPtr) ); // the part before the 'purge' section.
      char[] chars = inStr.ToCharArray(fromPtr, toPtr-fromPtr+1);
      int unlen = Unwanted.Length;
      if (unlen == 0)
      { foreach (char ch in chars) { if (ch > '\u0020') retained.Append(ch); } } // 'Purged' not filled in this case.
      else
      { bool foundone;
        foreach (char ch in chars)
        { foundone = false;
          foreach (char ch1 in Unwanted) 
          { if (ch == ch1) 
            { foundone = true;  
              if (usePurged)
              { bool found = false;
                for (int i=0; i < Purged.Length; i++) { if (Purged[i] == ch) { found = true; break; } }
                if (!found) Purged += ch;
              }
              break;
            }    
          }
          if (!foundone) retained.Append(ch);
        }
      }  
      if (toPtr < inStr.Length-1) retained.Append(inStr._Extent(toPtr+1)); // the part after the 'purge' section
      return retained.ToString(); // OK if retained is empty.
    }
    /// <summary>
    /// <para>Str._PurgeExtent(fromPtr, toPtr, only_0to32, theseMustStay ) - Two distinct forms:</para>
    /// <para>(1) Boolean 'only_0to32' is 'true': returns a copy of Str with ONLY characters #0 to #32 removed, EXCEPTING such of these 
    /// as are listed in 'theseMustStay'. (Any other higher chars. in this array will simply be ignored, as ALL chars. higher than ' ' 
    /// will be retained automatically.)</para>
    /// <para>(2) 'only_0to32' is 'false': simply removes ALL chars. from the copy of Str, EXCEPTING any listed in 'theseMustStay'.</para>
    /// <para>In both cases, null and empty strings tolerated. Impossible pointers corrected. (To keep white spaces, use 'JS.WhiteSpaces' 
    /// as argument.)</para>
    /// <para>Pointers are corrected as possible; in particular, toPtr of -1 (or less) or oversized --> toPtr set to last char. of InStr.</para>
    /// </summary>
    public static string _PurgeExcept(this string inStr, int fromPtr, int toPtr, bool only_0to32, params char[] theseMustStay)
      // The pointers go first (uncharacteristically) so that Unwanted can be a params array, allowing either single char(s). or a char. array as arg.
    { if (string.IsNullOrEmpty(inStr)) return String.Empty;
      if (fromPtr < 0) fromPtr = 0;  
      if (toPtr < 0 || toPtr >= inStr.Length) toPtr = inStr.Length-1;
      if (fromPtr > toPtr) return String.Empty;
      int staylen = theseMustStay.Length;
      if (staylen == 0) return String.Empty; // In this case, the boolean arg. is irrelevant.
      // Now the pointers will always correctly define a finite piece of the string:
      string stayers = theseMustStay._ToString();
      StringBuilder sbldr = new StringBuilder();
      if (fromPtr > 0) sbldr.Append(inStr._Extent(0, fromPtr) ); // the part before the 'purge' section.
      char[] chars = inStr.ToCharArray(fromPtr, toPtr-fromPtr+1);
      foreach (char ch in chars) 
      { if ( (only_0to32 && ch > '\u0020') || stayers.IndexOf(ch) >= 0) sbldr.Append(ch); }
      if (toPtr < inStr.Length-1) sbldr.Append(inStr._Extent(toPtr+1)); // the part after the 'purge' section
      return sbldr.ToString(); // OK if sbldr is empty.
    }
    /// <summary>
    /// <para>Returns Str with all instances removed of character(s) within the given substring range
    ///  which have unicodes between, or at, those of LoChar and HiChar. Case-sensitive.</para>
    /// <para>Returns the empty string if (a) the input string is empty or null, or (b) all characters were in the unwanted range,
    ///  or (c) if the unicode of LoChar is greater than that of HiChar.</para>
    /// <para>If toPtr is negative or oversized, it is reset to point to the end of the string.</para>
    /// </summary>
    public static string _PurgeRange(this string inStr, int fromPtr, int toPtr, char LoChar, char HiChar)
    { if (string.IsNullOrEmpty(inStr) || LoChar > HiChar) return String.Empty;
      if (fromPtr < 0) fromPtr = 0;  
      if (toPtr < 0 || toPtr >= inStr.Length) toPtr = inStr.Length-1;
      if (fromPtr > toPtr) return String.Empty;
      // Now the pointers will always correctly define a finite piece of the string:
      char[] chars = inStr.ToCharArray(fromPtr, toPtr-fromPtr+1);
      StringBuilder sb = new StringBuilder();
      foreach (char ch in chars) { if (ch < LoChar || ch > HiChar) sb.Append(ch); }
      return sb.ToString(); // OK if sb is empty.
    }
    /// <summary>
    /// <para>Remove all characters of inStr which DON'T have values between LoChar and HiChar inclusive.</para>
    /// <para>Pointers are corrected as possible; in particular, toPtr of -1 (or less) or oversized --> toPtr set to last char. of InStr.</para>
    /// </summary>
    public static string _PurgeExceptRange(this string inStr, int fromPtr, int toPtr, char LoChar, char HiChar)
    { string Purged = null;
      return inStr._PurgeExceptRange(fromPtr, toPtr, LoChar, HiChar, out Purged, false); 
    }
    /// <summary>
    /// <para>Remove all characters of inStr which DON'T have values between LoChar and HiChar inclusive.</para>
    /// <para>Pointers are corrected as possible; in particular, toPtr of -1 (or less) or oversized --> toPtr set to last char. of InStr.</para>
    /// <para>In this overload, purged characters are listed in 'out Purged' (without duplication).</para>
    /// </summary>
    public static string _PurgeExceptRange(this string inStr, int fromPtr, int toPtr, char LoChar, char HiChar, out string Purged)
    { return inStr._PurgeExceptRange(fromPtr, toPtr, LoChar, HiChar, out Purged, true); }

    // PRIVATE method used by the above two:
    private static string _PurgeExceptRange(this string inStr, int fromPtr, int toPtr, char LoChar, char HiChar, out string Purged, 
                                                    bool usePurged) // this is a PRIVATE version, to which both PUBLIC overlays vector.
    { Purged = "";
      if (string.IsNullOrEmpty(inStr)) return String.Empty;
      if (fromPtr < 0) fromPtr = 0;  
      if (toPtr < 0 || toPtr >= inStr.Length) toPtr = inStr.Length-1;
      if (fromPtr > toPtr) return String.Empty;
      // Now the pointers will always correctly define a finite piece of the string:
      char[] chars = inStr.ToCharArray(fromPtr, toPtr-fromPtr+1);
      StringBuilder retained = new StringBuilder();
      foreach (char ch in chars) 
      { if (ch >= LoChar && ch <= HiChar) retained.Append(ch); 
        else if (usePurged)
        { bool found = false;
          for (int i=0; i < Purged.Length; i++)
          { if (Purged[i] == ch) { found = true; break; } }
          if (!found) Purged += ch;
        }  
      }
      return retained.ToString(); // OK if sb is empty.
    }



    /// <summary>
    /// <para>Str._NoRuns( [ charArray ] ) -- returns a copy of Str with all runs of given character(s) replaced with a single character.
    /// If no argument, runs of any character will be replaced by a single instance of that character.</para>
    /// </summary>
    public static string _NoRuns(this string inStr, params char[] Unwanted)
    { if (string.IsNullOrEmpty(inStr)) return String.Empty;
      int inlen = inStr.Length;  if (inlen < 2) return inStr;
      int unlen = Unwanted.Length;
      bool anychar = (unlen == 0);
      char[] inChars = inStr.ToCharArray();
      StringBuilder sb = new StringBuilder(inlen);
      char ch, prev = inChars[0];  sb.Append(prev);
      bool dupfound = false;
      for (int i=1; i < inlen; i++)
      { ch = inChars[i];
        if (ch == prev)
        { if (!anychar)
          { dupfound = false;
            for (int j=0; j < unlen; j++)
            { if (ch == Unwanted[j]) { dupfound = true; break; } }
            if (!dupfound) sb.Append(ch);
          }
        }
        else sb.Append(ch);
        prev = ch;      
      }
      return sb.ToString();
    }
 
 // ++++ STRING PARSING ++++        //parse

   //########*** REWRITE SOME / ALL OF THESE USING C#'s .TryParse(...), which allows you to specify all sorts of conditions,
   //########***  and so obviates the need for some of the conditions below. 
      
    /// <summary>
    /// Str._ParseByte(out success) -- returns the byte version of the parsed string, if success, or 0 (with 'out' arg. 'false'), if not.
    /// Leading and white space chars. are ignored, but not internal ones. 
    /// For success, the string's numerical representation must lie between 0 and 255 inclusive.
    /// </summary>
    public static byte _ParseByte( this string inStr, out bool success) // No overload; this is a rarely needed method, not worthy of overloading.
    { success = false;  byte result = 0;
      if (string.IsNullOrEmpty(inStr)) return result;
      try { result = byte.Parse(inStr);  success = true; }
      catch { success = false; }
      return result;
    }
    /// <summary>
    /// Str._ParseInt(out success) -- returns the integer version of the parsed string, if success, or 0 (with 'out' arg. 'false'), if not.
    /// Leading and white space chars. are ignored, but not internal ones.
    /// </summary>
    public static int _ParseInt( this string inStr, out bool success)
    { success = false;  int result = 0;
      if (string.IsNullOrEmpty(inStr)) return result;
      try { result = int.Parse(inStr);  success = true; }
      catch { success = false; }
      return result;
    }
    /// <summary>
    /// Str._ParseInt(int errorValue) -- returns the integer version of the parsed string, if success; or errorValue, if not.
    /// Leading and white space chars. are ignored, but not internal ones.
    /// </summary>
    public static int _ParseInt( this string inStr, int errorValue)
    { int result = errorValue;
      if (string.IsNullOrEmpty(inStr)) return result;
      try { result = int.Parse(inStr); }
      catch { result = errorValue; }
      return result;
    }
  /// <summary>
  /// <para>Given a string of integers and their ranges like "8-10, 3, 2-4", returns an int[] containing all represented values.
  /// In the given case, returns { 8, 9, 10, 3, 2, 3, 4 }; or if 'SortOutput' is TRUE, { 2, 3, 3, 4, 8, 9, 10 }; or if in addition
  ///  AndRemoveDuplicates is true. { 2, 3, 4, 8, 9, 10 }. ('AndRemoveDuplicates' is ignored if 'SortOutput' is false.)</para>
  /// <para>Ranges in descending order corrected ( e.g. "5-2", is treated as "2-5").</para>
  /// <para>Spaces are removed before any parsing.</para>
  /// <para>Errors return null, and set outcome.B to false and outcome.S to an error message. (No errors: outcome = (true, "") ).</para>
  /// <para>The optional parameters: [0] = delimiter (default being the comma ","), [1] = range marker (default being minus sign "-"). Do not include
  ///  spaces in these, as they will not be detected (see above). If pasting from a word processor, make sure that e.g. long dashes are
  ///  converted to simple dashes (or other char.), or else set the delimiter to the long dash.</para>
  /// <para>Negative numbers are allowed in ranges only if you change the range marker from the default (which is a minus sign).</para>
  /// </summary>
  public static int[] _ParseIntRanges(this string InStr, bool SortOutput, bool AndRemoveDuplicates, out Boost outcome, params string[] DelimrRanger)
  { if (InStr._Length() < 1) { outcome = new Boost(false, "input string is null or empty");  return null; }
    string delimr, ranger;
    if (DelimrRanger.Length > 0) delimr = DelimrRanger[0];  else delimr = ",";
    if (DelimrRanger.Length > 1) ranger = DelimrRanger[1];  else ranger = "-";
    string[] stroo = (InStr._Purge()).Split(new string[] {delimr}, StringSplitOptions.RemoveEmptyEntries);
    if (stroo.Length == 0) { outcome = new Boost(false, "input string contained no printable characters other than delimiters");  return null; };
    List<int> result = new List<int>();
    int ptr, m, n, q;  bool success;
    for (int i=0; i < stroo.Length; i++)
    { ptr = stroo[i].IndexOf(ranger);
      if (ptr == -1) // not a range - nice 'n easy:
      { m = stroo[i]._ParseInt(out success);
        if (!success) { outcome = new Boost(false, "unable to parse '" + stroo[i] + "' as an integer");  return null; }
        result.Add(m);
      }
      else // a range:
      { m = (stroo[i]._Extent(0, ptr))._ParseInt(out success);
        if (!success) { outcome = new Boost(false, "unable to parse '" + stroo[i]._Extent(0, ptr) + "' as an integer");  return null; }
        n = (stroo[i]._Extent(ptr+1))._ParseInt(out success);
        if (!success) { outcome = new Boost(false, "unable to parse '" + stroo[i]._Extent(ptr+1) + "' as an integer");  return null; }
        if (m > n) { q = m;  m = n;  n = q; }
        for (int j = m; j <= n; j++) result.Add(j);
      }
    }
    // Sort if required:
    if (SortOutput)
    { result.Sort();
      if (AndRemoveDuplicates)
      { ptr = 0; // points to an element in the list
        while (ptr < result.Count-1)
        { m = result[ptr];
          while (result.Count > ptr+1 && result[ptr + 1] == m) result.RemoveAt(ptr+1);
          ptr++;
        }
      }
    }
    outcome = new Boost(true);
    return result.ToArray();
  }
    /// <summary>
    /// Str._ParseLong(out success) -- returns the Int64 version of the parsed string, if success, or 0 (with 'out' arg. 'false'), if not.
    /// Leading and white space chars. are ignored, but not internal ones.
    /// </summary>
    public static long _ParseLong( this string inStr, out bool success) // No overload; this is a rarely needed method, not worthy of overloading.
    { success = false;  long result = 0;
      if (string.IsNullOrEmpty(inStr)) return result;
      try { result = Int64.Parse(inStr);  success = true; }
      catch { success = false; }
      return result;
    }
    /// <summary>
    /// Str._ParseDouble(out success) -- returns the double version of the parsed string, if success, or 0.0 (with 'out' arg. 'false'), if not.
    /// Leading and trailing white space chars. are ignored, but not internal ones. The decimal point must be '.'. Examples of allowed forms:
    /// "1", "1.0", ".12", "0.12", "-1E2", "-1E+2", "1.2e-3".
    /// </summary>
    public static double _ParseDouble( this string inStr, out bool success)
    { success = false;  double result = 0.0;
      if (string.IsNullOrEmpty(inStr) || inStr.IndexOf(',') >= 0) return result; // C#'s .Parse ignores commas: "12,3" --> "123.0".
      try { result = double.Parse(inStr);  success = true; }
      catch
      { // Still a chance:
        inStr = inStr.Trim();
        for (int i=0; i < SpecialDoubles.Length; i++)
        { if (SpecialDoubles[i].S == inStr) { result = SpecialDoubles[i].X;  success = true;  break; } } // e.g. NaN, Infinity...
      }
      return result;
    }
    /// <summary>
    /// Str._ParseDouble(errorValue) -- returns the double version of the parsed string, if success, or errorValue, if not.
    /// Leading and white space chars. are ignored, but not internal ones. The decimal point must be '.'. Exa9mples of allowed forms:
    /// "1", "1.0", ".12", "0.12", "-1E2", "-1E+2", "1.2e-3".
    /// </summary>
    public static double _ParseDouble( this string inStr, double errorValue)
    { double result = errorValue;
      if (string.IsNullOrEmpty(inStr) || inStr.IndexOf(',') >= 0) return result; // C#'s .Parse ignores commas: "12,3" --> "123.0".
      try { result = double.Parse(inStr); }
      catch { result = errorValue; }
      return result;
    }
    /// <summary>
    /// Str._ParseHex(out success) -- returns the Int32 version of the hexadecimal string, if success, or 0 (with 'out' arg.
    /// set to 'false'), if not. An initial "0x" is tolerated: "ff", "fF" and "0xFF" all return 255. Spaces and dashes are
    /// removed before conversion, so delimiters containing these (as in "10 AE" or "10 - AE") are tolerated.
    /// </summary>
    public static int _ParseHex( this string inStr, out bool success)
    { success = false;  int result = 0;
      if (string.IsNullOrEmpty(inStr)) return result;
      string inStr1 = inStr._Purge(' ', '-');
      try { result = Convert.ToInt32(inStr1.Trim(), 16);  success = true; } // '16' being the number base
      catch { success = false; }
      return result;    
    }
    /// <summary>
    /// Str._ParseHex(errorValue) -- returns the Int32 version of the hexadecimal string, if success, or errorValue, if not.
    /// An initial "0x" is tolerated: "ff", "fF" and "0xFF" all return 255. Spaces and dashes are
    /// removed before conversion, so delimiters containing these (as in "10 AE" or "10 - AE") are tolerated.
    /// </summary>
    public static int _ParseHex( this string inStr, int errorValue)
    { int result = errorValue;
      if (string.IsNullOrEmpty(inStr)) return result;
      string inStr1 = inStr._Purge(' ', '-');
      try { result = Convert.ToInt32(inStr1.Trim(), 16); } // '16' being the number base
      catch { result = errorValue; }
      return result;    
    }
    /// <summary>
    /// Str._ParseHexLong(out success) -- returns the Int64 version of the hexadecimal string, if success, or 0 (with 'out' arg.
    /// set to 'false'), if not. An initial "0x" is tolerated: "ff", "fF" and "0xFF" all return 255. Spaces and dashes are
    /// removed before conversion, so delimiters containing these (as in "10 AE" or "10 - AE") are tolerated.
    /// </summary>
    public static long _ParseHexLong( this string inStr, out bool success)
    { success = false;  long result = 0;
      if (string.IsNullOrEmpty(inStr)) return result;
      string inStr1 = inStr._Purge(' ', '-');
      try { result = Convert.ToInt64(inStr1.Trim(), 16);  success = true; } // '16' being the number base
      catch { success = false; }
      return result;    
    }

    /// <summary>
    /// <para>Divides a full filename into path and file name components. Heritage MS Windows names are adjusted in that
    /// any "\" or "\\" is replaced by '/' in the output. 'PFName' is trimmed of white spaces and char. 0 before parsing.</para>
    /// <para>'CurrentPath' should only ever either be empty or be the full path name of the default directory (with or without
    /// a terminal '/'), with allowance for a standard abbreviation.</para>
    /// <para>Recognized standard abbreviations: "./" refers the path to CurrentDir; "../" refers the path to the
    /// next highest directory in CurrentDir (but leaves as is, if none such in CurrentDir); "~/" refers to the user's
    /// personalized home directory, e.g. "/home/fred/".</para>
    /// <para>RETURN EXAMPLES: (A) FPName = "~/foo.txt" or "/home/fred/foo.txt" --> [0] = "/home/fred/", [1] = "foo.txt" (and
    /// CurrentDir is NOT used, as FPName is taken as a complete filename back to the root directory);
    /// (B) FPName = "foo.txt" --> [0] = CurrentDir, [1] = FPName; (C) FPName = "/home/fred/" --> [0] = FPName, [1] empty.
    /// (D) FPName = "foo/bloo.txt": --> [0] = CurrentDir + "foo/"; [1] = "bloo.txt".</para>
    /// <para>RETURN EXAMPLES USING ABBREVIATIONS: (A) FPName = "./foo.txt":  --> [0] = CurrentDir (with no check on
    /// its contents), [1] = "foo.txt"; (B) CurrentDir = "/home/fred/", FPName = "../foo.txt": --> [0] = "/home/",
    /// [1] = "foo.txt"; (C) CurrentDir = "/home/", FPName = "../foo.txt": --> [0] = "/", [1] = "foo.txt".</para>
    /// <para>No errors are raised, as there is no check for unallowed chars. or silly arguments: GIGO applies.</para>
    /// </summary>
    public static string[] _ParseFileName(this string PFName, string CurrentDir)
    { string[] result = new string[] {"", ""};
      string currdir = CurrentDir.Trim();
      string pfName = PFName.Trim()._Purge('\u0000');
      // Replace any Microsoft hangovers using backslashes:
      currdir = currdir.Replace(@"\\", "/");    currdir = currdir.Replace(@"\", "/");
      pfName = pfName.Replace(@"\\", "/");    pfName = pfName.Replace(@"\", "/");
      if (currdir != "") currdir = currdir._ForceLast('/');
      // Deal with abbreviations in path:
      int n=0;
      if (pfName._Extent(0, 2) == "~/")
      { pfName = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + pfName._Extent(1); }
      else if (pfName._Extent(0, 3) == "../")
      { int[] en = currdir._IndexesOf('/');
        n = en._Length();
        if (n >= 2)
        { currdir = currdir._Extent(0, en[n-2]+1); // chop off the last directory of CurrentDir
          pfName = pfName._Extent(3); // chop off the "../" from the front of pfName
        }
      }
      else if (pfName._Extent(0, 2) == "./")
      { pfName = pfName._Extent(2);
      }
      // Split into path and name:
      n = pfName.LastIndexOf('/');
      if (n == -1) { result[0] = currdir;  result[1] = pfName;  return result; } // PFName was all filename.
      string path = pfName._Extent(0, n+1); // Path name with a terminal '/'
      if (pfName[0] != '/') path = currdir + path; // If pfName starts with '/', regard it as a complete address.
      string name = pfName._Extent(n+1);
      // Assign the return:
      result[0] = path;   result[1] = name;
      return result;
    }

    /// <summary>
    /// <para>The input string is stripped of all spaces and is converted to upper case before being processed.
    /// Argument 'force_0x_for_Hex': if FALSE, 6 or 8 hex digits will be interpreted as a colour (see below); BUT a string of
    ///  6 or 8 numerals ('123456') would then be interpreted as Hex - (2), (3) below - rather than as an integer no. - (1) below.</para>
    /// <para>The desired Gdk.Color object is returned if any of the following conditions are fulfilled:</para>
    /// <para>(1) Six hexadecimal digits: format "0xRRGGBB". (If force_0x_for_Hex false, "RRGGBB" is also valid).</para>
    /// <para>(2) Eight hexadecimal digits: format "0xAARRGGBB" (if force_0x_for_Hex false, "AARRGGBB" is also valid). "AA" is always ignored.</para>
    /// <para>(3) The string parses as an Int32 (neg. nos. allowed). (If force_0x_for_Hex, positive 6 or 8 digit numbers
    ///   would be regarded as hex instead, as handling of (1) and (2) precedes handling of (3).)</para>
    /// <para>(4) There are four integers in inStr, preceded / followed / separated by any nondigit characters. (Integers will be interpreted
    /// respectively as Alpha, Red, Green and Blue values; 'Alpha' ignored). All four must lie between 0 and 255 inclusive.</para>
    /// <para>(5) There are three integers in inStr, preceded / followed / separated by ANY no. of ANY nondigit characters. (Integers will be 
    /// interpreted respectively as Red, Green and Blue values; all three must lie between 0 and 255 inclusive.</para>
    /// <para>(6) There are NO digits in inStr, and no non-letter chars. (except possibly '.'). All spaces (and lower chars.) are removed, 
    /// and if there are fullstops, only the text after the final fullstop is used (e.g. for "System.Drawing.Color.Blue", only "Blue"
    /// is referenced). The text must match a .NET system colour name, though this matching is NOT case-sensitive. ("grey"
    /// is allowed in place of "gray".)</para>
    /// <para>(7) There are NO digits in inStr, and '[', ']' are found. All is ignored except the text within the square brackets.
    /// This text is then processed exactly as for (6) above.</para>
    /// <para>ERROR: failureColour is returned. (You can use 'Color.Empty' if the returned colour is irrelevant in case of error.)</para>
    /// </summary>
    public static Gdk.Color _ParseColour( this string inStr, Gdk.Color failureColour, bool force_0x_for_Hex, out bool success)
    {
      string ss, instr = inStr._Purge().ToUpper(); // remove all spaces, internal and external.
      int[] numbers = new int[3];
      success = false;
     // HEX STRINGS:
      string testStr = instr, testStrPrefix = testStr._Extent(0,2);
      if (!force_0x_for_Hex || testStrPrefix == "0X")
      { if (testStrPrefix == "0X") testStr = testStr._Extent(2);
        int testLen = testStr.Length;
        if ((testLen == 6 || testLen == 8) && testStr._IndexOfNoneOf(JS.HexDigits) == -1)
        { if (testLen == 8) testStr = testStr.Substring(2); // Now guaranteed to have a string of six hex. digits.
          for (int i = 0; i < 3; i++)
          { ss = testStr._Extent(2*i, 2);  numbers[i] = ss._ParseHex(0); }
          success = true;
          return new Color((byte)numbers[0], (byte)numbers[1], (byte)numbers[2]);
        }
      }
     // SINGLE INTEGER:
      int p = inStr._ParseInt(out success );  if (success) return JS.NewColor(p);
      // See if this contains numerals, in amongst other stuff:
      int[] found = JS.FindInteger(inStr, true); // [0] = 1 for success; [1] = the value; [2], [3] = its first and last ptrs.
     // COLOUR NAME:
      if (found[0] == 0) // no integers
      { ss = inStr._Purge();
        p = ss._IndexOf('['); // If '[..]' found, test its contents; otherwise test the whole string
        if (p >= 0)
        { int q = ss._IndexOf(']', p);
          if (q > p) ss = ss._FromTo(p+1, q-1); else return failureColour; // unmatched brackets
        }
        p = ss.LastIndexOf('.');  if (p >= 0) ss = ss._Extent(p+1); // Remove any namespace and class prefix.
        if (ss == "Empty" || ss == "Zero") { success = true;  return Gdk.Color.Zero; }
        return JS.NewColor(ss, out success); // Colour from STRING. (Swaps for 'grey'.) Colour zero, if success is FALSE.
      }
     // EITHER THREE OR FOUR INTEGERS, separated by any other chars.
      else
      { success = false;
        if (found[1] >= 0 && found[1] <= 255) numbers[0] = found[1]; else return failureColour; // numbers[0] is either A (if 4 ints.) or R (if 3 ints.).
        found = JS.FindInteger(inStr, true, found[3]+1); // search on, starting from after the first integer, found above.('true' = '-' ignored.)
        if (found[0] == 1 && found[1] <= 255) numbers[1] = found[1]; else return failureColour;
        found = JS.FindInteger(inStr, true, found[3]+1);
        if (found[0] == 1 && found[1] <= 255) numbers[2] = found[1]; else return failureColour;
        success = true; // as we have at leat 3 integers.
        found = JS.FindInteger(inStr, true, found[3]+1);
        if (found[0] == 1) // then we have 4 integers:
        { if (found[1] > 255) return failureColour;
          success = true;
          return new Gdk.Color( (byte) numbers[1], (byte) numbers[2], (byte) found[1]); // 'out success' is already true.
        }
        else // just 3:
        { success = true;
          return new Color( (byte)numbers[0], (byte)numbers[1], (byte)numbers[2]); // 'out success' is already true.
        }
      }
    }

    /// <summary>
    /// Overload which uses Gdk.Color.Zero as the return if the string cannot be parsed to a colour. Hex nos. must have '0x' prefix (case-insens.).
    /// </summary>
    public static Gdk.Color _ParseColour( this string inStr, out bool success)
    { return inStr._ParseColour(Gdk.Color.Zero, true, out success); }

    /// <summary>
    /// Overload which uses Gdk.Color.Zero as the return if the string cannot be parsed to a colour, and gives no
    /// other indication of error. Only use if you are absolutely sure that there can be no mistake, or if mistake
    /// returning colour zero is inconsequential. Hex nos. must have '0x' prefix (case-insens.).
    /// </summary>
    public static Gdk.Color _ParseColour( this string inStr)
    { bool success;  return inStr._ParseColour(Gdk.Color.Zero, true, out success); }


    /// <summary>
    /// <para>Will develop a System.Drawing.Color object from a string, if any of the following conditions are fulfilled:</para>
    /// <para>(1) The string contains only decimal numerals (leading '-' allowed; spaces not tolerated), and is within the Int32 range.</para>
    /// <para>(2) The string begins with '0x' and is followed by exactly eight hexadecimal chars. This will be interpreted as the four-byte
    /// hex number [intensity][R][G][B].</para>
    /// <para>(3) The string begins with '0x' and is followed by exactly six hexadecimal chars. This will be interpreted as the three-byte
    /// hex number [R][G][B]. (Intensity, called 'Alpha' by .NET, will be set by .NET to 255.)</para>
    /// <para>(4) There are four integers in inStr, preceded / followed / separated by any nondigit characters. (Integers will be interpreted
    /// respectively as Alpha, Red, Green and Blue values). All four must lie between 0 and 255 inclusive.</para>
    /// <para>(5) There are three integers in inStr, preceded / followed / separated by ANY no. of ANY nondigit characters. (Integers will be
    /// interpreted respectively as Red, Green and Blue values; Alpha will be set by .NET to 255). All three must lie between 0 and 255
    /// inclusive.</para>
    /// <para>(6) There are NO digits in inStr, and no non-letter chars. (except possibly '.'). All spaces (and lower chars.) are removed,
    /// and if there are fullstops, only the text after the final fullstop is used (e.g. for "System.Drawing.Color.Blue", only "Blue"
    /// is referenced). The text must match a .NET system colour name, though this matching is NOT case-sensitive.</para>
    /// <para>(7) There are NO digits in inStr, and '[', ']' are found. All is ignored except the text within the square brackets.
    /// This text is then processed exactly as for (6) above.</para>
    /// <para>ERROR: failureColour is returned. (You can use 'Color.Empty' if the returned colour is irrelevant in case of error.)</para>
    /// <para>PARSING THE OUTPUT OF A COLOUR'S 'TOSTRING()' METHOD: Suppose your input 'inStr' here is the output from 'MyColour.ToString()',
    /// where 'MyColour' is of type Color. Suppose 'MyColour' is blue. If you had earlier SET MyColour using 'Color.FromName("Blue")' or
    /// 'Color.FromKnownColor(.)', then 'MyColour.ToString()" will return "Color [Blue]", and method (7) above will successfully parse it.
    /// But suppose you had SET the colour directly: "MyColour = Color.FromArgb(..)"; then you will get back "Color [A=255, R=0, G=0, B=255]".
    /// In this case, method (4) above will successfully parse it.</para>
    /// </summary>
    public static System.Drawing.Color _ParseColourMS( this string inStr, System.Drawing.Color failureColour, out bool success)
    { success = false;
      int[] numbers = new int[3];
      // Deal with the hex strings first:
      if (inStr._Extent(0, 2) == "0x")
      { int len = inStr.Length;  if (len != 8 && len != 10) return failureColour;
        int ptr = len - 6; // points to the 'R' byte.
        string ss;
        for (int i = 0; i < 3; i++)
        { ss = inStr._Extent(ptr + 2*i, 2);
          numbers[i] = ss._ParseHex(out success);  if (!success) return failureColour;
        }
        if (len == 8) return System.Drawing.Color.FromArgb(numbers[0], numbers[1], numbers[2]); // 'out success' is already true, from the above loop.
        else
        { ss = inStr._Extent(2, 2); // intensity byte
          int intensity = ss._ParseHex(out success);
          if (!success || intensity > 255) return failureColour;
          return System.Drawing.Color.FromArgb(intensity, numbers[0], numbers[1], numbers[2]); // 'out success' is already true, from the above step.
        }
      }
    // All hex cases have been returned, so deal with integer / name cases:
      // Whole string encodes a single integer:
      int p = inStr._ParseInt(out success );  if (success) return System.Drawing.Color.FromArgb(p);
      // See if this contains numerals, in amongst other stuff:
      int[] found = JS.FindInteger(inStr, true);
      if (found[0] == 1 && found[1] >= 0 && found[1] <= 255) numbers[0] = found[1]; // numbers[0] is either A (if 4 ints.) or R (if 3 ints.).
      else if (found[0] == 0) // No digits, so see if this is a named colour.
      { string ss = inStr._Purge();
        p = ss._IndexOf('['); // If '[..]' found, test its contents; otherwise test the whole string
        if (p >= 0)
        { int q = ss._IndexOf(']', p);
          if (q > p) ss = ss._FromTo(p+1, q-1); else return failureColour; // unmatched brackets
        }
        p = ss.LastIndexOf('.');  if (p >= 0) ss = ss._Extent(p+1); // Remove any namespace and class prefix.
        System.Drawing.Color result = System.Drawing.Color.FromName(ss); // This .NET function uses a non-case-sensitive name match.
        if (result.ToArgb() == 0) // Can't check "result == Color.Empty", as Color objects have a Name field as well as ARGB values.
         // one more chance, as .FromName(.) does not recognize Color.Empty:
        { if (ss == "Empty") { result = System.Drawing.Color.Empty; }
          else return failureColour;
        }
        success = true;  return result;
      }
      found = JS.FindInteger(inStr, true, found[3]+1); // search on, starting from after the first integer, found above.('true' = '-' ignored.)
      if (found[0] == 1 && found[1] <= 255) numbers[1] = found[1]; else return failureColour;
      found = JS.FindInteger(inStr, true, found[3]+1);
      if (found[0] == 1 && found[1] <= 255) numbers[2] = found[1]; else return failureColour;
      success = true; // as we have at leat 3 integers.
      found = JS.FindInteger(inStr, true, found[3]+1);
      if (found[0] == 1) // then we have 4 integers:
      { if (found[1] > 255) return failureColour;
        return System.Drawing.Color.FromArgb(numbers[0], numbers[1], numbers[2], found[1]); // 'out success' is already true.
      }
      else // just 3:
      { return System.Drawing.Color.FromArgb(numbers[0], numbers[1], numbers[2]); } // 'out success' is already true.
    }

    /// <summary>
    /// <para>Attempts to parse a string into a bool array. Substrings separated by the delimiter undergo a single test: if (after removing
    /// spaces) the first character is 'T' or 't', it is taken as value 'true'; if 'F' or 'f', as false. If neither applies,
    /// null is returned (error state). (Spaces are removed after string splitting, so it is allowable to use a delimiter of space(s) only.)</para>
    /// <para>Any initial and final 'delimiter' are ignored, as are duplicated delimiters with nothing (or just spaces) between them.</para>
    /// <para>RETURNED -- NO ERROR: a bool[] array of length at least 1 (1 being the length if no delimiters are present).</para>
    /// <para>RETURNED -- IF ERROR: If parsing fails, or if input string is empty or null or only delimiters, a NULL array is returned.</para>
    /// <para>Note that whether an error has occurred or not, the returned array NEVER has length 0.</para>
    /// </summary>
    public static bool[] _ToBoolArray(this string inStr, string delimiter)
    { if (string.IsNullOrEmpty(inStr) || string.IsNullOrEmpty(delimiter) ) return null;
      string[] stroo = inStr.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries );
      if (stroo._NorE()) return null;
      List<bool> result = new List<bool>();
      string ss;
      for (int i=0; i < stroo.Length; i++)
      { ss = stroo[i]._Purge();  if (ss == "") continue;
        char c = char.ToUpper(ss[0]);
        if (c == 'T') result.Add(true);  
        else if (c == 'F') result.Add(false);
        else return null; 
      }
      return result.ToArray();                  
    }
    /// <summary>
    /// <para>Attempts to parse a string into a byte array. Apart from the delimiter, the string must contain no characters except digits 0 to 9
    /// (leading / trailing white spaces tolerated). Also, each entry must lie between 0 and 255.<para>
    /// <para>(Any initial and final 'delimiter' are ignored, as are duplicated delimiters with nothing between them.)</para>
    /// <para>RETURNED -- NO ERROR: a byte[] array of length at least 1 (1 being the length if no delimiters are present).</para>
    /// <para>RETURNED -- IF ERROR: If parsing fails, or if input string is empty or null or only delimiters, a NULL array is returned.</para>
    /// <para>Note that whether an error has occurred or not, the returned array NEVER has length 0.</para>

    /// </summary>
    public static byte[] _ToByteArray(this string inStr, string delimiter)
    { if (string.IsNullOrEmpty(inStr) || string.IsNullOrEmpty(delimiter) ) return null;
      string[] stroo = inStr.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
      if (stroo == null) return null;
      byte[] result = new byte[stroo.Length];
      for (int i=0; i < stroo.Length; i++)
      { try { result[i] = Byte.Parse(stroo[i]); } // .NET's parser crashes if value < 0 or > 255, but tolerates '+', and white spaces fore/aft.
        catch { result = null; break; }
      }
      if (result._NorE()) return null; // A non-null but empty array should never occur, but you never know...
      return result;                  
    }
    /// <summary>
    /// <para>Attempts to parse a string into an integer array. Apart from the delimiter, the string must contain no characters except valid 
    /// Int32 numbers (leading '+' and '-' allowed, and leading / trailing white spaces tolerated.)
    /// (Any initial and final 'delimiter' are ignored, as are duplicated delimiters with nothing between them.)</para>
    /// <para>RETURNED -- NO ERROR: an int[] array of length at least 1 (1 being the length if no delimiters are present).</para>
    /// <para>RETURNED -- IF ERROR: If parsing fails, or if input string is empty or null or only delimiters, a NULL array is returned.</para>
    /// <para>Note that whether an error has occurred or not, the returned array NEVER has length 0.</para>
    /// </summary>
    public static int[] _ToIntArray(this string inStr, string delimiter)
    { if (string.IsNullOrEmpty(inStr) || string.IsNullOrEmpty(delimiter) ) return null;
      string[] stroo = inStr.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
      if (stroo == null) return null;
      int[] result = new int[stroo.Length];
      for (int i=0; i < stroo.Length; i++)
      { try { result[i] = Int32.Parse(stroo[i]); } // .NET's .Parse() allows for sign + leading / trailing white spaces.
        catch { result = null; break; }
      }
      if (result._NorE()) return null; // A non-null but empty array should never occur, but you never know...
      return result;                  
    }
    /// <summary>
    /// <para>Attempts to parse a string into a type double array. Apart from the delimiter, the string must contain no characters except 
    /// a valid 'double' number representation (leading '+' and '-' allowed, and leading / trailing white spaces tolerated.)
    /// (Any initial and final 'delimiter' are ignored, as are duplicated delimiters with nothing between them.) </para>
    /// <para>RETURNED -- NO ERROR: a double[] array of length at least 1 (1 being the length if no delimiters are present).</para>
    /// <para>RETURNED -- IF ERROR: If parsing fails, or if input string is empty or null or only delimiters, a NULL array is returned.</para>
    /// <para>Note that whether an error has occurred or not, the returned array NEVER has length 0.</para>
    /// </summary>
    public static double[] _ToDoubleArray(this string inStr, string delimiter)
    { if (string.IsNullOrEmpty(inStr) || string.IsNullOrEmpty(delimiter) ) return null;
      string[] stroo = inStr.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
      if (stroo == null) return null;
      double[] result = new double[stroo.Length];
      bool ok;
      for (int i=0; i < stroo.Length; i++)
      { result[i] = stroo[i]._ParseDouble(out ok);
        if (!ok) return null;
      }
      return result;
    }
    /// <summary>
    /// Str._ToDoubleMx(valuesDelimiter, rowsDelimiter) -- attempts to parse a string into a type double[,] array; the string must contain nothing
    /// except valid type double numbers and the two string arguments. Spaces are ignored and, being removed before any
    /// processing, must not be used as delimiters of either type. (Spaces within delimiters - ", " - are also ignored.) 
    /// If parsing fails, a NULL array is returned. Example of allowed 'Str' for 2x3 matrix, using ',' as valuesDelimiter
    /// and ';' as rowsDelimiter: "1, 2, 3; 4, 5, 6". (A ';' after the final 6 is optional, but there must be no ',' at the end of a row.) 
    /// This method does not work for 'ragged' matrices; all rows must have the same length.
    /// </summary>
    public static double[,] _ToDoubleMx(this string inStr, string valuesDelimiter, string rowsDelimiter)
    { if (string.IsNullOrEmpty(inStr) ) return null;
      string rowsDelim = rowsDelimiter._Purge(' '), colsDelim = valuesDelimiter._Purge(' '); // OK if strings are null
      if (string.IsNullOrEmpty(rowsDelim) || string.IsNullOrEmpty(colsDelim) ) return null;
      // Develop one string for each row:
      string[] rows = inStr.Split(new string[] { rowsDelim }, StringSplitOptions.RemoveEmptyEntries);
      if (rows == null) return null;
      int norows = rows.Length; if (norows == 0) return null;
      // Develop one string for each value in each row:
      int nocolumns = -1;   string rowstr = "";
      double[,] result = null;  bool success = false;
      for (int rw=0; rw < norows; rw++)
      { rowstr = rows[rw]._Purge(' ');
        if (String.IsNullOrEmpty(rowstr)) return null;
        if (rw == 0) 
        { nocolumns = 1 + rowstr._CountStr(colsDelim);
          result = new double[norows, nocolumns];
        }
        string[] values = rowstr.Split(new string[] { colsDelim },  StringSplitOptions.RemoveEmptyEntries);
        if (values == null || values.Length != nocolumns) return null;
        // We have the right number of value strings, so we try to fit them to the matrix to be returned:
        for (int cl=0; cl < nocolumns; cl++)
        { result[rw, cl] = values[cl]._ParseDouble(out success);
          if (!success) return null;
        }
      }
      return result;                  
    }


 // ++++ INTEGER EXTENSIONS ++++       //int
    
    /// <summary>
    /// N._Ordinal() -- returns the ordinal string version of integer N; e.g. N = 0 returns "0th.", N = -21 returns "-21st.".
    /// Optional bool arguments (default to FALSE if absent):  Bools[0] TRUE --> number not returned; e.g. for N = 21, "st." is returned.
    /// Bools[1] TRUE --> no fullstop at the end (e.g. N = 21 --> "21st" or "st").
    /// </summary>
    public static string _Ordinal(this int Value, params bool[] Bools)
    { bool noNumber = (Bools.Length > 0 && Bools[0]);
      bool noFullstop = (Bools.Length > 1 && Bools[1]);
      string ss = "";
      int absValue = Math.Abs(Value);
      switch (absValue % 100)
      { case 11: case 12: case 13: ss = "th"; break;
        default: break;
      }
      if (ss == "")
      { switch (absValue % 10)
        { case 1  : ss = "st"; break;
          case 2  : ss = "nd"; break;
          case 3  : ss = "rd"; break;
          default : ss = "th"; break;
      } }
      if (!noFullstop) ss += '.';
      if (noNumber) return ss; else return Value.ToString() + ss;
    }
    /// <summary>
    /// Intgr._ToHex(minLength) -- returns the hexadecimal version (capital 'A' to 'F') of the integer as a string of minimum length 'minLength'
    /// (which should be between 1 and 99; out-of-range values are corrected). 
    /// </summary>
    public static string _ToHex(this int value, int minLength)
    { if (minLength < 1) minLength = 1; else if (minLength > 99) minLength = 99;
      return value.ToString("X" + minLength.ToString());
    }
    /// <summary>
    /// Intgr._ToHexSmall(minLength) -- returns the hexadecimal version (small 'a' to 'f') of the integer as a string of minimum length 'minLength'
    /// (which should be between 1 and 99; out-of-range values are corrected). 
    /// </summary>
    public static string _ToHexSmall(this int value, int minLength)
    { if (minLength < 1) minLength = 1; else if (minLength > 99) minLength = 99;
      return value.ToString("x" + minLength.ToString());
    }
    /// <summary>
    /// Intgr._ToHex(minLength, groupSize, separator) -- returns the hexadecimal version of the integer (using capitals 'A' to 'F').
    /// The string will have minimum length 'minLength' (range 1 - 99), padded at the start with zeros if necessary.
    /// Digits will be grouped in groups of 'groupSize' separated by 'separator'.
    /// Examples: "(1023)._ToHex(4, 2, ' ')" --> "FF FF";  "(0xFFFFF)._ToHex(1, 2, '-')" --> "F-FF-FF".
    /// </summary>
    public static string _ToHex(this int value, int minLength, int groupSize, char separator)
    { if (minLength < 1) minLength = 1; else if (minLength > 99) minLength = 99; // limits imposed by .NET.
      string asHex = value.ToString("X" + minLength.ToString());
      int len = asHex.Length;
      if (groupSize < 1 || groupSize >= len) return asHex;
      int firstGroup = len % groupSize;  if (firstGroup == 0) firstGroup = groupSize;
      string result = "";
      int extent, ptr = 0;
      while (ptr < len)
      { if (ptr == 0) extent = firstGroup; else extent = groupSize;
        result += asHex._Extent(ptr, extent);   ptr += extent; 
        if ( ptr <= len-groupSize) result += separator;
      }
      return result;            
    }
    /// <summary>
    /// Intgr._ToBytes() -- returns a byte array, size 4, containing the bytes that make up Intgr; the low byte has index 0 in the byte array.
    /// </summary>
    public static byte[] _ToBytes(this int value)
    { byte[] result = new byte[4];
      unsafe // both byte and integer are non-heap structures, so immune to GC; no 'fixed' block is needed. ('result' is on heap, 
             // but was not generated in an 'unsafe' block, so GC is irrelevant.)
      { int* ip = &value;    
        byte* bo = (byte*) ip; // to set a byte[] pointer to this, you would have to use a 'fixed' block. Not worth interrupting the GC.
        result[0] = *bo;  bo++;  result[1] = *bo;  bo++;   result[2] = *bo;  bo++;   result[3] = *bo;  
      }
      return result;
    }  
// *** THE ABOVE WILL RAISE ERRORS when the compiler option '/unsafe' is not set (and it seems to unset itself from time to time). So here
// ***  is stuff copied from the 'help' file. (The link to AllowUnsafeBlocks failed, and I haven't yet gone on line for more info.)
//////To set this compiler option in the Visual Studio development environment
//////Open the project's Properties page. [Bottom of 'Projects' menu, prefixed by name of the particular project]
//////Click the Build property page.
//////Select the Allow Unsafe Code check box.
//////For information about how to set this compiler option programmatically, see AllowUnsafeBlocks.
// *** They make it bloody hard, don't they? Best of luck.


/// <summary>
/// <para>Int32._ToChar() -- always returns a valid unicode character. Input value below 0 corrected to 0, and above 65535 corrected to 65535.</para>
/// <para>ErrorCode: 0 = no error; -1 = input value negative; 1 = input value > 65535.</para>
/// </summary>
    public static char _ToChar(this int value, out int ErrorCode)
    { if (value < 0) { ErrorCode = -1;  return '\u0000'; }
      else if (value > 65535) { ErrorCode = 1;  return '\uFFFF'; }
      ErrorCode = 0;
      return Convert.ToChar(value);
    }

/// <summary>
/// <para>If the integer lies between 0 and 65535 inclusive, the unicode char. of that value is returned; if outside this range, 
/// an out-of-range substitute is returned (see below).</para>
/// <para>KeyValues: [0] = out-of-range substitute character (default \u0000); [1] = lowest valid char. (default \u0000); [2] is
/// highest valid char. (default \uffff). KeyValues errors simply result in the default being used.</para>
/// </summary>
    public static char _ToChar(this int value, params int[] KeyValues)
    { int keylen = KeyValues.Length;
      char dummychar = (char) 0; // replacement where double value cannot be translated into a valid character.
      int n, lowchar = 0, hichar = 65535; // allowed range of values for valid chars.
      // Deal with any supplied KeyValues elements:
      if (keylen > 0)
      { n = KeyValues[0];
        if (n >= 0 && n <= 0xffff) dummychar = (char) n;
        if (keylen > 1)
        { n = KeyValues[1];
          if (n >= 0 && n <= 0xffff) lowchar = (char) n;
          if (keylen > 2)
          { n = KeyValues[2];
            if (n >= lowchar && n <= 0xffff) hichar = (char) n;
      } } }
      // Deal with 'value':
      if (value < lowchar || value > hichar) return dummychar;
      return Convert.ToChar(value);      
    }

    /// <summary>
    /// Integer._Chain(extent) -- returns an int[] consisting of repetitions of the integer up to array length 'extent'.
    /// No errors raised; extent 0 returns an empty array, negative extent returns NULL.
    /// </summary>
    public static int[] _Chain(this int Intgr, int extent)
    { if (extent < 0) return null;
      int[] result = new int[extent];
      for (int i=0; i < extent; i++) result[i] = Intgr;
      return result;
    }

 // ++++ LONG INTEGER EXTENSIONS ++++       //long
    
    /// <summary>
    /// Intgr64._ToBytes() -- returns a byte array, size 8, containing the bytes that make up Intgr64; the low byte has index 0 
    /// in the byte array.
    /// </summary>
    public static byte[] _ToBytes(this long value)
    { byte[] result = new byte[8];
      unsafe // both byte and integer are non-heap structures, so immune to GC; no 'fixed' block is needed. ('result' is on heap, 
             // but was not generated in an 'unsafe' block, so GC is irrelevant.)
      { long* ip = &value;    
        byte* bo = (byte*) ip; // to set a byte[] pointer to this, you would have to use a 'fixed' block. Not worth interrupting the GC.
        result[0] = *bo;  bo++;  result[1] = *bo;  bo++;   result[2] = *bo;  bo++;   result[3] = *bo;  bo++;
        result[4] = *bo;  bo++;  result[5] = *bo;  bo++;   result[6] = *bo;  bo++;   result[7] = *bo;  
      }
      return result;
    }  

 // ++++ DOUBLE EXTENSIONS ++++       //double
    
    /// <summary>
    /// Dub._ToBytes() -- returns a byte array, size 8, containing the bytes that make up double Dub; the low byte has index 0 
    /// in the byte array.
    /// </summary>
    public static byte[] _ToBytes(this double value)
    { byte[] result = new byte[8];
      unsafe // both byte and integer are non-heap structures, so immune to GC; no 'fixed' block is needed. ('result' is on heap, 
             // but was not generated in an 'unsafe' block, so GC is irrelevant.)
      { double* ip = &value;    
        byte* bo = (byte*) ip; // to set a byte[] pointer to this, you would have to use a 'fixed' block. Not worth interrupting the GC.
        result[0] = *bo;  bo++;  result[1] = *bo;  bo++;   result[2] = *bo;  bo++;   result[3] = *bo;  bo++;
        result[4] = *bo;  bo++;  result[5] = *bo;  bo++;   result[6] = *bo;  bo++;   result[7] = *bo;  
      }
      return result;
    }  

    /// <summary>
    /// <para>Dub._ToChar() -- returns either a valid character or, if not possible, '\u0000'.</para>
    /// <para>ErrorCode: 0 = no error; -1 = value negative (after rounding); 1 = value > FFFF (after rounding); 2 = value is double.NaN.</para>
    /// <para>Note that .NET and MONO's type 'char' encodes the 'Basic Multilingual Plane' of the Unicode system, which plane
    ///  covers the range 0 to FFFF with no exclusions. There are 16 more planes, the highest going up to 10FFFF.) Also note
    ///  that there are character ENCODINGS which convert these characters to indices of images in a font; the usual encoding for common
    ///  fonts is UTF8, which has a different numbering system (1 to 6 bytes; great majority of values in this range are invalid).</para>
    /// </summary>
    public static char _ToChar(this double value, out int ErrorCode)
    { char result = '\u0000';
      if (double.IsNaN(value))   { ErrorCode = 2;  return result; }
      else if (value <= -0.5)    { ErrorCode = -1; return result; }
      else if (value >= 65535.5) { ErrorCode = 1;  return result; }
      ErrorCode = 0;
      int n = Convert.ToInt32(value);
      result = Convert.ToChar(n);
      return result;
    }
    /// <summary>
    /// <para>Dub._ToChar() -- returns either a valid character or, if not possible, ErrChar.</para>
    /// <para>ErrorChar: This will replace any value which cannot be converted to a unicode (too small; too big; NaN).</para>
    /// <para>Note that .NET and MONO's type 'char' encodes the 'Basic Multilingual Plane' of the Unicode system, which plane
    ///  covers the range 0 to FFFF with no exclusions. There are 16 more planes, the highest going up to 10FFFF.) Also note
    ///  that there are character ENCODINGS which convert these characters to indices of images in a font; the usual encoding for common
    ///  fonts is UTF8, which has a different numbering system (1 to 6 bytes; great majority of values in this range are invalid).</para>
    /// </summary>
    public static char _ToChar(this double value, char ErrChar)
    { if (double.IsNaN(value) || value <= -0.5 || value >= 65535.5) return ErrChar;
      int n = Convert.ToInt32(value);
      return Convert.ToChar(n);
    }


 // ++++ GENERAL ARRAY EXTENSIONS (1-DIM) ++++ (extensions valid for more than one type of array)

/// <summary>
/// Returns -1 if the input array is null, 0 if it is empty; otherwise returns the nonempty length.
/// </summary>
    public static int _Length(this Array inArr)
    { if (inArr == null) return -1;
      return inArr.Length;
    }
    public static string _LengthStr(this Array inArr)
    { if (inArr == null) return "NULL";
      return inArr.Length.ToString();
    }
/// <summary>
/// Returns TRUE if the input array is either null or empty.
/// </summary>
    public static bool _NorE(this Array Arr)
    { if (Arr == null || Arr.Length == 0) return true;
      return false;    
    }

    /// <summary>
    /// Arr._ToString(separator) -- Gives sensible output only if there is a useful '.ToString()' method for the array's element type.
    /// 'separator': if nonempty, follows the string version of every value except at the end of the string.
    /// </summary>
    public static string _ToString(this Array inArr, string separator, params int[] FromExtent)
    { return inArr._ToString(separator, null, null, FromExtent);
    }
    /// <summary>
    /// Arr._ToString(separator, numbering, formatstring [, tablength [, rowWrap ] ] ) -- Gives sensible output only
    /// if there is a useful '.ToString()' method for the array's element type. Using the term 'value string' for Arr[i].ToString(),
    /// arguments are as follows: (1) 'separator': if nonempty and nonnull, follows every value string except the last one.
    /// (2) 'numbering': if nonempty and nonnull, will cause values to be numbered (from 0 upwards). Consecutive zeroes in string 'numbering' 
    /// will be replaced by the index of the value, the no. of zeroes in 'numbering' defining the minimum length of this index. (Padding 
    /// to the left with spaces occurs, for shorter indices.) If zeroes are not found in 'numbering', no numbering occurs. 
    /// The rest of string 'numbering' is exactly reproduced. Example: for 'numbering' = "[000]: " and index 5, the numbering component
    /// of the output will be "[  5]: ".
    /// (3) 'formatstring' ignored unless the type in question uses it. Examples for double[] as set by .NET: "F1" to "F15" (fixed decimals), 
    /// "G1" to "G15" (significant digits, in shortest deployment).
    /// (4) 'parameters', if partly or fully filled, has the following significance:
    /// (4a) 'parameters[0]' -- start pointer (default = 0);  (4b) 'parameters[1]' -- extent of array (default = to end of array);
    /// (4c) 'parameters[2]' -- if greater than 0 (the default), pads the end of (number string + value string + separator) with 
    /// spaces to this length. (For non-fixed fonts, better tabbing occurs by not setting 'tablength' but instead including '\t' in 'separator'.)
    /// (4d) 'parameters[3]' -- if greater than 0 (the default), adds a line-wrapping '\n' after multiples of this many value strings.
    /// ERRORS RETURN the empty string. They occur: (1) if 'Arr' is multidimensional; (2) if 'formatstring' is provided, but .NET regards it 
    /// as an error. (Unfortunately for most faulty format strings, .NET just replicates formatstring itself, in place of value strings, 
    /// without raising an error.) (3) if pointers are ridiculous. (But oversized extent is adjusted down.)
    /// </summary>
    public static string _ToString(this Array inArr, string separator, string numbering, string formatstring, params int[] parameters)
    { if (inArr == null || inArr.Rank != 1) return ""; // Must be a one-dimensional array.
      int arrlen = inArr.Length;  if (arrlen == 0) return "";
      if (separator == null) separator = String.Empty;
      bool useFmtStr = (!String.IsNullOrEmpty(formatstring));
      int startptr = 0, endptr = arrlen-1, tablength = 0,  rowWrap = arrlen+1; // the default row length, if not set in 'parameters'
      if (parameters.Length > 0) startptr = parameters[0];
      if (startptr < 0) startptr = 0; else if (startptr >= arrlen) return "";
      if (parameters.Length > 1) endptr = startptr + parameters[1] - 1;
      if (endptr >= arrlen) endptr = arrlen-1;
      if (endptr < startptr) return "";    
      if (parameters.Length > 2) tablength = parameters[2];
      if (parameters.Length > 3) rowWrap = parameters[3];  
      if (rowWrap < 1 || rowWrap >= arrlen) rowWrap = arrlen+1; // tantamount to not using row wrapping.
      // Get numerical indicator of array type. See comment below - "I can't yet see..."
      int typeno = 0; // applies where no format string is valid / required.
      if (useFmtStr)
      { // I can't yet see any way around this next caboodle. If you try "try {inArr.GetValue(i).ToString(formatstring); ...}
        //  you can't get past the compiler, which simply won't allow arguments to .ToString before a specific type cast.
        typeno = TypeNo(ref inArr);
        useFmtStr = (typeno != 0);
      }
      // OK, all parameters set, so let's write the return string:
      string ss, tt="", result = "";  bool endOfWrapLength = false;
      int extent = endptr-startptr+1;
      for (int i = 0; i < extent; i++)
      { // Prepare a number prefix: 
        ss = Fill_Out(numbering, startptr+i); // will be "", if 'numbering' empty or null
        tt = ValueStr(ref inArr, -1, startptr+i, typeno, formatstring);
        ss += tt;
        endOfWrapLength = ( (i+1) % rowWrap == 0);
        // Add tabbing characters and separator, if required:
        if (i < extent-1) ss += separator;
        if (!endOfWrapLength && tablength > 0) ss = ss._PadTo(tablength, ' ', true);        
        result += ss;
        // Add LF if more than one row, and this is the end of the row:
        if (endOfWrapLength) result += '\n';
      }
      return result;
    }
    
    // *** NOT YET WRITTEN. Go ahead and write it.
//    public static Array _Resize(this Array inArr, int setLength, object padder)
//    {
//    return null;
//    }

    
    /// <summary>
    /// Mx._ToString(valuesSeparator, rowsSeparator, valuesNumbering, rowsNumbering, formatstring [, tablength [, rowWrap ] ] ) -- 
    /// Gives sensible output only if there is a useful '.ToString()' method for the array's element type. Using the term 'value string' 
    /// for Mx[i,j].ToString(), arguments are as follows: (1) 'valuesSeparator': if nonempty and nonnull, follows every value string except 
    /// the last one in a row.
    /// (2) 'rowsSeparator': if nonempty and nonnull, comes at the end of every row (including the last one). If you want rows to be on
    /// separate lines, end this string with the line-feed character '\n'.
    /// (3) 'valuesNumbering': if nonempty and nonnull, will cause values to be numbered (from 0 upwards). Consecutive zeroes in string 'valuesNumbering' 
    /// will be replaced by the index of the value, the no. of zeroes in 'valuesNumbering' defining the minimum length of this index. (Padding 
    /// to the left with spaces occurs, for shorter indices.) If zeroes are not found in 'valuesNumbering', no numbering occurs. 
    /// The rest of string 'valuesNumbering' is exactly reproduced. Example: for 'valuesNumbering' = "[000]: " and index 5, the numbering component
    /// of the output will be "[  5]: ".
    /// (4) 'rowsNumbering' - as for values numbering, except that this will go at the start of every row. Ignored, if null or empty.
    /// (5) 'formatstring' ignored unless the type in question uses it. Examples for double[] as set by .NET: "F1" to "F15" (fixed decimals), 
    /// "G1" to "G15" (significant digits, in shortest deployment).
    /// (6) 'tablength': if present and greater than 0, pads the end of (number string + value string + separator) with spaces to this length. 
    /// (For non-fixed fonts, better tabbing occurs by not setting 'tablength' but instead including '\t' in 'separator'.)
    /// (7) 'rowWrap': if present and greater than 0, adds a line-wrapping '\n' after multiples of this many value strings.
    /// ERRORS RETURN the empty string. They occur: (1) if 'Mx' is not a matrix; (2) if 'formatstring' is provided, but .NET regards it 
    /// as an error. (Unfortunately for most faulty format strings, .NET just 
    /// replicates formatstring itself, in place of value strings, without raising an error.)
    /// </summary>
    public static string _ToString(this Array inArr, string valuesSeparator, string rowsSeparator, 
                                string valuesNumbering, string rowsNumbering, string formatstring, params int[] parameters)
    { if (inArr == null || inArr.Rank != 2) return ""; // Must be a matrix.
      int arrlen = inArr.Length;  if (arrlen == 0) return "";
      int norows = 1 + inArr.GetUpperBound(0),  nocols = 1 + inArr.GetUpperBound(1);
      if (valuesSeparator == null) valuesSeparator = String.Empty;
      if (rowsSeparator == null) rowsSeparator = String.Empty;
      bool useFmtStr = (!String.IsNullOrEmpty(formatstring));
      int tablength = 0,  rowWrap = arrlen+1; // the default row length, if not set in 'parameters'
      if (parameters.Length > 0) tablength = parameters[0];
      if (parameters.Length > 1) rowWrap = parameters[1];  
      if (rowWrap < 1 || rowWrap >= nocols) rowWrap = nocols+1; // tantamount to not using row wrapping.
      // Get numerical indicator of array type. See comment below - "I can't yet see..."
      int typeno = 0; // applies where no format string is valid / required.
      if (useFmtStr)
      { // I can't yet see any way around this next caboodle. If you try "try {inArr.GetValue(i).ToString(formatstring); ...}
        //  you can't get past the compiler, which simply won't allow arguments to .ToString before a specific type cast.
        typeno = TypeNo(ref inArr);
        useFmtStr = (typeno != 0);
      }
      // OK, all parameters set, so let's write the return string:
      string ss, tt, result = "";  
      bool endOfWrapLength = false;
      for (int rw = 0; rw < norows; rw++)
      { for (int cl = 0; cl < nocols; cl++)
        { // Prepare number prefixes: 
          ss = "";
          if (cl == 0) ss = Fill_Out(rowsNumbering, rw); // will be "", if arg. empty or null
          ss += Fill_Out(valuesNumbering, cl); // will be "", if arg. empty or null
          tt = ValueStr(ref inArr, rw, cl, typeno, formatstring);
          ss += tt;
          // Add tabbing characters and separator, if required:
          if (cl < nocols-1) ss += valuesSeparator;  else ss += rowsSeparator;
          endOfWrapLength = ( (cl+1) % rowWrap == 0);
          if (cl < nocols-1 && !endOfWrapLength && tablength > 0) ss = ss._PadTo(tablength, ' ', true);        
          result += ss;
          // Add LF if more than one row, and this is the end of the row:
          if (endOfWrapLength) result += '\n';
        }
      }
      return result;
    }

  // Intended for local use. Used by the above methods to flesh out row and value numbering labels. '0..0' is replaced by the string
  //  version of digits. The no. of '0's sets the minimum length, with padding by spaces at the left if nec. padded by spaces at left if nec.
  //  to get same no. characters. No '0' --> empty string.
  //  Null or empty strings return "".
    public static string Fill_Out(string instr, int index)
    { string result = String.Empty;
      if (String.IsNullOrEmpty(instr)) return result;
      int ptr1 = instr.IndexOf('0');  if (ptr1 == -1) return result;                                                                                
      int ptr2 = ptr1, lastone = instr.Length-1;
      while (ptr2 < lastone)
      { if (instr[ptr2 + 1] != '0') break; 
        ptr2++;
      }        
      int minlen = ptr2 - ptr1 + 1;
      string numstr = index.ToString();   
      numstr = numstr._PadTo(minlen, ' ', false);
      result = instr._ScoopTo(ptr1, ptr2, numstr);
      return result;
    }      
    // I have so far failed to find a way around having to have this private method, as .ToString(.) won't
    // work with a format string if you don't specifically state the array type.
    private static int TypeNo( ref Array inArr)
    { string typoe = inArr.GetType().Name; // will come back as e.g. "Double[]" (array) or "Double" (scalar).
      int ptr = typoe.IndexOf('[');
      if (ptr != -1) typoe = typoe._Extent(0, ptr); // If ptr is -1 (scalar type), leave typoe as is.
      int result = 0; // the default for any other type.
      if      (typoe == "Double")  result = 1; // No particular numbering order intended.
      else if (typoe == "Quad")    result = 2;
      else if (typoe == "Octet")   result = 3;
      else if (typoe == "Duplex")  result = 4;
      else if (typoe == "PairIX")  result = 5;
      else if (typoe == "Triplex") result = 6;
      else if (typoe == "TDRect")  result = 7;
      else if (typoe == "Twelver") result = 8;
      else if (typoe == "Single")  result = 9;
      else if (typoe == "Int32")   result = 10;
      return result;
    }  
    // If a one-dim array, set rowNo to -1.
//##########*** Abolish the need for method TypeNo(.), and rewrite ValueStr(.), as follows.
//##########*** This line goes into the above ._ToString(.) method:
//##########*** " TypeCode typeno = Type.GetTypeCode( testObject.GetType() ); " (and of course remove earlier "int typeno" defn.)
//##########*** And the following goes into ValueStr(.), which will then take argument 'TypeCode typeNo', not 'int typeNo'.
//##########*** switch(toad)
//##########*** { case TypeCode.Boolean:
//##########***   { <do such-and-scuh>;
//##########***     break;
//##########***   }

    private static string ValueStr(ref Array inArr, int rowNo, int colNo, int typeNo, string formatstring)
    { int[] ii;   string result;
      if (rowNo == -1) { ii = new int[1];                    ii[0] = colNo; }
      else             { ii = new int[2];    ii[1] = colNo;  ii[0] = rowNo; }
      try
      { switch (typeNo)
        { case 0:  result = inArr.GetValue(ii).ToString();  break; // no hassling with format strings nec.
          case 1:  result = ( (double) inArr.GetValue(ii)).ToString(formatstring);  break;
          case 2:  result = ( (Quad) inArr.GetValue(ii)).ToString(formatstring);  break;
          case 3:  result = ( (Octet) inArr.GetValue(ii)).ToString(formatstring);  break;
          case 4:  result = ( (Duplex) inArr.GetValue(ii)).ToString(formatstring);  break;
          case 5:  result = ( (PairIX) inArr.GetValue(ii)).ToString(formatstring);  break;
          case 6:  result = ( (Triplex) inArr.GetValue(ii)).ToString(formatstring);  break;
          case 7:  result = ( (TDRect) inArr.GetValue(ii)).ToString(formatstring);  break;
          case 8:  result = ( (Twelver) inArr.GetValue(ii)).ToString(formatstring);  break;
          case 9:  result = ( (float) inArr.GetValue(ii)).ToString(formatstring);  break;
          case 10: result = ( (int) inArr.GetValue(ii)).ToString(formatstring);  break;
          default: result = ""; break;
        }  
      }
      catch { result = ""; }
      return result;
    }  

 // ++++ GENERAL ARRAY EXTENSIONS: MULTI-DIM'L ++++

/// <summary>
/// The input array may have any number of dimensions; the returned int[] will have size = no. dimensions,
/// and dimensions will be in the order in which they are used in an assigment (e.g. [len0, len1, len2] for an array
/// assigned using "thisArray[i0, i1, i2] = ..."). If the input array is NULL, so will be the returned int[].
/// </summary>
  public static int[] _Lengths(this Array InArr)
  { if (InArr == null) return null;
    int noDims = InArr.Rank;
    int[] result = new int[noDims];
    for (int i=0; i < noDims; i++) result[i] = InArr.GetLength(i);
    return result;
  }

  public static object _Clone(this Array InArr)
  { if (InArr == null) return null;
    else return InArr.Clone();
  }

/// <summary>
/// Assumes that rows and columns are deployed such that a matrix element is addressed as: Matrix[row, column].
/// If InArr is NULL, returns -1, -1.  If InArr is not a matrix, returns -2, -2. Don't try it on with a ragged
/// matrix; I have no idea what would happen.
/// </summary>
  public static void _MxDimensions(this Array InArr,  out int NoRows,  out int NoCols)
  { NoRows = -1;  NoCols = -1;   if (InArr == null) return;
    int noDims = InArr.Rank;
    if (noDims != 2) { NoRows = -2;  NoCols = -2;  return; }
    NoRows = InArr.GetLength(0);   NoCols = InArr.GetLength(1);
  }


 // ++++ INTEGER ARRAY EXTENSIONS ++++        //arrint

    /// <summary>
    /// inArr._IndexOf(Value [, P [, Q ] ]) -- returns address of first instance of Value in inArr, or -1 if none.
    /// If P and Q are present, they define inclusive limits of the search. 
    /// Improper P and Q corrected as best possible. Null or empty 'inArr' or crossed pointers cause a return of -1. 
    /// </summary>
    public static int _IndexOf(this int[] inArr, int target, params int[] FromTo)
    { if (inArr == null) return -1;
      Trio trio = JS.SegmentData(inArr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      return Array.IndexOf(inArr, target);
    }

    /// <summary>
    /// inArr._IndexOfAny(targets [, P [, Q ] ]) -- returns address of first instance in inArr of any value in targets, or -1 if none.
    /// If P and Q are present, they define inclusive limits of the search. 
    /// Improper P and Q corrected as best possible. Null or empty 'inArr' or 'targets' or crossed pointers cause a return of -1. 
    /// </summary>
    public static int _IndexOfAny(this int[] inArr, int[] targets, params int[] FromTo)
    { if (inArr._NorE() || targets._NorE()) return -1;
      Trio trio = JS.SegmentData(inArr.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inStr defined by the limits.
      int p, firstFind = -1;
      for (int i=0; i < targets.Length; i++)
      {  p = Array.IndexOf(inArr, targets[i]);
         if (p != -1 && (firstFind == -1 || p < firstFind) ) firstFind = p;
      }   
      return firstFind;
    }

    /// <summary>
    /// inArr._IndexOfNot(checkList [, P [, Q ] ] ) -- returns location of first integer not in checkList'; or -1, if no such integer found.
    /// If P and Q are present, they define inclusive limits of the search. Improper P and Q corrected as best possible.
    /// Null or empty inArr or checkList or crossed pointers returns -1.
    /// </summary>
    public static int _IndexOfNoneOf(this int[] inArr, int[] checkList, params int[] FromTo)
    { if (inArr._NorE() || checkList._NorE() ) return -1;
      int inlen = inArr.Length;
      Trio trio = JS.SegmentData(inlen, false, FromTo); // sort out the pointers
      int fromPtr = trio.X;
      if (fromPtr < 0) return  -1;
      // In all remaining cases there is a nonzero extent of inArr defined by the limits.
      for (int i=fromPtr; i <= trio.Y; i++)
      { if (Array.IndexOf(checkList, inArr[i]) == -1) return i; }
      return -1;
    }


/// <summary>
/// Return an integer array with all values in Insertion inserted at atPtr. If atPtr is zero or neg., Insertion is added to the 
/// front of the array (i.e. before its first element); if atPtr is beyond the end of IntArray, Insertion will be appended to it. 
/// FAULTY ARGS: If Insertion is null or empty, a copy of IntArray is returned. If IntArray is null or empty, a copy of Insertion is returned. 
/// If both are null or empty, NULL is returned.
/// </summary>
    public static int[] _Insert(this int[] IntArray, int atPtr, params int[] Insertion)
    {
      int origlen = 0;   if (IntArray != null)  origlen   = IntArray.Length;
      int insertlen = 0; if (Insertion != null) insertlen = Insertion.Length;
      if (origlen + insertlen == 0) return null; // Both arrays are empty, so no dice.
      int outlen = origlen + insertlen;
      int[] result = new int[outlen];
      if (atPtr > 0) Array.Copy(IntArray, result, atPtr); // Put in whatever preceded atPtr.
      if (insertlen > 0)  Array.Copy(Insertion, 0, result, atPtr, insertlen); // Put in Insertion, if it exists.
      if (atPtr < origlen)Array.Copy(IntArray, atPtr, result, atPtr + insertlen, origlen - atPtr); // Put in Insertion, if it exists.
      return result;
    }

/// <summary>
/// Return an integer array with all values between pointers (inclusive) removed (leaving intact all values before and after the pointers.)
/// If Replacement is non-null and non-empty, it will replace the removed segment.
/// Pointer adjustments: (1) negative fromPtr adjusted to 0; (2) negative or overlength toPtr is adjusted
/// to last entry of IntArray. (To cover whole array, use e.g. -1 for both pointers.) RETURNED: (1) the original array,
/// if pointers cross. (2) NULL returned if IntArray is null or empty
/// </summary>
    public static int[] _ScoopTo(this int[] IntArray, int fromPtr, int toPtr, params int[] Replacement)
    {
      int[] result = null;
      if (IntArray == null || IntArray.Length == 0) return null;
      int inlen = IntArray.Length;
      if (fromPtr < 0) fromPtr = 0;  
      if (toPtr < 0 || toPtr >= inlen) toPtr = inlen-1;
      int extent = toPtr - fromPtr + 1; // will be 0 or neg, for crossed pointers.
      // Return simply a copy of the input array, if extent is zero or negative:
      if (extent <= 0) { result = new int[IntArray.Length];  Array.Copy(IntArray, result, IntArray.Length); return result; }
      // Otherwise return the doctored copy:
      int replen;
      if (Replacement == null) replen = 0; else replen = Replacement.Length;
      int outlen = IntArray.Length - extent + replen;
      result = new int[outlen];
      if (fromPtr > 0) Array.Copy(IntArray, result, fromPtr); // Put in whatever preceded fromPtr.
      if (replen > 0)  Array.Copy(Replacement, 0, result, fromPtr, replen); // Put in Replacement, if it exists.
      if (toPtr < inlen-1)Array.Copy(IntArray, toPtr+1, result, fromPtr + replen, inlen - toPtr - 1); // Put in Replacement, if it exists.
      return result;
    }

/// <summary>
/// Return an integer array with all values in Target removed between pointers inclusive. (The returned array retains intact 
/// all values before and after the pointers.)
/// Pointer adjustments: (1) negative fromPtr adjusted to 0; (2) negative or overlength toPtr is adjusted
/// to last entry of IntArray. (To cover whole array, use e.g. -1 for both pointers.) RETURNED: (1) the original array,
/// if Target empty or null, or none of its values found, or if pointers cross. (2) NULL returned if IntArray is null or empty
/// </summary>
    public static int[] _Purge(this int[] IntArray, int fromPtr, int toPtr, params int[] Target)
    {
      if (IntArray == null || IntArray.Length == 0) return null;
      int n=0, tarlen;  if (Target == null) tarlen = 0; else tarlen = Target.Length;
      int inlen = IntArray.Length;
      if (fromPtr < 0) fromPtr = 0;  
      if (toPtr < 0 || toPtr >= inlen) toPtr = inlen-1;
      int extent = toPtr - fromPtr + 1; // will be 0 or neg, for crossed pointers.
      List<int> resultList = new List<int>(); 
     // Start the list off with any stuff in IntArray preceding the start pointer.
      for (int i=0; i < fromPtr; i++) resultList.Add(IntArray[i]);         
     // Add any values from the extent not shared with Target:
      bool found;
      for (int i=0; i < extent; i++)
      { n = IntArray[fromPtr+i];  found = false;
        for (int j=0; j < tarlen; j++)
        { if (Target[j] == n) { found = true; break; } }
        if (!found) resultList.Add(n);
      }  
     // Tack on any stuff in IntArray following the end pointer.
      for (int i=toPtr+1; i < inlen; i++) resultList.Add(IntArray[i]);         
      if (resultList.Count == 0) return null;
      int[] result = resultList.ToArray();        
      return result;
    }

/// <summary>
/// Removes values from that segment of IntArray indicated by fromPtr and toPtr (inclusive). (Indicate the whole array by using 0 or -1
/// for fromPtr and -1 for toPtr.) All values lying between lowValue and highValue are removed. 
/// ARGUMENT ADJUSTMENTS: fromPtr less than 0 adjusted to 0; toPtr less than 0 or beyond end of IntArray is adjusted to end of IntArray;
/// if lowValue > highValue, their values are swapped.
/// RETURNED ARRAY: All values before fromPtr and after toPtr are returned as is, with the processed delineated segment between them.  
/// Where all values have been removed from a whole array, a NULL array is returned (NULL also returned if IntArray is null or empty).
/// </summary>
    public static int[] _PurgeRange(this int[] IntArray, int fromPtr, int toPtr, int lowValue, int highValue)
    { if (IntArray == null || IntArray.Length == 0) return null;
      int n=0;
      if (lowValue > highValue) {n = lowValue; lowValue = highValue; highValue = n; }
      int inlen = IntArray.Length;
      if (fromPtr < 0) fromPtr = 0;  
      if (toPtr < 0 || toPtr >= inlen) toPtr = inlen-1;
      int extent = toPtr - fromPtr + 1; // will be 0 or neg, for crossed pointers.
      List<int> resultList = new List<int>(); 
     // Start the list off with any stuff in IntArray preceding the start pointer.
      for (int i=0; i < fromPtr; i++) resultList.Add(IntArray[i]);         
     // Add any values from the extent not shared with Target:
      { for (int i=0; i < extent; i++)
        { n = IntArray[fromPtr+i];  
          if (n < lowValue || n > highValue) resultList.Add(n);
        }  
      }
     // Tack on any stuff in IntArray following the end pointer.
      for (int i=toPtr+1; i < inlen; i++) resultList.Add(IntArray[i]);         
      if (resultList.Count == 0) return null;
      int[] result = resultList.ToArray();        
      return result;
    }

/// <summary>
/// FINDS details of the MAXimum value in IntArray (if MaxMin = '+') or of its MINimum value (if MaxMin = '-' [or anything but '+']).
/// RETURNS an array of length 4: [0] = value of extremum, [1] = first location of it, [2] = last location of it, [3] = no. occurrences of it.
/// ARGUMENT ADJUSTMENTS: fromPtr less than 0 adjusted to 0; toPtr less than 0 or beyond end of IntArray is adjusted to end of IntArray.
/// ERROR (caused by: IntArray null or empty; pointers invalid): returns NULL instead.
/// SEE ALSO: .Max(.) and ._Min(.) - returns the maximum / minumum value, with no other data.
/// </summary>
    public static int[] _Extremum(this int[] IntArray, int fromPtr, int toPtr, char MaxMin)
    { if (IntArray == null || IntArray.Length == 0) return null;
      int n=0;  bool isMax = (MaxMin == '+');
      int inlen = IntArray.Length;
      if (fromPtr < 0) fromPtr = 0;  
      if (toPtr < 0 || toPtr >= inlen) toPtr = inlen-1;
      if (toPtr < fromPtr) return null;
      int extreme = IntArray[fromPtr], firstone = fromPtr, lastone = fromPtr, count = 1;
      for (int i=fromPtr+1; i <= toPtr; i++)
      { n = IntArray[i];  
        if ( (isMax && n > extreme) || (!isMax && n < extreme) ) 
        { extreme = n; firstone = i;  lastone = i; count = 1; }
        else if (n == extreme) { lastone = i; count++; }
      }  
      return new int[] { extreme, firstone, lastone, count };
    }
/// <summary>
/// RETURNS the Maximum value in IntArray. If present, FromTo[0] points to the first location checked in InArr; and if present, 
/// FromTo[1] points to the last checked. If supplied, these pointers must be valid.
/// ERROR (caused by: IntArray null or empty; pointers supplied but invalid): returns Int32.MaxValue instead.
/// SEE ALSO: ._Min(.) - returns minumum; ._Extremum(.) - returns max. or min. with further data.
/// </summary>
    public static int _Max(this int[] IntArray, params int[] FromTo)
    { if (IntArray == null || IntArray.Length == 0) return Int32.MaxValue;
      int n, inlen = IntArray.Length, fromPtr = 0, toPtr = inlen-1;
      if (FromTo.Length > 0) 
      { fromPtr = FromTo[0];  if (FromTo.Length > 1) toPtr = FromTo[1];
        if (fromPtr < 0 || toPtr >= inlen || toPtr < fromPtr) return Int32.MaxValue;
      }  
      int extreme = IntArray[fromPtr];
      for (int i=fromPtr+1; i <= toPtr; i++) { n = IntArray[i];  if (n > extreme) extreme = n; }  
      return extreme;
    }
/// <summary>
/// RETURNS the Maximum ABSOLUTE value in IntArray. If present, FromTo[0] points to the first location checked in InArr; and if present, 
/// FromTo[1] points to the last checked. If supplied, these pointers must be valid.
/// ERROR (caused by: IntArray null or empty; pointers supplied but invalid): returns Int32.MaxValue instead.
/// SEE ALSO: ._Min(.) - returns minumum; ._Extremum(.) - returns max. or min. with further data.
/// </summary>
    public static int _MaxAbs(this int[] IntArray, params int[] FromTo)
    { if (IntArray == null || IntArray.Length == 0) return Int32.MaxValue;
      int n, inlen = IntArray.Length, fromPtr = 0, toPtr = inlen-1;
      if (FromTo.Length > 0) 
      { fromPtr = FromTo[0];  if (FromTo.Length > 1) toPtr = FromTo[1];
        if (fromPtr < 0 || toPtr >= inlen || toPtr < fromPtr) return Int32.MaxValue;
      }  
      int extreme = Math.Abs(IntArray[fromPtr]);
      for (int i=fromPtr+1; i <= toPtr; i++) { n = Math.Abs(IntArray[i]);  if (n > extreme) extreme = n; }  
      return extreme;
    }
/// <summary>
/// RETURNS the Minimum value in IntArray. If present, FromTo[0] points to the first location checked in InArr; and if present, 
/// FromTo[1] points to the last checked. If supplied, these pointers must be valid.
/// ERROR (caused by: IntArray null or empty; pointers supplied but invalid): returns Int32.MinValue instead.
/// SEE ALSO: ._Max(.) - returns maximum; ._Extremum(.) - returns max. or min. with further data.
/// </summary>
    public static int _Min(this int[] IntArray, params int[] FromTo)
    { if (IntArray == null || IntArray.Length == 0) return Int32.MinValue;
      int n, inlen = IntArray.Length, fromPtr = 0, toPtr = inlen-1;
      if (FromTo.Length > 0) 
      { fromPtr = FromTo[0];  if (FromTo.Length > 1) toPtr = FromTo[1];
        if (fromPtr < 0 || toPtr >= inlen || toPtr < fromPtr) return Int32.MinValue;
      }  
      int extreme = IntArray[fromPtr];
      for (int i=fromPtr+1; i <= toPtr; i++) { n = IntArray[i];  if (n < extreme) extreme = n; }  
      return extreme;
    }
/// <summary>
/// Returns the result of multiplying IntArray on a term-by-term basis with the argument value(s). If there are less values than the length of
/// IntArray, values are recycled (e.g. [2,2,2,2,2,2] x [10, 20] --> [20, 40, 20, 40, 20, 40]). If more, excess values are ignored.
/// HINT: (1) Square an array by multiplying it by itself. (2) Down-sample an array by multiplying by e.g. [0,0,0,1].
/// </summary>
    public static int[] _MultiplyBy(this int[] IntArray, params int[] Multiplier)
    { if (IntArray == null) return null;
      int inlen = IntArray.Length;
      int[] result = new int[inlen];
      int mullen = Multiplier.Length;
      if (mullen == 1) // separate this case out, for the sake of speed
      { int multiplier = Multiplier[0];
        for (int i=0; i < inlen; i++) { result[i] = IntArray[i] * multiplier; }  
      }
      else if (mullen != 0)
      { for (int i=0; i < inlen; i++) { result[i] = IntArray[i] * Multiplier[i%mullen]; }  
      }
      return result;
    }
/// <summary>
/// Returns the result of adding IntArray on a term-by-term basis to the argument value(s). If there are less values than the length of
/// IntArray, values are recycled (e.g. [1,1,1,1] + [10, 20] --> [11, 21, 11, 21]). If more, excess values are ignored.
/// </summary>
    public static int[] _Add(this int[] IntArray, params int[] Addend)
    { if (IntArray == null) return null;
      int inlen = IntArray.Length;
      int[] result = new int[inlen];
      int addlen = Addend.Length;
      if (addlen == 1) // separate this case out, for the sake of speed
      { int adder = Addend[0];
        for (int i=0; i < inlen; i++) { result[i] = IntArray[i] + adder; }  
      }
      else if (addlen != 0)
      { for (int i=0; i < inlen; i++) { result[i] = IntArray[i] + Addend[i%addlen]; }  
      }
      return result;
    }
/// <summary>
/// <para>Returns a copy of IntArray; the copy does not have to be pre-dimensioned, as it does with the void method ".CopyTo(.)".
/// Copies null and empty arrays as well.</para>
/// <para>Adjusts out-of-range pointers to array extremes; crossed pointers return empty array.</para>
/// </summary>
    public static int[] _Copy(this int[] IntArray, params int[] FromExtent)
    { if (IntArray == null) return null;
      int[] result = null;
      int inarrlen = IntArray.Length;
      if (FromExtent.Length == 0) // The form below with params. takes only around 3% longer (using exact params. for whole string).
                                  // (A bit of trivia: to copy the same length of a char[] took less than 50% as long, with same params.)
      { result = new int[inarrlen];
        if (inarrlen > 0) IntArray.CopyTo(result, 0);
        return result;
      }
    // FromExtent supplied:
      int fromptr = FromExtent[0];
      if (fromptr < 0) fromptr = 0; 
      else if (fromptr >= inarrlen) return new int[0];
      int extent = inarrlen - fromptr;
      if (FromExtent.Length > 1) 
      { int n = FromExtent[1]; if (n < extent) extent = n; } // effectively reduces an oversized FromExtent[1].
      if (extent <= 0) return new int[0];
      // Must be a valid nonempty extent:      
      result = new int[extent];
      Array.Copy(IntArray, fromptr, result, 0, extent);
      return result;
    }

/// <summary>
/// Returns a copy of IntArray with values replaced by their absolute values.
/// </summary>
    public static int[] _Abs(this int[] IntArray)
    { if (IntArray == null) return null;
      int inlen = IntArray.Length;
      int[] result = new int[inlen];
      { for (int i=0; i < inlen; i++) { result[i] = Math.Abs(IntArray[i]); }  
      }
      return result;
    }
/// <summary>
/// Returns a copy of IntArray with each value in the array replaced by (-value).
/// </summary>
    public static int[] _Negate(this int[] IntArray)
    { if (IntArray == null) return null;
      int inlen = IntArray.Length;
      int[] result = new int[inlen];
      { for (int i=0; i < inlen; i++) { result[i] = -IntArray[i]; }  
      }
      return result;
    }
/// <summary>
/// Returns a copy of IntArray with these changes:  If HL is 'H', all values above HiValue are replaced by HiValue. (LoValue is ignored.) 
/// If HL is 'L', all values below LoValue are replaced by LoValue. (HiValue is ignored.) If HL is 'B' for 'both',
/// both tests are applied (LoValue test first). (Any other value of HL results in no changes to the array.)
/// </summary>
    public static int[] _Clamp(this int[] IntArray, char HL, int LoValue, int HiValue)
    { if (IntArray == null) return null;
      int inlen = IntArray.Length;  if (inlen == 0) return new int[] {};
      int[] result = new int[inlen];   IntArray.CopyTo(result, 0);
      if (HL == 'L' || HL == 'B')
      { for (int i=0; i < inlen; i++) { if (result[i] < LoValue) result[i] = LoValue; } }
      if (HL == 'H' || HL == 'B')
      { for (int i=0; i < inlen; i++) { if (result[i] > HiValue) result[i] = HiValue; } }
      return result;
    }

    public static double[] _ToDubArray(this int[] IntArray, params int[] FromExtent)
    { if (IntArray == null) return null;
      Trio trio = JS.SegmentData(IntArray.Length, true, FromExtent); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  new double[0];
      // In all remaining cases there is a nonzero extent of IntArray defined by the limits.
      double[] result = new double[extent];
      for (int i=0; i < extent; i++) result[i] = (double) IntArray[fromPtr + i];
      return result;
    }  

    /// <summary>
    /// int[]._Chain(extent) -- returns an int[] made up by cycling through values in the input array, appending them to the output
    /// until the output array has length 'extent'. (If extent is less than the length of the input array, the effect will be of truncation.)
    /// No errors raised; if extent is less than 1, an empty array is returned.
    /// </summary>
    public static int[] _Chain(this int[] IntArray, int extent)
    { int inLen = IntArray._Length();  if (inLen == -1) return null; else if (inLen == 0) return new int[0];
      if (extent <= 0) return new int[0];
      List<int> result = new List<int>(extent);
      int times = extent / inLen, leftover = extent % inLen;
      for (int i=0; i < times; i++) result.AddRange(IntArray);
      if (leftover > 0) result.AddRange(IntArray._Copy(0, leftover));
      return result.ToArray();
    }

/// <summary>
/// Returned int[] is NULL of JaggedMx is null; otherwise its length = the no. rows in JaggedMx, elements being row length
///  (or -1 for a null row).
/// </summary>
  public static int[] _JaggedLengths(this int[][] JaggedMx)
  { if (JaggedMx == null) return null;
    int noRows = JaggedMx.GetLength(0);
    int[] result = new int[noRows];
    for (int i=0; i < noRows; i++)
    { if (JaggedMx[i] == null) result[i] = -1;  else result[i] = JaggedMx[i].Length; }
    return result;
  }

  /// <summary>
  /// <para>For the integer array, find the first instance of Value between and including the supplied limits; or within the whole array, if no limits
  /// supplied; or from the start point to the end of the array, if just one limit supplied. Errors and No Finds both return -1;
  /// otherwise the return is the index of the find.</para>
  /// <para>Out of range arguments are adjusted. If (after any adjustment) they cross, -1 is returned.</para>
  /// </summary>
  public static int _Find(this int[] IntArray, int Value, params int[] FromTo)
  { if (IntArray == null) return -1;
    Trio trio = JS.SegmentData(IntArray.Length, false, FromTo); // sort out the pointers
    int fromPtr = trio.X, toPtr = trio.Y;
    if (fromPtr < 0) return  -1; // -1 is an error code from fn. JS.SegmentData(.), covering all impossible FromTo values.
    int findindex = -1;
    for (int i = fromPtr; i <= toPtr; i++)
    { if (IntArray[i] == Value)
      { findindex = i;  break; }
    }
    return findindex;
  }

   /// <summary>
    /// <para>In the integer array, find all instances of Value between and including the supplied limits; or within the whole array, if no limits
    /// supplied; or from the start point to the end of the array, if just one limit supplied. Errors and No Finds both return a non-null
    /// array of zero length; otherwise an int[] array with as many elements as there are finds is returned, giving their indices in the input array.</para>
    /// <para>Out of range arguments are adjusted. If (after any adjustment) they cross, the empty array is returned.</para>
    /// </summary>
    public static int[] _FindAll(this int[] IntArray, int Value, params int[] FromTo)
    { if (IntArray == null) return new int[0];
      Trio trio = JS.SegmentData(IntArray.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y;
      if (fromPtr < 0) return  new int[0];
      List<int> lister = new List<int>();
      for (int i = fromPtr; i <= toPtr; i++)
      { if (IntArray[i] == Value) lister.Add(i); }
      if (lister.Count == 0) return new int[0];
      return lister.ToArray();
    }



 // ++++ BYTE ARRAY EXTENSIONS ++++        //arrbyte

/// <summary>
/// <para>Returns a copy of ByteArray; the copy does not have to be pre-dimensioned, as it does with the void method ".CopyTo(.)".
/// Copies null and empty arrays as well.</para>
/// <para>Adjusts out-of-range pointers to array extremes; crossed pointers return empty array.</para>
/// </summary>
    public static byte[] _Copy(this byte[] ByteArray, params int[] FromExtent)
    { if (ByteArray == null) return null;
      byte[] result = null;
      int inarrlen = ByteArray.Length;
      if (FromExtent.Length == 0) // The form below with params. takes only around 3% longer (using exact params. for whole string).
                                  // (A bit of trivia: to copy the same length of a char[] took less than 50% as long, with same params.)
      { result = new byte[inarrlen];
        if (inarrlen > 0) ByteArray.CopyTo(result, 0);
        return result;
      }
    // FromExtent supplied:
      int fromptr = FromExtent[0];
      if (fromptr < 0) fromptr = 0;
      else if (fromptr >= inarrlen) return new byte[0];
      int extent = inarrlen - fromptr;
      if (FromExtent.Length > 1)
      { int n = FromExtent[1]; if (n < extent) extent = n; } // effectively reduces an oversized FromExtent[1].
      if (extent <= 0) return new byte[0];
      // Must be a valid nonempty extent:
      result = new byte[extent];
      Array.Copy(ByteArray, fromptr, result, 0, extent);
      return result;
    }

    public static double[] _ToDubArray(this byte[] InArray, params int[] FromExtent)
    { if (InArray == null) return null;
      Trio trio = JS.SegmentData(InArray.Length, true, FromExtent); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  new double[0];
      // In all remaining cases there is a nonzero extent of IntArray defined by the limits.
      double[] result = new double[extent];
      for (int i=0; i < extent; i++) result[i] = (double) InArray[fromPtr + i];
      return result;
    }


    /// <summary>
    /// <para>In the byte array, find all instances of Value between and including the supplied limits; or within the whole array, if no limits
    /// supplied; or from the start point to the end of the array, if just one limit supplied. Errors and No Finds both return a non-null
    /// array of zero length; otherwise an int[] array with as many elements as there are finds is returned, giving their indices in the input array.</para>
    /// <para>Out of range arguments are adjusted. If (after any adjustment) they cross, the empty array is returned.</para>
    /// </summary>
    public static int[] _FindAll(this byte[] ByteArray, byte Value, params int[] FromTo)
    { if (ByteArray == null) return new int[0];
      Trio trio = JS.SegmentData(ByteArray.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y;
      if (fromPtr < 0) return  new int[0];
      List<int> lister = new List<int>();
      for (int i = fromPtr; i <= toPtr; i++)
      { if (ByteArray[i] == Value) lister.Add(i); }
      if (lister.Count == 0) return new int[0];
      return lister.ToArray();
    }


// ++++ DOUBLE ARRAY EXTENSIONS ++++        //arrdouble

/// <summary>
/// Returns a copy of DubArray; the copy does not have to be pre-dimensioned, as it does with the void method ".CopyTo(.)".
/// Copies null and empty arrays as well.
/// </summary>
    public static double[] _Copy(this double[] DubArray, params int[] FromExtent)
    { if (DubArray == null) return null;
      double[] result = null;
      int inarrlen = DubArray.Length;
      // This method does not have a separate empty-args. variation (which char[] and int[] versions have), as there was no time diff. on testing.
      int fromptr = 0; if (FromExtent.Length > 0) fromptr = FromExtent[0];
      if (fromptr < 0) fromptr = 0; 
      else if (fromptr >= inarrlen) return new double[0];
      int extent = inarrlen - fromptr;
      if (FromExtent.Length > 1) 
      { int n = FromExtent[1]; if (n < extent) extent = n; } // effectively reduces an oversized FromExtent[1].
      if (extent <= 0) return new double[0];
      // Must be a valid nonempty extent:      
      result = new double[extent];
      Array.Copy(DubArray, fromptr, result, 0, extent);
      return result;
    }
    /// <summary>
    /// <para>In DubArray, find the first instance of Value between and including the supplied limits; or within the whole array, if no limits
    /// supplied; or from the start point to the end of the array, if just one limit supplied. Errors and No Finds both return -1;
    /// otherwise the location in DubArray is returned.</para>
    /// <para>Out of range arguments are adjusted. If (after any adjustment) they cross, -1 is returned.</para>
    /// </summary>
    public static int _Find(this double[] DubArray, double Value, params int[] FromTo)
    { if (DubArray == null) return -1;
      Trio trio = JS.SegmentData(DubArray.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y;
      if (fromPtr < 0) return  -1;
      for (int i = fromPtr; i <= toPtr; i++)
      { if (DubArray[i] == Value) return i; }
      return -1;
    }    
    /// <summary>
    /// <para>In DubArray, find the first instance of array Sequence between and including the supplied limits; or within the whole array,
    ///   if no limits supplied; or from the start point to the end of the array, if just one limit supplied. Errors and No Finds both return -1;
    /// otherwise the location in DubArray is returned.</para>
    /// <para>Out of range arguments are adjusted. If (after any adjustment) they cross, -1 is returned.</para>
    /// </summary>
    public static int _Find(this double[] DubArray, double[] Sequence, params int[] FromTo)
    { int inLen = DubArray._Length(),  seqLen = Sequence._Length();
      if (inLen <= 0 || seqLen <= 0) return -1;
      Trio trio = JS.SegmentData(DubArray.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y - seqLen + 1;
      if (fromPtr < 0|| toPtr < fromPtr) return  -1;
      for (int i = fromPtr; i <= toPtr; i++)
      { if (DubArray[i] == Sequence[0])
        { bool isMatch = true;
          for (int j = 1; j < seqLen; j++)
          { if (DubArray[i+j] != Sequence[j]) { isMatch = false;  break; }
          }
          if (isMatch) return i;
        }
      }
      return -1;
    }

    /// <summary>
    /// <para>In DubArray, find all instances of Value between and including the supplied limits; or within the whole string, if no limits
    /// supplied; or from the start point to the end of the array, if just one limit supplied. Errors and No Finds both return a non-null
    /// array of zero length; otherwise an int[] array with as many elements as there are finds is returned, giving their indices in DubArray.</para>
    /// <para>Out of range arguments are adjusted. If (after any adjustment) they cross, the empty array is returned.</para>
    /// </summary>
    public static int[] _FindAll(this double[] DubArray, double Value, params int[] FromTo)
    { if (DubArray == null) return new int[0];
      Trio trio = JS.SegmentData(DubArray.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y;
      if (fromPtr < 0) return  new int[0];
      List<int> lister = new List<int>();
      for (int i = fromPtr; i <= toPtr; i++)
      { if (DubArray[i] == Value) lister.Add(i); }
      if (lister.Count == 0) return new int[0];
      return lister.ToArray();
    }    

    /// <summary>
    /// <para>In DubArray, find all instances of array Sequence between and including the supplied limits; or within the whole string, if no limits
    /// supplied; or from the start point to the end of the array, if just one limit supplied. Errors and No Finds both return a non-null
    /// array of zero length; otherwise an int[] array with as many elements as there are finds is returned, giving their indices in DubArray.</para>
    /// <para>Out of range arguments are adjusted. If (after any adjustment) they cross, the empty array is returned.</para>
    /// </summary>
    public static int[] _FindAll(this double[] DubArray, double[] Sequence, params int[] FromTo)
    { int inLen = DubArray._Length(),  seqLen = Sequence._Length();
      if (inLen <= 0 || seqLen <= 0) return new int[0];
      Trio trio = JS.SegmentData(DubArray.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y - seqLen + 1;
      if (fromPtr < 0|| toPtr < fromPtr) return new int[0];
      List<int> lister = new List<int>();
      int i = fromPtr;
      while (i <= toPtr)
      { bool isMatch = false;
        if (DubArray[i] == Sequence[0])
        { isMatch = true;
          for (int j = 1; j < seqLen; j++)
          { if (DubArray[i+j] != Sequence[j]) { isMatch = false;  break; }
          }
        }
        if (isMatch) { lister.Add(i);  i += seqLen; }  else i++;
      }
      if (lister.Count == 0) return new int[0];
      return lister.ToArray();
    }

    /// <summary>
    /// <para>In DubArray, find all DELIMITED subarrays equal to array Sequence. In all that follows, assume an initial 'virtual' delimiter at
    ///   (notional) index [-1] of DubArray, and a final 'virtual' delimiter at (notional) index [length of DubArray].</para>
    /// <para>Case 1: Sequence is nonempty. Will find any matching subarray, provided 'From' is no later than the preceding delimiter (virtual
    ///   or actual) and 'To' no earlier than the delimiter beyond the last element of that subarray. NB: To open the whole of DubArray
    ///   for checking, 'From' must be -1, not 0; and 'To' must be the length of DubArray, not the index of its last element. (In fact, any 
    ///   negative 'From' is taken as -1, and any oversized 'To' is taken back to length of DubArray.) In the absence of FromTo, these 
    ///   are the default values.</para>
    /// <para>'Maxno' - if present and > 0, dictates the maximum number of finds allowed. (If present but ≤ 0, is ignored; all finds occur.)</para>
    /// <para>RETURNED, if no errors: (a) No finds - empty array; (b) finds: for each, the index of the preceding delimiter (can be -1).
    ///   Errors also return an empty array, with no other indication of error; occurs if DubArray or Sequence is null or empty, or
    ///   if pointers From and To are equal or cross.</para>
    /// </summary>
    public static int[] _FindAllDelimited(this double[] DubArray, double[] Sequence, double Delimiter, params int[] FromToMaxno)
    { int inLen = DubArray._Length();  if (inLen <= 0) return new int[0];
      int seqLen = Sequence._Length(); // can be zero, legally (when looking for empty subarrays between contiguous delimiters)
      // Define the search range pointers
      int paramsLen = FromToMaxno.Length;
      int fromPtr = -1; // the virtual opening delimiter;
      int toPtr = inLen; // the virtual closing delimiter;
      if (paramsLen > 0)
      { fromPtr = FromToMaxno[0];  if (fromPtr < -1) fromPtr = -1;
        if (paramsLen > 1) { toPtr = FromToMaxno[1]; if (toPtr > inLen) toPtr = inLen; }
        // Now adjust these ptrs if nec, so that they point to a delimiter:
        if (fromPtr >= 0) // if it is -1, it is ipse facto already at the virtual delimiter 
        { while (true)
          { if (fromPtr >= inLen) return new int[0]; // No find possible, if fromPtr allowed to go to the final virtual delimiter.
            if ( DubArray[fromPtr] == Delimiter) break;
            fromPtr++;
          }
        }
        if (toPtr < inLen)
        { while (true)
          { if (toPtr < 0) return new int[0]; // No find possible, if toPtr allowed to go to the opening virtual delimiter.
            if ( DubArray[toPtr] == Delimiter) break;
            toPtr--;
          }
        }
        if (fromPtr + seqLen + 1 > toPtr) return new int[0]; // not enough room left to accommodate Sequence.
      }
      int maxNoFinds = inLen;
      if (paramsLen > 2  && FromToMaxno[2] > 0) maxNoFinds = FromToMaxno[2];
      List<int> lister = new List<int>(inLen);
      // In the below I will refer to the 'left' delimiter and the 'right' delimiter, in their relationship with the current subarray.
      int leftDelimPtr = fromPtr; // first 'left' delimiter.
      while (true)
      {// At the start of each relooping, leftDelimPtr MUST be set to what was the right delimiter for the last subarray.
        bool isMatch = false;
        if (seqLen == 0)
        { isMatch = (leftDelimPtr == inLen-1  ||  DubArray[leftDelimPtr+1] == Delimiter); // 1st. case: the right delim is the virtual last delim
        }
        else // nonempty 'Sequence'
        { if (leftDelimPtr + seqLen >= inLen) break; // no further matches possible. (nec. step, so 'j' won't overrun the array in loop below.)
          else if (DubArray[leftDelimPtr+1] == Sequence[0])
          { isMatch = true; // provisionally...
            for (int j = 2; j <= seqLen; j++)
            { if (DubArray[leftDelimPtr+j] != Sequence[j-1]) { isMatch = false;  break; }
            }
          }
          if (isMatch) // is it the whole subarray?
          { isMatch = (leftDelimPtr + seqLen + 1 == inLen  ||  DubArray[leftDelimPtr + seqLen + 1] == Delimiter); }
        }
        // Update the 'find' list, if a match; and in any case, set the next loop's left delimiter pointer.
        if (isMatch)
        { lister.Add(leftDelimPtr);  
          leftDelimPtr += seqLen + 1;
          if (leftDelimPtr == toPtr) break; // can't hope to find any more matches
          if (lister.Count == maxNoFinds) break; // user doesn't want any more matches
        }
        else
        { int ptr = leftDelimPtr+1; // involves some rescanning, but to avoid this there is a big cost in extra code with extra time-consuming steps.
          while (ptr < inLen)
          { if (DubArray[ptr] == Delimiter) break; 
            ptr++;
          }
          if (ptr == toPtr) break; // can't hope to find any more matches
          leftDelimPtr = ptr;
        }
      }
      if (lister.Count == 0) return new int[0];
      return lister.ToArray();
    }

/// <summary>
/// VOID. If VirtZero is a positive value, all of its values lying between +/- VirtZero (inclusive) will be reset to 0.
/// (If VirtZero is 0 or negative, DubArray is unaltered. Null or empty arrays unaltered also.)
/// </summary>
    public static void _Defluff(this double[] DubArray, double VirtZero)
    { double x;
      if (DubArray == null) return;
      for (int i=0; i < DubArray.Length; i++)
      { x = DubArray[i];
        if (x <= VirtZero && x >= -VirtZero) DubArray[i] = 0.0;
      } 
    }

/// <summary>
/// <para>If VirtZero is a positive value, all of its values lying between +/- VirtZero (inclusive) will be reset to 0.
/// (If VirtZero is 0 or negative, DubArray is unaltered. Null or empty arrays unaltered also.)</para>
/// <para>The final dummy argument is to distinguish this particular overload; any value will do.</para>
/// <para>RETURN: After defluffing, .X holds the number of positive values, .Y the number of zeroes, and .Z the number of neg. values.</para>
/// </summary>
    public static Trio _Defluff(this double[] DubArray, double VirtZero, int dummy)
    { double x;
      int posses = 0, zeros = 0, negs = 0;
      if (DubArray == null) return new Trio();
      for (int i=0; i < DubArray.Length; i++)
      { x = DubArray[i];
        if (x <= VirtZero && x >= -VirtZero) { DubArray[i] = 0.0;  zeros++; }
        else if (x < 0.0) negs++;  else posses++;
      } 
      return new Trio(posses, zeros, negs);
    }

    /// <summary>
    /// <para>The outcome will be an array of the required size (therefore an empty array if NewSize is 0, or NULL if NewSize is negative).
    /// If NewSize is less than the input array size, the input array is truncated (the last two args. being irrelevant).
    /// If NewSize is greater: If Recycle is TRUE, PadValue is ignored, and values of the input array are recycled.
    /// otherwise the array is padded with PadValue.</para>
    /// <para>The combination of DubArray empty/null and Recycle TRUE is indeterminate, and will result in an array of zeroes.</para>
    /// </summary>
    public static double[] _ConformSize(this double[] DubArray, int NewSize, bool Recycle, double PadValue)
    { if (NewSize < 0) return null;  else if (NewSize == 0) return new double[0];
      int inLen = DubArray._Length();
      if (inLen >= NewSize) return DubArray._Copy(0, NewSize);
      // In all cases below, NewSize is at least 1. inLen is < NewSize, so padding must occur; but inLen may be -1 or 0.
      List<double> lst = new List<double>(NewSize);
      if (Recycle)
      { if (inLen < 1) return new double[NewSize]; // All zeroes, as there was nothing to recycle.
  		  while (true)
        { if (lst.Count + inLen > NewSize) // then only part of the inLen array is required:
          { lst.AddRange(DubArray._Copy(0, NewSize - lst.Count));  break; }
          lst.AddRange(DubArray);
          if (lst.Count == NewSize) break;
        }
      }
      else // Pad the end, rather than recycle.
      { if (inLen > 0) lst.AddRange(DubArray);
        if (inLen == -1) inLen = 0; // for the sake of the start value of i below
        for (int i = inLen; i < NewSize; i++) lst.Add(PadValue);
      }
      return lst.ToArray();
    }

    /// <summary>
    /// <para>Dub._ToCharArray() -- returns a char. array of the same length as DubArray (or NULL, if DubArray is null or empty).</para>
    /// <para>Where a value cannot be translated to a Unicode value, it codes to a valid character according to the following scheme:</para>
    /// <para>(1) If 'Replacements' is NULL, all invalid character codes will code to '\u0000'.</para>
    /// <para>(2) If it has length 3, substitutions are as follows: Replacements[0] = replacement where the code rounded to a negative integer;
    ///  [1] = where it was > the highest allowed value for .NET / MONO type 'char', which is 0xFFF; [2] = where it is NaN.</para>
    /// <para>If 'Replacements' has a shorter length, shortfalls default to '\u0000'. (If longer, excess ignored.)</para>
    /// <para>Note that .NET and MONO's type 'char' encodes the 'Basic Multilingual Plane' of the Unicode system, which plane
    ///  covers the range 0 to FFFF with no exclusions. There are 16 more planes, the highest going up to 10FFFF.) Also note
    ///  that there are character ENCODINGS which convert these characters to indices of images in a font; the usual encoding for common
    ///  fonts is UTF8, which has a different numbering system (1 to 6 bytes; great majority of values in this range are invalid).</para>
    /// </summary>
    public static char[] _ToCharArray(this double[] DubArray, params char[] Replacements)
    { if (DubArray._NorE() ) return null;
      char[] replacer = new char[3]; // All its default starting values are '\u0000'.
      for (int i=0; i < Replacements.Length; i++) replacer[i] = Replacements[i];
      int arrlen = DubArray.Length;
      double x;  int n;
      char[] result = new char[arrlen];
      for (int i=0; i < arrlen; i++)
      { x = DubArray[i];
        if      (x <= -0.5)       result[i] = replacer[0];
        else if (x >= 65535.5)    result[i] = replacer[1];
        else if (double.IsNaN(x)) result[i] = replacer[2];
        else { n = Convert.ToInt32(x);  result[i] = Convert.ToChar(n); }
      }
        return result;
    }
    /// <summary>
    /// <para>Dub._ToCharString() -- returns a string of the same length as DubArray (or "", if DubArray is null or empty).</para>
    /// <para>Where a value cannot be translated to a Unicode value, it codes to a valid character according to the following scheme:</para>
    /// <para>(1) If 'Replacements' is NULL, all invalid character codes will code to '\u0000'.</para>
    /// <para>(2) If it has length 3, substitutions are as follows: Replacements[0] = replacement where the code rounded to a negative integer;
    ///  [1] = where it was > the highest allowed value for .NET / MONO type 'char', which is 0xFFF; [2] = where it is NaN.</para>
    /// <para>If 'Replacements' has a shorter length, shortfalls default to '\u0000'. (If longer, excess ignored.)</para>
    /// <para>Note that .NET and MONO's type 'char' encodes the 'Basic Multilingual Plane' of the Unicode system, which plane
    ///  covers the range 0 to FFFF with no exclusions. There are 16 more planes, the highest going up to 10FFFF.) Also note
    ///  that there are character ENCODINGS which convert these characters to indices of images in a font; the usual encoding for common
    ///  fonts is UTF8, which has a different numbering system (1 to 6 bytes; great majority of values in this range are invalid).</para>
    /// </summary>
    public static string _ToCharString(this double[] DubArray, params char[] Replacements)
    { if (DubArray._NorE() ) return "";
      return DubArray._ToCharArray(Replacements)._ToString();
    }

/// <summary>
/// <para>Conversion features: (a) all values &lt; Int32.MinValue or > Int32.MaxValue are set to the respective limit;
/// (b) all remaining values are rounded, using traditional rounding (i.e. decimal fraction exactly 0.5 --> round away from zero).</para>
/// <para>Empty or null input array returns a null output array.</para>
/// </summary>
    public static int[] _ToIntArray(this double[] DubArray)
    { if (DubArray._NorE() ) return null;
      int arrlen = DubArray.Length;
      double x, Top = (double) Int32.MaxValue, Bottom = (double) Int32.MinValue;
      int[] result = new int[arrlen];
      MidpointRounding moo = MidpointRounding.AwayFromZero;
      for (int i=0; i < arrlen; i++)
      { x = DubArray[i];
        if (x > Top) x = Top;
        else if (x < Bottom) x = Bottom;      
        result[i] = (int) Math.Round(x, moo);
      }
      return result;
    }

    public static int _NaNCount(this double[] DubArray)
    { int result = 0;
      if (DubArray == null) return result;
      for (int i=0; i < DubArray.Length; i++)
      { if (Double.IsNaN(DubArray[i])) result++; }
      return result;    
    }

/// <summary>
/// Returned int[] is NULL of JaggedMx is null; otherwise its length = the no. rows in JaggedMx, elements being row length
///  (or -1 for a null row).
/// </summary>
  public static int[] _JaggedLengths(this double[][] JaggedMx)
  { if (JaggedMx == null) return null;
    int noRows = JaggedMx.GetLength(0);
    int[] result = new int[noRows];
    for (int i=0; i < noRows; i++)
    { if (JaggedMx[i] == null) result[i] = -1;  else result[i] = JaggedMx[i].Length; }
    return result;
  }

    /// <summary>
    /// double[]._Chain(extent) -- returns a double[] made up by cycling through values in the input array, appending them to the output
    /// until the output array has length 'extent'. (If extent is less than the length of the input array, the effect will be of truncation.)
    /// No errors raised; if extent is less than 1, an empty array is returned.
    /// </summary>
    public static double[] _Chain(this double[] DubArray, int extent)
    { int inLen = DubArray._Length();  if (inLen == -1) return null; else if (inLen == 0) return new double[0];
      if (extent <= 0) return new double[0];
      List<double> result = new List<double>(extent);
      int times = extent / inLen, leftover = extent % inLen;
      for (int i=0; i < times; i++) result.AddRange(DubArray);
      if (leftover > 0) result.AddRange(DubArray._Copy(0, leftover));
      return result.ToArray();
    }


 // ++++ CHARACTER ARRAY EXTENSIONS ++++        //arrch

/// <summary>
/// <para>Converts a character array (or part thereof) to a string. Optional parameters: FromExtent[0] gives the starting point
/// (default 0) and FromExtent[1] the extent to convert (default - array length). Improper parameters do not raise an error;
/// oversized extent is adjusted down, but starting point too large or extent too small return "". Null or empty CharArr 
/// also return "".</para>
/// <para>Note that you usually don't need this method; "string ss = new string(CharArr, From, Extent)" is just fine. It is
/// only worth using this method if there is a risk that CharArr might be empty or null, or you don't want crashing with wrong pointers.</para>
/// </summary>
    public static string _ToString(this char[] CharArr, params int[] FromExtent)
    { if (CharArr == null) return String.Empty;
      Trio trio = JS.SegmentData(CharArr.Length, true, FromExtent); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr < 0) return  String.Empty;
      // In all remaining cases there is a nonzero extent of CharArr defined by the limits.
      return new string(CharArr, fromPtr, extent);
    }

/// <summary>
/// Converts a character array (or part thereof) to an integer. Optional parameters: FromExtent[0] gives the starting point
/// (default 0) and FromExtent[1] the extent to convert (default - array length). Improper parameters do not raise an error;
/// oversized extent is adjusted down, but starting point too large or extent too small return NULL. Null or empty CharArr 
/// also return NULL. (A nonnull but empty array is never returned.)
/// </summary>
    public static int[] _ToIntArray(this char[] CharArr, params int[] FromExtent)
    { if (CharArr == null) return null;
      Trio trio = JS.SegmentData(CharArr.Length, true, FromExtent); // sort out the pointers
      int fromptr = trio.X, extent = trio.Z;
      if (fromptr < 0) return  null;
      // In all remaining cases there is a nonzero extent of CharArr defined by the limits.
      int[] result = new int[extent];
      for (int i=0; i < extent; i++) result[i] = Convert.ToInt32(CharArr[i+fromptr]);
      return result;      
    }
/// <summary>
/// <para>Returns index of SoughtChar, if found, otherwise -1. (Null or empty input array returns -1 also.)</para>
/// <para>If "FromTo provided, FromTo[0] less than 0 is corrected to 0, FromTo[1] beyond end of array is corrected to end.</para>
/// <para>No errors crash the code. Simply GIGO applies.</para>
/// </summary>
    public static int _Find(this char[] CharArr, char SoughtChar, params int[] FromTo)
    { if (CharArr == null) return -1;
      int startat = 0, endat = CharArr.Length-1; 
      if (FromTo.Length > 0)
      { startat = FromTo[0];
        if (startat < 0) startat = 0;
        if (FromTo.Length > 1)
        { endat = Math.Min(endat, FromTo[1]);
        }
      }
      int result = -1;
      for (int i = startat; i <= endat; i++)
      { if (CharArr[i] == SoughtChar)
        { result = i;  break; }
      }
      return result;
    }
/// <summary>
/// Returns a copy of CharArr; the copy does not have to be pre-dimensioned, as it does with the void method ".CopyTo(.)".
/// Copies null and empty arrays as well. (Only if CharArr is NULL will null ever be returned.)
/// </summary>
    public static char[] _Copy(this char[] CharArr, params int[] FromExtent)
    { if (CharArr == null) return null;
      char[] result = null;
      int inarrlen = CharArr.Length;   if (inarrlen == 0) return new char[0];
      if (FromExtent.Length == 0) // The form below with params. takes around 8% longer (using exact params. for whole string).
      { result = new char[inarrlen];
        if (inarrlen > 0) CharArr.CopyTo(result, 0);
        return result;
      }
    // FromExtent supplied:
      Trio trio = JS.SegmentData(inarrlen, true, FromExtent); // sort out the pointers
      int fromptr = trio.X, extent = trio.Z;
      if (fromptr < 0) return  new char[0];
      // In all remaining cases there is a nonzero extent of CharArr defined by the limits.
      result = new char[extent];
      Array.Copy(CharArr, fromptr, result, 0, extent);
      return result;
    }
    /// <summary>
    /// CharArr._CountPlus(chr [, P [, Q ] ]) detects no. instances of 'chr' between CharArr[P] and CharArr[Q] inclusive.
    /// RETURNED: .X = no. instances; .Y, .Z point to first and last instances (-1, if none). No errors raised; faulty
    /// arguments adjusted for as best as possible. Null or empty base array returns .X = 0.
    /// </summary>
    public static Trio _CountPlus(this char[] Chars, char target, params int[] FromTo)
    { Trio result = new Trio(0, -1, -1);
      if (Chars == null) return result;
      Trio trio = JS.SegmentData(Chars.Length, false, FromTo); // sort out the pointers
      int fromPtr = trio.X, toPtr = trio.Y;
      if (fromPtr < 0) return  result;
      // In all remaining cases there is a nonzero extent of Chars defined by the limits.
      int firstfind = -1, lastfind = -1;
      for (int i = fromPtr; i <= toPtr; i++)
      { if (Chars[i] == target) { result.X++;  lastfind = i;  if (firstfind == -1) firstfind = i; } }
      result.Y = firstfind;  result.Z = lastfind;
      return result;
    }

 // ++++ STRING ARRAY EXTENSIONS ++++       //arrstr

/// <summary>
/// <para>Returns a copy of the string array; the copy does not have to be pre-dimensioned, as it does with the void method ".CopyTo(.)".
/// Copies null and empty arrays as well.</para>
/// <para>Adjusts out-of-range pointers to array extremes; crossed pointers return empty array.</para>
/// </summary>
    public static string[] _Copy(this string[] InArray, params int[] FromExtent)
    { if (InArray == null) return null;
      string[] result = null;
      int inarrlen = InArray.Length;
      if (FromExtent.Length == 0)
      { result = new string[inarrlen];
        if (inarrlen > 0) InArray.CopyTo(result, 0);
        return result;
      }
    // FromExtent supplied:
      int fromptr = FromExtent[0];
      if (fromptr < 0) fromptr = 0;
      else if (fromptr >= inarrlen) return new string[0];
      int extent = inarrlen - fromptr;
      if (FromExtent.Length > 1)
      { int n = FromExtent[1]; if (n < extent) extent = n; } // effectively reduces an oversized FromExtent[1].
      if (extent <= 0) return new string[0];
      // Must be a valid nonempty extent:
      result = new string[extent];
      Array.Copy(InArray, fromptr, result, 0, extent);
      return result;
    }


    /// <summary>
    /// StrArr._Purge(chr1 [, chr2 [, chr3.. ] ] ) -- returns StrArr with all instances of character(s) chr1, chr2, chr3,... removed
    /// from its elements. StrArr._Purge() removes all characters from U+0000 to U+0032 inclusive. 
    /// (For white spaces, use 'JS.WhiteSpaces' as argument.) Returns null if StrArr is null or empty.
    /// </summary>
    public static string[] _Purge(this string[] inArr, params char[] Unwanted)
    { if (inArr == null || inArr.Length == 0) return null;
      int inlen = inArr.Length;
      string[] result = new string[inlen];
      for (int i=0; i < inlen; i++) result[i] = inArr[i]._Purge(Unwanted);
      return result;
    }
    /// <summary>
    /// Applies system method .Trim() to each element of the string array. (Null or empty array returns null; but null strings within
    /// a nonempty array are accepted, being replaced by the empty string.)
    /// </summary>
    public static string[] _Trim(this string[] inArr) // uses the private utility method below.
    { if (inArr == null || inArr.Length == 0) return null;
      return Trimmery(ref inArr, 'B');
    }
    /// Applies system method .TrimStart() to each element of the string array. (Null or empty array returns null; but null strings within
    /// a nonempty array are accepted, being replaced by the empty string.)
    /// </summary>
    public static string[] _TrimStart(this string[] inArr) // uses the private utility method below.
    { if (inArr == null || inArr.Length == 0) return null;
      return Trimmery(ref inArr, 'S');
    }
    /// Applies system method .TrimEnd() to each element of the string array. (Null or empty array returns null; but null strings within
    /// a nonempty array are accepted, being replaced by the empty string.)
    /// </summary>
    public static string[] _TrimEnd(this string[] inArr) // uses the private utility method below.
    { if (inArr == null || inArr.Length == 0) return null;
      return Trimmery(ref inArr, 'E');
    }
   // Utility function, serving the next three extension methods. 'Where' is 'S'(start), 'E'(end), 'B'(both). No error checks on args. 
   // inArr must be nonempty; however null element strings are allowed and will be converted to the empty string.
    private static string[] Trimmery(ref string[] inArr, char Where)
    { int inlen = inArr.Length;
      string[] result = new string[inlen];
      for (int i=0; i < inlen; i++)
      { if (String.IsNullOrEmpty(inArr[i])) result[i] = String.Empty;
        else
        { if      (Where == 'S') result[i] = inArr[i].TrimStart();
          else if (Where == 'E') result[i] = inArr[i].TrimEnd();
          else result[i] = inArr[i].Trim();    
        }
      }
      return result;
    }
   /// <summary>
   /// <para>Returns the index of the find, if 'target' found; otherwise -1. Case must match. No trimming occurs.</para>
   /// <para>'target' may be null or empty; in either case the inArr element must exactly match 'target'.</para>
   /// <para>No errors raised in any situation.</para>
   /// <para>A lot more options are offered by the overload.</para>
   /// </summary>
    public static int _Find(this string[] inArr, string target)
    { return inArr._Find(target, 0, -1).X; }
   /// <summary>
   /// <para>Returns a Duo object: .X = index of the find, if 'target' found; otherwise -1. .Y only used where substring search required,
   /// in which case it is the position in the inArr element of the find. (Unused .Y = 0.)</para>
   /// <para>POINTERS: Only automatic adjustments: intEndPtr &lt;0 or beyond end of inArr is adjusted to end of inArr.</para>
   /// <para>FLAGS: If absent, all default to 0.</para>
   /// <para>* flag[0]: 1 = Ignore letter case. (0 or any other value = case-sensitive search.)</para>
   /// <para>* flag[1]: 1 = Trim target and inArr element before search; 2 = Purge target and element of all white spaces first.  Any other value: no trim or purge.</para>
   /// <para>* flag[2]: 1 or 2 = If target is only a substring of the inArr entry, still record a find. 1 = But the inArr entry must
   /// begin with the substring; 2 = substring can be anywhere in the entry. (0 or any other value: none of these.)</para>
   /// <para>'target' may be null or empty; in either case the inArr element must exactly match 'target', AND all flags will be ignored.</para>
   /// <para>No errors raised in any situation. Null or empty inArr simply returns a no-find.</para>
   /// </summary>
    public static Duo _Find(this string[] inArr, string target, int startPtr, int endPtr, params int[] flags)
    { Duo result = new Duo(-1, 0);
      if (inArr._NorE() ) return result;
      int inLen = inArr.Length, flagpole = flags.Length, targetLen;
      if (target == null) targetLen = -1; else targetLen = target.Length;
      if (endPtr < 0 || endPtr >= inLen) endPtr = inLen-1;
      bool ignoreCase = (flagpole > 0 && flags[0] == 1);
      bool cullString = (flagpole > 1 && flags[1] == 2);
      bool trimString = (flagpole > 1 && flags[1] == 1); // ignored, if cullTarget TRUE.
      bool findSubStr = (flagpole > 2 && (flags[2] == 1 || flags[2] == 2) );
      bool isAnywhere = (findSubStr && flags[2] == 2); // ignored, if findSubStr FALSE.
     // THE SEARCH:
     // Any find --> return from the middle of its loop. So if all loops passed, it means there was no find.
      if (targetLen == -1) // NULL string being sought:
      { for (int i = startPtr; i <= endPtr; i++) { if (inArr[i] == null) { result.X = i; return result; } } }
      else if (targetLen == 0) // EMPTY string being sought:
      { for (int i = startPtr; i <= endPtr; i++) { if (inArr[i] == "") { result.X = i; return result; } } }
      else // target has at least one character:      
      { if (ignoreCase) target = target.ToUpper(); //####### check that it is not returned to calling code as such.
        if (cullString) target = target._Purge(JS.WhiteSpaces);
        else if (trimString) target = target.Trim();
        for (int i = startPtr; i <= endPtr; i++)
        { string arrStr = inArr[i];  if (String.IsNullOrEmpty(arrStr)) continue; // couldn't possibly be a find for this nonempty target. 
          if (ignoreCase) arrStr = arrStr.ToUpper();
          if (cullString) arrStr = arrStr._Purge(JS.WhiteSpaces);
          else if (trimString) arrStr = arrStr.Trim();
          if (findSubStr)
          { int n = arrStr.IndexOf(target);
            if (n > -1)
            if (n == 0 || isAnywhere) { result.X = i;  result.Y = n;  return result; } 
          }
          else  // a full string match required:
          { if (target == arrStr) { result.X = i; return result; } }
        }
      } 
      // If got here, then search failed:
      return result; // which is (-1, 0).
    }
   /// <summary>
   /// <para>If no finds, returns NULL. If finds, returns a Duo[] object whose length = no. of finds. For each, .X = index in inArr
   /// of the find and .Y = 0 unless a substring search is required, when it is position in the inArr element of the find.
   /// (Note that if the trimming or purging options are chosen below, .Y is the position in the element of inArr AFTER trimming or purging.)</para>
   /// <para>FLAGS: If absent, all default to 0.</para>
   /// <para>* flag[0]: 1 = Ignore letter case. (0 or any other value = case-sensitive search.)</para>
   /// <para>* flag[1]: 1 = Trim target and inArr element before search; 2 = Purge target and element of all white spaces first.  Any other value: no trim or purge.</para>
   /// <para>* flag[2]: 1 or 2 = If target is only a substring of the inArr entry, still record a find. 1 = But the inArr entry must
   /// begin with the substring; 2 = substring can be anywhere in the entry. (0 or any other value: none of these.)</para>
   /// <para>'target' may be null or empty; in either case the inArr element must exactly match 'target', AND all flags will be ignored.</para>
   /// <para>No errors raised in any situation.</para>
   /// </summary>
    public static Duo[] _FindAll(this string[] inArr, string target, params int[] flags)
    { List <Duo> output = new List<Duo>();
      int startPtr = 0;
      while (true)
      { Duo euro = inArr._Find(target, startPtr, -1, flags);
        if (euro.X == -1) break;
        output.Add(euro);
        startPtr = euro.X + 1;
      }  
      if (output.Count == 0) return null;
      return output.ToArray();
    }


/// <summary>
/// Returned int[] is NULL of JaggedMx is null; otherwise its length = the no. rows in JaggedMx, elements being row length
///  (or -1 for a null row).
/// </summary>
  public static int[] _JaggedLengths(this string[][] JaggedMx)
  { if (JaggedMx == null) return null;
    int noRows = JaggedMx.GetLength(0);
    int[] result = new int[noRows];
    for (int i=0; i < noRows; i++)
    { if (JaggedMx[i] == null) result[i] = -1;  else result[i] = JaggedMx[i].Length; }
    return result;
  }


 // ++++ STRINGBUILDER EXTENSIONS ++++       //stringbuilder

   /// <summary>
   /// <para>The extent itself is not checked; but if on either side of it there are no word-negating chars., then TRUE
   ///  is returned. FALSE is returned if (a) word-negating chars. occur at either end, or (b) if args. faulty.</para>
   /// <para>A string extension with the same name exists.</para>
   /// </summary>
    public static bool _IsWholeWord(this StringBuilder sb, int startPtr, int extent, ref string WordNegators)
    { int sbLen = sb.Length; if (extent < 1 || startPtr < 0 || startPtr + extent > sbLen) return false;
      bool beginsOk = false, endsOk = false;
      beginsOk = (startPtr == 0 || WordNegators.IndexOf(sb[startPtr-1]) == -1);
      if (beginsOk)
      { endsOk = (startPtr + extent == sbLen || WordNegators.IndexOf(sb[startPtr + extent]) == -1); }
      return beginsOk && endsOk;
    }

   /// <summary>
   /// <para>Return the character in the StringBuilder with either the highest unicode (HighestLowest = 'H') or the lowest ('L'),
   ///  over the stated range</para>
   /// <para>In the case of error (the StringBuilder is empty, HighestLowest is improper, or impossible limits after correction),
   ///  the return is unicode 0, and the OUT boolean is set to FALSE.</para>
   /// </summary>
    public static char _ExtremeChar(this StringBuilder sb, char HighestLowest, out bool Success, params int[] FromExtent)
    { Success = true;
      Trio trio = JS.SegmentData(sb.Length, true, FromExtent); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr == -1) { Success = false;  return '\u0000'; } // includes empty sb, crossed pointers, zero extent.
      char ch;
      if (HighestLowest == 'H')
      { ch = char.MinValue; // which is '\u0000'
        for (int i = fromPtr;  i < fromPtr + extent; i++) if (sb[i] > ch) ch = sb[i];
      }
      else if (HighestLowest == 'L')
      { ch = char.MaxValue; // which is '\uFFFF'
        for (int i = fromPtr;  i < fromPtr + extent; i++) if (sb[i] < ch) ch = sb[i];
      }
      else { Success = false;  return '\u0000'; }
      return ch;
    }

   /// <summary>
   /// <para>Returns the absolute index of the first character within the stated range in the StringBuilder which has unicode
   ///  either at or beyond that of TestChar. 'Beyond' means 'higher than' if HighestLowest = 'H' or 'lower than' if HighestLowest = 'L'.</para>
   /// <para>If no character reaches TestChar, -1 is returned, but NoErrors is TRUE.</para>
   /// <para>In the case of error (the StringBuilder is empty, HighestLowest is improper, or impossible limits after correction),
   ///  the return is again -1, but the OUT boolean is set to FALSE.</para>
   /// </summary>
    public static int _FirstAtOrBeyond(this StringBuilder sb, char HighestLowest, char TestChar, out bool NoErrors, params int[] FromExtent)
    { NoErrors = true;
      Trio trio = JS.SegmentData(sb.Length, true, FromExtent); // sort out the pointers
      int fromPtr = trio.X, extent = trio.Z;
      if (fromPtr == -1) { NoErrors = false;  return -1; } // includes empty sb, crossed pointers, zero extent.
      if (HighestLowest == 'H')
      { for (int i = fromPtr;  i < fromPtr + extent; i++)
        { if (sb[i] >= TestChar) return fromPtr + i; }
      }
      else if (HighestLowest == 'L')
      { for (int i = fromPtr;  i < fromPtr + extent; i++)
        { if (sb[i] <= TestChar) return fromPtr + i; }
      }
      else NoErrors = false; // improper HighestLowest value
      return -1; // includes where TestChar never reached (and NoErrors remains true).
    }



 // ++++ BOOL ARRAY EXTENSIONS ++++       //arrbool

/// <summary>
/// Returns a copy of CharArr; the copy does not have to be pre-dimensioned, as it does with the void method ".CopyTo(.)".
/// Copies null and empty arrays as well. (Only if CharArr is NULL will null ever be returned.)
/// </summary>
    public static bool[] _Copy(this bool[] BoolArr, params int[] FromExtent)
    { if (BoolArr == null) return null;
      bool[] result = null;
      int inarrlen = BoolArr.Length;   if (inarrlen == 0) return new bool[0];
      if (FromExtent.Length == 0) // The form below with params. takes longer (using exact params. for whole string).
      { result = new bool[inarrlen];
        if (inarrlen > 0) BoolArr.CopyTo(result, 0);
        return result;
      }
    // FromExtent supplied:
      Trio trio = JS.SegmentData(inarrlen, true, FromExtent); // sort out the pointers
      int fromptr = trio.X, extent = trio.Z;
      if (fromptr < 0) return  new bool[0];
      // In all remaining cases there is a nonzero extent of BoolArr defined by the limits.
      result = new bool[extent];
      Array.Copy(BoolArr, fromptr, result, 0, extent);
      return result;
    }
/// <summary>
/// <para>If StrArray is nonempty and parses properly, an int[] of the same length is returned, and Outcome fields are: .B true,
///  .S empty, .I = -1. (,X is unused by this method.)</para>
/// <para>If StrArray is null or empty, or if any item in the string array fails to parse, NULL is returned, and
///  Outcome.B is false. If nonempty and parsing failed, .S holds the offending string, and Outcome.I its index.</para>
/// <para>If StrArray is null or empty, .S is empty and .I is 0.</para>
/// </summary>
    public static int[] _ParseIntArray(this string[] StrArr, out Quad Outcome)
    { Outcome = new Quad(false);
      if (StrArr == null) return null;
      int n, len = StrArr.Length;  if (len == 0) return null;
      int[] result = new int[len];
      bool success;
      for (int i=0; i < len; i++)
      { n = StrArr[i]._ParseInt(out success);
        if (!success) { Outcome = new Quad(i, 0.0, false, StrArr[i]);  return null; }
        result[i] = n;
      }
      Outcome.B  = true;  Outcome.I = -1;  return result;
    }

/// <summary>
/// <para>If StrArray is nonempty and parses properly, an int[] of the same length is returned, and Outcome fields are: .B true,
///  .S empty, .I = -1. (,X is unused by this method.)</para>
/// <para>If StrArray is null or empty, or if any item in the string array fails to parse, NULL is returned, and
///  Outcome.B is false. If nonempty and parsing failed, .S holds the offending string, and Outcome.I its index.</para>
/// <para>If StrArray is null or empty, .S is empty and .I is 0.</para>
/// </summary>
    public static double[] _ParseDubArray(this string[] StrArr, out Quad Outcome)
    { Outcome = new Quad(false);
      if (StrArr == null) return null;
      int len = StrArr.Length;  if (len == 0) return null;
      double[] result = new double[len];
      bool success;  double x;
      for (int i=0; i < len; i++)
      { x = StrArr[i]._ParseDouble(out success);
        if (!success) { Outcome = new Quad(i, 0.0, false, StrArr[i]);  return null; }
        result[i] = x;
      }
      Outcome.B  = true;  Outcome.I = -1;  return result;
    }


    /// <summary>
    ///  Returns a copy of the number of instances of InstancesOfThis. If BoolArr is null, returns -1; otherwise, >= 0.
    /// </summary>
    public static int _Count(this bool[] BoolArr, bool InstancesOfThis)
    { if (BoolArr == null) return -1;
      int sz = BoolArr.Length, result = 0;
      for (int i=0; i < sz; i++) { if (BoolArr[i] == InstancesOfThis) result++; }
      return result;
    }    
     

 // ++++ DATETIME EXTENSIONS ++++       //datetime

/// <summary>
/// 'Model' MUST represent how you want the EXACT modelling date (Sunday June 8th., 2003) to be presented: e.g. "Sun 8 Jun, 03" or
/// "Jun 8" or "08/06/03". Allowed variations of the components: 
/// <para>(1)-- 'Sunday' - can also be 'Su' or 'Sun'; upper or lower case of each is also allowed (e.g. "su", "Su", "SU", but not "sU").</para>
/// <para>(2)-- 'June' - can also be 'Jun' or '6' or '06' (the last enforces a leading zero for months &lt; 10). Case mixes allowed.</para>
/// <para>(3)-- '8th' - can also be '8' or '08' (the last enforces a leading zero for dates &lt; 10).</para>
/// <para>(4)-- '2003' - can also be '03'.</para>
/// <para>Between the components can go any nonambiguous characters - spaces, standard punctuation marks (but not letters or numerals). They
/// will be reproduced exactly as is.</para>
/// <para>USE OF 'GUIDE': This Quad[5], if supplied, speeds the method up considerably. It is generated here if and only if: (1) it enters
/// as null; or (2) it does not have length 5; or (3) Quad[4].S is not exactly equal to Model. (It would not be intended that the user
/// would build Guide externally; the method itself should be used for this.)</para>
/// <para>ERRORS NOT RAISED: any error invokes a default format or in some other way produces a stuffed-up date.</para>
/// </summary>
  public static string _ToModelledString(this DateTime DT, string Model, ref Quad[] Guide) // The makeup of 'Guide' is explained below.
  { 
    if (Guide == null || Guide.Length != 5 || Guide[4].S != Model) // then we build a new Guide:
    { string tt, ModelLo = Model.ToLower(); 
    // PREPARE THE INTERNAL TETRO OBJECT THAT WILL GIVE COMPONENT INFORMATION:
      Tetro[] Order = new Tetro[4]; // X1: 0=dayname, 1=dayno, 2=month, 3=year; X2, X3: start, end of date component; X4 = layout code.
      Order[0].X1 = 0; // X1: code for 'dayname' (as Order will later be sorted); X2, X3: start, end of date component; X4 = layout code.
      // Search for Day Name in Model:
      int n = Order[0].X2 = ModelLo.IndexOf('s');
      if (n >= 0)
      { if (ModelLo._Extent(n, 6) == "sunday")  { Order[0].X3 = n + 5;  Order[0].X4 = 0; }
        else if (ModelLo._Extent(n, 3) == "sun"){ Order[0].X3 = n + 2;  Order[0].X4 = 10; }
        else { Order[0].X3 = n + 1;  Order[0].X4 = 20; }
        // Add on a letter-case code:
        tt = Model._Extent(Order[0].X2, 2);
        if (tt == "su") Order[0].X4 += 1; else if (tt == "SU") Order[0].X4 += 2; // "Su": no add-on. (Also the default for error.)
      }
      // Search for Day Number in Model:
      Order[1].X1 = 1;
      n = Order[1].X2 = ModelLo.IndexOf('8');
      if (n >= 0)
      { if (n > 0 && ModelLo[n-1] == '0') { Order[1].X2--; Order[1].X3 = n;  Order[1].X4 = 10; }
        else if (ModelLo._Extent(n+1,2) == "th") { Order[1].X3 = n+2;  Order[1].X4 = 20; }
        else { Order[1].X3 = n;  Order[1].X4 = 0; }
      }
      // Search for Month Name or No. in Model:
      Order[2].X1 = 2;  
      n = Order[2].X2 = ModelLo.IndexOf('j');
      if (n >= 0) // Month Name found:
      { if (ModelLo._Extent(n, 4) == "june")  { Order[2].X3 = n + 3;  Order[2].X4 = 0; }
        else if (ModelLo._Extent(n, 3) == "jun") { Order[2].X3 = n + 2;  Order[2].X4 = 10; }
        else { Order[2].X3 = n + 1;  Order[2].X4 = 20; } // "ju" and default for error
        // Add on a letter-case code:
        tt = Model._Extent(Order[2].X2, 2);
        if (tt == "ju") Order[2].X4 += 1; else if (tt == "JU") Order[2].X4 += 2; // "Ju": no add-on. (Also the default for error.)
      }
      else // look for a Month No.:
      { n = Order[2].X2 = ModelLo.IndexOf('6');
        if (n >= 0)
        { if (n > 0 && ModelLo[n-1] == '0') { Order[2].X2--; Order[2].X3 = n;  Order[2].X4 = 30; }
          else { Order[2].X3 = n;  Order[2].X4 = 40; }
        }
      }
      // Search for Year Number in Model:
      Order[3].X1 = 3;
      n = Order[3].X2 = ModelLo.IndexOf('3');
      if (n >= 0)
      { if (n >= 3 && Model._Extent(n-3, 4) == "2003") { Order[3].X2 = n-3; Order[3].X3 = n;  Order[3].X4 = 10; }
        else { Order[3].X2 = n-1;  Order[3].X3 = n;  Order[3].X4 = 0; } // "03" and default for error
      }
     // Now sort this, in order of Order[n].X2 - i.e. in order of appearance in Model:
      int[] Sortie = new int[Order.Length]; // the indices of Order, in order of appearance.
      int[] Key = new int[Order.Length]; 
      for (int i=0; i < Order.Length; i++) { Sortie[i] = Order[i].X1;  Key[i] = Order[i].X2; }
      JS.SortByKey(Sortie, Key, true);

    // PREPARE THE EXTERNAL QUAD OBJECT THAT WILL DICTATE DATE STRING FORMAT:
      Guide = new Quad[Order.Length+1];
      int lastEnd = -1; // will point to the end of the last element in Model.
      for (int i=0; i < Order.Length; i++)
      { Guide[i] = new Quad(true);
        n = Sortie[i]; // the element of Order to use
        if (Order[n].X2 == -1) { Guide[i].B = false; continue; } // date element that is not required.
        Guide[i].X = (double) n; // the date element (code: 0 = day name, 1 = day no., 2 = month, 3 = year)
        Guide[i].I = Order[n].X4; // formatting code fot that date element
        if (lastEnd != -1) 
        { Guide[i].S = Model._FromTo(lastEnd+1, Order[n].X2-1); } // text preceding this data element 
        lastEnd = Order[n].X3;
      }  
      Guide[Order.Length].S = Model; // In future, Guide will only be used if the arg. Model exactly matches this string.
    }

  // WHAT THE HEY IS THIS 'GUIDE'?
  // The first four elements represent (NOT nec. in this order): day name; day no.; month name or no.; year. Their order is that dictated
  //  by model. The final element is a copy of the input Model, so that whenever this method is called with a filled Quad object, the 
  //  method knows to accept and use the Quad object as long as [4] is an exact copy of Model.
  // Since the order of the first four Quad[] varies, I will label elements as [<function>] rather than as [0], [1]...
  // FOR ALL of Quad[0 to 3] elements, .B = TRUE if this element is to be present, and .X is its code (0 for day name, 1 for day no.,
  //   2 for month, 3 for year); also, .S is always the fixed (i.e. date-independent) text preceding this element but following the previous
  //   element (or the start of the string, if this is the first element). 
  // This leaves only the .I field to have different meanings for different Quad objects. The meanings are as follows:
  // [dayname] -- .I = sum of {0 = full name; 10 = 1st. 3 letters; 20 = 1st. 2 letters} PLUS {case: 0 = "Xxx.."; 1 = "xxx.."; 2 = "XXX.."}.
  // [dayno]   -- .I = 0 (no. accepted as is, e.g. '8'), 10 (1-digit no. padded with zero: '08'), 20 (ordinal 'th' form: '8th').
  // [month]   -- .I < 30 where month is named, >= 30 where month number used instead. If named: .I is exactly the same as for dayname.
  //                (see above). If numbered month: 30 = 1-digit no. padded with zero: '08'; 40 = use as is: '8'.
  // [year]    -- .I = 0 (2-digit form: '03') or 10 (4-digit form: '2003').

  // ASSUME A VALID 'GUIDE': .B = date element present; .X = its type code; .I = format category; .S = fixed text to PRECEDE this entry.
    string ss="", result = "";
    int q=0;
    for (int i=0; i < Guide.Length; i++)
    { if (!Guide[i].B) continue;
      result += Guide[i].S;
      if (Guide[i].X == 0.0) // day name:      
      { ss = DT.DayOfWeek.ToString(); 
        if (Guide[i].I >= 20) ss = ss._Extent(0, 2);  else if (Guide[i].I >= 10) ss = ss._Extent(0, 3);
        q = Guide[i].I % 10;
        if (q == 1) ss = ss.ToLower();  else if (q == 2) ss = ss.ToUpper(); // if 0, accept the .Net default: first letter only is capital.
        result += ss; 
      }         
      else if (Guide[i].X == 1.0) // day no.:
      { q = DT.Day;
        if (Guide[i].I == 20) result += q._Ordinal(false, true); // 23 --> "23rd"
        else 
        { ss = q.ToString();  if (Guide[i].I == 10 && q < 10) ss = '0' + ss;
          result += ss;
        }
      }
      else if (Guide[i].X == 2.0) // month name or no.:
      { if (Guide[i].I >= 30)
        { ss = DT.Month.ToString();  if (Guide[i].I == 30 && DT.Month < 10) ss = '0' + ss; }
        else
        { ss = JS.MonthStr._Extent(1 + 10*(DT.Month-1), 9); // E.g. for DT.month = 3, "March////" will be returned. 
          ss = ss._Purge(new char[] {'/'});
          if (Guide[i].I >= 20) ss = ss._Extent(0, 2);  else if (Guide[i].I >= 10) ss = ss._Extent(0, 3);
          q = Guide[i].I % 10;
          if (q == 1) ss = ss.ToLower();  else if (q == 2) ss = ss.ToUpper(); // if 0, accept the .Net default: first letter only is capital.
        }
        result += ss; 
      }         
      else // year:
      { q = DT.Year;  ss = q.ToString();
        if (q < 1000) { ss = ss._PadTo(4, '0', false); } // important e.g. where calling code uses DateTime.MinValue as a sort of null date.
                                                         // DateTime.MaxValue is still only 4 digits - 9999 - so no problem there.
        if (Guide[i].I == 0) ss = ss._Extent(2); // the last two digits
        result += ss;
      }    
    }
    return result;
  }

 // ++++ COLOUR EXTENSIONS ++++       //colour  //clr

  /// <summary>
  /// Returns string 'Model' with every instance of "%R" (if present) replaced by the red byte (as a string),
  ///  and similarly for "%G" and "%B". (The letters after '%' must be capitals.) If you want hex digits,
  ///  instead use "xR", "xG", "xR",
  /// </summary>
  public static string _ToString(this Gdk.Color colour, string Model)
  { if (String.IsNullOrEmpty(Model)) return colour.ToString(); // Settle for MONO's default 'ToString()' handler.
    // Sadly, colour.Red (et al) is a ushort (range 0 to 65535); we have to cut it back to a byte, range 0 to 255.
    byte bred   = (byte) (colour.Red / 256);
    byte bgreen = (byte) (colour.Green / 256);
    byte bblue  = (byte) (colour.Blue / 256);
    string result = Model.Replace("xR", bred.ToString("X2"));
    result = result.Replace("xG", bgreen.ToString("X2"));
    result = result.Replace("xB", bblue.ToString("X2"));
    result = result.Replace("%R", bred.ToString());
    result = result.Replace("%G", bgreen.ToString());
    result = result.Replace("%B", bblue.ToString());
    return result;
  }
  /// <summary>
  /// Returns a byte array of length 3, [0] = red, [1] = green, [2] = blue. (The Gdk.Color properties 'Red', 'Green'
  /// and 'Blue' return 4-byte 'ushort' values, which is often not suitable.)
  /// </summary>
  public static byte[] _ToByteArray(this Gdk.Color colour)
  { return new byte[] { (byte) (colour.Red / 256), (byte) (colour.Green / 256), (byte) (colour.Blue / 256) };
  }
  /// <summary>
  /// Returns a string array of hex values (range 0x00 to 0xFF) of length 3, [0] = red, [1] = green, [2] = blue.
  /// (The Gdk.Color properties 'Red', 'Green' and 'Blue' return 4-byte 'ushort' values, which is often not suitable.)
  /// </summary>
  public static string[] _ToStringArray(this Gdk.Color colour)
  { byte[] boodle = colour._ToByteArray();
    string[] result = new string[3];
    result[0] = boodle[0].ToString("X2");  result[1] = boodle[1].ToString("X2");  result[2] = boodle[2].ToString("X2");
    return result;
  }
  /// <summary>
  /// Returns true if the BYTE VERSION of the R,G and B components of the colours are equal. The byte is taken as
  /// the upper two of the four hex digits of Color.Red, .Green and .Blue, without any rounding. As this is Gdk.Color,
  /// there is no 'alpha' or brightness component to the colour.
  /// </summary>
  public static bool _Equals(this Gdk.Color ThisColour, Gdk.Color ThatColour)
  { byte red1   = (byte) (ThisColour.Red / 256),   green1 = (byte) (ThisColour.Green / 256),   blue1  = (byte) (ThisColour.Blue / 256);
    byte red2   = (byte) (ThatColour.Red / 256),   green2 = (byte) (ThatColour.Green / 256),   blue2  = (byte) (ThatColour.Blue / 256);
    return (red1 == red2  &&  green1 == green2  &&  blue1 == blue2);
  }
  /// <summary>
  /// Returns true if the R,G and B components of the colours are equal. The 'alpha' or brightness component of the colours is IGNORED.
  /// </summary>
  public static bool _EqualsMS(this System.Drawing.Color ThisColour, System.Drawing.Color ThatColour)
  { return (ThisColour.R == ThatColour.R  &&  ThisColour.G == ThatColour.G  &&  ThisColour.B == ThatColour.B);
  }
  /// <summary>
  /// Converts a Gdk.Color into a System.Drawing.Color. If 'Intensity' is missing, is not one of the allowed types (byte, integer or
  /// double), or comes to a value (rounded if double) not in the range 0 to 255, then the intensity byte 'A' will be set to the max. value of 255.
  /// </summary>
  public static System.Drawing.Color _ToSDColor(this Gdk.Color ThisColour, params object[] Intensity)
  { int A = 255;
    if (Intensity.Length > 0)
    { if      (Intensity[0] is byte) A = (int) ( (byte) Intensity[0] );
      else if (Intensity[0] is int) { A = (int) Intensity[0]; if (A < 0 || A > 255) A = 255; }
      else if (Intensity[0] is double)
      { double x = Math.Round( (double) Intensity[0] ); if (x < 0.0 || x > 255.0) x = 255.0;  A = Convert.ToInt32(x); }
    }
    int R = (int) (ThisColour.Red / 256),   G = (int) (ThisColour.Green / 256),   B  = (int) (ThisColour.Blue / 256);
    return System.Drawing.Color.FromArgb(A, R, G, B);
  }
  /// <summary>
  /// Converts a System.Drawing.Color into a Gdk.Color, the R,G and B values being transferred as bytes (0 to 255). The intensity
  /// factor ('A') in the System.Drawing.Color is ignored.
  /// </summary>
  public static Gdk.Color _ToGdkColor(this System.Drawing.Color ThisColour)
  { return new Gdk.Color(ThisColour.R, ThisColour.G, ThisColour.B); }


/* AWAITING ADAPTATION TO GTK
  /// <summary>
  ///<para>ARGB: Consists of any or all of these case-insensitive chars: 'A' (alpha value), 'R'(ed value), 'G'(reen), 'B'(lue),
  /// 'S' (a single Int32 or hex for the whole colour). If HexPrefix is absent or empty, all of these will be as decimal numbers; 
  /// otherwise as hex strings (digits AARRGGBB), prefixed by HexPrefix. The default for null or empty ARGB is "ARGB". The order of 
  /// appearance in the output will be the order in ARGB. Each number will be prefixed with its label; e.g. 
  /// "A = 23; R = 10; G = 255; B = 34; S = ..."</para> 
  ///<para>If HexPrefix has a second element = "6", and if a hex version of the colour was requested (i.e. HexPrefix set and ARGB contains 'S'),
  ///then the hex output will consist only of 6 digits (RRGGBB), the alpha digits being omitted.</para>
  ///<para>If HexPrefix has a third element = any English letter, e.g. "A", and if HexPrefix is TRUE, then the case of that letter will
  ///be the case of any hex digits A..F in the output. (The default is capitals.) If setting this but not element [1], set [1] to e.g. "".
  ///then the hex output will consist only of 6 digits (RRGGBB), the alpha digits being omitted.</para>
  ///<para>Improper chars. in ARGB are simply ignored.</para>
  ///<para>---------</para>
  /// <para> .NET's TOSTRING() METHOD: "clr.ToString()" will produce one of two output formats: (a) for a colour assigned by ARGB
  /// values: "Color [A=10, R=20, G=30, B=40]"; (b) for a colour assigned by equating to a named .NET colour: "Color [LawnGreen]".</para>
  /// </summary>
  public static string _ToString(this Color colour, string ARGB, params string[] HexPrefix)
  { if (String.IsNullOrEmpty(ARGB)) ARGB = "ARGB";
    ARGB = ARGB.ToUpper();
    bool asHex = (HexPrefix.Length > 0 && !String.IsNullOrEmpty(HexPrefix[0]) );
    string prefix = "";  
    bool asHex6Digits = false;
    if (asHex) 
    { prefix = HexPrefix[0];
      asHex6Digits = (HexPrefix.Length > 1 && HexPrefix[1].Trim() == "6");
    }
    char[] fields = ARGB.ToCharArray();
    StringBuilder sb = new StringBuilder();    
    byte b = 0; 
    int flen = fields.Length;
    for (int i=0; i < flen; i++)
    { char c = ' ', f = fields[i];
      if (f == 'S')
      { sb.Append("S = ");
        if (asHex) sb.Append(prefix + (colour.ToArgb())._ToHex(8));
        else       sb.Append(colour.ToArgb().ToString());
        c = '.'; // a flag for end of loop, indicating that processing has occurred. Value must be < 'A' and not ' '.
      }
      else if ( f == 'A') { b = colour.A; c = 'A'; }
      else if ( f == 'R') { b = colour.R; c = 'R'; }
      else if ( f == 'G') { b = colour.G; c = 'G'; }
      else if ( f == 'B') { b = colour.B; c = 'B'; }
      if (c >= 'A') // i.e. one of the above 4 has applied:
      { sb.Append(c);   sb.Append(" = "); // e.g. "A = "
        if (asHex) sb.Append(prefix + ((int) b)._ToHex(2)); // e.g. "A = 0xff"
        else       sb.Append(b.ToString()); // e.g. "A = 255"
      }
      if (c != ' ' && i < flen-1) sb.Append("; "); // i.e. if any processing has occurred, add the delimiter.
    }
    if (asHex)
    { string ss = sb.ToString();
      if (asHex6Digits) ss = ss._Extent(2);
      if (HexPrefix.Length > 2 && HexPrefix[2].Length > 0 && HexPrefix[2][0] >= 'a') ss = ss.ToLower();
      return ss;
    }  
    else return sb.ToString();  
  }
  /// <summary>
  /// <para>Returns TRUE if ALL the fields referenced in CheckThese are equal. These fields are: [0] = check Alpha; [1] = check Red; [2] = check Green; 
  /// [3] = check Blue.) Any omitted fields default to TRUE. Therefore full ARGB equality is checked if there is no bool[] argument.</para>
  /// <para>No error situations, but realize that if all flags are FALSE, the method will always return TRUE.</para>
  ///<para>---------</para>
  /// <para>POINTS RE .NET's Color STRUCTURE: it has a read-only field 'Name'. When creating a colour, you cannot assign a name to it.
  /// If you create a colour via "Color.FromArgb(.)", it is automatically assigned a hex version (with leading zeroes removed) as the Name. 
  /// E.g. with "clr = Color.FromArgb(255,3,4,5), clr.Name will return "ff30405"; for ...(0,0,0,0), clr.Name returns "0". 
  /// But if you create the colour with a built-in .NET name - "clr = Color.Red;" - clr.Name will return "Red". This is important when it
  /// comes to using '=' between colours. Given that Color.Red has ARGB fields (255, 255, 0, 0):</para>
  /// <para>"clr1 = Color.FromArgb(255,255,0,0); clr2 = Color.Red; bool isEqual = (clr1 == clr2);" - isEqual will evaluate to FALSE.</para>
  /// <para>"clr1 = Color.FromArgb(255,255,0,0); clr2 = Color.FromArgb(255,255,0,0); bool isEqual = (clr1 == clr2);" - isEqual will evaluate to TRUE.</para>
  /// <para>"clr1 = Color.Red; clr2 = Color.Red; bool isEqual = (clr1 == clr2);" - isEqual will evaluate to TRUE.</para>
  /// </summary>
  public static bool _Equals(this Color colour, Color otherClr, params bool[] checkThese )
  { int len = checkThese.Length;
    bool checkA = (len < 1 || checkThese[0]);
    bool checkR = (len < 2 || checkThese[1]);
    bool checkG = (len < 3 || checkThese[2]);
    bool checkB = (len < 4 || checkThese[3]);
    if (checkA && colour.A != otherClr.A) return false;
    if (checkR && colour.R != otherClr.R) return false;
    if (checkG && colour.G != otherClr.G) return false;
    if (checkB && colour.B != otherClr.B) return false;
    return true;
  }
*/

  } // END OF CLASS JX

} // END OF NAMESPACE J_LIB
