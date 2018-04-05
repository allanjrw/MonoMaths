using System;
using System.Collections.Generic;
using System.Text;
using JLib;

//==============================================================================

namespace MonoMaths
{
//==============================================================================
// STORAGE CLASSES AND STRUCTURES
//------------------------------------------------------------------------------
  public class TTerm
  {                        //NB: If ever adding dynamic fields (hopefully not!),
                           // will need to use a specific Copy function, instead
                           // of Array.Copy(..), in UpsizeAsstRowIfNec().
   // FIXED AT PARSE TIME:
    public short Fn;  public short At; // Where, in 2D arrays R.Scals or R.Arrs, the
    // variable is to be found. By runtime, must always have valid zero or positive
    // values assigned. Fn is not accessed for constants and literals.
    public byte VUse;//Exactly as for TVar field '.Use', except that array functions
    // will set it to 50 + no dims., in which case .At points to Store[] slot.
    // Other values: 0xFD used as temporary dummy value in flowline for negating and complementing; 
    //  and 0xFE is returned by F.GetVarUse(.) if its argument ia invalid. 
    //  Avoid 0xFF, till completely sure it is harmless (I haven't got the time to check just now).
    // A suitable test for arrayhood is: "(>= 10 && < 200)".
    public byte OpCode; // code for the operation between this and the next variable(s).
    // Codes are given in class P.
    public byte Hier; // operation hierarchy.
    public short Rating; // = (16 * nestlevel of next sign) + Hierarchy. Is neg.
                         // for LHS variable at start of expression.
    public byte ArgsDue; // the no. arguments expected by the operator of this term.
   // VARYING FIELDS -- alterable during run time.
    public double X; // Assigned for literal constants at parse time; for scalars, at
                    //  runtime, when it is assigned the value of the variable, WHICH MUST
                    //  be negated before each calculation, if Negate is TRUE.
                    //  Not used, for arrays.
    public bool IsRefArg;

    // Operational:
    public byte OpCodeRT;  // Runtime opcode; starts the same as OpCode, and changes during run time.
    public short RatingRT;  // Runtime rating; starts the same as Rating, and changes during run time.
    public byte Valid; // Used in processing a series of terms. 0 = processing
        // finished; 1 = still operative; 2 = AND or OR, so operation cannot
        // proceed beyond this term until all solved to the left.

    //---- constructor ------------
    public TTerm(int dummy)
    { VUse=0; Fn=0; At=-1; OpCode=0; Hier=0; Rating=0; ArgsDue=0; X=0.0;
      OpCodeRT=0; RatingRT=0; Valid=0;
    }
    public override string ToString()
    { return "VUse: " + VUse.ToString() + "; Fn: " + Fn.ToString() +
        "; At: " + At.ToString() +
        "; OpCode: " + OpCode.ToString() + " (" + P.OpCdTransln[OpCode] +
        ") ; Hier: " + Hier.ToString() + "; Rating: " + Rating.ToString() +
        "; ArgsDue: " + ArgsDue.ToString() +
        "; X: " + X.ToString() + "; OpCodeRT: " + OpCodeRT.ToString() +
        "; RatingRT: " + RatingRT.ToString() + "; Valid: " + Valid.ToString();
    }
    public string ToTabbedString() // 'X' goes on the end, as it is the one element which can occupy a lot of length.
    { return VUse.ToString() + '\t' + Fn.ToString() + '\t' + At.ToString() + '\t' + OpCode.ToString() + 
        " (" + P.OpCdTransln[OpCode] +")\t" + Hier.ToString() + '\t' + Rating.ToString() + '\t' + ArgsDue.ToString() + 
        '\t' + OpCodeRT.ToString() + '\t' + RatingRT.ToString() + '\t' + Valid.ToString() + '\t'  + X.ToString();
    }
  }

//------------------------------------------------------------------------------
  public class TFn
  { public string Name;
    public int NoSysVars; // in Vars[fn][], the first argument will
        // be in Vars[fn][NoSysVars]; system vars. (like '__WH') precede args.
    public int MinArgs; // No. of obligatory args.
    public int MaxArgs; // Above, + no. optional args. IF -1, (a) no limit to no.
                 // args., (b) ArgDefaults is null.
   // REF args. are only used with system fns., and involve the next two fields.
   // REF args. are those for which 'Fn no.' and 'Var. no' indices must be
   //  supplied. In many situations it doesn't matter if 'Fn' is >= 0; for
   //  example, the fn. may be happy with either a temp. or a reg'd array. (It
   //  would treat them differently, but would accept both.) Where it DOES matter,
   //  .REFsRegd is set to TRUE; in all other cases, to FALSE.
   // REF args. must be the first in the list. They may have optional values.  <==== ##### EH?? IF THIS IS TRUE, IT MUST BE MADE IMPOSSIBLE!
   //  Their no. must not exceed F.MaxREFArgs.
    public int REFArgCnt; // Different uses for the two function types. SYSTEM
      // fns.: No REF args. USER fns: recursion depth of call (nonrecursive call:
      // 1; while not running: 0.)
    public bool REFsRegd; // TRUE if REF args. are guaranteed to be registered
      // as variables. You see, what happens is this. If REFArgCnt > 0, then
      // Runner.RunAssignment(.) will stick registered variable data into
      // REFArgLocn - or something negative, if in fact the variable is not
      // registered. Some system fns. will simply test REFArgLocn, and if it is
      // negative, keep functioning (but a bit differently). Others insist that
      // the variable be registered; they do this by having REFsRegd set to TRUE.

    // the next 4 fields are used only by USER fns., and are sized by MaxArgs;
    public string[] ArgNames; // Used only with User fns.
    public short[] ArgRefs; // indexes in Vars[this fn][]. (Nothing to do with REF args; all args. of the function go here.)
    public double[] ArgDefaults; // only relevant for optional scalar args.
    public string[] ArgDefaultStrings; // Holds the default values for optional arguments, to be passed early in run time.
    public bool[] PassedByRef; // Used only with User fns. If an array arg. has been passed using the 'ref' keyword, set to TRUE.
    public Duo[] ArgSource; // Currently only used by User fns. Stores the 'fn' (as .Y) and 'at' (as .X) values for sources of ALL args. 
    // from 0 to MinArgs-1 (i.e. ignoring optional args., which cannot be 'ref' args). If the args are not 'ref' args., 
    // the info. is (currently at least) never used. At creation in method Run(.), ArgSource[fn] is sized to MaxArgs for that function.

    // - - - - - - - - -  - - - -
    public string Text; // [0]: Whole program, after removal of functions.
                        // [1]+: All between '{' and '}' of the function.
    public int LFBeforeOpener; // User fns. only.  No. line feeds in pgm. preceding 'function'.
    public int LFBeforeCloser; // User fns. only. No line fees before the line containing the closing brace of the function
    public int OpenerPtr, CloserPtr; // User fns. only. The pointer to the opening and closing braces of the fn. in the original unprocessed text.
      // Only given valid values at first use, e.g. by search for the value of some selected variable in the Results window.
    public byte Hybrid; // Only used with system functions; used to batch
      // fns. in F.RunSysFn(.), so as to cut down on code. No purpose outside
      // RunSysFn. Codes: 0 = unused (User fns.). 1 = scalars and arrays need
      // separate handling, OR one or the other are not handled at all; 2 =
      // the same handling applies for the data of both.

    // Methods:
    public TFn(int dummy)
    { Name = ""; MinArgs = 0; MaxArgs = 0; REFArgCnt = 0;  REFsRegd = false;
      ArgNames = null; ArgDefaults = null; PassedByRef = null; ArgSource = null; 
      Text = ""; LFBeforeOpener = LFBeforeCloser = 0;  OpenerPtr = CloserPtr = -1;  Hybrid = 0;  }
    // Initializer for SYSTEM functions only:
    public TFn(string name, int minargs, int maxargs, int REFargcnt, bool REFsregd, byte hybrid )
    { Name = name;  MinArgs = minargs;  MaxArgs = maxargs; REFArgCnt = REFargcnt;  REFsRegd = REFsregd;  Hybrid = hybrid;
      ArgSource = null; ArgDefaults=null; PassedByRef=null; Text=""; 
      LFBeforeOpener = LFBeforeCloser = 0;  OpenerPtr = CloserPtr = -1;  }

    public override string ToString()
    { string ss = "Name: "+ Name + "; MinArgs: "
        + MinArgs.ToString() + "; MaxArgs: " + MaxArgs.ToString();
      ss += "  \nREFArgCnt:  " + REFArgCnt.ToString() + ";  REFsRegd: "
                                                      + REFsRegd.ToString();
      ss += "  \nArgNames: ";
      if (ArgNames == null) ss+="NULL"; else ss += String.Join(", ", ArgNames);
      ss += "  \nArgRefs: ";
      if (ArgRefs == null) ss += "NULL"; else ss += ArgRefs._ToString(", ");
      ss += "  \nArgDefaults: ";
      if (ArgDefaults == null) ss += "NULL"; else ss += ArgDefaults._ToString(", ");
      ss += "  \nPassedByRef: ";
      if (PassedByRef == null) ss += "NULL"; else ss += PassedByRef._ToString(", ");
      ss += "  \nArgSource: ";
      if (ArgSource == null) ss += "NULL"; else 
      { for (int i=0; i < ArgSource.Length; i++)
        { ss += "Arg." + i + ": Fn "+ ArgSource[i].Y + ", Var " + ArgSource[i].X +";  ";
      } }  
      ss += "\nLFBeforeOpener: " + LFBeforeOpener + "\nLFBeforeCloser: " + LFBeforeCloser;
      ss += "\nOpenerPtr: " + OpenerPtr + "\nLCloserPtr: " + CloserPtr;
      ss += "\n  Text: " + C.Legible(Text, 0) + "  \nHybrid: " + Hybrid;
      return ss;
    }
  }


//==============================================================================
public class P
{
	private P(){} // Class cannot be instantiated.
//==============================================================================
// FIELDS AND PROPERTIES:
//------------------------------------------------------------------------------

  public static string Identifier1stChar = JS.Identifier1stChar; // These two are set at the start of each program parse event. They are normally
  public static string IdentifierChars = JS.IdentifierChars;   // just initialized to JS. fields of the same name; if cued by the pgm, extra chars
      //  are added. They are initialized here because they can be used before any program is parsed. In R.KillData(.), they are reset to this.
  public static TFn[] UserFn;
  public static List<string> RawAsstText; // only used if bool RecordParsedAssts is TRUE - used for troubleshooting then.
  public static List<string> CookedAsstText; // used as above.
  public static string[] PreFlow; // Final product of parsing, ready for FlowLine
    // construction. Index = function level. Created in InitializeArrays(.).
    // Holds flow control chars from CH[.], + asst. nos. coded as chars. and
    // each preceded by char. Char.MaxValue.
  private static string FnOpr, FnComma; // set at end of InitializeArrays(.)
  public static short NestCoeff = 32; // In calculating TTerm .Rating, the nest
  // level is mult. by this. It MUST exceed the highest value in Hierarchies[].
  public static List<int> GeneralDummies; // Entries will be added in series for each function, beginning with the main program;
                                          // each entry will be the 'At' in that fn. for '__D'. That is, GeneralDummies[i] will be 
                                          // the 'At' at fn no. 'Fn' for '__D' in that function. Used in the check for inappropriate "=="'s.
  public static List<string> Quotation; // Stores all bits in quotes which are identified by C.ReplaceQuotes(.). Added to by Conformer,
                                        //  but no deletions from it until the next GO.
// - - - - - - - - - - - - - - - -
// OPCODE FIELDS:
  public static int NoOperationCodes = 19; // size of next FOUR arrays.
    // These MUST keep in step with entries in the following arrays!
  public static byte snNull=0, snEq=1, snOr=2, snAnd=3, snXor=4, snLess=5, snEq_less=6,
     snEq_eq=7, snNot_eq=8, snEq_gr=9, snGreater=10, snAppend = 11, snMinus=12, snPlus=13,
     snDiv=14, snMult=15, snPower=16, snNegate=17, snNot =18, snFunc=19;
         //These become TTerm .OpCode fields. NB: Code uses the order of the above,
         // so don't alter it without scouring the pgm. for effects!
         // E.g. all cond'l assigners should lie between snLess and snGreater, as code uses these limits;
         //  and no other type should lie between these limits. And snFunc should be beyond all signs.
  public static byte snFirstArith = snAppend, snLastArith = snPower; // *** IF ADDING OPERATORS, MAKE SURE THESE LIMITS STILL APPLY.
         // ..and also, if adding operators, add same to the line "AssignmentChars = ..." in Conformer.cs.
  public static byte[] Hierarchies = new byte[] // Highest value must be less than NestCoeff above.
  //     =   || && ^^ < <= == !=  >= >     #    -  +    /  *    ^  Negate Complement Func
    {0,  1,  2, 3, 3, 4, 4, 4, 4, 4, 4,    5,   6, 6,   7, 7,   8,    9,    10,       11 };

  public static char[] OpCHcodes; // Can't be assigned here, as CH[..] don't exist
  // until runtime of user pgm; so assigned in InitializeArrays(). These codes are
  // as found in assignments at the beginning of parse time.
  public static void SetUpOpCHcodes()// Called from C.Conform() once only, at 1st. use.
  { // SET UP THE OPCODE ARRAY OPN[]:
    OpCHcodes = new char[]
 //             =                ||            &&             ^^             <
    { '\u0000', C.CH[C.CHequal], C.CH[C.CHor], C.CH[C.CHand], C.CH[C.CHxor], C.CH[C.CHless], 
 //   <=                   ==               !=                >=
      C.CH[C.CHeq_less],   C.CH[C.CHeq_eq], C.CH[C.CHnot_eq], C.CH[C.CHeq_gr],
 //   >                   #    -    +    /    *    ^     Negate  Complement  Func
      C.CH[C.CHgreater], '#', '-', '+', '/', '*', '^', '\u0000', '!', C.CH[C.CHfunction] };
// These correspond to the definitions above of snNull, ....
    FnOpr = C.CH[C.CHfunction].ToString();
    FnComma = ')' + FnOpr + '('; // will replace ',' inside fn. brackets.
  }
      // IF ADDING chars. > 255 below (like the current '\u03A6'), add them into
      //  C.Legible(..) so that they will display as required.
  public static string[] OpCdTransln = new string[]
  { " ", "=", "||", "&&", "^^", "<", "<=", "==", "!=", ">=", ">", "#", "-", "+", "/", "*",
    "^", " neg", "!", " \u03A6 "};// '\u03A6' is 'Φ'.
                           // Keeps in step with Hierarchies[] and OpCHcodes[].

// - - - - - - - - - - - - - - - -
// ASSIGNMENT CODES:
  public static short NegatorMarker = -10, ComplementMarker = -15, SysFnMarker = -20, UserFnMarker = -30;
//==============================================================================
// STATIC METHODS OF CLASS P:
//------------------------------------------------------------------------------
// MAIN METHOD CALLED FROM OUTSIDE:  //conf   //8   //parse
// Operates on whatever the Conformer has stuck into array Function[].
// Errors --> .B and .S as usual, with rich edit selection par. no. in .I.
// No errors --> only .B given data.
  public static Quad Parse(bool RecordParsedAssts)
  { Quad result = new Quad(false);  Quad quo;
    // Initialize V.Vars[,], and give it its constant and readonly members.
    // Also initialize R.Assts[].
    InitializeArrays(RecordParsedAssts);

  //######## TEMPORARY EXPERIMENTAL CODE!!! #################
  //######## If the MAIN FUNCTION has the cue "DUO" at the start (suggested line: "DUO = 1", which must NOT be remarked out),
  //########   then there will be a diversion into new experimental territory...
    int nn = P.UserFn[0].Text.IndexOf("DUO");
    if (nn >= 0  &&  nn < 10)
    { bool keepGoing = DM.Experimental(P.UserFn);
      if (!keepGoing) { result.S = "Experimental code aborted";  return result; }
    }
  //######## END OF EXPERIMENTAL CODE #######################

    // DEAL WITH STATEMENTS OF THE TYPE "A = exp1 ? exp2 : exp3"  and  "A = exp1 ? exp2 : exp3 : exp4"
    quo = ProcessQueryStatements();
    nn = P.UserFn[0].Text.IndexOf("DUO");
    if (nn >= 0  &&  nn < 10)
    { bool keepGoing = DM.Experimental(P.UserFn);
      if (!keepGoing) { result.S = "Experimental code aborted";  return result; }
    }
    if (!quo.B)
    { result.S = C.Legible(quo.S, 2); result.I = quo.I;  return result; }
    // Finish parsing assignments and build the PreFlow string array:
    quo = ProcessAssignments(RecordParsedAssts);
    if (!quo.B)
    { result.S = C.Legible(quo.S, 2); result.I = quo.I;  return result; }
    // Build FlowLine
    quo = BuildFlowLine();
    if (!quo.B) { result.S = quo.S; result.I = quo.I; return result; }
    // Breath easy - you got here without any errors.
    result.B = true;  return result;

  }
//------------------------------------------------------------------------------
//init  //ia
// Set up the two array V.Vars and R.Assts. Vars: Give it a more than adequate
//  length, and define its first couple of entries as constants and readonly
//  variable(s) (others left as null). Assts: Sets R.NoAssts, and gives each
//  row AsstRowBlock created elements.
  private static void InitializeArrays(bool RecordParsedAssts)
  {
 // INITIALIZE V.VARS[]:
    V.InitializeVarsArrays(C.UserFnCnt);

 // Insert readonly values into V.Vars[0]:
     // Insert constants:
    V.AddConstant("__L0", 0.0); // NB - DON'T CHANGE these lightly, as code elsewhere relies on '__L0', '__L1'
    //   having these values. ('__L0' is also used as a dummy 'array' tag with recursion, in R.RunUserFn(). )
    // If adding to the list, you might like to adjust "namedConstants" in the code completion routine of MainWindow.cs.
    V.AddConstant("__L1", 1.0);
    V.AddConstant("__L2", 2.0);
    V.AddConstant("__L3", 3.0);
    V.AddConstant("MAXREAL", Double.MaxValue);
    V.AddConstant("MINREAL", Double.MinValue);
    V.AddConstant("MAXINT32", (double) Int32.MaxValue);
    V.AddConstant("MININT32", (double) Int32.MinValue); // *** If changing name, realize that a line in P.CheckCharSequences(.) depends on it.
    V.AddConstant("MAXINT64", (double) Int64.MaxValue);
    V.AddConstant("MININT64", (double) Int64.MinValue);
    V.AddConstant("POSINF", Double.PositiveInfinity);
    V.AddConstant("NEGINF", Double.NegativeInfinity);
    V.AddConstant("NAN", Double.NaN);
    V.AddConstant("NaN", Double.NaN); // Duplicates the above.
    V.AddConstant("PI", System.Math.PI);
    V.AddConstant("EE", System.Math.E);
    V.AddConstant("TRUE", 1.0);
    V.AddConstant("FALSE", 0.0);
    V.AddConstant("true", 1.0);
    V.AddConstant("false", 0.0);
    UserFn[0].NoSysVars = V.Vars[0].Count; // For main pgm, used only in (1) V.ResetUserConstant(.), to save overwriting a system constant;
      // and (2) in handling menu item "Other | List Unused Variables", where it enables suppression of the above.
      // Any added code must NOT assume that all program constants will be in the above list, and so have var. index < NoSysVars.
     // NB!!!! IF EVER ADDING MORE VARIABLES TO FUNCTIONS (as opposed to the Main Program) at the start, be sure
     //  to adjust the UserFn field UserFn[fn].NoSysVars below, as assigning of values to arguments in R.RunUserFn(..) relies on it.
     //  Also, check in unit Runner for "__WH": adjustment there also.
    for (int fn = 1; fn < C.UserFnCnt; fn++) UserFn[fn].NoSysVars = 0;
    for (int fn = 0; fn < C.UserFnCnt; fn++)
    { V.AddSystemVariable(fn, "__WH");
      TFn thisFn = UserFn[fn];
      if (fn > 0) thisFn.NoSysVars++; // fn 0 excluded as this field's use for main pgm is entirely different to its use in fns.
      int dumbo = V.AddSystemVariable(fn, C.GeneralDummyVarName); 
      if (fn == 0) GeneralDummies = new List<int>();
      GeneralDummies.Add(dumbo); // used to check for standalone stmts like "a == b:" outside of if/while conditional sections.
      if (fn > 0) thisFn.NoSysVars++;
  // Add in user function argument variables
      if (thisFn.ArgNames != null)
      { for (int i = 0; i < thisFn.MaxArgs; i++)
        { thisFn.ArgRefs[i] = Convert.ToInt16(V.AddUnassignedVariable(fn, thisFn.ArgNames[i], thisFn.PassedByRef[i]));
        }
      }
    }


 // INITIALIZE R.ASSTS[]:
    R.Assts = new List<TTerm[]>();
 // INITIALIZE RAW-/COOKED ASST TEXT ARRAYS, IF REQUIRED:
    if (RecordParsedAssts)
    { RawAsstText = new List<string>();
      CookedAsstText = new List<string>();
    }
 // INITIALIZE PreFlow[]:
    PreFlow = new string[C.UserFnCnt];
                 // initialized  and filled in ProcessAssignments(.).


 // INITIALIZE ANYTHING ELSE YOU CAN THINK OF:
     for (int i = 0; i < F.MaxREFArgs; i++) F.REFArgLocn[i] = new Duo(-1,-1);

  }

  //------------------------------------------------------------------------------
//nl  //name
/// <summary>
/// IF A FIND: .Y is fn. level, .X is element of that row. (Var. is v.Vars[.Y][.X])
///  (for system fns. and user fns., only .X valid, and contains the fn. no.)
///  NamedIn is: 'V' for variable, 'F' for system function, 'U' for user function.
/// IF NOT A FIND, but NAME VALID: NamedIn = ' ', and .X is the next slot in
///   V.Vars[.Y][] where a new var. should go (.Y being the input fn. level).
/// IF NAME INVALID: NamedIn = '#'.
/// Note that for variables, fn. level FnLevel is searched first, and if no
///   find AND if ThisLevelOnly is FALSE, level 0 is searched (unless that was
///   the input FnLevel).
/// SPECIAL USE: If entered with FnLevel = -1, skips the variable search. Used
///   to check fn. names before any variables added into the system.
/// </summary>
  public static Duo NameLocn(string name, int FnLevel,  bool ThisLevelOnly,  out char NamedIn)
  { Duo pt = new Duo(-1,-1);  int n;
    // Is it a legal identifier?
    if (!C.LegalName(name)){ NamedIn='#';  return pt; }
    else NamedIn=' ';
    if (FnLevel >= 0) // then search variables:
    {// Is it a variable?
      n = V.FindVar(FnLevel, name);
      if (n > -1){ NamedIn = 'V'; return new Duo(n, FnLevel); }
      if (FnLevel > 0 && !ThisLevelOnly)
      { n = V.FindVar(0, name);
        if (n > -1){ NamedIn = 'V'; return new Duo(n, 0); }
    } }
    // Is it a system function?
    for (int i = 0; i < F.SysFnCnt; i++)
    { if (F.SysFn[i].Name == name){NamedIn = 'F'; return new Duo(i,-1); } }
    // Is it a user function?
    for (int i = 0; i < C.UserFnCnt; i++)
    { if (UserFn[i].Name == name){NamedIn = 'U'; return new Duo(i,-1); } }
    // Tough luck - no find.
    return new Duo(-1,-1); // and NamedIn remains ' '.
  }
//------------------------------------------------------------------------------
// Return no. LFs in original text before/including ptr (which is within fn. fnno):
  private static int parag (int fnno, int ptr)
  { return P.UserFn[fnno].LFBeforeOpener + C.LFsToHere(ref P.UserFn[fnno].Text, ptr);
  }
//------------------------------------------------------------------------------
//anv  //add   //new
// Add a new variable, which will be unassigned (.Use field = 0), UNLESS
//  it begins with '__', in which case (a) if with '__A', unassigned;
//  else will have .Use set to 2.
// If you create a new variable in Conformer.cs and want it to be treated by parsing like any other variable, put the '__'
//  later in the name (as is done in C.Conform_SIFT_Segments(.), using "__var" + NextSysVarNo.ToString() ).
  private static Duo AddNewVariable(string VarName, int FnLvl)
  { Duo result = new Duo();  result.Y = FnLvl;
    if (VarName._Extent(0,2) == "__" && VarName[2] != 'A') // As user's '__' can't get this far, will always be a [2].
    { result.X = V.AddSystemVariable(FnLvl, VarName); }
    else result.X = V.AddUnassignedVariable(FnLvl, VarName);
    return result;
  }
//------------------------------------------------------------------------------
//cnt
// Counts the number of occurrences of Ch in InStr between StartEnd[0] and
//  StartEnd[1] (or their defaults, if absent). Exclude those at a higher or
//  lower nesting level than the char at StartEnd, which should NOT be a bracket
//  itself. Brackets are: '[ ]' and '( )'. E.g. Ch=x in string "a(xzx(x[x]x)x)x",
//  with startptr = 2: fn. would only count the first, 2nd and 2nd last x's.
  public static int CountAtLevel(string InStr, char Ch, params int[] StartEnd)
  { int result = 0,  sqbkt = 0,  rndbkt = 0;  char c;
    char[] chrs = InStr.ToCharArray();
    for (int i = 0; i < chrs.Length; i++)
    { c = chrs[i];
      if (c == '[') sqbkt++;  else if (c == ']') sqbkt--;
      else if (c == '(') rndbkt++;  else if (c == ')') rndbkt--;
      else if (c == Ch && sqbkt == 0 && rndbkt == 0) result++;
    }
    return result;
  }
//------------------------------------------------------------------------------
// At the END of the present string, look for either [][]....[] or [,,,...];
//  integers may or may not be fitted in between; those missing are replaced by 0.
// If you want 'out Dimensions' to have a specific size, set NoDims to that size;
//  in that case unused dims. will be low, and be set to zero. (If size of
//  Dimensions would exceed NoDims, an error is raised.)
// If NoDims is 0 or negative, the size of Dimensions will be set by contents of [].
// RETURNED: .B TRUE if properly formatted section found (or none, for scalar).
//  .S is all of InStr up to (not including) the first '[' (error or not).
//  If TRUE, .I is no. of stated dimensions (0 for scalar), the out array giving
//  the expressions in[...]. If NoDims supplied, .I still reflects no. components
//  to [...], not NoDims itself.
// NOT EVERY ERROR is checked for. Other parsing will have to do this.
//  It is NOT an error if there is nothing before the square-bracketted bit.
//  Also it is NOT an error to have empty '[]'. (Filter out beforehand, if a problem.)
  public static Quad GetDimensions(string InStr, int NoDims, out string[] Dimensions)
  { Dimensions = null;   Quad result = new Quad(false);
    if (InStr == "") {result.S = "empty string"; return result;}
    int pp = InStr.IndexOf('[');
    if (pp==-1)
    { result.S = InStr;  result.I = 0; result.B = true;  return result;}//Scalar.
    result.S = InStr._Extent(0, pp);
    int qq = InStr.LastIndexOf(']');
    if (qq != InStr.Length-1){result.S = "']' not at end"; return result;}
    InStr = InStr._FromTo(pp+1, qq-1); // all between initial and final sq. bkts.
    if (InStr == "") // then we had InStr of "...[]"
    { result.B = true; result.I = 1;
      Dimensions = new string[1]; Dimensions[0] = "";  return result; }
    // could be e.g. ("1,x+1,3" or "1,,,"); or ("1][x+1][3" or "1][][")
    // Replace all commas with "][", but only if they are not inside any brackets,
    // square or round, within. (E.g."a,fn(b,c[1]),d" --> "a][fn(b,c[1])][d".
    char[] instr = InStr.ToCharArray();  StringBuilder sb = new StringBuilder();
    int rndlvl=0, sqrlvl=0;  char ch;
    for (int i = 0; i < instr.Length; i++)
    { ch = instr[i];
      if (ch=='[')sqrlvl++; else if (ch==']')sqrlvl--;
      if (ch=='(')rndlvl++; else if (ch==')')rndlvl--;
      if (ch==',' && rndlvl==0 && sqrlvl==1)sb.Append("][");
      else sb.Append(ch);
    }
    InStr = (sb.ToString()).Replace("][", C.CH[C.CHbkmark].ToString());
                               // as next fn. requires a char. delimiter.
    Dimensions = InStr.Split(C.CH[C.CHbkmark]);
    result.B = true; result.I = Dimensions.Length;  return result;
  }
//------------------------------------------------------------------------------
//check
// Check InStr for character sequences and positions not legal for an assignment.
// In the process, remove redundancies, and replace unary negative signs with the
// code Negator. LFs are finally removed at this stage of processing. But in order
// to preserve paragraph-selection, .I returns no. of LFs removed.
// Errors reflected in .B and .S.
  public static Quad CheckCharSequences(string InStr)
  { Quad result = new Quad(false);
    if (InStr == ""){result.S = "empty assignment";  return result;}
 // FIRST FILTER: Remove LF; replace '[]' with '[-1]' (meaningful with arrays,
 //   but will cause shock waves elsewhere); replace '()' with '(0)'.
    result.I = InStr._CountChar('\n'); // needed for para. referencing.
    char[] instr = InStr.ToCharArray();  int ptr, final = instr.Length-1;
    char ch, lastch, thisch, nextch;
    StringBuilder sb = new StringBuilder();
    ptr = 0;
    while (ptr <= final)
    { thisch = instr[ptr];
      if (ptr > 0)lastch = instr[ptr-1]; else lastch = ' ';
      if (ptr < final) nextch = instr[ptr+1]; else nextch = ' ';
  // Filter the character:
      if (thisch == '[' && nextch == ']')
      { sb.Append("[MINREAL]"); ptr += 2; } // "[]"--> "[MINREAL]", i.e. to [double.MinValue]. I use a value that can't be
            // converted to an integer, so that if the user ever accidentally inserts this value, an error message will be raised
            // in "__assign(.)" and "__segmt(.)" functions because it cannot be translated to an integer.
      // Convert "()" to (in effect) "(0)":
      else if (thisch=='(' && nextch==')')
      { sb.Append("(__L0)"); ptr += 2; } // jump over next char.
      else if (thisch=='\n')
      { if (P.IdentifierChars.IndexOf(lastch)>-1 &&
                        P.IdentifierChars.IndexOf(nextch)>-1)
        { result.S = "name or no. interrupted by a linefeed. ";
          result.S += "(Did you forget a ';' ?)";  return result; }
        ptr++; }// ignore the LF.
      else // any other char.:
      { sb.Append(thisch); ptr++;}
    }
    if (sb.Length==0){result.S = "empty assignment";  return result;}
  // NEXT FILTER: replace 'x:y', (..):y', '(..):(..)' all by 'round(..,..)':
    if (InStr.IndexOf(':') != -1)
    { string sbstr = sb.ToString(),  fore = "", aft = "";
      int colon = -1, err = 0,   forestart = -1, aftend = -1;
      ptr = 0;
      while (true) // handle all instances of ':':
      { colon = sbstr.IndexOf(':', ptr);  if (colon == -1) break;
        if (colon==0 || colon==sbstr.Length-1){ err = 1; break; }
        // define aft and aftend:
        ch = sbstr[colon+1];
        if (ch == '(')
        { aftend = JS.CloserAt(sbstr, '(', ')', colon+1);
          if (aftend == -1){ err = 2; break; }
          aft = sbstr._FromTo(colon+1, aftend);
        }
        else
      { Octet oxy = JS.FindWord(sbstr, colon+1, Int32.MaxValue,
               P.IdentifierChars, "", true); // extract no/var name to right.
        if (!oxy.BX){err = 3; break; } // no find.
        if (oxy.IX != colon+1){err = 4; break; }// e.g. a '-' sign followed ':'.
          aft = oxy.SX;  aftend = oxy.IY;
        }
        // define fore and forestart:
        if (sbstr[colon-1] == ')') // we have "(yak):..."
        { forestart = JS.OpenerAt(sbstr, '(', ')', 0, colon-1);
          if (forestart == -1){ err = 5; break; }
          fore = sbstr._FromTo(forestart, colon-1); // = "(yak)"
        }
        else // we have "yak:..."
        { int qtr = sbstr._FirstFromEndNotIn(P.IdentifierChars+"[]", 0, colon-1);
          if (qtr == -1 || qtr == colon-1){err = 5; break; } // I think -1 is impossible...
          forestart = qtr+1;  fore = sbstr._FromTo(forestart, colon-1);
        }
        sbstr = sbstr._ScoopTo(forestart, aftend, "round("+fore+','+aft+")");
        ptr = colon+1; if (ptr >= sbstr.Length) break;
      }
      if (err > 0)
      { result.S = "illegal use of ':' somewhere nearby"; return result;}
      sb = new StringBuilder(sbstr);
    }
  // SIFT CHAR-BY-CHAR:
    thisch = '('; // this default, because actions for the 1st.
        // char. in the string are always the same as if it followed a '('.
    char lasttype, thistype='{'; // Allowed: '{' (for '[' or '('), '}' (for ']' or ')'),
            // 'A' (for all identifier chars.), 'O' (any operator),  ',' (comma).
    int contig = 0; // keeps tally of the no. of contiguous opcodes.
    StringBuilder sbout = new StringBuilder();
    char endbkmk = C.CH[C.CHbkmark];
    for (ptr = 0; ptr <= sb.Length; ptr++)
    { lastch = thisch;  lasttype = thistype;
      if (ptr == sb.Length)thisch = endbkmk; else thisch = sb[ptr];
      // CLASSIFY thisch. NB - every char. except endbkmk MUST occur in a
      // 'thistype' category, or else the fn. will not work properly.
      if (P.IdentifierChars.IndexOf(thisch) != -1) thistype = 'A';
      else if (thisch=='(' || thisch=='[') thistype = '{';
      else if (thisch==')' || thisch==']') thistype = '}';
      else if (Array.IndexOf(OpCHcodes, thisch) != -1) thistype = 'O';
      else if (thisch==',') thistype = ',';
      else if (thisch!=endbkmk)
      { result.S = "illegal char.: '" + thisch + "'";   return result;}
      if (thistype == 'O') contig++; else contig = 0;
      if (contig > 2)
      { result.S = "illegal combination of operators"; return result;}
      // handle end of string:
      if (thisch == endbkmk)
      { if (lasttype!='}' && lasttype!='A') // (If is these, do nothing.)
        { result.S = "incomplete at end";  return result; }
      } // to get here, ptr is not the final character:
      else if (thistype == 'A')
      { if (lasttype!='}') sbout.Append(thisch);
        else { result.S = "sign missing after " + thisch; return result; } }
      else if (thisch == ',')
      { if (lasttype!='{' && lasttype!='O') sbout.Append(thisch);
        else { result.S = "comma out of place"; return result; } }
      else if (thisch == '(')
      { if (lasttype != '}')sbout.Append(thisch);
        else { result.S = "misuse of brackets"; return result; } }
      else if (thisch == '[')
      { if (lastch==']' || lasttype=='A' || lastch==')') sbout.Append(thisch); // "Arr[][" or "Arr[" or "(<arr. exp'n.>)["
        else { result.S = "misplaced '['"; return result; } }
      else if (thistype == '}')
      { if (lasttype=='}'||lasttype=='A') sbout.Append(thisch);
        else { result.S = "misplaced '" + thisch + "'"; return result; } }
      else if (thisch == '+' || thisch == '#')
      { if (lasttype=='}' || lasttype=='A') sbout.Append(thisch);
        else if (lastch == '!') { result.S = "'!" + thisch +"' not allowed"; return result; } }  //otherwise ignore.
      else if (thisch == '-')
      { if(lasttype=='}'||lasttype=='A') sbout.Append(thisch);
        else if (lastch == '!') { result.S = "'!-' not allowed"; return result; }
        else sbout.Append(C.Negator); }
      else if (thisch == '!')
      { if (lasttype=='}'||lasttype=='A'||lastch=='!') { result.S = "misplaced '!'"; return result; }
        else sbout.Append(C.Complementer); }
      else if (thistype=='O')// except '+', '-' and '!' (filtered out just above):
      { if (lasttype=='}' || lasttype=='A') sbout.Append(thisch);
        else { result.S = "misplaced '" + thisch + "'"; return result; } }
      else // should not be possible to reach this 'else'! But leave it here, in case of future fiddling of the above.
      { result.S = "Programming error in handling '"+thisch+"'?"; return result;}
    }
    result.B = true;  result.S = sbout.ToString();  return result;
  }
//------------------------------------------------------------------------------
// Convert references to array segments like "Arr[..][..]" either to "__D = __assign(Arr, ...) (for LHS reference)
// or to "__segment(Arr, ...) (for RHS reference). Resultant form is returned in .S.
// Errors: .B FALSE, and .I stores error code. (.S not used for errors.)
  public static Quad ConvertArrayRefs(string InStr)
  { Quad que, result = new Quad(false);  int opbkt, clbkt;  string[] InBkts;
    string ss, tt;
    // KEEP: char[] choo = InStr.ToCharArray(); // here for testing only.
    while (true)
    { opbkt = InStr.IndexOf('[');  if (opbkt == -1)break; // no more array refs.
      int n = opbkt;
      do // cross higher nestings, AND intermediate '][' (as e.g. in "Arr[1][2]"):
      { clbkt = JS.CloserAt(InStr, '[', ']', n); //matching closer
        if (clbkt < n) { result.I = 1;  return result;}//mismatched '[',']'
        n = clbkt+1; 
      } while (n < InStr.Length && InStr[n] == '[');
      // backtrack to beginning of name:
      string Nm = ""; int NmPtr = opbkt;
      if (InStr[opbkt-1] == ')')// then we have "(<array expression>)[m][n]..", rather than "ArrName[m][n]..":
      { NmPtr = JS.OpenerAt(InStr, '(', ')', 0, opbkt-1); // I think -1 is impossible here.
        Nm = InStr._FromTo(NmPtr, opbkt-1); // Nm = brackets '(',')' + all between them.
      }         // If is LHS, then "(Arr)[n]= " works (but is silly), but "(<array expression>)[n]=" crashes later on. So no test in this fn.
      else 
      { for (int i = opbkt-1; i >= 0; i--)
        { if (P.IdentifierChars.IndexOf(InStr[i]) == -1) break;  else  {Nm = InStr[i] + Nm;  NmPtr--; } }
        if (Nm == ""){ result.I = 3; return result;} // '[...]' without a prior name.
      }
      ss = InStr._FromTo(opbkt, clbkt);
      que = GetDimensions(ss, -1, out InBkts);
      if (!que.B){ result.I = 5; return result;}// error in sq. bkt. parser.
      tt = String.Join(",", InBkts);//e.g.by now, orig.[1][fn(x)+2] --> 1,fn(x)+2
      // Allow for the word "last" in sq bkts; it must be the first content of the string of InBkts,
      //  and followed by nil or by '-'.
      if (tt.IndexOf("last") == 0 && tt.IndexOf(',') == -1) // this use of 'last' is only valid for one-dimensional arrays
      { int len = tt.Length;
        if (len == 4 || tt[4] == '-')
        { // Identified as keyword, not as function 'last(.)':
          tt = tt._Scoop(0, 4, "size(" + Nm + ")-__L1" );
        }              
      }
      // Array on LHS:  then e.g. "Arr1[2,3]=<exp.>" --> "__D = __assign(Arr1,2,3,<exp.>)":
      if (clbkt < InStr.Length-1 && InStr[clbkt+1]==C.CH[C.CHequal]) 
      { string rhs = InStr._Extent(clbkt+2);
        InStr = C.GeneralDummyVarName + C.CH[C.CHequal] + C.ArrayFnLHS + '(' + Nm + ',' + tt + ',' + rhs + ')'; 
      }
      // Array on RHS: then  e.g. Arr1[1,fn(..)] --> __segmt(Arr1,1,fn(..)):
      else  { tt = C.ArrayFnRHS + '(' + Nm + ',' + tt + ')';  InStr = InStr._ScoopTo(NmPtr, clbkt, tt); }
    }
    result.S = InStr;  result.B = true;  return result;
  }


  public static Quad HandleInject(string RHS, int FnLvl)
  {
    Quad result = new Quad(false);
    if (RHS.IndexOf("inject(",0) != -1)
    { if (FnLvl != 0) { result.S = "'inject' can only be used in the main program"; return result; }
      if (RHS.IndexOf("inject(",0) != 0) { result.S = "'inject' must be the first word in the line"; return result; }
      if (RHS.IndexOf("inject(text(__quote(") != 0) { result.S = "'inject' does not start with a literal character string"; return result; }
    }
    return result;
  }
//------------------------------------------------------------------------------
// Convert e.g. "func(a,b,c)" to "func % (a) % (b) % (c)", where % represents
//  the function operator, and a,b,c may be expressions. Result in .S.
// Errors: .B FALSE, and .S has error message.
  public static Quad ConvertFunctionRefs(string InStr, int FnLvl)
  {// Used below are class strings FnOpr, = C.CH[C.CHfunction], and
   //   CommaReplacer, = ')' + FnOpr + '('.
    Quad result = new Quad(false);  string snippet;  char typ, funtype;
    int ptr=0, opbkt, clbkt, n;  Duo pt;
    string ss, name="";
    do
    { opbkt = InStr.IndexOf('(', ptr);  if (opbkt == -1) break;
      if (opbkt > 0 && P.IdentifierChars.IndexOf(InStr[opbkt-1]) != -1)
      {// then this is taken as the bracket after a function reference:
        clbkt = JS.CloserAt(InStr, '(', ')', opbkt);
        if (clbkt == -1)
        { result.S = "bracket mismatch";  return result; }
        // Identify the name before the '(': (above guarantees there is a name)
        int qtr = InStr._FirstFromEndNotIn(P.IdentifierChars, 0, opbkt-1); 
        name = InStr._FromTo(qtr+1, opbkt-1);
        pt = NameLocn(name, 0, false, out funtype);
        if (funtype != 'F' && funtype != 'U')
        { result.S = "the name before the bracket ('" + name + "') is not a recognized function";  return result; }
        snippet = InStr._FromTo(opbkt+1, clbkt-1);//e.g. "a, b, c"
        // Check that REF args are single variable names
        if (funtype=='F' && F.SysFn[pt.X].REFArgCnt > 0)
        { string[] stroo = snippet.Split(','); // safe without worrying about nest level of ',', as only ref. args. in stroo[] will be accessed.

        // Deal with the only functions which are allowed to handle unregistered variable names.
        // Some background...
          // *** LEGAL versions of 'stroo[i]' can be:                                'oldname', 'newname' '__L1', 'size(arr1)', '__segmt(arr..'.
          // *** Corresponding returned values of 'out typ' from NameLocn will be:      'V'        ' '     'V'        '#'         '#'      
          // ***  ILLEGAL versions could be: 'new*name' (typ '#'), 'sin' (typ 'F'),  'MyUserFn' (typ 'U'). (Brackets after last 2: typ = '#'.)
          // *** ALSO, the call to NameLocn (with bool 'false') first checks current function level, and if it is not 0, then checks level 0.
          // *** It is VITAL to understand that later code should pick up all errors; all we have to do is ensure that where the user has
          // ***  written a SENSIBLE instruction and wants a valid name to be registered as either a scalar (Use 3, value 0) or an unassigned
          // ***  array, it will be so.
        // What we do below is pick out the new names (typ = ' '), not detected earlier by the parser, and assign them either as scalars or
        //   unassigned arrays, the choice depending on the particular function and in some cases on its placement within that function.
        //   We ignore the cases where typ = 'V' already, as they were registered earlier by the parser. But we crash if typ = '#'.
        //   We also ignore the cases where typ returned 'U' or 'V', as runtime code finds the errors, so no extra handling done here.
        //   (You could write in error detection here, but it would be a bit messy. Don't bother, eh?)
          if (name == "dim" || name == "dimlike" || name == "__array")
          { for (int i=0; i < stroo.Length; i++) 
            { pt = NameLocn(stroo[i], FnLvl, false, out typ); // searches this fn. level first; if not found, and FnLvl not 0, searches lvl.0.
              if (typ != ' ' && typ != 'V') break; // don't examine 'stroo' further if this can't be either an old array or an unused valid name.
              if (typ == 'V')
              { if  (pt.Y == 0 && V.GetVarUse(0, pt.X) == 1) break; // a constant, so array names cannot legally follow as 'dim' arguments.
                if (pt.Y == FnLvl) continue; // just pass over it for now; later code will check if this is a scalar. Even if it is, and
                                     // the user has added array names after it, runtime checking in SysFns will detect the misdemeanour.
              } // if the last equality failed, stroo[i] is not reg'd for this fn. level (and is not a constant), so the name is free to use.
              n = V.AddUnassignedArray(FnLvl, stroo[i]); // the 'n' is for trouble-shooting only.
            }
          }
          else if (name == "__constant")
          { pt = NameLocn(stroo[0], 0, true, out typ); // "__constant" can only occur in the main program.
            if (typ == '#'){ result.S = "improper variable name"; return result; } // context of error is added elsewhere.
            else if (typ == ' ') V.AddConstant(stroo[0], 0.0); // a dummy value, relaced in R.RunSysFn(.) - function "__constant".
          }
         // Functions 'unpack' converts them to scalars, and in runtime will fill these new scalars (along with named variables) with data.
          // The other three have args. converted to unassigned variables, which unit SysFn will sort into arrays or scalars.
          else if (name == "unpack" || name == "sequence" || name == "__scalar" || name == "__importscalar" || name == "__importarray")
          { n = 0;
            if (name == "sequence") n = 1;
            else if (name == "unpack")
            { // We allow the first arg. to be a function and so to contain commas; and we don't test it here. Therefore we must blot it out.
              string tt = JS.BlotOut(snippet, "(", ")", '*'); // to get rid of commas in 1st. arg, e.g. "unpack( data(1,2,3), a, b, c)".
              int p = tt.IndexOf(',');
              tt = tt._Extent(p+1);
              stroo = tt.Split(',');
              n = 0;
            }
            for (int i = n; i < stroo.Length; i++) // for 'unpack', the first arg. is ignored, as it must be an assigned array.
            { pt = NameLocn(stroo[i], FnLvl, true, out typ);
              if (typ == ' ')
              { // the variable name has not yet been met with by the parser. In all cases except "import array", it is to be made a scalar.
                if (name == "__importarray")
                { V.AddUnassignedArray(FnLvl, stroo[i]);
                } 
                else // to be made a scalar. Force Use to 3, so checks for unass'd RHS variables will not crash the fn.
                { int At = V.AddUnassignedVariable(FnLvl, stroo[i]); // ignore return value; leave error detection to later stuff.
                  V.SetVarValue(FnLvl, At, 0.0);
                }
              }
              else if (typ == 'V') // Suppose a scalar 'foo' is mentioned earlier in the program but unused, as in "if (1==2) foo = 1;".
                                   //  It will have been registered as type 'V', but with use 0. This will result in a runtime error message:
                                   //  Search for "is an unassigned variable" in class R (the version prefixed by 'argument', not by 'operand').
              { if (V.GetVarUse(FnLvl, pt.X) == (byte) 0)  V.SetVarUse(FnLvl, pt.X, (byte) 3);
              }
              else if (typ == '#'){ result.S = "improper variable name"; return result; } // context of error is added elsewhere.
            }
          }
        }
       // isolate and operate on the function's argument statement "a,b,c":
       // (A very temporary blank is inserted at the beginning of snippet, in
       //  case snippet begins with a bracket - that would cause the fn. to
       //  search incorrectly at the next bracket level up.)
        ss = JS.ReplaceAtLevel(' ' + snippet, '(',')', ',', FnComma) + ')';
        snippet = FnOpr + '(' + ss._Extent(1);
           // Now looks like "%(a)%(b)%(c)", which, after grafting back to
           //  follow the function name ("boo") will give: "boo%(a)%(b)%(c)".

        InStr = InStr._ScoopTo(opbkt, clbkt, snippet);
        ptr = opbkt; // by now, this points to the first '%' after "boo".
      }
      else ptr++;
    } while (ptr < InStr.Length);
   result.B = true; result.S = InStr;  return result;
  }

//------------------------------------------------------------------------------
// Returns assignment no. in .I.
// ERROR: usual pair, .B and .S.
  public static Quad BuildAsstRow(string AsstStr, int FnLvl)
  { Quad result = new Quad(false);
    TTerm term = new TTerm(0);
    List<TTerm> termery = new List<TTerm>(); // will accumulate the terms, for adding into R.Assts.
    char[] Assig = AsstStr.ToCharArray();
    bool is_alpha = false;  byte is_opn;    int n, nmstart=-1;
    char nuthin = C.CH[C.CHbkmark], thisch = nuthin, nmtype;
    bool namegathering = false, namejustended = false;
    string Nm="";  Duo nmpt;  short nestlvl = 0;
    char termstate = '0'; // '0' = no term being built; '+' = awaiting
     // opcode/hier; '!' = term finished, and ready for implanting in R.Assts[].
    bool[] insidefn = new bool [1 + AsstStr._CountChar('(')];// >= nec. size
    for (int i = 0; i < insidefn.Length; i++) insidefn[i] = false;
    bool equalfound = false; // true after '=' found.
    bool expectOpener = false; // TRUE after a function name has been found, so that next char. should be '('

  // LOOP THROUGH ALL CHARACTERS:
    for (int chptr = 0; chptr <= Assig.Length; chptr++)//goes beyond end ('<=')
    { if (chptr == Assig.Length)thisch = nuthin; else thisch = Assig[chptr];
      if (expectOpener)
      { if (thisch == '(') expectOpener = false;
        else { result.S = "function name without brackets";  return result; }
      }
      is_alpha = false;   is_opn = 0xFF; // value equiv. to FALSE
      if (P.IdentifierChars.IndexOf(thisch) != -1) is_alpha = true;
      else
      { n = Array.IndexOf(OpCHcodes, thisch);
        if (n != -1) is_opn = Convert.ToByte(n); // else leave as is, = 0xFF.
      }
     // ALPHANUMERIC CHARACTER:
      if (is_alpha ) // start / continue gathering the name in Nm:
      { if (!namegathering)
        { namegathering = true; Nm = thisch.ToString();
          nmstart = chptr;  namejustended = false; }
        else { Nm += thisch; }
      }
     // NOT AN ALPHANUMERIC CHARACTER:
      else
      { if (chptr==0){result.S= "LHS must start with a var. name";  return result;}
        if (namegathering) // The previous char. was the last of a name:
        // NAME JUST FINISHED - DEAL WITH IT:
        { namegathering = false;  namejustended = true;
          nmpt = NameLocn(Nm, FnLvl, false, out nmtype);// Preexsting thingos: 'V','F',
                  // 'U'.  Not recog, but good name --> ' '. Lousy name --> '#'.
          if (nmtype == '#'){result.S= "invalid name: " + Nm;  return result;}
          // NAME IS A VARIABLE NAME:
          if (nmtype == 'V')
          { term.Fn = (short) nmpt.Y;  term.At = (short) nmpt.X;
            byte vu = V.GetVarUse(nmpt.Y,nmpt.X);
            if (term.Fn != FnLvl && vu != 1) // then this is a main pgm.
              // variable, but we are in a user function, so can't access it
              // (if it is not a constant):
            { nmtype = ' '; }// Will be picked up by the 'if' a few lines down.
            else // a registered variable:
            { term.X = V.GetVarValue(nmpt.Y,nmpt.X);
              term.VUse = vu;
            // Fields not used, so left as default: ArgsDue
            // Fields filled later in this fn.: OpCode, Hier, Rating
            // Filled only at runtime (apart from op. flds.):
            termstate = '+'; }
          }
          // NAME IS VALID BUT UNREGISTERED NAME, so take it as a new variable:
          if (nmtype == ' ') // NOT 'else if', as above 'if' can spawn it.
          { // Add a new variable, which will be unassigned(.Use field = 0) UNLESS
            //  it begins with '__', in which case it will have .Use set to 2.
            nmpt = AddNewVariable(Nm, FnLvl); //nmpt now has same mng. as above.
            term.Fn = (short) nmpt.Y;  term.At = (short) nmpt.X;
            // All other fields left at TTerm's constructor defaults.
            termstate = '+';
          }
          // NAME IS A FUNCTION:
          else if (nmtype=='F' || nmtype == 'U')
          { if (nmstart==0){result.S= "LHS must start with a var. name";  return result;}
            if (termstate!='0'){result.S= "some omission just before: "+Nm; return result;}
             // Generate a dummy variable as the next term:
            if (nmtype=='F') // is SYSTEM function:
            { term.Fn = SysFnMarker; }// SysFnMarker is -20
            else // is USER function:
            { term.Fn = UserFnMarker; }// UserFnMarker is -30
            term.At = (short) nmpt.X;
             // These .Type settings are only used for system fns., but there is
             //  no harm in setting them for user fns. (ignored).
            term.VUse = 3; // a dummy, as will never be accessed till fn. has been
              // evaluated, and by that time VUse will have been reset by the fn.
            termstate = '+';
            expectOpener = true; // next char. must be '('
          }
        } // END OF PROCESSING A NAME JUST FINISHED

       // Continue processing non-name char:
       // Is a BRACKET:
        if (thisch == '(')
        { nestlvl++;
          insidefn[nestlvl] = true;
        }
        else if (thisch == ')')
        { insidefn[nestlvl] = false;
          nestlvl--;
          if (nestlvl < 0)
          { result.S= "unmatched ')' occurred. (This error can result from inadvertently using '=' where '==' intended, e.g. in 'x = (y = 1);'";  return result; }
        }
        // Is the COMPLEMENTER:
        else if (thisch == C.Complementer)
        { if (termstate != '0')
          { result.S= "Some pgm. prob. with complementing char."+Nm; return result;}
          // construct a negating term:
            term.Fn = ComplementMarker;
            term.VUse = 0xFD; // A temporary dummy filler, never accessed. After the 1st. pass over the assignment, will be changed to 3 or 11.
                              // No matter; the value is not used in any case, as the term is just a carrier of the unary complement opcode.
            term.OpCode = snNot; term.Hier = Hierarchies[snNot];
            term.Rating = Convert.ToInt16(nestlvl*NestCoeff + term.Hier);
                // NB - this takes the CURRENT nest level for calculating Rating,
                // NOT the nest level of the next sign, as variable terms do.
            // Fields not used: Fn, At, ArgsDue, Ptr, Offset, Extent, X.
            // Fields filled later in this fn.: nil.
            // Filled at runtime (apart from op. flds.): nil.
            termstate = '!'; // term finished, and awaiting placement.
        }
        // Is the NEGATOR:
        else if (thisch == C.Negator)
        { if (termstate != '0')
          { result.S= "Some pgm. prob. with negating char."+Nm; return result;}
          // construct a negating term:
            term.Fn = NegatorMarker;  
            term.VUse = 0xFD; // A temporary dummy filler, never accessed. After the 1st. pass over the assignment, will be changed to 3 or 11.
                              // No matter; the value is not used in any case, as the term is just a carrier of the unary negate opcode.
            term.OpCode = snNegate;  term.Hier = Hierarchies[snNegate];
            term.Rating = Convert.ToInt16(nestlvl*NestCoeff + term.Hier);
                // NB - this takes the CURRENT nest level for calculating Rating,
                // NOT the nest level of the next sign, as variable terms do.
            // Fields not used: Fn, At, ArgsDue, Ptr, Offset, Extent, X.
            // Fields filled later in this fn.: nil.
            // Filled at runtime (apart from op. flds.): nil.
            termstate = '!'; // term finished, and awaiting placement.
        }
          // Is an OPCODE:
        else if (is_opn != 0xFF)
        { if (termstate != '+')
          { result.S= "Some pgm. prob. with term building after "+Nm; return result;}
          term.OpCode = is_opn;   term.Hier = Hierarchies[is_opn];
          term.Rating = Convert.ToInt16(nestlvl * NestCoeff + term.Hier);
          termstate = '!';
          // check for sign at nestlevel 0 preceding this one ( a no-no):
          if (!equalfound && nestlvl == 0 && is_opn != P.snEq)
          { result.S= "left side is an expression, not a single term"; return result;}
          if (is_opn == P.snEq) equalfound = true;
        }

        // Is the END-OF-ASSIGNMENT CHARACTER:
        else if (thisch == nuthin)//then have reached end of AsstStr:
        {
          if (nestlvl != 0){result.S= "unmatched '(' occurred";  return result;}
          if (termstate != '+'){result.S= "improper end";  return result;}
          termstate = '!'; // Leaves default zero OpCode and Hier.
        }

      } // END OF 'IF NOT AN ALPHANUMERIC CHARACTER'

      if (termstate == '!')
    // ADD TERM TO what will later become a TTerm[] array of List<TTerm[]> R.Assts:
      { termery.Add(term);
        if (thisch != nuthin) // then get ready for further terms:
        { term = new TTerm(0);  termstate = '0'; }
      }
       // End-of-loop parameter adjustments:
      if (namejustended == true)
      { namejustended = false;  Nm = ""; }
    }
    // END OF LOOP THROUGH CHARACTERS
 //--------------------------------------
    result.I = R.Assts.Count; // i.e. what will be the index of this asst., after we add it to the end
    R.Assts.Add(termery.ToArray());
    result.B = true;  return result;
  }

//------------------------------------------------------------------------------
// If there is already a literal in function level 0 with the value 'value', this
//  fn. will simply return the existing literal's name. If not, it will add
//  a nice new literal to V.Vars[0][i], and return its name (e.g. '__L23',
//  if it comes to occupy V.Vars[0].[23]). ALL LITERALS AT ALL FN. LEVELS ARE
//  STORED ONLY IN THE MAIN PGM LEVEL (0).
  private static string AssignLiteral(double value)
  { int n = V.FindLiteralValue(value);
    if (n > -1) return V.GetVarName(0, n);
    // Not found, so create it:
    string ss = "__L" + V.Vars[0].Count.ToString();
    V.AddConstant(ss, value);
    return ss;
  }
//------------------------------------------------------------------------------
// Detect all literals in string InStr. If valid, assign to a variable, and
//  substitute that variable; if not valid, .B and .S reflect the sad fact.
// NB: Literals go in signed; e.g. "a-3" --> "a + __L23", and __L23 --> -3.
// Blanks must have been cleared out of InStr before calling this.
// NB: Fails if the first char. is part of a number. This should never happen,
// as only assignments are sent here, and they must never begin with a numeral.
  private static Quad ReplaceLiterals(ref string InStr)
  { Quad result = new Quad(false);
    Octet och;  int startptr = 0; // if you don't use this, the loop below will
                               // detect e.g. '23' of the '__L23' just added!
    while (true)
    { och = JS.FindNumber(InStr, startptr, int.MaxValue, true, P.IdentifierChars);
                        //ignores init.'-', and ignores nos. imbedded in names.
      if (!och.BX) break; // No more candidate literal nos. found.
      if (!och.BY) // Candidate no. found, but evaluation threw an error.
      { result.S = "error trying to evaluate '" + och.SX + "'"; return result;}
      // a valid identifier found:
      string ss = AssignLiteral(och.XX);// redundant signs parsed out elsewhere.
      InStr = InStr._ScoopTo(och.IX, och.IY, ss);
      startptr = och.IX + ss.Length; // clear the added name.
    }
    result.B = true;  return result;
  }
//------------------------------------------------------------------------------
// *** This has been tacked on long years after the parsing routine has been written, so is not an ideal approach. In future
//  versions of the program it should come before assignments are delineated.
  private static Quad ProcessQueryStatements()
  { Quad result = new Quad(false);
    // Caution here... You can't use C.CH[if], you have to use C.CH[iffinal]; similarly, C.CH[elsefinal].
    char equalsCode = C.CH[C.CHequal], eqeqCode = C.CH[C.CHeq_eq],  notequalsCode = C.CH[C.CHnot_eq], greaterCode = C.CH[C.CHgreater];
    char ifCode = C.CH[C.CHiffinal], thenCode = C.CH[C.CHthen],
      elseCode = C.CH[C.CHelsefinal], endifCode = C.CH[C.CHendif], openerCode = C.AssOpener, closerCode = C.AssCloser;
    string query = C.AssCloStr + '?' + openerCode;
    for (int fn = 0; fn < C.UserFnCnt; fn++)
    {
      bool queryFound = false;
      int ptr = 0;
      string FnText = P.UserFn[fn].Text;
      while (true)
      { ptr = FnText.IndexOf(query, ptr);
        if (ptr == -1) break; // no more of these '?' constructions left.
        queryFound = true;
        result.I = parag(fn, ptr); // all error messages need this.
        // Get START AND END of the whole query statement: 
        int pQstart, pQend, p;
        pQstart = ptr-1;
        while (pQstart >= 0)
        { char c = FnText[pQstart];  if (c == openerCode) break;
          pQstart--;
        }
        pQend = FnText.IndexOf(closerCode, ptr+3);
        if (pQstart == -1 || pQend == -1)
        { result.S = "Code for handling a '?' statement failed: call programmer"; return result; } // Should be impossible
        // GET ELEMENTS which are to be recombined in the replacement statement 
        // (A) from ASST TO LEFT of the '?'
        string origLHS, condition;
        var tt = FnText.Substring(pQstart+1, ptr - pQstart - 1); // All between the first pair of assignment delimiters
        p = tt.IndexOf(equalsCode);
        if (p < 1 || p >= tt.Length-1) { result.S = "Incorrect use of the question mark symbol ('?')";  return result; }
        origLHS = tt.Substring(0, p);
        condition = tt.Substring(p+1);
        // (B) from ASST TO RIGHT of the '?'
        int[] colonPtrs = FnText._IndexesOf(':', ptr+3, pQend);
        int noColons = colonPtrs._Length();
        if (noColons < 1 || noColons > 2) { result.S = "No colons found after '?', so not a proper query statement";  return result; }
        int eq = FnText.IndexOf(equalsCode, ptr+4);
        if (eq > colonPtrs[0]-2) { result.S = "Improper statement after '?', so not a valid query statement";  return result; }
        string outcome1 = FnText._Between(eq, colonPtrs[0]); // The asst was "--di = foo:..."; we have captured the 'foo' bit
        if (outcome1 == "")  { result.S = "Incorrectly formatted query statement";  return result; }
        string outcome2;
        // PREPARE REPLACEMENT STATEMENT - we will replace all between pQstart and pQend INCLUSIVE.
        StringBuilder replacer = new StringBuilder();
        // SINGLE COLON VERSION:
        if (noColons == 1)
        { outcome2 = FnText._Between(colonPtrs[0], pQend);
          replacer.Append(ifCode);     replacer.Append(openerCode);   
          replacer.Append(condition);  replacer.Append(notequalsCode);  replacer.Append('0');       replacer.Append(closerCode);
          replacer.Append(thenCode);   replacer.Append(openerCode);     replacer.Append(origLHS);   replacer.Append(equalsCode);
          replacer.Append(outcome1);   replacer.Append(closerCode);     replacer.Append(elseCode);  replacer.Append(openerCode);
          replacer.Append(origLHS);    replacer.Append(equalsCode);     replacer.Append(outcome2);  replacer.Append(closerCode);
          replacer.Append(endifCode);
        }  
        else // TWO-COLON VERSION:
        { outcome2 = FnText._Between(colonPtrs[0], colonPtrs[1]);
          string outcome3 = FnText._Between(colonPtrs[1], pQend);
          replacer.Append(ifCode);      replacer.Append(openerCode);   
          replacer.Append(condition);   replacer.Append(greaterCode);   replacer.Append('0');       replacer.Append(closerCode);
          replacer.Append(thenCode);    replacer.Append(openerCode);    replacer.Append(origLHS);   replacer.Append(equalsCode);
          replacer.Append(outcome1);    replacer.Append(closerCode);    replacer.Append(elseCode);
          replacer.Append(ifCode);      replacer.Append(openerCode);   
          replacer.Append(condition);   replacer.Append(eqeqCode);      replacer.Append('0');       replacer.Append(closerCode);
          replacer.Append(thenCode);    replacer.Append(openerCode);    replacer.Append(origLHS);   replacer.Append(equalsCode);
          replacer.Append(outcome2);    replacer.Append(closerCode);    replacer.Append(elseCode);
          replacer.Append(openerCode);
          replacer.Append(origLHS);    replacer.Append(equalsCode);     replacer.Append(outcome3);  replacer.Append(closerCode);
          replacer.Append(endifCode);  replacer.Append(endifCode);
        }  
        FnText = FnText._ScoopTo(pQstart, pQend, replacer.ToString());       
        ptr = pQstart + replacer.Length;
        if (ptr >= FnText.Length) break;
      }
      if (queryFound) P.UserFn[fn].Text = FnText;
    }
    result.B = true;  return result;
  }



//------------------------------------------------------------------------------
// Go through all function texts, find every assignment, and prepare a corresponding
//  row in R.Assts[,] for each. If no error, only .B (TRUE) has meaning.
// Also prepare PreFlow, the string array from which FlowLine is built.
// ERROR: returns .B and .S as usual, .I as rich edit par., .X as fn. no.
  private static Quad ProcessAssignments(bool RecordParsedAssts)
  { Quad quo, que, qui, result = new Quad(false);  Twelver ass;
    string RawCandidate="", AssText;  int LFcnt, asstno;
    // Go through all the assignments in the whole pgm.
    for (int fn = 0; fn < C.UserFnCnt; fn++)
    { int AfterLastAsst = 0;
      StringBuilder preflow = new StringBuilder();
      result.X = (double) fn; // All error messages would need this.
      int startptr = 0;
      while (true)
  // GET NEXT ASSIGNMENT:
      { ass = C.FindNextAsst(ref P.UserFn[fn].Text, startptr);
        AssText = ass.SZ;
        result.I = parag (fn, ass.IX+1); // all error messages need this.
      // Filter gross errors, including wrong mix of '=' and comparators:
        if (!ass.BX) break; // no more assts. in this function.
        else if (!ass.BY) // error in asst. - should never be possible!
        { result.S = "Oh no! Call the programmer!"; return result; }
        if (ass.XX > 1.0) // i.e. more than one '=' in the assignment
      // DEAL WITH MULTIPLE ASSIGNMENTS IN ONE STATEMENT: "x = y = z = foo;". This will be replaced in user-fn. text by
      // the equivalent of "z = foo; y = z;  x = y;". [Earlier version's replacement was ""x = foo; y = foo; z = foo;". But if
      // foo is a function which, like fn. plot, returns a different value with each call, then you would have three different values,
      // together with three separate plottings of the same curve!]    
        { int noEqSigns = (int) ass.XX,  noLFs = AssText._CountChar('\n');
          if (noLFs > 0) AssText = AssText._Purge('\n'); // We'll stick them back on later.
          int[] equator = AssText._IndexesOf(C.CH[C.CHequal]);
          if (equator._Length() != noEqSigns) { result.S = "Incomprehensible line: " + AssText;  return result; }
          StringBuilder sb = new StringBuilder(); // It will be in the form [[...]][[...]][[...]] (for as many derived assmts. as necessary).
          // First we put in the final equation of the series:
          sb.Append(C.AssOpener);  
          string ss = AssText._Extent(1 + equator[noEqSigns-2]); // what is before and after the last '='.
          if (ss[0] == '(') // as would be the case for "x = (y = 1)" where the user really meant "x = (y == 1)".
          { result.S = "Misuse of multiple '=' signs. (Have you used '=' instead of '==' in a conditional expression?)"; return result; }
          sb.Append(ss); // what is before and after the last '='.
          sb.Append(C.AssCloser);  
          // Now the copy cat assignments, in reverse order:
          for (int i = noEqSigns-2; i >= 0; i--)
          { sb.Append(C.AssOpener);  
            if (i > 0)
            { sb.Append(AssText._FromTo(equator[i-1] + 1, equator[i+1] - 1 ) ); // All after the prev. '=' and before the next '='; will include this '='.
              sb.Append(C.AssCloser);  
            }
            else // we won't append the final closer yet
            { sb.Append(AssText._FromTo(0, equator[i+1] - 1 ) ); } // All from the start and before the second '='; will include this first '='.
          }
          sb.Append(C.AssCloser);  
          for (int j=0; j < noLFs; j++) sb.Append('\n');
          P.UserFn[fn].Text = P.UserFn[fn].Text._FromTo(0, ass.IX-1) + sb.ToString() + P.UserFn[fn].Text._Extent(ass.IY+1);
          continue; // Reenter the 'while' loop with startptr still pointing to the same place.
        }
        if (!ass.BZ && ass.IZ==0)
        { result.S = "no '=' or comparator in: " + AssText; return result; }
        if (ass.BZ && (ass.SX=="" || ass.SY==""))
        { result.S = "incomplete assignment: " + AssText;  return result; }
       // *** This value of ass is used repeatedly below - don't reassign it.
        if (RecordParsedAssts) RawCandidate = AssText;
  // CHECK FOR AN LHS OF THIS ASSIGNMENT:
        if (!ass.BZ) // then this is a conditional expression, so add a dummy
                     // variable and assignment to it:
        { AssText = C.GeneralDummyVarName + C.CH[C.CHequal] + AssText; }//"__D"
  // REPLACE LITERALS THROUGHOUT ASSIGNMENT
        quo = ReplaceLiterals(ref AssText);
        if (!quo.B)
        { result.S = "numerical error in '" + AssText + "':  " + quo.S;
          result.I = parag (fn, ass.IY-1); return result; }
  // FINAL PREPROCESSING OF ASSIGNMENT STRING:
        quo = CheckCharSequences(AssText); // Detects illegal chars. and many wrong
        // comb'ns. of legal ones, resolves redundancies, assigns Negator, and
        // returns the doctored AsstStr in quo.S. Note that LFs are removed here,
        // but that quo.I returns no. LFs removed, for reinsertion further down.
        if (!quo.B){ result.S = "in '" + AssText + "', " + quo.S;  return result; }
        LFcnt = quo.I;
        qui = ConvertArrayRefs(quo.S); // returns doctored AsstStr in qui.S
        if (!qui.B){result.S="array bracketting faulty in: "+AssText;return result;}
  // DEAL WITH ARGUMENT LISTS OF FUNCTIONS  
        quo = ConvertFunctionRefs(qui.S, fn); // returns doctored AsstStr in quo.S
        if (!quo.B){result.S="fault in '"+AssText+"': "+quo.S ;return result;}
    // - - - - - - - - - - - - - -- - - - - -
    // *** BUILD THE ROW IN R.ASSTS[][]:
    // - - - - - - - - - - - - - -- - - - - -
       que = BuildAsstRow(quo.S, fn);
        if (!que.B)
        { result.S = "In '" + AssText + "', " + que.S;
          result.I = parag (fn, ass.IY-1); return result; }
        asstno = que.I;
        if (RecordParsedAssts)
        { RawAsstText.Add(RawCandidate);
          CookedAsstText.Add(AssText);
        } // *** If adding code, don't reuse qu.. till after PreFlow finished below.
       // BUILD THE STRING IN PREFLOW[]:
       // Stick all since the last assignment into PreFlow:
        if (ass.IX - AfterLastAsst >= 1) // i.e. at least 1 char. since last asst.
        { preflow.Append(P.UserFn[fn].Text._FromTo(AfterLastAsst, ass.IX-1));}
        AfterLastAsst = ass.IY+1; // used during next loop, and below.
        startptr = AfterLastAsst;
       // Stick this assignment into PreFlow:
        preflow.Append(Char.MaxValue);  preflow.Append((char)asstno);
        if (LFcnt > 0) preflow.Append('\n', LFcnt);
  // - - - - - - - - - - - - - -- - - - - -
      }// END OF 'WHILE' loop through ALL ASSTS. OF THIS FUNCTION LEVEL.
         // Add on any control stuff after the last assignment:
      if (AfterLastAsst < P.UserFn[fn].Text.Length)
      for (int i = AfterLastAsst; i < P.UserFn[fn].Text.Length; i++)
      { preflow.Append(P.UserFn[fn].Text[i]); }
      PreFlow[fn] = preflow.ToString();
    }// END OF 'FOR' loop through FUNCTIONS.
    result.B = true;  return result;
  }
//------------------------------------------------------------------------------
// Good result: .B TRUE. Error: .B FALSE, .S has msg., .I has paragraph ref.
  public static Quad BuildFlowLine()
  { Quad result = new Quad(false);   result.I = -1;
    int n, iflvl;
    char IF = C.CH[C.CHiffinal], THEN = C.CH[C.CHthen], ELSE = C.CH[C.CHelsefinal],
       ENDIF = C.CH[C.CHendif], BACKIF = C.CH[C.CHbackif], EXIT = C.CH[C.CHexit],
       RETURN = C.CH[C.CHreturn], BREAK = C.CH[C.CHbreak],
       CONTINUE = C.CH[C.CHcontinue], DUMMY = C.CH[C.CHdummymarker];
    char lasttermgenr = ' '; // the last character to generate a term.
                      // In particular, ignores LFs. (Dummy start value here.)
    int ParCnt;  bool BreakOrContFound;
    R.FlowLine = new TBubble[C.UserFnCnt][];
    TBubble[] fnline;
    TBubble bub = new TBubble();

 // LOOP THROUGH FUNCTIONS:
    for (int fn = 0; fn < C.UserFnCnt; fn++)
    { ParCnt = P.UserFn[fn].LFBeforeOpener;
      fnline = new TBubble[PreFlow[fn].Length+10]; // more than enough room;
      //**** BUT IF ADDING MORE DUMMY MARKERS, will need to adjust this array size.
      iflvl = -1; // First IF in the line will increment this, to have iflvl of 0.
      bool Imbed = false;   BreakOrContFound = false;
      char[] pref = PreFlow[fn].ToCharArray();
      char ch = '\u0000',  lastch = ch;  int endptr = pref.Length-1;
      int pf = PreFlow[fn]._CountChar(IF);
      int[] IFs = new int[pf];
      int[] THENs = new int[pf];
      int[] ELSEs = new int[pf];
      for (int i = 0; i < pf; i++){ IFs[i] = -1;  THENs[i] = -1;  ELSEs[i] = -1; }
      int bubno = 0; // will be the index of bub, when implanted into fnline.
      bub.Ass = Convert.ToInt16(R.DummyMarker); // -1
      bub.Next = -1;  bub.IfNot = -1;  bub.Ref = -1;
      bool nonvoidreturn = false;

  // LOOP THROUGH CHARS. OF PreFlow[fn]:
      int AfterIFCount = 100; // i.e. starts way above 2. Incremented for every NON-CR char.
  //  Reset to 0 if ch is IF, then increments till detected by THEN,f
  // by which time it must be 3. 
      for (int chptr = 0; chptr < pref.Length; chptr++)
      {// When updating lastch, distinguish between '\n' and Asst. 10
       //  (in which case the 10 would be preceded by Char.MaxValue):
        if (ch != '\n' || lastch == Char.MaxValue) lastch = ch;
        ch = pref[chptr];  AfterIFCount++; // A.I.C. is decremented if ch = \n.
       // Note that the bubble 'bub' in some cases is still open for more
       //  additions to it, from the previous loop. (The case, if Imbed was FALSE.)
       // Note also that if an enormous user program had more than C.CHBASE
       //  assignments (i.e. > 0xE000), there would be overlap with CH codes.
       //  While this is not likely, what is possible is that future programming
       //  might introduce other lower codes. If so, they will need the same
       //  proviso as appears in the next line of code for char. 10 - i.e., a
       //  check for a preceding "next-is-an-assmt." code (Char.MaxValue).
        if (ch=='\n' && lastch != Char.MaxValue)//i.e. if not the 10 of Asst. 10!
        { ParCnt++; Imbed=false; AfterIFCount--; }//ignore LFs, after using them to update ParCnt.
        else if (ch == Char.MaxValue) // = warning that next ch is an Asst. no.
        { if (chptr == endptr)
          { result.S = "assignment missing at end of function or pgm.";
            result.I = ParCnt;  return result; }
          else Imbed = false;
        }
       // ASSIGNMENT found:
        else if (lastch == Char.MaxValue) // then this ch codes an assignment no.:
        { bub.Ass = (short) ch;
          bub.Next = Convert.ToInt16(bubno + 1); // Later altered, if next item is ELSE.
          bub.Ref = Convert.ToInt16(ParCnt);
          Imbed = true;
        }
        else if (ch == DUMMY)
        { bub.Ass = Convert.ToInt16(R.DummyMarker);
          bub.Next = Convert.ToInt16(bubno + 1);
          bub.Ref = Convert.ToInt16(ParCnt);
          Imbed = true;
        }
        else if (ch == IF)
        { bub.Ass = Convert.ToInt16(R.IFMarker); // -10.
          bub.Next = Convert.ToInt16(bubno + 1);
          bub.Ref = Convert.ToInt16(ParCnt);
          iflvl++; // First IF in the line will then have iflvl of 0.
          IFs[iflvl] = bubno;
          Imbed = true;   AfterIFCount = 0;
        }
        else if (ch == THEN)
        { if (AfterIFCount != 3) // <IF> <asst.marker><asst.no.><THEN>
          { result.S = "illegal syntax after 'if'";
            result.I = ParCnt;  return result; 
          }
          THENs[iflvl] = bubno; // no. of the next assignment
          Imbed = false;
        }
        else if (ch == ELSE)
        { if (IFs[iflvl] == -1)
          { result.S = "'if' missing / misplaced before 'else'";
            result.I = ParCnt;  return result; }
          if (THENs[iflvl] == bubno)
          { result.S = "no action between 'if (..)' and 'else'";
            result.I = ParCnt;  return result;
          }
          bub.Ass = Convert.ToInt16(R.DummyMarker); // the dummy marker here
                        // has the meaning of "END-OF-THEN" rather than of ELSE,
                        // which remains implicit.
          // bub.Next will be set later, from the corresponding ENDIF.
          bub.Ref = Convert.ToInt16(ParCnt);
          if (ELSEs[iflvl] != -1)
          { result.S = "duplication of 'else'";
            result.I = ParCnt;  return result;
          }
          ELSEs[iflvl] = bubno+1; // = no. of the next assignment. As mentioned,
               // this bubble is NOT an 'else' bubble. There is no 'else' bubble.
          Imbed = true;
        }
        else if (ch == ENDIF || ch == BACKIF)
        { if (IFs[iflvl] == -1)
          { result.S = "syntax error with 'if' (technical jargon: IF missing / misplaced before ENDIF / BACKIF)";
            result.I = ParCnt;  return result; }
          if (ch == ENDIF) bub.Ass = Convert.ToInt16(R.ENDIFMarker);
          else bub.Ass = Convert.ToInt16(R.BACKIFMarker);
          if (ch == BACKIF) bub.Next = Convert.ToInt16(IFs[iflvl]);
          else bub.Next = Convert.ToInt16(bubno + 1);
          bub.Ref = Convert.ToInt16(ParCnt);
          if (ELSEs[iflvl] == -1) // no ELSE:
          { if (ch == ENDIF)
            { fnline[IFs[iflvl]+1].IfNot = Convert.ToInt16(bubno); }
            else // BACKIF: must jump over the dummy marker, or eternal loop!
            { fnline[IFs[iflvl]+1].IfNot = Convert.ToInt16(bubno+1); }
          }
          else // there is an ELSE: (and therefore BACKIF can't apply: it does not use 'else')
          { fnline[IFs[iflvl]+1].IfNot = Convert.ToInt16(ELSEs[iflvl]);
            fnline[ELSEs[iflvl]-1].Next = Convert.ToInt16(bubno);
          }
          IFs[iflvl] = -1;  THENs[iflvl] = -1; ELSEs[iflvl] = -1; iflvl--;
          Imbed = true;
        }
        else if (ch == EXIT)
        { if (fn != 0)
          { result.S = "'exit' cannot be used in functions. Use 'return'.";
            result.I = ParCnt;  return result; }
          bub.Ref = Convert.ToInt16(ParCnt); //.Ass is already -1.
          bub.Next = Convert.ToInt16(R.EXITNext); // -100
          // Leave .Ass as the dummy marker.
          Imbed = true;
        }
        else if (ch == RETURN)
        { if (fn == 0)
          { result.S = "'return' cannot be used outside of functions. Use 'exit'.";
            result.I = ParCnt;  return result; }
          // If the next bubble would be an assignment, set a reminder for that
          //  assignment to have its .Next set to RETURNNext. If not, then this
          //  is to be an empty return, so set a return dummy here:
          if (chptr < endptr && pref[chptr+1] == Char.MaxValue)
          { nonvoidreturn = true; Imbed = false; }
          else // is either the last ch, or a non-assignment follows (e.g. IF):
          { bub.Ref = Convert.ToInt16(ParCnt); //.Ass is already -1.
            bub.Next = Convert.ToInt16(R.RETURNNext); // -200
            // Leave .Ass as the dummy marker and .X as default 0.0.
            Imbed = true;
          }
        }
        else if (ch == BREAK || ch == CONTINUE)
        {// Stick in a marker, and wait till after the fn. loop is over before
         //  dealing with its .Next field.
          if (ch == BREAK) bub.Ass = Convert.ToInt16(R.BREAKMarker);
          else bub.Ass = Convert.ToInt16(R.CONTMarker);
          bub.Ref = Convert.ToInt16(ParCnt); //.Ass is already -1.
          // bub.Next will be set after the end of the fn. loop.
          BreakOrContFound = true;
          Imbed = true;
        }
        else // unrecognized char.:
        { result.S = "unrecognized char '" + ch + "' (unicode " + Convert.ToInt32(ch) + ")";
//        { result.S = "pgm. error - unrecognized PreFlow[] char";
          result.I = ParCnt;  return result; }

       // IMBED, IF 'Imbed' IS TRUE:
        if (Imbed)
        { lasttermgenr = ch; // useful as LFs may be interposed between elements.
         // Catch a data-returning RETURN:
          if (nonvoidreturn) // from testing above, we are sure that a valid
            // assignment is being laid down. It would normally be a dummy asst.,
            // but will work OK even if not. (e.g. if the user wrote:
            // "return a=x^3" instead of "return a" or "return x^3".)
          { bub.Next = Convert.ToInt16(R.RETURNNext);
            nonvoidreturn = false; // nec., as there can be multiple returns.
          }
          fnline[bubno] = bub;  bubno++;
           // Prepare the next blank bubble:
          bub.Ass = Convert.ToInt16(R.DummyMarker);
          bub.Next = -1;  bub.IfNot = -1;  bub.Ref = -1;
          Imbed = false;
        }
      }// END of FOR loop through CHARS. OF PreFlow[fn]
// - - - - - - - - - - - - - - - - - - -- - - - -
      // FINAL CHECKS AND ADJUSTMENTS:
      if (Imbed)
      { result.S = "unexpected end of PreFlow[]";
        result.I = ParCnt;  return result; }
     // Any leftover IFs?
      for (int i = 0; i < IFs.Length; i++)
      { if (IFs[i] != -1)
        { result.S = "ENDIFs were still due at the end of the pgm / fn.";
          result.I = ParCnt;  return result; }
     // Forward leapfrogs must have something to land on. Therefore a
         //  dummy marker must be added after a final BACKIF:
      }
      if (lasttermgenr == BACKIF)//i.e. last char. to generate a term;
                                 // specifically, final LFs are ignored.
      { bub.Ref = Convert.ToInt16(ParCnt); //.Ass is already the dummy marker value.
        bub.Ass = Convert.ToInt16(R.DummyMarker);
        bub.Next = Convert.ToInt16(R.EXITNext);
        bub.IfNot = -1;  bub.Ref = -1;
        fnline[bubno] = bub;  bubno++;
      }
      // ASSIGN BREAK / CONTINUE ADDRESSES. (Could not be done until the above
      //  step made sure there was always something in a flow line after BACKIF.)
      if (BreakOrContFound)//then worthwhile searching:
      {// Go forward through the flowline, and for every break/continue find the 
       // first subsequent BACKIF which has a return address (in .Next) pointing to earlier than 
       // the address of the break/continue.
        short ass1;   int find;
        for (int i = 0; i < bubno; i++)
        { ass1 = fnline[i].Ass;
          if (ass1==R.BREAKMarker || ass1==R.CONTMarker)
          { find = -1;
            for (int j = i+1; j < bubno; j++)
            { if (fnline[j].Ass == R.BACKIFMarker && (int) fnline[j].Next < i)
              { find = j; break; }
            }  
            if (find == -1){ result.S = "'break' or 'continue' not contained in a loop";  result.I = ParCnt;  return result; }
            // Found the right BACKIF, so adjust the break/cont's .Next field:
            if (ass1 == R.BREAKMarker) n = find+1; else n = find-1; // BREAK jumps PAST the BACKIF, CONT to incrementer JUST BEFORE the BACKIF.
            fnline[i].Next = Convert.ToInt16(n); 
          }            
        }
      }

      // Prevent the final .Next from pointing to a nonexistent next bubble:
      if (fnline[bubno-1].Next >= 0)// e.g. ignore a final imbedded RETURNNext
      { fnline[bubno-1].Next = Convert.ToInt16(R.EXITNext); } // -100. This is
                  // the implicit "exit" at the end of main program text. (It
                  // also goes at the end of user function text; but if it is
                  // ever reached, there will be a nasty error message.)
      // Note that while this

    // LAY DOWN fnline[] INTO FlowLine[]:
      R.FlowLine[fn] = new TBubble[bubno];
      for (int i = 0; i < bubno; i++) R.FlowLine[fn][i] = fnline[i];
                            // no dynamic fields, so assmt. should work.
    }//END of FOR loop through FUNCTIONS
Quad quoo = TestFlowLineForOrphanedConditionals(); // ######################################## put elsewhere
if (!quoo.B) return quoo;//###################
    result.B = true;  return result;
  }

// The following test, to detect esp. "a == b" when not as part of an IF/WHILE condition, was added
//  after a day's worth of experimentation with neuronal circuitry had to be repeated because of one
//  such line, which did not raise any error message.
// Returned: the usual error pair .B and .S; and .I holds the paragraph, if .B false.
  public static Quad TestFlowLineForOrphanedConditionals()
  { Quad result = new Quad(false);   result.I = -1;
    int ParCnt,  Iffy = R.IFMarker;
    for (int fn = 0; fn < C.UserFnCnt; fn++)
    { ParCnt = P.UserFn[fn].LFBeforeOpener;
      TBubble[] flowline = R.FlowLine[fn]; 
      bool ignore = false;
      for (int bubno = 0; bubno < flowline.Length; bubno++)
      { if (ignore) { ignore = false; continue; } // only ignore for one loop after this bool was set.
        int ass = (int) flowline[bubno].Ass;
        if (ass == Iffy) { ignore = true; continue; } // ignore the valid conditional assmt. bubble that follows.
        else if (ass < 0) continue; // opcodes and whatnot - anything that ain't a valid asst.
        // This bubble is attached to a valid assignment, so check its opcode:
        if (R.Assts[ass][1].Hier != 4) continue; // we are only interested in [1], the second term of "__D_ = x == ..." 
        // This bubble is assoc'd with a conditional assignment. If the first term assigns to the general dummy, then this is illegal,
        //   unless this bubble is the RETURN bubble.
        if (R.Assts[ass][0].At == Convert.ToInt16(GeneralDummies[fn]) &&  (int) flowline[bubno].Next != R.RETURNNext)
        { result.S = OpCdTransln[R.Assts[ass][1].OpCode];
          result.S = "Isolated conditional statement with '" + result.S + "' somewhere in ";
          if (fn == 0) result.S += "the main program"; else result.S += "this function";
          result.I = ParCnt;  return result;
        }
      }
    }
    result.B = true;  return result;
  }


  /// <summary>
  /// A utility for the systme function "solveexp(.)" as well as for the assigning of optional arguments.
  /// Expression has all spaces, tabs and par. marks stripped from it; after that, it mus consist ONLY
  ///   of three element types: (1) value references, which may only be literal numbers, constants or (IF
  ///   argument "AllowMainPgmScalars" is true) MAIN program SCALAR variables; (2) operation signs, the 5 allowed
  ///   ones being in the set " + - + * / ^ " (the last being the power index sign); and
  ///  (3) matched brackets '(', ')', with any level of nesting.
  /// The RETURN is material which function JS.SolveExpression(.) can use to solve the expression. It is a string
  ///   in the form of (1) value references only in the form of "{n}" - where n is a literal integer, referring
  ///   to some element of ValueDataBase; (2) and (3) as above (as mentioned, no spaces etc. will be present).
  /// If there is an ERROR, "" is returned, and the ErrMsg is appropriately filled (it being "" if no error);
  ///   and ValueDataBase is NULL.
  /// Hierarchy: at any one bracket nesting level, operations are processed in the reverse order of their
  ///   occurrence in the above string ("^" first, "+" last).
  /// </summary>
  public static string ParseExpession(string Expression, out double[] ValueDataBase, out string ErrMsg, bool AllowMainPgmScalars)
  { ErrMsg = "";  ValueDataBase = null;
    char dumdum = '\u0000';
    char[] expo = (Expression + dumdum).ToCharArray(); // add a dummy at the end
    int expoLen = expo.Length;
    bool inaword = false, isavaluechar = false;
    string theword = "";
    string nonvaluechars = "0+-*" + "/^()" + dumdum; // By default, any non-space character not there will be regarded for better or worse as a value char.
    StringBuilder sb = new StringBuilder();
    List<double> valueLst = new List<double>();
    int ndx = 0;
    for (int i=0; i < expoLen; i++)
    { char ch = expo[i];
      if (ch == ' ' || ch == '\t' || ch == '\n') continue; // We ignore these white spaces
      isavaluechar = (nonvaluechars.IndexOf(ch) == -1);
      if (isavaluechar)
      { if (!inaword) { inaword = true;  theword = ch.ToString(); }
        else theword += ch;
      }
      else // a nonvalue char.
      { if (inaword)
        { inaword = false;
          // Is it a variable name?
          int At = V.FindVar(0, theword); // *** Later, allow for local user fn. variables.
          double x;
          if (At >= 0)
          { byte varuse = V.GetVarUse(0, At);
            if (varuse != 1)
            { if (!AllowMainPgmScalars)
              { ErrMsg = "identifier '" + theword + "' is not the name of a constant";  return ""; }
              else if (varuse != 3)
              { ErrMsg = "indentifier '" + theword + "' is not the name of a constant or main program variable";  return ""; }
            }
            x = V.GetVarValue(0, At);
          }
          else // not a constant or main pgm. variable; so is either a literal value or an error.
          { bool success;
            x = theword._ParseDouble(out success);
            if (!success)
            { ErrMsg = "string '" + theword + "' is not a main program variable or constant or a literal number";  return ""; }
          }
          sb.Append("{" + ndx.ToString() + "}");   ndx++;
          valueLst.Add(x);
        }
        if (ch != dumdum) sb.Append(ch);
      }
    }
    if (valueLst.Count == 0) { ErrMsg = "no values found in the expression";  return ""; }
    ValueDataBase = valueLst.ToArray();
    return sb.ToString();
  }

} // END OF CLASS Parse
} // END OF NAMESPACE MonoMaths


