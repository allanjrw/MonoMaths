using System;
using System.Collections.Generic;
using System.Text;
using JLib;

namespace MonoMaths
{

public struct TBubble // one unit in FlowLine.
{ public short Ass; // ref. to assignment in Assts[][].
  public short Next; // jump this far to the next thing to do. Usually = 1. Must never = 0, but can be negative.
  public short IfNot; // jump this far when preceding condition eval'd to FALSE.
  public short Ref; // No. of the paragraph to highlight, if error found.
  public override string ToString()
  { string ss = "Ass = " + Ass.ToString() + ";    Next = " + Next.ToString() +
         ";    IfNot = " + IfNot.ToString() + ";    Ref = " + Ref.ToString();
    if (Ass < 0)
    { string tt = "      (";
      if      (Ass==R.DummyMarker)  tt += "Dummy"; 
      else if (Ass==R.IFMarker)     tt += "IF";
      else if (Ass==R.CONTMarker)   tt += "CONTINUE";
      else if (Ass==R.BREAKMarker)  tt += "BREAK";
      else if (Ass==R.BACKIFMarker) tt += "BACKIF";
      else if (Ass==R.ENDIFMarker)  tt += "ENDIF";
      /*1*/ else if (Ass==R.PAUSEMarker)  tt += "PAUSE"; /*2*/
      else tt+="???";
      ss += tt + ')';
    }
    return ss;
  }
}
//tvar--------------------------------------------------------------------------
public class TVar // type for all arrays and scalars (incl. literals, constants).
{ public static int MaxNoDims = 3; // If altering, will need to alter the routines which handle array assignments,
    // as these involved MaxNoDims nested loops. Also load/save in 'A' mode, with the formatting option.
    // There are going to be increasingly more places where adjustments are needed. I will try always to refer to "TVar.MaxNoDims'
    // somewhere in the remarks of any code which is built around there being just 3 dimensions.
  public string Name;
  public byte Use; // 0 = not defined. 1 to 3 = SCALAR -- 1: system constant
   // (e.g. PI), and literal numerals in the user's program (e.g. "2.78");
   // 2: variables which only the system can change; 3: a normal user scalar variable.
   //(Internal scalars starting with '__' are never accessible to the user, and
   // are set to type 2. Type 3 is reserved for USER-generated variables.)
   // ARRAYS: 10 = unassigned array (no data yet); 11 = assigned array.
   // (All array details are stored at the location to which field .Ptr points.)
  public bool Accessed; // If the variable, after assignment, has ever been accessed, this is set to TRUE.
  public bool IsRefArg; // If the variable is a REF argument of a function, this is set to TRUE.
  public double X; // The current value of var., if scalar; for arrays, always
   // has the default value 0.0.
  public short Ptr; // The row (or 'storeroom') of ragged array R.Store in which
  //  the user's array values are stored. All scalars and all unassigned
  //  arrays have Ptr = -1.
// - - - - - - - - - - - -
   // As fields may be added / altered, using these strict-argument constructors
   //  is the only safe way to create new non-empty TVar objects. (Errors will
   //  then be thrown on any uncorrected old attempt to use older constructor.)
  public TVar(int dummy)
  { Name = ""; Use = 0;  X = 0.0;  Ptr = -1; Accessed = false; }
  public TVar(string _Name, double _X) // valid for USER-GENERATED SCALARS ONLY.
  { Name = _Name; Use = 3;  X = _X;  Ptr = -1; Accessed = false; }
  public TVar(string _Name, byte _Use, double _X)//for SCALARS and UNDEFINED ARRAYS
  { Name = _Name; Use = _Use;  X = _X;  Ptr = -1; Accessed = false; }
  public TVar Copied() // provide a complete copy of all fields.
  { if (this == null) return null;
    TVar result = new TVar(0);
    result.Name = Name;  result.Use = Use;  result.X = X;  result.Ptr = Ptr;  result.Accessed = Accessed;
    return result;
  }
  public override string ToString()
  { return  String.Format("Name: {0};  Use: {1};  X: {2};  Ptr: {3}", Name, Use, X, Ptr); }
  public string ToFormattedString()
  { string result = "<# ";
    if (Use == 0) result += "grey";
    else if (Use == 1)
    { if (Name[0]=='_')result += "green"; else  result += "black"; }
    else if (Use == 2) result += "magenta";
    else if (Use == 3) result += "blue";
    else if (Use == 10) result += "orange";
    else if (Use > 10) result += "red";
    else result += "black";
    result += ">Name: <B>" + Name + "</B>;   Use: " + Use.ToString() + "   ";
    if (Use >= 1 && Use <= 3) result += "<B>X: " + X.ToString() + "</B>";
    else result += "X: " + X.ToString();
    result += ";   Ptr: " + Ptr.ToString();
    if (Ptr > -1)
    { if (R.Store[Ptr] == null) result += " Storeroom NULL, despite pointer > -1.";
      else
      { result += ";   TotSz: " + R.Store[Ptr].TotSz + ";   DimCnt: ";
        result += R.Store[Ptr].DimCnt;
        result += ";   DimSz: ";
        if (R.Store[Ptr].DimSz == null) result += "NULL.";
        else
        { string ss = "";
          for (int i = TVar.MaxNoDims-1; i >= 0;  i--)
          { ss += R.Store[Ptr].DimSz[i].ToString();
            if (i > 0) ss += " x ";
          }
          result += ss;
        }
      }
    }
    result += "<# black>";  return result;
  }
}
//v------------------------------------------------------------------------------
public class V
{
 // FIELDS
	private V() {} // Cannot instantiate this class.
  public static List<TVar>[] Vars;
    // **** Design point: This was originally a ragged array of TVar. Switching to an array of lists downgraded performance
    //  by 1% to 2%. A trial of a list of a list, "List<List<TVar>>", downgraded performance by around 40%, so was abandoned.
    //  The advantage of current version over ragged array is simpler programming: no need to hold counts of inner array lengths,
    //  and no need to resize inner arrays periodically. (19 Feb 08.)
  public static List<Strint2>[] VarAliasses; // Only instantiated if a system fn. invokes an alias; if so, it is created to cover
    //  all functions. Each alias consists of two integers and a string: .IX = index in Vars[FnLvl], .IY = assigned Alias,
    //  .SX = variable's name. (.SY not used.)
  public static double[] PersistentArray; // The user may do what he/she likes with this. It is always set to an array of size 1,
                      // content NaN, when the MonoMaths instance starts up; it is only ever altered thereafter if system fns.
                      // alter it. Hence it is persistent over successive pgm runs occurring during the one instance of MonoMaths.

// DESTROYER:
  public static void SetVarsToNull()
  { if (Vars != null)
    { int varLen = Vars.Length;
      for (int i=0; i < varLen; i++) Vars[i].Clear();
      Vars = null; 
    }
  }
 // METHODS
  // All inner Vars lists are created along with Vars itself, so a test for null of inner lists is not needed.
  public static void InitializeVarsArrays(int NoFunctions)
  { Vars = new List<TVar>[NoFunctions];
    for (int i = 0; i < NoFunctions; i++) Vars[i] = new List<TVar>();
  }
  public static string FormattedString(int FnLvl, int VarNo)
  { return Vars[FnLvl][VarNo].ToFormattedString(); }
  public static bool VarsExists()
  { if (Vars == null || Vars.Length == 0) return false; else return true; }

  // PRIVATE VARIABLE ADDERS:
// They return the slot in R.Vars[FnLvl] of the new scalar (if successful), or -1
//  if not stored because a duplicate). Note that there is no check on Name,
//  Use or Value.
//  (1) Add a scalar (constant, literal or variable) to Vars[].
  private static int AddScalar(int FnLvl, string Name, byte Use, double Value)
  {// Check that it is not a duplicate:
    List<TVar> lars = Vars[FnLvl];
    for (int i=0; i < lars.Count; i++) { if (lars[i].Name == Name) return -1; }
    lars.Add(new TVar(Name, Use, Value));
    return lars.Count - 1;
  }  
//  (2) Add an unassigned array to Vars[]. If already exists, returns -1;
//   otherwise returns the new index of Vars[FnLvl][].
  private static int AddArray(int FnLvl, string Name)
  {// Check that it is not a duplicate:
    List<TVar> lars = Vars[FnLvl];
    for (int i = 0; i < lars.Count; i++) { if (lars[i].Name == Name) return -1; }
    lars.Add(new TVar(Name, 10, 0.0));
    return lars.Count - 1;
  }
  public static int AddArrayIfNec(int FnLvl, string Name, out bool NewOneAdded)
  { NewOneAdded = false;
    // Return its slot, if a duplicate:
    List<TVar> lars = Vars[FnLvl];
    for (int i = 0; i < lars.Count; i++) { if (lars[i].Name == Name) return i; } // with N.O.A. def. of FALSE.
    NewOneAdded = true;
    lars.Add(new TVar(Name, 10, 0.0));
    return lars.Count - 1;
  }

// PUBLIC METHODS:
  // Returns no. of new constant. If a duplicate, returns -1 as error indicator.
  public static int AddConstant(string Name, double Value)
  { return AddScalar(0, Name, 1, Value);
  }
  // Returns no. of new variable. If a duplicate, returns -1 as error indicator.
  public static int AddSystemVariable(int FnLvl, string Name)
  { return AddScalar(FnLvl, Name, 2, 0.0);
  }
  // Returns no. of new variable. If a duplicate, returns -1 as error indicator.
  public static int AddUnassignedVariable(int FnLvl, string Name)
  { return AddScalar(FnLvl, Name, 0, 0.0);
  }
  // Returns no. of new variable. If a duplicate, returns -1 as error indicator; and sets V.Vars[][].IsRefArg a/c to the third argument.
  public static int AddUnassignedVariable(int FnLvl, string Name, bool IsARefArgument)
  { int At = AddScalar(FnLvl, Name, 0, 0.0);
    if (FnLvl > 0 && At >= 0 && IsARefArgument) V.Vars[FnLvl][At].IsRefArg = true;
    return At;
  }
// Add an unassigned array to Vars[]. If already exists, returns -1;
//  otherwise returns the new index of Vars[FnLvl][].
  public static int AddUnassignedArray(int FnLvl, string Name)
  { return AddArray(FnLvl, Name);
  }
// - - - - - - - -
  /// <summary>
  /// Searches only the given function level (NOT level 0 as well). Returns -1, if no find; otherwise returns the index in V.Vars[FnLvl][].
  /// </summary>
  public static int FindVar(int FnLvl, string Name)
  { if (Vars != null) 
    { List<TVar> lars = Vars[FnLvl];
      for (int i = 0; i < lars.Count; i++) { if (lars[i].Name == Name) return i; } 
    }
    return -1; // occurs if Vars is null, if .Count is 0, or if Name not identified.
  }
  public static int FindLiteralValue(double Value)
  { List<TVar> lars = Vars[0];
    for (int i = 0; i < lars.Count; i++)
    { if (lars[i].Use == 1 && lars[i].X == Value) return i; } 
    return -1;
  }
// If FnLvl wrong, will crash. If VarNo wrong, will return "".
  public static string GetVarName(int FnLvl, int VarNo)
  { List<TVar> lars = Vars[FnLvl];
    if (VarNo >= 0 && VarNo < lars.Count) return lars[VarNo].Name;
    else return String.Empty;
  }
// If FnLvl wrong, will crash. If VarNo wrong, will return JM.Unlikely.
  public static double GetVarValue(int FnLvl, int VarNo)
  { List<TVar> lars = Vars[FnLvl];
    if (VarNo >= 0 && VarNo < lars.Count) return lars[VarNo].X;
    else return JM.Unlikely;
  }
/// <summary>
/// For setting scalars only, including ones previously unassigned. Returns 1 for success:  -1 for wrong VarNo (crashes if FnLvl wrong);
/// -2 if var. is a constant. (Arrayhood not tested for; if an array, it will actually set Vars[FnLvl].X uselessly to the supplied value.)
/// </summary>
  public static int SetVarValue(int FnLvl, int VarNo, double ScalarValue)
  { List<TVar> lars = Vars[FnLvl];
    if (VarNo < 0 || VarNo >= lars.Count) return -1;
    byte use = lars[VarNo].Use;
    if (use == 1) return -2;
    lars[VarNo].X = ScalarValue;
    if (use == 0) lars[VarNo].Use = 3;
    return 1;
  }
/// <summary>
/// <para>Cannot be used to reset a system constant! Currently the only use for it is in handling system fn. "__constant(.)", which
///  sets user's constants. (They were set to dummy value 0.0 when first detected by the parser).
///  when the user uses the CONSTANT directive at the very start of the program, before any data lines. This has inherent safety measures.</para>
/// <para>Returns 1 for success, -1 if VarNo is out of range, -2 if the constant is a system constant, -3 if the variable is not already a constant.</para>
/// </summary>
  public static int ResetUserConstant(int VarNo, double ScalarValue)
  { List<TVar> lars = Vars[0];
    if (VarNo < 0 || VarNo >= lars.Count) return -1;
    if (VarNo < P.UserFn[0].NoSysVars) return -2;
    byte use = lars[VarNo].Use;  if (use != 1) return -3;
    lars[VarNo].X = ScalarValue;
    return 1;
  }

/// <summary>
/// Returns Use as a byte, or 254 if VarNo is incorrect. (FnLvl must NOT be incorrect, orthe program will crash.
///  Test omitted because this method is called extremely often, and user code could not raise this error.)
/// </summary>
// If FnLvl wrong, will crash. If VarNo wrong, will return 0xFE.
  public static byte GetVarUse(int FnLvl, int VarNo)
  { List<TVar> lars = Vars[FnLvl];
    if (VarNo >= 0 && VarNo < lars.Count) return lars[VarNo].Use;
    return 0xFE;
  }
// If FnLvl wrong, will crash. If VarNo wrong, will return a negative code. If all well, returns 1.
  public static int SetVarUse(int FnLvl, int VarNo, byte Use)
  { List<TVar> lars = Vars[FnLvl];
    if (VarNo < 0 || VarNo >= lars.Count) return -1;
    else if (lars[VarNo].Use == 1) return -2;
    else {lars[VarNo].Use = Use;  return 1;}
  }

// Returned: >= -1 : the .Ptr value; -2 = error: VarNo out of range.
// If FnLvl wrong, will crash.
  public static short GetVarPtr(int FnLvl, int VarNo)
  { List<TVar> lars = Vars[FnLvl];
    if (VarNo >= 0 && VarNo < lars.Count) return lars[VarNo].Ptr;
    return (short) -2;
  }  
// Re+turns TRUE if no error in VarNo. If FnLvl wrong, will crash.
  public static bool SetVarPtr(int FnLvl, int VarNo, int PtrVal)
  { List<TVar> lars = Vars[FnLvl];
    if (VarNo < 0 || VarNo >= lars.Count) return false;
    lars[VarNo].Ptr = Convert.ToInt16(PtrVal);  return true;
  }
/// <summary>
/// <para>Copies all data from one storeroom to another, EXCEPT for Clients field, ParentAsst and Tag;
///  AND StoreUsage[ToSlot] is NOT set. </para>
///  <para>The third argument prevents copying fields DimCnt, DimSz and TotSz, therefore it should be used with great caution; 
///  but it is safe e.g. if the ToSlot storeroom has just been created by V.GenerateTempStoreRoom(R.Store[FromSlot].DimSz).
///  Note that the Data field is also not recreated if this bool is set, as Data already has the right length.</para>
/// </summary>
  public static void CopyStoreroom(int FromSlot, int ToSlot, bool DontCopySizingData)
  { StoreItem toStoreroom = R.Store[ToSlot], fromStoreroom = R.Store[FromSlot];
    if (!DontCopySizingData)
    { toStoreroom.DimCnt = fromStoreroom.DimCnt;
      fromStoreroom.DimSz.CopyTo(toStoreroom.DimSz, 0);
      toStoreroom.Data = new double[fromStoreroom.TotSz];
    }  
    toStoreroom.IsChars = fromStoreroom.IsChars;
    fromStoreroom.Data.CopyTo(toStoreroom.Data, 0);
 }

/// <summary>
/// Make a complete copy of an array and its storeroom; the copy will have its own storeroom, and the new array will be 
/// registered as its client. The new array's old existence (if it had one) is dereferenced.
/// This one will NOT work with scalars.
/// RETURNS 'true' if no error, 'false' if the 'From' variable has a null storeroom.
/// [Added code, April 2017:] If the source and the destination are the same animal (as happens with "Arr1 = Arr1;"), 'true'
///   is still returned, but no copying happens, for obvious reasons. 
/// </summary>
  public static bool MakeCopy(int FromFn, int FromVarNo, int ToFn, int ToVarNo )
  { if (FromVarNo == ToVarNo  &&  FromFn == ToFn) return true; // and do nothing, as there is no point in copying something to itself.
    TVar toVar = Vars[ToFn][ToVarNo],  fromVar = Vars[FromFn][FromVarNo];
    toVar.Use = fromVar.Use;
    toVar.X   = 0.0;
    R.DereferenceStoreRoom(ToFn, ToVarNo, true, true); // Nothing bad will happen, if To's ptr. was not pointing to a storeroom.
          // 'true' --> don't do anything, if the existing StoreItem object has already been bookmarked (during recursion). 
    int fromslot = fromVar.Ptr;
    StoreItem fromitem = R.Store[fromslot];
    if (fromitem == null) return false;
    int toslot = GenerateTempStoreRoom(fromitem.DimSz);
    toVar.Ptr = Convert.ToInt16(toslot);
    CopyStoreroom(fromslot, toslot, true); // 'true' = don't duplicate work by copying sizing fields set by GenerateTempStoreRoom(.).
    R.Store[toslot].SetClient(ToFn, ToVarNo);  R.StoreUsage[toslot] = 2;
    return true;
  }

/// <summary>
/// Generates all fields of a storeroom, including Data. Client[] is created with all entries (-1,-1).  Slot returns as .I
/// (which is -1 if error). Note that StoreUsage[returned int] will be set to 1 (if no error).
/// Argument DimSizes can omit zero padding at its top end. If DimSizes is being entered as scalars, note that the order must
/// be from the lowest dimension to the highest.
/// ERROR indicated by a return of -1; generated by (a) DimSizes[i] < 0; (b) DimSizes[0] = 0;  (c) DimSizes[i] != 0 but DimSizes[i-j] = 0;
///  (d) too many (or no) dimensions.
/// </summary>
  public static int GenerateTempStoreRoom(params int[] DimSizes)
  { int n, slot = R.NextStoreSlot(); // --> StoreUsage[slot] set to 1; leave it so.
    StoreItem storeroom = R.Store[slot];
    int nodims = 0, totsz = 1;
    int inDimSzLen = DimSizes.Length,  outDimSzLen = TVar.MaxNoDims;
    if (inDimSzLen == 0 || inDimSzLen > outDimSzLen) return -1; // We will detect inDimSzLen of zero below, when testing nodims.
    int[] dimSz = new int[outDimSzLen];
    // We have to check each item in DimSizes, as there could be errors, e.g. a neg. value, or a zero between nonzero elements.
    bool zeroFound = false;
    for (int i = 0; i < inDimSzLen; i++)
    { n = DimSizes[i];
      if (n > 0)
      { if (zeroFound) return -1; // a nonzero dimension occurred after a zero dimension.
        dimSz[i] = n;  totsz *= n;  nodims++;
      }
      else if (n == 0)
      { if (i == 0) return -1; // first dimension can't be zero.
        zeroFound = true;
      }
      else return -1; // negative dimension
    }
    storeroom.DimSz = dimSz;
    storeroom.DimCnt = nodims;
    storeroom.Data = new double[totsz];
    storeroom.ParentAsst = R.AssInstance - 1; // The parent RunAssignment(.) instance increments R.AssInstance after setting
              // its bookmark ThisAssInstance, so we must decrement R.AssInstance to retrieve the bookmark of the parent assignment.
    return slot;
  }

///<summary>
/// Assign a temporary storeroom to the To variable, updating the storeroom's
/// Client field in the process. ALL fields of the To variable will be replaced,
/// except the .Name field. (No checks on To's fields; hopefully attempts to
/// redefine a scalar as an array were picked up elsewhere.)
/// If the To .Ptr was pointing to a storeroom, that storeroom is dereferenced.
///</summary>
  public static void TransferTempArray(int Slot, int ToFn, int ToVarNo)
  { TVar toVar = Vars[ToFn][ToVarNo];
    toVar.Use   = 11;
    toVar.X     = 0.0;
    R.DereferenceStoreRoom(ToFn, ToVarNo, true, true); // Nothing bad will happen, if To's ptr. was not pointing to a storeroom.
          // 'true' --> don't do anything, if the existing StoreItem object has already been bookmarked (during recursion). 
    toVar.Ptr   = Convert.ToInt16(Slot);
    // Update Client field:
    R.Store[Slot].SetClient(ToFn, ToVarNo);
    R.StoreUsage[Slot] = 2; // assigned-storeroom code
  }
  public static void NeuterTheArray(int Fn, int VarNo)
  { R.DereferenceStoreRoom(Fn, VarNo, true, true); // Nothing bad will happen, if To's ptr. was not pointing to a storeroom.
          // 'true' --> don't do anything, if the existing StoreItem object has already been bookmarked (during recursion). 
    TVar vary = Vars[Fn][VarNo];
    vary.Use = 10;
    vary.X = 0.0;
    vary.Ptr = -1;
    // The name remains unaltered.
  }
// Remove temporary storerooms (which have StoreUsage[] set to 1), BUT ONLY if .ParentAsst = OnlyFromThisAsstInstance.
// ButNotThisOne will be spared. Set it to a neg. no., if none to be spared.
  public static void CleanStore(int ButNotThisStore, int OnlyFromThisAsstInstance)
  { int storeCount = R.Store.Count;
    for (int i = 0; i < storeCount; i++)
    { if (i == ButNotThisStore) continue;
      if (R.StoreUsage[i] == 1  && R.Store[i].ParentAsst == OnlyFromThisAsstInstance)
      { R.Store[i].Demolish();  R.Store[i] = null;  R.StoreUsage[i] = 0; } //null storeroom.
  } }  
  ///<summary>
  /// <para>RETURNED: If no errors, .B is TRUE and .S holds the string to display; otherwise .B is FALSE and .S holds the error message.</para>
  /// <para>InsertTags - if true, formatting tags (you know, things like "&lt;B>") go in.</para>
  /// <para>ShowRowNos - if true, whatever has rows (i.e. matrices and higher structures) has its rows prefixed with row no.</para>
  /// <para>ShowElementNos - if true, "[n]" precedes every value, where n is the ABSOLUTE address in the array.</para>
  /// <para>FmtStr[0] - if present and has a format string recognized by C# (like "F4"), sets numerical format accordingly.
  ///   For default (no prescribed format), use "".</para>
  /// <para>FmtStr[1] - if present and nonempty, sets the delimiter to that string, instead of the default ", ".</para>
  /// <para>FmtStr[2] - if present and nonempty, causes its contents (e.g. a tab) to follow e.g. "[0,0]" rather than 2 spaces (the default).</para>
  /// <para>FmtStr[3] - Normally, the display will be either of numerals or letters according to the .IsChars rating of the array.
  /// But if this arg. is present and nonempty, and begins with one of C,c,N,n, then this default behaviour is overruled;
  /// 'C'/'c' forces char. display, and 'N'/'n' forces numeric display.</para>
  /// <para>FmtStr[4] - if present and one integer, or a set of monotonically increasing integers sep'd by commas, taken as tabs.
  ///  obviously this will only have effect if the delimiter was set to be (or contain) a tab. Default is empty string (no settings).</para>
  /// </summary>
  public static Boost StoreroomDataFormatted(int slot, bool InsertTags, bool ShowRowNos, bool ShowElementNos, params string[] FmtStr)
  { if (slot < 0) return new Boost(false, "scalar supplied instead of an array");
    StoreItem story = R.Store[slot];
      // No need to copy the data array, as the method called does not alter it.
    int[] dimsz = story.DimSizes._Copy();
    return StoreroomDataFormatted(story.Data, dimsz, null, null, story.IsChars, InsertTags, ShowRowNos,
                                                 ShowElementNos, FmtStr);
  }

  ///<summary>
  /// <para>RETURNED: If no errors, .B is TRUE and .S holds the string to display; otherwise .B is FALSE and .S holds the error message.</para>
  /// <para>DimSizes must be exactly the dimensions of all of TheData, and so must have a length of at least 1, and no zeroes or negatives.</para>
  /// <para>LowLimits - lowest index to be displayed for each dimension. Shortfall (including NULL) made up with zeroes; excess length ignored.</para>
  /// <para>BeyondHighLimits - the highest value displayed for dimension i will be one less than BeyondHighLimits[i].
  ///   Shortfall (including NULL) made up with values in DimSizes; excess length ignored.</para>
  /// <para>InsertTags - if true, formatting tags (you know, things like "&lt;B>") go in.</para>
  /// <para>ShowRowNos - if true, whatever has rows (i.e. matrices and higher structures) has its rows prefixed with row no.</para>
  /// <para>ShowElementNos - if true, "[n]" precedes every value, where n is the ABSOLUTE address in the array.</para>
  /// <para>FmtStr[0] - if present and has a format string recognized by C# (like "F4"), sets numerical format accordingly.
  ///   For default (no prescribed format), use "".</para>
  /// <para>FmtStr[1] - if present and nonempty, sets the delimiter to that string, instead of the default ", ".</para>
  /// <para>FmtStr[2] - if present and nonempty, causes its contents (e.g. a tab) to follow e.g. "[0,0]" rather than 2 spaces (the default).</para>
  /// <para>FmtStr[3] - Normally, the display will be either of numerals or letters according to the .IsChars rating of the array.
  /// But if this arg. is present and nonempty, and begins with one of C,c,N,n, then this default behaviour is overruled;
  /// 'C'/'c' forces char. display, and 'N'/'n' forces numeric display.</para>
  /// <para>FmtStr[4] - if present and one integer, or a set of monotonically increasing integers sep'd by commas, taken as tabs.
  ///  obviously this will only have effect if the delimiter was set to be (or contain) a tab. Default is empty string (no settings).</para>
  /// </summary>
  public static Boost StoreroomDataFormatted(double[] TheData, int[] DimSizes, int[] LowLimits, int[] BeyondHighLimits,
                                             bool AsChars,  bool InsertTags, bool ShowRowNos, bool ShowElementNos,  params string[] FmtStr)
  { Boost result = new Boost(false);
    string fmtstr = "";          if (FmtStr.Length > 0 && FmtStr[0] != "") fmtstr = FmtStr[0];
    string initialDelimr = "  "; if (FmtStr.Length > 2 && FmtStr[2] != "") initialDelimr = FmtStr[1];
    int datalen = TheData._Length();
    if (datalen == -1) return new Boost(true, "NULL");   else if (datalen == 0) return new Boost(true, "EMPTY");
    // Check out DimSizes:
    int noDims = DimSizes._Length();  if (noDims < 1) return new Boost(false, "null or empty argument 'DimSizes'");
    int totSz = 1;
    for (int i=0; i < noDims; i++)
    { if (DimSizes[i] < 1) return new Boost(false, "zero or negative dimension found in 'DimSizes'");
      totSz *= DimSizes[i];
    }
    if (totSz != datalen)
    { string sss = "'DimSizes' fits for a total data size of " + totSz.ToString() + ", but TheData has length " + datalen.ToString();
      return new Boost(false, sss);
    }
    // Ensure that LowLimits has the right length; expand it with zeroes, if short, and amputate it, if long:
    int[] lowLimits = null, beyondHighLimits = null; // We will use these so that the argument arrays are shielded from changes.
    int n = LowLimits._Length();
    if (n == noDims) lowLimits = LowLimits._Copy();
    else if (n == -1) lowLimits = new int[noDims]; // Leave its contents as all zeroes.
    else if (n < noDims)
    { lowLimits = new int[noDims];
      for (int i = 0; i < n; i++) lowLimits[i] = LowLimits[i];
    }
    else if (n > noDims) lowLimits = LowLimits._Copy(0, noDims);
    // ...and the input argument 'LowLimits' will not be referenced from here down.
    // Ensure that BeyondHighLimits has the right length; expand it with values from DimSizes, if short, and amputate it, if long:
    n = BeyondHighLimits._Length();
    if (n == noDims) beyondHighLimits = BeyondHighLimits._Copy();
    else if (n == -1) beyondHighLimits = DimSizes._Copy();
    else if (n < noDims)
    { beyondHighLimits = DimSizes._Copy();
      for (int i = 0; i < n; i++) beyondHighLimits[i] = BeyondHighLimits[i];
    }
    else if (n > noDims) beyondHighLimits = BeyondHighLimits._Copy(0, noDims);
    // ...and the input argument 'BeyondHighLimits' will not be referenced from here down.
    // Ensure that the limits are possible to satisfy:
    for (int i=0; i < noDims; i++)
    { if (lowLimits[i] >= beyondHighLimits[i]) return new Boost(false, "Display limits are impossible to fulfill"); }
    // Formatting presets:
    if (FmtStr.Length > 3 && FmtStr[3] != "")
    { char c = FmtStr[3][0];  c = Char.ToUpper(c); // Allowed arg. values were "C", CF" (honour tags) and "N".
      if (c == 'C') AsChars = true;  else if (c == 'N') AsChars = false; // otherwise leave AsChars with its default setting as above.
    }
    string delimr = ", ";   if (AsChars) delimr = ""; // Absence of delimiter is only overruled for chars. if FmtStr[1] is set.
                                 if (FmtStr.Length > 1 && FmtStr[1] != "") delimr = FmtStr[1];
    string tabstops = "";
    if (FmtStr.Length > 4  &&  !AsChars)
    { string ss = FmtStr[4]._Purge();
      string[] striggle = ss.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
      Quad outcome;
      striggle._ParseIntArray(out outcome); // covers empty 'striggle'
      if (outcome.B) tabstops = "<stops " + ss + ">";
    }
    result.S += tabstops;
    bool IsStructured = (noDims > 1);
    // SET THE DATA RANGE:
    if (noDims > 2) // display a line consisting of "[i, j, ..., m]":
    { if (InsertTags) result.S += "<# MAGENTA>";
      result.S += "[" + IntArrayValuesInverted(lowLimits, 0, lowLimits.Length - 1, ", ") +  ']';//index of 1st.datum.
      result.S += '\n';
    }
    int[] RunningDims = lowLimits._Copy(); // The lowest dimension will not be incremented, and will remain zero
    // If necessary, put column nos. across the top of the window:
    int lolimit = lowLimits[0],   rowshowsize = beyondHighLimits[0] - lolimit;
    int offset; // a counter, updated at the end of every row.
    // Show a top line of col. nos., but only if the delimiter is/contains a TAB, AND col. nos. are not to precede values in rows.
    if (delimr.IndexOf('\t') >= 0 && !ShowElementNos)
    { result.S += "<# magenta>";
      if (IsStructured) result.S += "        "; // This takes the first column no past the 1st. line's prefix"[0]". *** Sadly, font-dependent for now.
      int lo = lowLimits[0], hi = beyondHighLimits[0]-1;
      for (int i = lo; i <= hi; i++)
      { result.S += "<u>" + i.ToString() + "</u>";
        if (i < hi) result.S += delimr;
      }
      result.S += '\n';
    }
    while (true)
    { // We deal with whole rows as a block, not worrying about RunningDims[0].
      // So initialize the row:
      string thisrow = "";
      if (ShowRowNos && noDims > 1)
      { if (InsertTags) thisrow += "<# 0x00CACA>"; // "Sky". (Light greenish blue.)
        thisrow += "[" + RunningDims[1].ToString() + ']' + initialDelimr;
      }
      if (InsertTags) thisrow += "<# BLUE>";
      // insert all the data of the row:
      offset = NoValuesToHere(DimSizes, RunningDims);
      if (AsChars) // then do the whole row at once:
      { thisrow += TheData._Copy(offset, rowshowsize)._ToCharString('?','?','?'); // Replace all types of non-unicodes with '?'}
        thisrow = thisrow.Replace('\u0000', ' '); // Char. 0 makes subsequent rows disappear
      }
      else
      { for (int val = 0; val < rowshowsize; val++)
        { if (ShowElementNos) thisrow += "<# ORANGE>[" + (lolimit + val).ToString() + "]<# BLUE>";
          thisrow += TheData[offset + val].ToString(fmtstr);
          if (val < rowshowsize-1) thisrow += delimr;
        }
      }
      // finish the row off:
      if (AsChars) thisrow += "\n";  else thisrow += ";\n";
      if (!IsStructured) // Then the job is now done, so go home:
      { thisrow += '\n';
        if (InsertTags) thisrow += "<# BLACK>";
        result.S += thisrow;  result.B = true;  return result;
      }
      // To get here, we have to have a structured array.
      // increment RunningDims as if it were an odometer:
      bool tisTheEnd = false;
      for (int i=1;  i < noDims; i++) // We never touch RunningDims[0], since we handle whole rows, not individual column data.
      { RunningDims[i]++;
        if (RunningDims[i] >= beyondHighLimits[i])
        { RunningDims[i] = lowLimits[i];   if (i == noDims-1) tisTheEnd = true; }
          else break;
      }
      // Is it the end of a matrix (or of a submatrix of a higher structure?)
      if (RunningDims[1] == lowLimits[1])
      { // Have we come to the end of the structure?
       if (tisTheEnd)
        { thisrow += '\n';
          if (InsertTags) thisrow += "<# BLACK>";
          result.S += thisrow;  result.B = true;  return result;
        }
        // To get here we have to have a structure higher than a matrix, and not be yet at its end:
        // Prepare the running indexes block "[i,j,k]" for the next structure:
        if (InsertTags) thisrow += "<# MAGENTA>";
        thisrow += "[" + IntArrayValuesInverted(RunningDims, 0, noDims-1, ", ") + "]\n";
      }
      result.S += thisrow;
    }
   // The only exit out of the above is via 'return', so no code after the 'while' loop.
  }
  /// <summary>
  /// Intended as a utility for StoreroomDataFormatted.
  /// </summary>
  public static string IntArrayValuesInverted(int[] IntArray, int StartPtr, int EndPtr, string Delimiter)
  { string result = "";
    for (int i = EndPtr; i >= StartPtr; i--)
    { result += IntArray[i].ToString();
      if (i > StartPtr) result += Delimiter;
    }
    return result;
  }
  /// <summary>
  /// Intended as a utility for StoreroomDataFormatted. NO error checking. ThisElement must have length = no. of valid dimensions,
  /// and DimRanges must be at least as big (higher values - which would be the zeroes of virtual dimensions - being ignored).
  /// </summary>
  public static int NoValuesToHere(int[] DimRanges, int[] ThisElement)
  { int result = 0;
    int nodims = ThisElement._Length(), blocksize = 1;
    for (int i=0; i < nodims; i++)
    { result += ThisElement[i] * blocksize;
      blocksize *= DimRanges[i];
    }
    return result;
  }
// The following is intended for saving of variables; hence the two FALSE args. in the final line (fn. call) - no format tags,
//  no matrix row nos. (but 3D+ structures will have their matrix components numbered).
  public static Boost ArrayDataFormatted(int FnLvl, int VarNo)
  { Boost result = new Boost(false);
    if (VarNo < 0 || VarNo >= Vars[FnLvl].Count) { result.S = "wrong variable reference"; return result; }
    short slot = Vars[FnLvl][VarNo].Ptr;
    return StoreroomDataFormatted(slot, false, false, false);
  }


}


//si--//storeitem----------------------------------------------------------------------------
public class StoreItem
{ public int DimCnt; // no. of dimensions of the matrix.
  public int[] DimSz; // sizes of dimensions. Size fixed: = TVar.MaxNoDims.
    // Unused higher dims. have DimSz of 0. (0 is NOT allowed between dimensions.)
    public int[] DimSizes // Same as DimSz, but with all zero higher dims removed; e.g. if DimSz is { 2, 3, 0, ... } DimSizes is { 2, 3 }.
    { get { int[] result = new int[DimCnt];  for (int i=0; i < DimCnt; i++) result[i] = DimSz[i];  return result;  }  }
  public int ClientFn, ClientVarNo; // Identities the V.Vars item currently pointing to the storeroom.
  public double[] Data; // the actual array data.
  public bool IsChars;
  public int ParentAsst; // at birth, a store item receives here the value of ThisAssInstance  for the instance
                         // of RunAssignment(.) during which the birth occurred. Set to -1 by constructors.
  public object Tag; // Only intended for short term use; NOT set by constructors,
  //  and NOT passed to other StoreItem objects by any copying method. RECORD HERE any 
  //  uses as added. Currently none, actually.

  public static int ClientBkMkIncrement = 10000; // on ascending recursion, is added to the store item's ClientFn field.

   //Constructor creates Clients, but leaves Data NULL.
  public StoreItem(int Dummy)
  { DimCnt = 0;//should be immediately changed to a nonzero value after creation.
    DimSz = new int[TVar.MaxNoDims];  
    ClientFn = -1;  ClientVarNo = -1;
    Data = null;  IsChars = false;
    ParentAsst = -1;
  }
  public int TotSz 
  { get { if (Data == null) return -1; else return Data.Length; } 
  }

  public override string ToString()
  { string ss = "TotSz: " + TotSz.ToString();
    ss += ";  DimCnt: " + DimCnt.ToString();
    ss += ";  DimSz: " + DimSz._ToString(", ");
    ss += ";  Client: " + ClientFn.ToString() + ", " + ClientVarNo.ToString();
    ss += ";  IsChars: " + IsChars.ToString();
    ss += ";  ParentAsst: " + ParentAsst.ToString();
    ss += ";  \nData: ";
    if (Data == null) ss += "NULL";
    else
    { int n = TotSz; if (n > 10) n = 10;
      double[] xx = new double[n];
      for (int i = 0; i < n; i++) xx[i] = Data[i];
      ss += xx._ToString(", ");
      if (TotSz > 10) ss += "...";
    }
    return ss + '\n';
  }

  public void Demolish() // Nulls storeroom; before re-use, use "new StoreItem(0)".
                // NB: DON'T FORGET to set StoreUsage[i] to 0. Can't do it here.
  { DimCnt = 0;  IsChars = false;
    DimSz = null; ClientFn = -1;  ClientVarNo = -1;  Data = null;
    ParentAsst = -1;  Tag = null;
  }
/// <summary>
/// Returns with ClientFn in .Y and ClientVarNo in .X
/// </summary>
  public Duo GetClient()
  { return new Duo(ClientVarNo, ClientFn); }
/// <summary>
/// <para>No test for FnLvl and ArrNo referring to a recorded array.</para>
/// <para>DON'T FORGET to set R.StoreUsage[slot] appropriately at each use. (Can't do it here.)</para>
/// </summary>
  public void SetClient(int FnLvl, int ArrNo)
  { ClientFn = FnLvl;  ClientVarNo = ArrNo; }
/// <summary>
/// <para>No test for the argument referring to a recorded array.</para>
/// <para>DON'T FORGET to set R.StoreUsage[slot] appropriately at each use. (Can't do it here.)</para>
  public void SetClient(Duo XisVarNo_YisFn)
  { ClientFn = XisVarNo_YisFn.Y;  ClientVarNo = XisVarNo_YisFn.X; }

/// <summary>
/// <para>No test for FnLvl and ArrNo referring to a recorded array.</para>
/// </summary>
  public void UnSetClient()
  { ClientFn = -1;  ClientVarNo = -1; }
/// <summary>
/// <para>Confirm that the client recorded in the StoreItem is indeed that of the arguments.</para>
/// </summary>
  public bool ConfirmClient(int FnLvl, int ArrNo)
  { return (ClientFn == FnLvl && ClientVarNo == ArrNo); }
/// <summary>
/// <para>During advancing recursion, ClientFn has the large integer 'ClientBkMkIncrement' added to it,
/// to preserve the slot from destruction before it is reclaimed by descending recursion.</para>
/// <para>Does nothing if ClientFn is already bookmarked.
/// </summary>
  public void BookmarkClient(int ThisFn)
  { if (ClientFn < ClientBkMkIncrement) ClientFn += ClientBkMkIncrement;
  }
/// <summary>
/// <para>During descending recursion, ClientFn has the large integer 'ClientBkMkIncrement' subtracted from it,
/// to redeem the slot for reclamation by an array of this level.</para>
/// <para>If there has been no such increment, nothing happens and no error is raised. (This would be the case for REF arguments.)</para>
/// </summary>
  public void UnBookmarkClient(int ThisFn)
  { if (ClientFn > ClientBkMkIncrement) ClientFn -= ClientBkMkIncrement;
  }

}
//-----------------------------------------------------------------------------
public struct Recur // used for storage of recursive params. *** NB: If ever adding strings
// or other fields not stored simply on the stack, you MUST stop simple '=' assignments to Recur objects,
// and will have to write a copying method here.
{ public int topdog1, victim1, assno1;
  public bool buryatend1, eq_serviced1;

  public Recur(int topdog, int victim, int assno, bool buryatend, bool eq_serviced)
  { topdog1 = topdog; victim1 = victim; assno1 = assno; buryatend1 = buryatend; eq_serviced1 = eq_serviced; }
  public override string ToString()
  { string ss = "topdog1 = " + topdog1.ToString() + "; victim1 = " +
     victim1.ToString() + "; assno1 = " + assno1.ToString() + "; buryatend1 = " +
     buryatend1.ToString() + "; eq_serviced1 = " + eq_serviced1.ToString();
     return ss;
  }
}

public class R             //run
{
	private R() {} // Cannot instantiate this class.
//==============================================================================
//  PARAMETERS
//------------------------------------------------------------------------------
//store
  public static List<StoreItem> Store; // USER ARRAY DATA.
  public static List<byte> StoreUsage; // For each Store slot holding data:
    // 0 = unused, arrays null, other fields zero;  1 = temp. storeroom (which has fields and arrays set, but client is [Fn = -1, VarNo = -1] );
    // 2 = storeroom of an array registered in Vars[][].
    // Store and StoreUsage[] are setup once only, at startup (MainWindow).
  public static int StuffupSite = -1; // set by any error detected during Run(.).
     // Nec. because Run(.) is called by indirect recursions (with a layer of
     // RunAssignment(.) in between successive recursions). It would be too
     // error-prone and involve too much code to pass the value through RunAsst.
     // Reset each time by MainWindow, at 'GO' click.
  public static List<TTerm[]> Assts; // CODED ASSIGNMENTS STORAGE -- each list element is an array of terms.
  public static TBubble[][] FlowLine; // [function no][bubble no.].
  // FlowLine controls:
  public static int DummyMarker = -1,  IFMarker = -10,   CONTMarker = -20,
                    BREAKMarker = -30, BACKIFMarker = -40,
                    ENDIFMarker = -50 /*1*/, PAUSEMarker = -60 /*2*/ ;
  // Indicators for the .FnRT field:
  public static short TempArrayCode = -500; // indicates a temporary array.
  public static short TempScalarCode = -600; // scalar results of numbercrunching become this, 
     //  as they no longer correspond to a reference in V.Vars. Intended for filling of F.REFArgLocn[n].Y
     //  for passing to functions, so they know that the argument is not a named assigned scalar.
   // FlowLine .Next values:
  public static int EXITNext = -100, RETURNNext = -200;
  public static List<TVar>[] FStack; // stack for variables between recursions. One list per function.
  public static List<Duo>[] ClientStack; // parallel stack for arg. sources, one per arg.
  public static List<Recur>[] RStack; // stack for RunAssignment(.) params between recursions. Sized as above.
  public static bool ComingFromRecursion;
  public static int OrigLoopTestTime = 100; // After this many loops (i.e. total
    // loopings, distributed around the program), a check is made to see if the
    // user is fed up and wants to stop the program. (This value produced no
    // detectible time increase; when it is 1, time increase is marked.)
  public static int LoopTestTime = OrigLoopTestTime;
    // Code outside the class is allowed to set R.LoopTestTime (e.g. to 1, so that the very
    //  next loop will result in a test of MainWindow.StopNow); the loop itself will always immediately
    //  reset ot to OrigLoopTestTime (in case the program is resumed after holding). In any case, 
    //  starting the user pgm. always sets LoopTestTime to OrigLoopTestTime.
  public static int Loopy = 0; // Incremented and compared at each loop with the
    // above; on exceeding it, interrupt request is checked for, and handled if
    // present; if no interrupt request, Loopy is reset to 0, and pgm. continues. (After the first 'while' loop, the
    // entry value of Loopy is unpredictable, depending on how the last loop left it.)
  public static Quad ErrorTrap; // .B TRUE if errors arising from system functions
    // are to be trapped (rather than being allowed to crash the program). If so,
    // .X is the value to be returned by each error arising from a system 
    // function call, .S is the cumulative error message, and .I is the number of
    // errors trapped to date.
 // External variables - I/O from any shell that is encapsulating MiniMath. The forward
 //  variables (the first two below) are NOT initialized or reset by MiniMath. It is up 
 //  to the shell to do so. The back variables (last two) are set by the system function
 //  which is invoked by the shell; otherwise they also are NOT initialized or reset.
  public static double[] ExtScalar;
  public static double[][] ExtArray;  
  public static List<double> ExtScalarBack;
  public static List<double[]> ExtArrayBack;
  public static int ExtFnNo; // number of the function in the MiniMath program which is
                             // to be invoked on behalf of the shell. (Fns. are as numbered 
                             // by MiniMath: 1 = 1st. non-main fn; numbered in order of occurrence.)
  public static int ThisFn = -1, ThisBubble = -1; // bookmarks for occasional use, e.g. to locate a 'pause(.);' in effect.
  public static int LatestAssignment = -1; // Each time "RunAssignment(AssNo)" is called, this is set to AssNo; it remains set till any next such call.
  public static int AssInstance = 0; // Incremented by every instance of a RunAssignment(.) run, enabling recognition of that
  // instance even though multiple recursions may have occurred during its processing. Reset by MainWindow's 'GO()'.
  // (Int32 allows for 2.1 billion instances of RunAssignment(.) in a single run - should be enough for now.)
  public static short SubstituteFn1 = 0, SubstituteAt1 = -1; // Set by system fn. 'setfunc(1, .)'; when at runtime the fn. 'func1()'
  // is detected, these values will be substituted for it. (The substitute can be a system or a user function.)
  // The test for nonexistence is that SubstituteFn1 is zero (instead of system fn. marker -20 or user fn. marker -30).
  // Rest to above defaults by R.KillData() (as for the those in the next line).
  public static short SubstituteFn2 = 0, SubstituteAt2 = -1; // As above; set by system fn. 'setfunc(2, .)'; as above for 'func2()'.
  public static System.Collections.Queue UserFnLog; // Each element is a Trio: X = called fn., Y = AssNo, Z = UserFnLogEntryNo.
  public static int UserFnLogSize = 40; // UserFnLog starts with no entries, and grows till it reaches this size.
  public static int UserFnLogEntryNo = 0; // Stored in UserFnLog at each call. To base 1.
  public static int CurrentlyRunningUserFnNo; // Set by each call to "Run(.)", and not altered anywhere else.
  public static int[] UserFnCalls = null; // Initialized early in C.SetUpFunctions(.). Whenever user fn. n is called, UserFnCalls[n] is incremented.

  //IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII

// MAIN METHOD CALLED FROM OUTSIDE    //run
//==============================================================================
/// <summary>
/// If no error, returns with result.B true, and (a) for scalar return, value in .X, -1 in .I; for array return, 0 in .X, store item in .I.
/// If a run-time error or abort by user at a break point, .B is FALSE and .S has the info. For errors, .I holds the line no. and .X is indeterminate;
///  for break point interruption, .I is -(100 + line no.). [The offset allows future revisions to return neg. codes from -2 to -100.]
/// </summary>
  public static Quad Run(int FnNo)
  { Quad result = new Quad(false), assoutcome = result;
    bool lastwasIF = false;   char stopnow = ' ';
    ErrorTrap = new Quad(false); // no error trap.
    CurrentlyRunningUserFnNo = FnNo; // Allows methods called directly and indirectly from here to know which user fn. is being processed.
    if (FnNo > 0) // Reset all local user-variables that persist from the last call to this function (as either 3 or 10).
                  // (This doesn't reset system variables like "__d1" which have use != 3, but they are never addressed before assigned, so no problem.) 
    { List<TVar> tvarList = V.Vars[FnNo];
      int noArgs = P.UserFn[FnNo].MaxArgs + P.UserFn[FnNo].NoSysVars,  tvarListCount = tvarList.Count;
      for (int i=noArgs; i < tvarListCount; i++) // skip the arguments, which are first on the list.
      { TVar tvar = tvarList[i];
        int use = tvar.Use;
        if (use == 3) tvar.X = 0.0; // scrub its old value, but retain its Use and Ptr (of -1). Important e.g. for "import scalar",
                                    // where Use is set at parse time, so that it is not seen as an unassigned variable during runtime.
        // Any arrays with use 10 are left as is. (There should be none with use 11, as all assigned arrays where unassigned at last fn. run.)
      }
    }  
    MainWindow winnow = MainWindow.ThisWindow; // Maybe this will speed things up a tad, in the break points check below?
        // (I tried using a List<int> alias here for main window's PgmBreakPoints, but ' = ' gives you a full copy, not a pointer alias!)
    int nxt, assno,  bub = 0;
    TBubble bubbly;
    while (true)
    { bubbly = FlowLine[FnNo][bub];
      nxt = bubbly.Next;
      assno = bubbly.Ass;
      // With a 1000 x 1000 double-loop I could not demonstrate a consistent time cost to including the line below, if there are no
      //  break points; if 1 (not met with during the test run), 12% time penalty; if 3 points, 15%.
      if (winnow.PgmBreakPoints.Count != 0 && winnow.PgmBreakPoints.IndexOf(bubbly.Ref) >= 0)
      { bool abortclicked = MainWindow.HoldingPattern(100 + bubbly.Ref); // waits locked in this method till 'GO' or 'ABORT' clicked.
        if (abortclicked) { result.I = -(100 + bubbly.Ref);  result.B = false; result.S = ""; return result; }
      }
      ThisBubble = bub; // class field, for external reference.
      ThisFn = FnNo; // class field, for external reference. Has to go here, not above, as FnNo can change inside the loop
                     // (when one fn. calls another).
      // CHECK FOR STOP REQUEST, if this is the end of a loop:
      if (assno == BACKIFMarker)
      { Loopy++;
        if (Loopy > LoopTestTime)
        { Loopy = 0;  LoopTestTime = OrigLoopTestTime; // it may have been temporarily set low to trigger, as e.g. with fn. show(.).
          stopnow = MainWindow.CheckStopNow();
          if (stopnow != ' ')
          { char ch = MainWindow.StopNow;
            MainWindow.StopNow = ' ';  // first action, before any recursion can occur.
            string WhatToDo = AskWhatToDo(ch);
            if (WhatToDo == "stop")
            { result.S = "program interrupted by user";
              result.I = FlowLine[FnNo][bub].Ref; return result; }
            else if (WhatToDo == "hold")
            { bool abortclicked = MainWindow.HoldingPattern(-1);  // after call, will be returned to here.
              if (abortclicked) // then holding pattern exited by 'ABORT' btn rather than by second click of 'GO':
              { result.S = "program interrupted by user";
                result.I = FlowLine[FnNo][bub].Ref; return result;
              }
              // just continue with the Loopy loop, if 'GO' was clicked.
            }
          }
        } 
      }     
    // No call for stopping, so process .Ass:
// PROCESS THE .ASS FIELD:
     // IF marker (.Ass = -10):
      if (assno == IFMarker) lastwasIF = true;
     // ASSIGNMENT marker (.Ass >= 0):
      else if (assno >= 0)
      { assoutcome = RunAssignment(assno, nxt);
        if (!assoutcome.B)
        { result.S = assoutcome.S;
          if ( assoutcome.I < -1) result.I = assoutcome.I; // a new-run request
          else // for a humble error, .I returns the site of the error (should always be >= 0):
          { if (StuffupSite == -1) StuffupSite = FlowLine[FnNo][bub].Ref;
            result.I = StuffupSite; // passed on from an earlier recursion.
          }  
          return result;
        }
      }
      // None of the other negative dummy markers have any impact on flow line
      //  running; their application is in parse time, in developing FlowLine.
      //  However, for the time being we check for wierdo values...
      // **** REMOVE THIS TEST after sufficient testing experience:
      else // a negative .Ass, but not the IF marker:
      { if (assno != PAUSEMarker)
        { if (assno!=DummyMarker && assno!=CONTMarker && assno!=BREAKMarker
                 && assno!=BACKIFMarker && assno!=ENDIFMarker)
          { result.S = "pgm. error - unrecognized .Ass code: " + assno.ToString();
            return result; }
        }    
      }
// PROCESS THE .NEXT FIELD:
      // Check for specially preset .Next:
      if (nxt == EXITNext)
      { if (FnNo == 0) break; // PROPER EXIT for main pgm. only.
         // Fns. always have an EXITNext at the end of the flow line, like the
         //  main program; but it should never be reached for functions.
        else
        { result.S = "path through the function ended without a 'return'";
          result.I = FlowLine[FnNo][bub].Ref; return result;
      } }
      else if (nxt == RETURNNext)
      { if (FnNo > 0)
        { if (assno >= 0)  // result .Next is sitting on
                         // the bottom of a bubble with an assignment at its top.
          { result.X = assoutcome.X;
            result.I = assoutcome.I;
            if (result.I >= 0) // then return the array as a temporary array
            { Store[result.I].UnSetClient();
            }
          }
          else {result.X = 0.0;  result.I = -1; } // a void return.
          // Deal with any REF args, which may be scalar or array.
          for (int i=0; i < P.UserFn[FnNo].MinArgs; i++) // optional arguments can't be REF arguments, hence 'MinArgs'.
          { if (P.UserFn[FnNo].PassedByRef[i]) // then a REF variable, ?array, ?scalar:
            { int ndx = ClientStack[FnNo].Count - P.UserFn[FnNo].MaxArgs + i;
              Duo duey = ClientStack[FnNo][ndx];
              int fn = duey.Y, att = duey.X;
              bool oops = false;
              if (fn <= -1) oops = true; // This oops detects both arrays and scalars without a variable name.
              else
              { int v_use = V.GetVarUse(fn,att);
                if (v_use < 3) oops = true; // can't change system constants
                // SCALAR REF args:
                else if (v_use == 3)   
                {// Retrieve the value of its local alias:
                  double x = V.GetVarValue(FnNo, P.UserFn[FnNo].ArgRefs[i]);
                  // Set the calling code's scalar to this value: 
                  V.SetVarValue(fn, att, x);
                }
                // ARRAY REF args:
                else if (v_use == 11) // Ignore the case where the alias has been dereferenced for whatever reason.
                { int varno = P.UserFn[FnNo].ArgRefs[i];
                  short newslot = V.Vars[FnNo][varno].Ptr;
                  if (newslot < 0) throw new Exception("Disaster has struck in R.Run, where RETURNNext handled, for REF arrays");
                  StoreItem newSitem = R.Store[newslot];
                  int oldslot = V.Vars[fn][att].Ptr;
                  // Note that many operations on the array within the function will change the StoreItem; the calling code's instance
                  //  of the array needs to know about this. The test for such a case is that 'newslot' and 'oldslot' will be different.
                  if (newslot != oldslot) V.Vars[fn][att].Ptr = newslot;
                  // Now set the client of the store item (whether the pointer is new or unaltered) back to its value when the function was entered:
                  newSitem.ClientFn = fn;   newSitem.ClientVarNo = att;
                 // Now dereference the in-function alias variable.
                  V.Vars[FnNo][varno].Ptr = -1;
                  // Now the alias can be destroyed along with all the other unwanted arrays of the fn.
                }
              }
              if (oops)
              { result.S = "function '" + P.UserFn[FnNo].Name + "': REF argument '" + P.UserFn[FnNo].ArgNames[i] 
                                                                            + "' not supplied with a named variable.";
                result.I = FlowLine[FnNo][bub].Ref; return result;
              }
            }
          }  
          // Remove all local arrays except the temporary one (if any) being returned:
          DemolishFnArrays(FnNo, result.I);
          break;
        }
        else
        { result.S = "'return' not allowed in the main pgm. Use 'exit'.";
          result.I = FlowLine[FnNo][bub].Ref; return result;
      } }

      // Check whether we should be using .IfNot instead of .Next:
      if (lastwasIF && assno != IFMarker)//i.e. don't reprocess the IF marker!
      { lastwasIF = false;
        if (assoutcome.X == 0.0) nxt = FlowLine[FnNo][bub].IfNot;
      }
      // In all cases, 'nxt' should now point to the next bubble to loop with.
      // Check that it is OK:
      if (nxt < 0 || nxt >= FlowLine[FnNo].Length)
      { result.S = "Ghastly flowline error!";
        result.I = FlowLine[FnNo][bub].Ref; return result;
      }
      // OK, we trust it...
      bub = nxt;
    } // END OF WHILE loop through bubbles
    result.B = true;  return result;
  }


//==============================================================================
// OTHER METHODS
//------------------------------------------------------------------------------
//######################### add comment re .I and re-running...
// If no error, .B is TRUE and .X and .I are the values for the '=' operation.
//  (Scalar: .I = -1, .X is data; Array: .I is storeroom ptr, .X is 0.)
// Error returns .B and .S set as usual.
  public static Quad RunAssignment(int AssNo, int Bubble_Next) //runasst
  { LatestAssignment = AssNo;
    int ThisAssInstance = AssInstance;   AssInstance++; // Gives a unique instance no. for each run of this fn. (to distinguish recursion)
    Quad quid, result = new Quad(false);  result.X = 0.0;
    int n, p, noargs, toprating, topdog, victim;
    short fn_fld, at_fld;  byte topcode;  byte buy, topuse, vicuse;
    bool eq_serviced, buryatend;   double victimX;
    short callfn1 = (short) F.AliasFn1Index,  callfn2 = (short) F.AliasFn2Index;
    short sysfnmarker = P.SysFnMarker;
    TTerm[] thisAsst = Assts[AssNo];
    int notms = thisAsst.Length;
 // PRESET OPERATIONAL FIELDS:
    short[] FnRT = new short[notms];
    short[] AtRT = new short[notms];
    for (int tm = 0; tm < notms; tm++)
    {
      TTerm thisTerm = thisAsst[tm];
      buy = thisTerm.OpCode;
      thisTerm.OpCodeRT = buy;
      thisTerm.RatingRT = thisTerm.Rating;
      if (buy == P.snAnd || buy == P.snOr) thisTerm.Valid = 2; // = can't pass this term till all to the left of it is solved.
      else thisTerm.Valid = 1;
      fn_fld = thisTerm.Fn;
      at_fld = thisTerm.At;
      if (fn_fld < 0) thisTerm.X = 0.0; // a dummy variable
      else // adjust nonoperational fields which may have changed during pgm. run:
      { thisTerm.VUse = V.GetVarUse(fn_fld, at_fld);
        thisTerm.X = V.GetVarValue(fn_fld, at_fld);
      }
      // Look for a call for function substitution:
      if ( (at_fld == callfn1 || at_fld == callfn2) && fn_fld == sysfnmarker) // 'func1' is a sys. fn., even if it is aliassing a user fn.
      { if (at_fld == callfn1 && SubstituteFn1 != 0) // if last is 0, 'call_fn' will be run, but will do nothing.
        { fn_fld = SubstituteFn1;   at_fld = SubstituteAt1; }
        else if (at_fld == callfn2 && SubstituteFn2 != 0)
        { fn_fld = SubstituteFn2;   at_fld = SubstituteAt2; }
      }
      FnRT[tm] = fn_fld;   AtRT[tm] = at_fld;
    }
    eq_serviced = false;
// - - - - - - - -  - - - - -
// SOLVE THE ASSIGNMENT:

// NB: VUse is changed separately from -RT fields; if adding code segments below,
//  insert VUse adjustments as needed.
// - - - - - - - -  - - - - -
    while (true)
    { if (eq_serviced) break; // PROPER EXIT - assignment of LHS has occurred.
      buryatend = true;
   // FIND THE TOPDOG ( = highest RatingRT with Valid > 0):
      toprating = 0;  topdog = -1;
      for (int tm = 0; tm < notms; tm++)
      {
        TTerm thisTerm = thisAsst[tm];
        buy = thisTerm.Valid;
        if (buy > 0 && thisTerm.RatingRT > toprating)
        { toprating = thisTerm.RatingRT;  topdog = tm; }
        if (buy == 2)break; // don't search beyond an AND or an OR.
      }
      TTerm thisTopdog = thisAsst[topdog];
      topcode = thisTopdog.OpCodeRT;
      topuse = thisTopdog.VUse;
      // Check - unassigned vars. in RHS are a no-no:
      if (topuse==0 || topuse==10)
      { if (topcode != P.snEq)
        { string ss = V.GetVarName(thisTopdog.Fn, thisTopdog.At);
          result.S = "unassigned variable '" + ss + "' on right side of an assignment";
          return result;
        }
      }
   // IDENTIFY ITS VICTIM:
      victim = -1;
      for (int tm = topdog+1; tm < notms; tm++)
      { if (thisAsst[tm].Valid > 0) {victim = tm; break; } }
      if (victim == -1)
      { result.S = "pgm. error with finding victim term"; return result; }
// - - - - - - - - - - - - - - - - - - - - - - -
   // OPERATE ON VICTIM:             //op
      victimX = thisAsst[victim].X;
      vicuse = thisAsst[victim].VUse;
      if (vicuse == 0) { result.S = "operand '" + V.GetVarName(thisAsst[victim].Fn, thisAsst[victim].At)
                             + "' is an unassigned variable"; return result; }
     // PLUS-MINUS-DIVIDE-MULTIPLY-POWER: Done together, as there are parallel
     //  opns. for scalar and array opns, the latter being bundled to reduce code:
      if (topcode >= P.snFirstArith && topcode <= P.snLastArith)
      { // TWO SCALARS:
        if (topuse <=3 && vicuse <=3)
        { FnRT[topdog] = TempScalarCode; // Where does not apply (*** currently only for snAppend), is corrected below.
          if      (topcode == P.snPlus)  thisTopdog.X += victimX;
          else if (topcode == P.snMinus) thisTopdog.X -= victimX;
          else if (topcode == P.snMult)  thisTopdog.X *= victimX;
          else if (topcode == P.snDiv)
          { double xx = victimX;
            thisTopdog.X /= xx;
            thisTopdog.VUse = 3;
          }
          else if (topcode == P.snAppend)
          { FnRT[topdog] = TempArrayCode; // correcting the default setting above
            quid = OpnBetweenScalars(thisTopdog.X, victimX, P.snAppend);
            thisTopdog.X = quid.X;
            AtRT[topdog] = Convert.ToInt16(quid.I);
            thisTopdog.VUse = 11;//not nec. at present, but stick it in,
               // because an added opn --> scalar result WILL need .VUse to be set.
          }
          else if (topcode == P.snPower)
          { double num = thisAsst[topdog].X; // number to be raised to the power
            if (num < 0 && Math.Round(victimX,0) != victimX) { result.S = "fractional power with negative argument"; return result;}
            thisTopdog.X = Math.Pow(num, victimX);
            thisTopdog.VUse = 3;
          }
        }
        // TWO ARRAYS:
        else if (topuse >10 && vicuse >10)
        { quid = OpnBetweenArrays(FnRT[topdog],  AtRT[topdog],
                                  FnRT[victim],  AtRT[victim], topcode);
          if (!quid.B){result.S = quid.S; return result; }
          thisTopdog.X = quid.X;
          FnRT[topdog] = TempArrayCode;
          AtRT[topdog] = Convert.ToInt16(quid.I);
          thisTopdog.VUse = 11;//not nec. at present, but stick it in,
             // because an added opn --> scalar result WILL need .VUse to be set.
        }
        // ARRAY opn SCALAR:
        else if (topuse >10 && vicuse <= 3)
        { quid = OpnArrayScalar(FnRT[topdog], AtRT[topdog], victimX, topcode);
          if (!quid.B){result.S = quid.S; return result; }
          thisTopdog.X = quid.X;
          FnRT[topdog] = TempArrayCode;
          AtRT[topdog] = Convert.ToInt16(quid.I);
          thisTopdog.VUse = 11;//not nec. at present, but stick it in,
             // because an added opn --> scalar result WILL need .VUse to be set.
        }
        // SCALAR opn ARRAY:
        else if (topuse <= 3 && vicuse > 10)
        { quid = OpnScalarArray(thisTopdog.X, FnRT[victim], AtRT[victim], topcode);
          if (!quid.B){result.S = quid.S; return result; }
          thisTopdog.X = 0.0;
          FnRT[topdog] = TempArrayCode;
          AtRT[topdog] = Convert.ToInt16(quid.I);
          thisTopdog.VUse = 11; // it was 1 to 3 before this.
        }
      }
       // COMPLEMENT operation: (scalars and arrays)
      else if (topcode == P.snNot)
      { if (vicuse <= 3) // a scalar
        { if (victimX == 0.0) thisTopdog.X = 1.0;  else thisTopdog.X = 0.0;
          thisTopdog.VUse = 3;
          FnRT[topdog] = TempScalarCode;
        }
        else // an array, so complement terms individually:
        { quid = UnaryArray(FnRT[victim], AtRT[victim], P.snNot);
          if (!quid.B){result.S = quid.S; return result; }
          thisTopdog.X = 0.0;
          FnRT[topdog] = TempArrayCode;
          AtRT[topdog] = Convert.ToInt16(quid.I);
          thisTopdog.VUse = 11; // it was 1 to 3 before this.
        }
      }
       // NEGATE operation: (scalars and arrays)
      else if (topcode == P.snNegate)
      { if (vicuse <= 3)
        { thisTopdog.X = - victimX; // a scalar.
          thisTopdog.VUse = 3;
          FnRT[topdog] = TempScalarCode;
        }
        else // an array, so multiply all terms by -1:
        { quid = UnaryArray(FnRT[victim], AtRT[victim], P.snNegate);
          if (!quid.B){result.S = quid.S; return result; }
          thisTopdog.X = 0.0;
          FnRT[topdog] = TempArrayCode;
          AtRT[topdog] = Convert.ToInt16(quid.I);
          thisTopdog.VUse = 11; // it was 1 to 3 before this.
        }
      }
       // EQUALS operation:
      else if (topcode == P.snEq)
      { int top_fn = FnRT[topdog], top_at = AtRT[topdog];
        if (vicuse < 10)// then RHS has evaluated to a scalar:
        { if (topuse >=10)
          { result.S = "cannot assign a scalar expression to an array variable";
            return result; }
          n = V.SetVarValue(top_fn, top_at, victimX);
          if (n != 1)
          { if (n==-2)result.S = "cannot change value of a constant";
            else result.S = "weird programming error in setting value";
            return result;
          }
          if (topuse == 0) V.SetVarUse(top_fn, top_at, 3);//treat as void fn.
          result.X = victimX; // the only place where it is changed from default 0.
          result.I = -1;
        }
        else // RHS --> an array result. The ONLY way this point can be reached is where the LHS array ref. will receive a complete array.
             //  Assts. like "Arr[1,2]=..." have long been changed to "__D =__assign(Arr,...".
        { int vic_fn = FnRT[victim], vic_at = AtRT[victim];
          if (topuse == 2) // the LHS is a dummy system variable:
          { if (vic_fn == TempArrayCode)
            { 
              if (Bubble_Next == RETURNNext)
              { result.I = vic_at; }
              // Other temp arrays should not be preserved, so are simply ignored here; other code will eliminate them.
              else
              { // Dispose of the unneeded temporary array:
                R.Store[vic_at].Demolish();  R.Store[vic_at] = null;  R.StoreUsage[vic_at] = 0;  //null storeroom.
                result.I = -1; // dummy scalar result of assignment solving
              }
            }
            else // RHS is a registered array variable:
            { result.I = V.GetVarPtr(vic_fn, vic_at); }
            result.X = 0.0;
          }
          else // not a dummy variable on LHS:
          { if (topuse > 0 && topuse < 10)
            { result.S = "cannot assign an array expression to a scalar variable";
              return result; }
            // Now copy the RHS array into the LHS one: (Don't worry, the methods
            //  called both look after dereferencing the RHS, and updating the LHS
            //  .Client field.)
            if (vic_fn == TempArrayCode)//RHS = temp. array:
            { V.TransferTempArray(vic_at, top_fn, top_at); }
            else V.MakeCopy(vic_fn, vic_at, top_fn, top_at);
            result.I = V.GetVarPtr(top_fn, top_at);  result.X = 0.0;
          }
        }
       // Record every variable above topdog as having been accessed, for checking after the run for variables assigned but never used.
       // REF arguments need to be tagged as accessed even if they only occur as topdog.
       // (The speed cost of including the code below was found to be much less than 1%.)
        for (int trm = topdog; trm < notms; trm++)
        { TTerm termite = thisAsst[trm];
          int vu = termite.VUse;
          if (trm == topdog)
          { if (V.Vars[termite.Fn][termite.At].IsRefArg) V.Vars[termite.Fn][termite.At].Accessed = true; }
          else // not topdog:
          { if (termite.Fn >= 0  &&  (vu == 3 || vu == 11) ) V.Vars[termite.Fn][termite.At].Accessed = true; }
        }

       // Last settings
        eq_serviced = true; // triggers exit at start of next WHILE loop.
        buryatend = false;//no further loops, so adjustments at loop end not needed.
      }
    //------------------------
  // CONDITIONAL OPERATIONS
      else if (topcode >= P.snLess && topcode <= P.snGreater)
      { if (topuse <= 3 && vicuse <= 3) // scalar - scalar:
        { thisTopdog.X = CompareScalars(thisAsst[topdog].X, topcode, victimX);
          thisTopdog.VUse = 3;
        }
        else if (topuse == 11 && vicuse == 11) // array - array: (only handles '==' and '!=')
        { quid = CompareArrays(FnRT[topdog],  AtRT[topdog], FnRT[victim],  AtRT[victim], topcode);
          if (!quid.B){ result.S = quid.S; return result; }
          thisTopdog.X = quid.X;
          thisTopdog.VUse = 3;
        }
        else if (topuse <= 3 && vicuse == 11) // scalar - array: (only allowed if the array has length 1)
        { short vicFn = FnRT[victim], vicAt = AtRT[victim], vicptr;
          if (vicFn == TempArrayCode) vicptr = vicAt; else vicptr = V.GetVarPtr(vicFn,vicAt);
          double[] vicdata = Store[vicptr].Data;
          if (vicdata.Length > 1) { result.S = "scalar - array comparisons: the array must have a length of exactly 1"; return result; }
          thisTopdog.X = CompareScalars(thisAsst[topdog].X, topcode, vicdata[0]);
          thisTopdog.VUse = 3;
        }
        else if (topuse == 11 && vicuse <= 3) // array - scalar: (only allowed if the array has length 1)
        { short topFn = FnRT[topdog],  topAt = AtRT[topdog], topptr;
          if (topFn == TempArrayCode) topptr = topAt; else topptr = V.GetVarPtr(topFn, topAt);
          double[] topdata = Store[topptr].Data;
          if (topdata.Length > 1) { result.S = "array - scalar comparisons: the array must have a length of exactly 1"; return result; }
          thisTopdog.X = CompareScalars(topdata[0], topcode, victimX);
          thisTopdog.VUse = 3;
        }
        else { result.S = "illegal comparison"; return result; } // Should never occur!
      }
  // LOGIC OPERATIONS
      else if (topcode == P.snOr || topcode == P.snAnd || topcode == P.snXor)
      { byte topvalid = thisTopdog.Valid;
        if (topuse > 3 || (topvalid == 1 && (vicuse > 3 && vicuse < 200))) 
        { result.S = "arrays aren't handled by the '" + P.OpCdTransln[topcode] + "' operation"; return result;}
        // Why 'topvalid = 1'? Consider an example - "X && array1 == array2". Always, when the term with '&&' is reached as topdog,
        // its .Valid is set to 2. You'll find this setting just a few lines from the very start of this function RunAssignment(.).
        // By the time '&&' has been reached, 'X' has been evaluated (however many terms it earlier involved), and is now
        // just plain 1 or 0. If it is '1', then we have '1 AND...' which needs the next term(s) for full evaluation. So this loop
        // is aborted, FIRST resetting the term's .Valid to 1. That allows a new choice of topdog (which will be the same if the 
        // next term is a single value, or different, if an expression follows). Alternatively, if we have '0 AND...', then whatever follows
        // the '&&' will simply be ignored, however complex. Either way, while .Valid is 2 the resolving of '&& array1' will never 
        // be attempted. However, once .Valid is set to 1, and again the term '&&' is in focus, '&& array1' will indeed raise the above error.
        double topdogX = thisTopdog.X;
          // If valid is 1, normal opcode handling applies with this topdog.
        if (thisTopdog.Valid == 1)
        { if 
          (       ( topcode==P.snOr && (topdogX != 0.0 || victimX != 0.0) )
               || ( topcode==P.snAnd && topdogX != 0.0 && victimX != 0.0)  
               || ( topcode==P.snXor && ( (topdogX == 0.0 && victimX != 0.0) || (topdogX != 0.0 && victimX == 0.0) ) )     
          ) 
            { thisTopdog.X = 1.0;}
          else thisTopdog.X = 0.0;
        }
        else // topdog's Valid is 2:
        { if (topcode == P.snAnd ^ topdogX == 0.0) // then this is either '1 AND'
           // or '0 OR', and so inconclusive. Downgrade Valid to 1, and reloop
           // without changing anything else, as this is no longer necessarily
           // the topdog in the wider world, and a new topdog search must happen.
          { thisTopdog.Valid = 1; buryatend = false; }
          else // This is '0 AND' or '1 OR', so wipe out the expression that would
            // have been AND/OR'd with this one, and inherit from its last term.
            // That last term will be followed by an AND or an OR of
            // the same nestlevel (anything earlier gets wiped out); or will be
            // the term at the end of the asst., with RatingRT of zero.
          {
            int uppr;  victim = -1;//as we are searching for the new victim.
            uppr = thisTopdog.RatingRT; if (topcode == P.snOr) uppr++;
            for (int tm = topdog+1; tm < notms; tm++)
            { if (thisAsst[tm].Valid > 0)//can be 1 or 2 - no matter.
              { n = thisAsst[tm].RatingRT;
                if (n <= uppr) { victim = tm; break; }
                // i.e. delete all terms till one which has EITHER an and/or at
                //  the same nest level, OR belongs to a lower nest level.
            } }
            if (victim == -1)//surely it can't possibly happen! But just in case...
            { result.S = "no valid term after 'and' or 'or'"; return result; }
            // wipe out the intermediate scum:
            for (int tm = topdog+1; tm < victim; tm++)
            { thisAsst[tm].Valid = 0; }
            // No need to set thisAsst[topdog].X; its existing value applies.
            thisTopdog.Valid = 1; // and leave 'buryatend' as true.
          }
        }
        thisTopdog.VUse = 3;
        FnRT[topdog] = TempScalarCode;
      }
    //------------------------

    // FUNCTIONS
      else if (topcode==P.snFunc)
      {
        fn_fld = FnRT[topdog];  at_fld = AtRT[topdog];
        bool isSysFn = (fn_fld == P.SysFnMarker);
        // count arguments
        noargs = 0;  p = thisTopdog.RatingRT;
        for (int tm = topdog; tm < notms; tm++)
        { if (thisAsst[tm].Valid > 0)
          { if (thisAsst[tm].RatingRT == p) noargs++; else break; }
        }
        if (!isSysFn)
        { int maxArgsForThisUserFn = P.UserFn[at_fld].MaxArgs;
          if (noargs > maxArgsForThisUserFn)
          { result.S = "function " + P.UserFn[at_fld].Name + " is called with too many arguments";
            return result;
          }
          P.UserFn[at_fld].ArgSource = new Duo[maxArgsForThisUserFn];
        }
                  // record of sources for args. is re-created at every user fn. call
      // Handle functions with a single argument separately:
        if (noargs==1 )
        { PairIX pix = new PairIX(-1, victimX);
          if (vicuse <= 3) n = -1;          // scalar arg.
          else if (FnRT[victim] == TempArrayCode)//temp. array arg.
          { n = AtRT[victim]; }
          else                             // array arg. reg'd in Vars[]:
          { n=V.GetVarPtr(FnRT[victim], AtRT[victim]);}
          pix.I = n; // ptr to array storeroom (or -1, for scalar)
           // Note that pix does NOT differentiate between temp and reg'd arrays.
        // Record details of calling code's values for transfer to fn. args.:
          if (isSysFn && F.SysFn[at_fld].REFArgCnt > 0)//stick ref. into class F's static array for such things:
                  // OK to use a static for this purpose, as (currently, at least!) system fns. don't call each other - no recursion.
          { F.REFArgLocn[0].Y = FnRT[victim];
            F.REFArgLocn[0].X = AtRT[victim];
          }
          else if (!isSysFn)// user fn: ALL arg. sources are tagged for user fns. (though only used for those tagged 'ref' by user)
          { P.UserFn[at_fld].ArgSource[0].Y = FnRT[victim];
            P.UserFn[at_fld].ArgSource[0].X = AtRT[victim];            
          }
          
          if (isSysFn)
          { quid = F.RunSysFn(at_fld, pix);
            if (!quid.B && ErrorTrap.B) // foil error detection:
            { quid.I = -1; quid.X = ErrorTrap.X;  ErrorTrap.I++; 
              ErrorTrap.S += "Error " + ErrorTrap.I + ": " + quid.S + "\n";  
              quid.S = "";  quid.B = true; }
          }
          else // a user function:
          { quid = RunUserFn(at_fld, ref topdog, ref victim, ref AssNo, ref buryatend, ref eq_serviced, pix);
                          // error handling after next 'else' section.
                          // Further assignments after next 'else' section also.
  // RETURN FROM RECURSION: The user function must fill the RHS, the LHS being
  //  a simple variable name. This means that at the end of the user fn., quid
  //  holds the wanted result, and it only remains to equate it to the LHS. But
  //  thisAsst fields were not put on any stack, so that values at return are
  //  unreliable. To avoid trouble with the 'if (buryatend)..' section below,
  //  we set thisAsst[victim].Valid to 1.
            if (ComingFromRecursion) thisAsst[victim].Valid = 1;
          }
        }

        else
     // Handle functions with more than one argument:
        { buryatend = false; // multi=arg. functions bury differently...
          PairIX[] pix = new PairIX[noargs];
          
          int noREFs = 0; if (isSysFn) noREFs = F.SysFn[at_fld].REFArgCnt; // currently only used for system fns.
          int cntr=0, v_use;
          for (int tm = topdog+1; tm < notms; tm++)
          { if (thisAsst[tm].Valid > 0)
            { v_use = thisAsst[tm].VUse;
              if (v_use <= 3)          // scalar arg.
              { // Check for unassigned variables: (NB - no equivalent test for single-arg functions; that is because the 
                //   single argument is always a 'victim', and victims are always checked - search above for "if (vicuse == 0)" ).
                if (v_use == 0)
                { result.S = "argument '" + V.GetVarName(thisAsst[tm].Fn, thisAsst[tm].At) + "' is an unassigned variable";
                  return result;
                }
                else  n = -1;
              }
              else
              { if ( FnRT[tm] == TempArrayCode)//temp. array arg.
                { n = AtRT[tm]; }
                else                             // array arg. reg'd in Vars[]:
                { n = V.GetVarPtr(FnRT[tm], AtRT[tm]);}
              }
              pix[cntr].I = n; // ptr to array storeroom (or -1, for scalar)
           // Note that pix does NOT differentiate between temp and reg'd arrays.
              pix[cntr].X = thisAsst[tm].X;
           // Record details of calling code's values for transfer to fn. args.:
              if (isSysFn && cntr < noREFs)
               // ref. --> class F's static array for such things:
              { F.REFArgLocn[cntr].Y = FnRT[tm]; F.REFArgLocn[cntr].X = AtRT[tm];}
              else if (!isSysFn) // for user fns., ALL arg. sources are tagged for user fns. (though only used for those tagged 'ref' by user)
              { P.UserFn[at_fld].ArgSource[cntr].Y = FnRT[tm];
                P.UserFn[at_fld].ArgSource[cntr].X = AtRT[tm];            
              }
              cntr++;
              if (cntr < noargs) thisAsst[tm].Valid=0;// kill intermed. terms
              else // have reached the last term, which remains valid.
              { thisTopdog.OpCodeRT = thisAsst[tm].OpCodeRT;
                thisTopdog.RatingRT = thisAsst[tm].RatingRT;
                if (ComingFromRecursion)thisAsst[topdog].Valid = 1;
                                // see notes above, in 1-arg. user fn. section
                else thisAsst[topdog].Valid = thisAsst[tm].Valid;
                thisAsst[tm].Valid = 0;
                 break;
              }
            }
          }
          if (isSysFn)
          { quid = F.RunSysFn(at_fld, pix);
            if (!quid.B && ErrorTrap.B) // foil error detection:
            { quid.I = -1; quid.X = ErrorTrap.X;  ErrorTrap.I++; 
              ErrorTrap.S += "Error " + ErrorTrap.I + ": " + quid.S + "\n";  
              quid.S = "";  quid.B = true; 
          } }
          else // a user function:
          { quid = RunUserFn(at_fld, ref topdog, ref victim, ref AssNo, ref buryatend, ref eq_serviced, pix); }
        }
        // assignments due for functions with any no. args.:
        if (!quid.B){ result.S = quid.S;  result.I = quid.I;  return result; } // either runtime error, or a new run requested (.I < -1).
        if (quid.I == -1) // scalar fn.'s return
        { thisTopdog.X = quid.X;
          thisTopdog.VUse = 3;
          FnRT[topdog] = TempScalarCode; // -600
        }
        else // was an array function:
        { thisTopdog.VUse = 11;
          FnRT[topdog] = TempArrayCode; // -500
          AtRT[topdog] = Convert.ToInt16(quid.I);//ref. to temp. storeroom
        }
      }
// - - - - - - - - - - - - - - - - - - - - - - -
   // RANSACK AND BURY VICTIM:
      if (buryatend) // already done (differently), for functions.
      { TTerm thisVictim = thisAsst[victim];
        thisTopdog.OpCodeRT = thisVictim.OpCodeRT;
        thisTopdog.RatingRT = thisVictim.RatingRT;
        thisTopdog.Valid    = thisVictim.Valid;
        thisVictim.Valid = 0; // r.i.p.
      }
    } // END OF 'WHILE' LOOP for all topdogs
    V.CleanStore(result.I, ThisAssInstance); // Spares only the arg. slot. No harm, if arg. is -1.
    result.B = true; return result;
  }


//################Set up a pointer for each storeroom; do the same for similar methods.


//==============================================================================
// 
/// <summary>
/// Both of the variables must be assigned arrays (whether registered or temp.). If Top is a temporary array, and the operation
///  is a basic arithmetic one (+ - * / ^), the output array's data strip will simply overlay that of Top, keeping its dimensionality.
///  Otherwise a new temporary array will be created.
/// RETURN: The result of all operations is an array. Hence fixed values of result.X (0.0) and result.I (temp array pointer):
///  result.B reflects error, .S holds any error message (or is empty).
/// </summary>
  public static Quad OpnBetweenArrays(int TopFn, int TopAt, int VicFn, int VicAt, byte OpCode)
  { Quad result = new Quad(false);
    int topptr, vicptr, outptr;
    if (TopFn == TempArrayCode) topptr = TopAt;  else topptr = V.GetVarPtr(TopFn, TopAt);
    if (VicFn == TempArrayCode) vicptr = VicAt;  else vicptr = V.GetVarPtr(VicFn,VicAt);
    StoreItem topitem = Store[topptr],  vicitem = Store[vicptr],  outitem = null;
    int topsz = topitem.TotSz,  vicsz = vicitem.TotSz;
    double[] topdata = topitem.Data, vicdata = vicitem.Data, outdata;
    // *** If new operations added, next line may change. It depends on all opns but 'append' requiring input and output arrays to have the same size.
    if (OpCode == P.snAppend) // the only operation for which arrays do not have to have equal length
    { // designate an output storeroom:
      outptr = V.GenerateTempStoreRoom(topsz + vicsz); // Can't do "outptr = TopAt" as is done below, as lengths may differ; and dimensions.
      outitem = Store[outptr];
      outdata = outitem.Data;
      topdata.CopyTo(outdata, 0);  vicdata.CopyTo(outdata, topsz);      
    }
    else
    { if (topsz != vicsz)
      { result.S = "arrays must have the same length for this operation"; return result;}
    // designate an output storeroom:
      if (TopFn == TempArrayCode) outptr = TopAt; //Topdog is temp., and has the required length, so will itself receive results.
      else outptr = V.GenerateTempStoreRoom(topitem.DimSz); // Topdog not temp., so make a temp. array, same dimensions as Topdog.
      outitem = Store[outptr];
      outdata = outitem.Data;
      // Operation:
      if      (OpCode == P.snPlus)  { for (int i = 0; i < topsz; i++) { outdata[i] = topdata[i] + vicdata[i]; } }
      else if (OpCode == P.snMinus) { for (int i = 0; i < topsz; i++) { outdata[i] = topdata[i] - vicdata[i]; } }
      else if (OpCode == P.snMult)  { for (int i = 0; i < topsz; i++) { outdata[i] = topdata[i] * vicdata[i]; } }
      else if (OpCode == P.snDiv)   { for (int i = 0; i < topsz; i++) { outdata[i] = topdata[i] / vicdata[i]; } }
      else if (OpCode == P.snPower)
      { double num, pwr;
        for (int i = 0; i < topsz; i++)
        { num = topdata[i];  pwr = vicdata[i];
          if (num < 0 && Math.Round(pwr,0) != pwr) { result.S = "fractional power with negative argument"; return result;}
          outdata[i] = Math.Pow(num, pwr);
      } }
      else {result.S = "this operation between arrays is not handled"; return result;}
    }
    // The first array gives its .IsChars rating to the result, for ALL operations calling this method
    outitem.IsChars = topitem.IsChars;
    result.B = true;  result.I = outptr;  return result;
  }

/// <summary>
/// <para>The result is based on { X1 [logic operation] X2 }. If this is TRUE, 1.0 is returned; otherwise 0.0.</para>
/// <para>'How' is the code for a logic operation; it will be one of the codes, defined early in class P, that use prefix 'sn'.</para>
/// </summary>
  public static double CompareScalars(double X1, byte How, double X2)
  { double result = 0.0;
    if      (How == P.snLess)     { if (X1 < X2)  result = 1.0; }
    else if (How == P.snEq_less)  { if (X1 <= X2) result = 1.0; }
    else if (How == P.snEq_eq)    { if (X1 == X2) result = 1.0; }
    else if (How == P.snNot_eq)   { if (X1 != X2) result = 1.0; }
    else if (How == P.snEq_gr)    { if (X1 >= X2) result = 1.0; }
    else if (How == P.snGreater)  { if (X1 > X2)  result = 1.0; }
    return result;
  }

/// <summary>
/// Both of the variables must be assigned arrays (whether registered or temp.).
/// RETURN: .X is 1 (true) or 0 (false). There are currently no error states, so .B is always trure.S is always emoty, .I always 0.
/// MEANINGS: (1) "==", "!=": Two arrays are equal if (a) they have the same length (structure ignored), and (b) all corresponding elements
///   are equal.  (2) ">" and "<": Dictionary ordering is used. E.g. "CA" < "CAT", "CAT" < "COT". Note that arrays can have different lengths. 
/// </summary>
  public static Quad CompareArrays(int TopFn, int TopAt, int VicFn, int VicAt, byte OpCode)
  { Quad result = new Quad(false);
    int topptr, vicptr;
    if (TopFn==TempArrayCode)topptr=TopAt; else topptr=V.GetVarPtr(TopFn, TopAt);
    if (VicFn==TempArrayCode)vicptr=VicAt; else vicptr=V.GetVarPtr(VicFn,VicAt);
    int topsz = Store[topptr].TotSz,  vicsz = Store[vicptr].TotSz;
    // Deal with "==" and "+=" first:
    if (OpCode == P.snEq_eq  ||  OpCode == P.snNot_eq)
    { bool are_equal = (topsz == vicsz); // to be equal, two arrays must at least have the same length.
      if (are_equal)
      { double[] topdata = Store[topptr].Data,  vicdata = Store[vicptr].Data;
        { for (int i = 0; i < topsz; i++)
          { if (topdata[i] != vicdata[i])
            { are_equal = false; break; }
          }
        }
      }
      result.X = ( (OpCode == P.snNot_eq) ^ (are_equal) )  ?  1.0  :  0.0;  
    }
    else // "<", "<=", ">=", ">":        
    { bool top_is_less = false, top_is_greater = false;
      double[] topdata = Store[topptr].Data,  vicdata = Store[vicptr].Data;
      int minLen = (topsz > vicsz) ?  vicsz  :  topsz; // N is length of the smaller array.
      { for (int i = 0; i < minLen; i++)
        { if      (topdata[i] < vicdata[i]) { top_is_less = true; break; }
          else if (topdata[i] > vicdata[i]) { top_is_greater = true; break; }
        }
      }
      if (topsz != vicsz  &&  !top_is_less  &&  !top_is_greater) // Unequally-sized arrays which are equal as far as the minimum length:
      { if (topsz > vicsz) top_is_greater = true;  else top_is_less = true; // The shorter one is lower in dictionary order.
      }
      if      (OpCode == P.snLess)     { if (top_is_less)  result.X = 1.0; }
      else if (OpCode == P.snEq_less)  { if (top_is_less || !top_is_greater) result.X = 1.0; }
      else if (OpCode == P.snGreater)  { if (top_is_greater)  result.X = 1.0; }
      else if (OpCode == P.snEq_gr)    { if (top_is_greater || !top_is_less) result.X = 1.0; }
    }
    // Endpoint for all:
    result.B = true;  return result;
  }

  /// <summary>
  /// Currently only used by the append operator, other scalar-scalar operations not requiring a special method.
  /// For this operator, a new temporary array of length 2 is created, concatenating the two scalar values.  
  /// RETURNED: *** Currently, with only one opcode calling this method, and it resulting in an array, and no error being possible,
  ///   .B (error indicator) is always TRUE, .S (error message) the empty string, .X (scalar output) = 0.
  ///  The only meaningful return is .I, which holds the temp. array pointer.
  /// NB - arg. OpCode is currently a dummy, as there is only one OpCode that results in the call. *** This will change, if others are added...
  /// </param>
  public static Quad OpnBetweenScalars(double X1, double X2, byte OpCode)
  { int outptr = V.GenerateTempStoreRoom(2);
    Store[outptr].Data = new double[] {X1, X2};
    return new Quad(outptr, 0.0, true, "");
  }

/// <summary>
/// If Top is a temporary array, and the operation is a basic arithmetic one (+ - * / ^), the output array's data strip will simply
///  overlay that of Top, keeping its dimensionality. Otherwise a new temporary array will be created.
/// RETURN: The result of all operations is an array. Hence fixed values of result.X (0.0) and result.I (temp array pointer):
///  result.B reflects error, .S holds any error message (or is empty).
/// </summary>
  public static Quad OpnArrayScalar(int TopFn, int TopAt, double XX, byte OpCode)
  { Quad result = new Quad(false);
    int topptr, outptr;
    if (TopFn == TempArrayCode) topptr = TopAt; else topptr = V.GetVarPtr(TopFn, TopAt);
    StoreItem topitem = Store[topptr], outitem;
    int topsz = topitem.TotSz;
    double[] topdata = topitem.Data, outdata;
    if (OpCode == P.snAppend) // the only operation for which the returned array has different length and dimensionality to Top.
    { // designate an output storeroom:
      outptr = V.GenerateTempStoreRoom(topsz + 1); // Can't do "outptr = TopAt" as is done below, as lengths may differ; and dimensions.
      outitem = Store[outptr];
      outdata = outitem.Data;
      topdata.CopyTo(outdata, 0);
      outdata[topsz] = XX;
    }
    else // operation preserves length and dimensionality of Top:
    {// designate an output storeroom:
      if (TopFn == TempArrayCode) outptr = TopAt;//Topdog temp., so will itself receive results.
      else outptr = V.GenerateTempStoreRoom(topitem.DimSz);//Topdog not temp., so make a temp. array.
      outitem = Store[outptr];
      outdata = outitem.Data;
      // Operation:
      if      (OpCode == P.snPlus)
      { for (int i = 0; i < topsz; i++) outdata[i] = topdata[i] + XX; }
      else if (OpCode == P.snMinus)
      { for (int i = 0; i < topsz; i++) outdata[i] = topdata[i] - XX; }
      else if (OpCode == P.snMult)
      { for (int i = 0; i < topsz; i++) outdata[i] = topdata[i] * XX; }
      else if (OpCode == P.snDiv)
      { for (int i = 0; i < topsz; i++) outdata[i] = topdata[i] / XX; }
      else if (OpCode == P.snPower)
      { for (int i = 0; i < topsz; i++)
        { double num = topdata[i]; // number to be raised to the power
          if (num < 0 && Math.Round(XX,0) != XX) { result.S = "fractional power with negative argument"; return result;}
          outdata[i] = Math.Pow(num, XX);
      } }
      else if (OpCode == P.snNot)
      { for (int i = 0; i < topsz; i++)
        { outdata[i] = topdata[i] * XX;
      } }
      else {result.S = "this array-scalar operation is not handled"; return result;}
      // The array gives its .IsChars rating to the result:
    } 
    outitem.IsChars = topitem.IsChars;
    result.B = true;  result.I = outptr;  return result;
  }
/// <summary>
/// RETURN: The result of all operations is a temporary array. For all operations except 'append' (OpCode = snAppend), this array
///  has the same length and dimensionality as the Victim array. In all cases it preserves the Chars rating of Victim.
///  Fixed values of the returned 'result' are: result.X (0.0) and result.I (temp array pointer). result.B reflects error,
///  result.S holds any error message (or is empty).
/// </summary>
  public static Quad OpnScalarArray(double XX, int VicFn, int VicAt, byte OpCode)
  { Quad result = new Quad(false);
    int vicptr, outptr;
    if (VicFn == TempArrayCode) vicptr = VicAt; else vicptr = V.GetVarPtr(VicFn,VicAt);
    StoreItem vicitem = Store[vicptr], outitem;
    int vicsz = vicitem.TotSz;
    double[] vicdata = vicitem.Data, outdata;
    if (OpCode == P.snAppend) // the only operation for which the returned array has different length and dimensionality to Victim.
    { // designate an output storeroom:
      outptr = V.GenerateTempStoreRoom(vicsz + 1);
      outitem = Store[outptr];
      outdata = outitem.Data;
      outdata[0] = XX;
      vicdata.CopyTo(outdata, 1);
    }
    else // operation preserves length and dimensionality of Top:
    { outptr = V.GenerateTempStoreRoom(vicitem.DimSz);
      outitem = Store[outptr];
      outdata = outitem.Data;
      // Operation:
      if      (OpCode == P.snPlus)  { for (int i = 0; i < vicsz; i++) outdata[i] = XX + vicdata[i]; }
      else if (OpCode == P.snMinus) { for (int i = 0; i < vicsz; i++) outdata[i] = XX - vicdata[i]; }
      else if (OpCode == P.snMult)  { for (int i = 0; i < vicsz; i++) outdata[i] = XX * vicdata[i]; }
      else if (OpCode == P.snDiv)   { for (int i = 0; i < vicsz; i++) outdata[i] = XX / vicdata[i]; }
      else if (OpCode == P.snPower)
      { for (int i = 0; i < vicsz; i++)
        { double pwr = vicdata[i];
          if (XX < 0 && Math.Round(pwr,0) != pwr) { result.S = "fractional power with negative argument"; return result;}
          outdata[i] = Math.Pow(XX, pwr);
      } }
      else {result.S = "this scalar-array operation is not handled"; return result; }
    }
    // The array gives its .IsChars rating to the result:
    outitem.IsChars = vicitem.IsChars;
    result.B = true;  result.I = outptr;  return result;
  }

//  Results will feed into a new temp array.
// Output: .B reflects error, .S holds any error message.  .X remains 0. 
//  .I holds the temp. array pointer.
  public static Quad UnaryArray(int VicFn, int VicAt, byte OpCode)
  { Quad result = new Quad(false);
    int vicptr, outptr;
    if (VicFn==TempArrayCode)vicptr=VicAt; else vicptr=V.GetVarPtr(VicFn,VicAt);
    int topsz = Store[vicptr].TotSz;
    outptr = V.GenerateTempStoreRoom(Store[vicptr].DimSz);
    double[] vicdata = Store[vicptr].Data,  outdata = Store[outptr].Data;
  // Operation:
    if      (OpCode == P.snNegate) { for (int i=0; i < topsz; i++) outdata[i] = - vicdata[i]; }
    else if (OpCode == P.snNot)
    { for (int i = 0; i < topsz; i++) { if ( vicdata[i] == 0.0) outdata[i] = 1.0;  else outdata[i] = 0.0; } }
    else {result.S="this unary array operation is not handled"; return result;}
    // The array gives its .IsChars rating to the result:
    Store[outptr].IsChars = Store[vicptr].IsChars;
    result.B = true;  result.I = outptr;  return result;
  }

//user
 // If error, returns .B FALSE, error msg. in .S, and paragraph to select in .I. [Later added: Bullo. .I does not hold this value.]
 // If no error, .B is TRUE. For SCALARS, .I returns as -1, and .X is the return
 // value. For ARRAYS, .I >= 0, being the pointer to the storeroom (which may
 // belong to either a temp. array or to a reg'd array; if the latter, the
 // reg'd array may belong to the main pgm. or to the fn.).
 // All the ref arguments are to conserve values if recursion occurs.
 // Note these vital 3 steps, if calling this from a system function (where the fn. has been passed the user fn. name, and has identified it):
 //  (1) DON'T ALLOW the function to be called recursively; the sys. fn. code should 1st.  check P.UserF.UserFn[n].REFArgCnt; if > 1, raise an error.
 //  (2) Supply a local COPY of "R.LatestAssignment" for REF arg. "AssNo", but dummy values for all four other REF args. (as they are only
 //        used for recursion). ("AssNo" will be the assignment containing the system fn. callint the user fn.)
 //  (3) Set P.UserF.UserFn[n].ArgSource to new Duo[]{ new Duo(), new Duo() }, or the fn. below will crash. These dummy values are OK, as 
 //        the storage of them below is only accessed if a recursion is occurring, and that you have made impossible.
   public static Quad RunUserFn(short Which,
                          ref int topdog, ref int victim, ref int AssNo,
                          ref bool buryatend, ref bool eq_serviced,
                          params PairIX[] Args)
  { Quad result = new Quad(false);
    UserFnCalls[Which]++; // will give the total no. of calls to every function. ([0] is never used - the main pgm. is not 'called'.)
    // Put data on the queue for later display by MainWindow menu "Run | Show Sequence of Function Calls".
    Trio kew = new Trio(Which, AssNo, ++UserFnLogEntryNo);
    UserFnLog.Enqueue(kew);
    while (UserFnLog.Count > UserFnLogSize) UserFnLog.Dequeue();
    int arglen = Args.Length;   double xx = 0.0;
    TFn ThisTFn = P.UserFn[Which];
    int offset = ThisTFn.NoSysVars;
    int maxargs = ThisTFn.MaxArgs;
    if (ThisTFn.MinArgs > 0)// if = 0, skip whole argument section below.
      // By the way: there is no point in allowing for the case of all args. being optional, as the first arg. is always overruled,
      //   since "Foo()" is internally equiv. to "Foo(0)". So all-args.-optional now raises an error after this block (as of April 2017).
    { if (arglen < ThisTFn.MinArgs)
      { result.S = "too few arguments for function " + ThisTFn.Name; return result; }
      if (arglen > maxargs)
      { result.S = "too many arguments for function " + ThisTFn.Name; return result; }
     // ALL ERROR OUTLETS should precede this point, to avoid stuffing up recursion.
      ThisTFn.REFArgCnt ++;//depth of call (1: normal call; 2+: recursive.)
      int callDepth = ThisTFn.REFArgCnt; // Badly named field, that. Nothing to do with REF arguments in user fns.
      if (callDepth == 1) // Redundant if no recursion; but if there is, we need to have a record of arg. sources for the ORIGINAL call.
      { ClientStack[Which] = new List<Duo>(ThisTFn.ArgSource); // holds sources of arguments.
      }
      else // RECURSION IS OCCURRING:
      {
        if (R.Assts[AssNo].Length > arglen + 2)
        { result.S = "to use the function recursively, both the LHS variable and the arguments must be simple variables (no fns, opns, or '[..]')";
          return result;
        }
        ClientStack[Which].AddRange(ThisTFn.ArgSource); // holds sources of arguments.
        int dlen = V.Vars[Which].Count;
        // VVars[Which].Count is unaltered by recursion. At each recursion we will take a copy of V.Vars[Which]
        //   and add that copy to a static class list, FStack. Fstack is only created at the first recursion, and then
        //   is nulled (further down in this method) at the end of the last recursion.
        if (callDepth == 2)
        { FStack[Which] = new List<TVar>();
        }
        for (int i = 0; i < dlen; i++) // go through all the variables registered for this function, and add them to FStack:
        { TVar vroom = V.Vars[Which][i];
          FStack[Which].Add(vroom.Copied());
          if (vroom.Use == 11) // Assigned array:
          {
  // If we were saving the store item (with its data strip) on the stack, life would be easy now; we could just continue with this
  //  instance of this function, as below. But to conserve space and time we are not putting the store items on the stack. What will
  //  happen if we just leave them there and take no precautions? In the case of REF arg. arrays, nothing; the code below this block
  //  will simply overwrite the client field (which would only be different at the first recursion anyway) and take over the store item,
  //  exactly as we would want. No action here is therefore required. Next, consider NON-REF argument arrays. The block below will reassign
  //  the V.Vars parameters to a new store item, into which it has copied all the data from the current one; it will not dereference the old
  //  store item, which will live on with the same duplicated client fields but with no V.Vars pointer to it. This is bad, as (a) it can't
  //  be cleaned away but clogs up memory, and (b) its value at the time of the recursion call is lost when the recursion is over. So we
  //  must bookmark its storeroom here, and write code at the end of this function, when arrays are being dereferenced, which gets the old
  //  storeroom back. Finally, consider arrays not passed as arguments but created locally. The code below ignores any such preexisting values,
  //  and simply sets them anew as program steps require it. Again, bookmarking of the store item is required.
  // So here is what we do here. We bookmark EVERY array (remember, REF args. will simply ignore and overwrite our bookmark). Then we dereference
  //  the V.Var instance; this ensures that no attempt will be made to use the store item which we have just bookmarked. (It is irrelevant to do
  //  so for argument arrays, but no harm is done.)
            Store[V.GetVarPtr(Which, i)].BookmarkClient(Which);
            vroom.Use = 10;  vroom.Ptr = -1;
          }
        }
        // Set up RStack[Which]:
        if (callDepth == 2) RStack[Which] = new List<Recur>();
        Recur rcr = new Recur(topdog, victim, AssNo, buryatend, eq_serviced);
        RStack[Which].Add(rcr);
      }//END OF "RECURSION is Occurring" segment.

      for (int i = 0; i < maxargs; i++)
      { if (i >= arglen || Args[i].I == -1) // this arg. is a scalar.
        { if (i < arglen) xx = Args[i].X;
          else // an optional argument, which is to receive its optional value:
          { if (ThisTFn.ArgDefaultStrings[i] != "") // This is a once-only operation, at the first invocation of the optional value
            { string sss = ThisTFn.ArgDefaultStrings[i]; // It can be a literal no., a constant, or a simple expression involving these.
              double[] values; string errmsg;
              string ttt = P.ParseExpession(sss, out values, out errmsg, false); // FALSE = allow constants but not main pgm variables.
                            // We can't allow variables as this is a once-only evaluation, and variables may later change their values.
              if (errmsg == "") xx = JS.SolveExpression(ttt, values, out errmsg);
              if (errmsg != "")
              { result.S = "Function " + ThisTFn.Name + ": optional argument expression '" + sss + "' failed parsing: " + errmsg;
                return result;
              }
              ThisTFn.ArgDefaults[i] = xx;
              ThisTFn.ArgDefaultStrings[i] = ""; // so that this block will not be called again for this argument.
            }
            xx = ThisTFn.ArgDefaults[i]; // an explicit numerical value was in the user's code at parse time.
          }
          V.SetVarValue(Which, offset+i, xx);
          V.SetVarUse(Which, offset+i, 3);
          V.SetVarPtr(Which, offset+i, -1);
        }
        else // this is an array argument:
        { int ptr = Args[i].I;
          // Deal with REF arrays first:
          if (ThisTFn.PassedByRef[i])
          {// We are about to tell the storeroom of the ref array that it now belongs to the alias of this function;
           // we do this by resetting its client fields to refer to the alias. The original array of calling code is
           // still happily pointing to the same storeroom, but as focus is out of that calling code this is not a problem;
           // after the function is finished, the storeroom will again have client fields aligned with the calling code instance.
           // (Note that you cannot use function 'kill(.)' to kill a variable passed as an argument to a function, so there is
           // no danger from that direction.)
            TVar tvar = V.Vars[Which][offset+i];
            tvar.X = 0.0;  tvar.Use = 11;  tvar.Ptr = (short) ptr;             
            StoreUsage[ptr] = 2; // '2' = assigned array. A temp array cannot be a REF argument.
            // Update the client field of the original store item to the current alias array:
            StoreItem sitem = R.Store[ptr];
            sitem.ClientFn = Which;
            sitem.ClientVarNo = offset+i;
          }
          else // NOT a REF array. Make the array a copy of the calling code's array:
          { int slot = V.GenerateTempStoreRoom(Store[ptr].DimSz);
            V.CopyStoreroom(ptr, slot, true); // 'true' stops re-creating sizing fields already set by the last step.
            V.TransferTempArray(slot, Which, offset+i); 
          }
        }
      }
    }
    else // MinArgs was zero. Nothing need be done - as there are no args. - unless there were optional args without set args. This is not
      // allowed (because even void functions "Foo()" in fact supply an arg. of 0). So we detect the error here, before it raises an unassigned
      // variable error later:
    if (maxargs != 0)
    { result.S = "Function " + ThisTFn.Name + " has only optional arguments. Optional args. must be preceded by at least one nonoptional arg";
      return result;
    }

   // --- RUN THE THING ----------------
    int holdit = CurrentlyRunningUserFnNo; // Only set in 'Run(.)'. We want it always to track the currently running user fn, so hold its old value.
    result = Run(Which);
    CurrentlyRunningUserFnNo = holdit;
    if (!result.B) return result;
   // ----------------------------------
    ComingFromRecursion = false;
    if (ThisTFn.REFArgCnt > 1)
   // If this was a recursive call, restore preexisting values:
    {// The story so far: in 'Run(.)', if you search for 'RETURNNext', you will find that just before the return occurs, 
     // all arrays go to the unassigned state, and all their storerooms are demolished EXCEPT for REF args. and the returned
     // named array, if any (it being returned as a temporary array, hence not reg'd in V.Vars).
      ComingFromRecursion = true;
      int dlen = V.Vars[Which].Count;      // We can't reuse above values of these
       // next 2, as interwoven multifn. recursions would set each others' values.
      int callDepth = ThisTFn.REFArgCnt;
      int olddatalen = (callDepth - 2)* dlen;
      for (int vari = 0; vari < dlen; vari++)
      { TVar tvar = FStack[Which][olddatalen + vari];
        V.Vars[Which][vari] = tvar;
        if (tvar.Use == 11)
        { StoreItem sitem = Store[tvar.Ptr];
          sitem.UnBookmarkClient(Which); // restore the functionality of the store item. (Will not try to unbookmark a REF arrray,
                                         // which was never bookmarked in the first place.)
        }
      }
      if (callDepth == 1) ClientStack[Which] = null;
      else
      { ClientStack[Which].RemoveRange(maxargs, ClientStack[Which].Count - maxargs);
        if (callDepth == 2) // returned from the last recursion
        { FStack[Which] = null; } // the last recursion is over.
        else FStack[Which].RemoveRange(olddatalen, FStack[Which].Count - olddatalen); // remove all data beyond olddatalen
      }
     // Now replace the RunAssignment(.) params from RStack:
      Recur cur = RStack[Which][callDepth-2];
      topdog = cur.topdog1;
      victim = cur.victim1;
      AssNo =  cur.assno1;
      buryatend = cur.buryatend1;
      eq_serviced = cur.eq_serviced1;
      // Then reset the stack:
      if (callDepth == 2) RStack[Which] = null;
      else RStack[Which].RemoveAt(callDepth-2);
    }
   // Recursion or not...
    ThisTFn.REFArgCnt--;
    return result;
  }


// The following is necessary before running each new program. It does NOT kill SysFn, which can safely be reused.
  public static void KillData()     //kill
  {// Nullify Arrays and Parameters in this class:
    V.SetVarsToNull();
    Assts = null;
    for (int i=0; i<Store.Count; i++)
    { if (StoreUsage[i] > 0){ Store[i].Demolish(); } }
    Store.Clear();  StoreUsage.Clear();
    FStack = null;  ClientStack = null;  RStack = null;
    SubstituteFn1 = 0;  SubstituteAt1 = -1;
    SubstituteFn2 = 0;  SubstituteAt2 = -1;
   // Nullify Arrays and Parameters in class P in Parser:
    P.UserFn = null;       
    P.RawAsstText = null;  P.CookedAsstText = null;
    if (P.Quotation != null) P.Quotation.Clear();
    P.Identifier1stChar = JS.Identifier1stChar; // Removes any extra chars. added to the copy of the JS field by the last run.
    P.IdentifierChars = JS.IdentifierChars;
   // Nullify Arrays and Parameters in class C in Conformer:
    C.UserFnCnt = 0;       C.NextSysVarNo= 0;
   // System lists: 
    if (F.Sys != null && !F.PreserveSysLists)
    { for (int i=0; i < F.Sys.Count; i++) F.Sys[i].LIST.Clear(); 
      F.Sys = null;  F.NoSysLists = 0;
    }
    F.CanPause[0] = true;  for (int i=1; i < F.CanPause.Length; i++){ F.CanPause[i] = false; } // --> [T, F, F, F, ....]
    F.CentreGraphs = false;   F.MaximizeGraphs = false;   F.PositionGraphs = false;
    MonoMaths.MainWindow thisWindow = MonoMaths.MainWindow.ThisWindow;
    thisWindow.AdjustLabelText('C', "", 'W');
    thisWindow.ButtonReleaseData = new double[20];
    MainWindow.NextPlotsPointChars = "";
    thisWindow.KeyQueue = new int[thisWindow.KeyQueueLength];
    thisWindow.LastKeyCombo = new int[thisWindow.KeyQueueLength];
    R.UserFnLog.Clear();   R.UserFnLogEntryNo = 0;
    if (thisWindow.BlockFileSave != null) thisWindow.BlockFileSave.Clear();
   // User-added submenus:
    thisWindow.InactivateExtraMenuSystem();
    MainWindow.DisplayScalarValuesAfterTheRun = true; // the default.
    F.GraphAscension = double.NaN;  F.GraphDeclination = double.NaN;
  }

/// <summary>
/// Demolish all arrays except (a) those whose ClientFn differs from FnNo (i.e. REF arguments), and 
/// (b) those with a R.Store[] index listed in ExceptThese. REF arguments are not in any way altered;
/// ExceptThese storerooms are dereferenced but left as temporary arrays; all others are completely eniolated.
/// </summary>
  public static void DemolishFnArrays(int FnNo, params int[] ExceptThese)
  { if (FnNo < 1) return; // no action. Should never happen.
    for (int var = 0; var < V.Vars[FnNo].Count; var++)
    { if (V.GetVarUse(FnNo, var) == 11) // then it is an assigned array:
      { int slot = V.GetVarPtr(FnNo, var);
        if (slot != -1) // Yes, there can be arrays with use 11 and slot -1; REF arg. aliasses are thus, just before this method is called.
        { StoreItem sightem = Store[slot];
        // If in ExceptThese, the array lingers on as a temporary array; otherwise it is demolished.
          if (ExceptThese.Length==0 || Array.IndexOf(ExceptThese, slot) == -1)
          { sightem.Demolish();  StoreUsage[slot] = 0; } 
        }  
        // The record in V.Vars must be unset.
        V.SetVarUse(FnNo, var, 10);
        V.SetVarPtr(FnNo, var, -1);
        V.SetVarValue(FnNo, var, 0.0);
      }
    }
  }

//------------------------------------------------------------------------------

//next--//slot----------------------------------------------------------------------------
// Installs a new StoreItem object into Store, and returns its index. If there are
//  any vacant ones in Store (StoreUsage[i]=0), that is where the new object goes;
//  otherwise it goes on the end of Store.
// It also sets StoreUsage to 1 for that slot; DON'T FORGET to reset it to 2,
//  if turning the storeroom into a registered array's storeroom.
  public static int NextStoreSlot()
  { StoreItem steve = new StoreItem(0);
    int slot = -1;
    for (int i = 0; i < Store.Count; i++)
    { if (StoreUsage[i] == 0) {slot = i; break;} }
    if (slot >= 0)
    { Store[slot] = steve;  StoreUsage[slot] = 1; }
    else 
    { slot = Store.Count;  Store.Add(steve);  StoreUsage.Add(1); }
    return slot;
  }
//------------------------------------------------------------------------------
/// <summary>
/// <para>Demolish the storeroom; reset the .Ptr field in the V.Vars reference to -1. No error checks.</para>
/// <para>If RefuseIfBookmarked is true, will do nothing if 'FnLvl' >= the book mark indicator StoreItem.ClientBkMkIncrement.</para>
/// <para>If RefuseIfAnotherClient is true, will do nothing if the storeroom's client is not (FnLvl, ArrayNo).</para>
/// </summary>
  public static void DereferenceStoreRoom(int FnLvl, int ArrayNo, bool RefuseIfBookmarked, bool RefuseIfAnotherClient)
  { short ptr = V.GetVarPtr(FnLvl, ArrayNo);
    if (ptr >= 0) // If already dereferenced, do nothing.
    { StoreItem sitem = Store[ptr];
      if (RefuseIfBookmarked && sitem.ClientFn >= StoreItem.ClientBkMkIncrement) return;
      if (RefuseIfAnotherClient){ if (sitem.ClientFn != FnLvl || sitem.ClientVarNo != ArrayNo) return; } // 2nd test should never fail, but just in case...
      V.SetVarPtr(FnLvl, ArrayNo, -1); // Storeroom is now dereferenced.
      // Now clean things up at the storeroom end:
      Store[ptr].Demolish();  StoreUsage[ptr] = 0; 
    }
  }
  public static string AskWhatToDo(char whichbtn)
  { if (whichbtn == 'A') return "stop"; // 'ABORT' btn.
      // If got here, 'GO' btn. was clicked.
    string result = "";
    string ss = "Do you want to ABORT the program run; or HOLD (and later click 'GO' to resume); or CONTINUE the run?";
    int btn = JD.DecideBox("PROGRAM INTERRUPTED:", ss, "CONTINUE", "HOLD", "ABORT");
    if (btn == 2) result = "hold"; else if (btn == 3)result = "stop";
    return result;
  }

//====== LISTING FUNCTIONS =====================================================
// Formatted text, ready for JB.Show(..).
// WhatSort: 'A'=arrays only; 'X'=scalar user-generated variables only (no
//  constants, no literals, no internal variables like __WH or '__S0');
//  'B'=both ('A'+'X'); 'L'=the Lot (incl. literals, constants, internal vars).
  public static string VariablesList(int FnLvl, char WhatSort, params int[] FromTo)
  { string ss = "";  if (FnLvl == 0) ss = "Main Program"; else ss = "'"+P.UserFn[FnLvl].Name + "'";
    string result = "<B>VARIABLES:   (Fn. Level " 
      + FnLvl.ToString()+" -- " + ss + ")</B>\n";
    if (V.Vars == null){ result += "*** NULL ***\n\n"; return result; }
    int n, startvar = 0;  int endvar = V.Vars[FnLvl].Count-1;
    if (FromTo.Length > 1){ n = FromTo[1];  if (n < endvar) endvar = n;}
    if (FromTo.Length > 0){ n = FromTo[0];  if (n > 0) startvar = n;}
    for (int i = startvar; i <= endvar; i++)
    { byte use = V.GetVarUse(FnLvl, i);
      if (WhatSort=='A' && (use < 10 || use > 0x7F )) continue;
      else if (WhatSort=='X' && use != 3 ) continue;
      else if (WhatSort=='B' && (use < 3 || use > 0x7F)) continue;
      result += i.ToString() + ":   " + V.FormattedString(FnLvl, i);  result += '\n';
    }
    return result;
  }

// User program scalars and their values are displayed in formatted form in the REditRes window.
  public static string DisplayMainPgmUserScalars()
  { if (V.Vars == null) return "";
    int varCnt = V.Vars[0].Count;  if (varCnt == 0) return ""; // should never happen
    string ss="";
    string result = "",  delim = ";   ", equals = " = ";
    bool first = true;
    if (Assts.Count == 1  &&  V.GetVarUse(0, varCnt-1) == 2) // then the user has entered a one-liner expression with no LHS:
    { result = "-->  " +  V.GetVarValue(0,varCnt-1).ToString() + delim; }
    else
    for (int i = 0; i < varCnt; i++)
    { if (V.GetVarUse(0, i) != 3 ) continue; // only user scalars allowed here.
      ss = V.GetVarName(0, i);
      if (ss.IndexOf("__") != -1) continue; // some system variables are of type 3, but are differentiated by containing '__' somewhere in the name.
      if (first) {result += "<# black>";  first = false;}
      result += ss + equals + V.GetVarValue(0,i).ToString() + delim;
    }
    return result;
  }
  
// Formatted text, ready for JB.Show(..):
  public static string AssignmentsList(params int[] FromTo)
  { string termvar="", nm="", result = "";
    int n, vu, vfn, vat, startass = 0;  int endass = R.Assts.Count-1;
    if (FromTo.Length > 1){ n = FromTo[1];  if (n < endass) endass = n;}
    if (FromTo.Length > 0){ n = FromTo[0];  if (n > 0) startass = n;}
    string colhdgs = "<# magenta>Subject\tName\tVUse\tFn\tAt\tOpCd\tHier\tRat\tArgsDue\tOpCdRT\tRatRT\tValid\tX\n";
    // Set up TABS:
    result += "<stops 130, 250, 330>\n";

    // LOOP THROUGH ALL ASSIGNMENTS:
    for (int i = startass; i <= endass; i++)
    {// Header for this assignment:
      result += "<B>Assignment " + i.ToString() + ":       </B><# blue>";
      if (P.RawAsstText != null && P.RawAsstText.Count > i)
      { result += C.Legible(P.RawAsstText[i],3) + "    <# magenta>---->    <# blue>" + C.Legible(P.CookedAsstText[i],3); }
      result += "    <# magenta>---->    <# blue>" + ReconstructAsst(i) + '\n';
      result += colhdgs;
      // Add on the term lines for this assignment:
      for (int j = 0; j < R.Assts[i].Length; j++)
      { // Build the line intro, = name of the variable / dummy:
        vu =  Assts[i][j].VUse;  vfn = Assts[i][j].Fn;  vat = Assts[i][j].At;
       // Description of the subject of the term:
        if      (vu > 10)   termvar = "array";
        else if (vu == 10)  termvar = "unass'd array";
        else if (vu == 3)   termvar = "scalar";
        else if (vu == 2)   termvar = "int'l scalar";
        else if (vu == 1)   termvar = "constant";
        else if (vu == 0)  termvar = "unass'd scalar";
        else termvar = "????";
       // Name of the subject of the term:
        if (vfn >= 0) nm = V.GetVarName(vfn, vat);
        else if (vfn == P.NegatorMarker) nm = "Negate"; // -10
        else if (vfn == P.ComplementMarker) nm = "Complement"; // -15
        else if (vfn == P.SysFnMarker) nm = "SysFnResult"; // -20
        else if (vfn == P.UserFnMarker) nm = "UserFnResult"; // -30
        if (nm.Length > 18) nm = nm._Extent(0, 15) + "...";
        result += "<# black>" + termvar + '\t' + nm + '\t' + Assts[i][j].ToTabbedString() + '\n';
      }
      result += "\n";
    }
    return result;
  }
  
// Reconstruct the text of assignment AssNo from its terms in Assts[Ass]:
// No checks.
  public static string ReconstructAsst(int AssNo)
  { string nm="", bk, result="";
    TTerm[] asst = Assts[AssNo];
    int n,p,q, vfn, vat, nobkts, lasttm = R.Assts[AssNo].Length-1;
    for (int tm = 0; tm <= lasttm; tm++)
    { // Get variable name or dummy descriptor:
      vfn = asst[tm].Fn;  vat = asst[tm].At;
      if (vfn >= 0) nm = V.GetVarName(vfn, vat);
      else if (vfn == P.NegatorMarker || vfn == P.ComplementMarker) nm = ""; // the opcode '~' is sufficient identification.
      else if (vfn==P.SysFnMarker)nm = F.SysFn[vat].Name;
      else if (vfn==P.UserFnMarker) nm = P.UserFn[vat].Name;
      else if (vfn==P.UserFnMarker) nm = P.UserFn[vat].Name;
      else if (vfn==TempArrayCode) nm = " temp.array ";
      else nm = "???";
      // Insert brackets, before (if closers) or after (if openers) the name:
      if (tm == 0) result += nm;
      else
      { n = P.NestCoeff;   bk="";
        p = asst[tm-1].Rating / n;  q = asst[tm].Rating / n;
        nobkts = q-p;
        if (nobkts > 0){bk = ('(')._Chain(nobkts);  result += bk + nm;}
        else if (nobkts < 0){bk = (')')._Chain(-nobkts);  result += nm + bk;}
        else result += nm;
      }
      // Add on the operator:
      result += P.OpCdTransln[asst[tm].OpCode];
    }
    return result;
  }
//------------------------------------------------------------------------------
// Formatted text, ready for JB.Show(..):
// RawCookedBurnt --> how to display the asst.: R = from RawAsstText,
//  C = from CookedAsstText,  B = from fn. ReconstructAsst(.).
  public static string PreFlowTranslation(int FnLvl, char RawCookedBurnt)
  { string ss = "";  if (FnLvl == 0) ss = "Main Program"; else ss = "'"+P.UserFn[FnLvl].Name + "'";
    string result = "<B>PREFLOW STRING:   (Fn. Level "+FnLvl.ToString()+ " -- " + ss + ")</B>";
    if (P.RawAsstText == null)
    { return result + "   Can't do. Set P.Parse(.) arg. to TRUE."; }
    else if (P.PreFlow == null || P.PreFlow[FnLvl] == null)
    { return result + "   PreFlow not created yet."; }

    char ch; bool expectassno = false;  int assno;
    string assig="";
    StringBuilder sb = new StringBuilder();
    char[] puff = P.PreFlow[FnLvl].ToCharArray();
    result += "      (assignments in <B>";
    if (RawCookedBurnt == 'R') result += "<# blue>pre-parsed";
    else if (RawCookedBurnt == 'C') result += "<# magenta>parsed";
    else result += "<# red>reconstructed";
    result +=  "</B><# black> form)\n";

    for (int i = 0; i < puff.Length; i++)
    { ch = puff[i];
      if (ch == Char.MaxValue)
      { expectassno = true;  sb.Append(" \'"); } // apostrophe aliasses the
                                                // expect-asst-no. character.
      else if (expectassno)
      { assno = (int) ch;
        if (RawCookedBurnt == 'R') assig = P.RawAsstText[assno];
        else if (RawCookedBurnt == 'C') assig = P.CookedAsstText[assno];
        else assig = ReconstructAsst(assno);
        sb.Append(assig); sb.Append("  ");
        expectassno = false;
      }
      else if (ch == '\n') sb.Append(" ! ");
      else sb.Append(ch);
    }
    // Translate the codes in between assignments:
    result += sb.ToString();  result += '\n';
    int n; if (RawCookedBurnt == 'R') n = 0; else n = 3;

    return C.Legible(result, n);
  }

  public static string FlowLineLists(int FnLvl)
  { int n;
    string ss = ""; if (FnLvl == 0) ss = "Main Program"; else ss = "'" + P.UserFn[FnLvl].Name + "'";
    string result = "<B>FLOWLINE:   (Fn. Level " + FnLvl.ToString() + " -- " + ss + ")</B>\n";
    result += "\t";
    if (FlowLine == null || FlowLine[FnLvl] == null)
    { result += "*** NULL ***<# black>\n\n"; return result; }

    int flen = FlowLine[FnLvl].Length;
    for (int i = 0; i < flen; i++)
    { result += "<# magenta><B>" + i.ToString() + "</B><# black>\t"; }
    result += "\nAss:\t";
    for (int i = 0; i < flen; i++)
    { result += FlowLine[FnLvl][i].Ass.ToString();  result += "\t"; }
      result += "\nNext:\t";
    for (int i = 0; i < flen; i++)
    { result += FlowLine[FnLvl][i].Next.ToString();  result += "\t"; }
      result += "\nIfNot:\t";
    for (int i = 0; i < flen; i++)
    { result += FlowLine[FnLvl][i].IfNot.ToString();  result += "\t"; }
      result += "\nRef:\t";
    for (int i = 0; i < flen; i++)
    { result += FlowLine[FnLvl][i].Ref.ToString();  result += "\t"; }
      result += "\n\t<# green>";
    for (int i = 0; i < flen; i++)
    { n = FlowLine[FnLvl][i].Ass;
      if      (n == IFMarker)     ss = "if";
      else if (n == DummyMarker)  ss = "dummy";  
      else if (n == CONTMarker)   ss = "cont";
      else if (n == BREAKMarker)  ss = "break";
      else if (n == BACKIFMarker) ss = "backif";
      else if (n == ENDIFMarker)  ss = "endif";
      else if (n < 0) ss = "???";
      else if (n >= R.Assts.Count) ss = "?!?";
      else
      { ss = (C.Legible(P.RawAsstText[n], 0))._Purge(JS.WhiteSpaces);
        if (ss.Length > 5)ss = ss._Extent(0, 5)+"..";
      }
      result += ss + "\t";
    }
    result += "<# black>\n\n";
    return result;
  }

  /// <summary>
  /// <para>String for JB.Show. FromPtr is the first slot for data display, and ToPtr the last. It is safe to set ToPtr to
  ///  an oversized value or to -1; either way all slots will be displayed.</para>
  /// <para>If CapOnData[0] has a positive value, that is the max. no. of data elements that will be displayed per slot.
  ///  If exceeded, "..." will be appended to the last displayed value.</para>
  /// </summary>
  public static string ArrayStorageList(int FromPtr, int ToPtr, params int[] CapOnData)
  { string ss="";
    string result = "<B><U>ARRAY STORAGE</U></B>\n\n";
    long TotalArrayStorage = 0;
    // Find the last valid slot, and adjust ToPtr if nec.:
    int lastslot = 0;
    for (int i = Store.Count-1; i > 0; i--)
    { if (StoreUsage[i] > 0){ lastslot = i; break; } }
    if (ToPtr > lastslot || ToPtr < 0) ToPtr = lastslot;
    // Get displaying:
    for (int slot = FromPtr; slot <= ToPtr; slot++)
    { result += "<B>SLOT " + slot.ToString()+":</B>     (";
      if (StoreUsage.Count <= slot)
      { result += "'StoreUsage' undefined)\n\n";  continue; }
      if (StoreUsage[slot] == 1) result += "temporary storeroom)";
      else if (StoreUsage[slot] == 2) result += "assigned storeroom)";
      else { result += "Empty slot with NULL arrays)\n\n";  continue; }
      StoreItem story = Store[slot];
      int fullLen = story.TotSz, displayedLen = fullLen;
      TotalArrayStorage += (long) fullLen;
      if (CapOnData.Length > 0 && CapOnData[0] > -1 && CapOnData[0] < displayedLen)
      { displayedLen = CapOnData[0]; }
      if (story.IsChars) result += " (characters array)";
      int nodims = story.DimCnt;
      result += "\nType:\t"+ nodims.ToString() + "-dimensional array";
      if (nodims > 1)
      { ss = ",  ";
        for (int i = nodims-1; i >= 0; i--)
        { ss += story.DimSz[i].ToString();
          if (i > 0) ss += " x ";
        }
        ss += ".   Total size: ";
      }
      else ss = ".   Size: ";
      result += ss + fullLen.ToString();
      if (displayedLen < fullLen) result += "    (only the first " + displayedLen.ToString() + " values displayed)";
      int Fn = story.ClientFn,  At = story.ClientVarNo;
      result += "\nClient:\t(" + Fn.ToString() + ", " + At.ToString() + "): ";
      if (Fn < 0 || Fn > C.UserFnCnt || At < 0 || At >= V.Vars[Fn].Count)
      { result += "??? (invalid values)"; }
      else result += "'" + V.GetVarName(Fn,At) + "';   ";
      result = result._CullEnd(4); // length of the above ";   ".
      result += "\nDATA:\t";
      double[] data = story.Data;
      for (int i = 0; i < displayedLen; i++)
      { result += data[i].ToString(); if (i < displayedLen-1) result += ";  ";}
      if (displayedLen < fullLen) result += "...";
      result += "\n\n";
    }
    double tas = (double) TotalArrayStorage / 1.0e6;
    result += "Total Array Storage:  " + tas.ToString("F3") + " million values.\n";
    return result;
  }
// 
/// <summary>
/// AVLFSQ contents: 'A'(ssignmts), 'V'(ariables), 'L'(flowlines), 'F'(user fns), 'S'(tore), 'Q'(uotations). An empty string is equiv. to "AVLFSQ".
/// </summary>
  public static void Show(string AVLFSQ)
  { string whatho = "";
    AVLFSQ = AVLFSQ.ToUpper();    if (AVLFSQ == "")AVLFSQ = "AVLFSQ";
    if (AVLFSQ.IndexOf('A')>=0)
    { whatho += AssignmentsList(0, R.Assts.Count-1) + "\n\n\n"; }
    if (AVLFSQ.IndexOf('V')>=0)
    { for (int fn = 0; fn < C.UserFnCnt; fn++)
      { whatho += VariablesList(fn, 'L') + '\n'; }
      whatho += '\n'; }
    if (AVLFSQ.IndexOf('L')>=0)
    { for (int fn = 0; fn < C.UserFnCnt; fn++)
      { whatho += PreFlowTranslation(fn, 'R')
           + '\n' + PreFlowTranslation(fn, 'B')
           + '\n' + FlowLineLists(fn) + "\n\n"; } }
    if (AVLFSQ.IndexOf('F')>=0)
    { for (int fn = 0; fn < C.UserFnCnt; fn++)
      { whatho += P.UserFn[fn].ToString(); whatho += "\n\n"; }
    }
    if (AVLFSQ.IndexOf('S')>=0)
    { whatho += ArrayStorageList(0, -1, 200); } // -1 = all slots; last value = limit of data displayed per slot.
    if (AVLFSQ.IndexOf('Q')>=0)
    { whatho += "\n\n<b><u>QUOTATIONS</u></b>\n";
      if (P.Quotation == null) whatho += "NULL";
      else
      { for (int i = 0; i < P.Quotation.Count; i++)  whatho += i.ToString() + ":   \"" + P.Quotation[i] + "\"\n"; }
    }
    JD.PlaceBox(0.9, 0.9, -1, -1);
    JD.Display("", whatho, true, false, false, "CLOSE");
  }

  /// <summary>
  /// String for formatted display, giving the total array storage and the breakdown by arrays.
  /// Returns a Strint object, .S = string for display, .I = no. million values stored in arrays (rounded).
  /// </summary>
  public static Strint TotalArrayStorageSize()
  { string outstr = "<stops 250><in 30><b><u>ARRAY</u>\t<u>STORAGE (million values)</u></b>\n\n";
    long TotalArrayStorage = 0;
    double thisSize = 0.0;
    string thisName = "";
    for (int slot = 0; slot < Store.Count; slot++)
    { if (slot == StoreUsage.Count) break; // No need to explore slots that have never been accessed.
      StoreItem story = Store[slot];
      int n = StoreUsage[slot];
      if (n < 1) continue; // ignore null storerooms
      thisSize = (double) story.TotSz;
      TotalArrayStorage += (long) thisSize;
      bool isAssigned = (n == 2);
      if (!isAssigned) thisName = "(temporary)";
      else
      { int Fn = story.ClientFn,  At = story.ClientVarNo;
        if (Fn < 0 || Fn > C.UserFnCnt || At < 0 || At >= V.Vars[Fn].Count)
        { thisName = "(??? invalid name)"; }
        else thisName = V.GetVarName(Fn,At);
      }
      outstr += thisName + "\t" + (thisSize / 1.0e6).ToString("F3") + '\n';
    }
    double tas = (double) TotalArrayStorage / 1.0e6;
    outstr += "\n<b>Total:\t" + tas.ToString("F3") + "</b>\n";
    Strint result = new Strint( (int) Math.Round(tas), outstr);
    return result;
  }

//==============================================================================
// NOTES
//------------------------------------------------------------------------------

//==============================================================================
}// END of CLASS R
}// END of NAMESPACE MonoMaths


/*
================================================================================
NOTES ON 'CLIENTS' AND ARRAY BEHAVIOUR
--------------------------------------------------------------------------------

(1)aa=data(1,2,3,4);
(2)bb=aa;
(3)aa=aa*2;  [OR:]  bb = bb*2;

What happens when these three steps occur:
(1) sets up aa to point to storeroom p.
(2) causes bb also to point to the one storeroom p. Two arrays, one storeroom.
(3) either form busts up the link; the the LHS array (either  version) gets a
     new storeroom q, and only the unmentioned array continues to point to p.
This prevents the awful situation, so common in C, where changing one array
automatically changes the other. At least it does so in an assignment.

But there is one other risk area - system functions that not only have REF
arguments but also CHANGE the arrays referred to. For example, inc(Arr). not
only returns a temp. array (e.g. for 'NewArr = inc(Arr)' - NewArr is safely
insulated from Arr's storeroom) but also modifies the argument array.

My current decision:
(1) inc(), dec(): Matters left as is:
      aa=data(1,2,3); bb=aa; inc(aa);
will increment both bb and aa. It may be useful to do so, and it is easy to
avoid the event by: bb=aa*1; inc(aa);
(2) rotate(Arr1,Arr2,Arr3): adjusted so that above does not happen. In other
words, if some other array ArrX pointed to Arr1 before, it now still contains
the same data, but now shares that data storeroom with Arr2.


*/




