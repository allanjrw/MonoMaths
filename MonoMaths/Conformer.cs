using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using JLib;
//==============================================================================

namespace MonoMaths
{// THE CONFORMER for MiniMaths  - conforms user program code, with its range
 //  of allowed syntax forms, to a set form recognizable by the Parser.
public class C
{
	private C(){} // Sorry, you can't instantiate this class.

//==============================================================================
// FIELDS AND PROPERTIES:
//------------------------------------------------------------------------------
  public static bool IsFirstUse; // i.e. first use since MiniMaths started up.
  // Set in MainForm (first pass of OnActivate handler); reset in Conform() here.
  public static int UserFnCnt = 0; // Always set to >= 1, function '0' being the main program.
  public static Duo[] FnExtremes;//fns. to be deleted between & incl. these ptrs.
  public static string Space_Par = " " + '\n'; // Used in sequences looking for the next char. which is not one of these.

// TOKEN-RELATED FIELDS:
 //------------vvvvvvvvvvvvvvvv----------------------
 // Constants to use with CH:
  public readonly static int
   // IF ADDING MORE, adjust CHcnt, CHkwds, SetUpCHname().
   // First group are keywords, to be replaced in conforming the code by fn. ImplantTokens(.); it is done
   //  halfway through, under the heading: "DO THE KEYWORD SUBSTITUTIONS:". All from CH[0] to CH[CHkwds-1]
   //  are replaced in a loop uncritically; hence the importance of 'CHkwds' being right.
   // *** KEEP THE FIRST ONES as conditional tests, followed by CHequal. I use this feature in code (e.g. in the SIFT handler).
    CHeq_eq    = 0,    
    CHeq_gr    = 1, 
    CHeq_less  = 2, 
    CHnot_eq   = 3, 
    CHgreater  = 4, 
    CHless     = 5,      
    CHequal    = 6, // MAKE THIS THE LAST EQUALITY/COND. SIGN, AS IT WILL BE USED AS A POINTER by ImplantTokens(). These first tokens
                    //  are not directly replaced in a loop; only the whole-word keywords below, from CHequal+1 to CHkwds-1,
                    //  are so replaced.  
    CHand      = 7,
    CHor       = 8,
    CHxor      = 9, // CHnot is missing here - it occurs in the NON-keyword section. Replacement of '!' is not done in ImplantTokens(.),
                    //  and 'not' is separately reserved as a system function name.
    CHif       = 10, // Corresponds to user-code IF, but such CH[CHif] codes will finally become CH[CHiffinal] codes.
    CHelseif   = 11,
    CHelse     = 12,      
    CHfor      = 13,       
    CHwhile    = 14, 
    CHdo       = 15,        
    CHforeach  = 16,
    CHsift     = 17,
    CHcontinue = 18, 
    CHbreak    = 19, 
    CHreturn   = 20, 
    CHexit     = 21, 
    CHfunction = 22, 
    CHref      = 23, 
    CHpopulate = 24, 
    CHthrough  = 25, 
    CHusing    = 26,
    CHlistopener = 27,
    CHlistcloser = 28,

// *** The following is the COUNT OF ALL VALUES ABOVE THIS POINT:
    CHkwds = 29, // It must be one more than the last value above.    
// The rest are NOT for keyword substitution.
    CHiffinal     = CHkwds,   // This and next two occur after conforming FOR, WHILE and DO loops, and after processing IF statements.
    CHendif       = CHkwds+1, 
    CHbackif      = CHkwds+2, 
    CHthen        = CHkwds+3, 
    CHelsefinal   = CHkwds+4, 
    CHnegator     = CHkwds+5,
    CHnot         = CHkwds+6,
    CHassopener   = CHkwds+7, 
    CHasscloser   = CHkwds+8, 
    CHfnopener    = CHkwds+9,  
    CHfncloser    = CHkwds+10,
    CHdummymarker = CHkwds+11, // flowline uses to build a dummy assignment.
    CHbkmark      = CHkwds+12, //for local short-term use. It MUST BE LAST, as fns. are free to use it + HIGHER VALUES for local use.
// *** The following is the COUNT OF ALL VALUES ABOVE, and will be used to size CH[] and CHname[]. 
   CHcnt = CHbkmark+1; // MUST BE one more than the last value above.
//------------^^^^^^^^^^^^^^^^----------------------
 
  // The following are all assigned in SetUpCHname() below:
  public static char AssOpener, AssCloser, Negator, Complementer;
  public static string AssOpStr, AssCloStr; 
  private static string AssignmentChars; // what can be inside a parsed assignment.
  private static string FlowBreakers; // string of CH[] codes for continue, break, return, exit. *** DON'T TRY TO GIVE IT
                                      //    A VALUE HERE, or you will get a very-hard-to-trace error. 
  private static string SpaceParAss;  // Space, '\n',AssOpener, AssCloser. *** As above - Don't try to assign it here!
  public readonly static char CHBASE = '\uf000'; // chars.e000 to f8ff are classified in .NET as 'for private use', 
         // so there should be no risk of their occurring fortuitously in the user's code. We have chosen F000 
         // (dec. 61440) rather than E000, so that it is safe to use 'JS.FirstPrivateChar' (E000) for local purposes.
  public readonly static int CHBASE_Int = Convert.ToInt32(CHBASE);
  public static char[] CH;// Token chars. to replace keywords. NB: Values
  public static void SetUpCH() // Called from C.Conform() once only, at 1st. use.
  { CH = new char[CHcnt];
    char ch = CHBASE;
    for (int i = 0; i < CHcnt; i++) {CH[i] = ch;  ch++; }
  }
  public static string [] CHname = new string[CHcnt]; // MUST have the same length as CH[], or crasho.
  public static char[] CompCodes; // Used in SetUpCHname() below. Will be CH[..] versions of '<', '<=' etc.,
      // but NOT including '=', which is an assignment code, not a comparator code.
  public static void SetUpCHname()//Called from C.Conform() once only, at 1st. use. These names are only used for
                                  // displaying diagnostic messages.
  { 
   // First group are keywords, to be replaced in conforming the code by fn. ImplantTokens(.); it is done
   //  halfway through, under the heading: "DO THE KEYWORD SUBSTITUTIONS:". All from CH[0] to CH[CHkwds-1]
   //  are replaced in a loop uncritically; hence the importance of 'CHkwds' being right, and of the following beingL
   //  literally what will occur in the user's code until CHkwds-1. (After that, CHname[.] only used for diagnostic displays.)
   // Note that some include spaces. These are for clearer diagnostic messages.  
   // *** KEEP THE FIRST ONES as conditional tests, followed by CHequal. I use this feature in code (e.g. in the SIFT handler).
    CHname[CHeq_eq] = " == ";
    CHname[CHeq_gr] = " >= ";
    CHname[CHeq_less] = " <= ";
    CHname[CHnot_eq] = " != ";
    CHname[CHgreater] = " > ";
    CHname[CHless] = " < ";          
    CHname[CHequal] = " = "; // These first tokens are not directly replaced in a loop; only the whole-word keywords below
                             //  are so replaced. Hence the spaces, which make their other use - display in diagnostic messages -
                             //  more user-friendly.
    CHname[CHand] = "__and"; // As of June 2010, 'and', 'or' and 'xor' have been reassigned as system function names; users 
    CHname[CHor] = "__or";   //  can only code with the standard abbreviations '&&', '||' and '^^' from now on.
    CHname[CHxor]  = "__xor"; // CHnot is handled AFTER the keyword section, as user's legal 'not' is for the system fn. 'not(.)'.
    CHname[CHif] = "if"; // User's 'if' will eventually be replaced by CH[CHiffinal].            
    CHname[CHelseif] = "elseif";  // User's 'if' will eventually be replaced using CH[CHelsefinal] and CH[CHiffinal].                
    CHname[CHelse] = "else"; // User's 'else' will eventually be replaced by CH[CHelsefinal].             
    CHname[CHfor] = "for";           
    CHname[CHwhile] = "while";
    CHname[CHdo] = "do";             
    CHname[CHforeach] = "foreach";             
    CHname[CHsift] = "sift";             
    CHname[CHcontinue] = "continue";
    CHname[CHbreak] = "break";
    CHname[CHreturn] = "return";
    CHname[CHexit] = "exit";
    CHname[CHfunction] = "function";
    CHname[CHref] = "ref";
    CHname[CHpopulate] = "populate";
    CHname[CHthrough] = "through";
    CHname[CHusing] = "using";
    CHname[CHlistopener] = "<<";
    CHname[CHlistcloser] = ">>";
  // Non-keyword members (i.e. those NOT DIRECTLY REPLACING words and symbols found in user's code, so can be fiddled with for visual perfection).
    CHname[CHiffinal] = "if";
    CHname[CHendif] = "endif";
    CHname[CHbackif] = "backif";
    CHname[CHthen] = "then";
    CHname[CHelsefinal] = "else";
    CHname[CHassopener] = "\u2560"; // symbol to be displayed for diagnostic work by e.g. fn. ShowIt(.)
    CHname[CHfnopener]    = "FnOpr";   
    CHname[CHnegator]     = "neg";     
    CHname[CHdummymarker] = "dumbo";
    CHname[CHasscloser] = "\u2563"; // symbol to be displayed for diagnostic work by e.g. fn. ShowIt(.)
    CHname[CHfncloser]  = "FnClr";
    CHname[CHbkmark]    = "bkmark";
    CHname[CHnot]       = "!";

    // Set up non-keyword elements:
    AssOpener = CH[CHassopener];   AssOpStr = AssOpener.ToString();
    AssCloser = CH[CHasscloser];   AssCloStr = AssCloser.ToString();
    Negator = CH[CHnegator];       
    Complementer = CH[CHnot];      

    // Define CompCodes (which does NOT contain code for assignment '='):
    CompCodes = new char[] { CH[CHeq_eq], CH[CHeq_gr], CH[CHgreater],
                             CH[CHeq_less], CH[CHless], CH[CHnot_eq] };

    FlowBreakers = " " + CH[CHreturn] + CH[CHcontinue]+ CH[CHbreak] + CH[CHexit]; // NB! Keep 'return' first on list - posn. used later.
    FlowBreakers = FlowBreakers.Remove(0, 1); // If you know a more elegant way of turning chars. into a string, bully for you.
    SpaceParAss = Space_Par + CH[CHassopener].ToString() + CH[CHasscloser].ToString();
  }
  // Internal codes using '__':
  public readonly static string ArrayFnRHS = "__segmt", ArrayFnLHS = "__assign";
  // "Arr[2]=<exp.>" --> "__assign(Arr,2, <exp.>)";
    //   elsewhere, "Arr[2] --> "__segmt(Arr,2)".
  public readonly static string GeneralDummyVarName = "__D";
  public readonly static string NumberedDummyVarName = "__d";
  public readonly static string NoShowDummyVarName = "__e"; // Certain void fns. shouldn't be shown in REditRes - 
    // e.g. 'inc(a);'. These get this prefix.
    // You will find the void fns' names in C.InsertDummyVariables(.).
  public static int NextSysVarNo = 0; // Used to generate successive nonambiguous 
   // names for system variables. NB: '__A..' is reserved for ARRAYS;
   // all other letters after '__' are parsed as being scalar (incl. '__a..').
  public static int NextDummyNo; // initialized to 1 in Conform(.). Generates
   // successive dummy variables, from (NumberedDummyVarName + "1") upwards.
  public static int NoShowNextDummyNo; // initialized in Conform(.).
 // The following two aliasses are used in C.ReplaceQuotes(); can't use "__quote()" or "__constant(" there, as "__" test comes after it.
  public static string QuoteFnAlias = JS.UnlikelyStr + "quote(";
  public static string ConstantFnAlias = JS.UnlikelyStr + "constant(";
  public static string OptArgFnAlias = JS.UnlikelyStr + "optarg(";

//==============================================================================
// STATIC CONSTRUCTOR OF CLASS C[onformer]:
//------------------------------------------------------------------------------
  static C ()
  {
    SetUpCH();  SetUpCHname();  P.SetUpOpCHcodes();
  }

//==============================================================================
// STATIC METHODS OF CLASS C[onformer]:
//------------------------------------------------------------------------------
// MAIN METHOD CALLED FROM OUTSIDE:  //conf   //8

/// <summary>
/// <para>"StrArr" is plain user-input text after all remarks have been removed; that text has been converted to a string array,
///   using the LF character (unicode 10) as the delimiter.</para>
/// <para>RETURNED if NO ERRORS: .B = TRUE;  .S = empty string;  .I == 0;   .X = 0.0.  Note that the processed text is not returned;
///   by the end of the method it has instead been broken down into function texts and implanted into "P.UserFn[≪function no.≫].Text".</para>
/// <para>RETURNED if ERROR: .B = false;   .S = error message;   .I is the paragraph no. of the original text (0 for the first paragraph);
///   .X = 0.0, except in the case of bracket checks, where .X holds a code which prompts for a particular subsequent dialog box.</para>
/// </summary>
  public static Quad Conform(string[] StrArr)
  { int n;  string ss;
    // Check for the instruction "CUT", which in effect remarks out all further code. Case-sensitive, no blanks.
    for (int i=0; i < StrArr.Length; i++)
    { if (StrArr[i] == "CUT")
      { bool silently = (i < StrArr.Length-1  &&  StrArr[i+1]._Extent(0, 4) == "====");
        string[] newStrArr = new string[i];
        Array.Copy(StrArr, newStrArr, i);
        StrArr = newStrArr;
        if (!silently) MainWindow.ThisWindow.AddTextToREditRes("** PROGRAM CODE IGNORED AFTER THE LINE \"CUT\" **\n", "start");
      }
    }
    // CHECK FOR DIRECTIVES which must be serviced before preparsing gets under way:
    // Check for the preprocessing instruction "ALLOW", to allow extra chars. in identifiers; it must be the first word in the first
    //   nonempty string of StrArr:
    ProcessDirectiveALLOW(ref StrArr, false); // This updates P.IdentifierChars and P.Identifier1stChar, if any found.
          // (It also returns the extra characters, but we don't need them here.)
          // StrArr will return with the ALLOW line replaced with a blank line.
    ProcessDirectiveCONSTANT(ref StrArr); // This replaces any line starting with CONSTANT with a line invoking
          // the hidden system function "__constant(.)". "this_line" returns either as the line in StrArr of __constant('),
          // or, if none, as -1.
    // Define AssignmentChars: (Can't do this at declaration, because P.IdentifierChars is not then set.)
    AssignmentChars = P.IdentifierChars + "()[]+-#^*/!,.:"  + '\n' +
      CH[CHand] + CH[CHor] + CH[CHxor] + CH[CHeq_eq] + CH[CHnot_eq] +
      CH[CHeq_gr] + CH[CHeq_less] + CH[CHgreater] + CH[CHless] + CH[CHequal] + CH[CHlistopener] + CH[CHlistcloser];
    // Convert the string array to one great string:
      string PgmText = String.Join("\n", StrArr); // however the rich edit imported
    // text ended its pars. (which may depend on the loaded file), we now have
    // only the LF character '\n' between them.
    // PROCESS QUOTATIONS: e.g.  Arr = data("ABC");   -->  Arr = data(65,66,67);
    // (Substitution occurs uncritically anywhere in PgmText, but currently onlyt
    //  would have meaning inside the data(..) system function.)
    Quad quo = ReplaceQuotes(ref PgmText);
    if (!quo.B) return quo;
  // ENSURE NO USER-SUPPLIED DOUBLE-UNDERLINES:
    n = PgmText.IndexOf("__");
    if (n != -1) { return new Quad(LFsToHere(ref PgmText, n), 0.0, false, "forbidden sequence '__'"); }
  // Replace aliassed functions alias with the real function, now that the above test has been circumvented:
    PgmText = PgmText.Replace(QuoteFnAlias, "__quote(");
    PgmText = PgmText.Replace(ConstantFnAlias, "__constant(");
  // CHECK BRACKET NESTING - { },  [ ],  ( ):
    ss = "";    
    if (!JS.BracketsCheck(ref PgmText, '(', ')').B)
    { return new Quad( -1,  10.0,  false, "somewhere brackets '(', ')' don't match"); } 
    if (!JS.BracketsCheck(ref PgmText, '[', ']').B)
    { return new Quad( -1,   0.0,  false, "somewhere brackets '[', ']' don't match"); } 
    if (!JS.BracketsCheck(ref PgmText, '{', '}').B)
    { return new Quad( -1,  20.0,  false, "somewhere brackets '{', '}' don't match"); } 

    // REMOVE RUNS OF SPACES, but LEAVE SINGLE SPACES (till removed further down):
    PgmText = JS.DetectRunsOf(PgmText, " ", out ss);
    // REPLACE KEYWORDS WITH TOKENS: Note that this does not replace math. signs (+,-, etc.), but does replace
    //  '=' and comparators and logic symbols and flow directives.
    quo = ImplantTokens(ref PgmText);
    if (!quo.B) return quo; // *** Currently ImplantTokens never returns an error, so this line is never used.

  //**** REMOVE SPACES:
    PgmText = PgmText._Purge(new char[]{' '});
    // IF ANY EMPTY BRACES "{\n\n..}" (0 or more '\n'), REPLACE WITH "{__D = 0;\n\n..}" (leaving the \n)
    int[] closers = PgmText._IndexesOf('}');
    if (closers != null)
    {
      char[] pgmTextChars = PgmText.ToCharArray();
      for (int j = closers.Length-1; j >= 0 ; j--)
      { bool onlyLFfound = true;
        int ptr = closers[j]-1;
        char c = ' ';
        while (ptr >= 0) // test never false, as every '}' will be matched by an earlier '{', and so a break from the loop occurs before ptr --> -1.
        { c = pgmTextChars[ptr];
          if (c == '{') break;
          if (c > ' ') { onlyLFfound = false; break; } // OK, if it is another nested '}'.
          ptr--;
        }
        if (onlyLFfound)
        { PgmText = PgmText.Insert(ptr+1, GeneralDummyVarName + CH[CHequal].ToString() + "0;");
        }
      }
    }
  //-----------------------------------------
  // SEPARATE OUT THE FUNCTIONS:
  //-----------------------------------------
    quo = SetUpFunctions(ref PgmText); // This is the end of the line for processing the ref variable PgmText.
    if (!quo.B) return quo;
    PgmText = ""; // ready for garbage collection.
    NextDummyNo = 1; // starting no. (not '0', as no. will be used for user display)
    NoShowNextDummyNo = 0;
  //--------------------------------------------
  // WORK THROUGH THE SEPARATE FUNCTION TEXTS:
  //--------------------------------------------
    for (int fncnt = 0; fncnt < UserFnCnt; fncnt++)
    { // DELINEATE ASSIGNMENTS and REMOVE SEMICOLONS in the process:
      string FnText = DelineateAssignments(ref P.UserFn[fncnt].Text); // This fn. does not have an error return.
          // *** The following check has been remarked out, as the code of the above should guarantee its success;
          //       if significantly altering DelineateAssignments(.), reinstate it:
          // Quad qq = JS.BracketsCheck(ref FnText, AssOpener, AssCloser);
          // if (!qq.B) { qq.S = "Programming error with delineating assignments: " + qq.S;  return qq; };
      // REPLACE SHORTHANDS: x++;  and x +=, x *=, ... 
      //   (Note that array square brackets - "aa[0]" - are only removed in unit Parser ("ConvertArrayRefs(.)");
      //    the code in the following functions assumes that the square brackets are not yet removed.
      quo = ReplaceShorthand(ref FnText);
      if (!quo.B)
      { quo.I += P.UserFn[fncnt].LFBeforeOpener;   return quo; }
    // Deal with statements of the type "<<var1, var2, ...>> = ":
      quo = ConvertLHSScalarListings(ref FnText);
      if (!quo.B)
      { quo.I += P.UserFn[fncnt].LFBeforeOpener;   return quo; }
//------------------------------------------------------------------------------
    // CONVERT VARIOUS LOOP SYNTAXES TO A SINGLE 'IF'-TYPE SYNTAX:
      quo = Conform_POPULATE_stmts(ref FnText); // must precede FOR handler, as translates populate sequences into FOR loops.
      if (quo.B) quo = Conform_FOR_Loops(ref FnText);
      if (quo.B) quo = Conform_FOREACH_Loops(ref FnText);
      if (quo.B) quo = Conform_DO_Loops(ref FnText); // Must precede Conform_WHILE_Loops(.)
      if (quo.B) quo = Conform_WHILE_Loops(ref FnText);
      if (quo.B) quo = Conform_SIFT_Segments(ref FnText);
      if (quo.B) quo = Conform_IF_Segments(ref FnText);
       // Check after all the loop-syntax conformers:
      if (!quo.B)
      { if (quo.I > -1) quo.I = P.UserFn[fncnt].LFBeforeOpener + quo.I;  return quo;  }
      // There shouldn't be any curly brackets left. Complain, if any found.
      n = FnText.IndexOf('{'); // No 2nd. check for '}', as matching earlier tested.
      if (n != -1)
      { var quaddle = new Quad(P.UserFn[fncnt].LFBeforeOpener + LFsToHere(ref FnText, n),  0.0,  false,  "inappropriate \"{ ... }\" passage found "
          + "(Typical scenarios: omitting the word 'function' "
          + "when defining a user function; or misspelling a keyword - e.g. \"If\" for \"if\"; or colon error in 'sift' block)");
        return quaddle;
      }
    // INSERT DUMMY VARIABLES in assignments with no '=' or comparators:
    //  (Can't go earlier, or it stuffs up e.g. FOR loop handling)
      InsertDummyVariables(ref FnText); // (expressions with comparators do get a dummy variable but not here.)

    // All finished, so assign the butchered text to P.UserFn[]:
      P.UserFn[fncnt].Text = FnText;   FnText = ""; // for garb. collection

    }// END OF looping through function texts

    return new Quad(true); // Success.
  }
//==============================================================================
  // Convert all LHS statements "<<var1, var2, ... >> = RHS" to "__D = unpack(RHS, var1, var2, ...)"; REF arg is altered.
  // Error: .B FALSE, .S message, .I par. in rich edit to select (after you add the fn. offset to it, in calling code).
  //  (Testing is NOT exhaustive. Some errors rely on detection at parse time.)
  public static Quad ConvertLHSScalarListings(ref string InStr) // InStr is the text of this function
  { int pOpen = 0, pClos;
    char opener = C.CH[CHlistopener], closer = C.CH[CHlistcloser], equals = C.CH[CHequal];
    // By now, "<<A,b>> = RHS" is translated to "{ << a,b >> = RHS }" (where '{' = asst. opener, etc. - there can be spaces anywhere.
    // We want it to be:  "{ unpack( RHS, a,b ) }".
    while (true)
    { pOpen = InStr.IndexOf(opener, pOpen);  if (pOpen == -1) break;
      pClos = InStr.IndexOf(closer, pOpen);  if (pClos == -1) break; // Leave it to ensuing code to deal with orphan "<<".
      string varNames = InStr._Between(pOpen, pClos);
      if (!C.LegalName(varNames._Purge(',', ' ')))
      { return new Quad(LFsToHere(ref InStr, pOpen), 0.0, false, "illegal chars. within brackets '<<  >>'"); }
      // Now we want the stuff between "equals" and AssCloser:
      int ass_clos = InStr.IndexOf(AssCloser, pClos);
      if (ass_clos == -1) return new Quad(LFsToHere(ref InStr, pOpen), 0.0, false, "no sensible text after '<<  >>'"); 
      int eq = InStr.IndexOf(equals, pClos);
      if (eq > ass_clos || eq == -1) return new Quad(LFsToHere(ref InStr, pOpen), 0.0, false, "no '=' after '<<  >>'"); 
      string rhs = InStr._Between(eq, ass_clos);
      // So set up the final text:
      string ss = "unpack(" + rhs + "," + varNames + ")";
      InStr = InStr._ScoopTo(pOpen, ass_clos-1, ss);
    }
  return new Quad(0, 0.0, true, "");  
  }
//---------------------------------------------------------
/// <summary>
/// This method always resets P.IdentifierChars and P.Identifier1stChar, whether special chars. found or not.
/// Searches 'StrArr' for a line beginning with 'ALLOW', which must occur before any other nonempty, non-remark text
///  i.e. before any valid program code. The method RETURNS the string of added chars, if any, otherwise the empty string.
/// 'RemarksMayBePresent': if FALSE, then time is saved by not looking for lines that begin with remarks.
///  FATE OF StrArr: Although a REF arg., the only changes made to it here is the removal of any valid 'ALLOW' line.
/// </summary>
  public static string ProcessDirectiveALLOW(ref string[] StrArr, bool RemarksMayBePresent)
  { string newguys = "", thisline = "";
    if (RemarksMayBePresent) MainWindow.ThisWindow.ExtractRemarks(ref StrArr);
    for (int i=0; i < StrArr.Length; i++) // in fact we stop after the first nonempty line.
    { thisline = StrArr[i].TrimStart();
      if (thisline == "") continue;
      if (thisline._Extent(0, 5) == "ALLOW")
      { // Look for key word "GREEK", which would cause all Greek letters, capital and small, to be included.
        if (thisline.IndexOf("GREEK") > 0)
        { newguys = "αβγδεζηθικλμνξοπρσςτυφχψωΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ"; }
        // Trawl through the line looking for special characters:
        char[] foo = thisline._Extent(6).ToCharArray(); // the rest of the line, after the char. after 'ALLOW'.
        for (int j=0; j < foo.Length; j++)  { if (foo[j] >= '\u0080')  newguys += foo[j]; }
        // remove the whole line's text from the input REF argument:
        StrArr[i] = "";
      }
      else break; // some other code found.
    }
    P.Identifier1stChar = JS.Identifier1stChar + newguys;
    P.IdentifierChars = P.Identifier1stChar + JS.Digits;
    return newguys;
  }
/// <summary>
/// This method searches 'StrArr' for a line beginning with 'CONSTANT', which must occur before any other nonempty,
///   non-remark text; i.e. before any valid program code (including the directive 'ALLOW', which must precede this one).
/// NB! Remarks must have been removed by the time of this call (though completely empty lines (no spaces) are tolerated
///   (such a line would have been created by the above method handling 'allow', if present.)
///  FATE OF StrArr: Although a REF arg., the only changes made to it here is the replacement of the CONSTANT line, if found,
///  with a format invoking the hidden system function "__constant(.)".
/// RETURNED: The index in StrArr of the constants line; or, if none found, -1.
/// </summary>
  public static int ProcessDirectiveCONSTANT(ref string[] StrArr)
  { string thisline = "";
    int result = -1;
    for (int i=0; i < StrArr.Length; i++) // in fact we stop after the first nonempty line.
    { thisline = StrArr[i].TrimStart();
      if (thisline == "") continue;
      if (thisline._Extent(0, 8) == "CONSTANT")
      { string stroo = thisline._Extent(9); // the rest of the line, after the char. after 'CONSTANT'. Includes spaces etc.
        stroo = stroo.Replace(';', ','); // would look after a terminal ';' in particular.
        string replacemt = "";
        string[] stroodle = stroo.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
        for (int k = 0; k < stroodle.Length; k++) // Note - no parsing checks here; fn. "__constant" will have to do this job.
        { int p = stroodle[k].IndexOf('=');  if (p == -1) continue; // No error raised, if no '='; text simply ignored.
          replacemt += ConstantFnAlias + stroodle[k]._Extent(0, p) + "," + stroodle[k]._Extent(p+1) + ");";
        }
        StrArr[i] = replacemt; // replace the line in the REF argument.
        result = i;
      }
      break; // either the directive line has been dealt with, or some other code found
    }
    return result;
  }

/// <summary>
/// Fine the number of occurrences of '\n' up to (and including) InStr[ptr].
/// </summary>
  public static int LFsToHere(ref string InStr, int ptr)
  { return InStr._CountChar('\n', 0, ptr);
  }
//rq----//quo-------------------------------------------------------------------
// Look for the first of either " or '. Then expect in the SAME LINE to find
//  another of the same; --> error if none. All between --> numbers, with
//  special exceptions: \n (LF), \t (TAB), , \", \' . (The latter is unnec. for
//  whatever in this quote is not the valid quotn. mark.) In all other cases,
//  the \ is simply ignored and omitted. Result: e.g. for FnName = "boo",
//  input "A's" or 'A\'s' --> boo(65,96,115).
// One other allowance: '' is replaced by ', and "" by ", anywhere in Txt.
// RETURNED: Error pair .B and .S. If error, position in .I. If any replacement
//  at all occurred, .X is 1.0; otherwise it is 0.
//chn  //suchn

/// <summary>
/// Given a CH keyword code, return the corresponding keyword, or "??" if none found. (E.g. all printable
/// characters return this. Calling code has the job of filtering all non-CH[] characters.)
/// </summary>
  private static string CHtransln (char CHcode)
  { if (CHcode >= CH[0] && CHcode <= CH[CHcnt-1])
    { int index = Convert.ToInt32(CHcode);
      index -= CHBASE_Int;
      return CHname[index];
    }
    else return "??";
  }
  
//leg  //ln  //nam
/// <summary>
/// Validate that a string could constitute a legal identifier: i.e. start with an English letter or '_', and contain only these and numerals.
/// Returns TRUE if validated.
/// </summary>
  public static bool LegalName(string InStr)
  { if (InStr == "")return false;
    if(JS.CharsNotInList(InStr, P.IdentifierChars.ToCharArray()) > 0 ||
          JS.Digits.IndexOf(InStr[0]) != -1) return false;
    else return true;
  }

// Finds the first assignment, and returns its analysis.
// NB! Only works after all assignments have been enclosed between AssOpener
//  and AssCloser; after Keyword substitution by CH[] members; and spaces removed.
// It DOES permit LFs in assignments.
// RETURNED:
//  .BX - TRUE if an assignment found (before any checks).
//  .BY - TRUE if asst. found (.BX true) AND no errors detected (very rudimentary
//          checking only; see below.) (Empty assignment not an error; you have to check .SZ to detect this.)
//  .BZ - TRUE if 1 CH[CHequal] found ('=' as assignment, not as part of comparator).
//  .XX - no. of '=' of assignment found. (.BZ is only true if exactly 1 present.)
//  .XY - If .BZ TRUE (just one '='), gives locn. in InStr (not the assignmt) of '='.
//  .IX - AssOpener pointer, -1 if none found, -2 if error.
//  .IY - AssCloser pointer, or -1 if none.
//  .IZ - no. of comparators found. (Does not include any '=' of assignment.)
//  .SX - LHS: Everything before '='; or "", if no '='. (AssOpener excluded.)
//          If error, holds the error message.
//  .SY - RHS: Everything after '='; or the whole InStr, if no '='. (No AssCloser.)
//  .SZ - All of InStr lying between AssOpener and AssCloser.
// ERRORS:
//  .BY is FALSE (.BX may be true or false), and .SX holds the error message.
//  Errors detected are few: (1) No AssCloser, or AssCloser before AssOpener;
//  (2) More than one '=' of assignment. (Any no. of comparators is allowed.)
//  (3) Silly arguments. (You can omit one or both of FromTo, to get defaults -
//       start / end of InStr; but you can't put an under-/oversized value in.)
  public static Twelver FindNextAsst(ref string InStr, params int[] FromTo)
  { Twelver result = new Twelver(0);  result.IY = -1;
    int nn, pp, FromPtr, ToPtr;
    if (FromTo.Length > 1) ToPtr = FromTo[1]; else ToPtr = InStr.Length-1;
    if (FromTo.Length > 0) FromPtr = FromTo[0]; else FromPtr = 0;
    if (FromPtr < 0 || ToPtr >= InStr.Length)
    { result.SX="Program error - faulty parameters"; result.IX=-2; return result;}
    int pope = InStr._IndexOf(AssOpener, FromPtr);
    int pclo = InStr._IndexOf(AssCloser, FromPtr+1);//safe, if beyond end of InStr.
    // Check for errors and for no find:
    if (pope==pclo){result.IX = -1; return result;} // no find, but no error.
    if (pope<0 || pclo<0 || pclo<pope)// silly bracketting:
    { result.SX = "Program error - no asst. closer"; result.IX=-2; return result;}
    // Properly bracketted assignment found.
    result.BX = true;  result.IX = pope;  result.IY = pclo;
    result.SZ = InStr._FromTo(pope+1, pclo-1);
    // Count '=', locate the first.
    nn = result.SZ._CountChar(CH[CHequal]);
    result.XX = (double) nn;
    if (nn==0 || nn>1) // either 0 or 2+. If 2 or more, this MAY be legitimate:
      // a conditional statement with the user writing '=' instead of '=='.
      // If so, the whole asst. should still go to .SY. (If two '=' is
      // considered by calling code to be an error, .SY will be irrelevant.)
    { result.SY = result.SZ; }
    else // one and only one '=' present:
    { pp = result.SZ.IndexOf(CH[CHequal]);
      result.BZ = true;  result.XY = (double) (pope + pp + 1);
      result.SX = result.SZ._Extent(0, pp);
      result.SY = result.SZ._Extent(pp+1);
    }
    // It just remains to count comparators...
    result.IZ = result.SZ._CountChar(CompCodes);
    result.BY = true; return result;
  }
// REPLACE QUOTES. The first type of quote mark (single or double inv. commas)
//  met with in a search loop is taken as the official quote mark; the opposite
//  type, if occurring inside, is treated like any other character.
// Replacement: "ABC" is replaced by: 65, 66, 67 (quotes removed). BUT if
//  immediately before first quote mark there is neither "data(" nor "text(",
//  then "text(" is added, + a terminal ")". I.e.:
//     {arr = "ABC";} -->  {arr = text(65,66,67);}
//     {arr = text("ABC");} -->  {arr = text(65,66,67);}
//  If no error, only .B is set (TRUE). If error, .S has message
//  and .I has paragraph for rich edit selection.

  public static Quad ReplaceQuotes(ref string Txt)
  { Quad result = new Quad(false);   string ss, tt, uu;
    int m, startptr = 0, endptr; char[] qmks = {'\"', '\'', '`'}; // last is unicode 96, a keyboard char.
    if (P.Quotation == null) P.Quotation = new List<string>(); // Only happens on first run. R.KillData() clears Quotation, but does not null it.
 // WORK THROUGH TXT, QUOTE BY QUOTE:
    while (true)
    {// Find " or ', whichever comes first.
      startptr = Txt.IndexOfAny(qmks,0);
      if (startptr == -1)
       // THE NORMAL EXIT FROM THE LOOP (i.e. if no errors):
      { result.B = true;  return result; }

    // Find the valid end quote of the same type:
      char qch = Txt[startptr];
      endptr = Txt._IndexOf(qch, startptr+1);
      if (endptr < startptr+2)
      { if (endptr == -1) result.S = "unpaired quote mark";  else result.S = "empty quotes not allowed"; 
        result.I = LFsToHere(ref Txt, startptr); return result;
      }
    // Extract the part between the quote marks, store it, and replace it in code with a system function that references that storage:
      int qn = P.Quotation.Count;
      P.Quotation.Add(Txt._FromTo(startptr+1, endptr-1));
      ss = QuoteFnAlias + qn.ToString() + ")";
    // Replace the quote in Txt:
      // First, look for a preceding "data(" or "text(":
      m = startptr - 20; if (m < 0) m = 0;
      tt = (Txt._FromTo(m, startptr-1))._Purge(JS.WhiteSpaces); // preceding chars, cleaned.
      int ttLen = tt.Length;
      uu = tt._Extent(ttLen-5); // the last 5 chars.
      if (uu == "data(" || uu == "text(")
      { // If no identifier char. precedes e.g. 'text(' - as it might with some future fn. 'yakyaktext(.)' or may with a user function - then
        //   this is indeed an instance of fn. data(.) or fn. text(.), so simply remove the quotes:
        char ch = ' ';
        if (ttLen > 5) ch = tt[ttLen-6];
        if ( ch < 'A' || ch > 'z' || (ch > 'Z' && ch < 'a' && ch != '_') ) { tt = "";  uu = ""; }
        else {tt = "text("; uu = ")"; }
      }
      else {tt = "text("; uu = ")"; }
      Txt = Txt._ScoopTo(startptr, endptr, tt + ss + uu);
    }
  }

//imp //it
// Implant tokens. At the same time, check for a couple of user errors.
// If found, .B FALSE, .S has error message, .I the rich edit par. for selection.
// By now, runs of spaces have been converted to a single space.
  private static Quad ImplantTokens(ref string Txt)
  { Quad result = new Quad(false); 
    int[] dummy;  char[] negators = P.IdentifierChars.ToCharArray();
    // Check for a couple of likely wrong guesses at keywords:
   // Replace 'else if' by 'elseif': (only blanks can separate the two words, not CRs.)
    Txt = Txt.Replace("else if","elseif"); // Before this fn. was called, runs of blanks were reduced to a single blank.
                                           // Also, CRs and tabs. etc. are not allowed between 'else' and 'if'.
   // Replace 'array A, B,..' by 'array(A,B,..,1)':
    int n = 0, p = 0;
    while (n != -1) // The 2 IMPORT searches MUST come first, as the same words 'scalar' and 'array' occur in the other constructs.
    { n = Txt.IndexOf("import scalar ", n);
      if (n != -1)
      { p = Txt.IndexOf(';', n);
        if (p != -1) // if it is, too bad.
        { Txt = Txt.Insert(p, ")");
          Txt = Txt._Scoop(n, 14, "__importscalar(");
        }
        n += 13; // In case the file ends after the 'n' find, this hop must only be to the end of whichever is shorter 
                 //  of the original text (which may not have been replaced) and the replacement text.
      }
    }            
    n = 0;
    while (n != -1) // The 2 IMPORT searches MUST come first, as the same words 'scalar' and 'array' occur in the other constructs.
    { n = Txt.IndexOf("import array ", n);
      if (n != -1)
      { p = Txt.IndexOf(';', n);
        if (p != -1) // if it is, too bad.
        { Txt = Txt.Insert(p, ")");
          Txt = Txt._Scoop(n, 13, "__importarray(");
        }
        n += 12; // In case the file ends after the 'n' find, this hop must only be to the end of whichever is shorter 
                 //  of the original text (which may not have been replaced) and the replacement text.
      }
    }            
    n = 0;
    while (true)
    { n = Txt._IndexOf("array ", n);
      if (n == -1) break;
      if (n == 0 || P.IdentifierChars.IndexOf(Txt[n-1]) == -1) // i.e. if 'array ' found and is not part of an identifier name
      { p = Txt.IndexOf(';', n);
        if (p != -1) // if it is, too bad.
        { Txt = Txt.Insert(p, ")");
          Txt = Txt._Scoop(n, 6, "__array(");
        }
      }
      n += 6; // suitable whether 'array' was standalone or was part of a variable name
    }
    n = 0;


    while (true)
    { n = Txt._IndexOf("scalar ", n);
      if (n == -1) break;
      if (n == 0 || P.IdentifierChars.IndexOf(Txt[n-1]) == -1) // i.e. if 'scalar ' found and is not part of an identifier name
      { p = Txt.IndexOf(';', n);
        if (p != -1) // if it is, too bad.
        { Txt = Txt.Insert(p, ")");
          Txt = Txt._Scoop(n, 6, "__scalar(");
        }
      }
      n += 7; // suitable whether 'array' was standalone or was part of a variable name
    }

   // DO THE KEYWORD SUBSTITUTIONS:
    for (int i = CHequal+1; i < CHkwds; i++) // this extent covers lettered replacements. Non-lettered ones dealt with below.
    { Txt = JS.SubstituteIf(Txt, CHname[i], CH[i].ToString(), negators, out dummy); }
    // KEYWORD DUPLICATIONS ALLOWED:
    //    ...actually there aren't any, at the moment.
    // REPLACE KEYWORD ALIASSES:
    // *** The following was timed using both String.Replace(.) - as below - and using StringBuilder.Replace(.). The StringBuilder version
    //  took around 4 times as long on two large programs. As far as space saving goes, there are 12 replacements below; my largest
    //  program occupies around 100 kbytes, much of which is comments. Even if it were 100 kb of code, the string version below
    //  would temporarily use up something like 1.2 megabytes of RAM - a very tiny morsel!
    Txt = Txt.Replace("&&", CH[CHand].ToString());// alias for 'and'
    Txt = Txt.Replace("||", CH[CHor].ToString());// alias for 'or'
    Txt = Txt.Replace("^^", CH[CHxor].ToString());//alias for 'xor' (but NOT '^'!)
    Txt = Txt.Replace("<<", CH[CHlistopener].ToString());
    Txt = Txt.Replace(">>", CH[CHlistcloser].ToString());

     // REPLACE COMPARATORS AND EQUALS SIGN: (order of duplications important)
    Txt = Txt.Replace("==", CH[CHeq_eq].ToString());
    Txt = Txt.Replace("!=", CH[CHnot_eq].ToString());
    Txt = Txt.Replace(">=", CH[CHeq_gr].ToString());
    Txt = Txt.Replace("<=", CH[CHeq_less].ToString());
    Txt = Txt.Replace(">",  CH[CHgreater].ToString());
    Txt = Txt.Replace("<",  CH[CHless].ToString());
    Txt = Txt.Replace("=",  CH[CHequal].ToString());
    result.B = true; return result;
  }

// Set up the array of user functions. Errors reflected in .B and .S; otherwise
//  .S is empty. If errors, .I is the par. for rich edit selection.
// Spaces must have been removed by the time of this function.
  public static Quad SetUpFunctions(ref string Txt)
  { Quad result = new Quad(false);
    UserFnCnt = 1 + Txt._CountChar(CH[CHfunction]); // Includes the main pgm in the count.
    R.UserFnCalls = new int[UserFnCnt];
     // Set up the stack for recursion (holds values of variables).
    R.FStack = new List<TVar>[UserFnCnt]; // recursion stack for holding vars. The individual lists of the array
     // will not be generated unless recursion actually occurs, and will be deleted as no longer needed.
    R.ClientStack = new List<Duo>[UserFnCnt]; // recursion stack for holding argument sources.
    R.RStack = new List<Recur>[UserFnCnt]; // recursion stack for holding params.
    TFn Trial;
    P.UserFn = new TFn[UserFnCnt];
    FnExtremes = new Duo[UserFnCnt];
    for (int i = 0; i < UserFnCnt; i++)
    { P.UserFn[i] = new TFn(0); FnExtremes[i] = new Duo(-1,-1); }
    int n, loopptr = 0, startptr, opener, closer, argop, argclo;
    string intro, argstrip, arg;   char ch;

    // MAIN PROGRAM: All we need do here is give it a name, for convenience in displays:
    P.UserFn[0].Name = "Main Program";
    // LOOP THROUGH ALL USER FUNCTIONS (excluding the main pgm. itself):
    for (int fnctr = 1; fnctr < UserFnCnt; fnctr++)
    { startptr = Txt.IndexOf(CH[CHfunction], loopptr);
      if (startptr == -1)
      { result.S="syntax error - perhaps you have written a function inside a function? " +
                    "Can also happen if function '{..}' brackets are somewhere wrongly deployed";
        result.I=LFsToHere(ref Txt, startptr); return result;}
      Trial = new TFn(0);
      // Get various openers and closers:
      opener = Txt.IndexOf('{', startptr);
      if (opener==-1)
      { result.S="no '{' to delineate function";   result.I=LFsToHere(ref Txt, startptr); return result;  }
      Trial.LFBeforeOpener = LFsToHere(ref Txt, opener); // no. pars. from start of pgm. to the opener '{' of this function.
      closer = JS.NestLevel(Txt, '{', '}', opener, false)[1];
      if (closer==Txt.Length)
      { result.S="no '}' to close function";   result.I=LFsToHere(ref Txt, startptr);  return result;  }
      Trial.LFBeforeCloser = LFsToHere(ref Txt, closer);
      FnExtremes[fnctr].X = startptr;   FnExtremes[fnctr].Y = closer;
      intro = (Txt._FromTo(startptr+1, opener-1))._Purge(JS.WhiteSpaces); // fn. name + args
      argop = intro.IndexOf('(');  argclo = intro.IndexOf(')');
      if (argop==-1 || argclo<argop)
      { result.S="improper arg bracketting for function";
        result.I=LFsToHere(ref Txt, startptr); return result;}
      // Deal with function name:
      Trial.Name = intro._Extent(0, argop); // all between 'fn' symbol and '('.
      P.NameLocn(Trial.Name, -1, false, out ch); // Return value not used; we only want a value assigned to the OUT arg.
                                                 // '-1' --> make no attempt to search V.Vars (which does not yet exist).
      if (ch != ' ') // then the name is unusable:
      { if (ch == '#') result.S="function name is not valid";
        else if (ch == 'U')result.S = "an earlier function has the same name";
        else  result.S = "this name is a system function name, so is unavailable";
        result.I = LFsToHere(ref Txt, startptr); return result;
      }
      // Deal with arguments:
      argstrip = intro._FromTo(argop+1, argclo-1);
      // What, no arguments? Then invent one...  Use '_d_u_m_m_y_'. *** It has to be registerable with Use = 3,
      //   as P.InitializeArrays(.) calls V.AddUnassignedVariable(.) on it, and this returns -1 for a system variable.
      //   One could rewrite that code, but I would rather wait till a major revision of the whole process in some later incarnation of MonoMaths.
      if (argstrip == "") argstrip = "_d_u_m_m_y_";

      argstrip += ','; // makes the loop below easier.

      int trialMaxArgs = argstrip._CountChar(',');
      Trial.MaxArgs = trialMaxArgs;  Trial.MinArgs = 0;
      Trial.ArgNames = new string[trialMaxArgs];    Trial.ArgRefs = new short[trialMaxArgs];
      Trial.ArgDefaults = new double[trialMaxArgs]; Trial.PassedByRef = new bool[trialMaxArgs];
      Trial.ArgDefaultStrings = new string[trialMaxArgs]; // will be parsed early in run time, after constants defined.
      int pp=0, qq, rr;
      bool optionalfound = false; // once true, no more nonoptionals allowed.
      for (int i = 0; i < trialMaxArgs; i++)
      { qq = argstrip.IndexOf(',', pp);
        arg = argstrip._FromTo(pp, qq-1);
         // Look for 'ref' tag; if so, set 'PassedByRef[]' to TRUE, and remove it:
        if (arg.Length > 1 && arg[0] == C.CH[C.CHref])
        { Trial.PassedByRef[i] = true;
          arg = arg._Extent(1);
        }
        else Trial.PassedByRef[i] = false; // no 'ref' tag
         // Is this an optional argument?
        rr = arg.IndexOf(CH[CHequal]);
        if (rr == -1)
        { Trial.MinArgs++;
          if (optionalfound)
          { result.S = "obligatory arg. follows an optional (defaulted) one";
            result.I = LFsToHere(ref Txt, startptr + rr);  return result;
        } }
        else // this is an optional parameter:
        { optionalfound = true;  bool success;
          Trial.ArgDefaults[i] = arg._Extent(rr+1)._ParseDouble(out success);
          Trial.ArgDefaultStrings[i] = success ? "" : arg._Extent(rr+1);
            // Testing is left to run time. If the value planted in ArgDefaultStrings is not a constant,
            //   a crash will occur on the first occasion that the fn. is called in a way which tries to invoke the default value.
          arg = arg._Extent(0, rr);
        }
        // record the arg. name:
        if (arg == "")
        { result.S = "missing argument name";
          result.I = LFsToHere(ref Txt, startptr + rr);  return result; }
        Trial.ArgNames[i] = arg;
        pp = qq+1;
      }// END OF 'FOR' LOOP through ARGUMENTS of the one function
////      }// END OF 'IF' SECTION processing ARGUMENT STRIP of the one function

      // Assign text:
      Trial.Text = Txt._FromTo(opener+1, closer-1);
      // Check that functions always have a 'return'. If none, then stick one in
      //  at the end.
      n = Trial.Text._CountChar(CH[CHreturn]);
      if (n == 0)Trial.Text += CH[CHreturn];
      // Check for the rare but DANGEROUS error of trying to return a REF argument (can lead to very mysterious crashes)
      n = 0;
      while (true)
      { n = Trial.Text.IndexOf(CH[CHreturn], n);   
        if (n == -1) break;  else n++;
        int p = Trial.Text._IndexOfNoneOf(P.IdentifierChars, n); // Dissect out any array name that follows the 'return'
        if (p == 0) continue; // 'return' immediately followed by 
        if (p == -1) p = Trial.Text.Length;
        string ss = Trial.Text.Substring(n, p-n); // If there was a statement like "return ThisVar", then ss = "ThisVar".
        // Now check against arg. names:
        for (int i=0; i < trialMaxArgs; i++)
        { if (ss == Trial.ArgNames[i])
          { if (Trial.PassedByRef[i]) 
            { // One last check - 'return ThisVar+1" would, for example, be permissible.
              if (p < Trial.Text.Length)
              { ch = Trial.Text[p];
                if (ch != ';' &&  ch != '\u000A') break;
              }
              result.S = "function is trying to return a REF array argument";
              result.I = LFsToHere(ref Txt, opener + n);  return result;
            }
            break; // as the same variable name won't appear twice in the arguments list
          }
        }
      }
      // PUT TRIAL INTO FUNCTIONS ARRAY P.UserFn[]:
      P.UserFn[fnctr] = Trial;
      loopptr = closer;
    }// END OF 'FOR' LOOP through FUNCTIONS

   // HACK OUT THE MAIN PGM: This will end up in P.UserFn[0].Text. It must include
    // all the LFs of the functions that are removed (so that rich edit pars.
    // can be tracked, for errors in main pgm.)
    if (UserFnCnt == 1) // no functions, only main pgm:
    { P.UserFn[0].Text = Txt; }
    else
    { StringBuilder main = new StringBuilder();
      char[] pgm = Txt.ToCharArray();
     // Everything up to the first function goes into main.
      for (int i = 0; i < FnExtremes[1].X; i++) main.Append(pgm[i]);
      for (int i = 1; i < UserFnCnt; i++)
      {// All LFs within functions (but nothing else) goes into main.
        n = Txt._CountChar('\n', FnExtremes[i].X, FnExtremes[i].Y);
        main.Append('\n', n);
     // Collect all text from this to next fn. (or to end of text, for last fn.):
        if (i == UserFnCnt-1) // gather all from end last fn. to end Txt:
        { for (int j = FnExtremes[i].Y+1; j < pgm.Length; j++) main.Append(pgm[j]);}
        else
        { for (int j = FnExtremes[i].Y+1; j < FnExtremes[i+1].X; j++)
          main.Append(pgm[j]); }
      }
      // Check that there remains a main program - i.e. more than just '\n', '\t':
      bool success;
      n = main._FirstAtOrBeyond('H', '0', out success); // numeral '0'. As a bare minimum there must be numerals, even if not letters.
            // Useful if you just want to write functions before you start on your main pgm; let the main pgm. consist solely of the digit 0.
      if (n == -1) // covers empty 'main' as well as no char. as high as '0'.
      { result.S = "function(s) found, but there is no main program";  result.I = -1; return result; }
      P.UserFn[0].Text = main.ToString();
    }
    result.B = true;  return result;
  }

// Put all assignments between AssOpener and AssCloser, removing all semicolons
//  and all spaces in the process. However, any LFs stay buried in the assignment,
//  except as the first char. or last char.
  private static string DelineateAssignments(ref string Txt)
  { if (Txt == "") return "";
    char[] stin = Txt.ToCharArray();  char ch;
    StringBuilder stout = new StringBuilder();
  // FILTER THE INPUT STRING, CHAR. BY CHAR., AND BUILD THE OUTPUT STRING:
    bool InAss = false; // TRUE if op. pt. is inside an assignment.
    bool IsLast = false; // TRUE for the last char. of the string.
    bool isasschar;  int stinLast = stin.Length-1;
    for (int i = 0; i <= stinLast; i++)
    { if (i == stinLast) IsLast = true;
      ch = stin[i];
      isasschar = (AssignmentChars.IndexOf(ch) != -1);
      if (ch <= ' ' && ch != '\n')continue; // ignore blanks and control chars.
      if (InAss) // INSIDE AN ASSIGNMENT:
      { if (isasschar)
        { stout.Append(ch); if (IsLast) stout.Append(AssCloser); }
        else
        { stout.Append(AssCloser); // *** This allows nonstandard characters through (e.g. greek letters where user forgot "ALLOW GREEK").
                                   //   For now I am hoping that the misplacement of the closer will be picked up elsewhere. But better
                                   //   would be a test for this to be any of the valid non-AssignmentChars that could be met with.
          if (ch != ';') stout.Append(ch);
          InAss = false;
        }
      }
      else // NOT INSIDE AN ASSIGNMENT
      {
        if (ch == ';') continue; // meaningless char., when not closing an assmt.
        if (!isasschar)
        { stout.Append(ch);
        }
        else
        { stout.Append(AssOpener);  stout.Append(ch);
          if (IsLast) stout.Append(AssCloser);
          InAss = true;
        }
      }
      // If this is 'return', then whether InAss is true (as it would be for e.g. "if (x==1) return") or false (as it would be for
      // "if (x==1){return}"), we have to check whether it is an empty return, and if so, change it to 'return 0':
      if (ch == CH[CHreturn]) 
      { if(i == stinLast || AssignmentChars.IndexOf(stin[i+1]) == -1) // an empty return...
        { stout.Append(AssOpener); stout.Append('0');  stout.Append(AssCloser); }
      }
    }
    // if there are LFs at the start, put them before the AssOpener. If some at
    //  the end, after the AssCloser. But leave internal LFs as is.
    for (int i = 0; i < stout.Length-2; i++)
    { if (stout[i]==AssOpener && stout[i+1]=='\n') // swap opener and LF:
      { ch = stout[i]; stout[i] = stout[i+1]; stout[i+1] = ch; } }
    for (int i = stout.Length-1; i >= 2; i--)
    { if (stout[i]==AssCloser && stout[i-1]=='\n') // swap closer and LF:
      { ch = stout[i]; stout[i] = stout[i-1]; stout[i-1] = ch; } }
    // This might leave some empty assts., so eliminate them now:
    for (int i = stout.Length-2; i >= 0; i--)
    { if (stout[i]==AssOpener && stout[i+1]==AssCloser) stout.Remove(i,2); }
    return stout.ToString();
  }

// Deal with some shorthands: (1) as used in 'a++;'. (2) as used in 'a += 2;'.
// Error: .B FALSE, .S message, .I par. in rich edit to select (after you add
//  the fn. offset to it, in calling code). (Testing is NOT exhaustive. Some
//  errors rely on detection at parse time.)
  private static Quad ReplaceShorthand(ref string Txt)
  { Quad result = new Quad(false);
    int n, ptr, qtr;  string ss="", tt="";
    char equalsCode = CH[CHequal];
  // Deal with assignments of the type "x++;", "x--;". For scalars and
  //  complete arrays (i.e. no sq. bkts.), this will be replaced by e.g. "inc(x)",
  //  allowing use inside fns. (e.g. "sin(x++)"). But for specific array elements
  //  or segments ("mx[][1]++" or "mx[2,1]++"), "++" is replaced by "+=1";
  //  hence for them, this format can only be used as a standalone expression.
    while (true)
    { ptr = Txt.IndexOf("++");
      if (ptr >= 0)
      { if (Txt[ptr-1]==']')
      {ss = "";  tt = "+" + equalsCode + '1';}
        else { ss = "inc("; tt = ")"; }
      }
      else
      { ptr = Txt.IndexOf("--");
        if (ptr>-1)
        { if (Txt[ptr-1]==']'){ ss = ""; tt = "-" + equalsCode + '1';}
          else { ss = "dec("; tt = ")"; }
      } }
      if (ptr==-1)break; // no more "x++" or "x--" left.
      qtr = Txt._FirstFromEndNotIn(P.IdentifierChars+"[]", 0, ptr-1);
          // There will always be a non-identifier char., as AssOpener must be there. So if qtr returns as -1,
          //  it means that there were no identifier chars. (or '[]') in the range 0 to ptr-1.
      if (qtr == -1) { result.S = "'++' does not follow a variable name";  result.I = LFsToHere(ref Txt, ptr);  return result; }
      ss = ss + Txt._FromTo(qtr+1, ptr-1) + tt; // e.g. ss = "inc(" + "xx" + ")"
      Txt = Txt._ScoopTo(qtr+1, ptr+1, ss); // chop out the identifier and the '++' and insert their replacement.
    }
  // Deal with assignments of the type "x += 6;", remembering that '=' has been replaced by a CH[] character:
    Duo pt;
    string stroo = "+?,-?,*?,/?,^?,#?";   stroo = stroo.Replace('?', equalsCode);
    string[] signequal = stroo.Split(new char[] {','},StringSplitOptions.None);
    int assop, assclo;
    while (true)
    { pt = Txt._IndexOfAny(signequal);   if (pt.X < 0) break;
      char thisSign = stroo[3*pt.Y];
      assop = Txt._LastIndexOf(AssOpener, 0, pt.X);
      assclo = Txt._IndexOf(AssCloser, pt.X);
      string lhs = Txt._FromTo(assop+1, pt.X-1).Trim();
      string rhs = '(' + Txt._FromTo(pt.X+2, assclo-1).Trim() + ')'; // No harm if existing brackets are duplicated.
      if (lhs=="" || rhs=="")
      { result.S = "misplaced '" + thisSign + "='";  result.I = LFsToHere(ref Txt, assop);  return result; }
      // Usually we could just take the whole lhs and write: lhs = lhs + rhs (for '+='). But in the case of "if (a==1) n+= 10;" - the
      //  legitimate no-curly-bracket form - the lhs is '(a==1)n' (until Conform_IF_Segments(.) puts things right). Worse still, a FOR statement
      //  gives a bracket-mismatched lhs "inc(i))" (where 'i' is the loop variable).
      // So the rules are: (1) Set pointer to last char. of lhs. if this is ']', instead set pointer to MATCHING '['. This way we exclude
      //  any brackets legally inside the "[..]". (2) n = last occurrence of ')' BEFORE pointer;  if found, the prefix to rhs will just be
      //  the stuff after this ')'; otherwise it is the whole lhs.
      int endptr = lhs.Length-1;
      if (lhs[endptr] == ']') 
      { endptr = JS.OpenerAt(lhs, '[', ']', 0, endptr);
        if (endptr == -1) { result.S = "unmatched ']' within assignment";  result.I = LFsToHere(ref Txt, assop);  return result; }
      }
      n = lhs._LastIndexOf(')', 0, endptr);
      if (n != -1) // then we only use that part of lhs which follows the bracket, for prefixing to rhs:
      { string lhspreamble = lhs._Extent(0, n+1),  lhslastvar = lhs._Extent(n+1);
        ss = lhspreamble + lhslastvar + equalsCode + lhslastvar + thisSign + rhs;
        Txt = Txt._ScoopTo(assop+1, assclo-1, ss);
      }
      else // stick the whole of the lhs into the rhs:
      { Txt = Txt._ScoopTo(assop + 1, assclo - 1, lhs + equalsCode + lhs + thisSign + rhs); }
    }
  // For testing:  string asdf = Legible(Txt);
    result.B = true; return result; 
  }

// If an expression lacks an '=' sign or a comparator, prefix its contents with
// a numbered dummy variable "__di" (i = integer, starting at 1, not 0). In some
// cases, it gets "__ei" - void fns. not for REditRes display of results.
// (Note: conditional expressions will LATER in parse time pick up the universal
// dummy variable, which is just "__D"; but not here.)
  private static void InsertDummyVariables(ref string Txt)
  { int assop, assclo = 0;
    Duo pt;    string ss;    int n;   char ch;
    while (true)
    { assop  = Txt._IndexOf(AssOpener, assclo); if (assop == -1) break;
      assclo = Txt._IndexOf(AssCloser, assop);//mismatches checked long ago.
      if (Txt.IndexOf(CH[CHequal], assop, assclo-assop) == -1 &&
           Txt.IndexOfAny(CompCodes, assop, assclo-assop) == -1 )
      {//no '=' or comparators. See if the assignment starts with a name that
       // is not for routine display in REditRes, namely all user functions,
       // and those system fns. with field .Hybrid not set to 2:
        bool shy = false;
        n = Txt.IndexOf('(', assop);
        if (n != -1)
        { pt = P.NameLocn(Txt._FromTo(assop+1, n-1), 0, false, out ch);
          if (ch=='U' || (ch == 'F' && F.SysFn[pt.X].Hybrid != 2)) shy = true; }
        if (shy)
        { ss = NoShowDummyVarName + NoShowNextDummyNo.ToString();
          NoShowNextDummyNo++; }
        else
        { ss = NumberedDummyVarName + NextDummyNo.ToString();
          NextDummyNo++; }
        Txt = Txt.Insert(assop+1, ss  + CH[CHequal]);
        assclo += ss.Length + 1; // the length of the inserted string.
      } 
    }
  }
//------------------------------------------------------------------------------
// Basically a MACRO to convert POPULATE statements into FOR/DO loops preceded
//  by call(s) to system function LADDER(.).
  // RETURNED: No error: .B TRUE, nothing else set. Error: >B FALSE, .S = error msg.,
  //  .I is paragraph offset of problem within this text.
  private static Quad Conform_POPULATE_stmts(ref string Txt)
  { Quad result = new Quad(false); 
    int pPOP, pTHRO=-1, pUSING, pOPEN, pCLOSE, pCOMMA, pENDPOP, ranges; 
    string ss="", tt, ArrName="", FnPart="", txtcopy="";
    int n=0, oops = 0;
    while (true)
    {// FIND THE NEXT 'POPULATE'
      pPOP = Txt.IndexOf(CH[CHpopulate]);
      if (pPOP == -1) break; // NORMAL EXIT from loop, if no errors.
      txtcopy = Txt; // for LFsToHere later.
      StringBuilder Strip = new StringBuilder(); // Replacement text builds here.
     // FIND POSNS OF ALL RELEVANT KEYWORDS / CURLY BRACKETS 
      pTHRO = Txt.IndexOf(CH[CHthrough], pPOP);
      if (pTHRO == -1) {oops = 10; break; }
      pUSING = Txt.IndexOf(CH[CHusing], pTHRO);
      if (pUSING == -1) { oops = 20; break; }
      // GET ARRAY NAME AND ARRAY DIMENSION RANGES
      ArrName = Txt._FromTo(pPOP+2, pTHRO-2);//no test for sensible name here.
                                // The 2's are to avoid AssOpener and AssCloser.
      ss = Txt._FromTo(pTHRO+1, pUSING-1); // the ranges (with AssOp & -Closer)
      bool namedarrays;
      // Check first user-code char. after 'through'; if '(', a range;
      //  otherwise taken as a named array.
      if (Txt[pTHRO+2] == '(') namedarrays = false; else namedarrays = true;
      Duo[] bktptr = null; 
      if (namedarrays) ranges = 1 + ss._CountChar(','); 
      else 
      { bktptr = JS.OpenersAndClosers(ref ss, 1, '(', ')'); 
        if (bktptr == null) { oops = 30; break; } // Could only occur if brackets unmatched in the extent ss.
        ranges = bktptr.Length; // theoretically can be 0 (no brackets), but "if (namedarrays.." excludes that case.
      }
      string[] dims = new string[ranges];   string[] loops = new string[ranges];
      string[]lows =  new string[ranges];   string[] highs = new string[ranges];      
      for (int r=0; r < ranges; r++) // get names for the loop variables:
      { loops[r] = "__S" + NextSysVarNo.ToString(); NextSysVarNo++; }
      if (namedarrays)
      { int ptr = 1; // hop over the ass. opener at ss[0].
        for (int i=0; i<ranges; i++) 
        { pCOMMA = ss.IndexOf(',', ptr); 
          if (pCOMMA==-1) pCOMMA = ss.Length-1;// = the ass. closer
          dims[i] = ss._FromTo(ptr, pCOMMA-1);//accumulate 'through' array names.
          ptr = pCOMMA+1; 
        }
        // Dimensions were specified from outer to inner; so reverse dimensions, [0] becoming the lowest dimension:
        if (ranges > 1) Array.Reverse(dims);
        // (Re)Dimension the array to be populated: 
        Strip.Append(CH[CHassopener] + "dim(" + ArrName + ",size(");
        for (int i=ranges-1; i>=0; i--) 
        { Strip.Append(dims[i] + ")");
          if (i > 0)Strip.Append(",size(");  else Strip.Append(')');  
        }
        Strip.Append(CH[CHasscloser]); 
      }  
      else // ranges, not named arrays:
      { pCLOSE = -1;
        for (int i=0; i < ranges; i++)
        { pOPEN = bktptr[i].X;  pCLOSE  = bktptr[i].Y;
          tt = ss._FromTo(pOPEN+1, pCLOSE-1);
          pCOMMA = tt.IndexOf(',');  if (pCOMMA==0){oops = 60; break; }
          lows[i] = tt._Extent(0, pCOMMA);  highs[i] = tt._Extent(pCOMMA+1);
        }
        // Dimension ranges were specified from outer to inner; so reverse dimensions, [0] becoming the lowest dimension:
        if (ranges > 1) { Array.Reverse(lows);   Array.Reverse(highs); }

        // SYSTEM VAR. NAMES, LADDER(.) PARTS
        for (int r=0; r < ranges; r++)
        { Strip.Append(CH[CHassopener]);
          dims[r] = "__A" + NextSysVarNo.ToString(); NextSysVarNo++;
          Strip.Append(dims[r]);  Strip.Append(CH[CHequal]); 
          Strip.Append("ladder(size("+ArrName+","+r.ToString()+"),");
          Strip.Append(lows[r] + "," + highs[r] + ")");
          Strip.Append(CH[CHasscloser]);   
        }
      }
      // 'FOR' INTROS:
      for (int r=0; r < ranges; r++)
      { Strip.Append(CH[CHfor]);  Strip.Append(CH[CHassopener]);    Strip.Append('(');
        Strip.Append(loops[r]);   Strip.Append(',');
        Strip.Append("size(");    Strip.Append(ArrName);    Strip.Append(',');
        Strip.Append(r.ToString());   Strip.Append("))");
        Strip.Append(CH[CHasscloser]);  Strip.Append('{');
      }
      // USER FUNCTION BIT
          // Get function name:
      n = pUSING+2; // jumps AssOpener, to start of user fn. name    
      pENDPOP = Txt.IndexOf(CH[CHasscloser],n);//no test - must be one, sooner or later...
      FnPart = Txt._FromTo(n, pENDPOP-1); // = all after 'using' till end of assignment.
        // Add to 'Strip':
      Strip.Append(CH[CHassopener]); Strip.Append(ArrName);
      Strip.Append('[');             
      for (int r=0; r < ranges; r++)  
      { Strip.Append(loops[ranges-r-1]); if (r < ranges-1)Strip.Append(','); }
      Strip.Append(']');          Strip.Append(CH[CHequal]);
      // Develop the arguments, without brackets:
      string arglist = "", fnname = "";
      for (int r = 0; r < ranges; r++)
      { arglist += dims[r]+"["+loops[r]+"]"; 
        if (r < ranges - 1)arglist += ','; }
      // If user supplied an argument listed, substitute arglist for the first args, 
      //  whatever they are, and leave the rest as is. If no arguments listed,
      //  append arglist with brackets.
      int FnOpr = FnPart.IndexOf('(');
      if (FnOpr == -1) fnname = FnPart; // no args. supplied after function name
      else // user has opted to supply args., so substitute for
        // the first ones, whatever their names, leaving any excess arguments unchanged:
      { fnname = FnPart._Extent(0, FnOpr);
        int FnClosr = FnPart.IndexOf(')');
        if (FnClosr < FnOpr+2) { oops = 70; break; } // no ')', or empty '()'.
        n = FnPart._CountChar(',', FnOpr, FnClosr);  if (n < ranges-1) { oops = 80; break; }
        if (n >= ranges) // append the excess args. to the end of arglist:
        { ss = FnPart._FromTo(FnOpr+1, FnClosr-1);
          int p = ss._IndexOfNth(',', ranges); // must exist, because of above tests
          arglist += ss._Extent(p); // everything from the first superfluous ',' onwards.
      } }
      Strip.Append(fnname + '(' + arglist + ')');
      Strip.Append(CH[CHasscloser]);
    // THROW IN A FEW TERMINAL BRACES:
      for (int r = 0; r < ranges; r++) Strip.Append('}');

    // *** SPLICE IN THE REPLACEMENT STRIP    
      Txt = Txt._ScoopTo(pPOP, pENDPOP, Strip.ToString());
    }
    // DEAL WITH ERRORS
    if (oops == 0) result.B = true;
    else
    { switch (oops)
      { case 10: ss="no 'through' after 'populate'"; n = pPOP; break;
        case 20: ss="no 'using' after 'populate'"; n = pPOP; break;
        case 30: ss="faulty bracketting after 'through'"; n = pTHRO; break; 
        case 40: case 50: case 60: 
        { ss="faulty statement of ranges after 'through'"; n=pTHRO; break; }
        case 70: case 80:
        { ss="faulty function arguments list after 'through'";  n=pTHRO; break; }
        default: break;
      }
      result.S = ss;        result.I = LFsToHere(ref txtcopy, n);
    }
    return result;
  }
// -----------
// Syntax, as revised October 2008: The only allowed forms are: (1) the C version - with '(..)' after FOR and '{..}' after that;
//  and (2) the shorthand "for (i, <expression>)" - short for "for (i=0; i < <expression>, i++)" ('i' must be a variable name, either
//  never previously assigned or not assigned earlier to an array.) ***NB: If ever cancelling / modifying this syntax, recall that
// Conform_POPULATE_stmts(.) converts 'populate' sequences into this 2nd. syntax.
// Oh, and one other concession: if braces left out after the condition, the first assignment has them inserted:
//  "for (yak) a += 1; " -->  "for (yak) { a += 1; } "
// The incoming syntax is translated into a form using CH[CHiffinal] - NOT - CH[CHif] - and CH[CHbackif].
// RETURNED: No error: .B TRUE, nothing else set. Error: .B FALSE, .S = error msg., .I is paragraph offset of problem within this text.
  private static Quad Conform_FOR_Loops(ref string Txt)
  { Quad result = new Quad(false);  int oops = 0;
    Twelver asst, bsst;
    int p, q, qq, pFOR, pOPEN=0, pCLOSE, origlfs;
    string ss="", tt;
    string sINIT="", sCOND="", sINCMT="", sACTION="";
    bool fullsyntax=false;
    while (true)
    {// FIND THE NEXT 'FOR':
      pFOR = Txt.IndexOf(CH[CHfor]);
      if (pFOR == -1) break; // NORMAL EXIT from loop, if no errors.
     // The first printable character in the first assignment should always be a '('. Get rid of this and its pair.
     // At the same time, use it to find out whether curlies immediately follow the bracketted section, and whether syntax complete. 
      p = Txt._IndexOfNoneOf(SpaceParAss, pFOR+1);   
      if (p < 0 || Txt[p] != '(') { oops = 10; break; } // no '(' after 'for'
      q = JS.CloserAt(Txt, '(', ')', p);  // No error check, as unmatched brackets found much earlier in parsing.
      int[] inty = InsertBracesIfNeeded(ref Txt, q);  if (inty[0] < 0) { oops = 20; break; } // nothing after 'for(..)'.
      // Now braces are always present. Next task: Is this the full syntax, or the shorthand form ("for (1,20).."?
      qq = Txt._CountChar(CH[CHassopener], pFOR, q); // count asst. openers (NOT closers) between 'for' and the final ')' of "for(..)".
      // Will be 1 for the shorthand form, 3 for the full form if no terminal semicolon ("if(...i++)"), 4 for same if terminal semicolon
      // ("if(...i++;)"). We will allow value 1 and any value 3+ (if user wants to add on meaningless extra assts, let him do so).
      fullsyntax = (qq >= 3);
      if (!fullsyntax && qq != 1) { oops = 30; break; }
      // We no longer need the encasing '(..)' as markers, so will remove them:      
      Txt = Txt.Remove(q, 1);  Txt = Txt.Remove(p, 1); 
      // There are now no '()'brackets encasing the loop-control part, and '{}' always encase the action part.
      if (fullsyntax) // We will call FindNextAsst(.) just 3 times, so extra assts. would be ignored.
      { asst = FindNextAsst(ref Txt, pFOR); // no error check, as we earlier counted 3 asst. openers
        sINIT = CH[CHassopener] + asst.SZ + CH[CHasscloser];
        bsst = FindNextAsst(ref Txt, asst.IY);
        sCOND = CH[CHassopener] + bsst.SZ + CH[CHasscloser];
        bsst = FindNextAsst(ref Txt, bsst.IY);
        sINCMT = CH[CHassopener] + bsst.SZ + CH[CHasscloser];
      }
      else // is the short version, like "for (i, 20).."
      { asst = FindNextAsst(ref Txt, pFOR); // no error check, as we earlier found this asst's. opener
        p = asst.SZ.IndexOf(',');   if (p == -1) { oops = 40; break; } // no comma in the shorthand form
        ss = asst.SZ._Extent(0, p);  tt = asst.SZ._Extent(p+1); // for above example, ss = "i", tt = "20".
        sINIT = CH[CHassopener] + ss + CH[CHequal] + '0' + CH[CHasscloser]; // "i=0". ('__L0' substitution comes later in parsing)
        sCOND = CH[CHassopener] + ss + CH[CHless] + tt + CH[CHasscloser]; // "i < N" ( N being an expression)
        sINCMT = CH[CHassopener] + "inc(" + ss + ')' + CH[CHasscloser]; // "i++" (i.e. "inc(i)").  
      }
      pOPEN = Txt.IndexOf('{', pFOR); // No error check - we either found it or inserted it, above.
      pCLOSE = JS.CloserAt(Txt, '{', '}', pOPEN); // No error check - matching braces was tested for much earlier in parsing.
      sACTION = Txt._FromTo(pOPEN + 1, pCLOSE - 1); // allowed to be empty
      origlfs = LFsToHere(ref Txt, pCLOSE) - LFsToHere(ref Txt, pFOR); // original no. '\n's between 'for' and '}', for later comparison.
      // Note that the '{' and '}' are no longer present.   
 // BUILD THE REPLACEMENT STRETCH TO GO INTO Txt:
      ss = sINIT + CH[CHiffinal] + sCOND + CH[CHthen];
      // If any linefeeds removed earlier, they will go in here, so develop the stuff to follow in a different string:
      tt = sACTION + sINCMT + CH[CHbackif];
      // Now insert any due linefeeds:
      p = ss._CountChar('\n') + tt._CountChar('\n');
      if (origlfs > p) ss += ('\n')._Chain(origlfs - p);
  //...AND GRAFT IT INTO Txt:
      Txt = Txt._ScoopTo(pFOR, pCLOSE, ss+tt);
    } // END of WHILE loop.
    if (oops == 0) result.B = true;
    else
    { if      (oops == 10) result.S = "The loop data which immediately follows 'for' must be enclosed in brackets '(..)'";
      else if (oops == 20) result.S = "incomplete data after 'for'";
      else if (oops < 100) result.S = "'for (..)' - the loop data in the brackets '()' is not properly formatted";
      result.I = LFsToHere(ref Txt, pFOR);
    }
    return result;
  }

  private static Quad Conform_FOREACH_Loops(ref string Txt)
  { Quad result = new Quad(false);  int oops = 0;
    int p, q, pFOREACH, pOPEN=0, pCLOSE;
    string sGETLEN="", sINIT="", sCOND="", sINCMT="", sACTION="";
    string newTxt = "";
    while (true)
    {// FIND THE NEXT 'FOREACH':
      pFOREACH = Txt.IndexOf(CH[CHforeach]);
      if (pFOREACH == -1) break; // NORMAL EXIT from loop, if no errors.
     // The first printable character in the first assignment should always be a '('. Get rid of this and its pair.
     // At the same time, use it to find out whether curlies immediately follow the bracketted section, and whether syntax complete. 
      p = Txt._IndexOfNoneOf(SpaceParAss, pFOREACH+1);   if (p < 0 || Txt[p] != '(') { oops = 10; break; } // no '(' after 'foreach'
      q = JS.CloserAt(Txt, '(', ')', p);  // No error check, as unmatched brackets found much earlier in parsing.
      int[] inty = InsertBracesIfNeeded(ref Txt, q);  if (inty[0] < 0) { oops = 20; break; } // nothing after 'foreach(..)'.
      // Now braces are always present.
      pOPEN = Txt.IndexOf('{', pFOREACH); // No error check - we either found it or inserted it, above.
      pCLOSE = JS.CloserAt(Txt, '{', '}', pOPEN); // No error check - matching braces was tested for much earlier in parsing.
      // Between p and q there should be exactly variable names separated by commas, e.g. "(x,y,i,AA)" - no special codes, spaces, tabs...
      string ss = Txt._Between(p, q); // can be "x,AA" or "x,y,AA" or "x,y,i,AA".
      int nameryLen = ss._CountChar(',') + 1;
      string[] namery = ss.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries); // No crash if ss is empty.
      if(nameryLen != namery.Length) { oops = 30; break; }
      if (nameryLen < 2 || nameryLen > 4) { oops = 20; break; } // reusing the message of 'oops = 20' in code above.
      string scalarX, scalarY = "", scalarI = "", arrayAA; // all symbols for the actual names of the variables symbolized above as x, y, i, AA.
      // Deploy elements of text between brackets, according to the different allowed options:
      scalarX = namery[0]; // 'x' is always present, and always the first variable in the list.
      if (nameryLen > 2) scalarY = namery[1];
      if (nameryLen > 3) scalarI = namery[2];
        else { scalarI = "__it" + NextSysVarNo.ToString();  NextSysVarNo++; } // An implicit iterator, if no explicit iterator required.
      arrayAA = namery[nameryLen-1]; // 'AA' must always be present, and must be the last variable in the list.      
      bool changeArray = (scalarY != "");
      // Start building the new text to replace all from "foreach" inclusive to "{" exclusive.
      string scalarL = "__len" + NextSysVarNo.ToString();  NextSysVarNo++; // will be the length of the array.
      sGETLEN = CH[CHassopener] + scalarL + CH[CHequal] + "size(" + arrayAA + ")" + CH[CHasscloser]; // "len = size(AA);"
      sINIT   = CH[CHassopener] + scalarI + CH[CHequal] + '0' + CH[CHasscloser]; // "i = 0;"
      sCOND   = CH[CHassopener] + scalarI + CH[CHless] + scalarL + CH[CHasscloser]; // "i < len;"
      sINCMT  = CH[CHassopener] + "inc(" + scalarI + ')' + CH[CHasscloser]; // "i++;"  
      sACTION = Txt._Between(pOPEN, pCLOSE); // All contents of "{ ... }" - but not the braces themselves. Allowed to be empty
      // Add to the user's text between braces:
      ss = CH[CHassopener] + scalarX + CH[CHequal] + "datastrip(" + arrayAA + ',' + scalarI + ')' + CH[CHasscloser]; // "x = datastrip(AA, i);"
      if (changeArray) { ss += CH[CHassopener] + scalarY + CH[CHequal] + scalarX + CH[CHasscloser]; } // "y = x;"
      sACTION = ss + sACTION;
      if (changeArray) // then we must put "datastrip(AA, i, y);" at the end
      { string tt = CH[CHassopener] + "datastrip(" + arrayAA + ',' + scalarI + ','  + scalarY + ')' +  CH[CHasscloser];
        sACTION += tt;
      }
   // BUILD THE REPLACEMENT STRETCH TO GO INTO Txt:
      // All up to what was the '{' is replaced; the new section ends in a 'then' (which will be where '{' used to be).
      newTxt = sGETLEN + sINIT + CH[CHiffinal] + sCOND + CH[CHthen];
      // Now insert any due linefeeds:
      int LFcount = Txt._CountChar('\n', pFOREACH, pOPEN);
      if (LFcount > 0) newTxt += ('\n')._Chain(LFcount);
      // Finally add on sACTION, which is the part that was between braces, together with the bits we have just added to it:
      newTxt += sACTION + sINCMT + CH[CHbackif];
   // GRAFT IT INTO Txt:
      Txt = Txt._ScoopTo(pFOREACH, pCLOSE, newTxt);
    } // END of WHILE loop.

    if (oops == 0) result.B = true;
    else
    { if      (oops == 10) result.S = "The loop data which immediately follows 'foreach' must be enclosed in brackets '(..)'";
      else if (oops == 20) result.S = "'foreach' must be followed by 2 to 4 variable names in brackets '(..)'";
      else if (oops == 30) result.S = "formatting mistake in brackets following 'foreach'";
      result.I = LFsToHere(ref Txt, pFOREACH);
    }
    return result;
  }

// NB! CALL TO THIS MUST PRECEDE CALL TO "Conform_WHILE_Loops(.)", so that the two uses of CH[CHwhile] are not mixed up. As this 
//  version is more specialized (in using the extra token DO), it is dealt with first. 
// Syntax: The only allowed form is the C version - with '{..}' after 'do' and '(..)' after the WHILE which follows the action section.
// And one other concession, as for IF and FOR: if braces left out after the condition, the first assignment has them inserted:
//  "do a += 1; while (yak)" -->  "do { a += 1; } while (yak)" (Although it would be easy to allow any number of unbraced assignments
//  between 'do' and 'while', for consistency with IF, FOR and WHILE  I am not allowing it to happen.)
// The incoming syntax is translated into a form using CH[CHiffinal] - NOT - CH[CHif] - and CH[CHbackif].
// RETURNED: No error: .B TRUE, nothing else set. Error: .B FALSE, .S = error msg., .I is paragraph offset of problem within this text.
  private static Quad Conform_DO_Loops(ref string Txt)
  { Quad result = new Quad(false);  int oops = 0;
    Twelver asst;
    int p=0, q, pDO, pWHILE=0, pOPEN=0, pCLOSE, pFINIS, origlfs;
    string ss="", tt, sCOND="", sACTION="";
    string SpaceParAss = Space_Par + CH[CHassopener] + CH[CHasscloser]; 
    while (true)
    {// FIND THE NEXT 'DO':
      pDO = Txt.IndexOf(CH[CHdo]);
      if (pDO == -1) break; // NORMAL EXIT from loop, if no errors.
     // If the next char. is not '{', insert '{}' around the first assignment.
      int[] bracery = InsertBracesIfNeeded(ref Txt, pDO); // returns brace positions, whether inserted or originally present.
      if (bracery[0] < 0) { oops = 10; break; } // nothing after 'do'.
      pOPEN = bracery[1]; pCLOSE = bracery[2];      
     // Expect a WHILE to be the next signif. character. 
      pWHILE = Txt._IndexOfNoneOf(SpaceParAss, pCLOSE+1);   
      if (pWHILE < 0 || Txt[pWHILE] != CH[CHwhile]) { oops = 20; break; } // no 'while' after '}'
     // The first printable character after 'while' should always be a '('. Get rid of this and its pair.
      p = Txt._IndexOfNoneOf(SpaceParAss, pWHILE+1);   if (p < 0 || Txt[p] != '(') { oops = 30; break; } // no '(' after 'while'
      q = JS.CloserAt(Txt, '(', ')', p);  // No error check, as unmatched brackets found much earlier in parsing.
      // We RETAIN the encasing '(..)', as we will be transplanting the condition later to where they are needed:
      asst = FindNextAsst(ref Txt, pWHILE);
      pFINIS = asst.IY; 
      if (q > pFINIS) { oops = 40; break; } // the bracketted section extends beyond a single assignment.
      origlfs = LFsToHere(ref Txt, pFINIS) - LFsToHere(ref Txt, pDO); // original no. '\n's between 'do' and ')' after while, for later comparison.
      sCOND = asst.SZ; // NO asst. opener or closer; and encasing brackets remain in situ.
      if (asst.XX > 0.0) sCOND = sCOND.Replace("=", "==");
      sACTION = Txt._FromTo(pOPEN + 1, pCLOSE - 1); // allowed to be empty
      // Note that the '{' and '}' are no longer present.   
 // BUILD THE REPLACEMENT STRETCH TO GO INTO Txt:
      // Not so easy. Recall that 'continue' must work, but it will later be made to point to one spot back from 'backif'. Also, 'break'
      //  has to break out of the whole 'if..backif' structure. For this reason I am recasting the DO loop into the form of a WHILE loop,
      //  as follows: "do {actions} while (cond)" --> "Dummy = 1; while (Dummy OR (cond)) {actions; Dummy = 0; } This way, the dummy
      //  variable will be just behind 'backif', so 'continue' will land on its resetting.
      ss = AssOpStr + "__WH" + CH[CHequal] + "1" + AssCloStr;
      ss += CH[CHiffinal] + AssOpStr + "__WH" + CH[CHeq_eq] + "1" + CH[CHor] + sCOND + AssCloStr + CH[CHthen];
      // If any linefeeds removed earlier, they will go in here, so develop the stuff to follow in a different string:
      tt = sACTION + AssOpStr + "__WH" + CH[CHequal] + "0" + AssCloStr + CH[CHbackif]; // 'While' loops need a dummy marker here,
            // but 'do' loops, like 'for' loops, don't, as there is always a machine-added step just before the 'backif', 
            // for a 'continue' to land on.
      // Now insert any due linefeeds:
      p = ss._CountChar('\n') + tt._CountChar('\n');
      if (origlfs > p) ss += ('\n')._Chain(origlfs - p);
  //...AND GRAFT IT INTO Txt:
      Txt = Txt._ScoopTo(pDO, pFINIS, ss+tt);
    } // END of WHILE loop.
    if (oops == 0) result.B = true;
    else
    { if      (oops == 10) { result.S = "unexpected end of file after 'do'";  p = pDO; }
      else if (oops == 20) { result.S = "no 'while' section after 'do'";      p = pDO; }
      else if (oops == 30) { result.S = "no bracket after 'while' in a 'do..while' construction"; p = pWHILE; }
      else if (oops == 40) { result.S = "faulty section after 'while' in a 'do..while' construction"; p = pWHILE; }
      result.I = LFsToHere(ref Txt, p);
    }
    return result;
  }

// Syntax, as revised October 2008: The only allowed form is the C version - with '(..)' after WHILE and '{..}' after that.
// And one other concession, as for IF and FOR: if braces left out after the condition, the first assignment has them inserted:
//  "while (yak) a += 1; " -->  "while (yak) { a += 1; } "
// The incoming syntax is translated into a form using CH[CHiffinal] - NOT - CH[CHif] - and CH[CHbackif].
// RETURNED: No error: .B TRUE, nothing else set. Error: .B FALSE, .S = error msg., .I is paragraph offset of problem within this text.
  private static Quad Conform_WHILE_Loops(ref string Txt)
  { Quad result = new Quad(false);  int oops = 0;
    Twelver asst;
    int p, q, pWHILE, pOPEN=0, pCLOSE, origlfs;
    string ss="", tt, sCOND="", sACTION="";
    string SpaceParAss = Space_Par + CH[CHassopener] + CH[CHasscloser]; 
    while (true)
    {// FIND THE NEXT 'WHILE':
      pWHILE = Txt.IndexOf(CH[CHwhile]);
      if (pWHILE == -1) break; // NORMAL EXIT from loop, if no errors.
     // The first printable character after 'while' should always be a '('. Get rid of this and its pair.
      p = Txt._IndexOfNoneOf(SpaceParAss, pWHILE+1);   if (p < 0 || Txt[p] != '(') { oops = 10; break; } // no '(' after 'while'
      q = JS.CloserAt(Txt, '(', ')', p);  // No error check, as unmatched brackets found much earlier in parsing.
      int[] inty = InsertBracesIfNeeded(ref Txt, q);  if (inty[0] < 0) { oops = 20; break; } // nothing after 'while(..)'.
      // We no longer need the encasing '(..)' as markers, so will remove them:      
      Txt = Txt.Remove(q, 1);  Txt = Txt.Remove(p, 1); 
      // There are now no '()'brackets encasing the loop-control part, and '{}' always encase the action part.
      asst = FindNextAsst(ref Txt, pWHILE); // no error check, as we earlier counted 3 asst. openers
      sCOND = CH[CHassopener] + asst.SZ + CH[CHasscloser];
      if (asst.XX > 0.0) sCOND = sCOND.Replace("=", "==");     
      pOPEN = Txt.IndexOf('{', pWHILE); // No error check - we either found it or inserted it, above.
      pCLOSE = JS.CloserAt(Txt, '{', '}', pOPEN); // No error check - matching braces was tested for much earlier in parsing.
      sACTION = Txt._FromTo(pOPEN + 1, pCLOSE - 1); // allowed to be empty
      origlfs = LFsToHere(ref Txt, pCLOSE) - LFsToHere(ref Txt, pWHILE); // original no. '\n's between 'while' and '}', for later comparison.
      // Note that the '{' and '}' are no longer present.   
 // BUILD THE REPLACEMENT STRETCH TO GO INTO Txt:
      ss = CH[CHiffinal] + sCOND + CH[CHthen];
      // If any linefeeds removed earlier, they will go in here, so develop the stuff to follow in a different string:
      tt = sACTION + CH[CHdummymarker] + CH[CHbackif]; // Note the DUMMY. This dummy is necessary so that CONTINUE can land the focus
                // one step back from BACKIF, as it does with FOR loops (which don't need a dummy, as there is always a system-added 
                // incrementing asst. there). 
      // Now insert any due linefeeds:
      p = ss._CountChar('\n') + tt._CountChar('\n');
      if (origlfs > p) ss += ('\n')._Chain(origlfs - p);
  //...AND GRAFT IT INTO Txt:
      Txt = Txt._ScoopTo(pWHILE, pCLOSE, ss+tt);
    } // END of WHILE loop.
    if (oops == 0) result.B = true;
    else
    { if      (oops == 10) result.S = "The loop data which immediately follows 'while' must be enclosed in brackets '(..)'";
      else if (oops == 20) result.S = "incomplete data after 'while'";
      result.I = LFsToHere(ref Txt, pWHILE);
    }
    return result;
  }

//sift:
// SIFT HANDLER.  
  private static Quad Conform_SIFT_Segments(ref string Txt)
  { Quad result = new Quad(false);
    string _if_ = CH[CHif].ToString(), _elseif_ = CH[CHelseif].ToString(); // We don't need an 'else' string, as it is already there in processed code
    string subject;
    int p, q, pBraceOp, pBraceClos, pSIFT;
    while (true)
    {// FIND THE NEXT 'SIFT':
      pSIFT = Txt.IndexOf(CH[CHsift]);
      if (pSIFT == -1) break; // NORMAL EXIT from loop, if no errors.
      // brackets should now immediately follow, enclosing the subject:
      p = Txt._IndexOfNoneOf(SpaceParAss, pSIFT+1);   if (p < 0 || Txt[p] != '(')
      { result.S = "no brackets '(...)' after the 'sift' keyword";  result.I = LFsToHere(ref Txt, pSIFT);  return result; }
      q = JS.CloserAt(Txt, '(', ')', p); // No error check, as unmatched brackets found much earlier in parsing.
      subject = Txt.Substring(p+1, q-p-1); // must be a variable name
      pBraceOp = Txt.IndexOf('{', q); // No test, as above loop put missing ones there. 
      pBraceClos = JS.CloserAt(Txt, '{', '}', pBraceOp); // No error check, as unmatched brackets found much earlier in parsing.
      // Replace "sift (expn)" with "__siftN = expn;"
      string siftVarName = "var__" + NextSysVarNo.ToString(); NextSysVarNo++;
      StringBuilder sb = new StringBuilder(); // will hold the replacement of the whole sift block
      sb.Append(AssOpStr + siftVarName + CH[CHequal] + subject + AssCloser);
      p = Txt._CountChar('\n', pSIFT, pBraceOp);
      sb.Append(('\n')._Chain(p));
      // Work through the colons. There are two 'break's from this loop; one for an 'else:' line, and one for no such line.
      bool isFirstInstance = true;
      int pColon = pBraceOp, last_pColon = pColon, pBeforeColon;
      string instance;
      while (true) // loop through all colons of the SIFT block
      { 
        while (true) // the next colon might be inside nested braces for handling some instance, and have nothing to do with this 'sift':
        { pColon = Txt.IndexOf(':', pColon+1, pBraceClos - pColon);
          if (pColon == -1) break;
          if (JS.CloserAt(Txt, '{','}', pColon) == pBraceClos) break; // Break if it is still in the original brace level of the 'sift'.
          pColon += 1;
        }
        if (pColon == -1)
        { if (isFirstInstance) 
          { result.S = "No colon found within a 'sift' block";  result.I = LFsToHere(ref Txt, pSIFT);  return result; }
          // No more colons, so we have to add on the code for the last instance:
          sb.Append(Txt._FromTo(last_pColon+1, pBraceClos-1));
          break;
        }
        pBeforeColon = Txt._LastIndexOf(AssOpener, last_pColon, pColon); // the asst. opener for the expression before this colon.
        if (pBeforeColon == -1) // #### check: I think it can occur if you try nesting instance-colon pairs after 'else:'.
        { result.S = "Some problem with the colon here (? acute colitis).";  result.I = LFsToHere(ref Txt, pColon);  return result; }
        if (isFirstInstance) // Insert any LFs that are in the original text between "{" and the first ":"
        { p = Txt._CountChar('\n', pBraceOp, pColon);
          sb.Append(('\n')._Chain(p));
        }
        else // Insert all the text up to (but not including) pBeforeColon; this should include all par. marks.
        { sb.Append(Txt._FromTo(last_pColon+1, pBeforeColon-1)); 
        }
        // Convert the instance(s) of this asssignment to conditional statements:
        instance = Txt.Substring(pBeforeColon+1,  pColon - pBeforeColon - 1); // all between start of par. and colon
        // Deal with "ELSE:":
        if (instance == "") // then we probably have "ELSE [[ : <code>".
        { if (Txt[pBeforeColon-1] == CH[CHelse])
          { // Two possibilities: (1) User wrote "else: x = 1;" or (2) he wrote "else: { x = 1; ... }". Case 1 gives us:
            // "ELSE [[:X = 1]]"; we want to end up with "ELSE [[x = 1]]. Case 2 gives us: "ELSE [[:]] { [[....]] };
            //  we want to end up with "ELSE {[[..]]}.
            if (Txt[pColon+1] == AssCloser) // Case 2 above:
            { sb.Append(Txt._FromTo(pColon+2, pBraceClos-1) ); }
            else // Case 1 above:
            { sb.Append(AssOpStr);
              sb.Append(Txt._FromTo(pColon+1, pBraceClos-1) );
            }
            break; // i.e. the "else: " statement, if present is taken as the last (instance + colon), so no point in continuing in the WHILE loop.
          }
          else { result.S = "something strange before this colon - get rid of it";  result.I = LFsToHere(ref Txt, pColon);  return result; }
        }        
        else // the next block is crafted to allow nested conditions - e.g. ">0 && <2:".
        { if (isFirstInstance) sb.Append(_if_ + AssOpStr + "(");  else sb.Append(_elseif_ + AssOpStr + "(");
          int startPtr = 0;
          int endPtr = -1;
          bool lastLoop = false; // becomes true once focus is past any && or ||
          string andOr = "" + CH[CHand] + CH[CHor];
          while (!lastLoop)
          { endPtr = instance._IndexOfAny(andOr, startPtr).X;
            if (endPtr == -1) { lastLoop = true;  endPtr = instance.Length-1; }
            string subinstance = instance._FromTo(startPtr, endPtr); // includes any '&&' or '||' at the end
            // If the subject contains its own conditional (like "<=") then we leave it there; otherwise we supply "==".
            char ch = subinstance[0];
            // If 'ch' is a conditional test character, OR if it is the '[' of an array element reference, don't insert a "==".
            if ( (ch >= CH[0] && ch < CH[CHequal]) || ch == '[') // then ch is a conditional test character:
            { sb.Append(siftVarName + subinstance); }
            // Otherwise insert the implicit "==":
            else sb.Append(siftVarName + CH[CHeq_eq] + subinstance);
            startPtr = endPtr+1;
          }
          sb.Append(")"); // end of the conditional section.
          last_pColon = pColon;
        } 
        isFirstInstance = false;
      } // END of loop through all colons of this SIFT block
      // REPLACE THE ORIGINAL:
//      string TxtOld = Txt;//##############  KEEP THIS REMARKED-OUT LINE, AND THE ONE BELOW THE NEXT LINE, for troubleshooting.
      Txt = Txt._ScoopTo(pSIFT, pBraceClos, sb.ToString() );
//      ShowIt(TxtOld + "\n============\n" + Txt);//###########
    } // END of loop through all SIFT blocks in the program
    result.B = true;
    return result;
  }  

//if:
// IF-ELSE HANDLER. NB - This function IGNORES the output of the above FOR, WHILE and DO handlers, all of which translate syntax into
//  forms using CH[CHiffinal] and CH[CHbackif], which are not looked at here. This fn. purely handles the user's 'if' statements. The
//  ONLY allowed syntax for this is the C-type: "if (condition) { action }" (both sets of brackets are obligatory), with 'elseif'
//  and 'else' catered for. As with FOR, WHILE and DO, braces can be omitted if there is a single action assignment after the condition.
// RETURNED: If no errors, .B TRUE, and no other fields are used. If errors, .S holds the error message, and .I is par. offset of error.
  private static Quad Conform_IF_Segments(ref string Txt)
  { Quad result = new Quad(false);
    string ss;
    int p=0, q, pp, qq, r, pIF=-1, pEIF=-1; // pEIF will point to the first of either 'if' or 'elseif' encountered.
    char AliasELSE = CH[CHbkmark],  AliasELSEIF = AliasELSE;  AliasELSEIF++;
    char[] if_elseif_else = new char[] { CH[CHif], CH[CHelseif], CH[CHelse] };
    char[] eechars = new char[] { CH[CHelseif], CH[CHelse] }; // order is vital.
    string SpaceParAss = Space_Par + CH[CHassopener] + CH[CHasscloser]; 
    int iffycnt=0, elseifcnt=0;
// STEP 1 -- (a) CHECK FOR CONDITION'S ENCLOSING '()' BRACKETS; and (b) INSERT BRACES '{}' WHERE LEGALLY OMITTED. (The '()'
//   brackets will be dispensed with at the end of this stage, as they are only needed to detect legally omitted braces.)
    pEIF = -1;
    while (true)
    {// FIND THE NEXT 'IF', 'ELSEIF' or 'ELSE':
      pEIF = Txt.IndexOfAny(if_elseif_else, pEIF + 1);
      if (pEIF == -1) break; // no more IFs or ELSEIFs    
     // The first printable character after 'if' should always be a '('. Locate this and its pair.
      if (Txt[pEIF] == CH[CHelse]) q = pEIF; // then no bracketted condition follows, so set argmt. of fn. call to point to the token itself.
      else // A bracketted condition expected; set argm.t of fn. call to point to the final ')':
      { p = Txt._IndexOfNoneOf(SpaceParAss, pEIF+1);   
        if (p < 0 || Txt[p] != '(') // no '(' after 'if'
        { result.S = "the condition after 'if' or 'else if' must be enclosed within brackets '(..)'"; 
          result.I = LFsToHere(ref Txt, pEIF);  return result; 
        } 
        q = JS.CloserAt(Txt, '(', ')', p);  // No error check, as unmatched brackets found much earlier in parsing.
        if (q == -1) 
        { result.S = "Somehow the delineating of assignments has failed. Typically this is because a nonlegal character has been inserted. " +
                        "E.g. you used a greek character but 'ALLOW GREEK' is not the first line of your program"; 
          result.I = LFsToHere(ref Txt, pEIF);  return result; 
        } 
      }
     // If '{}' not present, insert them. 
      InsertBracesIfNeeded(ref Txt, q); // we don't use the returned array from this fn. in this case.
     // CHECK FOR '=' used instead of '=='; and in the process REMOVE THE BRACKETS, which have now done their duty.
      if (Txt[pEIF] != CH[CHelse]) // then there is a condition, enclosed in brackets:
      { pp = Txt.IndexOf(CH[CHequal], p);
        if (pp != -1 && pp < q)
        { result.S = "the condition after 'if' or 'else if' must not contain '=' -- use '==' instead."; 
          result.I = LFsToHere(ref Txt, pEIF);  return result; 
        } 
        Txt = Txt.Remove(q, 1); Txt = Txt.Remove(p, 1); 
      }
    }
// STEP 2 -- LOOP THROUGH ALL 'IF' INSTANCES and (a) change all ELSEIFs to ELSE { IF ... }, and (b) replace all '{}', and 
//               (c) replace user IF codes (CH[CHif]) with machine IF codes (CH[CHiffinal]):
    List<TRect> Iffy = null; // CODING for each TRect object:   .L is the type of the IF/ELSEIF/ELSE code(0=IF, 1=ELSEIF, 2=ELSE); 
                                                             // .T is ptr to that code; .R is ptr. to ensuing '{', .B to '}'.
  // NB - we will update and keep Iffy for use in step 3.
    pIF = -1;  TRect wrecked;
    while (true)
    {// FIND THE NEXT 'IF':
      pIF = Txt.IndexOf(CH[CHif], pIF+1);
      if (pIF == -1) break; // no more IFs.
      Iffy  = new List<TRect>();
      q = Txt.IndexOf('{', pIF); // No test, as above loop put missing ones there. 
      r = JS.CloserAt(Txt, '{', '}', q);
      wrecked = new TRect(0, pIF, q, r); // This covers the primary IF part.
      Iffy.Add(wrecked); // record details for this IF.
      // Now look for secondary components to this IF sequence:
      pp = pIF;
      while (true)
      { // 'wrecked.B' is the '}' of the last action segment put into Iffy.
        q = Txt._IndexOfNoneOf(SpaceParAss, wrecked.B+1); // what follows the '}'.
        if (q < 0) break;
        r = Array.IndexOf(eechars, Txt[q]); // returns 0 for elseif, 1 for else, -1 if neither.
        if ( r == -1) break;
        pp = Txt.IndexOf('{', q); // No test, as earlier loop put missing ones there. 
        qq = JS.CloserAt(Txt, '{', '}', pp);
        wrecked = new TRect(1+r, q, pp, qq);
        Iffy.Add(wrecked);
      }         
    // CONFORM TO BASIC FORMAT, in the process converting 'ELSEIF' to ELSE and IF combinations:
      iffycnt = Iffy.Count;
      elseifcnt = 0; for (int i = 0; i < iffycnt; i++) { if (Iffy[i].L == 1) elseifcnt++; }
      // For each ELSEIF, add a ENDIF at the end of the sequence:
      if (elseifcnt > 0)
      { ss = CH[CHendif]._Chain(elseifcnt);
        p = Iffy[iffycnt-1].B; // the final '}' of the segment. (Note: that final '}' is not itself replaced with ENDIF here.)
        if (p == Txt.Length-1) Txt += ss;
        else Txt = Txt.Insert(p+1, ss);
      }
      // Get an ELSE-IF replacement sequence ready:  
      string elseifstr = ""; elseifstr += CH[CHelsefinal].ToString() + CH[CHiffinal].ToString(); // will replace ELSIF.
      // We now have to work from backwards forwards, so we don't negate the Iffy pointers.
      for (int i = iffycnt-1; i >= 0; i--)
      {// Deal with the final '}' first: 
        if (i == iffycnt-1) Txt = Txt._Scoop(Iffy[i].B, 1, CH[CHendif].ToString()); // the last one --> ENDIF
        else Txt = Txt._Scoop(Iffy[i].B, 1); // all others are simply removed.
      // Deal with the '{'s next:
        if (i == 0 || Iffy[i].L == 1) Txt = Txt._Scoop(Iffy[i].R, 1, CH[CHthen].ToString()); // IF and ELSEIF: '{' --> then
        else Txt = Txt._Scoop(Iffy[i].R, 1); // all others are simply removed.       
      // Deal now with the instruction:
        if (Iffy[i].L == 0) Txt = Txt._Scoop(Iffy[i].T, 1, CH[CHiffinal].ToString());
        else if (Iffy[i].L == 1) Txt = Txt._Scoop(Iffy[i].T, 1, elseifstr);
        else Txt = Txt._Scoop(Iffy[i].T, 1, CH[CHelsefinal].ToString());
      }
    } // END of WHILE loop.
    result.B = true;  return result;
  }

// Used by the IF, DO, FOR, FOREACH, WHILE handlers. StartPoint is critical. In the case of tokens which must be followed by a bracketted
//   statement (FOR, FOREACH, WHILE not at the end of a 'do' loop, IF, ELSEIF), StartPoint MUST point to the final ')' of that statement.
// In the case of tokens which should be followed directly by '{' (ELSE, DO), StartPoint MUST point to the token itself.
//  This fn. does not remove the '()' brackets, in the former case; that is left to the calling code, after this fn. has been called. 
// RETURNED: 
//  [0]: Error if negative ( = error code). Otherwise [0] is 1 if braces were inserted, or 0 if opening brace was already there.
//  [1], [2]: Positions in Txt of '{', '}' (whether inserted here or not).
  private static int[] InsertBracesIfNeeded(ref string Txt, int StartPoint)
  { int[] result = new int[3];
    // Find the first signif. character after StartPoint:
    int signif = Txt._IndexOfNoneOf(SpaceParAss, StartPoint+1); // Ignore spaces, '\n', asst. openers and closers.
    if (signif < 0) { result[0] = -1; return result; } // Unexpected end of text - error code -1.
    char signifchr = Txt[signif];
    if (signifchr == '{') // Opening brace found.
    { result[0] = 0;   result[1] = signif;
      result[2] = JS.CloserAt(Txt, '{', '}', signif); // No test for existence needed at this stage of parsing.
      return result; 
    }
   // No opening brace found, so we have to insert braces. Find out what character has been found.
    result[0] = 1; // The returned code for 'no opening brace found, so braces inserted'.
    int q =  FlowBreakers.IndexOf(Txt[signif]); // Is it the code for 'return', 'continue', 'break' or 'exit'?
    if (q == 0) // token for 'RETURN' - the only one which may have a predicate (like "return x;"), so it needs special handling:
    { Twelver asst = FindNextAsst(ref Txt, signif);
      // 'return' ALWAYS has an asst. following, as we converted an empty 'return' to "return <asst. opener>0<asst. closer>" in
      //  method DelineateAssignments(.) (search in that method for 'CHreturn' to find the spot).
      Txt = Txt.Insert(asst.IY+1, "}");    // This following asst. is enclosed in the same braces as 'return'.
      result[2] = asst.IY+2; // '+2', not '+1', because the '{' will be inserted before it in the next step.
      Txt = Txt.Insert(signif, "{");  result[1] = signif;
      return result;
    }
    else if (q > 0) // (NB - not 'else if'.) Another flow breaker:
    { Txt = Txt.Insert(signif + 1, "}");  result[2] = signif + 2;  // '+2', not '+1', because the '{' will be inserted before it, a bit later.
      Txt = Txt.Insert(signif, "{");  result[1] = signif;
      return result;
    }
   // To get here, we have an unbraced assignment following StartAfterThis.
    int p = Txt.IndexOf(CH[CHasscloser], StartPoint);
    Txt = Txt.Insert(p+1, "}");  
    if (Txt[StartPoint] == ')') // then we are dealing with IF or ELSEIF or WHILE or FOR, which take '(..)' after them:
   // E.g. If Txt was: "if (i < 10) i = i+1;" we would now be looking at this (using '>>' for asst. opener, '<<' for closer):
   //  "if >>(i < 10)a = a+1 <<". We now convert this to "if >>(i < 10)<<{>>a = a+1 <<}".
    { Txt = Txt.Insert(StartPoint+1, AssCloStr + "{" + AssOpStr);  
      result[1] = StartPoint+1;  result[2] = p + 4;
    }
    else // we are dealing with ELSE or DO, and StartPoint points to the token for same:
    { Txt = Txt.Insert(StartPoint+1, "{");  
      result[1] = StartPoint+1;   result[2] = p + 2;
    }
    return result;
  }
//leg
/// <summary>
/// <para>Returns 'Txt' with all codes recorded in CH[.] replaced by a user-friendly translation. </para>
/// <para>Casery: Sets the case for the display of the translation of codes. Suppose the code being translated is the 'IF' code;
/// then values of Casery would result in the following format: 0 --> "if", 1 --> "If", 2 --> "IF", 3 --> "_if_", 4 --> ".if.".</para>
/// <para>This method returns unformatted text. If you want formatted text, use C.LegibleFormatted(.); if you don't want text returned
/// but just want it displayed, use C.ShowIt(.) instead.</para>
/// </summary>
  public static string Legible(string Txt, params int[] Casery)
  { if (Txt == null || Txt == "")return "";
    char[] stin = Txt.ToCharArray();
    StringBuilder stout = new StringBuilder();
    string ss;  int casing = 0; if (Casery.Length > 0) casing = Casery[0];
    foreach (char ch in stin)
    { ss = CHtransln(ch);
      if (ss != "??") // "??" is only a cue for the 'else' items below; it will not appear in the final text.
      { if (casing==1) { ss = char.ToUpper(ss[0]) + ss._Extent(1);}
        else if (casing==2) ss = ss.ToUpper();
        else if (casing==3) ss = '_'+ss+'_';
        else if (casing==4) ss = '.'+ss+'.';
        stout.Append(' '); stout.Append(ss); stout.Append(' ');
      }
      else if (ch == CH[CHassopener]) stout.Append("[");
      else if (ch == CH[CHasscloser]) stout.Append("]");
      else if (ch == CH[CHfnopener])  stout.Append("_{_");
      else if (ch == CH[CHasscloser]) stout.Append("_}_");
      else if (ch == Negator)stout.Append('~');
      else
      { if (ch < 255 || ch == '\u03A6') stout.Append(ch); // Fn. symbol (phi)
        else stout.Append("char."+((int)ch).ToString()+' '); }
    }
    return stout.ToString();
  }

/// <summary>
/// <para>Translates text which has been conformed and parsed (partly or wholly), into formatted text ready for a display routine.
///   There should be no remarks anywhere in any text.</para>
/// <para>If RawVersion is ' ', the above is simply returned. Otherwise the raw characters in unicode form are also displayed.
///   if 'h' or 'd', it is interleaved as hex or decimal. If 'H' or 'D', the raw output follows the formatted version, again as hex or dec.</para>
/// <para>If you don't want this text but just want it displayed, use C.ShowIt(same arg).</para>
/// </summary>
public static string LegibleFormatted(char RawVersion, params string[] InStr)
{ StringBuilder sbInterpreted = new StringBuilder(), sbChars = null;
  bool includeRaw = (RawVersion != ' ');
  bool asHex = (RawVersion == 'H' || RawVersion == 'h');
  bool interleave = (RawVersion == 'h' || RawVersion == 'd');
  if (includeRaw) sbChars = new StringBuilder(); 
  foreach (string Str in InStr)
  {
    char[] inch = Str.ToCharArray();
    foreach (char ch in inch)
    {
      string prefix = "", decoding = "";
      if (ch >= CHBASE) 
      { int p = Array.IndexOf(CH, ch); 
        if (p == -1)
        { prefix = "<# red>";  decoding = "?? "; }
        else 
        { if (ch == AssOpener || ch == AssCloser) prefix = "<# grey> ";
          else prefix = (p < CHkwds) ? "<# magenta> " : "<# green> ";
          decoding = CHname[p].Trim().ToUpper() + " "; 
        }
      }
      else if (ch == '\n') 
      { prefix = "<# black>";  decoding = "<# black>\u00b6\n"; }
      else if (ch == ' ')
      { prefix = "<# blue>";  decoding = "\u00b7"; }
      else if (ch < ' ')
      { prefix = "<# red>";  decoding = "SPACE "; }
      else
      { prefix = "<# blue>";  decoding = ch.ToString(); }
      // Add to stringbuilders:
      sbInterpreted.Append(prefix + decoding);
      if (includeRaw)
      { string ss = (asHex) ? ((int) ch).ToString("x") :  ((int) ch).ToString();
        sbChars.Append(prefix + ss);
        if (ch == '\n') sbChars.Append(ch);  else if (asHex) sbChars.Append(" "); else sbChars.Append(", ");
      }
    }
  }
  if (includeRaw) 
  { if (interleave)
    { char[] lf = new char[] { '\n' };
      string ssInt = sbInterpreted.ToString(),  ssRaw = sbChars.ToString();
      string[] arrInt = ssInt.Split(lf),   arrRaw = ssRaw.Split(lf);
      StringBuilder sboo = new StringBuilder();
      for (int i=0; i < arrInt.Length; i++)
      { sboo.Append(arrInt[i] + '\n');  if (i < arrRaw.Length) sboo.Append("   " + arrRaw[i] + '\n'); }
      sboo.Append('\n');
      return sboo.ToString();
    }
    else { sbInterpreted.Append(sbChars);  sbInterpreted.Append('\n'); }
  }
  else sbInterpreted.Append("\n\n<# black>");
  return sbInterpreted.ToString();
}
// Overloads of above:
public static string LegibleFormatted(char RawVersion, List<string> StrList)
{ string[] stroo = new string[StrList.Count];
  StrList.CopyTo(stroo);
  return LegibleFormatted(RawVersion, stroo);
}
public static string LegibleFormatted(char RawVersion, string Str)
{ return LegibleFormatted(RawVersion, new string[] {Str} );
}

/// <summary>
/// <para>Translates and then displays text which has been conformed and parsed (partly or wholly).</para>
/// <para>InStr[] strings are interpreted as remark strings if they start with "//" (no blanks); otherwise treated as a coded assignment.
/// Remark strings have a single '\n' appended; other strings have "\n\n" appended.</para>
/// <para>If you don't want a display but want a return of translated text, use either C.LegibleFormatted(.) - for formatted text -
/// or C.Legible(.) - for unformatted text.</para>
/// </summary>
public static void ShowIt(params string[] InStr)
{
  JD.PlaceBox(0.6, 0.6, -1,-1);
  JD.Display("", LegibleFormatted(' ', InStr), true, true, false, "CLOSE");
}
// Overloads of above:
public static void ShowIt(List<string> StrList)
{ string[] stroo = new string[StrList.Count];
  StrList.CopyTo(stroo);
  ShowIt(stroo);
}
public static void ShowIt(string Str)
{ ShowIt(new string[] {Str} );
}
//==============================================================================
} // END OF CLASS C[onformer]
} // END OF NAMESPACE MonoMaths


