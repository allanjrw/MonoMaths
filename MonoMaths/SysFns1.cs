using System;
using System.Collections.Generic;
using System.IO;
using JLib;

namespace MonoMaths
{
public struct DubList
{ internal List<double> LIST;
  internal DubList (int dummy)
  { LIST = new List<double>(); }
}

internal partial class F
{
  private F(){} // Can't instantiate.

//==============================================================================
  public static int SysFnCnt; // [0] is a dummy, not used. Highest valid
                              // fn. no. is SysFnCnt-1.
   // REF args. are those for which 'Fn' and 'At' indices must be supplied. In
   //  many situations it doesn't matter if 'Fn' is >= 0; for example, the fn.
   //  may be happy with either a temp. or a reg'd array. (It would treat them
   //  differently, but would accept both.) Where it DOES matter, .REFsRegd is
   //  set to TRUE; in all other cases, to FALSE.

  public static int MaxREFArgs = 256; // Max. no. of REF args. Should be large enough to cater for strings in 'show(..)', but not a memory hog.
  public static Duo[] REFArgLocn = new Duo[MaxREFArgs]; // Only used where .REFArgCnt > 0. .Y is set to .Fn for that variable, .X to its .At.
  // NB - .Y can be negative validly (-600, = R.TempScalarCode; the arg. is the scalar result of solving a function, including e.g. 'arr[1,2]')
  //  or invalidly. If you call 'V.GetVar...' functions with a negative RefArgLocn[i].Y, these will crash the whole program, as they have 
  //  no test for faulty fn. level (for speed, since they are called extremely frequently). 
  // IF SysFn[Which].TFn.REFsRegd IS  SET TO TRUE, there is NO NEED TO TEST for these, as it is done routinely (see line below:
  //  "if (SysFn[Which].REFsRegd)")
  // Otherwise test as follows  ('fn' used for REFArgLocn[i].Y): (1) "if (fn == R.TempScalarCode){<react as for scalar arg>};" followed by:
  //  (2) "if (fn < 0 || fn >= C.UserFnCnt){ < raise an error> }".
  public static TFn[] SysFn;
  public static List<string> SysFnNames;
  public static Random Rando; // the Random object, with methods for generating
                              //  random nos.
  public static int NoTimers = 20;
  public static DateTime[] Timers = new DateTime[NoTimers]; // Hopefully the individual values are set to no msecs since start of 1AD.
  public static TimeSpan[] TimerHolds = new TimeSpan[NoTimers]; // If a timer is paused, holds the time counted up till the pause.
  public static TimeSpan TimeZero = new TimeSpan(0);
  public static bool IOError = false; // Set by all disk I/O functions, and by function 'iok()' - which resets it after the call.
  public static string IOMessage = ""; // Set by all disk I/O functions, usually (but not always) only "" if an error. The setting
    // persists until the next I/O function or until function 'iomessage()' called - which reads it and then resets it to "".
    // Note that call to 'iok()' does not reset IOMessage, and call to 'iomessage()' does not reset IOError.
  public static string IOFilePath = ""; // Set by all disk I/O functions that access a particular file. Persists as for IOMessage above.
                                        // Accessed by 'iofile()'.
  public static List<DubList> Sys; // system lists accessed by functions of the type "list(s)_xxx()" below
  public static int NoSysLists;
  public static bool PreserveSysLists; // Set to FALSE when MiniMaths starts up. Set to TRUE by fn. 'lists_preserve()'.
      // Then reset to FALSE just before the 'GO' click routine calls R.Run(0).
  public static bool[] CanPause = new bool[10]; // answering to sys. fn. 'pause(0 to 9)'. Set to default TRUE in R.KillData().

// All of the following can be reset by method ResetStaticParams(.):
  public static bool MaximizeGraphs = false, CentreGraphs = false, PositionGraphs = false; 
      // Dealt with in this order - only the first 'true' bool will be handled. The first two centre or 
      //   maximize all Board instances till user alters setting. The third apply the board's top left corner 
      //   to the values set in the variable below:
  public static Duo BoardLocation = new Duo (0, 0);
  public static int GraphRimPixWd = 0,  GraphRimPixHt = 0; // 0 is the 'ignore' setting.
  public static int NextXSegments, NextYSegments, NextZSegments;
  public static double NextXCornerReal, NextXTipReal,   NextYCornerReal, NextYTipReal,   NextZCornerReal, NextZTipReal;
  public static double GraphAscension = double.NaN,  GraphDeclination = double.NaN; // Set by function 'aspect', then immediately reset after use.
                                                                // Also reset for each new run by R.KillData().
  public static double DefaultGraphAscension = - Math.PI/8,  DefaultGraphDeclination = 3 * Math.PI/8;
    // The above values are multiples of class Graph's default for keyboard rotations: PI/8 for coarse moves, PI/40 for fine moves.
    // This ensures that rotations will enable views normal to each of the axis planes.
  public static int AliasFn1Index = 323; // The index in SysFn of the alias function 'func1()'.
  public static int AliasFn2Index = 324; // The index in SysFn of the alias function 'func2()'.
  public static double[] ForwardFiddleFactor, InverseFiddleFactor; // Used only by the FFT system function, which sets either
  // and reuses it if required. There is and should always be NO access by the user to these two fields; nor can any code except that in 
  // sys. fn. 'fft(.)' generate / alter them. They are killed by R.KillData(), so do not persist between runs. (This is because they
  // may be huge, and the next program might be a memory hog.)
  public static string PaletteStr = "blue|red|green|orange|magenta|darkviolet|deepskyblue|brown|grey|black";
  public static string[] Palette; // Generated from the above in F.SetUpSysFns' immediately below.
  public static DateTime MonoMathsStartTime;

//==============================================================================
  public static void SetUpSysFns() // Called at program startup, from Mainform.
  { // Initialize a random no. object, based on some time-related algorithm of C#:
    Rando = new Random(); // Will operate until the user invokes 'seed(..)'.
    // Set up the array of system functions:
    Palette = PaletteStr.Split(new [] {'|'}); 
    SysFnCnt = 457;
    SysFn = new TFn[SysFnCnt];
    SysFn[0] = new TFn(0);  SysFn[0].Name = "____"; // Dummy only, but accessed in searches, so make the name impossible.
    SysFnNames = new List<string>(SysFnCnt);
    MonoMathsStartTime = DateTime.Now; // Only set when MonoMaths itself is starting up.
    int MaxArrayDims = TVar.MaxNoDims;
// NB: DON'T CHANGE INDEXES below unless you really have to. If you do so, search
//  for every occurrence of 'Which' within the switch statement, as in some cases
//  the index in SysFn[] is used there. ALSO search for every reference in OTHER
//  files for "F.RunSysFn", as such calls supply a fn. index as an argument.
// ALSO, IF CHANGING FN. NAMES, check every ref. to ".Name" below, as some code
//  depends on the exact name.
// DON'T RETURN "result.I = slot;" if 'slot' is not the result of a call to
//   V.GenerateTempStoreRoom (so that it may well point to the storeroom of a 
//  named array.) Otherwise the named variable will be treated as if it were
//  a transient array, --> unreliable behaviour and possible crash.

//  FIVE STEPS FOR CHANGING THE CONTENTS OF AN EXISTING ARRAY (which should be referenced by one of the MaxREFArgs):
//    1.  Set up what will soon be the StoreItem field DimSz; Make sure its size is TVar.MaxNoDims!
//        int[] dim_sz = new int[... 
//    2.  int newslot = V.GenerateTempStoreRoom(dim_sz);
//    3.  StoreItem sitem = R.Store[newslot];
//        sitem.Data = ...;
//        sitem.IsChars = ...;
//    4.  int Fn = REFArgLocn[i].Y, At = REFArgLocn[i].X;   <where 'i' is the appropriate argument index>
//    5.  V.TransferTempArray(newslot, Fn, At); // This vital step takes care of everything - don't try to do its work by hand!!
//    Special case: If you are CERTAIN that there is no change in dimensions, then you don't need to do any of this;
//        keep the same store item, but reset its data, without otherwise changing the store item or V.Vars[][] in any other way.

    //                 NAME,        MIN/MAXargs REFArgCnt  REFsRegd Hybrid
    SysFn[1]   = new TFn("dim",       2, MaxREFArgs, MaxREFArgs, false,1); // <--- if changing name or details, change ref. in units Parser & Conformer.
    SysFn[2]   = new TFn("dimlike",   2, MaxREFArgs, MaxREFArgs, false,1); // <--- if changing name or details, change ref. in Parser
    SysFn[3]   = new TFn("__array",   1, MaxREFArgs, MaxREFArgs,false, 1); // <--- if changing name or details, change ref. in units Parser & Conformer.
  //  case 348: DIMLIKE(load of arrays) -- see case 1 - "DIM(.)"
//    SysFn[254] = new TFn("__array",   1, MaxREFArgs, MaxREFArgs,false, 1); // <--- if changing name or details, change ref. in units Parser & Conformer.

    SysFn[4]   = new TFn("inc",       1, 1,      1,        true,       1);
    SysFn[5]   = new TFn("dec",       1, 1,      1,        true,       1);
    SysFn[6]   = new TFn("deg",       1, 1,      0,        false,      2);
    SysFn[7]   = new TFn("rad",       1, 1,      0,        false,      2);
    SysFn[8]   = new TFn("sin",       1, 1,      0,        false,      2);
    SysFn[9]   = new TFn("cos",       1, 1,      0,        false,      2);
    SysFn[10]  = new TFn("tan",       1, 1,      0,        false,      2);
    SysFn[11]  = new TFn("arcsin",    1, 2,      0,        false,      2);
    SysFn[12]  = new TFn("arccos",    1, 2,      0,        false,      2);
    SysFn[13]  = new TFn("arctan",    1, 2,      0,        false,      1);
    SysFn[14]  = new TFn("abs",       1, 1,      0,        false,      2);
    SysFn[15]  = new TFn("sqrt",      1, 1,      0,        false,      2);
    SysFn[16]  = new TFn("fact",      1, 2,      0,        false,      2);
    SysFn[17]  = new TFn("logfact",   1, 2,      0,        false,      2);
    SysFn[18]  = new TFn("exp",       1, 1,      0,        false,      2);
    SysFn[19]  = new TFn("ln",        1, 1,      0,        false,      2);
    SysFn[20]  = new TFn("log",       1, 2,      0,        false,      2);
    SysFn[21]  = new TFn("round",     1, 2,      0,        false,      2);
    SysFn[22]  = new TFn("frac",      1, 1,      0,        false,      2);
    SysFn[23]  = new TFn("floor",     1, 2,      0,        false,      2);
    SysFn[24]  = new TFn("ceiling",   1, 2,      0,        false,      2);
    SysFn[25]  = new TFn("mod",       2, 2,      0,        false,      2);
    SysFn[26]  = new TFn("div",       2, 2,      0,        false,      2);
    SysFn[27]  = new TFn("isintegral",1, 2,      0,        false,      2);
    SysFn[28]  = new TFn("rand",      1, 3,      1,        false,      1);
    SysFn[29]  = new TFn("seed",      1, 1,      0,        false,      1);
    //                  NAME,        MIN/MAXargs REFArgCnt  REFsRegd Hybrid
    SysFn[30]  = new TFn("data",      1, 1000,   0,        false,      1);
    SysFn[31]  = new TFn("show",      1, MaxREFArgs, MaxREFArgs,false, 1);
    SysFn[32]  = new TFn(C.ArrayFnRHS,2, MaxArrayDims+1, 0,   false, 1);//__segmt()  *** If changing index, amend fn. "getval(.)" which recursively accesses it.
    SysFn[33]  = new TFn(C.ArrayFnLHS,3, MaxArrayDims+2, 1,   true,  1);//__assign()
    SysFn[34]  = new TFn("label",      1, MaxREFArgs, MaxREFArgs,false, 1);
    SysFn[35]  = new TFn("redim",     2, 1000,   0,        false,      1);
    SysFn[36]  = new TFn("redimlike", 2, 1000,   0,        false,      1);
    SysFn[37]  = new TFn("fill",      2, 1000,   1,        false,      1);
    SysFn[38]  = new TFn("__quote",   1, 1,      0,        false,      1);
    SysFn[39]  = new TFn("polystring",1, 4,      0,        false,      1);
    SysFn[40]  = new TFn("gridx",     2, 5,      0,        false,      1);
    SysFn[41]  = new TFn("gridy",     2, 5,      0,        false,      1);
    SysFn[42]  = new TFn("gridz",     2, 5,      0,        false,      1);
    SysFn[43]  = new TFn("killplot",  1, 1000,   0,        false,      1);
    SysFn[44]  = new TFn("killgraphs",2, 2,      0,        false,      1);
    SysFn[45]  = new TFn("addplot",   2, 1000,   0,        false,      1);
    SysFn[46]  = new TFn("save",      4, 7,      2,        false,      1);
    SysFn[47]  = new TFn("datastrip", 2, 3,      0,        false,      1);
    SysFn[48]  = new TFn("defrac",    1, 1,      0,        false,      2);
    SysFn[49]  = new TFn("chars",     1, 1000,   0,        false,      1);
    SysFn[50]  = new TFn("unchars",   1, 1000,   0,        false,      1);
    SysFn[51]  = new TFn("write",     1, MaxREFArgs, MaxREFArgs, false, 1);
    SysFn[52]  = new TFn("hex",       1, 2,      0,        false,      1);
    SysFn[53]  = new TFn("unhex",     1, 2,      0,        false,      1);
    SysFn[54]  = new TFn("ladder",    3, 4,      1,        false,      1);
    SysFn[55]  = new TFn("bin",       1, 3,      0,        false,      1);
    SysFn[56]  = new TFn("writeln",   1, MaxREFArgs, MaxREFArgs, false, 1);
    SysFn[57]  = new TFn("size",      1, 2,      0,        false,      1);
    SysFn[58]  = new TFn("dims",      1, 2,      0,        false,      1);
    SysFn[59]  = new TFn("neat",      1, 3,      0,        false,      1);
    SysFn[60]  = new TFn("copy",      2, 3,      0,        false,      1);
    SysFn[61]  = new TFn("copyto",    3, 3,      0,        false,      1);
    SysFn[62] = new TFn("find",       3, 4,      0,        false,      1);
    SysFn[63] = new TFn("finds",      3, 4,      0,        false,      1);
    SysFn[64]  = new TFn("findall",   3, 4,      0,        false,      1);
    SysFn[65]  = new TFn("transpose", 1, 1,      0,        false,      1);
    SysFn[66]  = new TFn("dot",       2, 2,      0,        false,      1);
    SysFn[67]  = new TFn("mxmult",    2, 3,      0,        false,      1);
    SysFn[68]  = new TFn("max",       1, 1000,   0,        false,      1);
    SysFn[69]  = new TFn("maxat",     1, 1000,   0,        false,      1);
    SysFn[70]  = new TFn("maxabs",    1, 1000,   0,        false,      1);
    SysFn[71]  = new TFn("maxabsat",  1, 1000,   0,        false,      1);
    SysFn[72]  = new TFn("sort",      1, 4,      0,        false,      1);
    SysFn[73]  = new TFn("sortbykey", 2, 1000,   0,        false,      1);
    SysFn[74]  = new TFn("grid",      2, 10,     0,        false,      1);
    SysFn[75]  = new TFn("removeplot",2, 1000,   0,        false,      1);
    SysFn[76]  = new TFn("request",   6, 1000,   1,        false,      1);
    SysFn[77]  = new TFn("clip",      3, 3,      0,        false,      1);
    SysFn[78]  = new TFn("clipabs",   3, 3,      0,        false,      1);
    SysFn[79]  = new TFn("evalpoly",  2, 2,      0,        false,      1);
    SysFn[80]  = new TFn("solvesim",  2, 2,      0,        false,      1);
    SysFn[81]  = new TFn("determinant",1,2,      0,        false,      1);
    SysFn[82]  = new TFn("solvepoly", 1, 3,      0,        false,      1);
    SysFn[83]  = new TFn("rootstopoly",1, 1,     0,        false,      1);
    SysFn[84]  = new TFn("undim",     1, MaxREFArgs, MaxREFArgs, true, 1);
    SysFn[85]  = new TFn("interpolate",2,2,      0,        false,      1);
    SysFn[86]  = new TFn("even",      1, 2,      0,        false,      2);
    SysFn[87]  = new TFn("diffpoly",  1, 1,      0,        false,      1);
    SysFn[88]  = new TFn("load",      1, 4,      0,        false,      1);
    SysFn[89]  = new TFn("between",   3, 3,      0,        false,      1);
    SysFn[90]  = new TFn("sign",      1, 5,      0,        false,      1);
    SysFn[91]  = new TFn("integral",  3, 3,      0,        false,      1);
    SysFn[92]  = new TFn("intcurve",  3, 4,      0,        false,      1);
    SysFn[93]  = new TFn("gauss",     5, 5,      0,        false,      1);
    SysFn[94]  = new TFn("randgauss", 2, 5,      1,        false,      1);
    SysFn[95]  = new TFn("graphcopy", 1, 2,      0,        false,      1);
    SysFn[96]  = new TFn("curvefit",  3, 3,      0,        false,      1);
    SysFn[97]  = new TFn("upsample",  3, 6,      0,        false,      1);
    SysFn[98]  = new TFn("downsample",2, 4,      0,        false,      1);
    SysFn[99]  = new TFn("odometer",  2, 3,      1,        true,       1);
    SysFn[100] = new TFn("moments",   1, 2,      0,        false,      1);
    SysFn[101] = new TFn("setbins",   1, 5,      0,        false,      1);
    SysFn[102] = new TFn("sum",       1, 3,      0,        false,      1);
    SysFn[103] = new TFn("shuffle",   1, 2,      0,        false,      1);
    SysFn[104] = new TFn("isarray",   1, 1,      0,        false,      1);
    SysFn[105] = new TFn("norm",      1, 1,      0,        false,      1);
    SysFn[106] = new TFn("inverse",   1, 2,      0,        false,      1);
    SysFn[107] = new TFn("text",      1, MaxREFArgs, MaxREFArgs,false, 1);
    SysFn[108] = new TFn("last",      1, 2,      0,        false,      1);
    SysFn[109] = new TFn("errortrap", 1, 2,      0,        false,      1);
    SysFn[110] = new TFn("errormsg",  1, 1,      0,        false,      1);
    SysFn[111] = new TFn("errorcnt",  1, 1,      0,        false,      1);
    SysFn[112] = new TFn("list_to",   3, 3,      0,        false,      1);
    SysFn[113] = new TFn("fourier",   1, 2,      0,        false,      1);
    SysFn[114] = new TFn("rect",      1, 2,      0,        false,      1);
    SysFn[115] = new TFn("polar",     1, 2,      0,        false,      1);
    SysFn[116] = new TFn("complex",   4, 5,      0,        false,      1);
    SysFn[117] = new TFn("merge",     2, 1000,   0,        false,      1);
    SysFn[118] = new TFn("graphresize",2,3,      0,        false,      1);
    SysFn[119] = new TFn("place",     2, 3,      0,        false,      1);
    SysFn[120] = new TFn("places",    2, 3,      0,        false,      1);
    SysFn[121] = new TFn("reorder",   2, 1000,   0,        false,      1);
    SysFn[122] = new TFn("matches",   2, 3,      0,        false,      1);
    SysFn[123] = new TFn("mismatches",2, 3,      0,        false,      1);
    SysFn[124] = new TFn("nozero",    1, 2,      0,        false,      1);
    SysFn[125] = new TFn("allzero",   1, 2,      0,        false,      1);
    SysFn[126] = new TFn("is",        1, 2,      0,        false,      1);
    SysFn[127] = new TFn("not",       1, 2,      0,        false,      1);
    SysFn[128] = new TFn("bkmkcopy",  4, 4,      0,        false,      1);
    SysFn[129] = new TFn("bkmkreplace",5,6,      0,        false,      1);
    SysFn[130] = new TFn("rowop",     5, 9,      0,        false,      1);
    SysFn[131] = new TFn("colop",     5, 9,      0,        false,      1);
    SysFn[132] = new TFn("randsign",  1, 2,      0,        false,      1);
    SysFn[133] = new TFn("starttimer",1, NoTimers, 0,      false,      1);
    SysFn[134] = new TFn("timer",     1, 2,      0,        false,      1);
    SysFn[135] = new TFn("chain",     2, 2,      0,        false,      1);
    SysFn[136] = new TFn("placeboard",2, 5,      0,        false,      1);
    SysFn[137] = new TFn("lastclosed",1, 1,      0,        false,      1);
    SysFn[138] = new TFn("jagger",    3, 6,      1,        true,       1);
    SysFn[139] = new TFn("cull",      2, 3,      0,        false,      1);
    SysFn[140] = new TFn("unmerge",   3, MaxREFArgs, MaxREFArgs,true,  1);
    SysFn[141] = new TFn("span",      3, 5,      0,        false,      1);
    SysFn[142] = new TFn("readgrid",  1, 1,      0,        false,      1);
    SysFn[143] = new TFn("captured",  1, 1,      0,        false,      1);
    SysFn[144] = new TFn("gfixedclick",1,1,      0,        false,      1);
    SysFn[145] = new TFn("decide",    3, 10,     0,        false,      1);
    SysFn[146] = new TFn("run",       2, 2,      0,        false,      1);
    SysFn[147] = new TFn("pgm_copy",  2, 4,      0,        false,      1);
    SysFn[148] = new TFn("pgm_graft", 3, 5,      0,        false,      1);
    SysFn[149] = new TFn("replace",   4, 4,      0,        false,      1);
    SysFn[150] = new TFn("replaceto", 4, 4,      0,        false,      1);
    SysFn[151] = new TFn("insert",    3, 3,      0,        false,      1);
    SysFn[152] = new TFn("delete",    2, 3,      0,        false,      1);
    SysFn[153] = new TFn("deleteto",  3, 3,      0,        false,      1);
    SysFn[154] = new TFn("list",      1, 3,      0,        false,      1);
    SysFn[155] = new TFn("lists",     3, 4,      0,        false,      1);
    SysFn[156] = new TFn("lists_to",  3, 4,      0,        false,      1);
    SysFn[157] = new TFn("clear",     1, MaxREFArgs, MaxREFArgs,true,  1);
    SysFn[158] = new TFn("extract_values",3,3,   1,        true,       1);
    SysFn[159] = new TFn("iok",       1, 1,      0,        false,      1);
    SysFn[160] = new TFn("iomessage", 1, 1,      0,        false,      1);
    SysFn[161] = new TFn("iofile",    1, 1,      0,        false,      1);
    SysFn[162] = new TFn("shufflemx", 2, 3,      0,        false,      1);
    SysFn[163] = new TFn("kill",      1, MaxREFArgs, MaxREFArgs,true,  1);
    SysFn[164] = new TFn("asc",       1, 1,      0,        false,      1);
    SysFn[165] = new TFn("pgm_load",  2, 2,      0,        false,      1);
    SysFn[166] = new TFn("list_new",  1, 1,      0,        false,      1);
    SysFn[167] = new TFn("lists_new", 1, 1,      0,        false,      1);
    SysFn[168] = new TFn("list_clear",1, 1000,   0,        false,      1);
    SysFn[169] = new TFn("__UNUSED",  2, 2,      0,        false,      1);
    SysFn[170] = new TFn("__UNUSED",  2, 2,      0,        false,      1);
    SysFn[171] = new TFn("lists_kill",1,1,       0,        false,      1);
    SysFn[172] = new TFn("lists_preserve",1,1,   0,        false,      1);
    SysFn[173] = new TFn("list_add",  2, 1000,   0,        false,      1);
    SysFn[174] = new TFn("lists_count",1,1,      0,        false,      1);
    SysFn[175] = new TFn("list_size", 1,1,       0,        false,      1);
    SysFn[176] = new TFn("list_read", 1, 3,      0,        false,      1);
    SysFn[177] = new TFn("list_read_to",3,3,     0,        false,      1);
    SysFn[178] = new TFn("list_alter",3,3,       0,        false,      1);
    SysFn[179] = new TFn("lists_read",3,4,       0,        false,      1);
    SysFn[180] = new TFn("lists_read_to",3,4,    0,        false,      1);
    SysFn[181] = new TFn("list_push", 2, 2,      0,        false,      1);
    SysFn[182] = new TFn("list_pop",  2, 2,      2,        false,      1);
    SysFn[183] = new TFn("placement", 2, 2,      0,        false,      1);
    SysFn[184] = new TFn("isplot",    1, 1000,   0,        false,      1);
    SysFn[185] = new TFn("isgraph",   1, 1000,   0,        false,      1);
    SysFn[186] = new TFn("push",      3, 4,      1,        true,       1);
    SysFn[187] = new TFn("pop",       2, 3,      1,        true,       1);
    SysFn[188] = new TFn("reversed",  1, 1000,   1,        false,      1);
    SysFn[189] = new TFn("newclick",  1, 1,      1,        false,      1);
    SysFn[190] = new TFn("list_delete",2,3,      0,        false,      1);
    SysFn[191] = new TFn("list_delete_to",3,3,   0,        false,      1);
    SysFn[192] = new TFn("list_insert",3, 1000,  0,        false,      1);
    SysFn[193] = new TFn("distance",  2, 4,      0,        false,      1);
    SysFn[194] = new TFn("pixels",    1, 1,      0,        false,      1);
    SysFn[195] = new TFn("hypot",     2, 3,      0,        false,      1);
    SysFn[196] = new TFn("vecdirns",  3, 3,      0,        false,      1);
    SysFn[197] = new TFn("primes",    2, 4,      0,        false,      1);
    SysFn[198] = new TFn("unpack",    2, MaxREFArgs, MaxREFArgs,false, 1); // <--- if changing name or details, change ref. in units Parser & Conformer.
    SysFn[199] = new TFn("binom",     2, 3,      0,        false,      1);
    SysFn[200] = new TFn("logbinom",  2, 3,      0,        false,      1);
    SysFn[201] = new TFn("select",    2, 4,      0,        false,      1);
    SysFn[202] = new TFn("substitute",3, 4,      0,        false,      1);
    SysFn[203] = new TFn("removedups",1, 3,      0,        false,      1);
    SysFn[204] = new TFn("boolstr",   1, 1000,   0,        false,      1);
    SysFn[205] = new TFn("reverse",   1, 1,      1,        true,       1);
    SysFn[206] = new TFn("selectrows",2,2,       0,        false,      1);
    SysFn[207] = new TFn("selectcols",2,2,       0,        false,      1);
    SysFn[208] = new TFn("pause",     1, 1,      0,        false,      1);
    SysFn[209] = new TFn("pausable",  1, 2,      0,        false,      1);
    SysFn[210] = new TFn("poke",      3, 3,      0,        false,      1);
    SysFn[211] = new TFn("pokerows",  3, 3,      0,        false,      1);
    SysFn[212] = new TFn("pokecols",  3, 3,      0,        false,      1);
    SysFn[213] = new TFn("boardresize",3,3,      0,        false,      1);
    SysFn[214] = new TFn("cullbykey", 2, 2,      0,        false,      1);
    SysFn[215] = new TFn("empty",     1, 1,      0,        false,      1);
    SysFn[216] = new TFn("appendrows",2, 3,      0,        false,      1);
    SysFn[217] = new TFn("insertrows",3, 4,      0,        false,      1);
    SysFn[218] = new TFn("appendcols",2, 3,      0,        false,      1);
    SysFn[219] = new TFn("insertcols",3, 4,      0,        false,      1);
    SysFn[220] = new TFn("plotshape", 7, 12,     0,        false,      1);
    SysFn[221] = new TFn("copyshape", 2, 6,      0,        false,      1);
    SysFn[222] = new TFn("plotbins",  1, 9,      0,        false,      1);
    SysFn[223] = new TFn("sumrows",   1, 3,      0,        false,      1);
    SysFn[224] = new TFn("sumcols",   1, 3,      0,        false,      1);
    SysFn[225] = new TFn("deleterows",2, 3,      0,        false,      1);
    SysFn[226] = new TFn("deletecols",2, 3,      0,        false,      1);
    SysFn[227] = new TFn("xmenu",     2, 2,      0,        false,      1);
    SysFn[228] = new TFn("xvisible",  1, 1000,   0,        false,      1);
    SysFn[229] = new TFn("xclick",    1, 1,      0,        false,      1);
    SysFn[230] = new TFn("plot",      1, 12,     0,        false,      1);
    SysFn[231] = new TFn("plot3d",    3, 13,     0,        false,      1);
    SysFn[232] = new TFn("graph",     1, 1000,   0,        false,      1);
    SysFn[233] = new TFn("graph3d",   1, 1000,   0,        false,      1);
    SysFn[234] = new TFn("header",    1, MaxREFArgs, MaxREFArgs,false, 1);
    SysFn[235] = new TFn("footer",    1, MaxREFArgs, MaxREFArgs,false, 1);
    SysFn[236] = new TFn("labelx",    1, MaxREFArgs, MaxREFArgs,false, 1);
    SysFn[237] = new TFn("labely",    1, MaxREFArgs, MaxREFArgs,false, 1);
    SysFn[238] = new TFn("labelz",    1, MaxREFArgs, MaxREFArgs,false, 1);
    SysFn[239] = new TFn("plotsof",   1, 1,      0,        false,      1);
    SysFn[240] = new TFn("scaleoverx",2, 2,      0,        false,      1);
    SysFn[241] = new TFn("scaleovery",2, 2,      0,        false,      1);
    SysFn[242] = new TFn("scaleoverz",2, 2,      0,        false,      1);
    SysFn[243] = new TFn("scalefudgex",3, 5,     0,        false,      1);
    SysFn[244] = new TFn("scalefudgey",3, 5,     0,        false,      1);
    SysFn[245] = new TFn("moveshape", 3, 7,      0,        false,      1);
    SysFn[246] = new TFn("scaleformatx",1, 2,    0,        false,      1);
    SysFn[247] = new TFn("scaleformaty",1, 2,    0,        false,      1);
    SysFn[248] = new TFn("scaleformatz",1, 2,    0,        false,      1);
    SysFn[249] = new TFn("scaleformat",1, 2,     0,        false,      1);
    SysFn[250] = new TFn("showhairlines",2, 4,   0,        false,      1);
    SysFn[251] = new TFn("scalefit",  1, 4,      0,        false,      1);
    SysFn[252] = new TFn("plotmesh",  2, 14,     0,        false,      1);
    SysFn[253] = new TFn("plotmesh3d",3, 15,     0,        false,      1);
    SysFn[254] = new TFn("sqr",       1, 1,      0,        false,      1);
    SysFn[255] = new TFn("finddup",   2, 4,      0,        false,      1);
    SysFn[256] = new TFn("perturb",   3, 3,      0,        false,      1);
    SysFn[257] = new TFn("mesh",      2, 3,      0,        false,      1);
    SysFn[258] = new TFn("__scalar",  1, MaxREFArgs,MaxREFArgs, false, 1); // <--- if changing name or details, change ref. in units Parser & Conformer.
    SysFn[259] = new TFn("replacerows",3,4,      0,        false,      1);
    SysFn[260] = new TFn("replacecols",3,4,      0,        false,      1);
    SysFn[261] = new TFn("chainrows", 2, 2,      0,        false,      1);
    SysFn[262] = new TFn("chaincols", 2, 2,      0,        false,      1);
    SysFn[263] = new TFn("copyrows",  2, 3,      0,        false,      1);
    SysFn[264] = new TFn("copycols",  2, 3,      0,        false,      1);
    SysFn[265] = new TFn("copyrowsto",3, 3,      0,        false,      1);
    SysFn[266] = new TFn("copycolsto",2, 3,      0,        false,      1);
    SysFn[267] = new TFn("compareto", 5, 5,      0,        false,      1);
    SysFn[268] = new TFn("mouse",     1, 2,      0,        false,      1);
    SysFn[269] = new TFn("gxmenu",    3, 3,      0,        false,      1);
    SysFn[270] = new TFn("gxvisible", 1,1000,    0,        false,      1);
    SysFn[271] = new TFn("gxclick",   1, 1,      0,        false,      1);
    SysFn[272] = new TFn("paintshape",3, 6,      0,        false,      1);
    SysFn[273] = new TFn("mxdiag",    1, 3,      0,        false,      1);
    SysFn[274] = new TFn("lufact",    3, 3,      3,        true,       1);//################# TEMPORARY
    SysFn[275] = new TFn("determ",    1, 2,      0,        false,      1);//################# TEMPORARY
    SysFn[276] = new TFn("compare",   2, 5,      0,        false,      1);
    SysFn[277]  = new TFn("str",      1, 2,      0,        false,      1);
    SysFn[278]  = new TFn("valuetostring",1,2,   0,        false,      1);
    SysFn[279] = new TFn("list_cull", 2, 1000,   0,        false,      1);
    SysFn[280] = new TFn("list_cull_range",3,3,  0,        false,      1);
    SysFn[281] = new TFn("list_find", 2, 3,      0,        false,      1);
    SysFn[282] = new TFn("mxhalf",    2, 5,      0,        false,      1);
    SysFn[283] = new TFn("offset",    2, 2,      0,        false,      1);
    SysFn[284] = new TFn("indexed",   2, 3,      0,        false,      1);
    SysFn[285] = new TFn("keyed",     1, 2,      1,        false,      1);
    SysFn[286] = new TFn("findinmx",  3, 5,      0,        false,      1);
    SysFn[287] = new TFn("cap",       3, 7,      0,        false,      1);
    SysFn[288] = new TFn("capped",    3, 7,      0,        true,       1);
    SysFn[289] = new TFn("hsl",       1, 2,      0,        false,      1);
    SysFn[290] = new TFn("hsl_to_rgb",1, 3,      0,        false,      1);
    SysFn[291] = new TFn("scalejumpx",3, 3,      0,        false,      1);
    SysFn[292] = new TFn("scalejumpy",3, 3,      0,        false,      1);
    SysFn[293] = new TFn("showarray", 1, 1,      1,        false,      1);
    SysFn[294] = new TFn("factors",   1, 1000,   0,        false,      1);
    SysFn[295] = new TFn("peck",      2, 3,      1,        true,       1);
    SysFn[296] = new TFn("prodrows",  1, 4,      0,        false,      1);
    SysFn[297] = new TFn("prodcols",  1, 4,      0,        false,      1);
    SysFn[298] = new TFn("product",   1, 2,      0,        false,      1);
    SysFn[299] = new TFn("rgb",       1, 2,      0,        false,      1);
    SysFn[300] = new TFn("join",      2, 4,      0,        false,      1);
    SysFn[301] = new TFn("graphkey",  1, 1,      0,        false,      1);
    SysFn[302] = new TFn("graphcolours",1, 7,    0,        false,      1);
    SysFn[303] = new TFn("evict",     2, 2,      0,        false,      1);
    SysFn[304] = new TFn("gfixedvisible",3,3,    0,        false,      1);
    SysFn[305] = new TFn("programfile",1,1,      0,        false,      1);
    SysFn[306] = new TFn("solve_de",  3, 4,      0,        false,      1);
    SysFn[307] = new TFn("unjag",     2, 3,      0,        false,      1);
    SysFn[308] = new TFn("linefit",   2, 3,      0,        false,      1);
    SysFn[309] = new TFn("differences",1,1,      0,        false,      1);
    SysFn[310] = new TFn("progressive",2,2,      0,        false,      1);
    SysFn[311] = new TFn("rowvec",    1, 1000,   0,        false,      1);
    SysFn[312] = new TFn("colvec",    1, 1000,   0,        false,      1);
    SysFn[313] = new TFn("matrix",    2, 1000,   0,        false,      1);
    SysFn[314] = new TFn("matrixop",  3, 3,      0,        false,      1);
    SysFn[315] = new TFn("lop",       2, 3,      0,        false,      1);
    SysFn[316] = new TFn("pad",       3, 4,      0,        false,      1);
    SysFn[317] = new TFn("truncate",  2, 2,      0,        false,      1);
    SysFn[318] = new TFn("readtable", 3, 6,      0,        false,      1);
    SysFn[319] = new TFn("intersection",2,1000,  0,        false,      1);
    SysFn[320] = new TFn("__importscalar", 1, MaxREFArgs, MaxREFArgs, false, 1); // <--- if changing name or details, change ref. in units Parser & Conformer.
    SysFn[321] = new TFn("__importarray",  1, MaxREFArgs, MaxREFArgs, false, 1); // <--- if changing name or details, change ref. in units Parser & Conformer.
    SysFn[322] = new TFn("setfunc",   2, 4,      0,        false,      1);
    SysFn[323] = new TFn("func1",     1, 1000,   0,        false,      1);
    SysFn[324] = new TFn("func2",     1, 1000,   0,        false,      1);
    SysFn[325] = new TFn("bitop",     2, 1000,   0,        false,      1);
    SysFn[326] = new TFn("exec",      1, 1000,   0,        false,      1);
    SysFn[327] = new TFn("kill_on_exit",1,1,     0,        false,      1);
    SysFn[328] = new TFn("expect_sd", 2, 2,      0,        false,      1);
    SysFn[329] = new TFn("expunge",   2, 4,      0,        false,      1);
    SysFn[330] = new TFn("expunge_range",3, 3,   0,        false,      1);
    SysFn[331] = new TFn("swing",     2, 6,      0,        false,      1);
    SysFn[332] = new TFn("list_opn",  4, 5,      0,        false,      1);
    SysFn[333] = new TFn("icon",      1, 1,      0,        false,      1);
    SysFn[334] = new TFn("revolve",   1, 3,      0,        false,      1);
    SysFn[335] = new TFn("revolved",  1, 3,      0,        false,      1);
    SysFn[336] = new TFn("graphfont", 4, 4,      0,        false,      1);
    SysFn[337] = new TFn("__UNUSED",  1, 1,      0,        false,      1);
    SysFn[338] = new TFn("__UNUSED",  1, 1,      0,        false,      1);
    SysFn[339] = new TFn("unicode",   1, 1000,   0,        false,      1);
    SysFn[340] = new TFn("overlay",   3, 5,      0,        false,      1);
    SysFn[341] = new TFn("overlaid",  3, 5,      1,        true,       1);
    SysFn[342] = new TFn("keydown",   1, 1,      0,        false,      1);
    SysFn[343] = new TFn("unbin",     1, 2,      0,        false,      1);
    SysFn[344] = new TFn("bestsquare",1, 1,      0,        false,      1);
    SysFn[345] = new TFn("charpoly",  1, 2,      0,        false,      1);
    SysFn[346] = new TFn("submatrix", 3, 3,      0,        false,      1);
    SysFn[347] = new TFn("cofactor",  3, 4,      0,        false,      1);
    SysFn[348] = new TFn("rotate",    1, MaxREFArgs, MaxREFArgs, false,1);
    SysFn[349] = new TFn("copymx",    3, 5,      0,        false,      1);
    SysFn[350] = new TFn("copymxto",  5, 5,      0,        false,      1);
    SysFn[351] = new TFn("tozero",    1, 2,      0,        false,      2);
    SysFn[352] = new TFn("fromzero",  1, 2,      0,        false,      2);
    SysFn[353] = new TFn("min",       1, 1000,   0,        false,      1);
    SysFn[354] = new TFn("minat",     1, 1000,   0,        false,      1);
    SysFn[355] = new TFn("minabs",    1, 1000,   0,        false,      1);
    SysFn[356] = new TFn("minabsat",  1, 1000,   0,        false,      1);
    SysFn[357] = new TFn("and",       2, 3,      0,        false,      1);
    SysFn[358] = new TFn("or",        2, 3,      0,        false,      1);
    SysFn[359] = new TFn("xor",       2, 3,      0,        false,      1);
    SysFn[360] = new TFn("xorcomp",   2, 3,      0,        false,      1);
    SysFn[361] = new TFn("correlation",4, 7,     0,        false,      1);
    SysFn[362] = new TFn("convolution",3, 3,     0,        false,      1);
    SysFn[363] = new TFn("fft",       1, 2,      0,        false,      1);
    SysFn[364] = new TFn("homedirectory", 1,1,   0,        false,      1);
    SysFn[365] = new TFn("playsound", 1, 2,      0,        false,      1);
    SysFn[366] = new TFn("aspect",    1, 3,      0,        false,      1);
    SysFn[367] = new TFn("choosefilename",1,2,   0,        false,      1);
    SysFn[368] = new TFn("currentdirectory",1,1, 0,        false,      1);
    SysFn[369] = new TFn("checkdirectory",1,2,   0,        false,      1);
    SysFn[370] = new TFn("filesize",  1, 1,      0,        false,      1);
    SysFn[371] = new TFn("split",     2, 7,      0,        false,      1);
    SysFn[372] = new TFn("clipcull",  3, 4,      0,        false,      1);
    SysFn[373] = new TFn("clipcullabs",3,4,      0,        false,      1);
    SysFn[374] = new TFn("nth",       1, 3,      0,        false,      1);
    SysFn[375] = new TFn("fixedsize", 3, 6,      0,        false,      1);
    SysFn[376] = new TFn("val",       1, 3,      0,        false,      1);
    SysFn[377] = new TFn("stringtovalue",1,3,    0,        false,      1);
    SysFn[378] = new TFn("chooseclr", 2, 2,      0,        false,      1);
    SysFn[379] = new TFn("ladderclr", 3, 6,      0,        false,      1);
    SysFn[380] = new TFn("structure", 1, MaxArrayDims, 0,  false,      1);
    SysFn[381] = new TFn("boardplacemt",1, 1,    0,        false,      1);
    SysFn[382] = new TFn("window",    2, 6,      0,        false,      1);
    SysFn[383] = new TFn("smash",     1, 3,      0,        false,      1);
    SysFn[384] = new TFn("thislineno",1, 1,      0,        false,      1);
    SysFn[385] = new TFn("findbrackets",4,4,     0,        false,      1);
    SysFn[386] = new TFn("window_find",1,5,      0,        false,      1);
    SysFn[387] = new TFn("datetime",  1, 1,      0,        false,      1);
    SysFn[388] = new TFn("plu",       2, 3,      0,        false,      1);
    SysFn[389] = new TFn("clipboard", 1, 2,      0,        false,      1);
    SysFn[390] = new TFn("lookup",    1, 3,      0,        false,      1);
    SysFn[391] = new TFn("touch_array",4,4,      0,        false,      1);
    SysFn[392] = new TFn("commandline",1,1,      0,        false,      1);
    SysFn[393] = new TFn("reposition",1, 4,      0,        false,      1);
    SysFn[394] = new TFn("repartition",2,2,      0,        false,      1);
    SysFn[395] = new TFn("palette",  1, 2,       0,        false,      1);
    SysFn[396] = new TFn("blockfile",1, 3,       0,        false,      1);
    SysFn[397] = new TFn("btnrelease",1,1,       0,        false,      1);
    SysFn[398] = new TFn("cursordata",1,1,       0,        false,      1);
    SysFn[399] = new TFn("persistent_array",1,1, 0,        false,      1);
    SysFn[400] = new TFn("lettercase",2,2,       0,        false,      1);
    SysFn[401] = new TFn("__UNUSED",4,4,     0,        false,      1);
    SysFn[402] = new TFn("exit_plus",  1,3,      0,        false,      1);
    SysFn[403] = new TFn("insert_image",4,4,     0,        false,      1);
    SysFn[404] = new TFn("isnan",     1, 1,      0,        false,      2);
    SysFn[405] = new TFn("randrange", 3, 4,      1,        true,       1);
    SysFn[406] = new TFn("fixangle",  1, 2,      0,        false,      2);
    SysFn[407] = new TFn("distmutual",1, 5,      0,        false,      1);
    SysFn[408] = new TFn("distpoints",2, 7,      0,        false,      1);
    SysFn[409] = new TFn("setalias",  1, MaxREFArgs, MaxREFArgs, true, 1);
    SysFn[410] = new TFn("getval",  1, 1+MaxArrayDims, 0,  false,      1);
    SysFn[411] = new TFn("setval",  2, 2+MaxArrayDims, MaxREFArgs, false, 1);
    SysFn[412] = new TFn("prevalence",2, 3,      0,        false,      1);
    SysFn[413] = new TFn("__constant",2, 2,      1,        false,      1);
    SysFn[414] = new TFn("filter",    4, 5,      1,        false,      1);
    SysFn[415] = new TFn("pointsof",  2, 3,      0,        false,      1);
    SysFn[416] = new TFn("cluster",   3, 6,      0,        false,      1);
    SysFn[417] = new TFn("mxcentre",  2, 3,      0,        false,      1);
    SysFn[418] = new TFn("graphvisible",1,2,     0,        false,      1);
    SysFn[419] = new TFn("cursorposn",1, 2,      0,        false,      1);
    SysFn[420] = new TFn("mainmenu",  1, 2,      0,        false,      1);
    SysFn[421] = new TFn("keyname",   1, 2,      0,        false,      1);
    SysFn[422] = new TFn("solveexp",  1, 2,      0,        false,      1);
    SysFn[423] = new TFn("sigmoid",   4, 4,      0,        false,      1);
    SysFn[424] = new TFn("sequence",  2, MaxREFArgs, MaxREFArgs,false, 1);
    SysFn[425] = new TFn("crash",     1, MaxREFArgs, MaxREFArgs,false, 1);
    SysFn[426] = new TFn("plotmx",    1, 8,      0,        false,      1);
    SysFn[427] = new TFn("rotaterow", 2, 6,      0,        false,      1);
    SysFn[428] = new TFn("rotatecol", 2, 6,      0,        false,      1);
    SysFn[429] = new TFn("multibox",  4, 5,      3,        true,       1);
    SysFn[430] = new TFn("setbox",    1, 4,      0,        false,      1);
    SysFn[431] = new TFn("count",     2, 4,      0,        false,      1);
    SysFn[432] = new TFn("getsegmt",  3, 5,      0,        false,      1);
    SysFn[433] = new TFn("setsegmt",  4, 4,      1,        true,       1);
    SysFn[434] = new TFn("findsegmt", 3, 6,      0,        false,      1);
    SysFn[435] = new TFn("findany",   3, 4,      0,        false,      1);
    SysFn[436] = new TFn("defluff",   2, 2,      1,        true,       1);
    SysFn[437] = new TFn("randum",    1, 1,      0,        false,      1);
    SysFn[438] = new TFn("equal",     2, 3,      0,        false,      1);
    SysFn[439] = new TFn("ini_data",  1, 1,      0,        false,      1);
    SysFn[440] = new TFn("train",     1, 5,      0,        false,      1);
    SysFn[441] = new TFn("removeruns",1, 2,      0,        false,      1);
    SysFn[442] = new TFn("seekno",    1, 5,      0,        false,      1);
    SysFn[443] = new TFn("enrol",     3, 4,      0,        false,      1);
    SysFn[444] = new TFn("hotkeysoff",1, 1,      0,        false,      1);
    SysFn[445] = new TFn("findlineno",1, 2,      0,        false,      1);
    SysFn[446] = new TFn("firfilter", 2, 5,      0,        false,      1);
    SysFn[447] = new TFn("graphtitle",2, 2,      0,        false,      1);
    SysFn[448] = new TFn("monotonicity",2,5,     0,        false,      1);
    SysFn[449] = new TFn("divmod",    2, 2,      0,        false,      1);
    SysFn[450] = new TFn("express",   2, 2,      0,        false,      1);
    SysFn[451] = new TFn("swaprows",  3, 3,      1,        true,       1);
    SysFn[452] = new TFn("swapcols",  3, 3,      1,        true,       1);
    SysFn[453] = new TFn("addtorows", 2, 3,      1,        false,      1);
    SysFn[454] = new TFn("addtocols", 2, 3,      1,        false,      1);
    SysFn[455] = new TFn("graphimage",2, 9,      1,        false,      1);
    SysFn[456] = new TFn("reflect",   5, 7,      1,        false,      1);



    //                     NAME,    MIN/MAXargs REFArgCnt  REFsRegd Hybrid

    for (int i=0; i < SysFnCnt; i++) SysFnNames.Add(SysFn[i].Name);
  }

//==============================================================================
// SYSTEM FUNCTION RUNNER
//------------------------------------------------------------------------------
// Args: for scalars, .I must be -1, and .X the value for computation. For arrays,
//  .I must be the pointer; .X will be ignored.
// Error: .B FALSE and .S has message. 
// Scalar functions will always return .I = -1, with computed value in .X; array
// functions will return .I as a Store[] pointer, and .X  as 0.
// Returned .I < -1 has significance in functions involved in running other programs. .I = -10, -20: close this program, 
//  and install and run the text in .S. (.I = -2 if .S contains contents of a user-named file; -3, if .S is current program, 
//  altered as user specifies.)
  public static Quad RunSysFn(short Which, params PairIX[] Args)
  { Quad result = new Quad(true); // Note the default: TRUE.
    result.I = -1; // ONLY CHANGE for array functions.
    int noREFs, NoArgs = Args.Length;
    if (NoArgs < SysFn[Which].MinArgs) return Oops(Which, "this function requires at least {0} args.", SysFn[Which].MinArgs);
    else if (NoArgs > SysFn[Which].MaxArgs) return Oops(Which, "this function cannot take more than {0} args.", SysFn[Which].MaxArgs);
    noREFs = SysFn[Which].REFArgCnt;
   // There has already been a check in unit Parser for REFArgCnt exceeding
   //  NoArgs, and therefore > REFArgLocn[] size.
   // For some functions, it is vital that REF args. be registered variables;
   //  for others it is not. The default will be the former...
    if (noREFs > 0)
    { int n = noREFs; if (NoArgs < n)n= NoArgs;//REF args can be optionally absent.
      // exclusions from the default check: (see top of unit, 'ExcludedFns')
      if (SysFn[Which].REFsRegd) // then guarantee that use of "V.GetVar.." functions won't crash, i.e. .Y is a valid function level:
      { int u = C.UserFnCnt;
        for (int i = 0; i < n; i++)
        { int f = REFArgLocn[i].Y;
          if (f < 0) return Oops(Which, "the {0}th. arg. must be a variable name", i+1);
                    // Usually it would be -600, which is R.TempScalarCode, i.e. signifies a numerical value resulting from earlier fn. solving.
          else if (f >= u) return Oops(Which, "the {0}th. arg. raised an unspecified error", i+1);
        }
      }
    }

  // - - - - - - - - - - - - - - - - - - - -
  // HYBRIDS:
    if (SysFn[Which].Hybrid == 2) // All 'Hybrid' methods currently take only 1 or 2 args., and the 2nd. is always scalar.
    // If more ever needed, will need to rewrite Quad Hybrids(..). They also all PRESERVE THE STRUCTURE of the 1st. argument, if it is an array.
    { double Y = 0.0; if (NoArgs > 1) Y = Args[1].X;
      int oldslot = Args[0].I;
      if (oldslot == -1) Hybrids(Which, Args[0].X, Y, ref result);//Scalar. Sets 'result'.
      else // array arg.:
      { StoreItem oldstoreitem = R.Store[oldslot];
        int arrsize = oldstoreitem.TotSz;
        int newslot = V.GenerateTempStoreRoom(oldstoreitem.DimSz);
        double[] olddata = oldstoreitem.Data, newdata = R.Store[newslot].Data;
        for (int i = 0; i < arrsize; i++)
        { Hybrids(Which, olddata[i], Y, ref result);
          if (!result.B) return result;
          else newdata[i] = result.X; }
        result.I = newslot;  result.X = 0.0;
      }
      return result; 
    }

  // NON-HYBRIDS:
    // These functions are divided currently across three files - this one, SysFns2.cs and SysFns3.cs.
    Quad result1;
    if (Which <= 200)
    { result1 = SystemFunctionSet_A(Which, NoArgs, Args, result); }
    else if (Which <= 400)
    { result1 = SystemFunctionSet_B(Which, NoArgs, Args, result); }
    else 
    { result1 = SystemFunctionSet_C(Which, NoArgs, Args, result); }
    return result1;
  }

  public static Quad SystemFunctionSet_A(int Which, int NoArgs, PairIX[] Args, Quad result)
  { 
    switch (Which)
    {
      case 1: case 2: case 3: // DIM  /  DIMLIKE  /  __ARRAY
      // All three take 'Victims' as first set of 1 or more args. These may be: (a) names not in use (either not occurring in code before 
      //  this, or else of variables eniolated by 'kill(.)'); (b) names of existing arrays. (Temporary arrays not allowed.)
      // The final args. depend on the function:
      //
      // DIM(array(s) Victims, scalar(s) Dimensions) -- 'Dimensions': outermost first (e.g. for matrix, NoRows precedes NoColumns).
      //   Dimensions are rounded; if the rounded version is <= 0, error raised.
      // __ARRAY (Victim(s)) -- same as if '__array(Victims)' were replaced by 'dim(Victims, 1). (If tracing its career, first search 
      //   unit Conformer for '"array', where 'array boo, hoo;' then look in unit Parser for '__array' or '//dim'.)
      // DIMLIKE(array(s) Victims, assigned array Model) -- if the dimensions of Model were (say) M3xM2xM1, this would be equivalent
      //   to 'dim(Victims, M3, M2, M1)' (M3 being the outermost dimension).
      { int slot = -1, firstscalar = -1, n;
        int spareMe = -1;  if (Which == 2) spareMe = NoArgs-1; // rescues the final array of 'dimlike(.)' from being eviscerated below.
        for (int i = 0; i < NoArgs; i++) // make sure all arrays are unassigned, up to the first scalar.
        { n = REFArgLocn[i].Y;
          if (n < 0 || n >= C.UserFnCnt)
          { if ( n == R.TempScalarCode) { firstscalar = i; break; } // a scalar value, the result of solving a function as argument to dim(.).
            else if (n == R.TempArrayCode) return Oops(Which, "attempt to dimension a temporary array");
            else if (n == P.SysFnMarker) return Oops(Which, "attempt to redefine a system function as an array");
            else if (n == P.UserFnMarker) return Oops(Which, "attempt to redefine a user function of this program as an array");
            else return Oops(Which, "proposed array name is somehow illegal");
          }
          int usage = V.GetVarUse(REFArgLocn[i].Y, REFArgLocn[i].X);
          
          if (usage != 0) // i.e. allow unassigned variables to bypass the following (as occurs after prior use of 'kill(.)' ).
                          // ##### The above one-line fix was added 15 Feb 10. Remove this cautionary note if no problems arise as a result.
          { if (usage < 10) 
            { if (Which == 1) { firstscalar = i;  break; } 
              else 
              { string ss = V.GetVarName(REFArgLocn[i].Y, REFArgLocn[i].X);
                return Oops(-1, "array declaration: '{0}' is a scalar", ss );
              }
            }
            if (i != spareMe) V.NeuterTheArray(REFArgLocn[i].Y, REFArgLocn[i].X); // No problem if array had not yet ever been assigned a storeroom.
          }
        }
        int[] DimSizes = null; 
        if (Which == 2) // DIMLIKE(..):
        { firstscalar = NoArgs-1; // it isn't really, but this value ensures that the general code at the end works properly.
          n = Args[firstscalar].I; // If this is -1 it doesn't (as usual) mean that the arg. is a scalar, but that it is an UNASSIGNED name,
                                   // which the twiddly bit in the Parser has converted to an unassigned array (see '//dim' in Parser).
          if (n == -1) return Oops(Which, "the last arg. is not a declared array");
          DimSizes = R.Store[n].DimSz._Copy();          
        }
        else if (Which == 3)  // ARRAY(..): set up as if there was a numerical argument of 1:
        { DimSizes = new int[1];  DimSizes[0] = 1;  
          firstscalar = NoArgs; // it isn't really, but this value ensures that the general code at the end works properly.
        }
        else // DIM(..): 
        { if (firstscalar < 1)
          { if (firstscalar == 0) return Oops(Which, "the first arg. has already been declared to be scalar");
            else return Oops(Which, "the last argument should be a scalar (dimension), but has not yet been declared");
          }
          if (NoArgs - firstscalar > TVar.MaxNoDims) return Oops(Which, "too many dimensions supplied");
          DimSizes = new int[NoArgs-firstscalar]; 
          for (int i = 0; i < DimSizes.Length; i++) 
          { DimSizes[i] = Convert.ToInt32(Args[NoArgs-i-1].X);
            if (DimSizes[i] <= 0) return Oops(Which, "a dimension is either zero or negative");
          }
        }
        // Applies whether DIM(..) or ARRAY(..):
        for (int arr = 0; arr < firstscalar; arr++)
        { n = REFArgLocn[arr].Y;
          if (n < 0) return Oops(Which, "syntax is really screwed up somehow"); // E.g. you call "Foo(array aa)", forgetting to remove word 'array'.
          slot = V.GenerateTempStoreRoom(DimSizes);
          V.TransferTempArray(slot, n, REFArgLocn[arr].X); // Even if Use was 0, it will be reset to 11 here.
        }
        break;
      }
      case 4: case 5: // X = INC(Y), X = DEC(Y). Meant to work with integer-like
      // values, hence output is rounded (to avoid numerical errors, whereby
      //  not 1 but 0.99999... is added to the number).
      // These are NOT PRIMARILY VOID functions. Therefore they must do 2 things:
      //  whether scalar or array arg., BOTH the registered variable must be
      //  updated AND the function must return a copy of its updated form.
      // **** IF EVER TEMPTED TO ALTER this fn., first see remarked notes at the end of unit Runner.
      { int oldslot = Args[0].I;
        if (oldslot == -1) // argument is scalar:
        { result.X = Args[0].X;
          if (Which == 4) result.X += 1.0; else result.X -= 1.0;
          result.X = Math.Round(result.X);
          int n = V.SetVarValue(REFArgLocn[0].Y, REFArgLocn[0].X, result.X);
          if (n < 0) // variable value refuses to be set (i.e. is a constant):
          { if (n == -1) return Oops(Which, "some unexpected program error");
            else return Oops(Which, "cannot change the value of a constant");
          }
        }
        else // argument is an array:
        { int arrsize = R.Store[oldslot].TotSz;  double xx;
          int newslot = V.GenerateTempStoreRoom(R.Store[oldslot].DimSz);
          double[] olddata = R.Store[oldslot].Data, newdata = R.Store[newslot].Data;
          for (int i = 0; i < arrsize; i++)
          { xx = olddata[i];
            if (Which == 4) xx += 1.0; else xx -= 1.0;  xx = Math.Round(xx);
            olddata[i] = xx;  newdata[i] = xx; }
               // as mentioned in heading, two stores are req'd for the same data.
          result.I = newslot;
        }
        break;
      }

//  DEALT WITH BY 'HYBRIDS(..)':
//    cases 6 through 12, and 14 through 27, and then later 48, 86, 351, 352, 376.

      case 13: // (1) ARCTAN(scalar/array YY): returns the arctan in the range -/+ PI/2 - i.e. always in the 1st. or 4th. quadrant.
               // (2) ARCTAN(scalars/arrays YY, XX): returns the angle(s) with tan = YY/XX, in the range -/+ PI - i.e. in any quadrant.
      // No problem in either case, if XX or YY is POSINF or NEGINF; the returned angle is as would be expected from geometry. However,
      // if XX = YY = 0, NAN is returned. (In the one-arg. version, if XX is NAN, NAN is returned.)
      { int slotY = Args[0].I;
        double[] yy = null;
        int len = 0;
        if (slotY >= 0) { yy = R.Store[slotY].Data._Copy();  len = yy.Length; }
        if (NoArgs == 1)
        { if (slotY == -1) { result.X = Math.Atan(Args[0].X);  break; }
          for (int i=0; i < len; i++) yy[i] = Math.Atan(yy[i]);
        }
        else // 2 args.
        { int slotX = Args[1].I;
          if (slotX == -1 ^ slotY  == -1) return Oops(Which, "the two args. must either both be scalars or both arrays");
          if (slotX == -1) { result.X = Math.Atan2(Args[0].X, Args[1].X);  break; }
          double[] xx = R.Store[slotX].Data; // Not a copy this time, but we won't be altering it.
          if (xx.Length != len) return Oops(Which, "the two array args. must have the same length");
          for (int i=0; i < len; i++) yy[i] = Math.Atan2(yy[i], xx[i]);
        }
        result.I = V.GenerateTempStoreRoom(len);
        R.Store[result.I].Data = yy;
        break;
      }
      case 28: // RAND(..) Three forms: (key: scalar 'N' determines type of random no.; 'X' is scalar; 'ArrName' is an array variable's name):
      // (1) RAND(N): NONVOID. N first rounded. Then, N <= 1 returns nonint, range 0 to 1; N > 1 returns integer, 0 to N-1.
      // (2) RAND(X, N [, NoRepetition] ): NONVOID. Returns a list array of size X, elements being in accord with N as above.
      // (3) RAND(ArrName, N [, NoRepetition]): VOID: The array is overwritten with values in accord with N as above.
      //  Re argument 'NoRepetition': (a) ignored for N <= 1, as natural repetition is unthinkably rare, and would waste time with conditional
      //    testing for whether the NoRepetition is turned on (even when it is off).
      //    (b) in the case of N > 1: If more unique values are requested than are possible (e.g. 'arr = rand(30, 3, true);"), error raised.
      // ERRORS: X < 1; ArrName not a registered array.
      { int N;
        if (NoArgs == 1)
        { N = Convert.ToInt32(Args[0].X);
          if (N <= 1) result.X = Rando.NextDouble();   else result.X = Rando.Next(N);
          break;
        }
      // TWO-ARG. FORMS:
        N = Convert.ToInt32(Args[1].X);
        double[] outdata; // In the first case below, generated as new entity; in second case, is an alias.
        int sz;
        int inslot = Args[0].I;
        // Case 1 - no preexisting array:
        if (inslot == -1) // then generate a new temp. array and return it:
        { sz = Convert.ToInt32(Args[0].X);
          if (sz < 1) return Oops(-1, "'rand(count, N)': 'count' must round to a positive integer");
          outdata = new double[sz];
        }
        else // a named array was passed:
        { int Fn = F.REFArgLocn[0].Y; // No need for .X here.
          if (Fn < 0) // Whoops! No named variable supplied
          { return Oops(-1, "'rand(Arr, N)': 'Arr' must be the name of an array that already exists"); }
          outdata = R.Store[inslot].Data;
          sz = outdata.Length;
        }
        // For both forms, now do the collecting of values:
        bool noRepeats = false;
        if (NoArgs > 2)
        { noRepeats = (Args[2].X != 0.0);
          if (noRepeats && sz > N)
          { return Oops(Which, "'No repetition' specified, but there cannot be {0} unique values between 0 and {1}", sz, N-1); }
        }

        if (N <= 1) { for (int i=0; i < sz; i++) outdata[i] = Rando.NextDouble(); }
        else // Integers required:
        { if (noRepeats)
          { // Two approaches: the first is [I think] more efficient where sz is of the same order as N, the 2nd. if sz << N.
            if (sz < 3*N) // *** choice of coefficient of N is arbitrary; testing could be done to get a better value.
            { int[] indices = JM.Shuffle(null, N, Rando);
              if (sz < N) indices = indices._Copy(0, sz);
              outdata = indices._ToDubArray();
              if (inslot >= 0) // If outdata was an alias, it has to be re-aliassed to the store item:
              { R.Store[inslot].Data = outdata; }
            }
            else
            { int n, i = 0;   bool isUnique;
              int[] values = new int[sz];
              while (i < sz)
              { n = Rando.Next(N);
                isUnique = true;
                for (int j=0; j < i; j++)
                { if (values[j] == n) { isUnique = false;  break; } }
                if (isUnique) { values[i] = n;  i++; }
              }
              outdata = values._ToDubArray();
              if (inslot >= 0) // If outdata was an alias, it has to be re-aliassed to the store item:
              { R.Store[inslot].Data = outdata; }
            }
          }
          else for (int i=0; i < sz; i++) outdata[i] = (double) Rando.Next(N);
        }
        // Assign a store item, for the case of no preexisting array. (Left till after the above, as an error can be raised there.)
        if (inslot == -1)
        { result.I = V.GenerateTempStoreRoom(sz);
          R.Store[result.I].Data = outdata;
        }
        break;
      }
      case 29: // SEED(X) - random generator seeder.
      { int n = (int) Math.Round(Args[0].X);
        if (n < 1) Rando = new Random(); // --> some C# time-dependent algorithm.
        else Rando = new Random(n);  break; }

      case 30: // DATA (Any mix of scalars or arrays)
      // All values are packed into a single R.Store[outslot].Data list array.
      { double[] outdata = AccumulateValuesFromArgs(Args, 0);
        int outslot = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[outslot].Data = outdata;
        result.I = outslot;  break; 
      }

    // DISPLAY FUNCTIONS -- All void, except 'Show' and 'Label'. All take any practical number of arguments, and all process them
    //  a/c to the same philosophy:
    //    Named variables, constants, expressions: value converted to string form. Pure literals: value 0xiiii --> char. '\uiiii'.
    //    Arrays: whether named or not, are displayed a/c to .IsChars; if TRUE, as chars.; FALSE, as space-comma delimited values.
    //  'statusbar' has a special arg; if arg is exactly "returncontent", nothing is written but the current content of LblComments
    //  is returned (or " ", if none). The returned content lacks any formatting instructions that might have been used to set it.
    // LABEL() / MAIN WINDOW TITLE: The first char. (preferably a separate argument) sets the label - 'A', 'R','C' or 'T' (case-sens).
    //  If no other chars. follow, the current content of the label is returned (the label text is unaltered). If a label, and the following
    //  text is exactly "_DEFAULT_", the label's default text is written. If following text is "_DEFAULT_yakyak", the default text is written,
    //  followed by 'yakyak'. In any other case, the remaining text simply replaces current label text.
    //  Note that if 'A' or 'R' apply, the change is always removed (elsewhere in MonoMaths) at the end of the program run; if 'C', it remains.
    // SHOW always returns an array; it returns text that was selected in the dialog box, or returns ' ' if none.
    //  If the concatenated args. of 'show' start with exactly "[EDITABLE]", then the display will be editable, and this cue will not be displayed.
      case 31:  case 51:  case 56:  case 34:
            // 31: SHOW(..) (--> msg. box); 51: WRITE(..) and 56: WRITELN(..) (--> REditRes);  34: LABEL(..) (--> labbelow REditRes).
      { string displaytext = ArgsToDisplayString(0, Args, REFArgLocn, "");
        if (displaytext[0] == '\u0000') return Oops(Which, "cannot display the text: " + displaytext._Extent(1));
      // SHOW(.):
        // In the case of SHOW(.), it is possible to add extra formatting to dictate size of message box...
        if (Which == 31)
        { // If 'setbox' was just used, BoxSize and/or BoxCentre will have values other than the default -1:
          bool boxWasSet = (JD.BoxSize.X > 0 || JD.BoxSize.Y > 0 || JD.BoxCentre.X > 0 || JD.BoxCentre.Y > 0);
          int closure; // Used to detect corner icon closure, which invokes 'hold' mode, thus offering escape from endless loops
          if (!boxWasSet) JD.PlaceBox(0.3, 0.3, -1, -1);
          bool editable = false;
          if (displaytext._Extent(0, 10) == "[EDITABLE]")
          { editable = true; displaytext = displaytext._Extent(10); }
          closure = JD.Display("", displaytext, true, true, editable, "CLOSE");
          if (closure == 0) // corner-icon closure, as means of escaping from eternal loop displaying this message.
          { MainWindow.StopNow = 'G'; R.LoopTestTime = 1; }// This value ensures that the very next end-of-loop test
               // will check for user's wish to end the program. But R.LoopTestTime will then be automatically reset to its orig. value of 100.
          else
          { double[] outdata;
            if (JD.SelectedText == "") outdata = new [] { 32.0 };
            else outdata = StringToStoreroom(-1, JD.SelectedText);
            result.I = V.GenerateTempStoreRoom(outdata.Length);
            R.Store[result.I].Data = outdata;
            R.Store[result.I].IsChars = true;
          }
        }
      // WRITE(.), WRITELN(.):
        else if (Which == 51 || Which == 56)
        { if (Which == 56) displaytext += '\n'; // WRITELN
          // ####### The following was added to cause display in the Results Window during run time, rather than at the end.
          // ####### If for some reason it is in some cases desirable to wait till the run finishes, then have another fn. name,
          // ####### and reintroduce the remarked-out line for it.
          MainWindow.ThisWindow.AddTextToREditRes(displaytext, "append");
        }
      // LABEL(.) or MAIN WINDOW TITLE:
        else if (Which == 34)
        { string ss = " ";
          int displaytextLen = displaytext.Length;
          char whichLabel = displaytext[0];
          if ("TRAC".IndexOf(whichLabel) >= 0)
          {
            if (displaytextLen == 1) // just return what's there:
            { ss = MainWindow.ThisWindow.AdjustLabelText(whichLabel, "", 'R');
              if (ss == "") ss = " ";
              result.I = V.GenerateTempStoreRoom(ss.Length);
              StringToStoreroom(result.I, ss);
              break; // separate exit for this case.
            }
            else if (displaytext._Extent(1, 9) == "_DEFAULT_")
            { if (displaytextLen == 10) // no subsequent text:
              { MainWindow.ThisWindow.AdjustLabelText(whichLabel, "", 'D'); }
              else // subsequent text, to append to the default text:
              { MainWindow.ThisWindow.AdjustLabelText(whichLabel, displaytext._Extent(10), 'E'); }
            }
            else MainWindow.ThisWindow.AdjustLabelText(whichLabel, displaytext._Extent(1), 'W');
          }
        }
        break;
      }
      case 32: // __SEGMT(REF InArray, scalar dimension indices) - returns a suitably structured array extracted from InArray. 
      // Returns a scalar if all indices are explicit, otherwise an array.  (When the user codes empty brackets '[ ]', at parse time this is
      // changed to [MINREAL], and becomes a full range indicator for the corresponding dimension.
      // Arguments are taken as occurring in descending order of array dimension.
      // Arrays returned are structured as MxNxPx..., where M, N, P.. are sizes of dimensions represented by full range indicators '[MINREAL]',
      //  in the order of their occurrence as fn. arguments.
      // Note that there is a time-saving shortcut for one-dim'l arrays with specific indices.
      // Troubleshooting hint: All service arrays developed during the run have LOWEST dimension as [0].
      {
        int n, p, inslot = Args[0].I, NoInDimensions = NoArgs-1;
        if (inslot == -1) return Oops(-1, "Square brackets '[.]' have followed either a scalar or a function name");
        // Get the details of the input array
        StoreItem itemInSlot = R.Store[inslot];
        int[] dimsizes = itemInSlot.DimSz;
        int nodims = itemInSlot.DimCnt;
        double[] indata = itemInSlot.Data;
        // Deal with dimensions. Some terminology first: a dimension may be 'omitted' (no corresponding argument); if supplied,
        //  it may be 'specified' - user used e.g. "[2]" - or 'unspecified' - user used either "[]" (or "[MINREAL]".
        if (NoArgs > nodims + 1) return Oops(-1, "source array has too many indices");
        // Put all the dimensioning args. int one array, reversed so that the lowest dimension is the last argument:
        int[] InDimensions = new int [nodims];
        int firstSpecified = -1; // will be the no. of the first specified dimension.
        int noUnspecified = 0;
        double x,  maxIntValue = (double) int.MaxValue, minReal = double.MinValue;
        for (int i=0; i < nodims; i++) // this rotates through dimensions in the order given; we must reverse that order.
        { n = nodims-i-1; // index for reversed order
          if (i >= NoArgs-1) // an omitted dimension, which is taken as a nonspecified dimension.
          { InDimensions[n]= -1; noUnspecified++; }
          else // supplied dimension:
          { x = Args[i+1].X;
            if (double.IsNaN(x)) return Oops(-1, "index of source is NaN");
            if (x <= -0.5) // so that it would round to a negative value:
            { if (x == minReal)
              { InDimensions[n] = -1; // the new cue for a nonspecified dimension (as minReal can't convert to an integer)
                noUnspecified++; // binary 1 added.
              }
              else return Oops(-1, "source array has a negative dimension");
            }
            else if (x > maxIntValue) return Oops(-1, "source array has an impossibly large dimension");
            else // specified dimension:
            { InDimensions[n] = p = Convert.ToInt32(x);
              if (p >= dimsizes[n]) return Oops(-1, "source array has a dimension out of range");
              firstSpecified = n; // will be progressively reduced
            }
          }
        }
        // Where an exact location is in view, a scalar is to be returned:
        if (noUnspecified == 0)
        { n = InDimensions[0];  p = dimsizes[0];
          for (int i = 1; i < NoInDimensions; i++)
          { n += InDimensions[i]* p;
            if (i < NoInDimensions-1) p *= dimsizes[i];
          }
          result.X = indata[n];  break;
        }
        // Define the output array:
        double[] outdata = null;
        int[] newDims = null;
        // Return the whole array, if no specified dimensions:
        if (firstSpecified == -1)
        { outdata = indata._Copy(); // The whole array is to be returned, as all dimensions are unspecified
          newDims = dimsizes;
        }

      // *** What follows is based on TVar.MaxNoDims being 3. If this is changed, rewrite all this stuff:
        // Only arrays of more than one dimension can get to this point, and only if they have unspecified dimension(s).
        else if (nodims == 2) // Deal with all matrices. They can only have args "[1][]" or "[][1]".
        { int norows = dimsizes[1], nocols = dimsizes[0];
          if (firstSpecified == 1) outdata = indata._Copy (InDimensions[1]* nocols,  nocols); // a row
          else
          { outdata = new double[norows];
            for (int i=0; i < norows; i++)  outdata[i] = indata[ i*nocols + InDimensions[0] ];
          }
          newDims = new [] {outdata.Length};
        }
        // Only 3D arrays with at least one unspecified dimension and at least one specified dimension are left.
        else if (nodims == 3) // *** just in case we do use more dimensions again later
        { int startat, extent;
          // Deal with single-strip cases first:
          if (firstSpecified == noUnspecified) // either "[1][][]" or "[1][1][]":
          { if (firstSpecified == 1) // "[1][1][]"
            { startat = InDimensions[2]*dimsizes[1]*dimsizes[0] + InDimensions[1]*dimsizes[0];
              extent = dimsizes[0];
              newDims = new [] {extent};
            }
            else // "[1][][]"
            { startat = InDimensions[2]*dimsizes[1]*dimsizes[0];
              extent = dimsizes[1] * dimsizes[0];
              newDims = new [] {dimsizes[0], dimsizes[1]};
            }
            outdata = indata._Copy(startat, extent);
          }
        // For the rest, it is just hard, time-consuming slog:
          else
          // Dimension the output array:
          { n = 1; // this will end up as the size of the output array
            int[] lo = new int[3],  abovehi = new int[3];
            newDims = new int[noUnspecified];
            int dimcntr = 0; // index of newDims.
            for (int i=0; i < 3; i++)
            { if (InDimensions[i] == -1)
              { n *= dimsizes[i]; lo[i] = 0;  abovehi[i] = dimsizes[i]; newDims[dimcntr] = dimsizes[i];  dimcntr++; }
              else { lo[i] = InDimensions[i];  abovehi[i] = lo[i]+1; }
            }
            outdata = new double[n];
            int dimsz1 = dimsizes[1], dimsz0 = dimsizes[0],   cntr = 0; // next index to receive data in output array.
            for (int i=lo[2]; i < abovehi[2]; i++)
            {
              int offset_i = i*dimsz1*dimsz0;
              for (int j=lo[1]; j < abovehi[1]; j++)
              {
                int offset_j = offset_i + j*dimsz0;
                for (int k=lo[0]; k < abovehi[0]; k++)
                {
                  outdata[cntr] = indata[offset_j + k];
                  cntr++;
                }
              }
            }
          }
        }
        else return Oops(Which, "Programming error:  __segmt(.) currently only handles up to 3 dimensions");
        // Return the array:
        int outslot = V.GenerateTempStoreRoom(newDims);
        R.Store[outslot].Data = outdata;
        R.Store[outslot].IsChars = itemInSlot.IsChars;
        result.I = outslot;  break; 
      }
      case 33: // __ASSIGN(REF Subject, scalars dimension indices, Donor) - where Donor is a scalar value (if a single location in InArray is
      // indicated) or a nonREF array (structure ignored). The size of Donor must exactly match the extent in InArray indicated by the indices.
      // Indices may be explicit (user used '[2]') or range indicators (user used '[ ]'; Parser would have corrected '[ ]' to
      // '[MINREAL]' before run time.  A shortcut is used for 1-dim'l arrays with specific indices.
      { int subjectslot = Args[0].I,   sourceslot = Args[NoArgs-1].I,   maxdims = TVar.MaxNoDims;
        if (subjectslot == -1) return Oops(-1, "scalars cannot have an index (like '[1]')");
        StoreItem itemSubject = R.Store[subjectslot];
        double[] subjectdata = itemSubject.Data;
      // Set up the donor data repository:
        double[] sourcedata;  
        if (sourceslot >= 0) sourcedata = R.Store[sourceslot].Data;  else  sourcedata = new double[1] {Args[NoArgs-1].X};
        int sourcelen = sourcedata.Length;
        int[] dimsizes = itemSubject.DimSz;
        int nodims = itemSubject.DimCnt;
        if (NoArgs > nodims + 2) return Oops(-1, "receiving array has too many indexes");
        double x, minReal = double.MinValue, maxIntAsDouble = (double) int.MaxValue;
        // Shortcut for one-dimensional arrays with explicit index:
        if (nodims == 1)
        { x = Math.Round(Args[1].X);
          if (x != minReal)
          { if (x < 0.0 || x >= (double) dimsizes[0] || Args[1].I != -1)
            { string ss = V.GetVarName(itemSubject.ClientFn,  itemSubject.ClientVarNo); 
              if (x < 0.0) return Oops(-1, "receiving array '{0}' has a negative dimension", ss);
              else if (Args[1].I != -1) return Oops(-1, "the index for the receiving array '{0}' is itself an array", ss);
              else return Oops(-1, "receiving array '{0}' has an oversized dimension", ss);
            }
            subjectdata[(int) x] = sourcedata[0];
            break; // EXIT FOR 1-DIM ARRAYS WITH EXPLICIT INDEX
          }
        }
        // Set up dimensioning arrays and limits:
        int[] maxval = new int[maxdims],  minval = new int[maxdims]; // [0] of these will represent the LOWEST dimension.
        for (int i=0; i < nodims; i++) { maxval[i] = dimsizes[nodims-i-1]-1; } // dimsizes[] entries over this range guaranteed nonzero.
        int[] argmts  = new int [maxdims]; // [0] will end up as the LOWEST dimension.
        // Collect dimension arguments together into an array. This needs a little thought. Suppose the args are "(Arr, m,n)" for a
        //  structure of 3 dims; then we want to end up with argmts - from high dim. down - as 0 0 m n -1, the final -1 standing in for
        //  omitted dimns. (i.e. the user's 'Arr[m][n]' and 'Arr[m][n][]' are equivalent). We build this array of dimensions up in three
        //  stages: Stage 1 - stick in the zeroes, for virtual dimensions beyond the actual dimensions of the array; Stage 2 - 
        //  stick in the arguments; Stage 3 - fill up the rest of the array (if any room left) with -1's.
        // Stage 1 -- pop in the zeroes for virtual arguments:
        int n, nextindex = maxdims - nodims; // will be used at end of 1st. and 2nd. loops to avoid horrendous expressions.
        // Stage 2 -- scoop up the dimension arguments:
        int fullDimension = -1; // Should be negative, but particular value is not important.
        for (int j = 1; j < NoArgs-1; j++)
        { n = fullDimension; // n remains so if x = minReal.
          x = Math.Round(Args[j].X);
          if (x != minReal)
          { if (x < 0.0 || x > maxIntAsDouble || Args[j].I != -1)
            { string ss = V.GetVarName(itemSubject.ClientFn,  itemSubject.ClientVarNo); 
              if (x < 0.0) return Oops(-1, "receiving array '{0}' has a negative dimension", ss);
              else if (Args[j].I != -1) return Oops(-1, "an index for the receiving array '{0}' is itself an array", ss);
              else return Oops(-1, "receiving array '{0}' has an oversized dimension", ss);
            }
            n = (int) x; // guaranteed >= 0.
          }
          argmts[nextindex + j - 1] = n;
        }
        nextindex += NoArgs - 2;
        // Stage 3 -- tack on 'fullDimension' for any unspecified indices:
        for (int k = nextindex; k < maxdims; k++) argmts[k] = fullDimension;
        // Finally, get the reverse of this, so that 'argmts' is in ASCENDING order, like maxval[] and minval[] are.
        Array.Reverse(argmts);
        // Now set maxval and minval, and in the process check for oversized arguments.
        for (int i=0; i < nodims; i++) 
        { n = argmts[i];
          if (n == fullDimension) maxval[i] = dimsizes[i]-1; // leave minval[i] at its startup value of 0.
          else if (n >= dimsizes[i])
          { string ss = V.GetVarName(itemSubject.ClientFn,  itemSubject.ClientVarNo); 
            return Oops(-1, "receiving array '{0}' has oversized dimension '{1}'", ss, n);
          }
          else { minval[i] = n;    maxval[i] = n; }
        }
        // All desired array spots now lie between index ranges (inclusive) specified by minval[] and maxval[].
        // Note that in the loops below, where minval[n] and maxval[n] are both 0, (a) if these are for dimensions beyond those of
        //  this array, then 'offsetn' below will be 0, so the loops for these unnecessary outer dims. will have no effect on the answer.
        // (b) if they are for dimensions within the set of actual array dimensions, they will have the desired effect on the answer.
        // Get the stuff from sourcedata to subjectdata:
        int sourcecntr = 0;
        int offset2, offset1;
        int sizer0 = dimsizes[0], sizer1 = dimsizes[1]*sizer0;
        for (int dim2 = minval[2]; dim2 <= maxval[2]; dim2++)
        { offset2 = dim2 * sizer1;
          for (int dim1 = minval[1]; dim1 <= maxval[1]; dim1++)
          { offset1 = dim1 * sizer0;
            for (int dim0 = minval[0]; dim0 <= maxval[0]; dim0++)
            { if (sourcecntr >= sourcelen) return Oops(-1, "donor array segment does not have enough data to assign to the receiving array segment");
              subjectdata[offset2 + offset1 + dim0] = sourcedata[sourcecntr];
              sourcecntr++;
            }
          }
        }
        if (sourcecntr != sourcelen) return Oops(-1, "donor array segment has too much data to fit the receiving array segment");
        break;
      }
//   case 34: STATUSBAR -- see case 31
     case 35: // REDIM(Any no. of Victim arrays; 1 to 5 scalar Dimensions following).
     case 36: // REDIMLIKE(Any no. of Victim arrays; one final Model array). Differs only in that 'Dimensions' are read from the Model.
      // NONVOID functions. Victims must have been declared previously as arrays. Data is conserved as possible: excess data truncated,
      // insufficient data padded with final zeroes.
      // RETURNS (a) if a single victim array, a SCALAR which = (new array size - original size). (b) If > 1 victims, an ARRAY of the same.
      // Note that the settings for these functions do not exclude temporary arrays. There is absolutely no point in redimensioning
      // a temporary array, but there is nothing here to stop you doing so.
      {// Check argument deployment:
        List<int> scalars = null;   int n;
        for (int i = 0; i < NoArgs; i++)
        { if (Args[i].I == -1) // scalar found:
          { if (Which == 36) return Oops(Which, "only array args. allowed");
            else // must be 'redim':
            { if (scalars == null) scalars = new List<int>();
              if (i == 0) return Oops(Which, "no arrays precede scalars");
              n = (int) Math.Round(Args[i].X);
              if (n <= 0) return Oops(Which, "zero or negative dimensions not allowed");
              scalars.Add(n);
            }
          }
          else // an array arg, so check that no scalars preceded it:
          { if (scalars != null) return Oops(Which, "scalar dimensions must follow all array args."); }
        }
        // Set up the model parameters:
        int[] modelDimSz;
        int modelTotSz,  modelDimCnt, noVictims;
        if (Which == 35) // 'redim' - so dimensions explicitly provided:
        { if (scalars == null) return Oops(Which, "no scalar dimensions provided");
          if (scalars.Count > TVar.MaxNoDims) return Oops(Which, "too many dimensions supplied");
          noVictims = NoArgs - scalars.Count;
          modelDimSz = new int[TVar.MaxNoDims];
          scalars.Reverse(); // so that the last arg. supplied ( = smallest dimension) will be the lowest element of the array.
          int[] scallywag = scalars.ToArray();
          modelDimCnt = scallywag.Length;
          modelTotSz = 1;
          for (int i=0; i < modelDimCnt; i++) modelTotSz *= scallywag[i];
          scalars.ToArray().CopyTo(modelDimSz, 0);
        }          
        else // 'redimlike' - so read dimensions from the model: 
        { noVictims = NoArgs-1;
          StoreItem model = R.Store[Args[NoArgs-1].I]; // the last array
          modelDimSz = model.DimSz._Copy();
          modelTotSz = model.TotSz;  modelDimCnt = model.DimCnt;
        }
        // Loop through the victim arrays, adjusting the data (lopping it or extending it with zeroes) if nec.:
        int[] NewMinusOrigSz = new int[noVictims];
        int oldTotSz;
        StoreItem victim;
        double[] oldData, victimData;
        for (int i = 0; i < noVictims; i++)
        { victim = R.Store[Args[i].I];
          oldTotSz = victim.Data.Length;
          // Alter the dimensioning parameters of the victim array:
          modelDimSz.CopyTo(victim.DimSz, 0);
          victim.DimCnt = modelDimCnt;
          NewMinusOrigSz[i] = modelTotSz - oldTotSz;
          // Now resize the data array, if necessary:
          if (oldTotSz != modelTotSz)
          { oldData = victim.Data._Copy();     
            victim.Data = new double[modelTotSz];   victimData = victim.Data;
            if (oldTotSz < modelTotSz) // then just overwrite old data into the earlier part of newdata, leaving zeroes at the end.
            { oldData.CopyTo(victimData, 0); }
            else // write as much as possible of old data into the smaller new data space, using the slower "Array.Copy(.)":
            { Array.Copy(oldData, victimData, modelTotSz); }
          }
        }
        // Return something:
        if (noVictims == 1) result.X = NewMinusOrigSz[0];
        else
        { result.I = V.GenerateTempStoreRoom(noVictims);
          R.Store[result.I].Data = NewMinusOrigSz._ToDubArray();
        }
        break;
      }
      case 37: // void FILL(Arr, <polynomial coeffs. from X^0 to X^N>, OR array fn. FILL(ArrSize, ,poly..>) (i.e. 1st. arg. scalar).
         // E.g. fill(Arr, 1, 2, 3) puts [3X^2 + 2X + 1], from x = 0 to x = size of Arr, into the Data field of Arr.
         // No changes made to other storeroom fields.
      { int revs, slot;
        if (Args[0].I == -1) // scalar, so 1st. arg. is size of new array:
        { revs = Convert.ToInt32(Args[0].X);
          if (revs < 1) return Oops(Which, "size arg. must be at least 1");
          // generate new temp. array:
          slot = V.GenerateTempStoreRoom(revs);
          result.I = slot;
        }
        else 
        { slot = Args[0].I;   revs = R.Store[slot].TotSz; }
         // Generate the series, storing it in xxx[]:
        double[] xxx = new double[revs];  double zz;
        for (int i = 0; i < NoArgs-1; i++)
        { zz = Args[i+1].X; // No test for arrayhood. Too bad if it is.
          for (int j = 0; j < revs; j++)
          { if (i==0)  xxx[j] = zz;  else if (i==1) xxx[j] += zz*j;
            else xxx[j] += zz * Math.Pow(j, i);
          }
        }
         // Fit the series into the given Arr:
        double[] slotdata = R.Store[slot].Data;
        for (int i = 0; i < revs ; i++) slotdata[i] = xxx[i];
        break;
      }
      case 38: // __QUOTE(QuotationIndex). "__quote(23)" will generate a temporary chars. array from Quotations[23].
      { int n = Convert.ToInt32(Args[0].X);
        string ss = P.Quotation[n];
        int ssLen = ss.Length;
        if (ssLen == 0) { return Oops(Which, "Programming disaster!!"); } // *** Remove after a discrete interval (from Dec 2012)
        result.I = V.GenerateTempStoreRoom(ssLen);
        StringToStoreroom(result.I, ss);
        break;
      }
      case 39: // POLYSTRING( Poly array [, DecPts (1-15) [, UseFormattedSuperscripting [, IndependentVariableText ]]] )
      // DecPts: <1 or >15 defaults to Math2.CS's default. U.F.S: if present and TRUE, formatting tags for superscripting are included.
      // If the fourth arg. is present and is an array, its content is taken as unicode values of a string, which is reproduced in each
      // term (degree 1+) as the independent variable. (The default is 'X'.)
      { int polyslot = Args[0].I;  if (polyslot == -1) return Oops(Which, "1st. arg. must be the polynomial as an array");
        string fmtstr = "";
        if (NoArgs > 1){ int n = Convert.ToInt32(Args[1].X);  if (n >= 1 && n <= 15) fmtstr = "G" + n.ToString(); }
        bool useREfmt = (NoArgs > 2 && Args[2].X != 0.0);
        string indepVar = "X";
        if (NoArgs > 3 && Args[3].X != -1.0)
        { int ivslot = (int) Args[3].I;
          indepVar = StoreroomToString(ivslot);
        }
        string ss = M2.PolyToStr(R.Store[polyslot].Data, fmtstr, indepVar, useREfmt);
        if (ss == "") ss = " "; // happens if polynomial is just '0'.
        result.I = V.GenerateTempStoreRoom(ss.Length);
        StringToStoreroom(result.I, ss);  R.Store[result.I].IsChars = true;
        break; 
      }
      case 40:  case 41:  case 42:// GRIDX, GRIDY, GRIDZ (scalar GraphID; 3 to 4 values, however packed:
      //                                                              LowValue, HighValue, NoCuts [, RememberTheOriginal ] ).
      // If 'RememberTheOriginal' present and TRUE, then the graph does NOT forget the prior scaling, so that menu item "Scaling | Original..."
      //  will revert to the scale PREEXISTING the call to "grid..(.)". In all other cases - as for ALL CASES OF "grid(.)" - the internal
      //  record of the original scale is replaced with the new value. (To overcome this, you would need to replace 'grid(.)' with
      //  separate calls "gridx(...); gridy(....);". 
      case 74:                    // GRID(scalar GraphID; 6 or 9 vals. in any form) - 6 for 2D. 9 for 3D.   NONVOID.
      // RETURNED: If success, an array of size 3 or 6 (2D) or 3 or 9 (3D), depending on which function was called. This array
      //  may conveniently be used as the argument for a later call to grid..(.) for a different graphing event.
      { NextXCornerReal = 0.0;  NextXTipReal = 0.0;  NextXSegments = 0; // Nulled whether or not other errors abort below.
        NextYCornerReal = 0.0;  NextYTipReal = 0.0;  NextYSegments = 0;
        NextZCornerReal = 0.0;  NextZTipReal = 0.0;  NextZSegments = 0;
        double[] values = AccumulateValuesFromArgs(Args, 1);
        int noValues = values.Length;
        int graphID = (int) Args[0].X;
        Graph graf;    Trio trill;
        // Deal with GRID(.) FIRST:
        if (Which == 74)
        { if (noValues != 6  &&  noValues != 9) { return Oops(Which, "the wrong no. of values has been supplied"); }
          trill = Board.GetBoardOfGraph(graphID, out graf);
          if (graf != null)
          { graf.OrigLowX = graf.LowX = values[0];  graf.OrigHighX = graf.HighX = values[1];
            graf.OrigSegsX = graf.SegsX = Convert.ToInt32(values[2]);
            if (graf.SegsX <= 0) return Oops(Which, "no. of X segments can't be less than 1");
            graf.OrigLowY = graf.LowY = values[3];  graf.OrigHighY = graf.HighY = values[4];
            graf.OrigSegsY = graf.SegsY = Convert.ToInt32(values[5]);
            if (graf.SegsY <= 0) return Oops(Which, "no. of Y segments can't be less than 1");
            if (NoArgs >= 9)
            { graf.OrigLowZ = graf.LowZ = values[6];  graf.OrigHighZ = graf.HighZ = values[7];
              graf.OrigSegsZ = graf.SegsZ = Convert.ToInt32(values[8]);
              if (graf.SegsZ <= 0) return Oops(Which, "no. of Z segments can't be less than 1");
            }
            Board.ForceRedraw(trill.X, false);
          }
        }
        else // GRIDX, GRIDY, GRIDZ:
        { if (noValues < 3 || noValues > 4) { return Oops(Which, "incorrect no. of values has been supplied"); }
          trill = Board.GetBoardOfGraph(graphID, out graf);
          if (graf != null)
          { int n = Convert.ToInt32(values[2]);
            if (n <= 0) return Oops(Which, "no. of segments can't be less than 1");
            bool resetOrig = (values.Length <= 3  ||  values[3] == 0.0);
            if (values.Length > 3) values = values._Copy(0, 3);
            if      (Which == 40)
            { graf.LowX = values[0];  graf.HighX = values[1];  graf.SegsX = n;
              if (resetOrig) { graf.OrigLowX = values[0];  graf.OrigHighX = values[1];  graf.OrigSegsX = n; }
            }
            else if (Which == 41)
            { graf.LowY = values[0];  graf.HighY = values[1];  graf.SegsY = n;
              if (resetOrig) { graf.OrigLowY = values[0];  graf.OrigHighY = values[1];  graf.OrigSegsY = n; }
            }
            else if (Which == 42)
            { graf.LowZ = values[0];  graf.HighZ = values[1];  graf.SegsZ = n;
              if (resetOrig) { graf.OrigLowZ = values[0];  graf.OrigHighZ = values[1];  graf.OrigSegsZ = n; }
            }
            Board.ForceRedraw(trill.X, false);
          }
        }
        // The return array:
        int newslot = V.GenerateTempStoreRoom(values.Length);
        R.Store[newslot].Data = values;
        result.I = newslot;
        break;
      }
      case 43: // KILLPLOT(any number of values, as scalars or arrays) - if values are identifiable current plots,
      // those plots are (a) removed from MainWindow.Plots2D&3D; and (b) removed from all boards in the Boards.BoardsRunning list.
      // If they can't be identified, they are ignored without raising an error.
      { double[] args = AccumulateValuesFromArgs(Args);
        int[] plotIDs = args._ToIntArray();
        // remove from any boards containing it:
        int[] boardsrunning = Board.CurrentBoards(); // If no board running, 'boardsrunning' will be an empty array.
        for (int i=0; i < boardsrunning.Length; i++)
        { Graph spee = Board.GetFocussedGraph(boardsrunning[i]);
          if (spee != null) spee.RemovePlotsByID(plotIDs);
        }
        // remove from MainWindow lists:
        MainWindow.RemovePlots(23, plotIDs);
        break;
      }
      case 44: // KILLGRAPHS( array/scalar GraphID, array DoWhatWithPlots). VOID. GraphID: if array, holds IDs of graphs to be killed; OR,
      // if exactly the word "all" (case-sensitive), then all graphs will be slaughtered.
      // (GraphIDs start from Board.GraphIDBase, currently 10000, and are way above these unicodes, so ambiguity not possible.)
      // 'DoWhatWithPlots': only the first element is checked; it has 3 allowed values (anything else defaults to the 3rd.):
      //     '!' = kill all plots that are contained in these graphs, whether or not they are also present in other graphs.
      //     '?' = query plots; if present in other graphs, leave them; if only present in these graphs, kill them. (The slowest option.)
      //     '-' (or anything else) = leave all plots.
      // Where GraphID = "all" there is no point in querying plots, so all will be killed if DWWP is either '?' or '!', left otherwise.
      // *** Note that 'lastclosed()' called after ANY of these operations will return 0 (see comment re same at end of the code below. 
      //  If this gets to you, recode as directed in that comment.
      { 
        double[] graphIDs;
        int idslot = Args[0].I;
        if (idslot == -1) graphIDs = new double[]{ Args[0].X };
        else graphIDs = R.Store[idslot].Data;
        int wotslot = Args[1].I;  if (wotslot == -1) return Oops(Which, "2nd. arg. must be an array"); 
        double doWhat = R.Store[wotslot].Data[0];
        bool killAllPlots = (doWhat == 33.0); // "!"
        bool queryPlots = (doWhat == 63.0); // "?"
        bool leavePlots = (!killAllPlots && !queryPlots);
        bool killAllGraphs = (graphIDs[0] == 97.0  &&  graphIDs[1] == 108.0  &&  graphIDs[2] == 108.0); // i.e. if graphIDs starts with "all":
        if (killAllGraphs)
        { Board.KillBoards(); // Kills all boards and their contained refs. to plots and graphs.
          if (!leavePlots)
          { MainWindow.Plots2D.Clear();
            MainWindow.Plots3D.Clear();
          }
          break;
        }
        // For the rest, we have to go through the whole rigmerole...
        Graph graf;
        int graphID;
        for (int i=0; i < graphIDs.Length; i++)
        { graphID = Convert.ToInt32(graphIDs[i]);
          if (graphID < Board.GraphIDBase) continue; // don't waste time with impossible graph ID.
          Trio treeoh = Board.GetBoardOfGraph(graphID, out graf);
          if (graf == null) continue;
          int bawd = treeoh.X,  noGraphsInBawd = treeoh.Z;
          List<int> killMainWindowReferences = null;
          if (!leavePlots) // then we have to remove values from Plots2D / 3D as well:
          { killMainWindowReferences = graf.PlotsExtantIDs; // All plots in the graph start off on the list of the Condemned.
            if (queryPlots) // then some of the condemned may get a reprieve:
            { Trio[] allplots = Board.ListAllPlotsInAllBoards().ToArray(); // new array copy, as the list will usually change with each loop
              foreach (Trio trot in allplots)
              { if (trot.X != bawd || trot.Y != graphID) // we are only interested in graphs other than the one we are killing
                { int n = killMainWindowReferences.IndexOf(trot.Z);
                  if (n >= 0) killMainWindowReferences[n] = 0; // This plot of the dying graph will not be removed from Plots2D / 3D.
                }
              }
            }
          }
          // First, remove the graph from its board; this will leave our ref. 'graf' as the last reference to the graph anywhere.
          Board.RemoveGraph(bawd, graphID);
          // Next strip graf of all of its own references to plots:
          graf.RemovePlots();
          // Now execute graf:
          graf = null;
          // Remove plot references from Plots2D / 3D, if slated for removal:
          if (killMainWindowReferences != null)
          { MainWindow.RemovePlots(23, killMainWindowReferences.ToArray()); }
          // Now kill the board, unless the board has other graphs:
          if (noGraphsInBawd == 1) Board.KillBoards(bawd); // Note that as 'bawd' was removed after NoGraphsInBawd was set, in fact
          // the condition means: "if this board has no graphs...". The call to KillBoards will set Board.LastKilledBoard.Y to 0,
          // because it is empty; this means that a call to 'lastclosed()' will now return 0. I am happy to leave it that way for now,
          // as 'lastclosed()' is usually used to see if the user has NONPROGRAMMATICALLY closed a graph by clicking on its top right icon.
          // But if you must change it, manually adjust Board.LastKilledBoard after this comment (it is public static).
        } 
        break;
      }
      case 45:  // ADDPLOT (scalar GraphID, any no. of PlotIDs as scalars or arrays). VOID. Use for 2D or 3D.
      {// Identify the graph:
        int graphID = (int) Args[0].X;
        Graph griffin;
        Board.GetBoardOfGraph(graphID, out griffin);
        if (griffin == null) break; // simply nothing happens, if the graph is lost to posterity.
        double[] plotstuff = AccumulateValuesFromArgs(Args, 1);
        Plot[] flot = MainWindow.GetPlotPointers(23, plotstuff._ToIntArray());
        griffin.AddPlot(flot);
        break;
      }
      case 46: // SAVE(array/scalar SaveType, array Data, array FileName, array WhatIfFileExists, ...  <up to 3 further args.>):
      // SAVE( 'A', Data, FN, WIFE [, ApplyPrefix [, Description [, DefaultExtension ] ] ] ) -- Saves an array, each element being
      //         translated by MONO to a block of 8 bytes. If ApplyPrefix present and true, saves a specially formatted prefix block
      //         with data about the saved variable; the data and prefix are together retrievable by 'load('A', ..., true)', which see.
      // SAVE( 'B', Data, FN, WIFE [, dummy, dummy [, DefaultExtension] ] ) -- save as bytes. ONLY WORKS IF all data rounds to values
      //         in the range 0 to 255; otherwise an error is raised.
      // SAVE( 'F', Data, FN, WIFE, VarNameInFile [, dummy [, DefaultExtension] ]) -- saves data in human-readable text format.
      //         May be appended to file holding other such variables. THIS IS THE ONLY VARIANT OF 'SAVE' WHICH saves scalar values
      //        (i.e. here alone, 'Data' is allowed to be scalar).
      // SAVE( 'T', Data, FN, WIFE [, dummy, dummy [, DefaultExtension] ] ) -- load data, convert all bytes to unicodes, and return
      //        all data as a chars.-rated list array.
      // ARGS:
      //   Data: Typically would be an assigned variable's name; but expressions are happily accepted. (Cannot be scalar for 'save'.)
      //     In the case of SaveType = 'T', must be convertible to Unicode (or anything might happen, including a crash);
      //     In the case of SaveType = 'B', all data MUST be in the byte range (0 to 255 on rounding), or error is raised.
      //   FileName: If no path, CurrentDataPath will be applied. Path shortcuts recognized: "~/", "./", "../".
      //     Leading and trailing white spaces and char. '\u0000' are trimmed off FileName before use.
      //     If FileName = just spaces OR "?", opens a file-chooser dialog box.
      //   WhatIfFileExists: Options are: 'O', 'o' (letters, not numeral zero) --> Overwrite without asking; '?' --> Warn, with option
      //     to overwrite or append or abandon [in fact, any char. but A,a,O,O will trigger this]; 'A', 'a' --> Append without asking.
      //   ApplyPrefix: Defining detail of the array (name, if an assigned variable; dimensions; chars. rating) are prefixed to the file
      //     before the data, with a time stamp; all is in a form that will be recognized by 'load(.)' with the 'ExpectPrefix' option set.
      //   Description: If present as an array, and if ApplyPrefix TRUE, then this goes into the 'name' part of the prefix; otherwise
      //     the name of the variable (if registered) goes there instead, or the empty string (for temporary array).
      //   VarNameInFile: No abbreviations, e.g. for name of TheData. However, formatting is lax; only rules - (1) no outer spaces (internal
      //     ones fine, but outer ones will be trimmed off before use), and (2) no colon (but other punctuation is fine). Case-sensitive.
      //     NB!! There is no check, either at save or at load time, for duplication of var. name in an appended file; if two or more
      //     variables have exactly the same VarNameInFile implanted into the text file, only the first will ever be read back.
      //   DefaultExtension: If present and an array, will be added to the file dialog box chosen name IF that name has no '.' anywhere in it.
      // RETURNED: scalar FALSE or TRUE (success of saving). If FALSE, iok() will also return FALSE, and iomessage() will detail the error.
      //   If saving was successful, iok() will return TRUE and iomessage() will contain the full path and file name.
      { // Reset error flags:
        IOError = false;   IOMessage = "";    IOFilePath = "";
        double x;
        char fileType;
        int fileTypeSlot = Args[0].I;
        if (fileTypeSlot == -1) x = Args[0].X;  else x = R.Store[fileTypeSlot].Data[0];
        if (x > 96.0) x -= 32.0;
        x = Math.Round(x);
        if      (x == 65.0) fileType = 'A';  else if (x == 66.0) fileType = 'B';
        else if (x == 70.0) fileType = 'F';  else if (x == 84.0) fileType = 'T';
        else return Oops(Which, "unrecognized 1st. argument (i.e. type of loading function)");
        int dataSlot = Args[1].I;
        // Get file name:
        int filenameslot = Args[2].I;
        if (filenameslot == -1) return Oops(Which, "2nd. arg. must be an array (the file name)");
        string filename = StoreroomToString(filenameslot, true, true);
        string[] flame = filename._ParseFileName(MainWindow.ThisWindow.CurrentPath);
        if (filename == "" || filename[0] == '?' || flame[1] == "") // Then a dialog box is required:
        { string filepath = flame[0]; if (filepath == "") filepath = MainWindow.ThisWindow.CurrentPath;
          Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog("SAVING DATA", null,
               Gtk.FileChooserAction.Save, "Cancel", Gtk.ResponseType.Cancel, "Open", Gtk.ResponseType.Accept);
          fc.SetCurrentFolder(filepath);
          int outcome = fc.Run();
          filename = fc.Filename;
          fc.Destroy(); // Without this, the FileChooserDialog window won't get closed.
          if (outcome != (int) Gtk.ResponseType.Accept || filename.Trim() == "")
          { result.I = EmptyArray();
            IOError = true; // BUT IOMessage remains as "", this being the way that cancellation of dialog box is recognized.
            break; // with result.X returning FALSE.
          }
        }
        else filename = flame[0]+ flame[1]; // Nec. where user has used an allowed abbreviation, handled by ._ParseFileName(.).
        // Check if there is to be a default extension:
        if (NoArgs > 6 && Args[6].I != -1 && filename.IndexOf('.') == -1)
        { string defaultExtension = StoreroomToString(Args[6].I, true, true);
          if (defaultExtension[0] != '.') defaultExtension = '.' + defaultExtension;
          filename += defaultExtension;
        }
        if (filename[filename.Length-1] == '.') filename = filename._Extent(0, filename.Length-1);
        // Action to take if file exists:
        if (Args[3].I == -1) return Oops(Which, "4th. arg. must be an array");
        x = R.Store[Args[3].I].Data[0];
        char whatIfFileExists = '?'; // the default - ask.
        if (x == 79.0 || x == 111.0) whatIfFileExists = 'O'; // 'O', 'o'
        else if (x == 65.0 || x == 97.0) whatIfFileExists = 'A'; // 'A', 'a'.
        // Get variable's contents:
        if (fileType == 'A')
        { if (dataSlot == -1) return Oops(Which, "scalar values can only be saved using the variant 'save('F', ...)'");
          string inputVarName = "";
          string formatAs = "data";
          if (NoArgs > 4  &&  Args[4].X != 0.0) // APPLY PREFIX
          { formatAs = "identified data";
            if (NoArgs > 5 && Args[5].I >= 0) // then we don't use the variable's name:
            { inputVarName = StoreroomToString(Args[5].I, false, false); }
            else if (F.REFArgLocn[1].Y >= 0) // then this is a registered array, so we can look for its name:
            { inputVarName = V.GetVarName(F.REFArgLocn[1].Y, F.REFArgLocn[1].X); }
          }
          Boost boorish = SaveVariable(dataSlot, 0.0, filename, whatIfFileExists, formatAs, inputVarName);
          if (!boorish.B) { IOError = true;  IOMessage = boorish.S;  }
          else { result.X = 1.0;    IOFilePath = filename; } // Successful save operation.
        }
       // SAVE AS READABLY-FORMATTED TEXT
        else if (fileType == 'F')
        { if (NoArgs < 5) return Oops(Which, "with save mode 'F', 5 args. are required (the last being the variable's name in the file)");
          string varNameInFile = StoreroomToString(Args[4].I, true, true);
          if (varNameInFile == "" || varNameInFile.IndexOf(':') != -1)
          { return Oops(Which, "5th. arg. must (a) have chars. other than spaces, and (b) no colon (':')"); }
          Boost boohoo = SaveVariable(dataSlot, Args[1].X, filename, whatIfFileExists, "text", varNameInFile);
          if (!boohoo.B) { IOError = true;  IOMessage = boohoo.S; }
          else // successful save operation:
          { result.X = 1.0;  IOFilePath = filename; }
        }
       // SAVE AS BYTES
        else if (fileType == 'B')
        { double[]bights = R.Store[dataSlot].Data;
          byte[] beadle = new byte[bights.Length];
          for (int i=0; i < bights.Length; i++)
          { x = bights[i];
            if ( (x < 0.0 && Math.Round(x) != 0.0) || (x > 255.0 && Math.Round(x) != 255.0) )
            { IOError = true;  IOMessage = "for 'save('B', ...), all values to save must round into the range 0 to 255";  break; }
            beadle[i] = Convert.ToByte(x);
          }
          if (IOError) break; // as the above break only broke out of the FOR loop.
          FileStream fs;
          try
          { if (whatIfFileExists == 'A')
            { fs = File.Open(filename, FileMode.Append); } // If no existing file, will create a new one.
            else
            { fs = new FileStream(filename, FileMode.Create); } // Overwrites without warning.
            fs.Write(beadle, 0, beadle.Length);
            fs.Flush();
            fs.Close();
          }
          catch
          {  IOError = true;  IOMessage = "could not save data to '" + filename + "'"; break; }
        }
        else // fileType 'T':
        { string intext = StoreroomToString(dataSlot, false, false); // DON'T strip off leading and trailing characters.
          intext = intext.Trim('\u0000'); // The one character that must go off both ends is this one.
          if (intext == "") { IOError = true;  IOMessage = "there was no text to be saved"; break; }
          bool append = false;
          if (File.Exists(filename))
          { if (whatIfFileExists == 'A') append = true;
            else if (whatIfFileExists != 'O')
            { int btn = JD.DecideBox("FILE ALREADY EXISTS", "A file named '" + filename + "' already exists. What action do you want?",
                  "OVERWRITE", "APPEND", "CANCEL");
              if (btn == 0 || btn == 3) { IOError = true;  IOMessage = "";  break; }
              append = (btn == 2);
            }
          }
          try
          { StreamWriter w;
            if (append) w = File.AppendText(filename);
            else w = File.CreateText(filename);
            w.Write(intext); // Don't ever use .WriteLine, as the 'B' option in particular requires that we don't add a final par. mark.
            w.Flush();   w.Close();
          }
          catch
          {  IOError = true;  IOMessage = "could not save data to '" + filename + "'";  break; }
        }
        if (!IOError) { result.X = 1.0;    IOFilePath = filename; }
        break;
      }
      case 47: // DATASTRIP (array Array, int AbsoluteIndex [, double X] ) -- Directly reads from or writes to the named argument.
      // 2-arg. version: returns Array[AbsoluteIndex]. 3-arg. version: VOID, but sets Array[AbsoluteIndex] to X.
      { int slot = Args[0].I;  if (slot == -1) return Oops(Which, "attempt to address a scalar as if it were an array");
        int ndx = Convert.ToInt32(Args[1].X);
        double[] arraydata = R.Store[slot].Data;
        if (ndx < 0 || ndx >= arraydata.Length) return Oops(Which, "index out of range");
        if (NoArgs == 2) result.X = arraydata[ndx]; // READ
        else arraydata[ndx] = Args[2].X; // WRITE
        break;
      }
   // case 48: DEFRAC(..) - dealt with under Hybrids
      case 49:  case 50:// CHARS(any no. of Arrays), UNCHARS(any no. of Arrays): void functions which set .IsChars. to TRUE / FALSE.
                        // Scalar args. don't provoke an error; they are simply ignored.
      { for (int i=0; i < NoArgs; i++)
        { int slot = Args[i].I;
          if (slot >= 0) R.Store[slot].IsChars = (Which == 49);
        }
        break;
      }
  //  case 51: - WRITE(Array) - See Case 31: (SHOW).

      case 52: // HEX( scalar / array Value(s) [, HowManyForcedLeftDigits ] -- returns up to 16 hex digits per value. If Value is scalar,
      // just that (ignoring any decimal portion). If an array, values are concatenated such that lower indexed elements appear to the
      // left in the displayed string. Each single value can be up to 16 digits. There are no prefixed characters (like "0X" or "H").
      // There is no way, from looking at the output, of telling how many values were input. Typically, array Values might be an R,G,B
      // colour array: e.g. [255, 254, 253], which - for a 2nd. arg. of 2 - would produce "FFFEFD".
      { string hexstr, ss = "X";
        if (NoArgs > 1 && Args[1].X >= 1.0) ss += ((int)Args[1].X).ToString();
        if (Args[0].I == -1)  hexstr = ((long)Args[0].X).ToString(ss);
        else // Values is an array:
        { hexstr = "";
          double[] values = R.Store[Args[0].I].Data;
          for (int i=0; i < values.Length; i++)
          { hexstr += ((long) values[i]).ToString(ss);
          }
        }
        int slot = V.GenerateTempStoreRoom(hexstr.Length);
        double[] data = R.Store[slot].Data;
        for (int i = 0; i < hexstr.Length; i++) data[i] = (double) hexstr[i];
        R.Store[slot].IsChars = true;  result.I = slot;  break; 
      }

     case 53: // UNHEX(chars. array HexString [, bool ToInt16 ]) -- converts a string of hex chars. to a value. If no 2nd. arg.,
        // or 2nd. arg. is FALSE, will provide positive values for strings up to 7fff ffff ffff ffff, larger
        // hex values producing a negative result. If ToInt16 present and true, strings beyond 7fff ffff will produce a negative result.
        // HexString is allowed to contain spaces and tabs, but otherwise only hex digits. Crashes if not properly formatted.
        // If HexString has more than 16 chars (or 8, if ToInt16 true), only the lower 16 (or 8) chars. will be used, the rest ignored.
      { int slot = Args[0].I;   if (slot == -1) return Oops(Which, "an array argument is required");
        string hexno = StoreroomToString(slot);   hexno = hexno._Purge(JS.WhiteSpaces); // removes any internal spaces.
        if (hexno == "") break; // empty string, so just return 0.
        bool allwell;
        if (NoArgs > 1  &&  Args[1].X != 0.0) result.X = (double) hexno._ParseHex(out allwell);
        else result.X = (double) hexno._ParseHexLong(out allwell);
        if (!allwell) return Oops(Which, "hex string not properly formatted");
        break;
      }
      case 54: // LADDER(..). Three versions:
      // Three arguments: LADDER(scalar Size OR NAMED array ArrayToFill,  scalar LowestValue,  scalar HighestValue)
      //   1st. Arg. SCALAR: RETURNS an array of that (rounded) size, made of equally spaced values from LowestValue to HighestValue inclusive.
      //   1st. Arg. NAMED ARRAY: Fills that array as above with no change of structure; RETURNS the interval between elements.
      // Four arguments:  LADDER(scalar Interval,  scalar LowestValue,  scalar HighestValue,  bool ConformInterval)
      //   RETURNS an array holding LowestValue = i * Interval, i = 0, 1, 2, ... If there is an exact fit, all is fine.
      //   Otherwise behaviour depends on ConformInterval. If TRUE, then Interval is adjusted to the nearest value which will
      //   ensure that the last value will be included. If FALSE, the original Interval stands, but HighestValue instead is adjusted,
      //   such that the actual highest value H is less than requested HighestValue H' if H + Interval would exceed H'. (This means that
      //   a numerical error could exclude the HighestValue, even though it theoretically would be included. To avoid this you might
      //   add a tad to HighestValue.)
      { double loval = Args[1].X,  hival = Args[2].X; 
        if (NoArgs == 3)
        { int slot = Args[0].I, arrsz; bool returnsarray = false;
          if (slot == -1)// then this form creates and returns the array:
          { arrsz = Convert.ToInt32(Args[0].X); if (arrsz < 1) return Oops(Which, "the returned array must have a size of at least 1");
            slot = V.GenerateTempStoreRoom(arrsz);
            returnsarray = true;
          }
          else // the first arg. is an array. It should be a named one...
          { if (REFArgLocn[0].X < 0) return Oops(Which, "1st. arg., if an array, must be an existing variable");
            arrsz = R.Store[slot].TotSz;
          }
         // generate the data:
          double[] xx = new double[arrsz];
          xx[0] = loval;    xx[arrsz-1] = hival;
          double interval = 0.0;
          if (arrsz > 1)interval = (hival-loval)/ ((double)(arrsz-1));
          for (int i = 1; i < arrsz-1; i++) xx[i] = loval + i*interval;
          R.Store[slot].Data = xx;
         // assign as appropriate:
          if (returnsarray) result.I = slot; else result.X = interval;
        }
        else // Four argument version
        { double interval = Args[0].X; // Can be positive or negative, but not zero.
          if (interval == 0.0) return Oops(Which, "1st. arg. (the interval) cannot be zero");
          double noSegs = (hival - loval) / interval;
          if (noSegs <= 0.0) return Oops(Which, "the range is either zero or is of opposite sign to the interval");
          bool conformInterval = (Args[3].X != 0.0);
          int arrsz;
          if (conformInterval)
          { double n = Math.Round(noSegs);
            if (n < 0.0) return Oops(Which, "this combination of interval and final value would produce an infinitely large array");
            if (n < 1.0) n = 1.0;
            interval = (hival - loval) / n;
            arrsz = 1 + (int) n;
          }
          else
          { arrsz = 1 + (int) Math.Floor( (hival - loval) / interval);
          }      
          double[] xx = new double[arrsz];
          for (int i = 0; i < arrsz; i++) xx[i] = loval + i*interval;
          if (conformInterval) xx[arrsz-1] = hival; // just in case of tiny numerical errors.
          result.I = V.GenerateTempStoreRoom(arrsz);
          R.Store[result.I].Data = xx;
        }
        break;
      }
      case 55: // BIN(scalar Value [, scalar GroupSize [, char. array Separator ] ] ). 'Value' (rounded) must be representable
      // as an Int32 value (see 'unhex8' notes re values - same applies re translating from + or - no. to binary digits).
      // 'GroupSize', if present and >= 1, causes the string length to be a multiple of GroupSize; and if 'Separator' is
      // present, groups of that size will be delimited by the separator.
      { string hexery = (Convert.ToInt32(Args[0].X)).ToString("X");
        int groupSize = 0;   if (NoArgs > 1) groupSize = Convert.ToInt32(Args[1].X);
        string delim = "";
        if (NoArgs > 2) delim = StoreroomToString(Args[2].I, false, false); // don't trim it.
        string binstr = JS.HexStrToBits(hexery, groupSize, delim);
        int slot = V.GenerateTempStoreRoom(binstr.Length);
        StringToStoreroom(slot, binstr);
        R.Store[slot].IsChars = true;
        result.I = slot;  break; 
      }
  //  case 56: - WRITELN(..) - See Case 31: (WRITE / SHOW).
      case 57: // SIZE(..): Two forms: (1) one arg.: SIZE(Variable). Returns
        // total size of an array, or 0 for scalars. (2): two args.:
        // 'size(Variable, dimension)' returns size of that dimension. Error
        // ( = impossible dimension) returns -1 (except for scalar var., -> 0)
      { int slot = Args[0].I;
        if (slot == -1) result.X = 0.0;
        else if (NoArgs == 1) result.X = (double) R.Store[slot].TotSz;
        else
        { int n = (int) Args[1].X;
          if (n < 0 || n >= TVar.MaxNoDims)result.X = -1.0;
          else result.X = (double) R.Store[slot].DimSz[n]; }
        break; }


      case 58: // DIMS(Variable, [IncludeZeroes]): Returns an array of variable's dimensions. Normally excludes zero upper dimensions,
      // but if the 2nd. argument is included and is nonzero, they are all there.
      // Scalars return the array [0], ignoring any second argument.
      { int len = 1,  oldslot = Args[0].I;
        double[] outdata = null;
        if (oldslot == -1) outdata = new double[1]; // SCALAR 1st. argument, so generate an all-zeroes array of appropriate length:
        else
        { if (NoArgs > 1 && Args[1].X != 0.0) len = TVar.MaxNoDims;  else len = R.Store[oldslot].DimCnt;
          outdata = new double[len];
          int[] foo = R.Store[oldslot].DimSz;
          for (int i = 0; i < len; i++){ outdata[i] = (double) foo[i]; } 
        }
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        break; 
      }
      case 59: // NEAT(DataToDisplay [, scalar or char. array Format [, scalar or char. Tabbing ] ] )
      // If DataToDisplay is scalar, this simply displays it according to the format string information.
      // Structured arrays are displayed as rows. If one arg. only, each value is displayed unformatted, and values are delimited by commas.
      // 'Format': If scalar and rounds to 0 to 15 inclusive, sets dec. places. using .NET's format string "Fn", where n is the scalar arg. 
      //   If an array, is passed directly to .NET's format string, in a 'try..catch' loop.
      //   Dummy, if wanting a 3rd. arg. but not this second arg.: set it to '-1' (or anything negative).
      // 'Tabbing' - Scalar (rounded): if <= 0, ignored: no tabbing, and the default delimiter is used. Values 0 to 9: that many default-step
      //   tabs delimit each value pair. Values >= 10 are taken instead as tabbing step.
      //   Array: (a) Starts with 'C' or 'c': displays contiguous letters.
      //    (b) Starts with 'T' or 't' - the number of 'T'/'t's is the number of default-step tabs between elements.
      //     If no starting 'c' or 't', or 'c' but array not a chars. array: No tabbing: original nontab delimiter is used.       
      { string displayStr = "";
        // Retrieve formatting data first up, as the case of a scalar 1st. arg. will need it:
        string formatStr = "";
        if (NoArgs > 1)
        { int fmtslot = Args[1].I;
          if (fmtslot == -1) // scalar formatting value:
          { double x = Args[1].X;
            if (x > -0.5 && x < 15.5) // then it will round to 0 to 15 inclusive:
            { int n = (int) Math.Round(x);
              formatStr = "F" + n.ToString();
            }
          }  
          else formatStr = StoreroomToString((int) Args[1].I, true, true, true);
        }                  
        // Identify data to display:
        int inslot = Args[0].I;
        if (inslot == -1) // Scalar 1st. argument - only action is to format it.
        { try { displayStr = Args[0].X.ToString(formatStr); } 
          catch { return Oops(Which, "illegal format string supplied for the scalar value"); }
        }
        else // Array 1st. argument:
        {// Get the tabbing information:
          int defaultTabSize = 50,  tabSize = 0; // '0' = no tabbing; '-1' = no tabbing, but is chars.; > 0 = tab step size.
          if (NoArgs > 2)
          { int tabslot = Args[2].I;
            if (Args[2].I == -1) // 3rd. arg. is a scalar:
            { double x = Args[2].X;
              if (x > -0.5 && x < 500) // then it will round to a sensible value (though should be nowhere near 500)
              { int n = (int) Math.Round(x);
                if (n < 10) tabSize = defaultTabSize * n;
                else tabSize = n;
              }  
              else tabSize = 0; // Any negative  values from the user will be set to 0, so neg. codes are available for special use below.
            }
            else // 3rd. arg. is an array:
            { if (R.Store[tabslot].IsChars) // ignore tabbing, if not a chars. array
              { string ss = StoreroomToString(tabslot).ToUpper();
                char c = ss[0];
                if      (c == 'C') tabSize = -1; // code for forcing chars. in strings. (User values below 0 were converted to 0 above.)
                else if (c == 'N') tabSize = -2; // code for forcing values in strings.
                else if (c == 'T') { int n = ss._CountChar(c);  tabSize = defaultTabSize * n; }
              }                
            }
          }
          // Set up the display:
          try
          { if (tabSize > 0)
            { int n = R.Store[inslot].DimSz[0];
              if (tabSize < 10) tabSize *= defaultTabSize;
              string tabsetting = "";
              for (int i=1; i <= n; i++) { tabsetting += (i * tabSize).ToString();  if (i < n) tabsetting += ", "; }
              displayStr = V.StoreroomDataFormatted(inslot, true, true, false, formatStr, "\t", "\t", "N", tabsetting).S;
            }
            else if (tabSize == -1) // force chars.:
            { displayStr = V.StoreroomDataFormatted(inslot, true, true, false, formatStr, "", "", "C").S;
            }
            else if (tabSize == -2) // force numbers:
            { displayStr = V.StoreroomDataFormatted(inslot, true, true, false, formatStr, ", ", "", "N").S; }
            else // (tabSize = 0, so no tabbing, and let natural chars.numbers rating apply:
            { displayStr = V.StoreroomDataFormatted(inslot, true, true, false, formatStr, ", ").S; }
          }
          catch { return Oops(Which, "format string illegal"); }
        }
        // Legal display string exists:
        int newslot = V.GenerateTempStoreRoom(displayStr.Length);
        StringToStoreroom(newslot, displayStr);
        result.I = newslot;  break;
      }
      case 60: case 61: case 89: //COPY(Arr, FromPtr[, Extent]), COPYTO(Arr,FromPtr,ToPtr), BETWEEN(Arr,FromPtr,ToPtr).
      // 'between(Arr, a, b)" is equivalent to "copyto(Arr, a+1, b-1)".
      // Returns an array in all cases; never crashes. If arguments ask the impossible, or logically would produce a null array,
      //  it returns an array of size 1 and value NaN, detectable with bool. fn. 'empty(Arr)'.
      // Automatic adjustments without error: FromPtr < 0 --> 0;  Extent / ToPtr too large --> max. valid value.
      // Adjustments setting [0] to NaN as mentioned: (1) FromPtr > ToPtr; (2) Extent <= 0.
      // The new array takes on the same .IsChars as the old one (except the dummy array). However the new array is ALWAYS a list array.
      {// Develop basic params:
        int oldslot = Args[0].I;  if (oldslot==-1) return Oops(Which, "1st. arg. should be an array");
        int oldlen = R.Store[oldslot].TotSz;
        int fromptr = Convert.ToInt32(Args[1].X), extent = -1;   bool emptyarray = false;
        if (fromptr < 0) { if (Which == 89) fromptr = -1; else fromptr = 0; }
        else if (fromptr >= oldlen) emptyarray = true;
        if (!emptyarray)
        { if (Which == 60) // copy(.):
          { extent = oldlen - fromptr; // default (2 args); also the max. allowed value, for downscaling 3rd. arg.
            if (NoArgs > 2) { int n = (int) Args[2].X;  if (n < extent) extent = n;         }
          }
          else if (Which == 61) // copyto(.):
          { int toptr = Convert.ToInt32(Args[2].X);
            if (toptr >= oldlen) toptr = oldlen-1;
            extent = toptr - fromptr + 1;
            if (extent <= 0) emptyarray = true; // from either copy(.) or copyto(.)
          }
          else // between(.):
          { int toptr = Convert.ToInt32(Args[2].X);
            if (toptr > oldlen) toptr = oldlen;
            fromptr++;
            extent = toptr - fromptr;
          }
          if (extent <= 0) emptyarray = true; // from either copy(.) or copyto(.)
        }
        if (emptyarray){ result.I = EmptyArray();  break; }
        // If got here, valid data can be returned:
        int newslot = V.GenerateTempStoreRoom(extent);
        Array.Copy(R.Store[oldslot].Data, fromptr, R.Store[newslot].Data, 0, extent);
        R.Store[newslot].IsChars = R.Store[oldslot].IsChars;
        result.I = newslot;  break; 
      }
      case 62: case 63: case 64: // FIND / FINDS / FINDALL(ArrayToSearch, scalar StartPtr, (array or scalar) SearchFor [, (scalar) OtherLimit].
      // All three take the same arguments; they differ only in what they return. 
      // THE STRUCTURE OF ArrayToSearch IS IGNORED; finds are returned as absolute addresses within its data strip.
      // StartPtr: Crashes, if this is negative. Does not crash, if StartPtr beyond end of ArrayToSearch; simply returns the code for failed search.
      // Four argument form: 3rd. must be scalar, and represents one limit (e.g. the lower limit) for values
      // answering to the search; OtherLimit is then the other (e.g. the higher limit). Both inclusive.
      // RETURNED: For 'find(.)', a scalar - position of the find, or -1 if no find. For 'finds(.)', an
      //  array of fixed size: [0] = no. finds (0 if none); [1] = locn. of first find (or -1); [2] =
      //  locn of last find (or -1). For 'findall(.)', a list array of length N, where N is the number of finds; 
      //  [i] is the locn of the ith. find. (Their values can be accessed using fn. 'select(.)'.) If no finds,
      //  returns an array of size 1, [0] = -1.
      { int argtypes = ArgTypesCode(Args);  
        char findtype = 'D'; if (Which == 63) findtype = 'S'; else if (Which == 64) findtype = 'A'; // (fin)D, (find)S, (find)A(ll).
        if (argtypes != 211 && argtypes != 212 && argtypes != 2111) return Oops(Which, "arg. types are not correct"); // 2 = array, 1 = scalar
        int inslot = Args[0].I,  startptr = (int) Args[1].X;
        if (startptr < 0) return Oops(Which, "the pointer to search start is negative");
        int inlen = R.Store[inslot].TotSz;  double[] indata = R.Store[inslot].Data;
        double[] finds = null;  if (findtype == 'S') finds = new double[] {0.0,  -1.0,  -1.0};
        List<double> findptrs = null; if (findtype == 'A') findptrs = new List<double>();
        int foundat=-1;
        if (argtypes == 212) // search for an array:
        { int soughtslot = Args[2].I;
          int soughtlen = R.Store[soughtslot].TotSz;  double[] soughtdata = R.Store[soughtslot].Data;
          int ptr = startptr;
          while (ptr <= inlen - soughtlen)
          { foundat = -1;
            if (indata[ptr] == soughtdata[0])
            { foundat = ptr;
              for (int j=1; j < soughtlen; j++){ if (indata[ptr+j] != soughtdata[j]){ foundat = -1; break; } }// break from FOR loop, NOT from fn.
            }
            if (foundat >= 0)
            { if (findtype == 'D') break; // 'finD(.)'  Break from WHILE loop, NOT from fn.
              else if (findtype == 'S') // 'findS(.)'
              { finds[2] = (double)ptr;  finds[0] = finds[0] + 1.0;  if (finds[0] == 1.0) finds[1] = finds[2]; }
              else if (findtype == 'A')findptrs.Add( (double)ptr); // 'findAll(.)':
              ptr += soughtlen-1; // '-1' because 'ptr++' is coming up real soon now.
            }
            ptr++;
          }
        }
        else
        { foundat = -1;
          double searchlo = Args[2].X, searchhi = searchlo;  
          if (NoArgs == 4) searchhi = Args[3].X;
          if (searchhi < searchlo){ double x = searchlo;  searchlo = searchhi;  searchhi = x; }
          for (int i=startptr; i < inlen; i++)
          { if (indata[i] >= searchlo && indata[i] <= searchhi) 
            { foundat = i;  
              if (findtype == 'D') break; // 'finD(.)'    Break from FOR loop, NOT from fn.
              else if (findtype == 'S')
              { finds[2] = (double)i;  finds[0] = finds[0] + 1.0;  if (finds[0] == 1.0) finds[1] = finds[2]; }
              else if (findtype == 'A') findptrs.Add((double)i); // 'findAll(.)'
            }
          }
        }
        if (findtype == 'D') result.X = (double)foundat;
        else if (findtype == 'S') { result.I = V.GenerateTempStoreRoom(3); R.Store[result.I].Data = finds; }
        else if (findtype == 'A') 
        { int n = findptrs.Count;
          if (n == 0){ result.I = V.GenerateTempStoreRoom(1); R.Store[result.I].Data[0] = -1.0; }
          else { result.I = V.GenerateTempStoreRoom(n); findptrs.CopyTo(R.Store[result.I].Data); }
        }  
        break; // The ONLY break which is a break from this fn.        
      }
      case 65: // TRANSPOSE(Array) - Returns transposed copy of Array. If DimCnt is 1 - list array - Array is regarded as
      // a row vector; a column vector is returned. DimCnt must not be > 2.
      { int oldslot = Args[0].I;
        if (oldslot == -1) return Oops(Which, "a scalar arg. was supplied");
        int nodims = R.Store[oldslot].DimCnt;
        if (nodims  > 2) return Oops(Which, "arrays of more than two dimensions not allowed");
        int oldcols, oldrows;
        if (nodims == 1) { oldrows = 1; oldcols = R.Store[oldslot].TotSz; }
        else { int[] dimsz = R.Store[oldslot].DimSz;  oldcols = dimsz[0];   oldrows = dimsz[1]; }
        int offset, newslot = V.GenerateTempStoreRoom(oldrows, oldcols);
        double[] olddata = R.Store[oldslot].Data,  newdata = R.Store[newslot].Data;
        for (int i = 0; i < oldrows; i++)
        { offset = oldcols * i;
          for (int j = 0; j < oldcols; j++) newdata[j * oldrows + i] = olddata[offset + j];
        }
        R.Store[newslot].IsChars = R.Store[oldslot].IsChars;
        result.I = newslot;  break; }
      case 66: // DOT(Arr1, Arr2) - dimensions of arrays not considered; but they must have the same length.
      { int slot1 = Args[0].I,  slot2 = Args[1].I;
        if (slot1 == -1 || slot2 == -1) return Oops(Which, "two array args. required");
        double[] data1 = R.Store[slot1].Data,  data2 = R.Store[slot2].Data;
        int len = data1.Length; if (len != data2.Length) return Oops(Which, "array args. must have the same length");
        double xx = 0.0;
        for (int i = 0; i < len; i++)  xx += data1[i] * data2[i]; 
        result.X = xx;  break; }
      case 67: // MXMULT(Arr1, Arr2 [, Array Opn] ) - returns matrix product of two matrices. Slight concession: if one arg.
      // is a list array, it is regarded as a ROW vector, but as such must conform to the usual mx. mult. rules.
      // If a 3rd. arg, represents an alternative operation. "*" is just plain multn. (duplicating absence of 3rd. arg.);
      //  'max' returns the maximum of each pair of values being 'multiplied'; 'min' minimizes same. Unrecognized Opn --> crasho.
      // Opn is case-insensitive.
      { int slot1 = Args[0].I,  slot2 = Args[1].I;
        if (slot1==-1 || slot2==-1) return Oops(Which, "scalar args. not allowed");
        int dimcnt1  = R.Store[slot1].DimCnt, dimcnt2 = R.Store[slot2].DimCnt;
        int[] dimsz1 = R.Store[slot1].DimSz,  dimsz2 = R.Store[slot2].DimSz;
        if (dimcnt1 > 2 || dimcnt2 > 2) return Oops(Which, "arrays with more than two dimensions not allowed");
        if (dimsz1[1] == 0) dimsz1[1] = 1;   if (dimsz2[1] == 0) dimsz2[1] = 1; // regard list arrays as row vectors.
        int inner = dimsz1[0];
        if (inner != dimsz2[1]) return Oops(Which, "matrix dimensions do not match");
        int norows = dimsz1[1],   nocols = dimsz2[0];
        int newslot = V.GenerateTempStoreRoom(nocols, norows);
        // What operation?
        int operation = 0; // default - multiplication.
        string opstr = "";
        if (NoArgs > 2)
        { opstr = StoreroomToString(Args[2].I, true, true); // trimmed fore and aft.
          opstr = opstr.ToLower();
          if (opstr == "*") operation = 0;
          else if (opstr == "max") operation = 1;   else if (opstr == "min") operation = 2;
          else return Oops(Which, "unrecognized operation '{0}'", opstr);
        }
       // OK, so multiply.
        double[] Column = new double[inner];  double z, xx;  int offset;
        double[] slot1data = R.Store[slot1].Data,  slot2data = R.Store[slot2].Data;
        double[] newdata = R.Store[newslot].Data;
        for (int cl = 0; cl < nocols; cl++)
        {// develop the ith. column vector of the 2nd. matrix:
          for (int j = 0; j < inner; j++)
          { Column[j] = slot2data[cl + j*nocols]; }
          // get dot product of each row vector with this column.
          for (int rw = 0; rw < norows; rw++)
          { xx = 0.0;  offset = rw*inner;
            if (operation == 0) // ordinary matrix multiplication:
            { for (int j = 0; j < inner; j++)
              { xx += slot1data[offset + j] * Column[j]; }
            }
            else if (operation == 1) // 'max' multiplication:
            { xx = slot1data[offset];
              for (int j = 0; j < inner; j++)
              { z = Math.Max(slot1data[offset + j], Column[j]); 
                if (z > xx) xx = z;
              }
            }
            else if (operation == 2)
            { xx = slot1data[offset];
              for (int j = 0; j < inner; j++)
              { z = Math.Min(slot1data[offset + j], Column[j]); 
                if (z < xx) xx = z;
              }
            }
            newdata[cl + rw * nocols] = xx;
          }
        }
        result.I = newslot;  break;
      }
      case 68: case 69: case 70: case 71: // MAX / MAXAT / MAXABS / MAXABSAT (Var,Var,Var,....) - arrays and scalars can be mixed.
      // MAX returns a scalar, the signed maximum value.  MAXAT returns a scalar, the ABSOLUTE value of the element with the maximum ABSOLUTE value;
      //   that is, the sign is lost. If you want the sign, you have to use 'maxabsat(.)' instead.
      // MAXAT returns an array of size 3:
      //     [0] = signed maximum value; [1] = no. of argument containing it (to base 0);  [2] = ABSOLUTE index within that argument.
      // MAXAT returns an array of size 4:
      //     [0] = the maximum absolute value - ALWAYS POSITIVE; [1] = no. of argument containing it (to base 0);  
      //     [2] = ABSOLUTE index within that argument;  [3] = the SIGNED version of [0]; i.e. the actual element indicated by [1] and [2].
      { int slot;
        double x, maxo, maxosigned = 0.0; // 'maxo' is used in the algorithm; 'maxosigned' simply holds a return value.
        int argno = 0, pos = 0; // locators of the max. value (for MAXAT(.))
          // Set to 0, in the unlikely event that all values of all args. are
          //  double.MinValue, so that loops below do not reset them.
        if (Which <= 69) // MAX, MAXAT:
        { maxo = double.MinValue;
          for (int var = 0; var < NoArgs; var++)
          {//If a scalar, check .X field against current maxo:
            if (Args[var].I == -1)
            { if (Args[var].X > maxo)
              { maxo = Args[var].X;  argno = var;  pos = 0; } }
            else //If an array, check through its entries:
            { slot = Args[var].I;   int totSz = R.Store[slot].TotSz;
              double[] xx = R.Store[slot].Data;
              for (int i = 0; i < totSz; i++)
              { if (xx[i] > maxo) { maxo = xx[i];  argno = var;  pos = i; } }
            }
          }
        }
        else // MAXABS, MAXABSAT:
        { maxo = 0.0;
          for (int var = 0; var < NoArgs; var++)
          {//If a scalar, check .X field against current maxo:
            if (Args[var].I == -1)
            { x = Args[var].X;
              if (x > maxo || x < -maxo)
              { maxosigned = x;  maxo = Math.Abs(maxosigned);  argno = var;  pos = 0; }
            }
            else //If an array, check through its entries:
            { slot = Args[var].I;   int totSz = R.Store[slot].TotSz;
              double[] xx = R.Store[slot].Data;
              for (int i = 0; i < totSz; i++)
              { x = xx[i];
                if (x > maxo || x < -maxo)
                { maxosigned = x;  maxo = Math.Abs(maxosigned);  argno = var;  pos = i; }
              }
            }
          } 
        }
       // Set up the return values
        if (Which == 68 || Which == 70) result.X = maxo; // MAX(.), MAXABS(.); the latter always returns a positive value, as explained in header.
        else // MAXAT(.) and MAXABSAT(.) returns array with localizing data as well:
        { 
          double[] outdata;
          if (Which == 69) // MAXAT
          { outdata = new double[] {maxo, argno, pos};
          }
          else // MAXABSAT
          { outdata = new double[] {maxo, argno, pos, maxosigned};
          }
          int newslot = V.GenerateTempStoreRoom(outdata.Length);
          R.Store[newslot].Data = outdata;
          result.I = newslot;
        }
        break;
      }
      case 72: // SORT(Array, [WhichWay, [StartAt, [EndAt]]]): VOID fn., which preserves the structure and chars. rating of Array.
      // Optional arguments: WhichWay >= 0, or omitted, --> ascending; < 0 --> descending. StartPtr, EndPtr: all between 
      //  and including ptrs. is in the sort. Out-of-range ptrs. are corrected; but crossed ptrs. --> crash.
      //  No check for scalarity of 2nd. arg. onwards. 
      { int oldslot = Args[0].I;
        if (oldslot==-1) return Oops(Which, "an array arg. is required");
        int len =R.Store[oldslot].TotSz;
        bool ascending = true; int startptr = 0, endptr = len - 1;
        if (NoArgs > 1 && Args[1].X < 0.0) ascending = false;
        if (NoArgs > 2) startptr = (int) Args[2].X;
        if (NoArgs > 3) endptr = (int) Args[3].X;
        if (startptr > endptr) return Oops(Which, "pointers are crossed");
        if (len == 1) break; // no point in trying to sort an array of length 1.
        JS.Sort(R.Store[oldslot].Data, ascending, startptr, endptr);
        break;
      }
      case 73: // SORTBYKEY(DataArray1 [DataArray2 [, DataArray3, ... ] ], KeyArray, [scalar WhichWay, [scalar StartAt, [scalar EndAt]]] -- 
      // VOID. Preserves the structure and chars. rating of DataArrays. If more than one, they MUST all be of the same length; however
      //  structure is ignored.
      // Optional scalar arguments: WhichWay >= 0, or omitted, --> ascending; < 0 --> descending. StartPtr, EndPtr: all between 
      //  and including ptrs. is in the sort. Out-of-range ptrs. are corrected; but crossed ptrs. --> crash.
      // If ptrs. supplied, only the part of KeyArray within the ptrs. is used.
      // Note that this function also sorts the key array, so copy it first if you want to retain the old version.
      { 
        int firstScalar = NoArgs; // default for no scalars.
        for (int i=0; i < NoArgs; i++)
        { if (Args[i].I == -1)
          { firstScalar = i;  break; }
        }
        if (firstScalar < 2) return Oops(Which, "the first two args. must be arrays");
        int keyslot = Args[firstScalar-1].I;
        double[] keydata = R.Store[keyslot].Data;
        int keyLen = keydata.Length;
        if (keyLen == 1) break; // no point in trying to sort an array of length 1. (Pointers ignored.)
        // Deal with any scalar args. first:
        bool ascending = true; int startptr = 0, endptr = keyLen-1;
        if (NoArgs > firstScalar && Args[firstScalar].X < 0.0) ascending = false;
        if (NoArgs > firstScalar+1) startptr = Convert.ToInt32(Args[firstScalar+1].X);
        if (NoArgs > firstScalar+2) endptr = Convert.ToInt32(Args[firstScalar+2].X); // no test for crossed pointers follows, as JS.SortByKey(.) then simply leaves arrays as is.
        // case 1 - single data array: sort that array directly.
        if (firstScalar == 2)
        { int dataslot = Args[0].I;
          double[] thedata = R.Store[dataslot].Data;
          if (thedata.Length != keyLen) return Oops(Which, "the 'data' and 'key' arrays must have the same length.");
          JS.SortByKey(thedata, keydata, ascending, startptr, endptr);
        }
        else // case 2 - Multiple data arrays, so we create an index array (integer) and sort that instead:
        { int[] indexes = new int[keyLen];
          for (int i=0; i < keyLen; i++) indexes[i] = i;
          JS.SortByKey(indexes, keydata, ascending, startptr, endptr);
          // Now we reorganize all the data arrays prior to the key array:
          for (int j=0; j < firstScalar-1; j++)
          { double[] thisdata = R.Store[Args[j].I].Data;
            if (thisdata.Length != keyLen)  return Oops(Which, "all of the 'data' and 'key' arrays must have the same length.");
            double[] newdata = new double[keyLen];
            for (int k=0; k < keyLen; k++)
            { newdata[k] = thisdata[indexes[k]]; }
            R.Store[Args[j].I].Data = newdata;
          }
        }
        break; 
      }
     // case 74: GRID -- see GRIDX etc, case 40
      case 75: // REMOVEPLOT(GraphID, any scalar/array mix of values being PlotIDs) - if GraphID valid, then each PlotID
      // which can be identified in the graph is removed from the graph; but the plot remains in any other graphs using it,
      // and it is not removed from MainWindow's lists of current plots. Valid for 2D and 3D.
      { int grafID = (int) Args[0].X;   Graph graf;
        Board.GetBoardOfGraph(grafID, out graf);   if (graf == null) break;
        double[] plottery = AccumulateValuesFromArgs(Args, 1);
        int[] plotids = plottery._ToIntArray();
        graf.RemovePlotsByID(plotids);
        break;
      }

      case 76: // REQUEST(REF scalar BtnClick, array Heading, array BodyText,
      //                        array/scalar Label1, array/scalar Content1 [, Label2, Content2, ... ] array Buttons ).
      // BtnClick - entry value ignored; at exit, holds: 0 = icon closure, 1+ = button no., in order of arg. ( = left to right on dialog).
      // Heading  - What goes in the title of the form. No formatting tags are valid.
      // BodyText - what goes above all the label-textbox combinations. Pango markup tagging applies.
      // Labeli is the text that goes to the right of the text box; if scalar, there will be no text there.
      // Contenti is initial content; if scalar, the text box will be empty at start of display.
      // No limit imposed by MonoMaths on the number of Label-Content pairs ( = the no. of text boxes), but must be at least one.
      // SPECIAL CASE: If (a) there is only one Label | Content pair, both arrays; and (b) the delimiter '|' occurs an equal no. of times
      //    in both arrays (at least once), then delimited segments will represent separate label / content pairs.
      //    (This was introduced to allow 'request(.)' to have its number of text boxes set at run time.)
      // Buttons - Button texts delimited by '|'. Buttons go from left to right in the order given. You may add an extra string after the
      //   last button which (after a '|') begins with "#:" and tells what to do if ENTER is keyed while a text box is focussed.
      //   After the "#:" should come integers delimited by commas, one per text box. The integer codes are as follows:
      //   To go to another text box: 100 + box no. (base 0); to focuss a button: the button no.; to close the box and return BtnClick
      //   as [100 + the box no. to base 0]: -1. To do nothing: 0 ( = the default without this string). If a button is focussed,
      //   it is vulnerable as the next Enter will activate that button - keep in mind if a risk of accidental extra 'Enter'.
      //   Errors in this extra string don't crash; simply the default applies (i.e. no action when ENTER keyed in a focussed text box).
      // RETURNED: Either a chars. list array (if only one text box), consisting of the final text box contents, trimmed at front and back;
      //    Or a chars. jagged matrix, consisting of all final text box contents. If a jagged matrix, then all future rows were trimmed before
      //    the matrix was constructed; but in the process of building a jagged matrix, a row padder of SPACEs will usually be added at the end.
      //    In addition, BtnClick is set, as described above.
      //    NB: NO PARSING occurs. In particular, empty text boxes simply return an array of one space. (Use system fn. 'stringtovalue(.)
      //          to turn strings into scalars or arrays.)
      // RETURNED IF CANCELLATION: The 'empty' array.
      // ERRORS: None recognized, as no parsing occurs. E.g. if all text boxes are empty, return is still as above, though with one space
      //    per row (or as the whole array, if just one text box). Note that if Labeli is scalar, no label will appear.
      {
        if (NoArgs % 2 != 0) return Oops(Which, "an even no. of args. is required");
        int noBoxes = (NoArgs - 4) / 2; // Guaranteed to be at least one.
        int btnFn = REFArgLocn[0].Y,   btnAt = REFArgLocn[0].X;
        bool erred = false;
        if (btnFn < 0) erred = true;
        else { byte use = V.GetVarUse(btnFn, btnAt);  if (use != 3) erred = true; }
        if (erred) return Oops(Which, "the 1st. arg. is not a named scalar variable");
        // Define and fill what will be args. of JD.InputBox(.):
        string heading = "",  bodytext = "",  btntexts = "";
        heading = StoreroomToString(Args[1].I);
        if (heading == "") erred = true;
        else
        { bodytext = StoreroomToString(Args[2].I);
          if (bodytext == "") erred = true;
        }
        if (erred) return Oops(Which, "the 2nd. and 3rd. args. must both be arrays");
        string[] prompts = null,  boxtexts = null;
        // Look for the special case where all the label texts and box texts have been compressed into two args:
        bool tisDone = false;
        if (noBoxes == 1)
        { string prompt0 = StoreroomToString(Args[3].I); // scalar arg. will return empty string
          string boxtext0 = StoreroomToString(Args[4].I);
          if (prompt0 != "" && boxtext0 != "")
          { string delim = "{|}";
            int n1 = prompt0._CountStr(delim);
            if (n1 > 0  &&  n1 == boxtext0._CountStr(delim) )
            { // We have 2 or more label-boxtext pairs compressed into this one argument:
              prompts  = prompt0.Split(new string[] {delim}, StringSplitOptions.None);
              boxtexts = boxtext0.Split(new string[] {delim}, StringSplitOptions.None);
              if (prompts.Length != n1+1 || boxtexts.Length != n1+1)
              { return Oops(Which, "Programming error in MonoMaths! Call the programmer"); }
              // No more scope for errors, so let's go...
              noBoxes = n1+1;
              tisDone = true;
            }
          }
          if (!tisDone) // just one label and textbox:
          { prompts = new string[] {prompt0};   boxtexts = new string[] {boxtext0};
            tisDone = true;
          }
        }
        if (!tisDone) // then there are two or more label-boxtext pairs:
        { prompts = new string[noBoxes];  boxtexts = new string[noBoxes];
          for (int i=0; i < noBoxes; i++)
          { prompts[i]  = StoreroomToString(Args[2*i+3].I); // scalar arg. will return empty string
            boxtexts[i] = StoreroomToString(Args[2*i+4].I);
          }
        }
        // Button texts:
        btntexts = StoreroomToString(Args[NoArgs-1].I);
        if (btntexts == "") return Oops(Which, "the final arg. must be an array");
        string[] btnTitles = btntexts.Split('|');

       // CALL THE INPUT BOX METHOD:
        int btn = JD.InputBox(heading, bodytext, prompts, ref boxtexts, btnTitles);
        if (btn == 0) { MainWindow.StopNow = 'G'; R.LoopTestTime = 1; }// Icon closure. This value ensures that the very next end-of-loop
               // test will check for user's wish to end the program. But R.LoopTestTime will then be automatically reset to its orig. value of 100.
        V.SetVarValue(btnFn, btnAt, (double) btn);

       // PROCESS THE RETURNED DATA
        if (noBoxes == 1)
        { string btxt = boxtexts[0].Trim();   if (btxt == "") btxt = " ";
          result.I = V.GenerateTempStoreRoom(btxt.Length);
          StringToStoreroom(result.I, btxt);
        }
        else // Multiple boxes, so return a jagged array:
        { int longest = 1; // Not 0, as we want empty strings converted to " ".
          for (int i=0; i < noBoxes; i++) // Trim strings, and find the longest length:
          { boxtexts[i] = boxtexts[i].Trim();
            if (boxtexts[i].Length > longest) longest = boxtexts[i].Length;
          }
          double[] outdata = new double[noBoxes * longest];
          double[] row;
          for (int i=0; i < noBoxes; i++) // build the jagged matrix:
          { string ss = boxtexts[i]._FixedLength(longest, " ");
            row = StringToStoreroom(-1, ss);
            row.CopyTo(outdata, i*longest);
          }
          result.I = V.GenerateTempStoreRoom(longest, noBoxes);
          R.Store[result.I].Data = outdata;
          R.Store[result.I].IsChars = true;
        }
        break;
      }

      case 77: case 78: // CLIP / CLIPABS(Variable LowLimit, Variable HighLimit, Array or Scalar Values): NONVOID; exactly three arguments.
      // Args. 1 and 2: If either is an array, there is no corresponding limit. (E.g. enter 'x', 'none', '-'...). If a scalar, it
      // is the limit. Returned: a COPY of the array or scalar 3rd. arg. with all values conformed to lie between or on the limits.
      // In the case of 'clipabs(.)', if |HighLimit| is n, positive values > n will be replaced by n and neg. values < -n by -n.
      // If |LowLimit| is n, zeros and positive values < n will be replaced by n and neg. values > -n by -n.
      // Analogously for |LowLimit| (. (In the case of 'clip(.)', of course, the abs. values of HighLimit and LowLimit are not taken.)
      { double lowLimit, highLimit;
        if (Which == 77) // CLIP:
        { if (Args[0].I == -1) lowLimit = Args[0].X;  else lowLimit = double.MinValue;      
          if (Args[1].I == -1) highLimit = Args[1].X;  else highLimit = double.MaxValue;      
        }
        else // CLIPABS:
        { if (Args[0].I == -1) lowLimit = Math.Abs(Args[0].X);  else lowLimit = 0;      
          if (Args[1].I == -1) highLimit = Math.Abs(Args[1].X);  else highLimit = double.MaxValue;      
        }
        int inSlot = Args[2].I;
        bool isScalar = (inSlot == -1);
        double[] outData;
        if (inSlot >= 0) outData = R.Store[inSlot].Data._Copy(); // array input
        else outData = new double[] { Args[2].X }; // scalar input
        int totsz = outData.Length;
        if (Which == 77) // CLIP:
        { if (highLimit != double.MaxValue)
          { for (int i = 0; i < totsz; i++)
            { if(outData[i] > highLimit) outData[i] = highLimit; }
          }
          if (lowLimit != double.MinValue)
          { for (int i = 0; i < totsz; i++)
            { if(outData[i] < lowLimit) outData[i] = lowLimit; }
          }
        }
        else // CLIPABS:
        { if (highLimit != double.MaxValue)
          { for (int i = 0; i < totsz; i++)
            { if     (outData[i] >  highLimit) outData[i] = highLimit;
              else if(outData[i] < -highLimit) outData[i] = -highLimit; 
            }
          }
          if (lowLimit != 0)
          { for (int i = 0; i < totsz; i++)
            { if     (outData[i] < lowLimit   && outData[i] >= 0) outData[i] = lowLimit;
              else if(outData[i] >- lowLimit  && outData[i] < 0)  outData[i] = -lowLimit;
            }  
          }
        }
        if (isScalar) result.X = outData[0];
        else 
        { int[] dims = R.Store[inSlot].DimSz;
          result.I = V.GenerateTempStoreRoom(dims);
          R.Store[result.I].Data = outData;
          R.Store[result.I].IsChars = R.Store[inSlot].IsChars;
        }
        break; 
      }
      case 79: // EVALPOLY(Array: the polynomial; Scalar or Array: the indep. var.
       // If an Nx2 matrix, regarded as sets of complex roots; any other array is
       // treated as if a list array, filled with real roots.
      { int polyslot = Args[0].I, Xslot = Args[1].I;
        if (polyslot == -1) return Oops(Which, "the 1st. arg. must be an array (the polynomial)");
        int polylen = R.Store[polyslot].TotSz;
        double coeff0 = R.Store[polyslot].Data[0], XX;
        if (Xslot == -1) // scalar argument:
        { result.X = coeff0;  XX = 1.0;
          for (int i = 1; i < polylen; i++)
          { XX *= Args[1].X;  result.X += XX * R.Store[polyslot].Data[i]; }
        }
        else // array argument:
        { int Xlen = R.Store[Xslot].TotSz;   int newslot;
          if (R.Store[Xslot].DimCnt==2 && R.Store[Xslot].DimSz[0]==2)
          // Then is a matrix of complex roots:
          { double[] Poly = new double[polylen];
            for (int i=0; i<polylen; i++){Poly[i] = R.Store[polyslot].Data[i];}
            newslot = V.GenerateTempStoreRoom(2, Xlen/2); // two columns.
            TCx cxno = new TCx(0,0), cxeval;  int cntr1 = 0, cntr2 = 0;
            for (int i = 0; i < Xlen/2; i++) // go through the array of X-values
            { cxno.re = R.Store[Xslot].Data[cntr1]; cntr1++;
              cxno.im = R.Store[Xslot].Data[cntr1]; cntr1++;
              cxeval = M2.EvaluatePoly(Poly, cxno, false);
              R.Store[newslot].Data[cntr2] = cxeval.re; cntr2++;
              R.Store[newslot].Data[cntr2] = cxeval.im; cntr2++;
            }
          }
          else // is a list of real roots:
          { newslot = V.GenerateTempStoreRoom(Xlen);
            double evaln, Xvalue;
            for (int i = 0; i < Xlen; i++) // go through the array of X-values
            { evaln = coeff0;  Xvalue = R.Store[Xslot].Data[i];  XX = 1.0;
              for (int j = 1; j < polylen; j++)
              { XX *= Xvalue;
                evaln += XX * R.Store[polyslot].Data[j];
              }
              R.Store[newslot].Data[i] = evaln;
            }
          }
          result.I = newslot;
        }
        break; }
      case 80:// SOLVESIM(Square Matrix (at least 2x2), Array RHS) - solve eq'ns:
      //  find '[Soln]' in: '[Matrix].[Soln] = [RHS]'. ALWAYS TEST size of returned array; if only 1, then no soln. possible;
      //  the returned value [0] = 1 (for homogeneous eqns) or 2 (for indeterminate eqns).
      { int LHSSlot = Args[0].I, RHSSlot = Args[1].I;
        if (LHSSlot == -1 || RHSSlot == -1) return Oops(Which, "only array args. allowed");
        StoreItem LHSIt = R.Store[LHSSlot],   RHSIt = R.Store[RHSSlot];
        int size = RHSIt.TotSz;
        if (size < 2 || LHSIt.DimCnt != 2 ||
           LHSIt.DimSz[0] != size || LHSIt.DimSz[1] != size ) return Oops(Which, "args. not properly formatted");
        double[,] Mx = new double[size, size];  int offset = 0;
        double[] RHS = RHSIt.Data._Copy();
        double[] LHSdata = LHSIt.Data;
        for (int i = 0; i < size; i++)
        { offset = i*size;
          for (int j = 0; j < size; j++)
          { Mx[i,j] = LHSdata[offset + j]; }
        }
        Quad Outcome;
        double[] soln = M2.SolveSimultEqns(Mx, RHS, out Outcome, 1e-10);
        int n = size; if (!Outcome.B) n = 1;
        int newslot = V.GenerateTempStoreRoom(n);
        if (!Outcome.B) // can't solve equations:
        { R.Store[newslot].Data[0] = (double) Outcome.I; }
        else R.Store[newslot].Data = soln;
        result.I = newslot; break;
      }
      case 81: // DETERMINANT(Square matrix [, Scalar]). If 2nd. arg., anything below this (e.g. 1e-10) will be taken as zero.
      { int MxSlot = Args[0].I;  double negligible = 0.0;
        if (NoArgs == 2) negligible = Math.Abs(Args[1].X); // too bad if an array.
        if (MxSlot == -1) return Oops(Which, "1st. arg. must be a matrix");
        StoreItem itemMx = R.Store[MxSlot];
        int size = itemMx.DimSz[0];
        if (size < 2 || itemMx.DimCnt != 2 || itemMx.DimSz[1] != size) return Oops(Which, "1st. arg. must be a square matrix, 2x2 or larger)");
        double[,] Mx = new double[size, size];  int offset = 0;
        double[] indata = itemMx.Data;
        for (int i = 0; i < size; i++)
        { offset = i*size;
          for (int j = 0; j < size; j++) Mx[i,j] = indata[offset + j];
        }
        result.X = M2.Determinant(Mx);
        if (Math.Abs(result.X) < negligible) result.X = 0.0;
        break;
      }
      case 82: // SOLVEPOLY(Poly array [, Cutoff (scalar) [, 'P' or 'R']]). Returns an Nx2 matrix where N = degree of poly, 
      // [i][0] is the real component of the ith. solution, [i][1] is the imag. component. If CutOff is supplied, and is > 0, 
      //  the default 1e-15 will be overruled. This parameter is used by M2.SolvePoly(..). (If you want to have a third argument 
      //  but not to overrule the default, use '-1'.) NB - You SHOULD NOT use 0 for 'negligible'; if you do, the return will be
      //  the empty array (unsolveable polynomial), as the solver pulls out of iteration routines only when convergence to zero falls
      //  below this value.
      //  If the last argument is supplied, 'P' or 'p' returns polar; 'R' or 'r' returns rectangular; anything else causes an error.
      // If the poly. can't be solved, an array of size 1, [0] = 0, is returned, so ALWAYS TEST SIZE before processing as a matrix.
      { int n, polyslot = Args[0].I;  double negligible = 1e-15;
        if (polyslot == -1) return Oops(Which, "1st. arg. must be an array.");
        int size = R.Store[polyslot].TotSz;
        if (size < 2) return Oops(Which, "the polynomial must be at least 1st. degree");
        if (NoArgs > 1 && Args[1].X > 0.0)
        { negligible = Math.Abs(Args[1].X); }// too bad if an array.
        bool AsPolar = false;
        if (NoArgs > 2)
        { if (Args[2].I == -1) n = -1; // scalar arg.: an error.
          else n = (int) R.Store[Args[2].I].Data[0];// array arg: take 1st. ltr.
          if (n == 0x50 || n == 0x70)AsPolar = true; // 'P' or 'p'
          else if (n != 0x52 && n != 0x72) n = -1;
          if (n == -1) return Oops(Which, "3rd. arg. can only be one of 'P', 'p', 'R' and 'r'");
        }
        double[] Poly = new double[size];
        for (int i = 0; i < size; i++) Poly[i] = R.Store[polyslot].Data[i];
        if (Poly[size-1] == 0.0) return Oops(Which, "highest coeff. of poly. cannot be zero");
        Quad quee;   int newslot;
        TCx[] cox = M2.SolvePoly(Poly, negligible, out quee);
        if (!quee.B)
        { newslot = V.GenerateTempStoreRoom(1);
          R.Store[newslot].Data[0] = 0.0;
        }
        else
        { newslot = V.GenerateTempStoreRoom(2, cox.Length);
          for (int i = 0; i < cox.Length; i++)
          { if (AsPolar)cox[i].ToPolar(); else cox[i].ToRect();
            R.Store[newslot].Data[2*i] = cox[i].re;
            R.Store[newslot].Data[2*i+1] = cox[i].im;
          }
        }
        result.I = newslot;  break;
      }
      case 83:// ROOTSTOPOLY(Array Roots). If the array is list or vector, Roots
      // is taken as real roots. If Nx2, complex roots ([i][0] = real part).
      // Complex roots are taken as being rect.
      // All errors crash, so if no crash, always returns a polynomial.
      { int rootslot = Args[0].I;  double negligible = 1e-15;
        if (rootslot == -1) return Oops(Which, "array arg. required");
        bool complexity = false,  oopsie = false;
        int dimcnt = R.Store[rootslot].DimCnt, totsz = R.Store[rootslot].TotSz;
        int dim0=R.Store[rootslot].DimSz[0], dim1=R.Store[rootslot].DimSz[1];
        if (dimcnt > 2)oopsie = true;
        else if (dimcnt == 2)
        { if (dim0 == 2) complexity = true;
          else if (dim0!=1 && dim1!=1) oopsie = true; // allow Nx1 and 1xN
        }
        if (oopsie) return Oops(Which, "array is improper");
        double[] poly;
        if (!complexity)  // Real roots:
        { double[] roots = new double[totsz];
          for (int i=0; i<totsz; i++) roots[i] = R.Store[rootslot].Data[i];
          poly = M2.RootsToPoly(roots, totsz);
        }
        else // complex roots:
        { TCx[] roots = new TCx[dim1];
          for (int i=0; i<dim1; i++)
          { roots[i] = new TCx(
                 R.Store[rootslot].Data[2*i], R.Store[rootslot].Data[2*i + 1]);}
          int err;
          TCx[] coots = M2.MatchConjugates(roots, negligible, out err);
          if (err != -1) return Oops(Which, "array has unpaired complex roots");
          poly = M2.RootsToPoly(coots);
        }
        int newslot = V.GenerateTempStoreRoom(poly.Length);
        for (int i=0; i<poly.Length; i++) R.Store[newslot].Data[i] = poly[i];
        result.I = newslot;  break;
      }
      case 84: // UNDIM(any no. of ASSIGNED arrays). A VOID function. Redimensions arguments to list arrays. All must be named arrays.
      { int len, slot;
        for (int i=0; i < NoArgs; i++)
        { slot = Args[i].I;  if (slot == -1) return Oops(Which, "only array args. are allowed");
          StoreItem stoic = R.Store[slot];
          len = stoic.TotSz;
          stoic.DimSz = new int[TVar.MaxNoDims];
          stoic.DimSz[0] = len;    stoic.DimCnt = 1;
        }
        break;
      }
      case 85: // INTERPOLATE (scalar/array PseudoIndex, list array RefArray ) -- If PseudoIndex is an integer(s) in the
      //  range 0 to (length of Array)-1, then this function trivially returns RefArray[PseudoIndex]. more generally, if
      //  PseudoIndex is M, and M is fractional, what is returned is a linear interpolation between RefArray[floor(M)]
      //  and RefArray[ceiling(M)]. If outside the range of array indexes, it is a linear extrapolation from the nearest
      //  two RefArray end values.
      //  If PseudoIndex is an array, its elements can be in any order; the return will be a list array of the same size.
      //  The structure of PseudoIndex will be preserved, but it will be a non-chars. array.
      //  For accuracy, extrapolation outside the limits of RefArray should be minimal. RefArray need not be sorted, but variation
      //  across its values should be smooth enough for interpolation to be mathematically meaningful.
      //  For some applications, the function 'readtable' might be a better way to go.
      { bool isScalar = (Args[0].I == -1); // We use this at the end to distinguishe between scalar I/O and I/O of array of length 1.
        StoreItem testIt = null; // the output array (if any) will copy DimSzfrom this.
        // Collect the test X value(s):
        double[] Xtest;
        if (isScalar) Xtest = new double[1] { Args[0].X };
        else { testIt = R.Store[Args[0].I];  Xtest = testIt.Data; }
        int XtestLen = Xtest.Length;
        int refSlot = Args[1].I;
        if (refSlot == -1) return Oops(Which, "2nd. arg. must be an array (the reference values)");
        double[] refArray = R.Store[refSlot].Data;
        int refArrayLen = refArray.Length;
        if (refArrayLen < 2) return Oops(Which, "2nd. arg. (the reference values) must have length at least 2");
        double[] outData = new double[XtestLen];
        double testval, incmt, loval;
        int lowIndex;
        for (int i=0; i < XtestLen; i++)
        { testval = Xtest[i];
          lowIndex = (int) Math.Floor(testval);
          if (lowIndex < 0) lowIndex = 0;  else if (lowIndex > refArrayLen-2) lowIndex = refArrayLen-2;
          incmt = testval - lowIndex;
          loval = refArray[lowIndex];
          outData[i] = loval + incmt * (refArray[lowIndex+1] - loval);
        }      
        if (isScalar) result.X = outData[0];
        else
        { result.I = V.GenerateTempStoreRoom(testIt.DimSz);
          R.Store[result.I].Data = outData;
        }    
        break;
      }
     // case 86: EVEN(..) - dealt with under Hybrids
      case 87: // DIFFPOLY(Array)
      { int oldslot = Args[0].I;
        if (oldslot == -1) return Oops(Which, "an array arg. is required");
        int oldlen = R.Store[oldslot].TotSz;
        int newlen = oldlen-1;  if (newlen == 0) newlen = 1;
        int newslot = V.GenerateTempStoreRoom(newlen);
        double[] olddata = R.Store[oldslot].Data,  newdata = R.Store[newslot].Data;
        for (int i = 0; i < oldlen-1; i++)//not 'newlen':  do nil, if poly size=1.
        { newdata[i] = (i+1) * olddata[i+1]; }
        result.I = newslot;  break;
      }

      case 88: // LOAD ( array/scalar LoadType, array FileName, <one further arg. with different meanings for different load types>):
      // LOAD( 'A', FileName [, ExpectPrefix ] ) -- expect array's data to be in blocks of 8, and return it afte decoded by MONO to double[].
      //                          If ExpectPrefix present and true, expect and parse a prefix block with data about the saved variable.
      // LOAD( 'B', FileName ) -- uncritically loads all data of the file and returns it as a single BYTE ARRAY.
      // LOAD( 'F', FileName, array VarNameInFile [, bool ErrorReturnsScalar ] ) -- expect data in human-readable text format,
      //                         and return the encoded variable. If last is TRUE, failure to load returns scalar NaN, not array [NaN].
      // LOAD( 'T', FileName ) -- load data, convert all bytes to unicodes, and return all data as a chars.-rated list array.
      // LOAD( 'D', FilePath ) -- returns the DIRECTORY NAME ONLY, of the file dialog in which a file (any file) was clicked. Terminal '/' guaranteed.
      // MORE ON ARGS:
      //   FileName - If no path, CurrentDataPath will be applied. Path shortcuts recognized: "~/", "./", "../".
      //     Leading and trailing white spaces and char. '\u0000' are trimmed off FileName before use.
      //     If FileName = just spaces OR "?", opens a file-chooser dialog box at current path. If FileName is a directory,
      //     opens dialog box there.
      //   VarNameInFile: If for some silly reason the name is duplicated in the file, only the data for the first instance is loaded.
      // MORE ON WHAT IS RETURNED, IF NO ERROR:
      //  (a) LOAD('A', FN, ExpectPrefix false or missing): returns decoded data as a single list array.
      //  (b) LOAD('A', FN, ExpectPrefix true: returns decoded data as above, starting from the first byte after the prefix. The decoded
      //        prefix is stored in IOMessage (this is a case where IOMessage is nonempty even though IOError is false).
      //        The format in IOMessage is: <var. name>::<time when saved, in secs since start of 1AD> - e.g. "Foodle::63467503669".
      //  (c) LOAD('F', FN, VarNameInFile ): will return a structured array OR scalar (the others cannot return a scalar).
      // ERRORS FOR ALL: Note that THE ONLY CRASHES that occur are for impossible arguments. All I/O errors set the boolean flag IOError,
      //    tested by fn "iok()"; and place their error message in IOMessage, ready for collection by fn. 'iomessage()'.
      //    The RETURN is an array of length 1, value NaN. (Exception: for 'load('F', ...)', with final arg. TRUE, scalar NaN is returned).
      //    But always use iok() as the validity test, rather than 'empty(.)'.
      //   Special cases: (1) User cancels from dialog box: IOError is true, but IOMessage is empty. (2) LOAD with ExpectPrefix TRUE, but no
      //    prefix found, not even an invalid one: IOMessage STARTS WITH a dash '-', but continues with the error message. (In all other
      //    situations, there is no space before an error message.)
      {
        // Reset error flags:
        IOError = false;   IOMessage = "";     IOFilePath = "";
        bool returnScalarError = (NoArgs > 3  && Args[3].X != 0.0);
        double x;
        char fileType;
        int fileTypeSlot = Args[0].I;
        if (fileTypeSlot == -1) x = Args[0].X;  else x = R.Store[fileTypeSlot].Data[0];
        if (x > 96.0) x -= 32.0;
        x = Math.Round(x);
        if      (x == 65.0) fileType = 'A';  else if (x == 66.0) fileType = 'B';  else if (x == 70.0) fileType = 'F';
        else if (x == 84.0) fileType = 'T';  else if (x == 68.0) fileType = 'D';
        else return Oops(Which, "unrecognized 1st. argument (i.e. type of loading function)");
        // Get file name:
        int filenameslot = Args[1].I;
        if (filenameslot == -1) return Oops(Which, "the 1st. arg. must be an array (the file name)");
        string filename = StoreroomToString(filenameslot, true, true);
        string finalFolder = " ";
        string[] flame = filename._ParseFileName(MainWindow.ThisWindow.CurrentPath);
        if (fileType == 'D' || filename == "" || filename[0] == '?' || flame[1] == "") // Then a dialog box is required:
        { Gtk.FileChooserAction action;
          if (fileType == 'D') action = Gtk.FileChooserAction.SelectFolder;
          else action = Gtk.FileChooserAction.Open;
          string filepath = flame[0]; if (filepath == "") filepath = MainWindow.ThisWindow.CurrentPath;
          Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog("LOADING DATA", null,
               action, "Cancel", Gtk.ResponseType.Cancel, "Open", Gtk.ResponseType.Accept);
          fc.SetCurrentFolder(filepath);
          int outcome = fc.Run();
          filename = fc.Filename;
          finalFolder = fc.CurrentFolder;
          if (finalFolder._Last() != '/') finalFolder += '/';
          fc.Destroy(); // Without this, the FileChooserDialog window won't get closed.
          if (outcome != (int) Gtk.ResponseType.Accept || filename.Trim() == "")
          { result.I = EmptyArray();
            IOError = true; // BUT IOMessage remains as "", this being the way that cancellation of dialog box is recognized.
            if (returnScalarError) result.X = double.NaN;   else  result.I = EmptyArray();
            break;
          }
        }
        else filename = flame[0]+ flame[1]; // Nec. where user has used an allowed abbreviation, handled by ._ParseFileName(.).
      // IF DIRECTORY ONLY WAS REQUIRED: exit at this point.
        if (fileType == 'D')
        { result.I = V.GenerateTempStoreRoom(finalFolder.Length);
          StringToStoreroom(result.I, finalFolder);
          break;
        }
        // Get FILE SIZE:
        long lulu = -1;
        int filesize = -1;
        try
        { FileInfo fie = new FileInfo(filename);
          if (fie.Exists)
          { lulu = fie.Length; // type is Int64
            if (lulu > Int32.MaxValue) filesize = -2;  else filesize = Convert.ToInt32(lulu);
          }
        }
        catch { filesize = -3; }
        if (filesize <= 0)
        { IOMessage = "file '" + filename + "' is ";
          if      (filesize ==  0) IOMessage += "empty";
          else if (filesize == -2) IOMessage += "too long (max. allowed: " + Int32.MaxValue.ToString() + ")";
          else                     IOMessage += "inaccessible";
          IOError = true;
          if (returnScalarError) result.X = double.NaN;   else  result.I = EmptyArray();
          break;
        }
       // LOAD DATA FROM SPECIALLY FORMATTED TEXT FILE:
        if (fileType == 'F')
        { 
          if (NoArgs <= 2 || Args[2].I == -1) return Oops(Which, "2nd. arg. must be present, and must be an array (the variable's name)");
          string varname = StoreroomToString(Args[2].I, true, true);
          if (varname == "")
          { IOMessage = "variable name arg. is empty";  IOError = true;   
            if (returnScalarError) result.X = double.NaN;   else  result.I = EmptyArray();
            break;
          }
          // Retrieve the text file:
          string ss, theText = "";
          using (StreamReader sr = new StreamReader(filename))
          { while ((ss = sr.ReadLine()) != null) // I'm not putting this in a 'try..catch' loop; there is enough protection above.
            { theText += ss.TrimStart() + '\n'; }
          }
          Strub2 tutu = ExtractFormattedTextVariable(theText, varname); // Sets up the variable as a temporary array
          if (tutu.X == -999.0) // Error:
          { IOMessage = tutu.SY;  IOError = true;
            if (returnScalarError) result.X = double.NaN;   else  result.I = EmptyArray();
            break;
          }
          // If no error, then ExtractVariable has been kind enough to set up a new temp. array for us...
          if (tutu.X == -1.0) result.X = tutu.Y; // scalar variable
          else result.I = Convert.ToInt32(tutu.X);
          IOFilePath = filename;
        }
       // LOAD ARRAY IN 8-BYTE MONO-CODED FORM:
       // and LOAD BYTES AS IS:
        else if (fileType == 'A' || fileType == 'B')
        { if (fileType == 'A' && filesize % 8 != 0)
          { IOMessage = "file contents not decipherable, as file size is not a multiple of 8";  IOError = true;
            result.I = EmptyArray();   break;
          }
          byte[] bitten = null;
          try
          { FileInfo fi = new FileInfo(filename); // No existence test, as we already did this once successfully above.
            FileStream fs = fi.OpenRead();
            bitten = new byte[filesize];
            fs.Read(bitten, 0, filesize);
            fs.Close(); // must always close a stream (or use 'using').
          }
          catch
          { IOMessage = "file exists and is nonempty but could not be opened for reading";  IOError = true; result.I = EmptyArray(); }
          if (IOError) break; // I am not sure if you can safely break from within a 'catch' statement.
          IOFilePath = filename;
          if (fileType == 'B')
          { double[] bitings = new double[filesize];
            for (int i=0; i < filesize; i++) bitings[i] = (double) bitten[i];
            result.I = V.GenerateTempStoreRoom(filesize);
            R.Store[result.I].Data = bitings;
          }
          else // fileType = 'A':
          { int dumpSize = filesize / 8;
            double[] dump = new double[dumpSize];
            System.Buffer.BlockCopy(bitten, 0, dump, 0, filesize); // turn the file stream's bytes into doubles, dumped into 'dump'.
            if (NoArgs <= 2 || Args[2].X == 0.0) // then no prefix expected:
            { result.I = V.GenerateTempStoreRoom(dumpSize);
              R.Store[result.I].Data = dump;
            }
            else // Expect a file prefix, properly formatted:
            { string varName = "";  int[]Dims = null;  bool IsItChars = false;  double SaveTime = 0.0;
              int outcome = ParseBinaryCodedFileDump(ref dump, ref varName, ref Dims, ref IsItChars, ref SaveTime);
              if (outcome == 1) // prefix was valid
              { IOMessage = varName + "::" + SaveTime.ToString();
                result.I = V.GenerateTempStoreRoom(Dims);
                StoreItem sitem = R.Store[result.I];
                sitem.Data = dump;
                sitem.IsChars = IsItChars;
              }
              else
              { IOError = true;
                IOMessage = ParseBinaryCodedFileDumpErrMsg(outcome) + ". Nevertheless the contents of the whole file has been loaded";
                result.I = V.GenerateTempStoreRoom(dumpSize);
                R.Store[result.I].Data = dump;
              }
            }
          }
        }
       // LOADTEXT:
        else if (fileType == 'T')
        { string ss, wholetext = "";
          try
          { using (StreamReader sr = new StreamReader(filename))
            { while ((ss = sr.ReadLine()) != null)
              { wholetext += ss + '\n'; }
            }
          }
          catch
          { IOMessage = "could not read file'" + filename + "'";  IOError = true; }
          if (IOError) { result.I = EmptyArray(); break; }
         // valid data, if got here:
          IOFilePath = filename;
          int len = wholetext.Length;
          if (len == 0) result.I = EmptyArray();
          else
          { int newslot = V.GenerateTempStoreRoom(len);
            StringToStoreroom(newslot, wholetext);   R.Store[newslot].IsChars = true;
            result.I = newslot;
          }
        }
        break;
      }
   // case 89: BETWEEN - see case 60

      case 90: // SIGN( array/scalar XX [, ValueForZero [, ValueForPositive [, ValueForNegative [, scalar VirtualZero ] ] ] ]);
      // If "ValueFor..." is an array - any array - then it is the null operator: do not substitute for values in this category.
      // Defaults: ValueForZero defaults to 0; ValueForPositive to 1;  ValueForNegative to -1, VirtualZero to 0 (incl. if is an array).
      // Values with absol. value ≤ VirtualZero are taken as 0. (The absolute value of VirtualZero is used.)
      { 
        double forZero = 0.0, forPositive = 1.0, forNegative = -1.0, VirtualZero = 0.0;
        bool leaveAloneZero = false, leaveAlonePositive = false, leaveAloneNegative = false;
        if (NoArgs > 1) { forZero     = Args[1].X;   leaveAloneZero     = (Args[1].I >= 0); }
        if (NoArgs > 2) { forPositive = Args[2].X;   leaveAlonePositive = (Args[2].I >= 0); }
        if (NoArgs > 3) { forNegative = Args[3].X;   leaveAloneNegative = (Args[3].I >= 0); }
        if (NoArgs > 4) VirtualZero = Math.Abs(Args[4].X);
        double[] indata;
        StoreItem sitem = null;
        int inslot = Args[0].I;
        if (inslot == -1)  indata = new double[] { Args[0].X };
        else
        { sitem = R.Store[inslot];
          indata = sitem.Data;
        }
        int dataLen = indata.Length;
        double[] outdata = new double[dataLen];
        double x;
        for (int i=0; i < dataLen; i++)
        { x = indata[i];
          if      (x > VirtualZero)  { if (!leaveAlonePositive) x = forPositive; }
          else if (x < -VirtualZero) { if (!leaveAloneNegative) x = forNegative; }
          else if (!leaveAloneZero) x = forZero;
          outdata[i] = x;
        }
        if (inslot == -1)  result.X = outdata[0];
        else
        { result.I = V.GenerateTempStoreRoom(sitem.DimSz);
          R.Store[result.I].Data = outdata;
        }
        break;
      }
     case 91: // INTEGRAL(YValues array, X low, X high). Simpson's rule.
      // YValues must be of ODD length >= 3.
      { int slot = Args[0].I;
        if (slot == -1) return Oops(Which, "the 1st. arg. must be an array (the integrand)");
        Duplex tdoop = JM.Integrate(R.Store[slot].Data, Args[1].X, Args[2].X);
        if (tdoop.Y < 0.0)
        { if (tdoop.Y >= -2.0) return Oops(Which, "the integrand array must have an ODD length, 3 or more");
          else return Oops(Which, "integration range extremes are either equal or cross");
        }
        result.X = tdoop.X;
        break;
      }
      case 92: // INTCURVE(YValues array, X low, X high [, const.of integration).
      // Uses Simpson's rule. YValues must be of ODD length >= 3.
      // Returned: a matrix 2 x (L+1)/2, where L is the length of YValues.
      //  0th. row is X values, other is Y vals.
      { int oldslot = Args[0].I;
        if (oldslot == -1) return Oops(Which, "the 1st. arg. must be an array (the integrand)");
        double[] xx = null;
        double Const = 0.0; if (NoArgs > 3) Const = Args[3].X;
        double[] curve = JM.IntegralCurve
             (R.Store[oldslot].Data, Args[1].X, Args[2].X, Const, ref xx);
        int curvelen = curve.Length;
        if (curvelen == 1)
        { if (curve[0] >= -2.0) return Oops(Which, "the integrand array must have an ODD length, 3 or more");
          else return Oops(Which, "integration range extremes are either equal or cross");
        }
        int newslot = V.GenerateTempStoreRoom(curvelen,2);
        double[] mxdata = new double[curvelen*2];
        for (int i = 0; i < curvelen; i++)
        { mxdata[i] = xx[i];  mxdata[curvelen+i] = curve[i]; }
        R.Store[newslot].Data = mxdata;
        result.I = newslot;  break; 
      }
      case 93: // GAUSS(Scalar or Array Values; then 4 scalars: Mean, SD,  CurvePeak, IsInverse).
      // If CurvePeak <= 0, the classical peak of 1/sqrt(2.PI.sqr(SD) is used (--> area under curve = 1). 
      // The absolute value of SD is used; if zero
      // If IsInverse, Value is taken as Y, and X (on + side of Mean) is returned. Otherwise value is taken as X, and Y is returned.
      // Errors do NOT crash, but produce the return value -1e100 ('errorval').
      { int valSlot = Args[0].I;
        double[] Values;
        StoreItem valItem = null;
        if (valSlot == -1) Values = new double[]{ Args[0].X };
        else { valItem = R.Store[valSlot];  Values = valItem.Data; }
        double x, y, Mean = Args[1].X,  SD = Args[2].X, Peak = Args[3].X, errorval = -1e100;
        int noValues = Values.Length;
        double[] Output = new double[noValues];
        if (SD <= 0.0) { for (int i=0; i < noValues; i++) Output[i] = errorval; }
        else
        { bool UseClassicalPeak = false; if (Peak <= 0.0)UseClassicalPeak = true;
          bool Inverse = (Args[4].X != 0.0);
          if (Inverse)
          { for (int i=0; i < noValues; i++)
            { y = Values[i];
              if (!UseClassicalPeak) y /= Peak;
              x = JM.InverseGauss(y, SD, !UseClassicalPeak);
              if (x < -0.5) Output[i] = errorval; // returned errors are -1 or -2.
              else Output[i] = x + Mean; // Errors are the only way in which output can be < Mean. JM.Inv... returns -1 or -2 for errors.
            }
          }
          else // forward fn., not inverse:
          { double coeff;
            if (UseClassicalPeak) coeff = 1 / SD*Math.Sqrt(2.0*Math.PI);  else coeff = Peak;
            for (int i=0; i < noValues; i++)
            { x = Mean - Values[i];
              Output[i] = coeff * Math.Exp( -x * x /(2*SD*SD) );
            }
          }
        }
        if (valSlot == -1) result.X = Output[0];
        else
        { result.I = V.GenerateTempStoreRoom(valItem.DimSz);
          R.Store[result.I].Data = Output;
        }
        break;
      }
      case 94: // RANDGAUSS(..) Two forms are allowed:
      // (1) is nonvoid, and provides a scalar; args. are (Scalar: Mean, Scalar: SD [, Scalars LowerLimit, UpperLimit]). 
      // (2) is void, and fills a variable; args. are (Named Variable; Scalar: Mean, Scalar: SD [, Scalars LowerLimit, UpperLimit]
      // Note that you have to provide both limits or none, as NoArgs is used to distinguish between the various variations, and must be
      //  unique for each of the four argument combinations. (This allows no way of detecting the error of providing one limit
      //  in the void form; code will take this as the nonvoid form, and hopefully raise an error, or at worst leave input var. unchanged.)
      // Cutoff of values is at 5.476.. standard devns. from the mean, unless you provide these limits. Numbers outside the limits will 
      //  simply be ignored. If (UpperLimit - LowerLimit) <= SD/10, or window wholly outside of Mean +/- 4*SD, an error is raised - 
      //  window too small.
      //  Zero or neg. SD also returns an error.
      { bool isVoidForm = (NoArgs == 3 || NoArgs == 5);    bool HasLimits = (NoArgs >= 4);
        int offset = 0;  if (isVoidForm) offset = 1;
        double x, Mean = Args[offset].X,  SD = Args[offset+1].X,  LoLmt = 0.0, HiLmt = 0.0;
        if (SD <= 0.0) return Oops(Which, "the supplied SD value of {0} is either zero or negative", SD);
        if (HasLimits)
        { LoLmt = Args[NoArgs-2].X;  HiLmt = Args[NoArgs-1].X; // no test for scalarity.
          if (HiLmt < Mean-4*SD || LoLmt > Mean+4*SD) return Oops(Which, "the limits window is wholly outside the range +/- 4*SD");
          else if (HiLmt - LoLmt < SD/10.0) return Oops(Which, "the supplied limits give a window width < 1/10 of an SD");
        }   // If we didn't exclude these cases, we would have to wait too long / forever for suitable values in 'while' loops below.
        if (!isVoidForm) 
        { while (true)
          { x = JM.RandGauss(Mean, SD, Rando);
            if (!HasLimits || (x >= LoLmt && x <= HiLmt) ) break; // If the limit window were too small, this would be a huge delaying line.
          }
          result.X = x;
        }
        else // this is the VOID form, with variable as 1st.:
        { int Fn = F.REFArgLocn[0].Y, At = F.REFArgLocn[0].X;
          if (Fn < 0) return Oops(Which, "if there are three args. (the void form), then the first must be a named variable");
          else if (Fn >= C.UserFnCnt) return Oops(Which, "the 1st. arg. raised an unspecified error");
          bool isarray = (V.GetVarUse(Fn,At) >= 10);
          // Scalar variable:
          if (!isarray) 
          { while (true)
            { x = JM.RandGauss(Mean, SD, Rando);
              if (!HasLimits || (x >= LoLmt && x <= HiLmt) ) break; // If the limit window were too small, this would be a huge delaying line.
            }
            V.SetVarValue(Fn, At, x);
          }
          else // Array variable:
          { int len = R.Store[Args[0].I].TotSz;
            double[] xx = new double[len];
            for (int i = 0; i < len; i++)
            { while (true)
              { x = JM.RandGauss(Mean, SD, Rando);
                if (!HasLimits || (x >= LoLmt && x <= HiLmt) ) break; // If the limit window were too small, this would be a huge delaying line.
              }
              xx[i] = x;
            }
            R.Store[Args[0].I].Data = xx;
          }
        }
        break;
      }
      case 95: // GRAPHCOPY(scalar GraphID, bool WithOriginalDimensions) -- copy the given graph.
      // If WithOriginalDimensions is false, will reflect changes of size from dragging margins and by 'graphresize(.)', and in the case
      // of 3D graphs, of change of graph cage orientation. CAVEAT: If called in less than around 0.1 secs. after 'graphresize(.)', no
      // resizing will occur. (If have tried to circumvent this by including 'Board.ForceRedraw(.)' and
      //    "while (Gtk.Application.EventsPending ())   Gtk.Application.RunIteration();" in different places, all to no avail.)
      // No plots are plotted on the resulting graph. If GraphID doesn't exist, 0 is returned; otherwise the new graph's ID.
      { int graphID = (int) Args[0].X;
        Graph oldGraph;
        Trio dummy = Board.GetBoardOfGraph(graphID, out oldGraph);
        if (oldGraph == null) break; // simply nothing happens, if there ain't no such graph.
        bool withOrigDims = (NoArgs > 1 && Args[1].X != 0.0);
        Graph newGraph = Graph.CopyOf(oldGraph.ArtID, withOrigDims);  if (newGraph == null) break;
        result.X = (double) newGraph.ArtID;
        break;
      }
      case 96: // CURVEFIT(XPoints, YPoints, Degree). XPoints must be in ascending
       // sorted order, with no duplication of values.
      { int slotx = Args[0].I, sloty = Args[1].I;
        if (slotx == -1 || sloty == -1) return Oops(Which, "the first two args. must be arrays");
        int Xsize = R.Store[slotx].TotSz, Ysize = R.Store[sloty].TotSz;
        if (Xsize != Ysize) return Oops(Which, "the first two args. must have the same size");
        int degree = (int) Args[2].X;
        if (degree < 1) return Oops(Which, "the last arg. must have integral value 1 or higher");
        if (Xsize <= degree) return Oops(Which, "for a {0}th. degree polynomial curve, at least {1} points must be supplied", degree, degree+1);
        double[] Xs = R.Store[slotx].Data; // NB - can be longer than TotSz!
        double[] Ys = R.Store[sloty].Data;
        for (int i = 1; i < Xsize; i++)//check for sorted order and no X duplicn.
        { if (Xs[i] <= Xs[i-1]) return Oops(Which, "X axis values must be in ascending order, with no duplications."); }
        Duplex[] Points = new Duplex[Xsize];
        for (int i = 0; i < Xsize; i++) Points[i] = new Duplex(Xs[i], Ys[i]);
        Quad Success;
        double[] poly = M2.PolyCurveFit(Points, degree, out Success);
        if (!Success.B) return Oops(Which, "curve fitting failed: {0}", Success.S);
        int newslot = V.GenerateTempStoreRoom(poly.Length);
        R.Store[newslot].Data = poly;
        result.I = newslot;  break;
      }
      case 97: // UPSAMPLE(Array to upsample, Scalar UpsampleRate, variable MethodIndicator [, StartPtr [, EndPtr [, Lop ]]]). 
      // Array's structure is ignored; it must have length >= 3.
      // UpsampleRate is rounded (as it is often the result of a calculation); the other pointers are not.
      //  If UpsampleRate rounds to <= 1, it is regarded as 1, the return being a simple copy (as list array) of all data in Array.
      // Values for MethodInd. are: 
      // 'Z','z',0 (Zeroes fill interspices); 'L','l', 1 (linear interpolation); 'S','s',2 (steps);
      // 'C','c',3 (cubic splines). If scalars StartPtr and EndPtr are present, they define the extent of the
      // array to expand. (StartPtr < 0 --> 0; EndPtr < 0 or beyond end is reset to end of array; '<0' allows '-1' 
      // fillers for these two where a sixth arg. needed.)
      // If scalar Lop is present and >=1 but < upsampled array size, then the returned 
      // array will be smaller by the value of Lop. (Use '1' for step-upsampling, if you want it to end on
      // a flat step rather than on the riser for the next virtual step.)
      // Oversized EndPtr is scaled down; otherwise args. must be accurate.
      // The RETURNED array is always a list array.
      { int slot = Args[0].I, uprate = Convert.ToInt32(Args[1].X);
        if (slot == -1) return Oops(Which, "the first arg. must be an array");
        StoreItem initem = R.Store[slot];
        int inlen = initem.TotSz;
        if (inlen < 3) return Oops(Which, "the array to upsample must have length 3 or greater");
        if (uprate < 2) // simply return the data of the original array:
        { result.I = V.GenerateTempStoreRoom(inlen); // output of this fn. must always be a list array
          Array.Copy(initem.Data, R.Store[result.I].Data, inlen);
          break;
        }
        // Upsample rate now guaranteed >= 2:
        int startptr = 0;  if (NoArgs >= 4) startptr = (int) Args[3].X;   if (startptr < 0) startptr = 0;
        int endptr = inlen-1;  if (NoArgs >= 5) endptr = (int) Args[4].X;
        if (endptr >= inlen || endptr < 0) endptr = inlen-1; // allow negative endptr, as filler for the 6-arg. version.
        if (startptr < 0 || startptr >= endptr) // extent must have length >= 2.
        { return Oops(Which, "improper start and end pointers were supplied"); }
        int LopIt = 0; 
        if (NoArgs >= 6 && Args[5].X != 0.0) 
        { LopIt = (int) Args[5].X;  if (LopIt < 0) LopIt = 0; }
        int method;
        if (Args[2].I == -1) method = (int) Args[2].X;
        else method = (int) R.Store[Args[2].I].Data[0];
        char methchar=' ';
        if      (method == 0 || method == 0x5A || method == 0x7A)methchar = 'Z';
        else if (method == 1 || method == 0x4C || method == 0x6C)methchar = 'L';
        else if (method == 2 || method == 0x53 || method == 0x73)methchar = 'S';
        else if (method == 3 || method == 0x43 || method == 0x63)methchar = 'C';
        else return Oops(Which, "unrecognized method descriptor");
        Quad quoo;
        double[] outarr = M2.UpSample(initem.Data, startptr, endptr, uprate,  methchar, out quoo);
        if (!quoo.B) return Oops(Which, "upsampling failed: {0}", quoo.S);
        int newsize = inlen + (int)quoo.X - LopIt;
        if (newsize < 1) newsize += LopIt; // tantamount to setting LopIt = 0, if it was too large.
        int newslot = V.GenerateTempStoreRoom(newsize);
        // Allows for the remote possibility that .Data of orig. array exceeded TotSz.
        // In that case, same will apply, but the new .TotSz will be correct.
        Array.Copy(outarr, R.Store[newslot].Data, newsize);
        result.I = newslot;  break;
      }
      case 98: // DOWNSAMPLE(Array to downsample, Scalar DownsampleRate[, StartPtr [, EndPtr]]). Nonvoid fn., returning downsized array.
      // StartPtr can be neg. Elements at [StartPtr + k modulo downrate] are retained (hence if k=0, [StartPtr] is retained). For neg. k,
      // first retained is [( |k| mod downrate)]. (Note that -4 mod 3 is -2, not -1, with C#; therefore is best to keep |k| below downrate.)
      { int slot = Args[0].I, downrate = (int) Math.Round(Args[1].X);
        if (slot == -1) return Oops(Which, "the first arg. must be an array");
        int inlen = R.Store[slot].TotSz;
        if (inlen < 2) return Oops(Which, "the array to downsample must have length 2 or greater");
        if (downrate < 1) return Oops(Which, "the downsampling rate must be at least 1");
        int startptr = 0;  if (NoArgs >= 3) startptr = (int) Args[2].X;
        int endptr = inlen-1;  if (NoArgs >= 4) endptr = (int) Args[3].X;
        if (endptr >= inlen) endptr = inlen-1;
        if (startptr > endptr) return Oops(Which, "the start pointer is greater than the end pointer");
        double[] outarr = M2.DownSample(R.Store[slot].Data, downrate,
                                                         startptr, endptr);
        int newslot = V.GenerateTempStoreRoom(outarr.Length);
        R.Store[newslot].Data = outarr;
        result.I = newslot;  break;
      }
      case 99: // ODOMETER(REF Array to treat as odometer, Scalar NoBase [, Scalar UpDown] ). UpDown: 0 or positive, or omission of argument,
      //   --> counts up by 1; negative --> counts down by 1. Returns 0 for array leaving as all zeroes, 1 otherwise, except for 2 if
      //  all digits are maximal.
      { int slot = Args[0].I;
        if (slot == -1) return Oops(Which, "the first arg. must be an array");
        int len = R.Store[Args[0].I].TotSz;
           // This being a 'ref arg req'd' fn., checks already done on existence.
        int NoBase = (int) Math.Round(Args[1].X);
        if (NoBase < 2) return Oops(Which, "the number base must be >= 2");
        bool CountUp = true;
        if (NoArgs > 2) CountUp = (Args[2].X >= 0.0);
        int[] odom = new int[len];  double[] xxx = R.Store[Args[0].I].Data;
        for (int i = 0; i < len; i++) odom[i] = (int) Math.Round(xxx[i]);
        int outval = JM.Odometer(ref odom, NoBase, CountUp);
        if (outval >= 0)//it should always be, but leave this condition in anyway.
        { for (int i = 0; i < len; i++) xxx[i] = (double) odom[i]; }
        result.X = (double) outval;   break;
      }
      case 100: // MOMENTS(DataArray [, IncludeKurtosis]). Returns an array of size 5 (no kurtosis) or 6; [0] is the mean,
      // [1]&[2] are SD & variance using a divisor of (pop. size-1); [3] & [4] the same, for divisor of just pop. size. 
      // If IncludeKurtosis is present and not zero, then [5] is present and equals the kurtosis, calc'd using divisor pop.size.
      // Error raised if pop. size < 2.
      { int dataslot = Args[0].I;
        if (dataslot==-1) return Oops(Which, "the first arg. must be an array");
        int popsize = R.Store[Args[0].I].TotSz;
        if (popsize < 2) return Oops(Which, "the first arg. must have size >= 2");
        double[] datain = R.Store[Args[0].I].Data;
        if (popsize < datain.Length)
        { Array.Copy(R.Store[Args[0].I].Data, datain, popsize); }
        int errno;
        double[] moments = M2.SampleMoments(datain, out errno, (NoArgs > 1 && Args[1].X != 0) );
        if (errno != 0) return Oops(Which, "computation of the moments failed");
        int newslot = V.GenerateTempStoreRoom(moments.Length);
        R.Store[newslot].Data = moments;
        result.I = newslot;  
        break; 
      }
      case 101: // SETBINS(DataArray [, BinWidth, LoCentre, HiCentre, IncludeAllData ] ] ) -- Given an array of data, bins that data, e.g. for use
      // in a histogram. RETURNED:  A matrix of 3 rows and minimum column size 4. Here is an example, for BinWidth 2, LoCentre 11, HiCentre 17:
      //  There will be 1 + (17 - 11)/2, i.e. four, bins, which will cover intervals 10 <= x < 12; 12 <= x < 14; 14 <= x < 16; 16 <= x < 18.
      //  Using these figures, suppose that DataArray has 11 values < 10; then values 3, 8, 7, 4, in the bins; and 6 values >= 18.
      //  The returned matrix - size 4 x NoBins - will be this:
      // Row [0]: BIN STARTS:  [   10,    12,  14,  16    ]
      // Row [1]: BIN ENDS:    [   12,    14,  16,  18    ]
      // Row [2]: DATA:        [ {11+}3,  8,   7,   4{+6} ] // If IncludeAllData TRUE, the bits in braces are added in.
      // Row [3]: STATS:       [ NoBins, Mean, S.D., Var. ] SD and Variance use the traditional divisor (pop. size -1)
      // ARGUMENTS: 
      // Either just one, or all five. The purpose of just one arg. is to allow a quick overview of the data, before setting up a proper histogram.
      //    If only one arg., the basis for internal choice of parameters will be such that 3 bins fit between the mean and the SD,
      //    and that there are at least 12 bins total.
      //  DataArray: must have length at least 4. Array structure is ignored. 
      //  BinWidth: should exactly divide HiCentre - LoCentre, or output is going to look rather odd. If no. bins would be < 4,
      //    your value is replaced such that no. bins = 4.
      //  IncludeAllData: if 'true', data which falls more than half a binwidth beyond LoCentre or HiCentre will be excluded from graphing.
      //    (It is not excluded from the stats calculations that produce row 3, however.
      // STATS: [0] = no. bins, OR error indicator (see below); [1] = mean; [2]&[3] = SD and Variance, computed using (pop.size-1) 
      //  as divisor - the statistician's method.
      // ERROR: Row[3][0] is a negative number, the code being: -1 = DataArray length < 4; -2 = LoCentre and HiCentre either equal or crossed.
      //  In case of error, the matrix should otherwise be ignored; but it has column length 4, with rows [0] to [3] containing zeroes.  
      { int dataslot = Args[0].I;
        if (dataslot == -1) return Oops(Which, "the first arg. must be an array");
        StoreItem dataIt = R.Store[dataslot];
        if (NoArgs != 1 && NoArgs != 5) return Oops(Which, "either exactly 1 or exactly 5 args. are required");
        int NoBins;
        double[][] histology = null;
        double binwidth, locentre, hicentre;
        bool includealldata = false;
        if (NoArgs == 5)
        { double LoCut, HiCut;
          binwidth = Args[1].X;  if (binwidth <= 0) return Oops(Which, "2nd. arg. cannot be negative");
          locentre = Args[2].X;
          hicentre = Args[3].X; if (locentre >= hicentre) return Oops(Which, "2nd. arg. must be less than the 3rd. arg.");
          includealldata = (Args[4].X != 0.0);
          NoBins = Convert.ToInt32(1.0 + (hicentre - locentre)/binwidth);
          if (NoBins < 4) { NoBins = 4;  binwidth = (hicentre - locentre) / 3.0;  };
          LoCut = locentre - binwidth/2.0;  HiCut = hicentre + binwidth/2.0;          
          histology = M2.HistogramData(ref dataIt.Data, NoBins, LoCut, HiCut);
          if (!includealldata) { histology[2][0] = 0.0;  histology[2][NoBins+1] = 0.0; } // discard the data beyond locentre and hicentre.
        }
        else // only one argument:
        { histology = M2.HistogramData(ref dataIt.Data, 0);
        }
        // Set up the returned matrix:        
        double[] output;
        NoBins = (int) histology[3][0]; // will either be the user's set value or, if that was < 4, the machine-calculated value.
        if (NoBins < 0) // ERROR:
        { output = new double[16];  output[13] = NoBins;  NoBins = 4; } // NoBins starts with a neg. value: see spiel above for error codes.
        else // No error:
        {
          output = new double[4 * NoBins];
          Array.Copy(histology[0], 1, output, 0, NoBins);
          Array.Copy(histology[1], 1, output, NoBins, NoBins);
          Array.Copy(histology[2], 1, output, 2*NoBins, NoBins);
          if (includealldata)
          { output[2*NoBins] += histology[2][0];   output[3*NoBins-1] += histology[2][NoBins+1]; }
          Array.Copy(histology[3], 0, output, 3*NoBins, 4); // No data items per compartment.
        }
        int newslot = V.GenerateTempStoreRoom(NoBins, 4);
        R.Store[newslot].Data = output;
        result.I = newslot;  break;
      }
      case 102://SUM(Array [, scalar StartPtr [, scalar EndPtr ] ] ) - sum of the terms. No errors raised; silly ptrs. corrected,
      // though crossed ptrs. will return 0. No check for arrayhood of 2nd and 3rd. args.
      { int inslot = Args[0].I;  if (inslot == -1) return Oops(Which, "an array arg. is required");
        double[] indata = R.Store[inslot].Data;
        int len = indata.Length,  startptr = 0,   endptr = len-1;
        if (NoArgs > 1)
        { startptr = Convert.ToInt32(Args[1].X); if (NoArgs > 2) endptr = Convert.ToInt32(Args[2].X);
          if (startptr < 0) startptr = 0;  if (endptr >= len) endptr = len-1;
        }
        double sum = 0.0;
        for (int i = startptr; i <= endptr; i++) sum += indata[i];
        result.X = sum;  break;
      }

      case 103: // Case 1 (VOID): SHUFFLE(Array);  Case 2 (NONVOID): SHUFFLE(Array, bool ReturnOrigIndices).
        // Shuffles contents of Array. In case 2, returns an array, being the original indices of the shuffled elements.
        // If Array consists of only zeroes, what is returned is the array [0,1,2,...N-1] shuffled. (Only has point for case 1;
        // for case 2 it would just return a copy of the sorted Array.)
      { int slot = Args[0].I;
        if (slot == -1) return Oops(Which, "the first arg. must be an array");
        bool returnKey = (NoArgs > 1 && Args[1].X != 0.0);
        double[] indata = R.Store[slot].Data;
        int datalen = indata.Length;
        // Test for all entries being zero. If so, set it to [0,1,2,3,...] instead.
        bool allzeroes = true;
        for (int i=0; i < datalen; i++){ if (indata[i] != 0.0) { allzeroes = false; break; } }
        int[] theKey = JM.Shuffle(null, datalen, Rando); // returns a shuffle of the array [0, 1, 2, ..., (datalen-1)].
        if (allzeroes) R.Store[slot].Data = theKey._ToDubArray();
        else
        { double[] outdata = new double[datalen];
          for (int i = 0; i < datalen; i++) outdata[i] = indata[theKey[i]];
          R.Store[slot].Data = outdata;
        }
        // Return the sort key, if the nonvoid form:
        if (returnKey)
        { result.I = V.GenerateTempStoreRoom(datalen);
          R.Store[result.I].Data = theKey._ToDubArray();
        }
        break;
      }
      case 104: //ISARRAY(Variable) - returns 1 for array arg, 0 for scalar.
      { if (Args[0].I == -1)result.X = 0.0;  else result.X = 1.0; break;
      }
      case 105: //NORM(Array) - returns the Euclidean norm of the data strip.
      { int slot = Args[0].I;  if (slot == -1) return Oops(Which, "an array arg. is required");
        double z, xx = 0.0;    
        int len = R.Store[slot].TotSz;
        double[] indata = R.Store[slot].Data;
        for (int i = 0; i < len; i++){z = indata[i];  xx += z*z; }
        result.X = Math.Sqrt(xx);  break; }
      case 106: // INVERSE(Nonsing. square matrix [, Negligible]). If a singular
      // matrix, does not raise error, but returns a list array of size 1, value NaN (testable with 'empty(.)').
      { int MxSlot = Args[0].I;  double negligible = 0.0;
        if (NoArgs == 2) negligible = Math.Abs(Args[1].X); // too bad if an array.
        if (MxSlot == -1) return Oops(Which, "the arg. must be a matrix");
        int size = R.Store[MxSlot].DimSz[0];
        if (size < 2 || R.Store[MxSlot].DimCnt != 2 || R.Store[MxSlot].DimSz[1] != size)
        { return Oops(Which, "the first arg. must be a square matrix, 2x2 or larger"); }
        double[,] Mx = new double[size, size];  int offset = 0;
        double[] mxslotdata = R.Store[MxSlot].Data;
        for (int i = 0; i < size; i++)
        { offset = i*size;
          for (int j = 0; j < size; j++)
          { Mx[i,j] = mxslotdata[offset + j]; }
        }
        Quad quoo;
        double[,] MxOut = M2.InverseOf(Mx, out quoo, negligible);
        if (!quoo.B) result.I = EmptyArray();
        else
        { int newslot = V.GenerateTempStoreRoom(size, size);
          double[] data = new double[size*size];
          int cnt = 0;
          for (int i = 0; i < size; i++)
          { for (int j = 0; j < size; j++)
            { data[cnt] = MxOut[i,j];  cnt++; } }
          R.Store[newslot].Data = data;
          result.I = newslot;
        }
        break;
      }
      case 107: // TEXT(<any no. of args.>) - returns a chars. array, with same formatting rules as for 'write(..)'.
      { string txt = ArgsToDisplayString(0, Args, REFArgLocn, "");
        if (txt[0] == '\u0000') return Oops(Which, "cannot process this text: " + txt._Extent(1));
        int newslot = V.GenerateTempStoreRoom(txt.Length);
        StringToStoreroom(newslot, txt);
        R.Store[newslot].IsChars = true;
        result.I = newslot; break;
      }
      case 108: // LAST(array [, N] ): returns the last value in the array, if N is 0 or absent. Otherwise if N is an integer,
      // returns the element indexed by (last index of array) - |N|; crashes if the address is out of range.
      { int slot = Args[0].I;
        if (slot==-1) return Oops(Which, "the first arg. must be an array");
        int sz = R.Store[slot].Data.Length,  indx = sz-1;
        if (NoArgs > 1)
        { indx -= Math.Abs( (int) Args[1].X );
          if (indx < 0) return Oops(Which, "the back-count index is too large");
        }
        result.X = R.Store[slot].Data[indx]; 
        break;
      }
      case 109: // ERRORTRAP(scalar On_Off, scalar ErrorReturn): If two arguments, sets error trapping;
      // the 2nd. argument will be returned by functions if error occurs. (If you are allowing some further code
      // after the possible offending line and before detecting the error, make this value something that would 
      // not crash the further code.) If one argument, error trapping is turned off.
      // Once error trapping is set, system fn. errors will mostly not crash the program but will return ErrorReturn
      // and will append data to cumulative internal buffers retrievable by using 'errormsg()' and 'errorcnt()'.
      // (This particular fn. never returns an error state, to lessen risk of messups. Hence there is no test of args.)
      { if (NoArgs == 1) R.ErrorTrap = new Quad(false); // equivalent to turning error trapping off (i.e. .B is FALSE).
        else R.ErrorTrap = new Quad(0, Args[1].X, true, ""); 
        break;
      }
      case 110: // ERRORMSG(anything): returns R.ErrorTrap.S as a char. array ( = '\n'-delimited error messages).
      // If .S = "", returns an array of length 1, value 32.
      { int n = R.ErrorTrap.S.Length; if (n==0) n = 1; 
        int newslot = V.GenerateTempStoreRoom(n);
        if (n==1) R.Store[newslot].Data[0] = 32.0;
        else StringToStoreroom(newslot, R.ErrorTrap.S);
        R.Store[newslot].IsChars = true;
        result.I = newslot;  break;
      }
      case 111: // ERRORCNT(anything): returns R.ErrorTrap.I, no. of errors to date.
      { result.X = (double) R.ErrorTrap.I;  break;
      }
   // case 112: // LIST_TO -- see case 177
      case 113:// FOURIER(ListArray OR Matrix Nx2, bool Forward) -- RETURNS an Nx2 matrix (even if the input was a 
      // list array - i.e. represents noncomplex values). Actually, any structured array with lowest dim. 2 is accepted; 
      // structure of same dimensions is then returned. If Forward is omitted or is TRUE, a forward transform is done;
      // otherwise a reverse transform. (The reverse transform uses a universal coeff. of 1/N; the forward transform has none.)
      { int inslot = Args[0].I;  
        if (inslot == -1) return Oops(Which, "the first arg. must be an array");
        StoreItem instore = R.Store[inslot];
        int dimcnt = instore.DimCnt, dim0 = instore.DimSz[0];
        double[] indata = instore.Data;
        bool forward = true; if (NoArgs > 1 && Args[1].X == 0.0) forward = false;
        bool complexin = false;  
        if (dimcnt > 1)
        { if (dim0 == 2) complexin = true;
          else return Oops(Which, "the first arg. must be either a list array or a matrix with two columns");
        }
        int noCxVals = indata.Length; if (complexin) noCxVals /= 2;
        if (noCxVals < 2) return Oops(Which, "the first arg. must have at least two values (whether real or complex)");
        double[] toDFT;
        if (complexin) toDFT = indata;
        else // input array is a real array:
        { toDFT = new double[2*noCxVals];
          int cntr = 0;
          for (int i=0; i < noCxVals; i++)
          { toDFT[cntr] = indata[i];  cntr++;
            toDFT[cntr] = 0.0;  cntr++;
          }
        } // Now toDFT[] has the form: [real 0][imag 0][real 1][imag 1]... and has length 2 x noCxVals.
       // Build the bank of exponentials:
        double[] ExpReals = new double[noCxVals],  ExpImags = new double[noCxVals];
        double sign = -1.0;  if (!forward) sign = 1.0;
        double x, PIx2divN = 2.0 * Math.PI / (double) noCxVals;
        for (int i=0; i < noCxVals; i++)
        { x = (double) i * PIx2divN;
          ExpReals[i] = Math.Cos(x);  ExpImags[i] = sign * Math.Sin(x); 
        }
        double[] outdata = new double[2*noCxVals];
       // Do the DFT: 
        double Coeff = 1.0;  if (!forward) Coeff = 1.0 / (double) noCxVals;
        for (int k=0; k < noCxVals; k++)
        { double sumReal = 0.0, sumImag = 0.0, XnRe, XnIm, ExpRe, ExpIm;
          int ndx;
          for (int n=0; n < noCxVals; n++)
          { XnRe = toDFT[2*n];  XnIm = toDFT[2*n+1];
            ndx = (n * k) % noCxVals;
            ExpRe = ExpReals[ndx];  ExpIm = ExpImags[ndx];
            sumReal += XnRe*ExpRe - XnIm*ExpIm;
            sumImag += XnRe*ExpIm + XnIm*ExpRe;
          }            
          outdata[2*k] = Coeff * sumReal;  outdata[2*k+1] = Coeff * sumImag;
        }
        if (complexin) result.I = V.GenerateTempStoreRoom(instore.DimSz);
        else result.I = V.GenerateTempStoreRoom(2, noCxVals);
        R.Store[result.I].Data = outdata;        
        break;
      }
      case 114:  case 115: // RECT(Arr [, scalar VirtZero] ), POLAR(Arr [, scalar VirtZero]): Convert between rect. and polar complex values.
      // 'Arr' can have any structure; the only constraint is that its total length must be even. The output array will have
      //  exactly the same structure. Input values[2n] will be taken as real / abs. value, [2n+1] as imag / angle.
      //  If VirtZero present and positive, input and output values at and below +/- this are returned as zero.
      //  Note that POLAR(.) returns angle 0 for the angle-indeterminate case of 0 + j.0. Returned angle in general is always between +/- PI.
      // Crashes if RECT, Arr[2n] is negative (or, if VirtZero suppplied, more negative than - VirtZero).
      { int oldslot = Args[0].I;
        if (oldslot==-1) return Oops(Which, "the first arg. must be an array");
        int newslot = V.GenerateTempStoreRoom(R.Store[oldslot].DimSz);
        double virtzero = 0.0;  if (NoArgs == 2) virtzero = Args[1].X; 
        double[] datain = R.Store[oldslot].Data;
        int len = datain.Length; if (len%2 != 0) return Oops(Which, "the first arg. must be of even length");
        double[] dataout = R.Store[newslot].Data;
        TCx cx = new TCx(0,0);
        for (int i=0; i < len; i += 2)
        { cx.re = datain[i];   cx.im = datain[i+1];  
          if (virtzero > 0.0) cx.Defluff(virtzero);
          if (Which == 114)  // Polar -->  Rect
          { if (cx.re < 0) return Oops(Which, "the absolute value of a polar number must not be negative");
            cx.polar = true;     cx.ToRect();  
          }
          else { cx.polar = false;    cx.ToPolar(); } // Rect --> Polar
          if (virtzero > 0.0) cx.Defluff(virtzero);
          dataout[i] = cx.re; dataout[i + 1] = cx.im;
        }  
        result.I = newslot; break;
      }
      
      case 116: // COMPLEX( Array1, Var2, array Opn, array PolRect [, VirtZero] )
      // Array1: the output will hold its structure. Every  2nth. value is taken as real/absol.value, and every 2n+1th. value as imag/angle. 
      //  Must have even length. Var2: if scalar, converted to an array with [0] = scalar value, [1] = 0. Otherwise must be either of length 2
      //  in which case every element of Var1 interacts with it) or of same length as Array1 (for an element-by-element opn.)
      // Opn: 1st. element must have ASCII value of: '+', '-', '*', '/', '^'. Other elements ignored. Improper 1st. element crashes.
      // PolRect:  must have at least 3 elements, each being ASCII value of: 'P', 'p', 'R', 'r'. Later array elements ignored.
      // [0] is form of Var1; [1] is form of Var2; and [2] is form of output. VirtZero, if present and > 0, is level at and below which 
      // inputs and output elements are regarded as zero.
      // Output is an array whose size is dictated by Var1 and Var2. 
      // Crash only occurs (1) if Polar absol. value is ever < 0 / < -VirtZero; (b) if Opn is '^' and there is an imag. component.
      // Whatever the structure of Var1, the output always has its exact same structure.
      { int var1slot = Args[0].I;  double[] var1data = null, var2data;  double var1len = 0, var2len = 0;  bool oops = false;
        if (var1slot == -1) oops = true; 
        else { var1data = R.Store[var1slot].Data;  var1len = var1data.Length;  if (var1len %2 != 0) oops = true; }
        if (oops) return Oops(Which, "the first arg. must be an array, of even size");
        int n=0,  var2slot = Args[1].I; 
        if (var2slot == -1) { var2data = new double[] { Args[1].X, 0.0}; var2len = 2; }
        else { var2data = R.Store[var2slot].Data;  var2len = var2data.Length; }
        if (var2len != var1len && var2len != 2) return Oops(Which, "the first two args. are mismatched");
        int opslot = Args[2].I; 
        if (opslot == -1) return Oops(Which, "the third arg. should be an array");
        char opn = R.Store[opslot].Data[0]._ToChar(' ');
        if ( ("+-*/^").IndexOf(opn) == -1) return Oops(Which, "the third arg. does not hold a recognized mathematical sign");
        int typeslot = Args[3].I;
        if (typeslot == -1 || R.Store[typeslot].TotSz < 3) return Oops(Which, "the fourth arg. should be an array of size 3");
        bool[] ispolar = new bool[3]; char ch = ' ';
        string stroo = DubArrToChars(R.Store[typeslot].Data, 65, 122, '?', '?'); // i.e. all values outside the range 'A'..'z' --> '?'
        stroo = stroo.ToUpper();
        for (int i=0; i < 3; i++)
        { ch = stroo[i];
          if (ch != 'P' && ch != 'R') return Oops(Which, "the third arg. elements must be 'P','p','R' or 'r'");
          ispolar[i] = (ch == 'P');
        }
        double virtzero = 0.0; if (NoArgs > 4 && Args[4].X >= 0.0) virtzero = Args[4].X;
        // Length settings: either Var1 and Var2 are of equal length (in which case the output length is the same), or one has length 2
        //  (in which case the output length is that of the other). If Var1 has structure, and is not the smaller array, that structure is 
        //  imparted to the output. The structure of Var2 is always ignored.
        int newslot = V.GenerateTempStoreRoom(R.Store[var1slot].DimSz);
        double[] dataout = R.Store[newslot].Data;
       // THE OPERATIONS
        TCx cx1  = new TCx(0,0),  cx2 = new TCx(0,0), cxout = new TCx(0,0);
        for (int i=0; i < var1len; i += 2)
        { cx1.re = var1data[i];  cx1.im = var1data[i+1];  cx1.polar = ispolar[0]; 
          if (var2len == 2) n = 0;  else n = i;
          cx2.re = var2data[n];  cx2.im = var2data[n+1];  cx2.polar = ispolar[1];
          if (virtzero > 0.0) { cx1.Defluff(virtzero);   cx2.Defluff(virtzero); }
          if      (opn == '+') cxout = M2.CxAdd(cx1, cx2, ispolar[2]);
          else if (opn == '-') cxout = M2.CxSubtract(cx1, cx2, ispolar[2]);
          else if (opn == '*') cxout = M2.CxMult(cx1, cx2, ispolar[2]);
          else if (opn == '/')
          { if (cx2.re == 0.0)
            { if (cx2.polar || cx2.im == 0.0) return Oops(Which, "attempt was made to divide by 0 (element {0} of the 2nd. array)", n);
            }
            cxout = M2.CxDivide(cx1, cx2, ispolar[2]);
          }  
          else if (opn == '^')
          { if (cx2.im != 0) return Oops(Which, "a complex power is not allowed");
            cxout = cx1.ToPower(cx2.re, ispolar[2]);
          }
          // For all operations, defluff:
          if (virtzero > 0.0) cxout.Defluff(virtzero);
          dataout[i] = cxout.re; dataout[i + 1] = cxout.im;
        }          
        result.I = newslot; break;
      }
      case 117: // MERGE(Array1, Array2 [, Array3...]   [, scalars Extent1, Extent2 [, Extent 3...]..] ).
      // Consider what happens if there is exactly as many Extent scalars as there are preceding arrays. Consider three arrays; let their
      //   elements be [A1, A2, ...], [B1, B2, ...] and [C1, C2, ...]. Suppose the extent scalars are 1, 3, 2. Then the returned array
      //   is [A1,  B1, B2, B3,  C1, C2,    A2,  B4, B5, B6,  C3, C4,  ... ]. Collation of values ends when the first attempt to get
      //   a new datum fails because there is no data left in the donor array. E.g. if the second array above had only values B1 to B5,
      //   the returned array would be [A1,  B1, B2, B3,   C1, C2,  A2,  B4, B5]. (The input arrays do NOT have to have the same lengths.)
      // If there are less extents than arrays, missing extents default to 1. If there are more, the excess extents are simply ignored.
      // An extent <= 0 is allowed; the corresponding array will simply be ignored. But if all extents are <= 0, an error is raised.
      // The chars. rating of the returned array is that of the first input array.
      { int[] extents = new int[NoArgs],  starts = new int[NoArgs],  inarrlengths = new int[NoArgs]; // Will often be larger than needed.
        int n, noArrays = 0;
        bool weveHitTheScalars = false,  outputChars = false;
        List<double> allArrays = new List<double>();         
        int lenCntr = 0, maxExtent = 0;
        for (int i = 0; i < NoArgs; i++)
        { int slotto = Args[i].I;
          if (slotto >= 0)
          { if (weveHitTheScalars) return Oops(Which, "all arrays come first, then the scalars");
            StoreItem it = R.Store[slotto];  
            inarrlengths[i] = it.TotSz;
            starts[i] = lenCntr; // before lenCntr is incremented.
            lenCntr += inarrlengths[i];
            allArrays.AddRange(it.Data); 
            noArrays++;
            if (noArrays == 1) outputChars = it.IsChars;
          }
          else // scalar:
          { weveHitTheScalars = true;
            if (noArrays == 0) return Oops(Which, "scalar arg. preceded all arrays");
            n = Convert.ToInt32(Args[i].X); // no matter if is 0 or negative; simply the loop in 'm' below will not be entered.
            extents[i - noArrays] = n;
            if (n > maxExtent) maxExtent = n;
          }
        }
        if (noArrays < 2) return Oops(Which, "there must be at least two arrays");
        // Now we make up for any deficiency in the number of extent args.
        for (int i = NoArgs; i < 2 * noArrays; i++) { extents[i - noArrays] = 1;  if (maxExtent == 0) maxExtent = 1; }
        if (maxExtent == 0) return Oops(Which, "scalar args. cannot all be zero");
        // prepare the result:
        double[] indata = allArrays.ToArray();
        List<double> Output = new List<double>(indata.Length); // its final length will be <= indata.Length    
        bool jobdone = false;
        int ndx, loopcnt = 0;
        while (true)
        { for (int k = 0; k < noArrays; k++)
          { int ex = extents[k], thislen = inarrlengths[k], thisstart = starts[k];
            for (int m = 0; m < ex; m++) // extract the next extent[k] chars. from indata. (If extent[k] <= 0, this loop is simply bypassed.)
            { ndx = ex * loopcnt + m; // amount of this arg. array already processed + next char. of this extent      
              if (ndx >= thislen) { jobdone = true;  break; }
              Output.Add(indata[thisstart + ndx]);
            }
            if (jobdone) break;
          }
          if (jobdone) break;
          loopcnt++;
        }
        result.I = V.GenerateTempStoreRoom(Output.Count);
        R.Store[result.I].Data = Output.ToArray();
        R.Store[result.I].IsChars = outputChars;
        break;
      }  
     case 118: // GRAPHRESIZE(GraphID, one array / two scalars: HorizPixels, VertPixels) - Void. ATTEMPTS to reset exact pixel dimensions
      // of the graph perimeter. There already has to be a graph in existence. For dimensions within obvious limits, it should be what you get.
      // Dimensions <= 20 pixel are ignored, returning FALSE. No errors raised; if graph does not exist or args. are too small, simply
      // nothing happens.
      { int graphID = (int) Args[0].X;
        double[] values = AccumulateValuesFromArgs(Args, 1); if (values.Length != 2) break;
        int newGraphWidth = Convert.ToInt32(values[0]);
        int newGraphHeight = Convert.ToInt32(values[1]);
        if (newGraphWidth < 20.0 || newGraphHeight < 20.0) break;
        Graph graf = null;
        Trio trill = Board.GetBoardOfGraph(graphID, out graf);
        if (graf == null) break;
        int[] sizery = graf.SizingParameters(); // 'sizery' has the following fields: ACTUAL BOARD dimensions: [0] = width, [1] = height,
          // [2] = LblHeader width, [3] = LblHeader height, [4] = drawing area width, [5] = drawing area height, [6] = TVBelow width ( or 0,
          // if no TVBelow exists), [7] = TVBelow height (or 0). ORIG. REQUESTED BOARD dimensions: [8] = width, [9] = height,
          // [10] = eboxHeader height, [11] = drawing area height, [12] = bool: 1 if TVBelow was requested, 0 if not.
          // Elements [13] to [19] are currently unused. GRAPH perimeter dimensions: [20] = width, [21] = height.
        int newBoardWidth = sizery[0] - sizery[20] + newGraphWidth; // Diff. between first 2 = width taken up by graph's vertical axis text.
        int newBoardHeight = (int) ((275.0 + 5 * values[1]) / 4.0); // Found by trial and error, the unknown being how Gtk adjusts the
                                                                     // height of the three vertical components as board height increases.
        Board.TryResize(trill.X, newBoardWidth, newBoardHeight);
        Board.ForceRedraw(trill.X, false);
        break;
      }
      case 119:  case 120: // 119 = PLACE(Array; Scalars LowLimit [, HighLimit] ) -
      // returns a scalar, = the first element that is between limits or on either limit. 
      // (If no high limit supplied, it defaults to double.MaxValue.)
      // If none is in the range, returns -1.
      // 120 = PLACES(same args) - returns an array, size 6: [0] = as above; [1] = last such (or -1);
      // [2] = number between these two points which are below the lower limit; [3] =
      // the number between the two points which are above the upper limit; [4] = the total 
      // number below, in the whole array; [5] = the total number above, in the whole array.
      { int slot = Args[0].I;
        if (slot == -1) return Oops(Which, "the first arg. must be an array");
        double lolimit = Args[1].X,   hilimit = double.MaxValue;
        if (NoArgs > 2) hilimit = Args[2].X;
        if (hilimit < lolimit) return Oops(Which, "the low limit (2nd. arg.) is more than the high limit (3rd. arg.)");
        double[] dudu = R.Store[slot].Data;   int len = R.Store[slot].TotSz;
        // Find the first and last between or on limits:
        int firstin = -1, lastin = -1; // if none within the range, both return as -1.
        for (int i = 0; i < len; i++)
        { if (dudu[i] >= lolimit && dudu[i] <= hilimit)
          { if (firstin == -1) firstin = i;   
            lastin = i;
        } } 
        if (Which == 119) {result.X = (double) firstin; break; } // 'PLACE(.)' just returns this, as a scalar.
        // 'PLACES(.)', so return all the stats as an array:
        int totalbelow = 0, totalabove = 0, insidebelow = 0, insideabove = 0;
        for (int i = 0; i < len; i++)
        { if (dudu[i] < lolimit)
          { totalbelow++;
            if (i > firstin && i < lastin) insidebelow++; }
          else if (dudu[i] > hilimit)
          { totalabove++;
            if (i > firstin && i < lastin) insideabove++; }
        }
        int newslot = V.GenerateTempStoreRoom(6);
        double[] nunu = R.Store[newslot].Data;
        nunu[0] = firstin;   nunu[1] = lastin;  nunu[2] = insidebelow;  nunu[3] = insideabove;
        nunu[4] = totalbelow;  nunu[5] = totalabove;
        result.I = newslot;  break; }
      case 121: // REORDER(Array: Key; one or more arrays Arr1, Arr2,... of SAME SIZE as Key):
      // Arrays Arr1, Arr2 ... will have their elements rearranged such that Arri[n] will contain
      // what was in Arri[Key[n]]. No error, if duplication of Key indices; but all indices must be in range.
      // (True rounding of the values in Key occurs.) 
      // VOID. The non-key arg. arrays are changed by the fn.
      { for (int ii=0; ii<NoArgs; ii++) { if (Args[ii].I == -1) return Oops(Which, "args. must be arrays"); }
        int size = R.Store[Args[0].I].TotSz;
        for (int ii=0; ii<NoArgs; ii++)
        { if (R.Store[Args[ii].I].TotSz != size) return Oops(Which, "all of the args. must have the same size"); }
        // Set up the key as a list of integers, which must be in the range 0 to size-1:
        double[] keynos = R.Store[Args[0].I].Data;
        int[] key = new int[size];   int n;
        for (int ii = 0; ii < size; ii++)
        { n = (int) (keynos[ii] + 0.5);
          if (n < 0 || n >= size)
          { return Oops(Which, "element [{0}] in the key array is either < 0 or >= the subject array's size {1}", n, size); }
          key[ii] = n;
        }
        // Substitute:
        for (int ii = 1; ii < NoArgs; ii++)
        { double[] dudu = R.Store[Args[ii].I].Data;
          double[] nunu = new double[size];
          for (int jj = 0; jj < size; jj++) nunu[jj] = dudu[key[jj]];
          R.Store[Args[ii].I].Data = nunu;
        }        
        break;
      }
      case 122: case 123: // MATCHES/MISMATCHES(Variable1, Variable2 [, Scalar Tolerance]), 
      // MATCHES: No arg. 'Tolerance': Returns an object of the same structure as Variable1 (scalar allowed), but with 1 wherever
      //  Variable1[i] = Variable[2], and 0 elsewhere. If Tolerance supplied and > 0, the replacement is 1 if the two elements differ 
      //  by Tolerance or less, otherwise 0.  MISMATCHES: the inverse: switch 1 and 0.
      // Allowed variable combns: (1) scalar-scalar; (2) array-array (same length; structure not checked); (3) array-scalar
      //  (every element of array checked against the one scalar). Not allowed: scalar-array.
      { double tolerance = 0.0;  
        if (NoArgs > 2) { tolerance = Args[2].X;  if (tolerance < 0) tolerance = 0; } // no test for arrayhood.
        int slot0 = Args[0].I,  slot1 = Args[1].I;
        double matchValue = 1.0, nomatchValue = 0.0;  if (Which == 123) { matchValue = 0.0;  nomatchValue = 1.0;}
        if (slot0 == -1) // first argument scalar:
        { if (slot1 == -1) // both arguments scalar:
          { if (Math.Abs(Args[0].X - Args[1].X) <= tolerance) result.X = matchValue;  else result.X = nomatchValue; }
          else return Oops(Which, "the combination of a first arg. scalar and a second arg. an array is not allowed");
        }
        else // First arg. is an array:
        { double[] dub0 = R.Store[slot0].Data;   int dub0len = dub0.Length;
          double[] dub1, dubout = new double[dub0len];
          if (slot1 == -1) // second array a scalar: Build an array consisting of repetitions of this value.
          { double x = Args[1].X;  dub1 = new double[dub0len];  for (int i=0; i < dub0len; i++) dub1[i] = x; }
          else dub1 = R.Store[slot1].Data;
          if (dub1.Length != dub0len) return Oops(Which, "arrays have different lengths");
          // Arrays now guaranteed to be of equal length:        
          for (int i=0; i < dub0len; i++)
          { if (Math.Abs(dub0[i] - dub1[i]) <= tolerance) dubout[i] = matchValue;  else dubout[i] = nomatchValue; }
          int newslot = V.GenerateTempStoreRoom(R.Store[slot0].DimSz);
          R.Store[newslot].Data = dubout;  result.I = newslot;
        }
        break; 
      }
      case 124: case 125: // NOZERO / ALLZERO ((Variable [, Scalar Tolerance])) -- returns scalar 1 (true) or 0 (false):
      // nozero(.) returns TRUE if no zeroes found (or no |value| <= Tolerance); allzero(.) returns TRUE if only zeroes found (or etc.).
      // Tolerance ignored if not positive.
      { double tolerance = 0.0;  
        if (NoArgs == 2) { tolerance = Args[1].X;  if (tolerance < 0.0) tolerance = 0.0; } // no test for arrayhood.
        int slot = Args[0].I;
        int zeroCnt = 0;  int len; // We will compare the two later, to get the final return.
        if (slot == -1) // first argument scalar:
        { len = 1;
          if (Math.Abs(Args[0].X) <= tolerance) zeroCnt = 1; }
        else // first argument an array:
        { double[] dub = R.Store[slot].Data;   len = dub.Length;
          for (int i=0; i < len; i++)
          { if (Math.Abs(dub[i]) <= tolerance) zeroCnt++; }
        }
        if ( (Which == 124 && zeroCnt == 0) || (Which == 125 && zeroCnt == len) ) result.X = 1.0; // otherwise .X stays as 0.
        break; 
      }
      case 126: case 127: // IS(Variable [, Scalar Tolerance]), NOT(the same).
      // IS: Returns an object ('Object') of the same structure as Variable (can even be scalar), with Object[i] = (Variable[i] != 0).
      // NOT: Ditto, but Object[i] = (Variable[i] == 0). Obviously in both cases, only values 0 and 1 will ever be present in Object.
      // If Tolerance <= 0 it is ignored. Otherwise if |Variable[i]| <= Tolerance it is treated as if it were exactly zero.
      { double tolerance = 0.0;  
        if (NoArgs == 2) { tolerance = Args[1].X;  if (tolerance < 0.0) tolerance = 0.0; } // no test for arrayhood.
        int slot = Args[0].I, newslot;
        // Define replacement object 
        double IfNotZero = 1.0, IfZero = 0.0;    if (Which == 127) { IfNotZero = 0.0;  IfZero = 1.0; } // IS / NOT replacemt values.
        if (slot == -1) // first argument scalar:
        { if (Math.Abs(Args[0].X) <= tolerance) result.X = IfZero;  else result.X = IfNotZero; }
        else // first argument an array:
        { double[] dub = R.Store[slot].Data;   int dublen = dub.Length;
          double[] dubout = new double[dublen];
          for (int i=0; i < dublen; i++)
          { if (Math.Abs(dub[i]) <= tolerance) dubout[i] = IfZero;  else dubout[i] = IfNotZero; }
          newslot = V.GenerateTempStoreRoom(R.Store[slot].DimSz);
          R.Store[newslot].Data = dubout;  result.I = newslot;
        }
        break; 
      }
      case 128: // BKMKCOPY    (scalar/array Source, scalar StartPtr, array OpenBkMk, array CloseBkMk).
      case 129: // BKMKREPLACE (array Source, scalar StartPtr, array OpenBkMk, array CloseBkMk, array NewData [, bool RetainBkMks].
      // 'Source' - if an array, the data content of the array is the context of copying and replacing; if scalar, then the whole content
      //   of the text currently in the Assignments Window (not nec. the text that was there when the program started running) is the context.
      //   The structure of Source is never accessed; in the case of bkmkreplace(.), the output is always a list array.
      // 'StartPtr' - search begins here. (This allows for the case where one of the bookmarks occurs earlier in text, e.g. in an explanatory note.)
      // 'OpenBkMk', 'CloseBkMk': can be any sequence, but should be unique, and the first occurrence of both (including in remarked-out text'
      //   If the context is program code) should be the occurrence intended for copying / replacing. The bookmarks themselves are not copied
      //   by 'bkmkcopy(.)'.
      // 'RetainBkMks' - if FALSE, the bookmarks are removed from the returned array, after the insertion.
      // Returned:
      //  BkMkCopy: The given extent of Source. If either bookmark is not found, returns 'empty' array (length 1, value NaN). Also, if no data
      //   between bookmarks, returns 'empty' array (length 1, value NaN).
      //  BkMkReplace: If either bookmark not found, returns the original array (as a list array), untouched.
      {
        int sourceslot = Args[0].I, openerslot = Args[2].I, closerslot = Args[3].I;
        if (openerslot == -1 || closerslot == -1) return Oops(Which, "3rd. and 4th. arg.s must be arrays");
        double[] sourcedata;
        bool asChars;
        if (sourceslot == -1)
        { string ss = MainWindow.ThisWindow.CopyOfREditAssText();
          sourcedata = StringToStoreroom(-1, ss);
          asChars = true;
        }
        else { sourcedata = R.Store[sourceslot].Data;  asChars = R.Store[sourceslot].IsChars; }
        double[] openerdata = R.Store[openerslot].Data,  closerdata = R.Store[closerslot].Data;
        int sourcelen = sourcedata.Length,  openerlen = openerdata.Length,  closerlen = closerdata.Length;
        int n=0, startptr = Convert.ToInt32(Args[1].X);  if (startptr < 0) startptr = 0; else if (startptr >= sourcelen) startptr = sourcelen-1;
       // Find the opener and closer; if either not found, opt out early.
        int openerptr=0, closerptr=0, enclosedptr=0;
        bool optout = false;
        if (openerlen == 1 && (openerdata[0] == 0.0 || openerdata[0] == 32.0)) // then take all from the start:
        { openerlen = 0; openerptr = 0; }
        else
        { openerptr = JS.FindInDubArray(ref sourcedata, ref openerdata, startptr);
          if (openerptr == -1) optout = true;
        }
        if (!optout)
        { enclosedptr = openerptr + openerlen; // Points to what immediately follows the bookmark.
          if (closerlen == 1 && (closerdata[0] == 0.0 || closerdata[0] == 32.0)) // then take all from the start:
          { closerlen = 0; closerptr = sourcelen; }
          else
          { closerptr = JS.FindInDubArray(ref sourcedata, ref closerdata, enclosedptr);
            if (closerptr == -1) optout = true;
          }
        }  
        if (optout)
        { if (Which == 128){ result.I = EmptyArray(); break; } // BkMkCopy, so return nothing.
          else // BkMkReplace - return all the data of the original array, with no insertion and no editing:
          { n = V.GenerateTempStoreRoom(sourcelen);
            R.Store[n].IsChars = asChars;
            Array.Copy(sourcedata, R.Store[n].Data, sourcelen);          
            result.I = n;  break;
          }
        }  
      // NO ERRORS, SO GET ON WITH THE ACTION:
       // BkMkCopy:
        int ptr=0, outlen=0, outslot=0, insertlen=0, insertslot=0, leftover=0; 
        bool retainbkmks=false;  double[] insertdata=null, outdata=null;
        if (Which == 128)
        { if (closerptr == enclosedptr) { result.I = EmptyArray(); break; } // nothing between the two bkmks.
          outlen = closerptr - enclosedptr; 
          outslot = V.GenerateTempStoreRoom(outlen); 
          Array.Copy(sourcedata, enclosedptr, R.Store[outslot].Data, 0, outlen);
        }
       // BkMkReplace: 
        else
        { insertslot = Args[4].I;  insertdata = R.Store[insertslot].Data;  insertlen = insertdata.Length;
          retainbkmks = (NoArgs > 5 && Args[5].X != 0.0);
          leftover = sourcelen - closerptr - closerlen; // length of source text beyond closing bookmark.
          outlen = openerptr + insertlen + leftover; // length of output if overwriting bookmarks.
          if (retainbkmks) outlen += openerlen + closerlen;
          outslot = V.GenerateTempStoreRoom(outlen);
          outdata = R.Store[outslot].Data;
          // copy the bit before the first bookmark:
          if (openerptr > 0) Array.Copy(sourcedata, 0, outdata, 0, openerptr);
          // copy the opener bookmark, if required:
          ptr = openerptr; // keeps track of insertion point into outdata, irrespective of value of 'retainbkmks'.
          if (retainbkmks) { Array.Copy(openerdata, 0, outdata, openerptr, openerlen); ptr += openerlen; }
          // copy the insertion text:
          Array.Copy(insertdata, 0, outdata, ptr, insertlen);  ptr += insertlen;
          // copy the closer bookmark, if required:
          if (retainbkmks) { Array.Copy(closerdata, 0, outdata, ptr, closerlen); ptr += closerlen; }
          // copy the remaining source text:
          if (outlen > 0) Array.Copy(sourcedata, closerptr + closerlen, outdata, ptr, leftover);
        }
        R.Store[outslot].IsChars = asChars;
        result.I = outslot; break; 
      }
     case 130: // ROWOP (matrix Mx, RowNo, Coeff1, array RorC1, Tier1 [, array Sign, Coeff2, array RorC2, Tier2] )  VOID.
                // arg. no.       0    1       2            3      4              5      6            7       8
      // Row Mx[RowNo] is replaced by { Coeff1.Tier1 . Operation . Coeff2.Tier2 }. RorC is 'R','r','C','c', or anything else (e.g. ' ')
      //  for scalar. (E.g. for "Mx[3] = 2*Mx[][1] + 5", use args (Mx, 3, 2, 'C', 1, '+', 5, ' ', -1).
      case 131: // COLOP (matrix Mx, ColNo, Coeff1, array RorC1, array Tier1, array Sign, array Coeff2, array RorC2, array Tier2)
      // All the same, for Mx[][ColNo].
      { int n = ArgTypesCode(Args);  char sign = ' ';
        if (n != 21121 && n != 211212121) return Oops(Which, "wrong deployment of arg types");
       // COLLATE ALL THE ARGUMENTS
        bool isRowOp = (Which == 130), isBinary = (NoArgs == 9);
        int mxslot = Args[0].I;  int[] mxdimsz = R.Store[mxslot].DimSz;  
        int mxrows = mxdimsz[1], mxcols = mxdimsz[0]; 
        if (mxrows == 0) return Oops(Which, "the first arg. must be a matrix");
        double[] mxdata = R.Store[mxslot].Data;
        int victim = Convert.ToInt32(Args[1].X); // row or column no. that will be altered.
        if (victim < 0 || (isRowOp && victim >= mxrows) || (!isRowOp && victim >= mxcols) )
        { return Oops(Which, "the second arg. is out of range"); }
        double coeff1 = Args[2].X,  coeff2=0;
        if (isBinary) coeff2 = Args[6].X;
        char tierType1 = '1', tierType2 = tierType1;  // Will end up as 'R', 'C' or '1' (last for no-tier).
        double x = R.Store[Args[3].I].Data[0];
        if (x == 82.0 || x == 114.0) tierType1 = 'R'; else if (x == 67.0 || x == 99.0) tierType1 = 'C';
        if (isBinary)
        { x = R.Store[Args[7].I].Data[0];
          if (x == 82.0 || x == 114.0) tierType2 = 'R'; else if (x == 67.0 || x == 99.0) tierType2 = 'C';
        }
        int operand1 = Convert.ToInt32(Args[4].X), operand2 = -1;
        if (isBinary) 
        { operand2 = Convert.ToInt32(Args[8].X);
          sign = R.Store[Args[5].I].Data[0]._ToChar(' ');
        }
       // Find the length of the row/col which will be overwritten:
        int dueLen = mxcols;  if (Which == 131) dueLen = mxrows; // rowop(..) / colop(..).
       
        // check sizes, as CopyMxUnit(..) takes no prisoners:
        n = 0;
        if (tierType1 == 'R')
        { if (operand1 < 0 || operand1 >= mxrows) n = 10; else if (dueLen != mxcols) n = 15; }
        else if (tierType1 == 'C')
        { if (operand1 < 0 || operand1 >= mxcols) n = 20; else if (dueLen != mxrows) n = 25; }
        if (n == 0 && isBinary)
        { if (tierType2 == 'R')
          { if (operand2 < 0 || operand2 >= mxrows) n = 50; else if (dueLen != mxcols) n = 55; }
          else if (tierType2 == 'C')
          { if (operand2 < 0 || operand2 >= mxcols) n = 60; else if (dueLen != mxrows) n = 65; }
        }
        string ss, tt;
        if (n != 0)
        { if (n < 50) ss = "the 5th."; else ss = "the 9th.";
          if (n % 10 == 0) tt = "is out of range"; else tt = "has row/col size incompatibility with matrix";
          return Oops(Which, ss + " arg. " + tt);
        }                
       // Retrieve the first row or column :
        double[] tier1data = null, tier2data = null;
        if (tierType1 == '1') // then generate a row/col with all values 1.
        { tier1data = new double[dueLen];  for (int i=0; i < dueLen;  i++) tier1data[i] = 1.0; }
        else tier1data = CopyMxUnit( (tierType1 == 'R'), operand1, ref mxdata, mxdimsz);
       // Multiply it by Coeff1: 
        for (int i=0; i < dueLen; i++) tier1data[i] *= coeff1;   
        if (isBinary)
        {// Retrieve the second row or column :
          if (tierType2 == '1') // then generate a row/col with all values 1.
          { tier2data = new double[dueLen];  for (int i=0; i < dueLen;  i++) tier2data[i] = 1.0; }
          else tier2data = CopyMxUnit( (tierType2 == 'R'), operand2, ref mxdata, mxdimsz);
         // Multiply it by Coeff2:
          for (int i=0; i < dueLen; i++) tier2data[i] *= coeff2;
       // CARRY OUT BINARY OPERATION, and put result into tier1data:
          for (int i = 0; i < dueLen; i++)
          { if      (sign == '+') tier1data[i] += tier2data[i];
            else if (sign == '-') tier1data[i] -= tier2data[i];
            else if (sign == '-') tier1data[i] -= tier2data[i];
            else if (sign == '*') tier1data[i] *= tier2data[i];
            else if (sign == '/') tier1data[i] /= tier2data[i]; // The user's watchout if division by 0 results.
            else return Oops(Which, "the 6th. arg. represents an unrecognized operation");
          }
        }
       // IMPLANT THE ROW/COL:
        F.InsertMxUnit(isRowOp, victim, ref mxdata, ref tier1data, mxdimsz);
        break;
      }
      case 132: // RANDSIGN (scalar or array Data [, scalar ProbOfPlus] ). A copy of the argument is returned with all data elements
      // randomly mult. by + or -1. The probability of getting '+' is either given by the last arg. (which MUST lie between 0 and 1
      // inclusive) or, if argument omitted, is 0.5.
      { int inslot = Args[0].I;   
        if (inslot == -1) { result.X = Args[0].X * (1 - 2 * Rando.Next(2) ); break; }
        double[] indata = R.Store[inslot].Data;   int len = indata.Length;
        double[] outdata = new double[len];
        double ProbOfPlus = 0.5;
        if (NoArgs > 1)
        { ProbOfPlus = Args[1].X;
          if (ProbOfPlus < 0.0 || ProbOfPlus > 1.0) return Oops(Which, "if a 2nd. arg. supplied, it must be in the range 0 to 1 inclusive");
        }
        for (int i=0; i < len; i++)
        { if (Rando.NextDouble() < ProbOfPlus) outdata[i] = indata[i];
          else outdata[i] = -indata[i];
        }
        result.I = V.GenerateTempStoreRoom(len);
        R.Store[result.I].Data = outdata;  break; 
      }
      case 133: // STARTTIMER(one or more timer indexes, as any mix of scalars/arrays). Void. Simply sets Timer[ timer no. ] to the present time.
      { int[] tickers = AccumulateValuesFromArgs(Args)._ToIntArray();
        int n, tickersLen = tickers.Length;
        for (int i=0; i < tickersLen; i++)
        { n = tickers[i];
          if (n < 0 || n >= NoTimers) return Oops(Which, "each timer index must be between 0 and {0}", NoTimers-1);
        }
        // The following is in a separate loop so as to minimize time between the setting of individual tickers.
        // Experimentation shows of the order of 3 or 4 microsecs. between timer settings in my current computer (Oct 2012).
        // BUT the bloody garbage collector (I presume) occasionally buts in, and then there is a one-off delay of several more microseconds.
        for (int i=0; i < tickersLen; i++)
        { Timers[tickers[i]] = DateTime.Now;
          TimerHolds[i] = TimeZero;
        }
        break;
      }
      case 134: // [A] TIMER( TimerID). Returns the no. milliseconds since the timer was set by 'starttimer(TimerID)'; if
      {         //  no such call preceded, then 'timer(TimerID)' returns the absolute time (msec since the start of 1 AD).
                //  Out of range TimerID returns an error, EXCEPT for two special values -1 (which returns time since the
                //  user program began) and -2 (which returns time since this instance of MonoMaths began). One use of this
                //  is to check for validity of 'timer(TimerID)' (i.e. check that it was initialized), as all values returned
                //  should be less than both of these.
                // [B] TIMER( TimerID, char. array DoWhat) -- Only DoWhat[0] is accessed: 'P' or 'p' --> timer is in effect paused;
                //  'R' or 'r' --> timing is resumed (if paused). In both cases the timer's current reading is returned.
                //  If 'R' without a prior pause; if 'P' after an earlier unresumed pause, nothing happens (except for this
                //  return of the current reading).
                // Note that there is no need for a 'clear' instruction; clearing is simply achieved by calling 'starttimer(TimerID)'.
                //  If DoWhat[0] is '?' (semantically best to use DoWhat = "?paused"), then returns TRUE if is paused, FALSE if not.
                //  NB - the special negative values of TimerID will ignore the second arg., if present, and behave as above.
        int n = (int) Math.Round(Args[0].X);
        int pauseCue = 0; // null value
        if (NoArgs > 1)
        { int pauseSlot = Args[1].I;   if (pauseSlot == -1) return Oops(Which, "2nd. arg. must be an array");
          double x = R.Store[pauseSlot].Data[0];
          if (x == 80 ||  x == 112) pauseCue = -1; // unicodes of 'P', 'p':  pause due
          else if (x == 82 ||  x == 114) pauseCue = 1; // unicodes of 'R', 'r':  resume due
          else if (x == 63) pauseCue = 99; // unicode of '?': do nothing but return whether or not timer is paused.
          else return Oops(Which, "2nd. arg. is invalid");
        }
        TimeSpan period, held;
        if (n >= 0 && n < NoTimers) // then this is a valid timer ID:
        { held = TimerHolds[n];
          if (pauseCue == 0) // One-arg. version was called:
          { if (held == TimeZero) period = DateTime.Now - Timers[n]; // no previous 'pause' in operation
            else period = held; // timer is in a paused state
          }
          else if (pauseCue == -1) // Pause requested:
          { if (held == TimeZero) { TimerHolds[n] = period = DateTime.Now - Timers[n]; }
            else period = held; // timer is already in a paused state, so do nothing
          }
          else if (pauseCue == 1) // Resume requested
          { if (held > TimeZero)
            { Timers[n] = DateTime.Now - held;  period = held;   TimerHolds[n] = TimeZero; }
            else period = DateTime.Now - Timers[n]; // timer was not paused, so do nothing.
          }
          else // pauseCue == 99
          { result.X  = (held == TimeZero) ? 0 : 1;  break; }
        }
        else if (n == -1) period = DateTime.Now - MainWindow.ThisWindow.StartTime; // Time since this user program started to run.
        else if (n == -2) period = DateTime.Now - MonoMathsStartTime; // Time since this instance of MonoMaths was started.
        else return Oops(Which, "the timer index must be between -2 and {0}", NoTimers-1);
        result.X = period.TotalMilliseconds;
        break;
      }
      case 135: // CHAIN(array/scalar Segment, scalar OutLength). Returns a list array repeating the segment until OutLength reached.
      // If 2nd. arg. silly, returns a scalar (0), which will probably crash the calling code.
      { int outlength = (int) Args[1].X;  if (outlength < 1) break; // returns a scalar (0), not the expected array. Tough luck.
        int outslot = V.GenerateTempStoreRoom(outlength);
        double[] outdata = R.Store[outslot].Data;
        bool ischars = false;
        double[] segment;
        if (Args[0].I == -1) { segment = new double[1];  segment[0] = Args[0].X; }
        else {  segment = R.Store[Args[0].I].Data;  ischars = R.Store[Args[0].I].IsChars; }
        int seglen = segment.Length, cntr = 0;
        while (cntr < outlength)
        { for (int i=0; i < seglen; i++)
          { outdata[cntr] = segment[i];
            cntr++; if (cntr == outlength) break;
          }
        }
        R.Store[outslot].IsChars = ischars;
        result.I = outslot;  break;
      }
      case 136: // PLACEBOARD(GraphID,  ...) -- VOID. Two versions:
      // Version 1: "..." = exactly 4 values: placeboard(GraphID, Left, Top, Width, Height). The values can be a mix of scalars / array(s).
      //   Defaults are allowed: if an arg. is negative, the existing value will not be altered.
      //   If any dimension is between 0 and 1 inclusive, it is taken as a fraction of screen width. Values > 1 are rounded
      //   and then taken as pixels. (If you want, say, an inset of 1 pixel, you therefore have to use e.g. 1.01, rather than exactly 1.) 
      // Verson 2: "..." = exactly one array, which must be one of the following (case-sensitive):
      //   "top_left", "top_middle", "top_right", "btm_left", "btm_mid", "btm_right", "mid_left", "mid_mid",  "mid_right".
      // ERRORS: Crashing is avoided because the graph might be displayed after many minutes or hours of some run, when you
      //   really don't need the program to crash because the graph can't be put exactly where you intended. Instead,
      //   improper args. of whatever type generate a message box, and leave the graph where it was.
      // Scalar errors are not handled here; GTK will do as it pleases when you put impossible values in for the four scalars. 
      { int graphID = (int) Args[0].X;
        Graph graf = null;
        Trio trill = Board.GetBoardOfGraph(graphID, out graf);
        if (graf == null) break; // No need for an error message, as there is no graph to place, so no harm done by ignoring the fn. call.
        int[] sizery = graf.SizingParameters();
        int currentBoardWidth = sizery[0],  currentBoardHeight = sizery[1];
        Gdk.Screen scream = Gdk.Screen.Default;
        double[] arguing = AccumulateValuesFromArgs(Args, 1);
        // THE FOUR-VALUES VERSION:
        if (arguing.Length == 4)
        { int currentBoardLeft = sizery[13],  currentBoardTop = sizery[14];
          var newset = new int[] { currentBoardLeft, currentBoardTop, currentBoardWidth, currentBoardHeight };
          for (int i = 0; i <= 3; i++)
          { double x = arguing[i];
            if (x < 0.0) continue; // negative value, so accept the default in 'newset'. 
            if (x > 1.0) newset[i] = Convert.ToInt32(x);
            else // must be that 0 < x < 1:
            { if (i % 2 == 0) newset[i] = Convert.ToInt32(x * scream.Width);
              else newset[i] = Convert.ToInt32(x * scream.Height);
            }
          }
          Board.TryResituate(trill.X, newset[0], newset[1], newset[2], newset[3]);
        }
        
        else // THE CHARS. ARRAY INSTRUCTION VERSION:
        { string instrucn = StoreroomToString(Args[1].I, true, true, true);
          if (NoArgs != 2 || instrucn == String.Empty) { JD.Msg("'placeboard': args. incorrectly deployed\n"); break; }
          int errno = 0;
          int vertPix=0, horizPix=0;
          if (instrucn[3] != '_') errno = 1;
          else
          { string vv = instrucn.Substring(0, 3);   string hh = instrucn._Extent(4, 3);
            if      (vv == "top") vertPix = 0;
            else if (vv == "mid") vertPix = (scream.Height - currentBoardHeight) / 2;
            else if (vv == "btm") vertPix = scream.Height - currentBoardHeight;
            else errno = 2;
            if (errno == 0)
            { if (hh == "lef") horizPix = 0;
              else if (hh == "mid") horizPix = (scream.Width - currentBoardWidth) / 2;
              else if (hh == "rig") horizPix = scream.Width - currentBoardWidth - 5;
              else errno = 3;
            }
          }
          if (errno != 0) { JD.Msg("'placeboard': unrecognized 2nd. arg. value, so graph not moved"); break; }
          Board.TryResituate(trill.X, horizPix, vertPix, currentBoardWidth, currentBoardHeight);
        }
        break;
      }
      case 137: // LASTCLOSED(ignored arg.): Graphing interrupt request. When the user closes a board, that board's ID goes
      // into int Board.LastKilledBoard (overwriting any previous values), together with the ID of its focussed graph.
      // This function returns that graph's ID (NB - not the board's ID); or 0, if either the board or the graph doesn't exist.
      // Whatever the outcome, Board.LastKilledBoard is always reset to its null value of (0,0).
      // to its startup value of 0. [*** If ever boards are allowed to have more than one graph, the operation of this function must change.]
      { if (Board.LastKilledBoard.X != 0) result.X = (double) Board.LastKilledBoard.Y;
        Board.LastKilledBoard = new Duo(0, 0);
        break;
      }

      case 138: // JAGGER(NAMED array Subject, array Operation, array/scalar NewRow, [, scalar RowNo [, scalar Length [, scalar/array Filler ]]])
      // NONVOID. Operates on Subject in situ. The RETURN is simply statistics: an array of size 3, { NoRows, NoCols, Filler }.
      // The operation is determined by Opn[0]:
      // 'I' or 'i' causes insertion; 'O' or 'o' causes overwriting; 'A' or 'a' causes appending.
      // The WHOLE matrix will be padded / amputated as indicated by scalar args. 
      // The 3-arg. version is allowed only for operation 'A(ppend)'. For 'A', if RowNo supplied, it is ignored; but Length and Filler still apply.
      // In the case of operation 'O' alone, if RowNo specifies a value beyond the end of the matrix, intermediate rows of padder will be
      //   generated in between. (A negative RowNo would raise an error.)
      // Remaining arguments:
      //  (a) NewRow, if scalar, will be interpreted as the array [NewRow]. 
      //  (b) No Length, no Filler: If the new row is longer than existing rows, they will be padded to its length with zeroes).
      //       If it is shorter, it will instead be padded to the existing row length of the matrix.
      //  (c) Length supplied and >= 1: All rows will be conformed to the prescribed length. (Neg. values ignored - the above then happens.)
      //  (d) Filler supplied: If rows are to be padded, this value is always used, replacing the system default of zero.
      //       (If is an array, its first element is taken as the padder and the rest ignored.)
      // RowNo must be valid EXCEPT for the Append operation, in which case it is simply ignored.
      // RE THE ENTRANCE STATE OF 'Subject':  This is crucial!
      //   If 'Subject' is a list array, its content and structure is lost forever, and it is recreated as a matrix with one row,
      //     and with its 'chars' rating corresponding to that of the added row. (In this case, Operation is ignored.)
      //   If 'subject' is a matrix, it is treated as a jagged matrix and is modified in accordance with the remaining arguments.
      // To use this function in a loop that generates rows, you would precede the loop with "array Foo;", and then have a single 'jagger(.)'
      //   call inside the loop which builds Foo into a full jagged array.
      { int mxslot = Args[0].I,  opslot = Args[1].I;
        if (mxslot == -1 || opslot == -1) return Oops(Which, "the first two args. must be arrays");
            // Note that the first arg. was defined as an obligatory REF argument in the header to F.SysFn(.).
        StoreItem mxItem = R.Store[mxslot];
        int[] dims = mxItem.DimSz;
        int oldnorows = dims[1], oldrowlen = dims[0];
        double[] oldData; // Filled differently a/c to whether we find later that Subject is a matrix or is a list array
        // Operation:
        double Op = Math.Round(R.Store[opslot].Data[0]);
        if (Op >= 97.0) Op -= 32.0; // converting to unicode of upper case.
        if (Op != 65.0  &&  Op != 73  &&  Op != 79) // 'A', 'I', 'O'
        { return Oops(Which, "unrecognized operation code '{0}' in the second argument", Op); }
        // NewRow:
        int newrowslot = Args[2].I;
        double[] newrowdata;
        if (newrowslot == -1) newrowdata = new double[] {Args[2].X};
        else newrowdata = R.Store[newrowslot].Data; // NB - a pointer assignment, not a copy!
        int newrowdatalen = newrowdata.Length;
        // Padder:
        double padvalue = 0.0;  
        if (NoArgs > 5)
        { if (Args[5].I == -1) padvalue = Args[5].X;  else padvalue = R.Store[Args[5].I].Data[0]; }
        // RowNo:
        int rowno = -1;
        if (NoArgs == 3) { if (Op != 65.0) return Oops(Which, "row number not supplied"); } // For op'n 'A', row no. is not consulted.
        else
        { rowno = Convert.ToInt32(Args[3].X);
          if (Op != 65.0 &&  rowno < 0) return Oops(Which, "negative row number supplied"); // For op'n 'A', row no. is not consulted.
        }
       // CASE 1 -- INPUT (1st. arg.) IS A LIST ARRAY, NOT A MATRIX: 
        if (oldnorows == 0) // then we make it look as if it were a matrix, which we are about to overwrite:
        { if (Op == 79.0 && rowno > 0) // Opn 'O' with higher row no., so we have to insert rows filled with padder:
          { oldData = new double[(1+rowno) * newrowdatalen]; // earlier rows + this new row
            if (padvalue != 0.0)
            { for (int i=0; i < rowno * newrowdatalen; i++) // set the added rows to all padder:
              { oldData[i] = padvalue; }
            }
            oldnorows = 1+rowno;
          }
          else
          { oldData = new double[newrowdatalen];
            Op = 79.0; // We reset Op to 'O', as notionally 'A' and 'I' would both reset NewRow as the first row.
            oldnorows = 1;
            rowno = 0;          
          }
          mxItem.DimCnt = 2;
          if (newrowslot == -1) mxItem.IsChars = false;
          else mxItem.IsChars = R.Store[newrowslot].IsChars;
        }
        else // CASE 2 -- INPUT IS A MATRIX:
        { if (Op == 79.0 && rowno >= oldnorows) // Opn. 'O': then we have to insert intervening rows:
          { oldData = new double[(1 + rowno) * oldrowlen ];
            int mxsz = mxItem.TotSz;
            Array.Copy(mxItem.Data, 0, oldData, 0, mxsz);
            if (padvalue != 0.0) { for (int i = mxsz; i < (oldData.Length - oldrowlen); i++) oldData[i] = padvalue; }
            oldnorows = 1 + rowno;
          }
          else
          { oldData = mxItem.Data._Copy(); // A copy, as we will alter it and then reinstall it.
            if (Op != 65.0 && rowno >= oldnorows ) return Oops(Which, "the row no. (i.e. the 4th. arg.) is out of range");
          }
        }
       // Deal with the Length argument.
        int finalrowlen = 0; if (NoArgs > 4) finalrowlen = Convert.ToInt32(Args[4].X);
            // If the user left it unset, it is < 1; in that case, leave it as is unless the new row is longer:
        if (finalrowlen < 1) { finalrowlen = oldrowlen; if (finalrowlen < newrowdatalen) finalrowlen = newrowdatalen; }
       // Set up the shell of the new structure:
        int finalnorows = oldnorows; if (Op != 79.0) finalnorows++; // i.e. if not opn 'O'
        double[] outdata = new double[finalnorows * finalrowlen];
        if (padvalue != 0.0) { for (int i=0; i < outdata.Length; i++) outdata[i] = padvalue; }
       // Copy existing data into outdata: 
        int copyrowlen = oldrowlen;  if (copyrowlen > finalrowlen) copyrowlen = finalrowlen; // length of old mx. rows to copy.
        int copyarraylen = newrowdatalen; if (copyarraylen > finalrowlen) copyarraylen = finalrowlen; // length of new row data to copy.
        if (Op == 65.0) // 'A' 
        { for (int i=0; i < oldnorows; i++) Array.Copy(oldData, i * oldrowlen, outdata, i*finalrowlen, copyrowlen);
          Array.Copy(newrowdata, 0, outdata, oldnorows * finalrowlen, copyarraylen);
        }
        else if (Op == 79.0) // 'O' 
        { for (int i=0; i < rowno; i++) Array.Copy(oldData, i * oldrowlen, outdata, i*finalrowlen, copyrowlen);
          Array.Copy(newrowdata, 0, outdata, rowno * finalrowlen, copyarraylen);
          for (int i=rowno+1; i < oldnorows; i++) Array.Copy(oldData, i*oldrowlen, outdata, i*finalrowlen, copyrowlen);
        }          
        else // opn == 'I':        
        { for (int i=0; i < rowno; i++) Array.Copy(oldData, i * oldrowlen, outdata, i*finalrowlen, copyrowlen);
          Array.Copy(newrowdata, 0, outdata, rowno * finalrowlen, copyarraylen);
          for (int i=rowno; i < oldnorows; i++) Array.Copy(oldData, i*oldrowlen, outdata, (i+1)*finalrowlen, copyrowlen);
        }          
        // Now modify the original variable with this data:
        mxItem.DimSz[0] = finalrowlen;   mxItem.DimSz[1] = finalnorows;
        mxItem.Data = outdata;
        // Return some statistics:
        result.I = V.GenerateTempStoreRoom(3);
        R.Store[result.I].Data = new double[] { (double) finalnorows, (double) finalrowlen, padvalue };
        break;
      }
      case 139: // CULL ( InArray , Unwanted : scalar or array [, Where : array] ). Removes all values of Unwanted from InArray.
      // If Where starts with 'R','r', trims from the right (back) end only; if 'L' or 'l', from the front (start) only. If 'T'(rim),
      // trims from both ends; if anything else, or absent, the whole array is subjected to the culling. 
      // RETURNS a list array, with the same .IsChars rating as the original array. If the whole input 
      //  array is made up of cullable chars., the array has length 1, with [0] = NaN, so that 'empty(.)' would return TRUE.
      { if (Args[0].I == -1) return Oops(Which, "the first arg. must be an array");
        int oldslot = Args[0].I;  double[] oldarr = R.Store[oldslot].Data;  int oldlen = oldarr.Length;
        bool waschars = R.Store[oldslot].IsChars;   double x;
        double[] badguys;
        if (Args[1].I == -1) { badguys = new double[1]; badguys[0] = Args[1].X; } // 2nd. arg. a scalar
        else badguys = R.Store[Args[1].I].Data;
        int badlen = badguys.Length;
        char whereat = 'A';//(All). Other values can be: 'L', 'R', 'T', as explained above.
        if (NoArgs == 3) 
        { if (Args[2].I == -1) return Oops(Which, "3rd. arg. must be an array");
          x = R.Store[Args[2].I].Data[0];
          if (x == 76 || x == 108) whereat = 'L';  else if (x == 82 || x == 114) whereat = 'R';  
          else if (x == 84 || x == 116) whereat = 'T';  // else leave it at 'A'.
        }
        List<double> dubs = new List<double>();  bool isbad = false;
      // SCAN FOR BAD GUYS.
        if (whereat == 'A')
        { for (int i=0; i < oldlen; i++)
          { x = oldarr[i];   isbad = false;   
            for (int j=0; j < badlen; j++){ if (x == badguys[j]){ isbad = true; break; } }
            if (!isbad) dubs.Add(x);
          } // If all elements were bad guys, dubs will have size 0. Tested for later.
        }    
        else // 'L', 'R', 'T'
        { int firstgood = -1, lastgood = -1;
          if (whereat == 'L' || whereat == 'T')
          { for (int i=0; i < oldlen; i++)
            { x = oldarr[i];   isbad = false;   
              for (int j=0; j < badlen; j++){ if (x == badguys[j]){ isbad = true; break; } }
              if (!isbad) { firstgood = i;  break; }
            } // If all elements were bad guys, firstgood remains -1. 
          }     
          if (whereat == 'R' || (whereat == 'T' && firstgood >= 0)) // find the last good guy:
          { for (int i=oldlen-1; i >= 0; i--) 
            { x = oldarr[i];   isbad = false;   
              for (int j=0; j < badlen; j++){ if (x == badguys[j]){ isbad = true; break; } }
              if (!isbad) { lastgood = i;  break; }
            } // If all elements were bad guys, lastgood remains -1.
          }     
          if (firstgood >= 0 || lastgood >= 0) // then there must be at least one good lad in the array:
          { int startptr = -1, endptr = -1;
            if      (whereat == 'L') { startptr = firstgood; endptr = oldlen-1; }
            else if (whereat == 'T') { startptr = firstgood; endptr = lastgood; }
            else                     { startptr = 0;         endptr = lastgood; } // 'R'
            for (int i=startptr; i <= endptr; i++) dubs.Add(oldarr[i]);          
          }        
        }
        if (dubs.Count == 0) result.I = EmptyArray();
        else
        { int newslot = V.GenerateTempStoreRoom(dubs.Count);
          double[] newarr = R.Store[newslot].Data;
          dubs.CopyTo(newarr);
          R.Store[newslot].IsChars = waschars;
          result.I = newslot;      
        }
        break;
      }
      case 140: // UNMERGE (Source, REF Destination1, REF Destination2 [, ...] ) -- all args. arrays. Bins the data in Source
      // into the subsequent arrays (which are redimensioned to fit). Returns the amount of data left over. E.g. for source
      // of {1,2,3,4,5} and two destinations, Destination1 will be {1,3}, Destination2 {2,4}, and return will be 1.
      // Structure of Source ignored; the REF arrays will be list arrays, of the same chars. type as Source.
      { int sourceslot = Args[0].I;
        if (sourceslot == -1) return Oops(Which, "the first arg. is not an array");
        StoreItem sitem = R.Store[sourceslot];
        double[] sourcedata = sitem.Data;
        bool ischars = sitem.IsChars;
        int sourceLength = sourcedata.Length;
        for (int arg = 1; arg < NoArgs; arg++)
        { if (Args[arg].I == -1) return Oops(Which, "the {0}th. arg. is not an array", arg+1); }
        // Reorder the data into one long array:
        int noDestinations = NoArgs-1;
        int subLength = sourceLength / noDestinations;
        if (subLength == 0) return Oops(Which, "the 1st. arg. has insufficient data");
        int reorderedLen = subLength * noDestinations;
        double[] reordered = new double[reorderedLen];
        int ndx;
        for (int i=0; i < reorderedLen; i++)
        {
          ndx = subLength * (i % noDestinations) + i / noDestinations;
          reordered[ndx] = sourcedata[i];
        }
        // Bin the data into the REF destination arrays:
        for (int i=0; i < noDestinations; i++)
        {
          int Fn = REFArgLocn[i+1].Y, At = REFArgLocn[i+1].X;
          V.NeuterTheArray(Fn, At);   V.Vars[Fn][At].Use = 11;   V.Vars[Fn][At].X = 0.0;
          int slot = V.GenerateTempStoreRoom(subLength);    V.Vars[Fn][At].Ptr = Convert.ToInt16(slot);
          StoreItem zloty = R.Store[slot];
          zloty.SetClient(Fn, At);  R.StoreUsage[slot] = 2;  zloty.IsChars = ischars;
          zloty.Data = reordered._Copy(i*subLength, subLength);
        }
        result.X = (double) sourceLength - reorderedLen;
        break;
      }
     case 141: // SPAN(array Source, array (/ scalar) FirstBkMk, array (/ scalar) SecondBkMk [, scalar FromPtr, scalar ToPtr] ) --
      // NB: CAN RETURN AN ARRAY, so needs test "(if (isarray(..) )...".
      // If both bookmarks are arrays, returns (a) whatever is between them (excluding the bookmarks themselves, IF both bookmarks
      //   found; or (b) SCALAR 1, if first bkmk not found, or if it is but the 2nd. not found, SCALAR 2.
      //   If the bookmarks are contiguous, so that there is nothing between them, the 'empty' array [NaN] is returned.
      // If FirstBkMk is scalar (whatever that scalar value), then the function returns (a) all from the start of Source / FromPtr to the
      //   second bookmark, if found; or (b) 2, if not. If SecondBkMk is scalar, then the return is all from the
      //   first bookmark (if found) to ToPtr / the end of Source, or if no FirstBkMk found, SCALAR 1.
      //   Again, in both cases if there is nothing between the bookmark and the start / end of the search zone, the 'empty' array is returned.
      // If no error, the chars. status of the returned array is the same as Source, but it is always a list array.
      // It is allowable for FirstBkMk and SecondBkMk to be the same value.
      // *** At the moment, the combination "BkMk1 .... BkMk1 ... BkMk2" would return the internal 'BkMk1'. It would require quite
      //  time-consuming code to do otherwise. My fear is that the usefulness of this fn. will be hampered by making it ever more complex.

      { int sourceSlot = Args[0].I;  if (sourceSlot == -1) return Oops(Which, "the first arg. must be an array");
        double[] inData = R.Store[sourceSlot].Data;
        bool isChars = R.Store[sourceSlot].IsChars;
        // Get the pointers:
        int fromPtr = 0, toPtr = inData.Length-1;
        if (NoArgs > 3) fromPtr = Convert.ToInt32(Args[3].X);
        if (NoArgs > 4) toPtr   = Convert.ToInt32(Args[4].X); // No checks on the pointers; extension "._Find(.)" corrects, and returns -1 if crossed.
        // Deal with the bookmarks:
        bool noBkMk1 = (Args[1].I == -1),  noBkMk2 = (Args[2].I == -1);
        if (noBkMk1 && noBkMk2) return Oops(Which, "at least one of the two book marks has to be defined (i.e. an array)");
        int startPtr = -1, endPtr = -1;
        if (noBkMk1) startPtr = fromPtr;
        else
        { double[] bkmk1data = R.Store[Args[1].I].Data;  int bkmk1Len = bkmk1data.Length;
          startPtr = inData._Find(bkmk1data, fromPtr, toPtr);
          if (startPtr == -1) { result.X = 1; break; } // scalar return
          startPtr += bkmk1Len;
        }
        if (noBkMk2) endPtr = toPtr;
        else
        { endPtr = inData._Find(R.Store[Args[2].I].Data, startPtr, toPtr);
          if (endPtr == -1) { result.X = 2; break; } // scalar return
          endPtr--;
        }
        if (startPtr > endPtr) { result.I = EmptyArray(); break; } // Nothing between the bookmarks, so startPtr is one past endPtr.
        int extent = endPtr - startPtr + 1;
        double[] outData = inData._Copy(startPtr, extent);
        result.I = V.GenerateTempStoreRoom(extent);
        R.Store[result.I].Data = outData;   R.Store[result.I].IsChars = isChars;
        break;
      }
      case 142: // READGRID(GraphID (scalar)) - returns the same array as would be used to set the grid;
      // so for 2D, returns an array of size 6: [Xmin, Xmax, Xcuts; same for Y ]; for 3D, of size 9: [same for X, then for Y, then for Z].
      // If graph cannot be found, returns the empty array (size 1, value NaN, and 'empty(.)' would return TRUE).
      {
        double[] gparams = null;
        Graph graf;
        Board.GetBoardOfGraph( (int) Args[0].X, out graf);
        if (graf == null) { result.I = EmptyArray();   break; }
        if (graf.Is3D)
        { gparams = new double[] { graf.LowX, graf.HighX, graf.SegsX, graf.LowY, graf.HighY, graf.SegsY, graf.LowZ, graf.HighZ, graf.SegsZ }; }
        else
        { gparams = new double[] { graf.LowX, graf.HighX, graf.SegsX, graf.LowY, graf.HighY, graf.SegsY }; }
        int newslot = V.GenerateTempStoreRoom(gparams.Length);
        R.Store[newslot].Data = gparams; result.I = newslot;  break;
      }
      case 143: // CAPTURED(scalar/array DoWhatWithInfo): If an array was saved to a system-named file as a binary stream, then it is
      // returned as a temporary array. DoWhatWithInfo: If an array, only the first value is examined, which may be any of the following.
      //   Note that small letters cause the abbreviated form like: "arr::/home/jon/MonoMaths/Scripts/foo.txt::33::63543025887" to be shown,
      //   whereas corresponding capitals cause a verbose message to be shown.
      // 'D', 'd' - show info in dialog box; 'R', 'r' - write into Results Window; 'M', 'm' - put into IOMessage as described below.
      //   If scalar, or if none of the above found, no information is provided in any form (though the array is still returned).
      // The format of the small-letter variant is: <var. name>::<file path and name of pgm that saved it>::<how many seconds ago>::<time imprint>.
      // If 'M' or 'm', F.IOError is also set - to false - so that fn. iok() would return true, though there is little point in consulting it.
      { int errno = 0, filesz = 0;
        byte[] bitten = null;
        try
        { FileInfo fi = new FileInfo(MainWindow.CapturedArrayPathName);
          if (!fi.Exists) errno = 10;
          else
          { filesz = Convert.ToInt32(fi.Length); // field Length is of type Int64, but we'll hopefully never go above MAXINT.
            if (filesz < 88 || filesz % 8 != 0) errno = 20;
            else
            { FileStream fs = fi.OpenRead();
              bitten = new byte[filesz];
              fs.Read(bitten, 0, filesz);
              fs.Close(); // must always close a stream (or use 'using').
            }
          }
        }
        catch { errno = 30; }
        if (errno != 0)
        { string sst = "problem with accessing '" + MainWindow.CapturedArrayPathName + "':  ";
          if      (errno == 10) sst += "cannot find this file";
          else if (errno == 20) sst += "file size is wrong (not a multiple of 8)";
          else if (errno == 30) sst += "could not read contents";
          return Oops(Which, sst);
        }
       // Develop the array of doubles from the byte array, structured as the prefix dictates:
        int nodubs = filesz / 8;
        double[] dump = new double[nodubs];
        System.Buffer.BlockCopy(bitten, 0, dump, 0, filesz); // turn the file stream's bytes into doubles, dumped into 'dump'.
        string varName = "";  int[]Dims = null;  bool IsItChars = false;  double SaveTime = 0.0;
        int outcome = ParseBinaryCodedFileDump(ref dump, ref varName, ref Dims, ref IsItChars, ref SaveTime);
        if (outcome != 1) return Oops(Which, "decoding of the prefix of file '{0}' failed", MainWindow.CapturedArrayPathName);
        result.I = V.GenerateTempStoreRoom(Dims);
        StoreItem sitem = R.Store[result.I];
        sitem.Data = dump;
        sitem.IsChars = IsItChars;
        // Publish its former identification:
        double cue = 0; 
        bool verboseForm = false;
        if (Args[0].I >= 0)
        { cue = Math.Round(R.Store[Args[0].I].Data[0]);
          if(cue >= 65.0 && cue <= 90.0)
          { verboseForm = true;
            cue += 32.0;
          }
        }
        string varPgm = "?";
        int n = varName._IndexOf(" in ");
        if (n > 0) { varPgm = varName._Extent(n+4);   varName = varName._Extent(0, n); }
        int elapsedSecs = (int) ((double) JS.Tempus('S', false) - SaveTime);
        string msg;
        if (verboseForm)
        { int elapsedMins = elapsedSecs / 60;
          int elapsedHrs = elapsedMins / 60;
          msg = "'captured()' has returned a ";
          string tt = "";
          n = sitem.DimCnt;
          if (n == 1) tt = "1-dim'l";
          else
          { tt = sitem.DimSz[n-1].ToString();
            for (int i = n-2; i >= 0; i--) tt += " x " + sitem.DimSz[i].ToString();
          }
          if (IsItChars) tt += " chars.";
          msg += tt + " array (length " + sitem.TotSz.ToString() + ')';
          msg += " previously named '" + varName + "', saved ";
          if (elapsedHrs >= 1)  { msg += elapsedHrs.ToString() + " hr. ";  elapsedMins -= elapsedHrs * 60; }
          if (elapsedMins >= 1) { msg += elapsedMins.ToString() + " min. ";  elapsedSecs -= elapsedMins * 60; }
          if (elapsedMins < 5) msg += elapsedSecs.ToString() + " sec. ";
          msg += "ago from program '" + varPgm + "'.\n";
        }
        else // concise form:
        { msg = varName + "::" + varPgm + "::" + elapsedSecs.ToString() + "::" + SaveTime.ToString(); }
        // What to do with this message?
        if      (cue == 'm') { IOMessage = msg;  IOError = false; }
        else if (cue == 'r') MainWindow.ThisWindow.AddTextToREditRes(msg, "append");
        else if (cue == 'd') JD.Msg(msg); // If none of these, no message output occurs (though the array is still returned).
        break;
      }
      case 144: // GFIXEDCLICK(GraphID): Information re clicks of any SUBMENU of a MAIN graph menu (excluding those of the 'extras' menu,
      // and excluding 'file | Exit'). A chars. array of length at least 3 is always returned, which is made up of three components: [0] = key,
      // [1] = time, [2+] = char. string:
      // (a) The graph does not exist: the array has length 3: {-2, -1, 32 } - the last being the SPACE char.'s unicode.
      // (b) No clicks have occurred since the graph was created: the array has length 3: {-1, -1, 32 } - the last being the SPACE char.'s unicode.
      // (c) A click has occurred, and has not yet been handled here: {1, msecs, ..text.. } - msecs = no. msecs since start of 1 AD;
      //      all from [2] on constitute the submenu's text, with blanks removed.
      // (d) Click(s) have occurred in the past, but have been handled previously by this function: [0] = 0, but [1] onwards are as for the last
      //      click handled.
      { double[] output = null;
        Graph graf;
        Trio trilogy = Board.GetBoardOfGraph( (int) Args[0].X, out graf);
        if (graf == null) output = new double[] { -2.0, -1.0, 32.0 };
        else
        { Quad quack = Board.Get_MainSubMenuClicked(trilogy.X);
          if (quack.X <= 0) output = new double[] { -1.0, -1.0, 32.0 }; // No click has ever occurred on this board.
          else
          { double[] diddly = StringToStoreroom(-1, quack.S);
            output = new double[diddly.Length + 2];
            output[0] = 1.0;  output[1] = quack.X;
            diddly.CopyTo(output, 2);
            // and reset the board's field:
            quack.B = false; // but leave .X and .S as is:
            Board.Set_MainSubMenuClicked(trilogy.X, quack);
          }
        }
        int slot = result.I = V.GenerateTempStoreRoom(output.Length);
        R.Store[slot].Data = output;
        R.Store[slot].IsChars = true;
        break;
      }
      case 145: // DECIDE(Heading text array, Body text array, one or more button face texts as arrays). 
      // Button texts will decide the no. of buttons (1 to 4), and will appear from left to right in the order given.
      // To adjust position and size of the box, call fn. 'setbox' immediately before this function. Otherwise box will be centred, standard size.
      // Returned: 0, for closure by corner box; 1 for first button, 2 for second, etc. (in the order of arguments).
      { string heading, bodytext;
        heading  = DubArrToChars(R.Store[Args[0].I].Data, 0, 0xffff, '?', '?');
        bodytext = DubArrToChars(R.Store[Args[1].I].Data, 0, 0xffff, '?', '?');
       // Set up button texts (from left [btn 1] to right [btn 2, 3, ...]):
        int noBtns = NoArgs - 2;
        string[] buttery = new string[noBtns];
        for (int i=0; i < noBtns; i++) buttery[i] = StoreroomToString( Args[i+2].I, true, true);
        int whichBtn = JD.DecideBox(heading, bodytext, buttery);
        if (whichBtn == 0) { MainWindow.StopNow = 'G'; R.LoopTestTime = 1; }// Icon closure. This value ensures that the very next end-of-loop
               // test will check for user's wish to end the program. But R.LoopTestTime will then be automatically reset to its orig. value of 100.
        else result.X = (double) whichBtn;
         break;
      }
      case 146: case 165: // RUN / PGM_LOAD(Array: Data;  Array: Mode) -- only Mode[0] is examined.
      // Calls on MainWindow to close down this user program and put another specified one in its place. In the case of 'run(.)',
      // causes that program to be run.
      // If Mode[0] = 'F' or 'f'(ile), Data is taken as the file name of the new program, which is then loaded into result.S.
      // If Mode[0] = 'T' or 't'(ext), Data is taken as the actual text of the program to be run.
      // If Mode[0]+[1] = 'FQ' or 'TQ' (case-insens. - Q for 'quietly'), as for 'F'/'T' EXCEPT that text is not copied into MainWindow.REditAss,
      //   and MainWindow.REditRes is not filled. ONLY APPLIES to 'run(.)'; [1] simply ignored for 'pgm_load(.)'.
      // In all of these cases, result.S is set to the actual program text to be run; though with 'F' the file name is prefixed (with delimiter),
      //   so that MainWindow.GO() can organize it to be displayed at top of window.
      // This fn. sets result.I to one of certain specific negative values, which returns as such via R.RunAssignment(.) and R.Run(.)
      //   to MainWindow.GO(.), where it is interpreted as meaning: abandon this program, implant whatever is in result.S into Assignments Window,
      //   then run it as if 'GO!' clicked.
      // VOID at present, though this may change ###. For example, the 'shell' version might return an array, filled
      // by the child program (though it could also work through existing 'save / load' functions to pass on data).
      { // function GO(.), which is where the (non-error) effects of this function are to unravel.
        int dataslot = Args[0].I, modeslot = Args[1].I;
        if (dataslot == -1 || modeslot == -1) return Oops(Which, "only array args. are allowed");
        int unic = (int) R.Store[modeslot].Data[0];  char ch = (char) unic;   ch = char.ToUpper(ch);
        // First arg is FILE NAME:
        if (ch == 'F') // then the first arg. is supposed to be a file name. The call to Store... below trims 0 and 32 from both ends.
        { string[] striddle = StoreroomToString(dataslot, true, true)._ParseFileName(MainWindow.ThisWindow.CurrentPath);
          string filename = striddle[0] + striddle[1]; // now should always be a full path and name. This is
          // important, as it will - if successful - be assigned to MainWindow.CurrentPathAndName.
          // Does the file exist, and is it loadable?
          bool wellread = false;  string ss="";
          try
          { using (StreamReader sr = new StreamReader(filename))
          { while ((ss = sr.ReadLine()) != null) { result.S += ss + '\n'; } } // implant the file program into result.S 
            wellread = true;  result.I = -10;                                                   //  (from where MainWindow.GO(.) will run it directly).
          }
          catch
          { result.S = "could not read data from '"+filename+"'"; }
          if (!wellread) break;
          if (Which == 146 && R.Store[modeslot].TotSz > 1) // Look for 'FQ', if is fn. 'run(.)' :
          { double d = R.Store[modeslot].Data[1];  if (d == 81 || d == 113)  result.I = -40; }
          result.S = filename + JS.FirstPrivateChar + result.S; // FILE NAME PREFIXED to program text, so MainWindow can display it. (No other purpose.)
        }
       // First arg is PROGRAM TEXT:
        else if (ch == 'T') // absence of JS.FirstPrivateChar (see above) tells MainWindow.GO(.) that  display of file name does not change.
        { result.S = StoreroomToString(dataslot, true, true);  result.I = -20;   
          if (Which == 146 && R.Store[modeslot].TotSz > 1) // Look for 'TQ', if is fn. 'run(.)' :
          { double d = R.Store[modeslot].Data[1];  if (d == 81 || d == 113)  result.I = -30; } 
        }
        else { result.S = "'run': the value in the 2nd. arg. was not a recognized code";  result.B = false; break; } // Don't use "Oops(.)".
        result.B = false; // .B FALSE allows 'result' to slip gracefully through R.RunAssignment(.) and then R.Run(.) as if
            // it were an error message. (The setting '-x' triggers these to leave its values completely alone in the process). Back at 
            // MainWindow.GO(.), -1x is the cue to replace the current program with the program contained in result.S and then run it.
        if (Which == 165){ result.I -= 1; } // 'pgm_load(.)' --> -11 for 'F', -21 for 'T'.
        break;
      }
      case 147: // PGM_COPY( Array/Scalar FromCue, Array/Scalar ToCue [, Scalar IncludeFromCue [, Scalar IncludeToCue]] ) -- Within program
        // text, first finds FromCue. If FromCue is scalar, it is taken as a pointer to a program character; if an array, the first occurrence
        // of the array (taken as a string of unicode chars.) is the bookmark after which text will be collected. Then, starting from the char. 
        // after it, finds the first occurrence of ToCue; then returns whatever lies between them as a list array, of 'characters' type. 
        // If ToCue is scalar and either beyond the end of the program or is negative, then it is taken to be at the end of the program.
        // If either cue is an array and is not found, crasho. 
        // The optional following arguments: 0 (FALSE) means: don't include FromCue / ToCue itself; any other value = do include it.
        // Ignored for either cue if FromCue / ToCue is scalar.
// HINTS FOR HELP TEXT: NB - this instruction should FOLLOW the two cues; otherwise the arguments themselves will be taken as the cues,
// so that you will simply be returned the comma between them!

      case 148: // PGM_GRAFT(Array ReplacemtText, <args. as for PGM_COPY>) - as above, but replaces delineated section with ReplacemtText.
      { int rep = 0;   if (Which == 148) rep = 1; // last 2 to 4 args. are the same for both fns.; these will be numbered from rep up.
        int newslot, replaceslot = -1;
        if (rep == 1){ replaceslot = Args[0].I;  if (replaceslot == -1)
        { result.S = "'pgm_graft': 1st. arg. should be an array";  result.B = false; break;} } // Don't use "Oops(.)"
        int IncludeFromCue = 0, IncludeToCue = 0;
        if (NoArgs >= rep+3) IncludeFromCue = (int) Args[rep+2].X;   if (NoArgs >= rep+4) IncludeToCue = (int) Args[rep+3].X;
        int startptr, endptr;   string fromcue = "", tocue = "";
        if (Args[rep].I == -1) startptr = (int) Args[rep].X;
        else { fromcue = StoreroomToString(Args[rep].I, true, true); startptr = IncludeFromCue; }
        if (Args[rep+1].I == -1) endptr = (int) Args[rep+1].X;
        else { tocue = StoreroomToString(Args[rep+1].I, true, true); endptr = IncludeToCue; }
        Quad outcome;
        if (Which == 147) outcome = MainWindow.ThisWindow.REditAssTextOpn('C', "",                             fromcue, startptr, tocue, endptr);
        else              outcome = MainWindow.ThisWindow.REditAssTextOpn('R', StoreroomToString(replaceslot,false, false), fromcue, startptr, tocue, endptr);
        if (!outcome.B){ result.S = '\'' + SysFn[Which].Name + "\': " + outcome.S;  result.B = false;  break; } // Don't use "Oops(.)"
        if (outcome.S == "") outcome.S = " "; // empty array not allowed. Char. 32 - SPACE - is returned in an array of 1, as minimum.
        newslot = V.GenerateTempStoreRoom(outcome.S.Length);
        StringToStoreroom(newslot, outcome.S);   R.Store[newslot].IsChars = true;
        result.I = newslot;  
        break;
      }

      case 149: case 150: // REPLACE(InArr, FromPtr, Extent, Replacement);  REPLACETO(InArr, FromPtr, ToPtr, Replacement) -- NO optional args.
      // Deletion first occurs; then, if successful, 'Replacement' replaces what was cut. Whatever the structure of InArr, a list array is returned.
      // Arg. messups: 'FromPtr' and 'Extent' work as if the array extended to elements minus and plus infinity; same with FromPtr / ToPtr.
      // E.g. FromPtr = -3 and Extent = 4 (or ToPtr = 0) will result in excision of InArr[0]. A simple copy of InArr is returned
      // (and no error is raised) if: (a) the excision range is outside of InArr; (b) Extent less than 1 / ToPtr less than FromPtr.
      // 'Replacement' - can be scalar (equiv. to array of length 1) or array. Irrespective of the length excised, this replaces the excision.
      // This function cannot be used as a surrogate 'insert' or 'append' (as nothing happens if no data is excised), or 'delete'
      // (as Replacement is an obligatory argument).
      case 151: // INSERT(InArr, AtPtr, SomethingToInsert).
      // AtPtr: must be in the range 0 to InArr.Length inclusive, or just a copy of InArr is returned (and no error raised.) (If AtPtr = 0,
      // new data is appended to the start of the array; if InArr.Length, it is appended to the end of the array.)
      // 'SomethingToInsert' - can be scalar (equiv. to array of length 1) or array.
      case 152: case 153: // DELETE(InArr, FromPtr[, Extent]), DELETETO(InArr, FromPtr, ToPtr).
      // Arg. messups: 'FromPtr' and 'Extent' work as if the array extended to elements minus and plus infinity; same with FromPtr / ToPtr.
      // E.g. FromPtr = -3 and Extent = 4 (or ToPtr = 0) will result in excision of InArr[0]. A simple copy of InArr is returned
      // (and no error is raised) if: (a) the excision range is outside of InArr; (b) Extent less than 1 / ToPtr less than FromPtr.
      { // Read in the input data array:
        int inslot = Args[0].I;   if (inslot == -1) return Oops(Which, "the first arg. must be an array");
        double[] indata = R.Store[inslot].Data;
        int n=0,  inLen = indata.Length;
        // Read in the replacement / insertion data:
        double[] newdata = null;
        if (Which <= 151) // i.e. 'replace..' or 'insert':
        { n = 3;  if (Which == 151) n = 2;
          int newdataslot = Args[n].I;
          if (newdataslot == -1) { newdata = new double[1];   newdata[0] = Args[n].X; }
          else newdata = R.Store[newdataslot].Data;
        }
        // Read in the positioning arguments:
        bool returnUnchanged = false;
        int fromPtr = (int) Args[1].X;
        int extent = 0;
        if (Which == 151) // 'insert' has in effect an 'extent'of zero, and needs separate treatment.
        { if (fromPtr < 0 || fromPtr > inLen) returnUnchanged = true; } // 'fromPtr' is allowed to equal inLen (effect: values appended.)
        else // 'replace..', 'delete..':
        { if (NoArgs == 2) extent = (int) 1e9; // only possible for 'delete(..)'. ('1e10' - make extent >> length, as negative 'fromPtr' is valid.)
          else extent = (int) Args[2].X;
          if (Which == 150 || Which == 153) extent = extent - fromPtr + 1; // "replaceTO' and ;deleteTO'
          if (fromPtr < 0) { extent += fromPtr;  fromPtr = 0; } // Remove negative fromPtr.
          extent = Math.Min(extent, inLen - fromPtr); // Deal with case where excision length would go beyond end of array.
          // Just return a copy of the original, if dumb arguments:
          if (extent < 1  || fromPtr >= inLen || fromPtr + extent <= 0) returnUnchanged = true;
        }
        if (returnUnchanged)
        { result.I = V.GenerateTempStoreRoom(inLen);
          indata.CopyTo(R.Store[result.I].Data, 0);
          R.Store[result.I].IsChars = R.Store[inslot].IsChars;
          break;
        }
       // Positioning arguments are valid, if reached this point.
        List<double> dubbles = new List<double>();
        // Add in whatever in InArr precedes the insertion point:   (applies to all functions)
        if (fromPtr > 0) dubbles.AddRange(indata._Copy(0, fromPtr) );
        // Add in the new data  (does not apply to deletion)
        if (newdata != null) dubbles.AddRange(newdata);
        // Add in whatever in InArr follows the insertion point:   (applies to all functions. Note that 'extent' is 0 for 'insert'.)
        if (fromPtr + extent < inLen) // then stick in the part of InArr after the excision area.
        { dubbles.AddRange(indata._Copy(fromPtr + extent, inLen - fromPtr - extent)); }
        // 'delete' can remove the whole bl. lot of InArr...
        if (dubbles.Count == 0) result.I = EmptyArray();
        else // there is valid 'dubbles' data:
        { result.I = V.GenerateTempStoreRoom(dubbles.Count);
          R.Store[result.I].Data = dubbles.ToArray();
          R.Store[result.I].IsChars = R.Store[inslot].IsChars;
        }
        break;
      }
 //   case 154: // LIST -- see case 176 (LIST_READ -- identical function)
 //   case 155: // LISTS -- see case 179 (LISTS_READ -- identical function) 
 //   case 156: // LISTS_TO -- see case 180 (LISTS_READ_TO -- identical function) 
      case 157: // CLEAR(any number of args.) -- zeroes all elements but otherwise leaves array / scalar structure intact. A VOID function.
      { for (int arg=0; arg<NoArgs; arg++)
        { int Fn = REFArgLocn[arg].Y,  At = REFArgLocn[arg].X;
          if (Fn < 0) return Oops(Which, "the {0}th. arg. is not an assigned variable name", arg + 1);
          int varuse = V.GetVarUse(Fn, At);
          if (varuse == 0 || varuse == 10) // unassigned scalars and arrays raise an error.
          { return Oops(Which, "the {0}th. arg. has not yet been declared", arg + 1); }
          else if (varuse <= 2) return Oops(Which, "the {0}th. arg. is a system constant or variable, or an expression", arg + 1);
          else if (varuse == 3) V.SetVarValue(Fn, At, 0.0);  // scalar:  
          else // an array.
          { int slot = Args[arg].I;  double[]zoo = new double[R.Store[slot].TotSz];  R.Store[slot].Data = zoo; } // zero the data
        }
        break;
      }
      case 158: // EXTRACT_VALUES (NAMED char. array Warnings, char. array InString, array DoWhat ) 
      // Returns the chosen operation involving identified, and thus parsed, values represented in the string. Rules of engagement:
      //  (1) values may contain nondigit characters "-.eE" used according to the normal formatting rules.
      //  (2) Spaces are significant; "12 34" will be interpreted as two separate values, 12 and 34. Also, "- 1" will be interpreted as 1, not -1.
      //  (3) Instances of "-.eE" not contiguous with digit(s) are simply ignored ("2 euros" - 'e' ignored. "2euros" - 'e' not ignored.).
      //  (4) Where an entity cannot be parsed (like this "2euros") it will be excluded from the output, but "Warnings" will record the failure.
      {
        if (Args[0].I == -1) return Oops(Which, "1st. arg. must be an array"); // But we access it only via REFArgLocn[0].
        string InStr = StoreroomToString(Args[1].I, true, true, true);
        if (InStr == "") return Oops(Which, "2nd. arg. must be an array");
        string DoWhat = StoreroomToString(Args[2].I, true, true, true);
        bool doSumming = (DoWhat == "sum");
        if (!doSumming && DoWhat != "array") return Oops(Which, "3rd. arg. is not a recognized value");
        string Digital = JS.Digits;
        char[] DigChars = JS.DigitList;
        string Numeral = Digital + "-.eE";
        string Warnings = "";        
        // Convert non-number chars. to '\e000' (reserved special character):
        char[] inchars = InStr.ToCharArray();
        char delimiter = '\ue000';
        for (int i=0; i < inchars.Length; i++)
        { if (Numeral._IndexOf(inchars[i]) == -1) inchars[i] = delimiter;
        }
        // Turn it into a string array, and parse
        string EditedInStr = inchars._ToString();
        string[] SubStrings = EditedInStr.Split(new char[]{ delimiter }, StringSplitOptions.RemoveEmptyEntries);
        double x, summation = 0.0;
        List<double> out_array = null;
        bool success;
        if (!doSumming) out_array = new List<double>();
        int array_len = SubStrings.Length;
        if (array_len == 0)
        { Warnings = "No numerical data identified\n";
          if (!doSumming) out_array.Add(double.NaN);
        }
        for (int i = 0; i < array_len; i++)
        { x = SubStrings[i]._ParseDouble(out success);
          if (success)
          { if (doSumming) summation += x;
            else out_array.Add(x);
          }
          else
          { if (SubStrings[i].IndexOfAny(DigChars) == -1) continue; // No numerals, so ignore the substring.
            Warnings += "Unable to parse '" + SubStrings[i] + "' to a number\n";
          }
        }
        // Deal with "Warnings"
        if (Warnings == "") Warnings = " ";
        int newslot = V.GenerateTempStoreRoom(Warnings.Length);
        StringToStoreroom(newslot, Warnings);
        R.Store[newslot].IsChars = true;
        int Fn = REFArgLocn[0].Y, At = REFArgLocn[0].X;
        V.TransferTempArray(newslot, Fn, At);
        // Set up the returns.
        if (doSumming)
        { result.X = summation;   break; }        
        // Left with the case where we return an array:
        result.I = V.GenerateTempStoreRoom(out_array.Count);
        R.Store[result.I].Data = out_array.ToArray();
        break;
      }
      case 159: // IOK() -- returns TRUE if the last File I/O opn. was successful (or if none yet), FALSE if it failed. Note that
      // if 'iok()' returns FALSE but 'iomessage()' returns only the empty " ", it means that the user cancelled out of a file
      // chooser dialog, and so an error display message might be inappropriate.
      // The call to 'iok()' does NOT reset the internal flag IOError. Only the next I/O function will do this.
      { if (!IOError) result.X = 1.0; // otherwise leave it at 0.
        break;
      }    
      case 160: // IOMESSAGE( [ array WhichPart ] ) -- returns content of IOMessage, set by last File I/O, and not nec. indicating error (can be information).
      // If IOMessage is empty, returns a string of one space. If WhichPart (case-sens) present and valid, it only has an effect where
      // the message contains "::" (as returned by 'load(.)' for a file with a prefix). In that case, "name" returns all before the '::',
      // "time" all after. (Wrong 'WhichPart': simply acts as if the arg. wasn't there.)
      // The call to 'iomessage()' does NOT reset the internal string IOMessage. Only the next I/O function will do this.
      { string ss = IOMessage;
        int argSlot = Args[0].I;
        if (argSlot >= 0) // then there is an array argument:
        { int n = ss._IndexOf("::", 0);
          if (n >= 0)
          { string tt = StoreroomToString(argSlot, true, true);
            if (tt == "name") ss = ss._Extent(0, n);
            else if (tt == "time") ss = ss._Extent(n+2);
          }
        }
        if (ss == "") ss = " ";
        result.I = V.GenerateTempStoreRoom(ss.Length);
        StringToStoreroom(result.I, ss); R.Store[result.I].IsChars = true;
        break;
      }    
      case 161: // IOFILE(scalar Dummy  OR  array PathOrName). If arg. scalar, path and name returned as one. If an array,
      // only [0] is examined; if 'P' or 'p', only the path is returned (with final '/'); if 'N' or 'n', only the name. (Anything
      // else --> same as if was scalar Dummy.)
      // What is returned is whatever is lurking in F.IOFilePath. This is reset at the start of every load and save operation,
      // and only set to a file path and name by that operation if the disk access was successful.
      { string fnf = IOFilePath;
        if (fnf == "") fnf = " "; // no need to access arguments in this case
        else
        { int slot = Args[0].I;
          if (slot >= 0) // then the argument is an array. If any value except P, p, N or n, the whole path + name is returned.
          { double x = R.Store[slot].Data[0];
            if      (x == 80 || x == 112) fnf = (fnf._ParseFileName(""))[0]; // 'P', 'p'
            else if (x == 78 || x == 110) fnf = (fnf._ParseFileName(""))[1]; // 'N', 'n'
          }
        }
        result.I = V.GenerateTempStoreRoom(fnf.Length);
        StringToStoreroom(result.I, fnf);
        break;
      }
      case 162: // SHUFFLEMX(Matrix, RowOrCol array [, NewOrder array] ). RowOrCol: [0] only is accessed:
      // 'R' or 'r' --> order of rows shuffled; 'C' or 'c' --> order of cols. shuffled. Any other argument crashes, 
      //  as does any 1st. arg. except a matrix. NewOrder: If absent, the new ordering of rows or cols. is random;
      //  if present, this array of indices imposes the new ordering of rows / cols. It MUST have the same length 
      //  as no. rows (if 'R') or no. cols (if 'C'), and all nos. must correspond to a row / col no. 
      //  Numbers do not have to be unique; [1,1,1,1] would be fine, for example. 
      //  .IsChars is transferred to the new array.
      { int mxslot = Args[0].I, rcslot = Args[1].I;
        if (mxslot == -1 || rcslot == -1) return Oops(Which, "scalar args. not allowed");
        if (R.Store[mxslot].DimCnt != 2) return Oops(Which, "the first arg. must be a matrix");
        double x = R.Store[rcslot].Data[0];   bool rows;
        if (x == 82.0 || x == 114.0) rows = true; else if (x == 67.0 || x == 99.0) rows = false;
        else return Oops(Which, "the second arg. must be the code 'C'(for columns) or 'R'(for rows)");
        int norows = R.Store[mxslot].DimSz[1],  nocols = R.Store[mxslot].DimSz[0];
        int sz;  int[] ndx; // will hold the new order of rows or cols.
        if (NoArgs == 3) // then the order is imposed by the user:
        { sz = R.Store[Args[2].I].TotSz;
          if ( (rows && sz != norows)||(!rows && sz != nocols)) return Oops(Which, "the third arg. is wrongly sized");
          ndx = new int[sz];   double[] doo = R.Store[Args[2].I].Data;
          for (int i=0; i<sz; i++){ ndx[i] = Convert.ToInt32(doo[i]); }
        }
        else // the order is to be random:
        { if (rows) sz = norows; else sz = nocols; // get a set of shuffled row nos.:
          ndx = JM.Shuffle(null, sz, Rando);
        }
        double[] olddata = R.Store[mxslot].Data;
        int newslot = V.GenerateTempStoreRoom(R.Store[mxslot].DimSz); 
        double[] newdata = R.Store[newslot].Data;
        if (rows) // get a set of shuffled row nos.:
        { for (int r=0; r < norows; r++)
          { Array.Copy(olddata, nocols*ndx[r], newdata, nocols*r, nocols); }
        }
        else // is columns. Sorry, this will take a little longer...
        { for (int r=0; r < norows; r++)
          { int offset = r*nocols;
            for (int c=0; c < nocols; c++)
            { newdata[offset + c] = olddata[offset + ndx[c]]; }
          }
        }
        R.Store[newslot].IsChars = R.Store[mxslot].IsChars;
        result.I = newslot; break;
      }  
      case 163: // KILL(any number of args.) -- VOID. The variable reference returns to (ptr -1, usage 0), which it had at the start of
      // the program run, before operation had come across it. The user is thereafter free to reuse it as an array or a scalar.
      // [ By the way, Use is set to 10 for new vars. in a 'dim(.)' statement, before run time; otherwise all Use set to 0.]
      // The main use (apart from deleting huge arrays to save memory) is in a loop which keeps reading a function that may return
      //  either a scalar or an array; without this, the first time it assumes its type, and the second time it raises an error if the
      //  function on that occasion returns the other type.
      // KILL used in a fn. on a REF arg. will certainly kill the fn's alias but will not affect the calling code variable's store item.
      // NB - KILL does not remove the name of the killed variable; it simply returns it to 'unassigned' state. If you try to reset
      // V.Vars[.][.].Name to something else, it will not affect subsequent code which uses the old name, as the program flow code does not
      //   use the variable's name, but only its Fn and At as they were at parse time. 
      { for (int arg = 0; arg < NoArgs; arg++)
        { int Fn = REFArgLocn[arg].Y,  At = REFArgLocn[arg].X;
          if (Fn < 0) return Oops(Which, "the {0}th. arg. is not an existing variable's name", arg + 1);
          if (Fn > 0 && At < P.UserFn[Fn].NoSysVars + P.UserFn[Fn].MaxArgs)
          { return Oops(Which, "it is not allowed to kill an argument of the current user function"); }
          int varuse = V.GetVarUse(Fn, At);
//#########          string varname = V.GetVarName(Fn, At); // used when removing this variable from VarAlias.
          if (varuse == 0) break; // it's already dead; you can't kill it any further.
          else if (varuse <= 2) return Oops(Which, "the {0}th. arg. is a system constant or variable, or an expression", arg + 1);
          else if (varuse == 3){ V.SetVarValue(Fn, At, 0.0);   V.SetVarUse(Fn, At, 0); }
          else // an array.
          { V.NeuterTheArray(Fn, At); // unassigned arrays not a problem. 'true' prevents eniolating the original storeroom,
               //  if this is a REF arg. alias inside a function, even though the aliassing array is successfully killed within the function.
            V.SetVarUse(Fn, At, 0);
          }
//##########          V.RemoveVariableAlias(Fn, varname); // no harm done, if alias dictionary not created, or if variable not recorded there.
        }
        break;
      }
      case 164: // ASC(array) -- returns the scalar numerical value of the first element in the array.
      // Intended use is e.g. for "asc('A');" to produce (in this case) 65. Otherwise bl. pointless.
      // No error messages; if you put in a scalar, you just get back its value (exceedingly pointless).
      { int slot = Args[0].I;  
        if (slot == -1) result.X = Args[0].X;  else result.X = R.Store[slot].Data[0];
        break;      
      }
  //  case 165: PGM_LOAD(..) -- see 146t  
      case 166: case 167: // LIST_NEW(), LISTS_NEW( No. new lists). Does not crash with arg. errors; 
      // instead, always adds at least one list. First form: only ever one; second form: 1 for arg <= 1, o'wise a/c to arg.
      { if (Sys == null) Sys = new List<DubList>();
        int numtoadd = (int) Args[0].X;  if (Which == 166 || numtoadd < 1) numtoadd = 1;
        for (int i=0; i < numtoadd; i++){ DubList dl = new DubList(0); Sys.Add(dl); }
        NoSysLists = Sys.Count;
        result.X = NoSysLists;   break;
      }
      case 168: // LIST_CLEAR(any no. values as arrays or scalars, being list IDs).
      // All lists must exist, or crasho.
      { double[] listIDs = AccumulateValuesFromArgs(Args);
        int[]IDs = listIDs._ToIntArray();
        for (int i = 0; i < IDs.Length; i++) { Sys[IDs[i]].LIST.Clear(); }
        break;
      }
      case 169:   case 170: // __ UNUSED
      {
        break;
      }
      case 171: // LISTS_KILL(list no.) -- kills all lists from list no. upwards. If argument out of range,
      // error is not raised; if too high, nothing happens, and if negative or zero (e.g. omitted arg.), all are killed.
      { if (Sys != null)
        { int first = (int) Args[0].X; if (first >= NoSysLists) break;
          if (first < 0) first = 0;
          for (int i = Sys.Count-1; i >= first; i--) 
          { Sys[i].LIST.Clear();  Sys.RemoveAt(i); }
          if (first == 0) Sys = null; 
          NoSysLists = first;
        }
        break;      
      }
      case 172: // LISTS_PRESERVE()
      { PreserveSysLists = true;   break;      
      }
      case 173: // LIST_ADD(list no., <any no. of arrays and scalars to append to list>)
      { int listno = (int) Args[0].X;
        if (listno < 0 || listno >= NoSysLists) return Oops(Which, "the list index is out of range");
        for (int var = 1; var < NoArgs; var++)
        { if (Args[var].I == -1) Sys[listno].LIST.Add(Args[var].X);
          else Sys[listno].LIST.AddRange(R.Store[Args[var].I].Data); // piously hopes that size of .Data = .TotSz.
        }
        result.X = Sys[listno].LIST.Count;  break;
      }        
      case 174: // LISTS_COUNT()
      { result.X = NoSysLists;  break;
      }
      case 175: // LIST_SIZE(list no.)
      { int listno = (int) Args[0].X;
        if (Sys == null || listno < 0 || listno >= NoSysLists) result.X = -1.0;
        else result.X = (double) Sys[listno].LIST.Count;
        break;
      }
      case 176: case 154:  // LIST_READ, LIST(list no. [, firstptr [, extent]]) -- identical fns.
      case 177:  case 112: // LIST_READ_TO, LIST_TO (list no., firstptr, lastptr) -- identical fns.
      // Crash if list does not exist or if any part of an indicated extent does not yet exist.
      { int listno = (int) Args[0].X;
        if (Sys == null || listno < 0 || listno >= NoSysLists) return Oops(Which, "list {0} does not exist", listno);
        int extent = 0, first = 0; if (NoArgs > 1) first = (int)Args[1].X; // no test for arrayhood.
        int listlen = Sys[listno].LIST.Count;
        if (NoArgs == 1) extent = listlen;
        else if (NoArgs == 2) extent = 1;
        else // NoArgs == 3:
        { if (Which == 176 || Which == 154) extent = (int)Args[2].X;
          else if (Which == 177 || Which == 112) extent = (int)Args[2].X - first + 1;
        }
        if (first < 0 || first >= listlen || extent < 1 || first + extent > listlen)
        { return Oops(Which, "part or all of the specified data range is unassigned"); }
        if (NoArgs == 2) result.X = Sys[listno].LIST[first];
        else
        { result.I = V.GenerateTempStoreRoom(extent);
          double[] newdata = R.Store[result.I].Data;
          Sys[listno].LIST.CopyTo(first, newdata, 0, extent);
        }
        break;
      }
      case 178: // LIST_ALTER(list no., firstptr, data) - 'data' is one scalar or one array. Overwrites existing data.
      // Crashes if list does not exist or if any part of an indicated extent does not yet exist.  
      { int listno = (int) Args[0].X;
        if (Sys == null || listno < 0 || listno >= NoSysLists) return Oops(Which, "list {0} does not exist", listno);
        int listlen = Sys[listno].LIST.Count;
        int first = (int)Args[1].X; // no test for arrayhood.
        if (first < 0 || first >= listlen) return Oops(Which, "the start pointer is outside of the extent of the list");
        int dataslot = Args[2].I;
        if (dataslot == -1)Sys[listno].LIST[first] = Args[2].X;   // a scalar value
        else // an array:
        { int extent = R.Store[dataslot].TotSz;
          if (first + extent > listlen) return Oops(Which, "part of the data range is beyond the end of the list");
          double[] doo = R.Store[dataslot].Data;
          for (int i=0; i < extent; i++) Sys[listno].LIST[first + i] = doo[i];
        }
        break;
      }
      case 179: case 155: // LISTS_READ, LISTS(pad value (scalar or array), first list, no. lists [, fixed length]) -- identical fns.
      case 180: case 156: // LISTS_READ_TO, LISTS_TO(pad value (scalar or array), first list, last list [, fixed length]) -- identical fns.
      // Return a matrix, length of rows being the length of longest list, unless the optional last arg. supplied.
      // If fixed length, then rows will be padded OR amputated to fit that length, without error message if amputated.
      // Fixed length <= 0 --> fixed length reverts to the default (longest list).
      { if (Sys == null) return Oops(Which, "no lists have been created");
        int padvalue = (int) Args[0].X;  if (Args[0].I >= 0) padvalue = (int) R.Store[Args[0].I].Data[0];
        int first = (int)Args[1].X,  extent = 0; 
        if (Which == 179 || Which == 155) extent = (int)Args[2].X; 
        else if (Which == 180 || Which == 156) extent = (int)Args[2].X - first + 1;
        if (first < 0 || first >= NoSysLists || extent < 1 || first + extent > NoSysLists)
        { return Oops(Which, "one or more of the indicated lists do not yet exist"); }
        int fixedlen = 0;
        if (NoArgs == 4){ fixedlen = (int) Args[3].X;  if (fixedlen < 1 ) fixedlen = 0; }
        if (fixedlen == 0) // then find the length of the longest list:
        { for (int i = 0; i < extent; i++) { int n = Sys[first + i].LIST.Count;  if (n > fixedlen) fixedlen = n; } }
        // Set up the matrix:
        result.I = V.GenerateTempStoreRoom(fixedlen, extent); 
        List<double> mxdata = new List<double>();
        for (int rw=0; rw<extent; rw++)  
        { double[] thisrow = new double[fixedlen];
          double[] thislist = new double[Sys[first+rw].LIST.Count];  int thislen = thislist.Length;
          Sys[first+rw].LIST.CopyTo(thislist);
          for (int cl=0; cl < fixedlen; cl++)
          { if (cl < thislen) thisrow[cl] = thislist[cl]; else thisrow[cl] = padvalue; }
          mxdata.AddRange(thisrow);      
        }
        mxdata.CopyTo(R.Store[result.I].Data);  break;
      }
      case 181: // LIST_PUSH(list no., value). Pushes value onto the end of the list (which must exist), and
                // returns the size of the list after the action. If an array, is pushed in such that [0] is
                // lower in the list.
      { int listno = (int) Args[0].X;
        if (listno < 0 || listno >= NoSysLists) return Oops(Which, "the list index is out of range");
        if (Args[1].I == -1) Sys[listno].LIST.Add(Args[1].X);
        else Sys[listno].LIST.AddRange(R.Store[Args[1].I].Data); // piously hopes that size of .Data = .TotSz.
        result.X = Sys[listno].LIST.Count;  break;
      }
      case 182: // LIST_POP(list no.,  specific var. name). If the variable size is n, this copies the last n
      // elements of the list into the variable. If an array, it is filled from the lower list index to the 
      // higher; e.g. if 'arr' has length 3, list[len-3] --> arr[0] and list[len-1] --> arr[2].
      // The list has these copied values lopped off its end. The fn. returns the size of the list after the action. 
      //If the list would not be long enough to fill the variable, the fn. does nothing at all to the variable OR the list,
      // and returns a negative no. - the shortfall (and therefore -1, if a scalar variable and the list is empty).
      //  pop returns the theoretical negative size - e.g. -3, if list was empty and popping of array of size 3 attempted,
      { int listno = (int) Args[0].X;
        if (listno < 0 || listno >= NoSysLists) return Oops(Which, "the list index is out of range");
        if (REFArgLocn[1].Y < 0) return Oops(Which, "the second arg. must be a variable name, not an expression or explicit value");
        else if (REFArgLocn[1].Y >= C.UserFnCnt) return Oops(Which, "the second arg. raised an unspecified error");
        int varslot = Args[1].I;
        int varlen = 1, listlen = Sys[listno].LIST.Count; 
        if (varslot >= 0) varlen = R.Store[varslot].TotSz;
        int newlistlen = listlen - varlen;
        result.X = (double) newlistlen; if (result.X < 0.0) break;
        // No more to do, if not enough data for popping.
        if (varslot == -1) // a scalar variable:
        { V.SetVarValue(REFArgLocn[1].Y, REFArgLocn[1].X, Sys[listno].LIST[listlen-1]); } // set the scalar variable's stored value.
        else // an array:
        { Sys[listno].LIST.CopyTo(newlistlen, R.Store[varslot].Data, 0, varlen); }
        // Truncate the list:
        Sys[listno].LIST.RemoveRange(newlistlen, varlen);
        break; // with result.X set already.
      }
      case 183: // PLACEMENT(scalar/array Value, array RefArray) -- Given a reference array RefArray which is sorted in ascending order,
      //   returns the pseudo-index(es) for Value(s) in RefArray.
      // If Value[n] exactly matches RefArray[p], the return is p. Otherwise: (a) if Value[n] - call it 'V' - lies between RefArray[p]
      //   and RefArray[p+1], the return is p + δ, where δ is a fraction determined by linear interpolation. If V < RefArray[0], the return
      //   is minus infinity; if V > last(RefArray), the return is infinity. (No allowance made for rounding errors.)
      // RefArray MUST BE IN ASCENDING SORTED ORDER, or you get back GARBAGE. (There is no test for its sorting order, so no error can be raised.)
      // If Value is an array, it is always tested to see if it is in ascending sort order. If it is found to be so sorted, then the search
      //   for a Value[n] match will begin from the RefArray index at or just below the floor of the latest pseudo-index. If not sorted,
      //   searching for a match for Value[n] will always begin from RefArray[0]. This is not significant if array RefArray is small, but is
      //   important for long RefArray: the function is MUCH FASTER if Values is sorted so that the shortened search method can be used.
      // In the case where Value is sorted, and Value[i] returns the pseudo-index ‒Infinity, then the search for placement of Values[i+1]
      //   starts at RefArray[0].
      {
        int testslot = Args[0].I, refslot = Args[1].I;
        if (refslot == -1) return Oops(Which, "2nd. arg. must be an array");
        double[] RefArray = R.Store[refslot].Data;
        double[] TestArray;
        bool isScalar = (testslot == -1);
        StoreItem testItem = null;
        if (isScalar) TestArray = new double[] { Args[0].X };  
        else { testItem = R.Store[testslot];   TestArray = testItem.Data; }
        int testLen = TestArray.Length, refLen = RefArray.Length;
        double thistestval, prevtestval = 0.0; // Initial value of prevtestval is irrelevant, as the first use of the line
        double[] output = new double[testLen];
        int startrefindex = 0, nextstartrefindex = 0;
        double firstrefval = RefArray[0], finalrefval = RefArray[refLen-1];
        for (int i = 0; i < testLen; i++)
        { thistestval = TestArray[i];
          if (thistestval < firstrefval)
          { output[i] = double.NegativeInfinity;  continue; }
          if (thistestval > finalrefval)
          { output[i] = double.PositiveInfinity;  continue; }
          // So now 'thistestval' is guaranteed to lie within the limits of the reference array.
          startrefindex = (thistestval >= prevtestval) ? nextstartrefindex : 0;
          double thisrefval, prevrefval; // Any change to coding MUST ensure that thisrefval cannot possibly be < prevrefval
              // at entry to the loop below. This involves care with assigning startrefindex within the loop.
          for (int j = startrefindex; j < refLen; j++)
          { thisrefval = RefArray[j];
            if (thistestval == thisrefval)
            { output[i] = (double) j;  break; } // and nextstartrefindex is left as is.
            else if (thistestval < thisrefval)
            { prevrefval = RefArray[j-1]; // This point can't be reached if j = 0, thanks to earlier test for thistestval < firstrefval.
              output[i] = (double) (j-1) + (thistestval - prevrefval) / (thisrefval - prevrefval);
              nextstartrefindex = j-1; // j must be > 0, thanks to earlier test for thistestval < firstrefval.
                // nextstartrefindex is set in this 'j' loop but is only used in the outer 'i' loop.
              break;
            }
            // If focus is here, thistestval > thisrefval; so do nothing except reloop.
            // Note that the loop will always  be exited at one of the two breaks above; focus cannot fall through the loop end. This is because
            //  there will always be a value of j such that thistestval <= RefArray[j], thanks to earlier test: thistestval > finalrefval.
          }
          prevtestval =thistestval;
        }
        if (isScalar) result.X = output[0];
        else
        { result.I = V.GenerateTempStoreRoom(testItem.DimSz);
          R.Store[result.I].Data = output;
        }
        break;
      }
      case 184: // ISPLOT (Scalar ID).  Returns '2' for 2D or '3' for 3D, if the plot occurs in MainWindow.Plots2D/3D; otherwise '0'.
      case 185: // ISGRAPH (Scalar ID). Returns '2' for 2D or '3' for 3D, if the object exists; otherwise '0'.
      // Note that both can be treated as pseudobooleans - "if (isplot(.))...".
      // Plots hopefully never occur within graphs that are not recorded in MainWindow.Plots2D/3D; but if they did, they would not be
      //  detected by 'isplot(.)'.
      { // Extract values from the arguments:
        int id = Convert.ToInt32(Args[0].X);   if (id <= 0) break; // with .X as 0
        if (Which == 184) // PLOT SEARCH:
        { List<Plot> glot = MainWindow.Plots2D;
          if (glot != null)
          { for (int i=0; i < glot.Count; i++) { if (glot[i].ArtID == id) { result.X = 2.0; break; } } }
          glot = MainWindow.Plots3D;
          if (glot != null)
          { for (int i=0; i < glot.Count; i++) { if (glot[i].ArtID == id) { result.X = 3.0; break; } } }
          break; // returning 0.
        }
        else // GRAPH SEARCH:
        { Graph graf;
          Board.GetBoardOfGraph(id, out graf);
          if (graf != null)
          { if (graf.Is3D) result.X = 3;  else result.X = 2.0; }
        }
        break; // with .X set as it should be.
      }
      case 186: // PUSH( NamedArray,  scalar/array NewData, scalar SizeLimit [, bool PushOntoLowEnd ] ) -- VOID. Pushes value(s) in NewData
      // onto the bottom or top of NamedArray. If no bool, or bool TRUE, NewData is pushed onto low-index end of NamedArray; if present
      // and FALSE, onto the high-index end. There are TWO VERSIONS: 
      // (a) If NamedArray is a MATRIX, then NewData must be an array, and its length must be an exact multiple of the row length of NamedArray;
      //       Otherwise the function crashes. If the length is right, NamedArray, as whole row(s), is added to NamedArray.
      // (b) If NamedArray is a LIST ARRAY, then NewData may have any length, all such data being added on the order supplied.
      // 'SizeLimit': NamedArray will grow until it reaches this size limit ( = array length, for NamedArray a list array, or No. ROWS,
      //   if NamedArray is a matrix). Thereafter data will be discarded from the opposite end to maintain exactly this size.
      //   Adding occurs before size restricting; hence, if NewData is actually larger than NamedArray, it will replace the whole array
      //   and then will itself be truncated from its high end (if PushOntoLowEnd is true or absent) or from its low end.
      //   SizeLimit must round to a positive integer; 0 or negative would crash. If you want no limit to size, set SizeLimit ridiculously large.
      //   (It is OK to use e.g. MAXREAL, as the code would correct oversized values down to MAXINT32.)
      // SPECIAL CASE: You can start off with NamedArray as an "EMPTY" ARRAY. If it is to function as a list array stack, then it must
      //   be a list array containing only NaNs (you would usually just set it to [NaN]). If it is to be a matrix stack, then NamedArray
      //   must be a 1xR matrix (R = proposed row size), all values being NaN.
      //   In either case, the chars. rating of the initially empty NamedArray will become that of NewData.
      { int stackslot = Args[0].I;  if (stackslot == -1) return Oops(Which, "the first arg. must be a named array");
        // Get stack data:
        StoreItem stackIt = R.Store[stackslot];
        double[] oldstack = stackIt.Data; 
        int oldstackLen = oldstack.Length;
        bool isEmpty = double.IsNaN(oldstack[0]); // Prima facie test; usually it won't be empty, so this test saves time.
        if (isEmpty && oldstackLen > 1)
        { for (int i=1; i < oldstackLen; i++) { if (!double.IsNaN(oldstack[i])) { isEmpty = false; break; } } }
        if (isEmpty) oldstackLen = 0;
        int[] dims = stackIt.DimSizes;
        if (dims.Length > 2) return Oops(Which, "this fn. does not handle structures of greater than 2 dimensions");
        bool isMatrix = (dims.Length == 2);
        int newdataslot = Args[1].I; 
        double[] newdata;
        if (newdataslot == -1) newdata = new double[]{Args[1].X};  else newdata = R.Store[newdataslot].Data;
        int newdataLen = newdata.Length;
        if (isMatrix  && newdataLen % dims[0] != 0)
        { return Oops(Which, "if 1st. arg. is a matrix - even if the 'empty' one - the 2nd. arg. must have length = an exact multiple of its row length"); }
        bool Is_Chars = isEmpty ? R.Store[newdataslot].IsChars : stackIt.IsChars;
        if (Args[2].X > 2147483647.0) Args[2].X = 2147483647.0; // Max value of Int32.
        int sizeLimit = Convert.ToInt32(Args[2].X);
        if (sizeLimit < 1) return Oops(Which, "3rd. arg. must be a scalar rounding to a positive integer");
        if (isMatrix) sizeLimit *= dims[0];
        bool addToLowEnd = (NoArgs <= 3  ||  Args[3].X != 0.0);
        // Accumulate potential output data:        
        List<double> newstacklist = new List<double>(oldstackLen + newdataLen);
        if (isEmpty) newstacklist.AddRange(newdata);
        else
        { if (addToLowEnd) 
          { newstacklist.AddRange(newdata);  newstacklist.AddRange(oldstack); }
          else
          { newstacklist.AddRange(oldstack);  newstacklist.AddRange(newdata); }
        }
        // Trim to size, if needed:
        int listsize = newstacklist.Count;
        if (listsize > sizeLimit)
        { if (addToLowEnd) newstacklist.RemoveRange(sizeLimit, listsize - sizeLimit);
          else newstacklist.RemoveRange(0, listsize - sizeLimit);
          listsize = sizeLimit;
        }
        // Adjust the internal settings of NamedArray:
        if (isMatrix) dims[1] = listsize / dims[0];
        else dims[0] = listsize;
        // Step 1 -- set up a new store item, not yet allotted to a variable:
        int newslot = V.GenerateTempStoreRoom(dims);
        StoreItem sitem = R.Store[newslot];
        sitem.Data = newstacklist.ToArray();
        sitem.IsChars = Is_Chars; 
        // Step 2 -- Plug this into the TVar object which represents InArray, and in the process abolish its old store item: 
        int Fn = REFArgLocn[0].Y, At = REFArgLocn[0].X;
        V.TransferTempArray(newslot, Fn, At); // This vital step takes care of everything - don't try to do its work by hand!!
       break;
      }
      case 187: // POP( NamedArray, scalar PopHowMuch [, bool PopFromLowEnd ] ) -- pops value(s) from NamedArray, returning what
      // was removed. If NamedArray is a list array, PopHowMuch refers to a number of elements; if NamedArray is a matrix, instead
      // to a number of rows. Stuff is taken from the low-index end in either case, if PopFromLowEnd is ABSENT or TRUE; otherwise
      // from the other end. PopHowMuch (rounded) must be at least 1, or crasho.
      // RETURNED: A list array in ALL CASES. Depending on the form of NamedArray -
      //   (1) NamedArray a list array: return is an array of length PopHowMuch, or - if less material available - the
      //     whole of NamedArray. If NamedArray is made empty, it is converted to the list array {NaN]. Likewise if an attempt is made
      //     to pop from this list array, [NaN] will be returned.
      //   (2) NamedArray a matrix: the list array consists of PopHowMuch rowspeeled off from NamedArray; or as many rows as are left,
      //     if less. If NamedArray is made empty, it becomes the empty MATRIX which has dimensions 1x1 and content NaN.
      //     Attempt to pop from this returns the empty list array [NaN].
      { int stackslot = Args[0].I;  if (stackslot == -1) return Oops(Which, "the first arg. must be a named array");
        // Get stack data:
        StoreItem stackIt = R.Store[stackslot];
        double[] oldstack = stackIt.Data; 
        int oldstackLen = oldstack.Length;
        bool isEmpty = (oldstackLen == 1  &&  double.IsNaN(oldstack[0]) ); // NamedArray is the empty array (whether a list array or a mx)
        int[] dims = stackIt.DimSizes;
        if (dims.Length > 2) return Oops(Which, "this fn. does not handle structures of greater than 2 dimensions");
        bool isMatrix = (dims.Length == 2);
        bool Is_Chars = stackIt.IsChars;
        // Deal with other arguments:
        bool fromLowEnd = (NoArgs < 3  ||  Args[2].X != 0.0);
        int PopHowMuch = Convert.ToInt32(Args[1].X);
        if (PopHowMuch < 1) return Oops(Which, "2nd. arg. must round to a positive integer");
        // If PopHowMuch would take more than is available, set it to all that is available.0,
        // In the process, set PopHowMuch to a number of elements, whether or not NamedArray is a matrix.
        if (isMatrix)
        { if (!isEmpty)
          { if (dims[1] < PopHowMuch) PopHowMuch = dims[1];
            PopHowMuch *= dims[0]; // now is elements, rather than rows
          }
          else PopHowMuch = 1; // elements.
        }
        else if (PopHowMuch > oldstackLen) PopHowMuch = oldstackLen;
        // Get the stuff to return:
        result.I = V.GenerateTempStoreRoom(PopHowMuch);
        if (fromLowEnd) { R.Store[result.I].Data = oldstack._Copy(0, PopHowMuch); }
        else            { R.Store[result.I].Data = oldstack._Copy(oldstackLen-PopHowMuch, PopHowMuch); }
        R.Store[result.I].IsChars = Is_Chars;
        // Reset NamedArray:
        if (!isEmpty) // Nothing to be done, if NamedArray is already empty)
        { double[] outdata;
          if (PopHowMuch == oldstackLen)
          { if (isMatrix)
            { outdata = new double[dims[0]];
              for (int i=0; i < outdata.Length; i++) outdata[i] = double.NaN;
            }
            else outdata = new double[] {double.NaN };
          }
          else 
          { if (fromLowEnd) { outdata = oldstack._Copy(PopHowMuch); }
            else            { outdata = oldstack._Copy(0, oldstackLen-PopHowMuch); }
            if (isMatrix) dims[1] = outdata.Length / dims[0];
          }
          // Implant the new version of NamedArray:
          int newslot;
          if (isMatrix) newslot = V.GenerateTempStoreRoom(dims);
          else newslot = V.GenerateTempStoreRoom(outdata.Length);
          StoreItem sitem = R.Store[newslot];
          sitem.Data = outdata;
          sitem.IsChars = Is_Chars; 
          // Step 2 -- Plug this into the TVar object which represents InArray, and in the process abolish its old store item: 
          int Fn = REFArgLocn[0].Y, At = REFArgLocn[0].X;
          V.TransferTempArray(newslot, Fn, At); // This vital step takes care of everything - don't try to do its work by hand!!
        }
        break;
      }
      case 188: // REVERSED(Any no. of variables) -- returns a list array which consists of all passed values, but in reverse order.
      // Cf. 'reverse()', which is the VOID version that takes a single argument.
      {//Set up a list of values:
        List<double> Lubble = new List<double>();  int slot;
        for (int var = 0; var < NoArgs; var++)
        { slot = Args[var].I; 
          if (slot == -1) Lubble.Add(Args[var].X);
          else Lubble.AddRange(R.Store[slot].Data);
        }
        int len = Lubble.Count;
        result.I = V.GenerateTempStoreRoom(len); 
        double[] doo = R.Store[result.I].Data;
        for (int i=0; i < len; i++)  doo[i] = Lubble[len-i-1]; // reverse the values.
        break;
      }
      case 189: // NEWCLICK(GraphID): Returns value of given graph's .ClickedGraph field (TRUE - 1.0 - or FALSE = 0.0),
      // and resets the field to false. Also returns false if graph not identified (which includes array arg. instead of scalar).
      // .ClickedGraph is set by button-down, so this returns 1.0 even before the button comes back up.
      { Graph raph;  int raphid = (int)Args[0].X;
        Board.GetBoardOfGraph(raphid, out raph);  if (raph == null) break;
        if (Board.ClickedGraph == raphid) { result.X = 1.0;  Board.ClickedGraph = 0; }
        break;
      }
      case 190: // LIST_DELETE(list no., firstptr [, extent])
      case 191: // LIST_DELETE_TO(list no., firstptr, lastptr)
      // In first form, omission of 'extent' --> all deleted to end of list. Apart from this correction, 
      // the fns. crash if list does not exist or if any part of an indicated extent does not yet exist.  
      // Returns the new length of the list.
      { int listno = (int) Args[0].X;
        if (Sys == null || listno < 0 || listno >= NoSysLists) return Oops(Which, "the list does not exist");
        int extent = 0, first = 0; if (NoArgs > 1) first = (int)Args[1].X; // no test for arrayhood.
        int listlen = Sys[listno].LIST.Count;
        if (NoArgs == 2) extent = listlen - first;
        else // NoArgs == 3:
        { if (Which == 190) extent = (int)Args[2].X;   else extent = (int)Args[2].X - first + 1; }
        if (first < 0 || first >= listlen || extent < 1 || first + extent > listlen)
        { return Oops(Which, "part or all of the specified data range is outside of the list"); }
        // Delete the given extent of the list:
        Sys[listno].LIST.RemoveRange(first, extent);
        result.X = Sys[listno].LIST.Count;   break;
      }  
      case 192: // LIST_INSERT(list no., position, one or more variables). 
        // Crashes if list does not exist or if position is not a valid position in the existing list. (Cannot be used to append.)
      { int listno = (int) Args[0].X;
        if (Sys == null || listno < 0 || listno >= NoSysLists) return Oops(Which, "the list does not exist");
        int listlen = Sys[listno].LIST.Count;
        int first = (int)Args[1].X; // no test for arrayhood.
        if (first < 0 || first >= listlen) return Oops(Which, "the second arg. is out of range");
        List<double> stuff = new List<double>(); // for accumulating the data in args. 3 onward
        for (int argie = 2; argie < NoArgs; argie++)
        { int slot = Args[argie].I;
          if (slot == -1) stuff.Add(Args[argie].X);   // a scalar value
          else stuff.AddRange(R.Store[slot].Data);  // an array
        }
        Sys[listno].LIST.InsertRange(first, stuff);
        result.X = Sys[listno].LIST.Count; break;
      }

      case 193: // DISTANCE(Variable1, Variable2 [, DontTakeSqRoot [, DontTakeMean ] ])
      // If only two args., returns the square root of the mean of the differences between corresponding Variable1 and Variable2 elements.
      // The two booleans are independent of one another. If both present and nonzero, you simply get back the sum of the squares
      //  of the differences. The geometrical distance between two points in a Cartesian system (the 'euclidean' distance) would
      //  be returned if the first boolean were 'false' and the second were 'true'.
      // 1st. and 2nd. ARG DEPLOYMENTS (the value calculated, before taking mean and squareroot):
      //  (1) Scalar p & scalar q (trivial) - (p - q)^2.  (2) Scalar p & array Q (struct ignored): (p - Q[0])^2 + (p - Q[1])^2 + ...
      //  (3) Equal-length arrays P and Q (structure ignored): (P[0] - Q[0])^2 + (P[1] - Q[1])^2 + ...
      //  (4) array P of length n and matrix Q with column size = n:  an ARRAY is returned; result[i] = (P[0] - Q[i,0])^2 + (P[1] - Q[i,1])^2 +..
      {
        int slot1 = Args[0].I,  slot2 = Args[1].I, lenForMean = 0;
        double Sum = 0;
        double[] Sums = null;
        if (slot1 + slot2 == -2) // Both scalars:
        { Sum = Args[0].X - Args[1].X;  Sum *= Sum;
          lenForMean = 1;
        }
        else if (slot1 == -1 && slot2 >= 0) // Scalar - array:
        { double[] inData = R.Store[slot2].Data;
          double x = Args[0].X, term;
          for (int i=0; i < inData.Length; i++) { term = x - inData[i];  Sum += term*term; }
          lenForMean = inData.Length;
        }
        else // both arrays:
        {
          StoreItem sitem1 = R.Store[slot1], sitem2 = R.Store[slot2];
          double[] inData1 = sitem1.Data;
          double[] inData2 = sitem2.Data;
          int len1 = inData1.Length, len2 = inData2.Length;
          if (len1 == len2) // Two equal length arrays:
          { for (int i=0; i < len1; i++) { double term = inData1[i] - inData2[i];  Sum += term*term; } }
          else // hopefully an array and a matching matrix:
          { if (len2 % len1 != 0 || sitem2.DimSizes[0] != len1) return Oops(Which, "the 1st. two args. are incompatible");
            int noRows = len2 / len1;
            Sums = new double[noRows];
            for (int rw = 0; rw < noRows; rw++)
            { double summ = 0;
              for (int cl = 0; cl < len1; cl++) { double term = inData1[cl] - inData2[rw*len1 + cl];  summ += term*term; }
              Sums[rw] = summ;
            }
          }
          lenForMean = len1;
        }
       // Apply 3rd. and 4th. args, if present:
        bool NoSqRt = (NoArgs > 2 && Args[2].X != 0.0);
        bool NoMean = (NoArgs > 3 && Args[3].X != 0.0);
        if (Sums == null) // then a scalar output is to happen:
        { if (NoMean) result.X = Sum; else result.X = Sum / (double) lenForMean;
          if (!NoSqRt) result.X = Math.Sqrt(result.X);
          break;
        }
        // If focus is here, 'Sums' must be non-null:
        int nosums = Sums.Length;
        if (!NoMean)
        { for (int i=0; i < nosums; i++) Sums[i] /= lenForMean; }
        if (!NoSqRt)
        { for (int i=0; i < nosums; i++) Sums[i] = Math.Sqrt(Sums[i]); }
        result.I = V.GenerateTempStoreRoom(nosums);
        R.Store[result.I].Data = Sums;
        break;
      }
      case 194: // PIXELS(one arg.). Two forms:
      // Form 1: SCALAR arg.: a Graph ID. Describes the blue-lined frame enclosing the graph proper. In the case of 2D, the frame
      //   is the extreme of the plottable region. For 2D, it contains the graph proper (which occupies a smaller area) along with
      //   descriptive details to its right. Returned: always an array of length 4, as follows:
      //   2D graph: [0] = X width per pixel;  [1] = Y width per pixel;  [2] = No. pixels wide;  [3] = No. pixels high.
      //   3D graph: [0] and [1] are always zero;  [2] = No. pixels wide;  [3] = No. pixels high.
      //   If the graph cannot be found, the array is { -1, -1, -1, -1 }.
      // Form 2: ARRAY arg.: Although at present any array has the same effect, that arg. should always be the lower-case word 'screen',
      //   in case of later extensions. Returns an array of size 2, [0] = screen width, [1] = screen height (pixels).
      {
        double[] output;
        int slot = Args[0].I;
        if (slot == -1) // SCALAR ARG. - a graph ID, so give the dimensions of the graph:
        { output = new double[4];
          Graph grapple;
          Board.GetBoardOfGraph( (int) Args[0].X, out grapple);
          if (grapple == null) { for (int i=0; i < 4; i++) output[i] = -1; }
          else
          { if (!grapple.Is3D) // For 3D, these two would be meaningless
            { output[0] = (grapple.HighX - grapple.LowX) / grapple.BoxWidth;
              output[1] = (grapple.HighY - grapple.LowY) / grapple.BoxHeight;
            } // But these apply for the blue-lined box of both 2D and 3D graphs:
            output[2] = grapple.BoxWidth;  output[3] = grapple.BoxHeight;
          }
        }
        else // ARRAY ARG. - so give the screen dimensions:
        { output = new double[2];
          Gdk.Screen scream = Gdk.Screen.Default;
          output[0] = (double) scream.Width;   output[1] = (double) scream.Height;
        }
        result.I = V.GenerateTempStoreRoom(output.Length);
        R.Store[result.I].Data = output;  break;
      }
      case 195: // HYPOT[enuse] (Variable1, Variable2, DontTakeSqRoot): square root of sum of squares. If two scalars, obvious.
      // If two EQUAL-sized arrays, returns an array of same length with root of sum of squares of corresponding terms.
      // If either is a scalar with the other an array, also returns an array of same length, sum of sq. of each array term
      // and sq. of scalar term. If DontTakeSqRoot is present and not exactly zero, sq. root not taken.
      { int slot0 = Args[0].I, slot1 = Args[1].I;
        bool NoSqRt = false; if (NoArgs > 2) NoSqRt = (Args[2].X != 0.0);
      // Dispose of two-scalars case:
        if (slot0 == -1 && slot1 == -1) 
        { result.X = Args[0].X*Args[0].X + Args[1].X*Args[1].X;  
          if (!NoSqRt) result.X = Math.Sqrt(result.X);
          break; 
        }
        // All other cases return an array:
        int len0 = 0, len1 = 0;   
        double x0 = Args[0].X, x1 = Args[1].X;
        double[] d0=null, d1=null;
        if (slot0 >= 0) { len0 = R.Store[slot0].TotSz;  d0 = R.Store[slot0].Data; }
        if (slot1 >= 0) { len1 = R.Store[slot1].TotSz;  d1 = R.Store[slot1].Data; }
        int maxlen = len0;  if (len1 > maxlen) maxlen = len1;
        int newslot = V.GenerateTempStoreRoom(maxlen);  double[] did = R.Store[newslot].Data;
        if (len0 > 0 && len1 > 0) // both arrays:
        { if (len0 != len1) return Oops(Which, "array lengths must be the same");
          for (int i=0; i < len0; i++) { did[i] = d0[i]*d0[i] + d1[i]*d1[i]; }
        }
        else if (len0 == 0){ x0 *= x0;  for (int i=0; i < len1; i++) { did[i] = x0 + d1[i]*d1[i]; } }
        else { x1 *= x1;  for (int i=0; i < len0; i++) { did[i] = d0[i]*d0[i] + x1; } }
        if (!NoSqRt)
        { for (int i=0; i<maxlen; i++) did[i] = Math.Sqrt(did[i]); }
        result.I = newslot; break;
      }
      case 196: // VECDIRNS( array Grid, matrix Angles, scalar OR matrix ArrowLength). Angles is (No. Y values) x (No. X values). Grid is:
        // [Xlow, Xhigh, no. X segments, Ylow, Yhigh, no. Y segments], so Angles is (Grid[5]+1) x (Grid[2]+1). ArrowLength must be either
        // a scalar (constant length vectors) or a matrix of the same dimensions as Angles. Note that Grid can be longer than 6, in which
        // case whatever the user has stuck on the end won't be accessed.
        // RETURNED: a matrix [2*size of Angles] x 2.
      { int slotgrid = Args[0].I,  slotangles = Args[1].I;
        int lenslot = Args[2].I;
        double arrowlen = -1.0;  if (lenslot == -1) arrowlen = Args[2].X;
        if (slotgrid == -1 || slotangles == -1) return Oops(Which, "the first arg. must be an array, the second a matrix");
        double[] grid = R.Store[slotgrid].Data,  angles = R.Store[slotangles].Data;
        double[] lengths = null;  if (lenslot >= 0) lengths = R.Store[lenslot].Data;
        if (R.Store[slotgrid].TotSz < 6) return Oops(Which, "the first array must have at least size 6");
        int[] dims = R.Store[slotangles].DimSz;  
        int noXs = Convert.ToInt32(grid[2]+1),  noYs = Convert.ToInt32(grid[5]+1);
        if (dims[0] != noXs || dims[1] != noYs || dims[2] != 0) return Oops(Which, "the second arg. is wrongly dimensioned");
        if (lenslot >= 0)
        { int[] dims1 = R.Store[lenslot].DimSz;
          if (dims1[0] != noXs || dims1[1] != noYs || dims1[2] != 0)
          { return Oops(Which, "the third arg. must be either a matrix (with the same dimensions as the second arg.) or a scalar"); }
        }
        int newslot = V.GenerateTempStoreRoom(2, 2 * noXs * noYs); // Two points per angle, and two values (X,Y) per point.
        double[] outpt = R.Store[newslot].Data;
        double delX = 0,   delY = 0;  bool boo = false; 
        if (grid[2]==0 || grid[5]==0) boo = true;
        else 
        { delX = (grid[1] - grid[0]) / grid[2];   delY = (grid[4] - grid[3]) / grid[5];
          if (delX <= 0.0 || delY <= 0.0) boo = true;
        }
        if (boo) return Oops(Which, "the first arg. has improper values");
        int outptcnt = 0; 
        double arlen = 0;
        for (int ycnt=0; ycnt < noYs; ycnt++)
        { double y = grid[3] + ycnt*delY;  int offset = ycnt*noXs;
          for (int xcnt=0; xcnt < noXs; xcnt++)
          { double x = grid[0] + xcnt*delX;
            double ang = angles[offset + xcnt];
            if (lenslot == -1) arlen = arrowlen; else arlen = lengths[offset + xcnt];
            double xlen = arlen*Math.Cos(ang)/2,  ylen = arlen*Math.Sin(ang)/2; // div. by 2 because half of arrow on each side of given point.
            outpt[outptcnt] = x - xlen;  outptcnt++;
            outpt[outptcnt] = y - ylen; outptcnt++;
            outpt[outptcnt] = x + xlen; outptcnt++;
            outpt[outptcnt] = y + ylen; outptcnt++;
          }
        }
        result.I = newslot;  break;
      }
      case 197: // PRIMES( scalar Lowest, scalar Highest, [scalar Extent [, anything - e.g. array 'th.' - means: use ordinal args.]])
        // Lowest adjusts up if too small, but Highest must be sensible. Anything that would return an empty array (e.g. primes
        //  from 14 to 16) returns the empty array: size 1, content NaN, and call to 'empty(.)' would return TRUE.
      {// No check on too large a number. Go ahead, help yourself - latch the computer, if you so wish.
        int fromptr = (int) Args[0].X, toptr =(int)  Args[1].X,  extent = 0;
        if (NoArgs >= 3) extent = (int) Args[2].X;
        bool argscardinal = (NoArgs < 4); // any fourth argument of any sort sets argscardinal to false.
        bool dumbo = false; // Won't lead to crashes, just to empty return array.
        if (toptr <= 0 && extent <= 0) dumbo = true;
        else if (argscardinal && toptr > 0 && (toptr < 2 || toptr < fromptr)) dumbo = true;
        if (dumbo){ result.I = V.GenerateTempStoreRoom(1); }
        else
        { List<int> primes = M2.PrimeEngine(fromptr, toptr, extent, argscardinal);
          int n = primes.Count;
          if (n == 0) result.I = EmptyArray();
          else
          { result.I = V.GenerateTempStoreRoom(n);
            if (primes.Count > 0)
            { double[] dee = R.Store[result.I].Data;
              for (int i=0; i<dee.Length; i++){ dee[i] = (double) primes[i]; }
            }
          }
        }
        break;
      }
      case 198: // UNPACK(Named Donor Array; any no. of Named Variables): Only this fn. and '__constant()' accept unregistered scalars.
      // This fn. defines them in the way that 'dim' defines arrays. So this function can take, as args: assigned scalars or arrays,
      // and previously unmentioned but valid variable names - they become scalars at the parsing stage (unit Parser).
      // Data from the donor array (which does NOT need to be a named array) will go into the variables; the 1st. variable
      // gets the donor's [0], with obvious order therafter. If not enough variable space, simply
      // not all of the donor gets unpacked. If too much, action stops after unpacking finished, so
      // variables not yet overwritten retain the un-overwritten values within them. RETURNS a scalar
      // value: 0 if donor data exactly matched space in following variables; +n if it had n items left over;
      // -n if it ran out when there was still room for n items in the variable chain.
      // **** NB: If you change the name of this function, also change the ref. to it in unit Parser, where along with 'dim' it is preprocessed.
      { int donorslot = Args[0].I;
        if (donorslot == -1) return Oops(Which, "the first arg. must be an array");
        StoreItem donoritem = R.Store[donorslot];
        double[] donor = donoritem.Data;  
        bool donorTemporary = (donoritem.ClientFn == -1);
        int cnt = 0,  donorlen = donoritem.TotSz; // cnt keeps track of how much space is available for receiving donations.
                                                           //  It also is the next spot in donor to access, while donor has data left.
        for (int arg = 1; arg < NoArgs; arg++)
        { int Fn = REFArgLocn[arg].Y,  At = REFArgLocn[arg].X;
          if (Fn < 0) return Oops(Which, "the {0}th arg. is not an existing variable name", arg + 1);
          int varuse = V.GetVarUse(Fn, At);
          if (varuse == 0 || varuse == 3) // Receiver is SCALAR. Unassigned scalars allowed here. (Unit Parser had set up unrecog'd. names as such.)
          { if (cnt < donorlen) // then there is still data to donate:
            V.SetVarValue(Fn, At, donor[cnt]); cnt++;
          }
          else if (varuse == 11) // Receiver is an ARRAY:
          { int thisslot = Args[arg].I;
            int thislen = R.Store[thisslot].TotSz;
            int n, wotsleft = donorlen - cnt;
            if (wotsleft > 0) 
            { if (wotsleft >= thislen) n = thislen; else n = wotsleft;
              Array.Copy(donor, cnt, R.Store[thisslot].Data, 0, n);
            }
            cnt += thislen;
          }
          else return Oops(Which, "args. cannot be constants or unassigned arrays");
        }
        result.X = donorlen - cnt;
        if (donorTemporary) // then eliminate it. (Where the donor is a user function returning an array, it doesn't automatically get eliminated.)
        { donoritem.Demolish();  donoritem = null;  R.StoreUsage[donorslot] = 0;
        }
        break;
      }
      case 199: case 200: // BINOM(ial coefficient) / LOGBINOM (Scalar TopNo,  Scalar LowBtmNo [, Scalar HighBtmNo]). TopNo must be >= 0 and:
      // (a) for BINOM(.), <= 170 (only limit for LOGBINOM(.) is Int32.MaxValue). LowBtmNo must be >= 0 and <= TopNo;
      // and if HighBtmNo is supplied, it must be >= LowBtmNo and  >= TopNo. All args. are rounded before testing. Silly args. crash.
      // The two-arg. form returns a scalar, the 3-arg. form an array, using successive values from LowBtmNo to HighBtmNo inclusive,
      //  so that array size = High - Low + 1.  Any TopNo - BtmNo combo produces the binomial coeff. (TopNo BtmNo), i.e.
      //  fact(TopNo) / { fact(BtmNo) * fact(TopNo - BtmNo)}.
      // No checks for e.g. providing arrays rather than scalars; but crashes with silly arguments.
        { int TopNo = (int) Math.Round(Args[0].X),  LowBtmNo = (int) Math.Round(Args[1].X),  HighBtmNo = LowBtmNo;
          if (Which == 199 && TopNo > 170)
          { return Oops(Which, "the first arg. must be <= 170. (You can use 'logbinom(.)' for larger values"); }
          if (NoArgs > 2) HighBtmNo = (int) Math.Round(Args[2].X);
          if (TopNo < 0 || LowBtmNo < 0 || LowBtmNo > TopNo || ( NoArgs > 2 && ( HighBtmNo < LowBtmNo || HighBtmNo > TopNo ) ) )
          { return Oops(Which, "improper arguments"); }
          int loggery = -1;  if (Which == 200) loggery = 0; // former: don't return a log; latter: always return a log.
          if (NoArgs == 2){ result.X = JM.Factorial(TopNo, LowBtmNo, 1, loggery).X;  return result; } // a scalar return.
          // An array of binom. coeffs. to be returned. (*** You could speed things up by avoiding duplication; E.g. for args(10, 0, 10),
          //  you could use the fact that bin. coeff. (10 r) = (10 (10-r) ) and simply copy the first for the second. Maybe some future day...)
          int novals = HighBtmNo - LowBtmNo + 1;
          double[] diddly = new double[novals];
          for (int i=0; i < novals; i++){ diddly[i] = JM.Factorial(TopNo, LowBtmNo+i, 1, loggery).X; }
          result.I = V.GenerateTempStoreRoom(novals);  R.Store[result.I].Data = diddly;
          break;
        }

    // . . . . . . . . . . . . .

      default: break;                                //default  //last
    }
  // - - - - - - - - - - - - - - - - - - - -
    return result;
  }

//=====================================================


//==============================================================================
}// END OF CLASS F

}// END OF NAMESPACE MonoMaths

/*
FORMAT FOR VARIABLES LOG: requirements for successful use of LoadVariable(..):
All lines have initial blanks removed, but not internal blanks.
Each variable's section must start with the NAME immediately followed by a
  colon. (Internal spaces are not removed: the name being sought must match with
  any spaces up to the colon.)
Each variable's section ends with a line of at least 3 dashes.
Any lines not between these two limiting lines are completely ignored (e.g. there
  might be a heading and an introductory paragraph.)
SCALARS:
A line after the first must begin with "Scalar" (case unimportant), and must end
  with the variable's value. (The first digit or dash after the word will be
  taken as the beginning of the number. Anything in between "Scalar" and that
  digit is ignored.)
ARRAYS:
A line after the first must begin with "Array" (case unimportant), and must end
  with dimensioning: "3" (list array), "4 x 3" (matrix), "2 x 3 x 4" (higher
  order structure). Spaces are allowed, and 'X' may replace 'x'. (Anything between
  "Array" and the first digit will be ignored. Zero dimensions not allowed.)
Subsequent lines will be ignored UNLESS they start with a digit, a minus sign,
  or '['. Lines that do so will be cumulated. Such lines must contain nothing
  else but numbers, passages in square brackets (which will be ignored), and
  either commas or semicolons as number separators. Internally semicolons will
  be converted to commas. (They would usually be there for visual help only, to
  show someone reading the text file where a row ends. Likewise the parts in
  square brackets would usually be to indicate to a reader what data is what.)

*/


