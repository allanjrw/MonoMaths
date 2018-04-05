using System;
using System.Collections.Generic;
using System.IO;
using JLib;

namespace MonoMaths
{

internal partial class F
{
  public static Quad SystemFunctionSet_C(int Which, int NoArgs, PairIX[] Args, Quad result)
  {
    switch (Which)
    {
      case 401: // UNUSED
      {
        break;
      }
      case 402: // EXIT_PLUS(array DoWhat [, FileName [, bool FormattedMode ] ] ) -- exit the program, then carry out further actions.
      // DoWhat = "new"  (case-insens.): carry out the same actions as for menu item "File | New", EXCEPT no warning displayed re unsaved text.
      // DoWhat = "load" (" "): same actions as for menu item "File | Load", again with no warning re overwriting unsaved text.
      //   In this case there must be a valid FileName, or the fn. simply acts as if the code had 'exit' instead of this function.
      // FormattedMode only applies to the 'load' case; if TRUE, the actions of "Appearance | Toggle Markup Tag System" are applied to the loaded file.
      {
        string ss = StoreroomToString(Args[0].I, true).ToUpper();
        if (ss == "NEW")
        { MainWindow.ExitPlus = new Strint2(1, 0, "", ""); // Handled in GO().
          result.S = "";  result.B = false; return result;
        }
        else if (ss == "LOAD")
        { if (NoArgs < 2) return Oops(Which, "the 'load' form of this fn. requires at least 2 args.");
          string fname = StoreroomToString(Args[1].I, true, true);
          string[] stuff = fname._ParseFileName(MainWindow.ThisWindow.CurrentPath); // If empty, will just trigger loading
          if (stuff == null) stuff = new string[] {"", ""}; // just triggers a dialog box in current directory.
          int n = 0;   if (NoArgs > 2 && Args[2].X != 0.0) n = 1;
          MainWindow.ExitPlus = new Strint2(2, n, stuff[0], stuff[1]); // Handled in GO().
          result.S = "";  result.B = false; return result;
        }
        else return Oops(Which, "unrecognized 1st. arg.");
      }
      case 403: // INSERT_IMAGE(array FilePathName, scalar Width, scalar Height, array LocationCue)
      // If successful, inserts the image where LocationCue first occurs in the Assts. Window, and in the process removes that cue.
      // Returns an array, which is " " (length 1) if success, and an error message (always longer than 1) if failure.
      // Only crashes if the first argument is scalar or has no printing chars.
      // Width and Height are in pixels. If either is less than 10 (e.g. 0 or negative), both will be ignored, and the natural size
      //   of the image will determine its size in the window.
     { string Result = " ";
        string filename = StoreroomToString(Args[0].I, true, true); // will be trimmed.
        if (filename == "")  return Oops(Which, "the 1st. arg. must be a file name");
        // This bit is here mainly to deal with abbreviations like "~/" and "./":
        string[] fillet = filename._ParseFileName(MainWindow.ThisWindow.CurrentPath);
        filename = fillet[0] + fillet[1];
        int Wd = Convert.ToInt32 (Args[1].X),   Ht = Convert.ToInt32 (Args[2].X);
        string cue = StoreroomToString(Args[3].I, true, true); // will be trimmed.
        Gtk.TextBuffer buffAss = MainWindow.ThisWindow.BuffAss;
        if (cue == "") Result = "No proper location cue was suppplied";
        else
        { Gtk.TextIter startIter = buffAss.StartIter, endIt = buffAss.EndIter, insertIter, afterIter;
          bool succeeded = startIter.ForwardSearch(cue, Gtk.TextSearchFlags.TextOnly, out insertIter, out afterIter, endIt);
          if (!succeeded) Result = "The location cue '" + cue + "' was not found";
          else
          { bool useShortForm = (Wd < 10 || Ht < 10);
            bool Success = true;
            Gdk.Pixbuf px = null;
            try
            { if (useShortForm) px = new Gdk.Pixbuf(filename);
              else              px = new Gdk.Pixbuf(filename, Wd, Ht);
            }
            catch { Success = false; }
            if (!Success) Result = "Unable to load the image file '" + filename + "'";
            else
            { buffAss.Delete(ref insertIter, ref afterIter);
              buffAss.InsertPixbuf(ref insertIter, px);
            }
          }
        }
        result.I = V.GenerateTempStoreRoom(Result.Length);
        StringToStoreroom(result.I, Result);
        break;
      }
    // case 404: ISNAN(.) -- see Hybrids.
    case 405: // RANDRANGE(.) - Returns random numbers within a stated range. VOID in all cases (unlike 'rand').
    // Args: (VariableName, LowLimit, HighLimit [, IntegerOnly]). VariableName - name of a persistent scalar or array;
    //   LowLimit, HighLimit obvious; if 'IntegerOnly' present and nonzero, returned values may be either limit, as well as in between.
    // If limits are crossed, they are automatically reversed; i.e. the order of limits is not important.
    // Special case: If VariableName is an array, and if LowLimit and HighLimit are both arrays of the same size, then individual
    //   elements of the array will range over the individual corresponding limits.
    // Crashing error: VariableName not a persistent variable.
    { int inslot = Args[0].I;
      bool isScalar = (inslot == -1);
      double[] data;
      int inFn = REFArgLocn[0].Y,   inAt = REFArgLocn[0].X;
      byte use = V.GetVarUse(inFn, inAt);
      if (use != 3 && use != 11) return Oops(Which, "the 1st. arg. must be either a named array or a named nonsystem scalar");
      isScalar = (use == 3);
      if (isScalar) data = new double[] {Args[0].X };
      else data = R.Store[inslot].Data; // NB - a pointer assignment, not a copy! Data will thus be directly written to the array.
      int dataCnt = data.Length;
      bool intOnly = (NoArgs > 3 && Args[3].X != 0.0);
      int loslot = Args[1].I, hislot = Args[2].I;
      bool scalarLimits = (loslot == -1 && hislot == -1);
      double[] lolimits = null, hilimits = null;
      if (!scalarLimits)
      { if (isScalar || loslot == -1 || hislot == -1) return Oops(Which, "limits must either both be scalar or both arrays. If arrays, then the 1st. arg. must also be an array");
        lolimits = R.Store[loslot].Data;   hilimits = R.Store[hislot].Data;
        if (lolimits.Length != dataCnt || hilimits.Length != dataCnt) return Oops(Which, "array limits must have the same length as the first arg.");
      }
      double loLimit, hiLimit;
      if (scalarLimits)
      { loLimit = Args[1].X;    hiLimit = Args[2].X;
        if (intOnly) { loLimit = Math.Round(loLimit);  hiLimit = Math.Round (hiLimit); }
        if (loLimit == hiLimit) { for (int i=0; i < dataCnt; i++) data[i] = loLimit; }
        else // unequal limits:
        { if (loLimit > hiLimit) { double x = loLimit; loLimit = hiLimit; hiLimit = x; }
          double range = hiLimit - loLimit;
          if (intOnly)
          { int intLo = Convert.ToInt32(loLimit),  intHi = 1 + Convert.ToInt32(hiLimit); // recall, the high limit is included as a possible output value
            for (int i=0;  i < dataCnt; i++) data[i] = Rando.Next(intLo, intHi);
          }
          else for (int i=0;  i < dataCnt; i++) data[i] = loLimit + range * Rando.NextDouble();
        }
      }
      else // array limits, and array 1st. arg., all arrays of same length:
      { for (int i=0; i < dataCnt; i++)
        { loLimit = lolimits[i];  hiLimit = hilimits[i];
          if (intOnly) { loLimit = Math.Round(loLimit);  hiLimit = Math.Round (hiLimit); }
          if (loLimit == hiLimit) data[i] = loLimit;
          else
          { if (loLimit > hiLimit) { double x = loLimit; loLimit = hiLimit; hiLimit = x; }
            if (intOnly) data[i] = Rando.Next(Convert.ToInt32(loLimit), Convert.ToInt32(hiLimit));
            else data[i] = loLimit + (hiLimit - loLimit) * Rando.NextDouble();
          }
        }
      }
      // Return the data:
      if (isScalar) V.SetVarValue(inFn, inAt, data[0]);
      // If an array, then the contents of 'data' is already installed, as 'data' is only a pointer.
      break;
    }
    // case 406: FIXANGLE(.) -- see Hybrids.
    case 407: // DISTMUTUAL(array XValues [, array YValues [, array ZValues ]], [ scalar DontTakeSquareRoot [, scalar ReturnFullMatrix ] ]).
    // Arrays must have the same length (if more than 1). Each set of elements [i] represents coordinates of the ith. point in 1D/2D/3D space.
    // Returns a matrix which acts as a table to give the distance between any two points.
    // Normally only the upper right triangular matrix is returned, the lower triangle holding all zeroes; if the last arg. is present & TRUE,
    //   both triangles are filled (the main diagonal obviously remaining zero).
    // In the case of ONE DIMENSION, returned values are SIGNED ( the sign being that of X[column] - X[row]). Consequently, if you use the
    //  'ReturnFullMatrix' option, signs in the lower triangle will be the reverse of those in the upper triangle.
    //  Also, note that the value of 'DontTakeSquareRoot' is ignored for one dimension.
    {
      string argtypes = ArgTypesCode(Args, ' ');
      if (argtypes.IndexOf("SA") != -1) return Oops(Which, "scalar args. must come after all array args.");
      int noDims = argtypes.IndexOf ('S');
      if (noDims == -1) noDims = NoArgs;
      if (noDims == 0 || noDims > 3) return Oops(Which, "args. must start off with at least 1 and no more than 3 arrays");
      StoreItem Sitem_X = R.Store[Args[0].I],  Sitem_Y = null,  Sitem_Z = null;
      double[] XCoords = Sitem_X.Data,  YCoords = null,  ZCoords = null;
      int noPoints = XCoords.Length;
      if (noDims > 1)
      { Sitem_Y = R.Store[Args[1].I];  YCoords = Sitem_Y.Data;
        if (YCoords.Length != noPoints) return Oops(Which, "1st. and 2nd. arrays have unequal lengths");
      }
      if (noDims > 2)
      { Sitem_Z = R.Store[Args[2].I];  ZCoords = Sitem_Z.Data;
        if (ZCoords.Length != noPoints) return Oops(Which, "3rd. array has a length different to the earlier arrays");
      }
      // Boolean args:
      int noScalarArgs = NoArgs - noDims;
      bool dontTakeSqRoot = (noScalarArgs > 0 && Args[noDims].X != 0.0);
      bool returnFullMx = (noScalarArgs > 1 && Args[noDims+1].X != 0.0);
      double x, y, z, t;
      // Get distances:
      double[] distances = new double[noPoints*noPoints];
      { for (int i=0; i < noPoints-1; i++)
        { int offset = i*noPoints;
          // The following 'if' segments hopefully increase speed, at the cost of extra program length.
          if (noDims == 1) // in this case, any arg 'dontTakeSqRoot' is ignored, and values returned are SIGNED, not absolute values.
          { for (int j=i; j < noPoints; j++) distances[offset+j] = XCoords[j] - XCoords[i]; }
          else if (noDims == 2)
          { for (int j=i; j < noPoints; j++)
            { x = XCoords[j] - XCoords[i];    y = YCoords[j] - YCoords[i];
              t = x*x + y*y;
              if (!dontTakeSqRoot) t = Math.Sqrt(t);
              distances[offset+j] = t;
            }
          }
          else if (noDims == 3)
          { for (int j=i; j < noPoints; j++)
            { x = XCoords[j] - XCoords[i];    y = YCoords[j] - YCoords[i];    z = ZCoords[j] - ZCoords[i];
              t = x*x + y*y + z*z;
              if (!dontTakeSqRoot) t = Math.Sqrt(t);
              distances[offset+j] = t;
            }
          }
        }
      }
      // Load the lower triangle of the matrix, if required.
      if (returnFullMx)
      { for (int i=1; i < noPoints; i++)
        { int offset = i*noPoints;
          if (noDims > 1)
          { for (int j=0; j < i; j++) distances[offset + j] = distances[j*noPoints + i]; }
          else
          { for (int j=0; j < i; j++) distances[offset + j] = -distances[j*noPoints + i]; }
        }
      }
      // Return the result:
      result.I = V.GenerateTempStoreRoom(noPoints, noPoints);
      R.Store[result.I].Data = distances;
      break;
    }
    case 408: // DISTPOINTS(array X1Values [, array Y1Values [, array Z1Values ]],  array X2Values [, array Y2Values [, array Z2Values ]],
    //                      [ scalar DontTakeSquareRoot]).
    // Returns a matrix of all distances between the 1st. and 2nd. set of points, mx dims. being (no. pts. in 1st. set) x (no. in 2nd. set).
    // Must be even no. of arrays, 2 (1 dim.) to 6 (3 dim.). The remaining arg., if present, must be scalar.
    // In the case of ONE DIMENSION, returned values are SIGNED ( the sign being that of X[j] of set 2 - X[i] of set 1).
    { // *** If adding further scalar args. at the end, the arg. testing below will have to be more complex; it is based on 0 or 1 scalars at end.
      bool takeSqRoot = (NoArgs % 2 == 0 || Args[NoArgs-1].X == 0.0); // Odd no. of args. indicates that there is a last scalar arg.
      int noDims = NoArgs / 2; // the no. args. is limited to 2 to 7, so this must always be 1 to 3.
      for (int i=0; i < noDims*2; i++) { if (Args[i].I == -1) return Oops(Which, "a scalar arg. found where an array was expected"); }
      StoreItem Sitem_X1 = R.Store[Args[0].I],  Sitem_Y1 = null,  Sitem_Z1 = null;
      StoreItem Sitem_X2 = R.Store[Args[noDims].I],  Sitem_Y2 = null,  Sitem_Z2 = null;
      double[] X1Coords = Sitem_X1.Data,  Y1Coords = null,  Z1Coords = null;
      double[] X2Coords = Sitem_X2.Data,  Y2Coords = null,  Z2Coords = null;
      int noPoints1 = X1Coords.Length,   noPoints2 = X2Coords.Length;
      if (noDims > 1)
      { Sitem_Y1 = R.Store[Args[1].I];   Sitem_Y2 = R.Store[Args[noDims+1].I];
        Y1Coords = Sitem_Y1.Data;        Y2Coords = Sitem_Y2.Data;
        if (Y1Coords.Length != noPoints1 || Y2Coords.Length != noPoints2) return Oops(Which, "a 2nd. dimension coordinate array has the wrong length");
      }
      if (noDims > 2)
      { Sitem_Z1 = R.Store[Args[2].I];   Sitem_Z2 = R.Store[Args[noDims+2].I];
        Z1Coords = Sitem_Z1.Data;        Z2Coords = Sitem_Z2.Data;
        if (Z1Coords.Length != noPoints1 || Z2Coords.Length != noPoints2) return Oops(Which, "a 3rd. dimension coordinate array has the wrong length");
      }
      double x, y, z, t;
      // Get distances:
      double[] distances = new double[noPoints1*noPoints2];
      int offset;
      { for (int i=0; i < noPoints1; i++)
        { offset = i * noPoints2;
          // The following 'if' segments hopefully increase speed, at the cost of extra program length.
          if (noDims == 1)
          { for (int j=0; j < noPoints2; j++)  distances[offset+j] = X2Coords[j] - X1Coords[i];
          }
          else if (noDims == 2)
          { for (int j=0; j < noPoints2; j++)
            { x = X2Coords[j] - X1Coords[i];    y = Y2Coords[j] - Y1Coords[i];
              t = x*x + y*y;
              if (takeSqRoot) t = Math.Sqrt(t);
              distances[offset+j] = t;
            }
          }
          else if (noDims == 3)
          { for (int j=0; j < noPoints2; j++)
            { x = X2Coords[j] - X1Coords[i];    y = Y2Coords[j] - Y1Coords[i];    z = Z2Coords[j] - Z2Coords[i];
              t = x*x + y*y + z*z;
              if (takeSqRoot) t = Math.Sqrt(t);
              distances[offset+j] = t;
            }
          }
        }
      }
      // Return the result:
      result.I = V.GenerateTempStoreRoom(noPoints2, noPoints1);
      R.Store[result.I].Data = distances;
      break;
    }
    case 409: // SETALIAS(any number of declared variable NAMES, up to MaxREFArgs names). Assigns integers from 0 upwards as
    // aliasses for variables belonging to this function (only), integers being assigned in the order in which arguments occur.
    // Any assignable variable may be aliased except where it has not yet been referenced (so that its 'Use' field is 0).
    // Variables may be accessed via system fn. 'alias(.)'. All aliasses vanish when the function finishes running, except for
    // main program aliasses, which persist or not in accordance with whether 'R.KillData(.)' is called or not.
    // Should be USED ONLY ONCE in any one function's code; it won't crash if you use it more than once, but each subsequent use
    // will remove all prior aliasses before setting up the new set.
    // ANY ERROR CRASHES the program. Errors include: Unassigned or nonpersistent variable; a variable name not recognized.
    // RETURNS: the number of arguments. Hence, if it returns N, then there are assignments for aliasses 0 to N-1.
    {
      if (V.VarAliasses == null) // then we create this list of arrays for all user functions, not just the present one.
      { V.VarAliasses = new List<Strint2>[C.UserFnCnt];
        for (int i = 0; i < C.UserFnCnt; i++) V.VarAliasses[i] = new List<Strint2>();
      }
      int thisFn = R.CurrentlyRunningUserFnNo,   varNo;
      V.VarAliasses[thisFn].Clear();
      Strint2[] struggle = new Strint2[NoArgs];
      for (int i=0; i < NoArgs; i++)
      { if (REFArgLocn[i].Y != thisFn) return Oops(Which, "the {0}th. arg. does not belong to this function", i+1);
        varNo = REFArgLocn[i].X;
        byte usage = V.GetVarUse(thisFn, varNo);
        if (usage != 3 && usage != 11) return Oops(Which, "the {0}th. arg. is not a declared and assignable variable", i+1);
        string ss = V.GetVarName(thisFn, varNo); // Should never fail, as the assigning of REFArgLocn has done the identification work.
        struggle[i] = new Strint2(varNo, i, ss, ""); // i is the alias.
      }
      // if got here, no errors...
      V.VarAliasses[thisFn].AddRange(struggle);
      result.X = NoArgs;
      break;
    }
    case 410: // GETVAL(scalar AliasIndex [, one or more scalar values, being address for an array segment ])
    // If scalar, returns its value. If array, returns the whole array (if no address arguments) or else exactly what any array address
    //  would return, except that to indicate the whole of an inner dimension, use -1 (or MINREAL): if 5 is the alias of 'Arr',
    //  "getval(5, -1, 2)" is equiv. to "Arr[][2]". (No other negative value is allowed; it would cause a crash with message.)
    case 411: // SETVAL(scalar AliasIndex ...) VOID. Two versions. If AliasIndex turns out to refer to a scalar, then only the next scalar
    // value is accessed (without a test for arrayhood): "setval(AliasIndex, x)". If an array, then the first 0+ args. after AliasIndex
    // must be scalars, and are read as the address of the array segment to receive data; if no array follows, and the address indicates
    // a single datum, then the final scalar is taken as that datum. Otherwise the array is taken as the data.
    // E.g., if alias 5 corresponds to a 2x3 matrix Mx, "setval(5, 0, data(10, 20, 30))" is equiv. to "Mx[0] = data(10, 20, 30)";
    // "setval(5, 0, 1, 11)" is equiv. to "Mx[0,1] = 11". For "Mx[][1] = data(1, 2)" use -1 or MINREAL: "setval(5, -1, 1, data(1, 2)".
    // (No other negative address is allowed; it would cause a crash with message.)
    {
     // Get at the aliassed variable:
      if (V.VarAliasses == null) return Oops(Which, "no aliasses have been assigned yet in this function");
      if (Args[0].I != -1) return Oops(Which, "the 1st. arg. must be scalar, being the alias of some variable");
      int thisAlias = Convert.ToInt32(Args[0].X);
      int thisFn = R.CurrentlyRunningUserFnNo;
      List<Strint2> aliassary = V.VarAliasses[thisFn];
      int foundat = -1;
      for (int i=0; i < aliassary.Count; i++)
      { if (aliassary[i].IY == thisAlias) { foundat = i;  break; } }
      if (foundat == -1) return Oops(Which, "no variable corresponds to this alias");
      int thisVarNo = aliassary[foundat].IX;
      byte usage = V.GetVarUse(thisFn, thisVarNo);
      // Scalar variable was aliassed:
      if (usage == 3)
      { if (Which == 410)  result.X = V.GetVarValue(thisFn, thisVarNo);
        else V.SetVarValue(thisFn, thisVarNo, Args[1].X);
        break;
      }
      else if (usage != 11) return Oops(Which, "Aliassed variables must be existing and declared arrays or scalars"); // e.g. 'kill(.)' was used.
    // Array variable was aliassed:
      int inslot = V.GetVarPtr(thisFn, thisVarNo);
      int nonaddressArgs = Which - 409; // 1 for 'GETval', 2 for 'SETval'
      int addressLen = NoArgs - nonaddressArgs;
      if (addressLen == 0)
      // the whole array was specified:
      { if (Which == 410) // GETvar:
        { StoreItem source = R.Store[inslot];
          double[] outdata = source.Data._Copy();
          int[] dims = source.DimSizes;
          result.I = V.GenerateTempStoreRoom(dims);
          StoreItem destn = R.Store[result.I];
          destn.Data = outdata;
          destn.IsChars = source.IsChars;
        }
        else // SETvar:
        { int donorFn = REFArgLocn[NoArgs-1].Y,  donorAt = REFArgLocn[NoArgs-1].X;
          if (donorFn == R.TempArrayCode)// then the donor is a temporary array:
          { V.TransferTempArray(Args[NoArgs-1].I, thisFn, thisVarNo); }
          else V.MakeCopy(donorFn, donorAt, thisFn, thisVarNo);
        }
      }
      else // A segment of the aliassed array was specified:
      {// Prepare to run this method F.RunSysFn(.) recursively, to apply function "__assign(.):
        double[] address = AccumulateValuesFromArgs(Args, 1, addressLen);
        PairIX[]  pricks = new PairIX[nonaddressArgs + addressLen];
        pricks[0] = new PairIX(inslot, 0.0);
        for (int j=0; j < addressLen; j++)
        { double d = address[j];  if (d == -1.0) d = double.MinValue; // the case where a column is required
          pricks[j+1] = new PairIX(-1, d);
        }
        if (nonaddressArgs == 1) // GETvar:
        { Quad squad = RunSysFn(32, pricks); // this is system fn. "__segmt", which returns a copy of the designated segment of the aliassed array.
          if (!squad.B) return Oops(Which, squad.S);
          else { result.I = squad.I;  result.X = squad.X; } // the return can be scalar or array
        }
        else // SETvar:
        { pricks[nonaddressArgs + addressLen - 1] = Args[NoArgs-1]; // Fine, whether this arg. is scalar or array
          Quad quid = RunSysFn(33, pricks); // system fn. "__assign".
          if (!quid.B) return Oops(Which, quid.S);
          // this system fn. is void, so don't set 'result'.
        }
      }
      break;
    }
    case 412: // PREVALENCE(InArray, ...) - Two distinct modes, distinguished by whether 2nd. arg. is SCALAR.
    // Mode 1 -- 2 or 3 args; 2nd. is SCALAR: (InArray, scalar MinimumOccurrences [, scalar ExcludedValue] ) -- registers the number
    //   of times that all values occur within InArray. (If 3rd. arg. present and true, that value is excluded from consideration.)
    //   RETURNS a MATRIX: result[0,i] is the value, result[1,i] is the number of times it occurs in InArray.
    //   If MinimumOccurrences rounds to <= 1 and there is no 3rd. arg., there would always be valid data returned. If MinimumOccurrences
    //   rounds to > 1, then only duplicated values occur, and only if duplicated at least that no. of times. If none so duplicated,
    //   the 'empty' array [NaN] is returned. Again, if all remaining values are ExcludedValue, [NaN] is returned.
    //   ORDER: The matrix is sorted by result[1], so that values occurring more often are represented in lower columns. Where two or more
    //   values are duplicated the same number of times, the order of values amongst these tying occurrences is indeterminate, due to the
    //   internal workings of the sorting method. To sort within tying occurrences would involve extra time overhead; at present I am avoiding it.
    // Mode 2 -- Two or three args, ALL arrays. (A) Two-arg version: (InArray, SpecificValues). RETURNS a LIST ARRAY of size(SpecificValues)+1;
    //   For all but the last, result[i] is the number of occurrences in InArray of SpecificValues[i]; result[last] is the number of elements
    //   In InArray not present in SpecificValues. (B) Three-arg verson: (InArray, LowValues, HighValues) - the two last arrays must have the
    //   same size. This time result[i] is incremented if InArray[k] >= LowValues[i] and < HighValues[i]. Again, failures recorded in result[last].
    {
      int inslot = Args[0].I;  if (inslot == -1) return Oops(Which, "the 1st. arg. must be an array");
      double[] indata; // Can't go further here, as mode 1 needs a copy but mode 2 can use pointer.
      int loSlot = Args[1].I;
      bool isMode1  = (loSlot == -1);
      bool isMode2A = (!isMode1 && NoArgs == 2);
      double[] outdata;
      if (isMode1)
      {
        int minDuplicns = Convert.ToInt32(Args[1].X);  if (minDuplicns < 1) minDuplicns = 1;
        bool omitAValue = (NoArgs > 2);
        double omitThis = 0.0; // dummy value, or compiler complains.
        if (omitAValue) omitThis = Args[2].X;
        // Sort the copy of InArray:
        indata = R.Store[inslot].Data._Copy(); // A copy, because we are going to sort it.
        JS.Sort (indata, true); // sort in ascending order.
        int inlen = indata.Length,   cntr = 1;
        List<double> values = new List<double>(inlen),  occurrences = new List<double>(inlen);
        double x = 0, xlast = 0; // dummy values
        bool firstValidValueProcessed = false;
        for (int i=0; i < inlen; i++)
        { x = indata[i];  if (omitAValue && x == omitThis) continue; // ignore it completely (so skip reset of xlast at the end of this loop).
          if (firstValidValueProcessed)
          { if (x == xlast)  cntr++;
            else
            { if (cntr >= minDuplicns) { values.Add(xlast);   occurrences.Add(cntr); }
              cntr = 1;
            }
          }
          else firstValidValueProcessed = true; // to ensure that the above loop is only omitted when the first valid value has been found.
          xlast = x;
        }
        // Deal with the last element of the array.
        if (firstValidValueProcessed && (cntr >= minDuplicns) ) { values.Add(xlast);   occurrences.Add(cntr); }
        // Get the stuff back to the user:
        if (values.Count == 0) { result.I = EmptyArray(); break; }
        double[] valuesArr = values.ToArray();
        double[] occurArr = occurrences.ToArray();
        int arrLen = valuesArr.Length;
        if (arrLen > 1) JS.SortByKey(valuesArr, occurArr, false); // sorts the two arrays together, in descending order of occurArr elements.
        outdata = new double[2 * arrLen];
        valuesArr.CopyTo(outdata, 0);   occurArr.CopyTo(outdata, arrLen);
        result.I = V.GenerateTempStoreRoom(arrLen, 2);
      }
      else // Mode 2A or 2B:
      { indata = R.Store[inslot].Data;
        double[] loData = R.Store[loSlot].Data;
        int inDataLen = indata.Length,  loDataLen = loData.Length;
        outdata = new double[loDataLen+1];
        if (isMode2A)
        { for (int i=0; i < inDataLen; i++)
          { double x = indata[i];
            bool foundit = false;
            for (int j=0; j < loDataLen; j++)
            { if (x == loData[j]) { outdata[j]++;  foundit = true;  break; } }
            if (!foundit) outdata[loDataLen]++;
          }
        }
        else // Mode2B:
        { double[] hiData = R.Store[Args[2].I].Data;
          if (hiData.Length != loDataLen) return Oops(Which, "the last two arrays must have the same length");
          for (int i=0; i < inDataLen; i++)
          { double x = indata[i];
            bool foundit = false;
            for (int j=0; j < loDataLen; j++)
            { if (x >= loData[j]  &&  x < hiData[j]) { outdata[j]++;  foundit = true;  break; } }
            if (!foundit) outdata[loDataLen]++;
          }
        }
        result.I = V.GenerateTempStoreRoom(loDataLen+1);
      }
      R.Store[result.I].Data = outdata;
      break;
    }
    case 413: // __CONSTANT(scalar NewConstant, scalar Value) -- Installs NewConstant as a scalar constant.
    // **** NB: If you change the name of this function, also change the ref. to it in unit Parser, where along with 'dim' it is preprocessed.
    {
      int ndx = REFArgLocn[0].X;
      int ff = V.ResetUserConstant(ndx, Args[1].X);
      if (ff == -2) return Oops(Which, "cannot reset the system constant {0}", V.GetVarName(0, ndx));
      if (ff != 1) return Oops(Which, "What the hey went wrong?");
      break;
    }
    case 414: // FILTER(array InArr, scalar WindowWidth, scalar StepSize, array Mode [, array HandlingEdges ] ) -- apply a filter
    // across InArr, returning an array of filtered data with the same dimensionality (1D to 3D) but (usually) different dimensions.
    // The filter never overlaps edges; it starts with the bottom left of the window at InMx[0,0] and ends with top right at InMx[last,last],
    // where 'last' is such that a further step would take the window partly or wholly over the edge.
    // WindowWidth should always be odd, though the fn. would still work if not.
    // Mode: See code below for allowed values, still rather changeable.
    // HandlingEdges: *** Not accessed at present. I imagine it would only be relevant for StepSize = 1, to bring output matrix size up to
    //   input matrix size.
    {
      int inslot = Args[0].I,  modeslot = Args[3].I;
      if (inslot == -1 || modeslot == -1) return Oops(Which, "either the 1st. or the 4th. arg. is not an array");
      StoreItem inItem = R.Store[inslot];
      double[] indata = inItem.Data;
      int[] inDims = inItem.DimSz;
      int inDimCnt = inItem.DimCnt;
      int windowWd = Convert.ToInt32(Args[1].X),  stepSize = Convert.ToInt32(Args[2].X);
      if (windowWd < 1 || stepSize < 1) return Oops(Which, "either the 2nd or 3rd. scalar value is less than one (or is an array)");
      string Mode = StoreroomToString(modeslot, true, true);
      int ModeNo = 0;
      if (Mode == "average") ModeNo = 1;
      else if (Mode == "maximum") ModeNo = 10;
      if (ModeNo == 0) return Oops(Which, "'{0}' is not a valid mode", Mode);
      // Work out the loop limits: *** If TVar.MaxNoDims is ever increased beyond 3, you'll have to modify the stuff below to cater
      //  for more dims. As it is, no error would be raised, but only the first 3D piece of a higher dimensional structure would be processed.
      int stepsCols, stepsRows = 1, stepsDeep = 1; // the last two have defaults, to apply for the 1D case.
      bool youErred = false;
      stepsCols = 1 + (inDims[0] - windowWd) / stepSize; // '1' is the starting position; then the window advances in steps of stepSize to
             //  a further (inDims[0] - windowWd) steps to reach the far end. This is integer division, so a partial final step won't be included.
      if (stepsCols < 1) youErred = true;
      else if (inDimCnt > 1)
      { stepsRows = 1 + (inDims[1] - windowWd) / stepSize;
        if (stepsRows < 1) youErred = true;
        else if (inDimCnt > 2)
        { stepsDeep = 1 + (inDims[2] - windowWd) / stepSize;
          if (stepsRows < 1) youErred = true;
        }
      }
      if (youErred) return Oops(Which, "The window width is greater than a dimension of the input array");
      // Define the output array. *** If 'HandlingEdges' is one day coded for, then this will change...
      double[] outdata = new double[stepsDeep * stepsRows * stepsCols];
      int[] outDims; // We will set it to length 3, so we can use the 3D loop below for all cases.
      int windowSize;
      if      (inDimCnt == 1) { outDims = new int[] {stepsCols, 0, 0};                  windowSize = windowWd; }
      else if (inDimCnt == 2) { outDims = new int[] {stepsCols, stepsRows, 0};          windowSize = windowWd * windowWd; }
      else                    { outDims = new int[] {stepsCols, stepsRows, stepsDeep};  windowSize = windowWd * windowWd * windowWd; }
      // Extract the strips of input data which are to be processed with the window:
      double x;
      double[] substrate = new double[windowSize];
      int indims0 = inDims[0];
      int substrateStart, inputRowsIn, inRowsOffset,  startInRow;
      int outdataCntr = 0;
      for (int outblock = 0;  outblock < stepsDeep; outblock++)
      {
        for (int outrow = 0;  outrow < stepsRows;  outrow++)
        {
          startInRow = outrow * stepSize;
          for (int outcol = 0;  outcol < stepsCols;  outcol++)
          {
            substrateStart = 0;
            for (int inblockIncmt = 0; inblockIncmt < windowWd;  inblockIncmt++)
            { if (inDimCnt < 3 && inblockIncmt > 0) break;
              for (int inrowIncmt = 0;  inrowIncmt < windowWd;  inrowIncmt++)
              { if (inDimCnt < 2 && inrowIncmt > 0) break;
                inputRowsIn = startInRow + inrowIncmt;
                inRowsOffset = inputRowsIn * indims0;
                Array.Copy (indata, inRowsOffset + outcol*stepSize, substrate, substrateStart, windowWd );
                substrateStart += windowWd;
              }
            }
            // Operate on it:
            if (ModeNo == 1) // average of values in substrate:
            { x = 0;
              for (int i=0; i < windowSize; i++) x += substrate[i];
              outdata[outdataCntr] = x / windowSize;    outdataCntr++;
            }
            else if (ModeNo == 10) // return the maximum value in substrate
            { x = substrate[0];
              for (int i=1; i < windowSize; i++) { if (substrate[i] > x) x = substrate[i]; }
              outdata[outdataCntr] = x;    outdataCntr++;
            }
          }
        }
      }
      result.I = V.GenerateTempStoreRoom(outDims);
      R.Store[result.I].Data = outdata;
      break;
    }
    case 415: // POINTSOF(PlotID, ScaledCoordinates [, OriginalShape] ) -- returns a matrix of all X, Y (, Z) values for the plot.
    // If ScaledCoordinates true, returns coords. in accordance with the graph scale; otherwise returns pixel coordinates relative
    //  to the whole drawing surface (i.e. not relative to the graph box margins).
    // Returns a matrix of size 2 x M (2D; 3D as pixels) or 3 x M (3D as scaled coords).
    // OriginalShape: only accessed if the plot is a shape plot (which is always 2D), and if ScaledCoordinates is true. If so, and if
    //  OriginalShape is present and true, then the returned matrix will be of the shape before any translation and rotation
    //  (from MoveShape, or from including args. PivotX etc. in the original PlotShape function call). Otherwise it is the default:
    //  the current shape vertices are returned. (Note that in the case of pixel coordinates, the returned points are always of the moved shape.)
    // ERRORS: (1) PlotID not recognized: No crash, but returns the 'empty' array.
    // BIG WARNING: If you have a shape, want scaled coords, and have set OriginalShape to FALSE, so you want the moved shape: UNLESS AND UNTIL
    //  you have graphed the moved shape, you will get either the original shape or (if shape was earlier moved and then graphed) the previous
    //  shape position. This is because class Shape does not actually compute vertex coordinates until the moved shape is drawn to the graph.
    // BIG WARNING 2: If the plot is featured in two or more graphs, only the points as they occur at the latest graphing
    //  will be returned, with no information as to which graph that was. Only an issue for pixel coordinates, or for shapes present on several
    //  graphs but just moved in one of them.
    { int plotID = Convert.ToInt32(Args[0].X);
      Plot splot = null;
      foreach (Plot plotto in MainWindow.Plots2D)
      { if (plotto.ArtID == plotID) { splot = plotto;  break; } }
      if (splot == null) { result.I = EmptyArray();  break; }
      bool isPixelCoords = (Args[1].X == 0.0);
      bool getMovedCoords = (splot is Shape  &&  !isPixelCoords  &&  (NoArgs < 3  ||  Args[2].X == 0.0) );
            // This boolean is therefore always FALSE for all non-Shape plots.
      double[] outdata;
      int[] dims = new int[3];
      if (isPixelCoords)
      { double[] Xpix = splot.Xpixels,   Ypix = splot.Ypixels;
        int pixLen = Xpix.Length;
        outdata = new double[2*pixLen];
        Xpix.CopyTo(outdata, 0);   Ypix.CopyTo(outdata, pixLen);
        dims[0] = pixLen;  dims[1] = 2;
      }
      else // scaled coordinates
      { double[] XX, YY;
        if (getMovedCoords) // that is, the shape, if moved, has actually been drawn on the graph:
        { XX = (splot as Shape).CornersX;
          if (XX != null) YY = (splot as Shape).CornersY;
          else { XX = splot.XCoords;  YY = splot.YCoords; }
        }
        else { XX = splot.XCoords;  YY = splot.YCoords; }
        int XXlen = XX.Length;
        if (splot.Is3D)
        { outdata = new double[3*XXlen];
          dims[0] = XXlen;  dims[1] = 3;
          XX.CopyTo(outdata, 0);   YY.CopyTo(outdata, XXlen);   splot.ZCoords.CopyTo(outdata, 2*XXlen);
        }
        else // 2D
        { outdata = new double[2*XXlen];
          dims[0] = XXlen;  dims[1] = 2;
          XX.CopyTo(outdata, 0);   YY.CopyTo(outdata, XXlen);
        }
      }
      result.I = V.GenerateTempStoreRoom(dims);
      R.Store[result.I].Data = outdata;
      break;
    }
    case 416: // CLUSTER(char array Action,  InMatrix, ...)
    // Action = "resolve": (Action, InMatrix, IncludeDiagonallyTouching): InMatrix should contain only two values: -1 (for 'Empty' boxes)
    //   and 0 (for 'Null' boxes). The returned matrix will have the same dimensions as InMatrix; all clusters of empty boxes will be identified,
    //   and each filled with a unique positive integer. The integers will have sequence gaps usually, as the algorithm tentatively labels
    //   subclusters and then relabels them if these merge. Nulls (0) are left as is.
    //   NO CHECK for values other than 0 and -1; if they are there, no doubt awful things will happen - garbage-in, garbage-out will apply.
    // Action = "single": (Action, InMatrix, IncludeDiagonallyTouching, ThisRow, ThisColumn, FillValue): InMatrix can have any values. Whatever is the
    //   value at the indicated location, clusters of that value will be sought AS LONG AS the value there is not 0 (which is again 'Null').
    //   The returned matrix will have the original value replaced by FillValue everywhere in the cluster; other values in InMatrix stay as is.
    //   (if the value at the location is already 0, the function will return SCALAR 0).
    {// #### Make crashproof for all -1 or all 0.
      int inslot = Args[1].I;  if (inslot == -1) return Oops(Which, "2nd. arg. must be a matrix");
      StoreItem insitem = R.Store[inslot];
      int[] inDims = insitem.DimSz;
      int noRows = inDims[1], noCols = inDims[0];  if (noRows == 0 || inDims[2] != 0) return Oops(Which, "the 2nd. arg. should be a matrix");
      double[] indata = insitem.Data._Copy();
      int indataLen = indata.Length;
      bool allowCornerTouch = (Args[2].X != 0);
      int[] touchRows, touchCols;
      if (allowCornerTouch) // E   SE  S  SW   W   NW  N  NE
      { touchRows = new int[] {0, -1, -1, -1,  0,  1,  1, 1};
        touchCols = new int[] {1,  1,  0, -1, -1, -1,  0, 1};
      }
      else //                  E  S   W  N
      { touchRows = new int[] {0, -1, 0, 1};
        touchCols = new int[] {1, 0, -1, 0};
      }
      int noNeighbours = touchRows.Length;
      string Action = StoreroomToString(Args[0].I);
      bool isSingle = (Action == "single"),   isResolve = (Action == "resolve");
      if (!isSingle && !isResolve) return Oops(Which, "unrecognized value of 1st. arg.");
      int thisRow = -1, thisCol = -1, thisBox = -1;
      double Null = 0.0;
      if (isSingle)
      { if (NoArgs != 6) return Oops(Which, "there should be 6 args. for Action = 'single'");
        double clusterValue = Args[5].X;  if (clusterValue == 0.0) return Oops(Which, "the last arg. must be a nonzero scalar");
        int[] fullNbrsRow = new int[noNeighbours], newNbrsRow = new int[noNeighbours];
        for (int i=0; i < noNeighbours; i++) fullNbrsRow[i] = 1;
        thisRow = Convert.ToInt32(Args[3].X);
        thisCol = Convert.ToInt32(Args[4].X);
        if (thisRow < 0 || thisRow >= noRows || thisCol < 0 || thisCol >= noCols)
        { return Oops(Which, "the last 2 args. do not constitute a valid address within the given matrix"); }
        thisBox = thisRow * noCols + thisCol;
        double coreValue = indata[thisBox];
        if (coreValue == Null) break; // i.e return SCALAR 0
        indata[thisBox] = clusterValue; // would not be set by the loop below.
        // Set up the stacks
        Stack<int[]> stackNbrs = new Stack<int[]>(); // *** NB! This stack is ELECTRIC - it only holds a pointer, with the result that
                                    // later changes to the pushed array outside of the stack also change the array (supposedly) on the stack.
        Stack<Duo> stackXY = new Stack<Duo>();
        stackXY.Push(new Duo(thisCol, thisRow));
        fullNbrsRow.CopyTo(newNbrsRow, 0);
        stackNbrs.Push(newNbrsRow);
        newNbrsRow = new int[noNeighbours]; // and so break newNbrsRow's connection to the stack
        Duo thisBoxRef, nextBoxRef;
        int[] whatsLeft;
        int nextMove;
        while (true)
        {
          thisBoxRef = stackXY.Peek(); // Leave it on the stack for now.
          whatsLeft = stackNbrs.Peek(); // Leave it on the stack for now. NB - even though we used Peek and not Poke, STILL any
                                        //  changes to 'whatsLeft' carry through to the array on the top of the stack.
          nextMove = -1;
          for (int i=0; i < noNeighbours; i++) { if (whatsLeft[i] == 1) { nextMove = i;  break; }  }
          if (nextMove == -1)
          { if (stackNbrs.Count == 1) break;   // nothing left undone on the stack
            stackXY.Pop(); // get rid of this box from the stack, as it has no more neighbours to test
            stackNbrs.Pop();
            continue;
          }
          // If got here, there is work to do for this square.
          nextBoxRef.X = thisBoxRef.X + touchCols[nextMove];
          nextBoxRef.Y = thisBoxRef.Y + touchRows[nextMove];
          whatsLeft[nextMove] = 0; // to avoid revisiting the new square when revisiting the old square on the stack
                                   // As mentioned above, we have just changed the array on the stack as well, by this move.
          int zeroat;
          if (nextBoxRef.X >= 0 && nextBoxRef.X < noCols && nextBoxRef.Y >= 0 && nextBoxRef.Y < noRows)
          {
            if (indata[nextBoxRef.Y*noCols + nextBoxRef.X] == coreValue)
            {
              indata[nextBoxRef.Y*noCols + nextBoxRef.X] = clusterValue;
              fullNbrsRow.CopyTo(newNbrsRow, 0); // safe because we dereferenced newNbrsRow earlier.
              // avoid useless reexploring the parent cell, so set the reciprocal move to zero:
              zeroat = (nextMove+noNeighbours/2) % noNeighbours;
              newNbrsRow[zeroat] = 0;
              stackXY.Push(nextBoxRef);
              stackNbrs.Push(newNbrsRow); newNbrsRow = new int[noNeighbours];
            }
          }
        }
      }
      else // Action = "resolve":
      { double lastlabel,  thislabel,  nextLabelNo = 1.0;
        double Empty = -1.0;
        for (int rw=0; rw < noRows; rw++)
        {
          lastlabel = Null;  thislabel = Null;
          for (int cl=0; cl < noCols; cl++)
          {
            thislabel =  indata[rw*noCols + cl];
            if (thislabel != Null)
            { // Assign a label to this box, if empty
              if (thislabel == Empty)
              { if (lastlabel == Null) {  thislabel = nextLabelNo;   nextLabelNo++;   indata[rw*noCols + cl] = thislabel;  }
                else { thislabel = lastlabel;   indata[rw*noCols + cl] = thislabel; }
              }
              else if (lastlabel != Null  &&  thislabel != lastlabel) // do nothing, if this label already = last label
              { for (int i=0; i < indataLen; i++) { if (indata[i] == lastlabel) indata[i] = thislabel; } }
                  // this label has precedence, and all instances of last label are switched to this label.
             // Check the contact squares above and below:  (Below is nec, as a cluster may curve upwards, ahead,
             //       and then hook downwards under this row, ahead of this column.)
              for (int ii = 0; ii < noNeighbours; ii++)
              { int crow =  rw + touchRows[ii];
                if (crow >= 0 && crow < noRows)
                { int coll = cl + touchCols[ii];
                  if (coll >= 0 && coll < noCols)
                  { double x =  indata[crow*noCols + coll];
                    if (x != Null)
                    { if (x == Empty) indata[crow*noCols + coll] = thislabel;
                      else if (x != thislabel)
                      { for (int j=0; j < indataLen; j++) { if (indata[j] == thislabel) indata[j] = x; }
                        thislabel = x;
                      }
                    }
                  }
                }
              }
            }
            lastlabel = thislabel;
          }
        }
      }
      result.I = V.GenerateTempStoreRoom(inDims);
      R.Store[result.I].Data = indata;
      break;
    }
    case 417: // MXCENTRE(char. array Mode, InMatrix [, bool RoundToNearestEven) -- find whatever type of centre is required by Mode.
    // The return is always an array of size 2. If the result is determinate and in range, this array is [ row index, column index ].
    // Otherwise [-1, -1] is returned. So [0] should be tested before using the return.
    // Mode = "of mass": Centre of mass is returned, taking into account actual values (including negative ones). In many cases where
    //   negative values are present, this would produce a COM off the matrix, in which case [ -1, -1 ] is returned.
    // Mode = "of rectangle": Centre of the rectangle which encloses all nonzero values is returned.
    // All modes: If no 3rd. arg., or it is FALSE, then rounding is away from zero instead.
    {
      string Mode = StoreroomToString(Args[0].I);  if (Mode == "") return Oops(Which, "1st. arg. should be a char. array");
      int inmxslot = Args[1].I;
      int noRows = 0, noCols = 0;
      double[] indata = null;
      if (inmxslot >= 0)
      { StoreItem sitem = R.Store[inmxslot];
        if (sitem.DimCnt == 2)
        { noRows = sitem.DimSz[1];   noCols = sitem.DimSz[0];
          indata = sitem.Data;
        }
        else inmxslot = -1; // to trigger the error message...
      }
      if (inmxslot == -1) return Oops(Which, "2nd. arg. must be a matrix");
      int indataLen = indata.Length;
      double[] outdata = new double[2];
      bool skipRounding = false; // only set to 'true' where outdata has been set to the failure result {-1, -1}.
      bool skipRangeTest = false; // after rounding has occurred, the range test of rounded values is skipped if they can't possibly be out of range.
      if (Mode == "of rectangle")
      {
        int loRow = indataLen, loCol = indataLen, hiRow = 0, hiCol = 0;
        for (int i = 0; i < indataLen; i++)
        { int rw = i / noCols,  cl = i % noCols;
          if (indata[i] != 0.0)
          { if (rw < loRow) loRow = rw;  if (rw > hiRow) hiRow = rw;
            if (cl < loCol) loCol = cl;  if (cl > hiCol) hiCol = cl;
          }
        }
        if (loRow == indataLen)
        { outdata[0] = -1.0;  outdata[1] = -1.0;  skipRounding = true; } // indeterminate, because no nonzero values in the matrix
        else
        { outdata[0] = ((double)loRow + (double)hiRow) / 2.0;
          outdata[1] = ((double)loCol + (double)hiCol) / 2.0;
          skipRangeTest = true; // impossible for values to round to out-of-range values.
        }
      }
      else if (Mode == "of mass")
      { double[] sumRows = new double[noCols], sumCols = new double[noRows];
        double x;
        for (int i = 0; i < indataLen; i++)
        { int rw = i / noCols,  cl = i % noCols;
          x = indata[i];
          sumRows[cl] += x;   sumCols[rw] += x;
        }
        bool isIndeterminate = false;
        double sumSumRows = 0.0,  sumSumCols = 0.0;
        double sumRowMoments = 0.0,  sumColMoments = 0.0;
        for (int i=0; i < noCols; i++) { sumSumRows += sumRows[i];  sumRowMoments += sumRows[i]* (double) i; }
        if (sumSumRows == 0.0) isIndeterminate = true;
        else
        { for (int i=0; i < noRows; i++) { sumSumCols += sumCols[i];  sumColMoments += sumCols[i]* (double) i; }
          if (sumSumCols == 0.0) isIndeterminate = true;
        }
        if (isIndeterminate) { outdata[0] = outdata[1] = -1.0;  skipRounding = true; }
        else
        { outdata[0] = sumColMoments / sumSumCols; // Can be out of range; but we check for that at the end of the function.
          outdata[1] = sumRowMoments / sumSumRows;
        }
      }
      else return Oops(Which, "1st. arg. is not a recognized mode");
      // ROUNDING:   Each Mode's code must ensure that values are such that rounding would not create out-of-range values at the top end.
      if (!skipRounding)
      { MidpointRounding moo = MidpointRounding.AwayFromZero;
        if (NoArgs > 2 && Args[2].X != 0.0) moo = MidpointRounding.ToEven;
        outdata[0] = Math.Round(outdata[0], moo);  outdata[1] = Math.Round(outdata[1], moo);
        if (!skipRangeTest) // don't waste time on this check if it is unnecesary
        { if (outdata[0] < 0.0 || outdata[0] >= (double) noRows || outdata[1] < 0.0 || outdata[1] >= (double) noCols)
          { outdata[0] = -1.0;  outdata[1] = -1.0; }
        }
      }
      result.I = V.GenerateTempStoreRoom(2);
      R.Store[result.I].Data = outdata;
      break;
    }
    case 418: // GRAPHVISIBLE(GraphID [, bool MakeVisible ]) -- If 2 args, changes visibility accordingly (if it was different);
    // in any case returns the current visibility of the graph. (Nonexistent graph would also return FALSE - no error raised.)
    { int graphID = (int) Args[0].X;
      Graph graf = null;
      Trio trill = Board.GetBoardOfGraph(graphID, out graf);
      if (graf == null) break; // returning 'false'.
      int doWhat = 0; // tells method below just to return visibility status, without altering it.
      if (NoArgs > 1)
      { if (Args[1].X == 0.0) doWhat = -1;  else doWhat = 1; }
      // We now have the board...
      if (Board.VisibilityStatus(trill.X, doWhat)) result.X = 1.0;
      break;
    }
    case 419: // CURSORPOSN(array Window [,  scalar Where ] ) -- returns the final cursor position. If a second arg, moves it first.
    // 'Where' = desired character position for placement. If rounds to below zero, reset to 0; if beyond end of text (e.g. MAXREAL), at end.
    { char windowID = ' ';
      double x = R.Store[Args[0].I].Data[0];
      if (x == 65 || x == 97) windowID = 'A';  else if (x == 82 || x == 114) windowID = 'R';  else break;
      int n = -1; // neg. values leave the cursor where it is.
      if (NoArgs > 1)
      { x = Args[1].X;
        if (x >= (double) int.MaxValue)  n = int.MaxValue;
        else if (x < -0.5) n = -1;
        else n = Convert.ToInt32(x);
      }
      result.X = (double) MainWindow.ThisWindow.CursorPosition(windowID, n);
      break;
    }
    case 420: // MAINMENU(array MenuTitles [, bool MakeVisible ] )
    // ONE ARG VERSION: MenuTitle must exactly match one of the ten main menu titles, with no leading or trailing spaces.
    //   If the menu name is identified, its visibility status is returned. Output coding: 1 = visible, 0 = invisible, -1 =
    //   menu name not identified.
    // TWO ARG VERSION: This time MenuTitle may be more than one menu title (delimiter = "|"), but there must be no leading
    //   or trailing spaces per menu title. If found, the menu(s) are made visible / invisible according to the last argument.
    //   The return is (trivially) 1 if MakeVisible TRUE or 0 if FALSE; it is -1 if ANY of the names is not recognized.
    //   SPECIAL CASE: If MenuTitle is exactly "ALL", then all ten menus are made visible / invisible.
    // Note that all menu items return to visibility at the end of the run.
    // Also note that only main menu item titles can be used; this function does not affect submenu visibilities.
    {
      int titleSlot = Args[0].I;  if (titleSlot == -1) return Oops(Which, "1st. arg. must be an array of menu name(s)");
      string argstr = StoreroomToString(titleSlot);
      string[]titles = argstr.Split(new char[]{'|'},  StringSplitOptions.RemoveEmptyEntries);
      if (titles.Length == 0) return Oops(Which, "no menu names supplied");
      char visible = '?';
      if (NoArgs > 1)
      { if (Args[1].X == 0.0) visible = 'I';  else visible = 'V'; }
      int output = 0;
      bool errorFound = false;
      for (int i=0; i < titles.Length; i++)
      { output = MainWindow.ThisWindow.MenuVisibility(titles[i], visible);
        if (output == -1) errorFound = true;
        if (visible == '?' || titles[i] == "ALL") break; // after the first name has been handled.
      }
      if (errorFound) result.X = -1.0;
      else result.X = (double) output;
      break;
    }
    case 421: // KEYNAME( scalar/array KeyValues [, array Delimiter] ) -- Returns key name(s) corresponding to key value(s) in
    // the first argument. If the JTV function called below cannot find a name, it simply returns "#" + the key value.
    // If there is no second argument but more than one value in KeyValues, the default delimiter is simply a space.
    // A key value of 0 returns "null". PROVISO: Names found by trial and error on my computer, and are not nec. general to other computers.
    {
      double[] values;
      int valSlot = Args[0].I;
      if (valSlot == -1) values = new double[] {Args[0].X};  else values = R.Store[valSlot].Data;
      string ss = "";
      string delim = " ";
      if (NoArgs > 1 && Args[1].I >= 0) delim = StoreroomToString(Args[1].I);
      int len = values.Length;
      for (int i=0; i < len; i++)
      {
        int n = Convert.ToInt32(values[i]);
        ss += JTV.KeyDenomination(n);
        if (i < len-1) ss += delim;
      }
      double[] outdata = StringToStoreroom(-1, ss);
      result.I = V.GenerateTempStoreRoom(outdata.Length);
      R.Store[result.I].Data = outdata;
      R.Store[result.I].IsChars = true;
      break;
    }
    case 422: // SOLVEEXP: Two forms allowed:
    // (A) one arg.: solveexp(char. array Expression) -- Expression must be a sequence of values (constants, variables
    //   in scope, literal values) with appropriately placed operation signs (allowed: +. -. *, /, ^) and possibly with appropriately
    //   placed brackets '(', ')' (nesting allowed). Any spaces, tabs or par. marks will be removed before processing begins. No other
    //   chars. should be allowed, and no functions are allowed.
    // (B) two arg.: solveexp(char. array Expression, array Values) -- This time no values, constants or variables are allowed in
    //   Expression. Instead, values are all stored in the 2nd. arg. Values, and references to them in Expression are in the form
    //   "{n}" (where n is an explicit integer, the index of the value in Values).
    {
      int expslot = Args[0].I;  if (expslot == -1) return Oops(Which, "the 1st. arg. must be an array (the expression to solve)");
      double[] values = null;
      if (NoArgs > 1)
      { int valslot = Args[1].I;
        if (valslot == -1) values = new double[] {Args[1].X};
        else values = R.Store[valslot].Data;
      }
      string expression = StoreroomToString(expslot);
      bool isFormA = (expression.IndexOf('{') == -1); // No braces, so assumed to be the one-arg. form (A) as described above.
      if (isFormA) // then we have to build arguments that would be suitable for form B:
      { string errmsg;
        expression = P.ParseExpession(expression, out values, out errmsg, true);
        if (errmsg != "") return Oops(Which, errmsg);
      }
      // Form B code, to which Form A has now been conformed:
      string oopsie;
      double z = JS.SolveExpression(expression, values, out oopsie);
      if (oopsie != "") return Oops(Which, "Problem with expression: " + oopsie);
      result.X = z;
      break;
    }
    case 423: // SIGMOID(array XX, scalar IndexCoefficient, scalar DenomCoefficient, char array Mode ) -- sigmoid functions.
    // Only the first character of Mode is accessed. Structure of XX ignored.
    // Mode = "rising": returns 1 / (1 + D.C. * e^-(I.C. * XX) ). Hence, 0 for X=-infinity, 1 for X = infinity.
    // Mode = "falling": Returns 1 - the above. Hence, 1 for X = -infinity, 0 for X = infinity.
    // Mode = "compressed": Returns  1 / ( (XX/D.C.)^I.C. + 1). Intended for use for X >= 0.
    //      Values: X = 0 --> 1; X = D.C. --> 0.5; X = infinity --> 0. No cliff for I.C. = 1, cliff more sheer as I.C. increases.
    // RETURNS a list array. Crashes if Mode present but unrecognizable
    { 
      int inslot = Args[0].I;  if (inslot == -1)  return Oops(Which, "1st. arg. must be an array");
      double[] indata = R.Store[inslot].Data;
      int dataLen = indata.Length;
      double x, IndexCoeff = Args[1].X,  DenomCoeff = Args[2].X;
      int modeslot = Args[3].I;
      if (modeslot == -1) return Oops(Which, "4th. arg. must be an array");
      double Mode = R.Store[modeslot].Data[0];
      double[] outdata = new double[dataLen];
      if (Mode == 114 || Mode == 102) // 114 = "r",  102 = "f"
      { for (int i=0; i < dataLen; i++)
        { x = 1.0 / (1.0 + DenomCoeff * Math.Exp(-IndexCoeff * indata[i]) );
          if (Mode == 114) outdata[i] = x;  else outdata[i] = 1 - x;
        }
      }
      else if (Mode == 99) // 99 = "c"
      { bool negIndex = (IndexCoeff < 0.0);
        if (negIndex) IndexCoeff = -IndexCoeff;
        double z = 1 / DenomCoeff;
        for (int i=0; i < dataLen; i++)
        { x = 1.0 / (1.0 + Math.Pow(z *  indata[i], IndexCoeff));
          if (negIndex) outdata[i] = 1.0 - x; else outdata[i] = x; 
        }
      }
      else return Oops(Which, "4th. arg. is not a recognized operation code");
      result.I = V.GenerateTempStoreRoom(dataLen);
      R.Store[result.I].Data = outdata;
      break;
    }
    case 424: // SEQUENCE (StartValue, any no. of named scalars) -- the Nth scalar receives the new value StartValue+(N-1).
    // If the .Use of a scalar is not 3, an error is raised. The return is the number of named scalar arguments.
    // StartValue does not have to be a named value. (If an array, defaults to 0, with no error message.
    // StartValue is rounded. *** I have deliberately avoided other parameters, e.g. a step argument. For anything else
    //  the much more powerful function "unpack" should be used.
    { double startValue = Math.Round(Args[0].X);
      for (int arg = 1; arg < NoArgs; arg++)
      { int Fn = REFArgLocn[arg].Y,  At = REFArgLocn[arg].X;
        if (Fn < 0) return Oops(Which, "the {0}th arg. is not an existing variable name", arg + 1);
        int varuse = V.GetVarUse(Fn, At);
        if (varuse == 0 || varuse == 3) // Receiver is SCALAR. Unassigned scalars allowed here. (Unit Parser had set up unrecog'd. names as such.)
        { V.SetVarValue(Fn, At, startValue); }
        else return Oops(Which, "the {0}th arg. is not a user-definable scalar variable", arg + 1);
        startValue += 1.0;
      }
      result.X = startValue;
      break;
    }    
    case 425: // CRASH(same args. as for 'text(.)' etc.)  Crashes the program from anywhere, leaving the error msg. in Results Window.
    { string displaytext = ArgsToDisplayString(0, Args, REFArgLocn, "");
      result.S = displaytext;  result.B = false; return result;
      // For once, no 'break' is needed!
    }
    case 426: // PLOTMX(matrix Mx [, Xcoords [, PtShape [, PtWidth [, PtColour [, LnShape [, LnWidth [, LnColour ]]]]]]] )
    // Plots each row of Mx separately against Xcoords. Xcoords must be a list array, and must have length exactly of rows of Mx.
    // PtShape ... LnColour have the same meaning as for "plot" EXCEPT that for the Type and Shape args., array items refer to whole
    // curves. Array items are addressed by modulo no. rows of Mx, so e.g. for a Mx with 5 rows, if PtWidth is data(1, 2, 3), for the
    // five curves point widths would be 1, 2, 3, 1, 2. Individual colours within PtColour and LnColour are treated likewise.
    // As a result of this system, there is no way of having different point features or line features across any one curve.
    // JAGGED MATRICES: If you want curves to be plotted only as far as a jagged matrix padder, first convert the padder in your
    // matrix to MAXREAL, as any row in Mx will be truncated at this value.
    // Corollary: If you want to cancel the plotting of some curve within Mx, simply install MAXREAL as its first element. 
    // RETURNED: A LIST ARRAY of plot IDs.
    {
      // First argument:
      int mxslot = Args[0].I;  if (mxslot == -1) return Oops(Which, "1st. arg. cannot be scalar");
      StoreItem mxitem = R.Store[mxslot];
      int[] dimsz = mxitem.DimSz;
      int noRows = dimsz[1];   if (noRows == 0) return Oops(Which, "1st. arg. must be a matrix");
      int rowLen = dimsz[0];
      double[] YY = mxitem.Data;
      // Second argument (or lack thereof):
      double[] XX;
      int Xslot = -1;
      if (NoArgs == 1) // then invent a set of X coordinates:
      { XX = new double[rowLen];   
        for (int i = 0; i < rowLen; i++)  XX[i] = i;
      }
      else
      { Xslot = Args[1].I;   if (Xslot == -1) return Oops(Which, "2nd. arg. must be an array");
        XX = R.Store[Xslot].Data;
        if (XX.Length != rowLen) return Oops(Which, "2nd. arg. must have the same length as rows of the 1st. arg. matrix");
      }
      // Retrieve the remaining six args., or if any absent, install default values:
      int dummy = 0; // needed because we are calling functions originally written for function "plot(.)", which needed that REF arg's output value.
      char[] ptshape, lnshape;   double[] ptwidth, lnwidth;   Gdk.Color[] ptclr = null, lnclr = null;
     // Point and line shape:
      if (NoArgs > 2) ptshape = Plot_Shape(Args[2].I, Args[2].X, ref dummy); // If scalar, taken as a unicode value.
        else ptshape = new char[] { '.' }; // default point - a dot. 
      if (NoArgs > 5) lnshape = Plot_Shape(Args[5].I, Args[5].X, ref dummy);
        else lnshape = new char[] { '_' }; // default line - continuous. 
     // Point and line width:
      if (NoArgs > 3) ptwidth = Plot_Width(Args[3].I, Args[3].X, ref dummy);
        else ptwidth = new double[] {3.0};
      if (NoArgs > 6) lnwidth = Plot_Width(Args[6].I, Args[6].X, ref dummy);
        else lnwidth = new double[] {1.0};
     // Point and line colour:
      if (NoArgs > 4) ptclr = Plot_Colour(Args[4].I, Args[4].X, ref dummy, JTV.Black);
        else ptclr = new Gdk.Color[] { JTV.Blue };
      if (NoArgs > 7) lnclr = Plot_Colour(Args[7].I, Args[7].X, ref dummy, JTV.Black);
        else lnclr = new Gdk.Color[] { JTV.Blue };
      // PLOT IT ALL:
      double[] PlotIDs = new double[noRows];
      for (int i=0; i < noRows; i++)
      { int inset = i*rowLen;
        Plot plotto = null;
        int ptshapeIndex = i % ptshape.Length,   ptwidthIndex = i % ptwidth.Length,  ptclrIndex = i % ptclr.Length;   
        int lnshapeIndex = i % lnshape.Length,   lnwidthIndex = i % lnwidth.Length,  lnclrIndex = i % lnclr.Length;   
        // Unfortunately we have to convert these single values to arrays:
        char[] thisptshape = new char[] { ptshape[ptshapeIndex] };
        char[] thislnshape = new char[] { lnshape[lnshapeIndex] };
        double[] thisptwidth = new double[] { ptwidth[ptwidthIndex] };
        double[] thislnwidth = new double[] { lnwidth[lnwidthIndex] };
        Gdk.Color[] thisptclr = new Gdk.Color[] { ptclr[ptclrIndex] };
        Gdk.Color[] thislnclr = new Gdk.Color[] { lnclr[lnclrIndex] };
        int n = YY._Find(double.MaxValue, inset, inset + rowLen - 1);
        if (n == inset) continue; // no curve to plot, as MAXVALUE is the first character.
        double[] yyo;
        if (n != -1) // then there is a MAXREAL, so curtail arrays:
        { double[] xxo = XX._Copy(0, n-inset);   yyo = YY._Copy(inset, n-inset);
          plotto = new Plot(xxo, yyo, thisptshape, thisptwidth, thisptclr, thislnshape, thislnwidth, thislnclr);
        }
        else
        { yyo = YY._Copy(inset, rowLen);
          plotto = new Plot(XX, yyo, thisptshape, thisptwidth, thisptclr, thislnshape, thislnwidth, thislnclr);
        }
        MainWindow.Plots2D.Add(plotto);
        PlotIDs[i] = (double) plotto.ArtID;
      }
      result.I = V.GenerateTempStoreRoom(noRows);
      R.Store[result.I].Data = PlotIDs;
      break;
    }
    case 427:  case 428: // ROTATEROW / ROTATECOL (Matrix, scalar RowColNo [, scalar NoTimes [, scalar PadValue
    //                                                    [, scalar FromIndex [, scalar ToIndex ] ] ] ] -- VOID. 
    // Rotates in situ a single row or column. NoTimes: N (pos or neg) --> content of Matrix[i] --> Matrix[i+N].
    // If no 'PadValue' arg., or if it is an array (like "wrap"), wraparound occurs, all values being preserved.
    //   Otherwise the last value to move is replaced by PadValue.
    // No error raised if Matrix is not a named array, but the fn. isn't much use to you if the arg. is a temporary array.
    // If FromIndex - ToIndex doesn't cover any of the row or column, simply no rotation occurs. Adjustments:
    //     FromIndex < 0 --> 0;  ToIndex < 0 or too large --> end of the row or column. 
    {
      bool isRow = (Which == 427); 
      int inslot = Args[0].I;  if (inslot == -1) return Oops(Which, "1st. arg. cannot be scalar");
      StoreItem inItem = R.Store[inslot];
      double[] indata = inItem.Data;
      int[] dims = inItem.DimSz;
      int noRows = dims[1], noCols = dims[0];
      if (noRows == 0) return Oops(Which, "1st. arg. must be a matrix");
      int NoTimes = 1;
      int whichStrip = Convert.ToInt32(Args[1].X); // We don't bother checking for scalarhood.
      if (NoArgs > 2) NoTimes = Convert.ToInt32(Args[2].X);
      if (NoTimes == 0) break; // do nothing. Now guaranteed that 0 < abs(NoTimes) < no. rows / cols. 
      double padder = 0;
      bool wrap = (NoArgs <= 3 || Args[3].I >= 0);
      if (!wrap) padder = Args[3].X;
      int startAt = 0, endAt = -1;
      if (NoArgs > 4) { startAt = Convert.ToInt32(Args[4].X);  if (startAt < 0) startAt = 0; }
      if (NoArgs > 5) { endAt = Convert.ToInt32(Args[5].X);  if (endAt < 0) endAt = -1; }
      int ndx;
      int stripLen = (isRow) ? noCols : noRows;
      // Extract the old strip:
      double[] oldstrip = new double[stripLen];
      if (isRow)
      { if (whichStrip < 0 || whichStrip >= noRows) return Oops(Which, "row index is out of range");
        oldstrip = inItem.Data._Copy(whichStrip * noCols, noCols);      
      }
      else
      { if (whichStrip < 0 || whichStrip >= noCols) return Oops(Which, "column index is out of range");
        // dissect out the original column from the matrix:
        for (int i=0; i < noRows; i++) // the donor
        { oldstrip[i] = indata[i * noCols + whichStrip]; }
      }
      // Rotate the strip:
      double[] newstrip = oldstrip._Copy(); // Where start and end pts were specified, we want the rest of newstrip to be the same as oldstrip
      if (endAt == -1 || endAt >= stripLen) endAt = stripLen-1;
      int sublen = endAt - startAt + 1;
      double implant; 
      for (int i = startAt; i <= endAt; i++)
      { ndx = i + NoTimes;
        if (!wrap && (ndx < startAt || ndx > endAt)) implant = padder;
        else implant = oldstrip[i];
        while (ndx > endAt)   ndx -= sublen;
        while (ndx < startAt) ndx += sublen;
        newstrip[ndx] = implant;
      }
      // Implant the new row or column:
      if (isRow)
      {  Array.Copy(newstrip, 0, inItem.Data, whichStrip * noCols, noCols);
      }
      else
      { 
        for (int j=0; j < noRows; j++)
        inItem.Data[j * noCols + whichStrip] = newstrip[j];
      }      
      break;
    }
    case 429: // MULTIBOX(array Heading, array LayoutString, array Texts, array ButtonTitles [, array TextsDelimiter ] )
    // TextsDelimiter: Normally '|' separates between texts. If you want that char. to be literally part of a text, or want to avoid the
    // possibility of the user inserting this as part of the text, then provide any printable character (i.e. above 'space') as the delimiter.
    // RETURNS the button ID.
    { bool oopsie = false;
      string heading =  StoreroomToString(Args[0].I);      if (heading == "") oopsie = true;
      string layout  =  StoreroomToString(Args[1].I);      if (layout == "") oopsie = true;
      string texts   =  StoreroomToString(Args[2].I);      if (texts == "") oopsie = true;
      string buttontitles = StoreroomToString(Args[3].I);  if (buttontitles == "") oopsie = true;
      if (oopsie) return Oops(Which, "all args. must be arrays");
      char delimChar = '|';
      if (NoArgs > 4)
      { string ss = StoreroomToString(Args[4].I);
        if (ss[0] > '\u0020') delimChar = ss[0];
      }
      string[] textsArray = texts.Split(new char[] {delimChar});
      // CALL THE METHOD:
      int btn = JD.MultiBox(heading, ref layout, ref textsArray, buttontitles);
      if (btn == 0) { MainWindow.StopNow = 'G'; R.LoopTestTime = 1; }// Icon closure. This value ensures that the very next end-of-loop
             // test will check for user's wish to end the program. But R.LoopTestTime will then be automatically reset to its orig. value of 100.
      if (btn < 0)
      { string errmsg = "???";
        if      (btn == -1000) errmsg = "no printable chars. in the 2nd. arg.";
        else if (btn == -1001) errmsg = "invalid character in the 2nd. arg.";
        else if (btn == -1002) errmsg = "no button titles supplied in the 4th. arg.";
        else if (btn == -1003) errmsg = "there was not exactly one text per widget in the 3rd. arg.";
        return Oops(Which, errmsg);
      }
      StoreItem sitem = R.Store[Args[1].I];
      double[] xx = StringToStoreroom(-1, layout);  if (xx.Length < 1) return Oops(Which, "?? programming error!");
      sitem.Data = xx;
      sitem.DimCnt = 1;  sitem.DimSz = new int[TVar.MaxNoDims];   sitem.DimSz[0] = xx.Length;
      StoreItem sitem1 = R.Store[Args[2].I];
      string textsBack = String.Join(delimChar.ToString(), textsArray);
      if (textsBack == "") textsBack = " ";
      string dely = delimChar.ToString();
      textsBack = textsBack.Replace(dely + dely,  dely + " " + dely);     
      if (textsBack[0] == delimChar) textsBack = " " + textsBack;
      if (textsBack[textsBack.Length-1] == delimChar) textsBack = textsBack + " ";
      xx = StringToStoreroom(-1, textsBack);
      sitem1.Data = xx;
      sitem1.DimCnt = 1;  sitem1.DimSz = new int[TVar.MaxNoDims];   sitem1.DimSz[0] = xx.Length;
      result.X = (double) btn;
      break;
    }
    case 430: // SETBOX(four values in any format: Width, Height, CentreX, CentreY). Settings for the next dialog box (as invoked
    // by 'show', 'request', 'decide'; it has no effect on file dialog boxes).
    // Sets box size and location either as a fraction of screen extent (if 0 < the dimension <= 1) or as pixels (dimension > 1).
    // Defaults are invoked by -1; but one -1 for Width or Height resets both to -1; similarly, -1 for either of CentreX/Y resets both to -1.
    // The settings of this function do not last beyond the first call to any dialog box.
    { double[] settings = AccumulateValuesFromArgs(Args);
      double width = -1, height = -1, centreX = -1, centreY = -1; 
      if (settings.Length >= 2) { width = settings[0];  height = settings[1]; }
      if (settings.Length == 4) { centreX = settings[2];  centreY = settings[3]; }
      JD.PlaceBox(width, height, centreX, centreY);
      break;
    }

    case 431: // COUNT(array InArray, array / scalar Target [, scalar startPtr [, scalar endPtr ] ] ) -- returns a SCALAR,
    // the no. instances of Target found (0, if none). Stuff at pointers is included in the counting process. Out-of-range pointers adjusted.
    // The structure of 'InArray' is ignored; only its data strip is accessed. Hence 'startPtr' and 'endPtr' are taken as whole-array iterators.
    // If speed is a big deal and the target is just one value, make it a scalar rather than a one-entry array.
    { int inslot = Args[0].I, targetslot = Args[1].I;
      if (inslot == -1) return Oops(Which, "1st. arg. must be an array");      
      double[] indata = R.Store[inslot].Data;
      int startPtr = 0, endPtr = indata.Length;
      if (NoArgs > 2) startPtr = Convert.ToInt32(Args[2].X); // "_FindAll(.) below adjusts improper value.
      if (NoArgs > 3) endPtr = Convert.ToInt32(Args[3].X); // "_FindAll(.) below adjusts improper value.
      int[] targetFinds;
      if (targetslot == -1) targetFinds = indata._FindAll(Args[1].X, startPtr, endPtr); // faster than the same method's overload below
      else targetFinds = indata._FindAll(R.Store[targetslot].Data, startPtr, endPtr);
      result.X = targetFinds.Length;
      break;
    }
    // RE THE NEXT THREE FUNCTIONS: These are for some list array "InArray" in which subarrays ("segments") are separated by a SINGLE-valued delimiter.
    //   Segments are numbered to base 0. Segments of size 0 are allowed, as occurs when InArray starts with the delimiter (Segment 0 then
    //   has zero length) or ends with the delimiter, or contains two or more contiguous delimiters.
    //   The number of segments is ALWAYS equal to the number of delimiters + 1. As an extreme example (representing the delimiter by "|"),
    //   InArray consisting of nothing but the delimiter still has two segments, both of zero size.

    case 432: // GETSEGMT(array InArray, array / scalar Delimiter, scalar WhichSegment [. scalar / array TrimValue [, scalar EmptyCode ] ] )
    //   -- See above notes first, re the concept of segments..
    // Returns the indicated segment, or [NaN] if the indicated segment is empty. CRASHES if WhichSegment is out of range.
    //  If Delimiter is an array, only its first element is used. The returned segment has the chars. rating of InArray.
    //  If a 4th arg. is present, that value (or its first element, if an array) will be trimmed off both ends of the entry (possibly returning [NaN] ).
    //  If a 5th. arg. is present, returns that scalar value (no test for arrayhood) instead of NaN, if an empty segment was found. 
    case 433: // SETSEGMT(NAMED array InArray, array / scalar Delimiter, scalar WhichSegment, scalar/array NewContent ) -- VOID.
    // Sets the indicated segment to the supplied value, unless that value is NaN (if scalar) or, if an array, starts with [0] = NaN;
    // in this case, the indicated segment will be an empty segment.
    // CRASHES if WhichSegment is out of range. If Delimiter is an array, only its first element is used.
    { bool isGetSegmt =  (Which == 432);
      double[] indata = null; // It is assigned in method 'LocateSegment' below.
      int leftDelimPtr=0, rightDelimPtr=0, WhichEnd=0;
      bool Is_Chars=false;
      double delimiter=0;
      int locate_outcome = LocateSegment(Args, ref indata, ref delimiter, ref leftDelimPtr, ref rightDelimPtr, ref WhichEnd, ref Is_Chars);
      if (locate_outcome != 1)
      { if (locate_outcome == -2) return Oops(Which, "1st. arg. must be an array");
        if (locate_outcome == -3) return Oops(Which, "1st. arg. cannot be the empty array [NaN]");
        if (locate_outcome <= 0) return Oops(Which, "no such segment exists");
      }
      int segmentWidth = rightDelimPtr - leftDelimPtr - 1; // width of stuff between delimiters. Meaningless, if isInsert, but then is not accessed.
      double[] outdata;
      if (isGetSegmt) // GETSEGMT:
      { 
        double Emptiness;  // What to return if an empty segment.
        if (NoArgs > 4)
        { if (Args[4].I == -1) Emptiness = Args[4].X;
          else Emptiness = R.Store[Args[4].I].Data[0];
        }
        else Emptiness = double.NaN;
        if (segmentWidth == 0) outdata = new double[] {Emptiness}; 
        else outdata = indata._Copy(leftDelimPtr+1, segmentWidth);
        if (NoArgs > 3)
        { double padvalue;
          if (Args[3].I == -1) padvalue = Args[3].X;
          else padvalue = R.Store[Args[3].I].Data[0];
          int datalen = outdata.Length;
          int firstnonpad = -1, lastnonpad = -1;
          for (int i=0; i < outdata.Length; i++)
          { if (outdata[i] != padvalue)
            { if (firstnonpad == -1) firstnonpad = i;
              lastnonpad = i;
            }
          }
          if (firstnonpad == -1) outdata = new double[]{Emptiness}; // only the pad character was present
          else
          { if (firstnonpad > 0 || lastnonpad < datalen-1) // then there is trimming to be done:             
            { outdata = outdata._Copy(firstnonpad, lastnonpad - firstnonpad + 1); }
          }
        }
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        R.Store[result.I].IsChars = Is_Chars;
      }
      else // SETSEGMT:
      { int indataLen = indata.Length;
        double[] newdata = null;
        if (Args[3].I == -1) newdata = new double[] {Args[3].X};
        else newdata = R.Store[Args[3].I].Data;
        bool isEmpty = double.IsNaN(newdata[0]);
        int newdataLen = newdata.Length;  if (isEmpty) newdataLen = 0;        
        outdata = new double[indataLen + newdataLen - segmentWidth];
        // Prefix with earlier data, if any:
        if (leftDelimPtr >= 0) // i.e. if there is a non-virtual left pointer:
        { Array.Copy(indata, 0, outdata, 0, leftDelimPtr+1); }
        // Put in the new data, if any:
        if (newdataLen > 0) newdata.CopyTo(outdata, leftDelimPtr+1); // OK for leftDelimPtr to be -1.
        // Append remaining old data, if any:
        if (rightDelimPtr < indataLen)
        { Array.Copy(indata, rightDelimPtr, outdata, leftDelimPtr+1+newdataLen, indataLen - rightDelimPtr); }
        // Change the variable's pointer to take the new array:
        int[] dim_sz = new int[TVar.MaxNoDims];   dim_sz[0] = outdata.Length;        
        int newslot = V.GenerateTempStoreRoom(dim_sz);
        StoreItem  newit = R.Store[newslot];
        newit.Data = outdata;
        newit.IsChars = Is_Chars;
        int Fn = REFArgLocn[0].Y, At = REFArgLocn[0].X;
        V.TransferTempArray(newslot, Fn, At);
      }
      break;
    }
    case 434: // FINDSEGMT (InArray, scalar/array Delimiter, array/scalar SearchFor [, bool FullLengthMatchReqd [, scalar StartFrom
              //      [, scalar MaxNoFinds  ] ] ] ). -- see notes before case 432 ("getsegmt").
    // Recall: If InArray is "a||cc" and Delimiter is '|', then segment 0 is "a", segment 1 is empty, segment 2 is "cc". If InArray = "|",
    //   segment 0 = segment 1 = empty segment. The empty segment is represented as NaN. So to this function...
    //   Delimiter: Always a single value; so if arg. is an array, only the first element is used.
    //   SearchFor: obvious. Can be NaN, in which case the returned segment no. will be that of the first empty segment.
    //   FullLengthMatchReqd: If TRUE or absent, the whole segment must equal SearchFor, to register a positive find. 
    //     Otherwise the match need only be for the length of SearchFor. (E.g. in "a|bb|bbcde", SearchFor = "bb" and this arg. FALSE,
    //     both segments 1 and 2 would qualify as finds.) If SearchFor is NaN, then FullLengthMatchReqd is force to TRUE; input value is ignored.
    //   StartFrom: A segment no., NOT an array index. (It would be too complex to code in array indexes here, since empty segments are allowed.)
    //     If absent, taken as 0. Negative StartFrom reset to 0; oversized StartFrom would simply return -1 (no find).
    //     (If you only have an array index to start from, use fn. "findall" to find all the delimiter indexes, and hence the starts of segments.)
    //   MaxNoFinds: If absent, taken as 1. To indicate 'find all possible', set to 0 or a negative value, or to a value much higher than possible.
    // RETURNED: Array, always of size 3. If no find, [-1, -1, -1]. If a find, [segment no.,  ptr. to start of segment, ptr. to end of segment ].
    //  If StartFrom is present and set to anything except 1, this array will have length 3 x no. of finds, each triplet taking the pattern
    //     just given for successive finds. (Still, if no finds, [-1, -1, -1] is returned.)
    //  In the case of search for the empty segment, will be different: [segment no., ptr. to 1st. of two adjacent delimiters, ptr to 2nd. such].
    //     Bear in mind that if the find is segment 0 (so that InArray starts with a delimiter), the first pointer will be -1 (pointing to a
    //     virtual delimiter before the start). Similarly if InArray ends with a delimiter, a find of the segment beyond it would return with
    //     element [2] pointing to a virtual delimiter at location InArray.Length.
    { int inslot = Args[0].I, delimslot = Args[1].I, targetslot = Args[2].I;
      if (inslot == -1) return Oops(Which, "1st. arg. must be an array");      
      StoreItem inItem = R.Store[inslot];
      double[] indata = inItem.Data;
      int indataLen = indata.Length;
      double delimiter;
      if (delimslot == -1) delimiter = Args[1].X;
      else delimiter = R.Store[delimslot].Data[0];
      int[] allDelims = indata._FindAll(delimiter);
      int noDelims = allDelims.Length; // will be 0, if none.
      int noSegments = noDelims+1;
      double[] target;
      if (targetslot == -1) target = new double[] { Args[2].X };
      else target = R.Store[targetslot].Data;
      int targetLen = target.Length;
      if (targetLen == 1 && double.IsNaN(target[0])) targetLen = 0;
      bool fullLengthSearch = (NoArgs == 3 || Args[3].X != 0.0);
      if (targetLen == 0) fullLengthSearch = true; // otherwise any and every segment is a find for target length 0 and full length search not nec.
      int startSegment = 0;  if (NoArgs > 4) startSegment = Convert.ToInt32(Args[4].X);
      if (startSegment < 0) startSegment = 0;
      int findSegment = -1;
      int startPtr = -1, endPtr = -1;
      int maxFinds = 1;
      if (NoArgs > 5)
      { maxFinds = Convert.ToInt32(Args[5].X);
        if (maxFinds <= 0) maxFinds = indataLen+1; // i.e. a value guaranteed to exceed the number of possible finds.
      }
      var findings = new List<double>(); 
      if (startSegment <= noDelims) // the search cannot succeed if not.
      // Derive from allDelims an array which also refers to a virtual delimiter at index -1 and another at index indata.Length:
      { int[] expandedDelims = new int[noDelims+2];
        expandedDelims[0] = -1;  expandedDelims[noDelims+1] = indataLen;
        if (noDelims > 0) allDelims.CopyTo(expandedDelims, 1);
        // In this new array, segment N for all legal values always lies between expandedDelims[N] and [N+1]. (Illegal N were excluded above.)
        int segmentLen, noFinds = 0;
        for (int i = startSegment; i < noSegments; i++)
        { startPtr = expandedDelims[i]+1;   endPtr = expandedDelims[i+1]-1;  segmentLen = endPtr - startPtr+1;
          if (segmentLen < targetLen) continue; // no hope of match.
          if (segmentLen > targetLen && fullLengthSearch) continue; // no hope of match.
          findSegment = i; // tentative
          for (int j=0; j < targetLen; j++)
          { if (indata[startPtr+j] != target[j])  { findSegment = -1;  break; } }
          if (findSegment == i) // a definite find:
          { if (targetLen == 0)
            { findings.Add((double) findSegment);   findings.Add((double) endPtr); findings.Add((double) startPtr); } // delim. before and after.
            else // for nonempty target, startPtr precedes endPtr:
            { findings.Add((double) findSegment);   findings.Add((double) startPtr); findings.Add((double) endPtr); }
            noFinds++;
            if (noFinds >= maxFinds) break;
          }
        }
      }
      // Prepare results:
      if (findings.Count == 0) findings.AddRange(new double[] {-1.0, -1.0, -1.0});
      result.I = V.GenerateTempStoreRoom(findings.Count);
      R.Store[result.I].Data = findings.ToArray();
      break;
    }
    case 435: // FINDANY(InArray, scalar StartPtr, array TargetSet [, scalar / array Delimiter) -- Finds the first instance of any
    // of the targets in InArray, and returns two items of data: where that target was found in InArray, and which target it was.
    // Two versions: (1) Three args only. The search is for each individual element of TargetSet;
    //  (2) Four args: the fourth is a single-valued delimiter (if Delimiter is an array, only Delimiter[0] is used). Then TargetSet is
    //   taken as a set of subarray targets delimited by Delimiter. CRASHES if any subarray is empty; that is, the following are forbidden
    //   (using delimiter '|'): "|AA|BB", "AA|BB|", "AA||BB". Proper is: "AA|BB|CC".
    // RETURN: Always an array of size 2. If no find, [-1, -1]. If a find, version 1 --> [locn. of find, index in TargetSet],
    //   and version 2 --> [locn. of find, subarray no. in TargetSet]. (Subarrays numbered to base 0; e.g. "AA" above is subarray 0.)
    { int inslot = Args[0].I,  targetslot = Args[2].I;
      if (inslot == -1 || targetslot == -1) return Oops(Which, "1st. and 3rd. args. must be arrays");
      double[] indata = R.Store[inslot].Data;
      int indataLen = indata.Length;
      int startPtr = Convert.ToInt32(Args[1].X); // no check for arrayhood.
      if (startPtr < 0) return Oops(Which, "2nd. arg. must be a non-negative integer");
      double[] targetdata = R.Store[targetslot].Data;
      double Delim = 0.0;
      bool hasDelim = (NoArgs > 3);
      if (hasDelim)
      { if (Args[3].I == -1) Delim = Args[3].X;
        else Delim = R.Store[Args[3].I].Data[0];
      } 
      double[] outcome = new double[] { -1.0, -1.0 }; // The default: No find.
      int targetLen = targetdata.Length;
      // Case 1 -- 3 args, so individual elements of TargetSet are each sought:
      if (!hasDelim) 
      { int n, firstAt = indataLen,  whatFound = -1; 
        for (int i=0; i < targetLen; i++)
        { n = indata._Find(targetdata[i], startPtr);
          if (n != -1  &&  n < firstAt)
          { firstAt = n; whatFound = i; }
        }
        if (whatFound != -1) { outcome[0] = (double) firstAt;  outcome[1] = (double) whatFound; }
      }
      // Case 2 -- 4 args, so TargetSet is a delimited set of subarrays:
      else
      { if (targetdata[0] == Delim  ||  targetdata[targetLen-1] == Delim)
        { return Oops(Which, "In the 4-arg. form, the 3rd. arg. cannot begin or end with the delimiter (which is the 4th. arg.)"); }
        int n, p, q, firstAt = indataLen,  whatFound = -1; // firstAt has dummy value, much higher than no. of subarrays
        int[] delimery = targetdata._FindAll(Delim);
        int noTargets = delimery.Length + 1;
        for (int i=0; i < noTargets; i++)
        { p = 0;  if (i > 0) p = delimery[i-1]+1; // first char. of subarray, which follows the delim (or is at the start of indata).
          q = targetLen;  if (i < noTargets-1) q = delimery[i]; // delim. after this subarray (or beyond end of indata).
          if (q == p) return Oops(Which, "two delimiters are consecutive in the 3rd. arg."); 
          n = indata._Find( targetdata._Copy(p, q-p), startPtr );
          if (n != -1  &&  n < firstAt)
          { firstAt = n; whatFound = i; }
        }
        if (whatFound != -1) { outcome[0] = (double) firstAt;  outcome[1] = (double) whatFound; }
      }
      // Prepare the return:
      result.I = V.GenerateTempStoreRoom(2);
      R.Store[result.I].Data = outcome;
      break;
    }

    case 436: // DEFLUFF(NAMED variable Subject, scalar VirtZero) -- All |values| ≤ VirtZero will be replaced in situ by 0.0.
    // Note: a VOID function; 'Subject' is altered. (If scalar and a constant, no error is raised; but also nothing happens.)
    // If VirtZero is ≤ 0, or is an array, no change will happen to Subject, but no error will be raised.
    { int slot = Args[0].I;
      double virt_zero = Args[1].X;
      if (virt_zero > 0.0)
      { if (slot == -1)
        { double x = Args[0].X;
          if (x <= virt_zero && x >= -virt_zero)
          { V.SetVarValue(REFArgLocn[0].Y, REFArgLocn[0].X, 0.0); // Does not alter the value, if arg. is a constant
          }
        }
        else 
        { double[] dudu = R.Store[slot].Data;
          dudu._Defluff(virt_zero);
        }
      }
      break;
    }
  case 437: // RANDUM (int NoBytes) -- gets random BYTES directly from the linux kernel via pseudofile "/dev/urandom", which
  // should have the same address on all Linux systems. They are returned as one byte per list array element. HUGELY slower than
  // "rand(Arr, 256)" for arrays of less than 10000 length, and still about 3 times slower for that and larger lengths.
  // The only advantage, if any, is to escape dependence on the NET-computed random numbers that are determined by a seed.
  // NoBytes: Values rounding to < 1 are corrected to 1. No upper limit or check for NaN - crash MonoMaths, if you wish.
  {
    int len = Convert.ToInt32(Args[0].X);  if (len < 1) len = 1;
    string filename = "/dev/urandom";
    byte[] data = new byte[len];
    using (FileStream fs = File.OpenRead(filename)) // Not in a 'try' block because the file is guaranteed to be there and usable in Linux systems.
    { fs.Read (data, 0, len); }
    double[] outdata = new double[len];
    for (int i=0; i < len; i++) outdata[i] = data[i];
    result.I = V.GenerateTempStoreRoom(len);
    R.Store[result.I].Data = outdata;
    break;
  }


  case 438: // EQUAL (scalar / array Standard, scalar / array Test [, scalar VirtZero ] )
  // Returns TRUE if the arrays are equal to within ±VirtZero, either in toto (no final scalar), or over the indicated length. Otherwise FALSE. 
  // Allowed for first two args: scalar-scalar, array-scalar, array-array (of same length). 
  // VirtZero -- default is 0; a supplied value < 0 is reset to 0.
  // If you want more information about how the two arrays match, use function COMPARE instead. This one has minimal functionality for speed. 
  {
    int slotStandard = Args[0].I,  slotTest = Args[1].I;
    double virt_zero = (NoArgs > 2) ?  Args[2].X  :  0.0;
    if (virt_zero < 0.0) virt_zero = 0.0;    
    // Deal with scalar - scalar:
    if (slotStandard == -1)
    { if (slotTest != -1) return Oops(Which, "if 1st. arg. is scalar, 2nd. arg. must also be scalar");
      double diff = Args[0].X - Args[1].X;
      if (diff <= virt_zero && diff >= -virt_zero) result.X = 1.0;
      break; 
    }
    // First arg. is now an array...
    double[] dataStandard = R.Store[slotStandard].Data;
    int datalen = dataStandard.Length;
    // Array - scalar:
    if (slotTest == -1)
    { result.X = 1.0;
      double x = Args[1].X;
      for (int i=0; i < datalen; i++)
      { double diff = dataStandard[i] - x;
        if (diff > virt_zero || diff < -virt_zero)
        { result.X = 0.0;  break; }
      }
      break;
    }
    // We now have array - array:
    { double[] dataTest = R.Store[Args[1].I].Data;
      if (dataTest.Length != datalen) return Oops(Which, "1st. and 2nd. args, if arrays, must have the same length");
      result.X = 1.0;
      for (int i=0; i < datalen; i++)
      { double diff = dataTest[i] - dataStandard[i];
        if (diff > virt_zero || diff < -virt_zero)
        { result.X = 0.0;  break; }
      }
    }
    break;
  }
 
  case 439: // INI_DATA(array Feature) -- returns data for any and all features that contain Feature (case-sens.) somewhere in the field name.
  // If scalar (or empty), returns data for all features. Field names are exactly as in the INI file, to left of equal signs.
  // RETURN: A delimited list array, the delimiter being MAXINT32. There are twice as many subarrays as there are returned data items.
  // For each datum, the arrangement is: <field name><delimiter<field value>. The form of 'value' depends on the field type. It can
  // be a single numerical value (directly entered, not in string form); or an array of 3 values for a colour (R, G, B values); or
  // an array of unicodes. E.g. if 'Feature' is "Font", the return as at the moment of writing this would be as follows (using '|' for MAXINT32):
  //   "FontNameAss|DejaVu Sans Condensed|FontNameRes|DejaVu Sans Condensed|FontPointsAss|11|FontPointsRes|11", where all are unicode values
  // except the two numbers '11', which are single values, not strings.
  // RETURN IF NO FINDS: Array of length 1, a space: " ".
  {
    string target = StoreroomToString(Args[0].I);
    bool getTheLot = (target == "");
    List<INIRecord> dataset = new List<INIRecord>();
    INIRecord[] INRalias = MainWindow.ThisWindow.INR;
    for (int i=0; i < INRalias.Length; i++)
    { string ss = INRalias[i].Nm;
      if (!String.IsNullOrEmpty(ss))
      { if (getTheLot  ||  ss.IndexOf(target) != -1)
        { INIRecord foo = new INIRecord();  foo.CopyFrom(INRalias[i]);
          dataset.Add(foo);
        }
      }
    }
    List<double> output = new List<double>();
    if (dataset.Count == 0) output.Add(32.0);
    else
    { double Delim = (double) int.MaxValue;
      for (int i=0; i < dataset.Count; i++)
      { INIRecord inr = dataset[i];
        output.AddRange(  StringToStoreroom(-1, inr.Nm) );
        output.Add(Delim);
        if (inr.Tp == 'X') output.Add(inr.X);
        else if (inr.Tp == 'I') output.Add( (double) inr.I);
        else if (inr.Tp == 'L')
        { byte[] boodle = inr.Clr._ToByteArray();
          double[] doodle = boodle._ToDubArray();
          output.AddRange(doodle);
        }
        else // string
        { string ss = inr.S;
          if (String.IsNullOrEmpty(ss)) ss = " ";
          output.AddRange(StringToStoreroom(-1, ss) );
        }          
        if (i < dataset.Count-1) output.Add(Delim);    
      }
    }
    result.I = V.GenerateTempStoreRoom(output.Count);
    R.Store[result.I].Data = output.ToArray();
    break;
  } 
  case 440: // TRAIN(..) -- A 'train' is a list array made up of subarrays ('carriages'). The format of the train is:
  // [No. Carriages][Length of Carriage1][Length of Carriage2]..[Length of Last Carriage] [All the data, undelimited].
  //   The part preceding the first data value is called the 'header'.
  // A train may be NULL - just the array [0] - but no carriage may contain 0 data.
  // Here are the modes, with arguments. Note that none are VOID; if a train is to be altered (appended to, deleted from...),
  // the return will be an altered copy of the input train.
  // Puffer = train("new"); -- creates the array [ 0 ] - the NULL train, with 0 carriages.
  // Stats = train("header", Puffer); returns the header of the train: [noCarriages, Carriage1Length, ... , lastCarriageLength ].
  // Puffer = train("from", list array InArray, scalar/array Delim);  builds train from a delimited list, ignoring empty subarrays.
  //     Delim, if array: only the first value is used.
  // Puffer = train("from", matrix JaggedMx, scalar/array Padder);  builds train from a jagged matrix.
  // DelimitedArray = train("delimit", Puffer, scalar/array Delimiter);  returns data as a delimited array.
  // Puffer1 = train("append", Puffer, scalar/array Carriage); -- as with all other operations, checked for inconsistencies and crashes if so.
  // Puffer1 = train("insert", Puffer, scalar WhichCarriage, scalar/array NewCarriage); // WhichCarriage must exist; this mode can't be used to append.
  // Puffer1 = train("alter", Puffer, scalar WhichCarriage, scalar/array NewCarriage);
  // Puffer1 = train("delete", Puffer, scalar startCarriage, scalar noCarriages); // noCarriages reduced if ovesized; but start can't be neg, or beyond end.
  //  ... and 3 functions that find: 'find' for a whole-carrage match; 'holds' for subarray somewhere in carriage; 'starts' for subarray is front of c.
  // Index = train("find", Puffer, scalar/array SoughtCarriage, scalar firstCarriageToCheck, scalar maxNoFinds ); --> array always; [-1] if no match.
  // Index = train("holds", Puffer, scalar/array SoughtSubarray, scalar firstCarriageToCheck, scalar maxNoFinds ); --> array always; [-1] if no match.
  // Index = train("starts", Puffer, scalar/array SoughtSubarray, scalar firstCarriageToCheck, scalar maxNoFinds ); --> array always; [-1] if no match.
  //   For the above 3, silly args. don't crash; simply "no find" code is returned.
  // Puffer1 = train("copy", Puffer, scalar fromCarriage, scalar noCarriages); // arg. laws as for 'delete'. Returns a properly formatted train.
  // array = train("read", Puffer, scalar CarriageNo); // Returns the empty array if CarriageNo doesn't exist, or Puffer is the NULL train.
  // NB: There are no optional args. for any case; the exact no. of args. is specific to each mode.
  {
    int n, errorNo;
    bool CharsRating = false;
    double[] outdata = null;
    int modeSlot = Args[0].I; // true for ALL modes
    if (modeSlot == -1) return Oops(Which, "1st. arg. must be an array");
    string doWhat = "≡" + StoreroomToString(modeSlot) + '≡';
    //                0         1         2         3         4         5         6         7         8         9         10        11        12
    string modery = "≡new≡≡≡≡≡≡≡header≡≡≡≡from≡≡≡≡≡≡delimit≡≡≡append≡≡≡≡insert≡≡≡≡alter≡≡≡≡≡delete≡≡≡≡find≡≡≡≡≡≡holds≡≡≡≡≡starts≡≡≡≡copy≡≡≡≡≡≡read≡";
    int modeIndex = modery.IndexOf(doWhat);
    if (modeIndex % 10 != 0) return Oops(Which, "1st. arg. not a recognized operation");
    modeIndex /= 10; // Now set to the numbers indicated above the line defining 'modery'.
    //                            0  1  2  3  4  5  6  7  8  9  10 11 12
    int[] noArgsDue = new int[] { 1, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 4, 3 };
    if (NoArgs != noArgsDue[modeIndex]) return Oops(Which, "this mode requires exactly {0} arg(s).", noArgsDue[modeIndex]);
    // NEW:
    if (modeIndex == 0)
    { result.I = V.GenerateTempStoreRoom(1);
      R.Store[result.I].Data[0] = 0.0;
      break;
    }
  // All cases past here have at least 2 args., and the 2nd. arg. is always an array:
    if (Args[1].I == -1) return Oops(Which, "2nd. arg. must be an array");
    // HEADER:
    else if (modeIndex == 1)
    { double[] puffer = R.Store[Args[1].I].Data;
      outdata = TrainHeader(puffer, out errorNo);
      if (errorNo != 0) return Oops(Which, "the arg. is not correctly formatted as a train");
    }
    else // ALL OTHER CASES have AT LEAST 3 ARGS.
    { // FROM:
      if (modeIndex == 2)
      { double padDelim = (AccumulateValuesFromArgs(Args, 2, 2))[0];
        outdata = BuildTrain(Args[1].I, padDelim);
        if (outdata == null) return Oops(Which, "could not convert 2nd. arg. into a train");
        CharsRating = R.Store[Args[1].I].IsChars;
      }
      else // ALL OTHER CASES have 2nd. arg. as PUFFER (must be a valid train).
      { int trainSlot = Args[1].I;
        if (trainSlot == -1) return Oops(Which, "2nd. arg. must be an array");
        StoreItem tritem = R.Store[trainSlot];
        double[] Zug = tritem.Data;
        double[] header = TrainHeader(Zug, out errorNo);
        if (errorNo != 0) return Oops(Which, "the 2nd. arg. is not formatted correctly as a train");
        int[] headerI = header._ToIntArray();
        int noCarriages = headerI[0];
        if (noCarriages == 0  &&  modeIndex >= 5  &&  modeIndex <= 7) return Oops(Which, "this mode cannot be used with a NULL train");
        CharsRating = tritem.IsChars;
        List<double> outthing = new List<double>(2 * Zug.Length);
        if (modeIndex == 3) // "DELIMIT"
        { double Delim = (AccumulateValuesFromArgs(Args, 2, 2))[0];
          int cntr = 0;
          for (int i = 0; i < noCarriages; i++) // rotate through all the carriages:
          { for (int j = 0; j < headerI[i+1]; j++)
            { outthing.Add(Zug[noCarriages + 1 + cntr]);  cntr++; }
            if (i != noCarriages-1) outthing.Add(Delim);
          }
          if (outthing.Count == 0) outthing.Add(Delim);
        }
        else if (modeIndex == 4) // "APPEND"
        { double[] lastCarriage = AccumulateValuesFromArgs(Args, 2, 2);
          outthing.AddRange(Zug);          
          outthing.AddRange(lastCarriage);          
          noCarriages++;
          outthing[0] = noCarriages;
          outthing.Insert(noCarriages, (double) lastCarriage.Length);
        }
        else if (modeIndex == 5  ||  modeIndex == 6) // "INSERT" or "ALTER"
        { int carriageNo = Convert.ToInt32(Args[2].X);
          if (carriageNo < 0  || carriageNo >= noCarriages) return Oops(Which, "3rd. arg. is out of range");
          double[] newCarriage = AccumulateValuesFromArgs(Args, 3, 3);
          outthing.AddRange(Zug);
          int insertAt = noCarriages+1; // 1st. non-header element in the train
          for (int i = 1; i <= carriageNo; i++) insertAt += headerI[i];
          if (modeIndex == 6) // ALTER:
          { outthing.RemoveRange(insertAt, headerI[carriageNo+1]); } // abolish the old carriage first
          outthing.InsertRange(insertAt, newCarriage); // BOTH insert and alter
          if (modeIndex == 5) // INSERT:
          { outthing[0] = noCarriages + 1;
            outthing.Insert(carriageNo + 1, (double) newCarriage.Length);
          }
          else outthing[carriageNo+1] = newCarriage.Length; // ALTER
        }
        else if (modeIndex == 7) // "DELETE"
        { int startCarriage = Convert.ToInt32(Args[2].X); // no test for arrayhood
          if (startCarriage < 0  || startCarriage >= noCarriages) return Oops(Which, "3rd. arg. is out of range");
          int doomedCarriages = Convert.ToInt32(Args[3].X);
          if (doomedCarriages < 1) return Oops(Which, "for this mode, 4th. arg. cannot be 0 or negative");
          if (startCarriage + doomedCarriages > noCarriages) doomedCarriages = noCarriages - startCarriage; // oversized arg. trimmed down.
          outthing.AddRange(Zug);
          int startPtr = noCarriages+1; // 1st. non-header element in the train
          for (int i = 1; i <= startCarriage; i++) startPtr += headerI[i];
          int endPtr = startPtr;
          for (int i = 0; i < doomedCarriages; i++) endPtr += headerI[startCarriage + i+1];
          outthing.RemoveRange(startPtr, endPtr - startPtr); // remove data
          outthing.RemoveRange(startCarriage+1, doomedCarriages); // adjust header
          outthing[0] = noCarriages - doomedCarriages;
        }
        else if (modeIndex >= 8  &&  modeIndex <= 10) // 8 = "FIND", 9 = "HOLDS", 10 = "STARTS".
        { CharsRating = false; // as it was set in the containing block above to rating of the train, but this mode's output is statistics only.
          bool mustMatchWhole = (modeIndex == 8);
          bool mustStartWithSought = (modeIndex != 9);
          double[] Sought = AccumulateValuesFromArgs(Args, 2, 2);
          int soughtLen = Sought.Length;
          int startCarriage = Convert.ToInt32(Args[3].X),  maxFinds = Convert.ToInt32(Args[4].X);
          if (startCarriage >= 0  &&  startCarriage < noCarriages  &&  maxFinds > 0) // i.e. if args. are sensible
          { int startptr, endptr = noCarriages, thisLen;
            for (int i = 0; i < noCarriages; i++)
            { thisLen = headerI[i+1];
              startptr = endptr + 1;
              endptr = startptr + thisLen - 1;
              if (i < startCarriage) continue;
              if ( (mustMatchWhole && thisLen != soughtLen)  ||  soughtLen > thisLen) continue; // save some time             
              n = Zug._Find(Sought, startptr, endptr);
              if (n != -1)
              { if (!mustStartWithSought || n == startptr) // A find:
                { outthing.Add(i);  if (outthing.Count == maxFinds) break;  }
              }
            }
          }
          if (outthing.Count == 0) outthing.Add( -1.0 );
        }

        else if (modeIndex == 11  ||  modeIndex == 12 ) // "COPY" and "READ"
        { bool isRead = (modeIndex == 12);
          int startCarriage = Convert.ToInt32(Args[2].X), noToCopy = 1;
          if (!isRead) noToCopy = Convert.ToInt32(Args[3].X);
          if (startCarriage >= 0  &&  startCarriage < noCarriages  &&  noToCopy > 0) // i.e. if args. are sensible
          { int startptr, endptr = noCarriages, thisLen;
            int overallStartPtr = -1;
            noToCopy = Math.Min(noToCopy, noCarriages - startCarriage);            
            double[] newheader = new double[noToCopy+1]; // header for the new train
            if (!isRead) newheader[0] = (double) noToCopy;
            for (int i = 0; i < startCarriage + noToCopy; i++)
            { thisLen = headerI[i+1]; // we have to start at carriage 0, even if not copying from there, to get the header right.
              startptr = endptr + 1;
              endptr = startptr + thisLen - 1;
              if (i < startCarriage) continue;
              if (i == startCarriage) overallStartPtr = startptr;
              if (!isRead) newheader[i - startCarriage + 1] = (double) thisLen;
            }
            if (overallStartPtr > -1)
            { double[] stuffToCopy = Zug._Copy(overallStartPtr, endptr - overallStartPtr + 1);
              if (!isRead)  outthing.AddRange(newheader); // no header needed for "READ"
              outthing.AddRange(stuffToCopy);
            }
          }
          else if (isRead) outthing.Add(double.NaN); // silly args.: if 'copy', leave empty; it will become the NULL train below.
        }
        // End point for all modes for which 2nd. arg. is the input train
        if (outthing.Count > 0) outdata = outthing.ToArray();
        else outdata = new double[] { 0.0};
      } // End of processing modes with 2nd. arg. = input train
    } // End of processing modes with 1st. arg. = the mode
    result.I = V.GenerateTempStoreRoom(outdata.Length);
    R.Store[result.I].Data = outdata;
    R.Store[result.I].IsChars = CharsRating;
    break;
  }
  case 441: // REMOVERUNS(array InArray [, scalar / array OfTheseValues) -- A 'run' of element X is a succession of two or more
  // XX... in sequence. Such a run will be reduced to a single X. If no 2nd. argument, runs of ANY value will be reduced: "AABBBBCDD"
  // would reduce to "ABCD". If 2nd. argument, only its value(s) will be reduced. E.g. removeruns("AABBBCCDD", "AD") --> "ABBBCCD". 
  // RETURNED: a processed copy of InArray. STRUCTURE of InArray is ignored; but the output is always a list array.
  //  The chars. rating of output is the same as that of input array.
  {
    int inslot = Args[0].I;  if (inslot == -1) return Oops(Which, "1st. arg. must be an array");
    StoreItem sindata = R.Store[inslot];
    double[] indata = sindata.Data;
    bool isChars = sindata.IsChars;
    int indataLen = indata.Length;
    double[] runsOfThese = null;
    bool runsOfAnything = (NoArgs == 1);
    if (!runsOfAnything) runsOfThese = AccumulateValuesFromArgs(Args, 1);
    // Scan for runs:
    double lastval = indata[0], thisval = 0.0;
    List<double> outlist = new List<double>(indataLen);   outlist.Add(lastval);
    int runsize = 0;
    bool isValidRunChar = true;
    for (int i = 1; i < indataLen; i++)
    { thisval = indata[i];
      if (thisval != lastval) 
      { outlist.Add(thisval);  
        runsize = 0;
      }
      else // this value duplicates the last:
      { if (runsize == 0) isValidRunChar = (runsOfAnything || runsOfThese._Find(thisval) >= 0);
        runsize++;
        if (!isValidRunChar) outlist.Add(thisval);
      }
      lastval = thisval;
    }            
    result.I = V.GenerateTempStoreRoom(outlist.Count);
    R.Store[result.I].Data = outlist.ToArray();
    R.Store[result.I].IsChars = isChars;
    break;
  }
    case 442: // SEEKNO(array Subject [, scalar FromPtr [, scalar ToPtr [, bool IntegerOnly [, bool AllowNegSign ]..]
    // Given a chars. array, seek the first string version of a valid number between pointers inclusive, and return details.
    // FromPtr, ToPtr: These corrected to start / end char.: neg. FromPtr, oversized ToPtr. In addition, ToPtr of -1 --> end char.
    // IntegerOnly: what it says. E.g. "yak 12.3" would return only 12, regarding the '.' as nonnumerical. Default: false.
    // AllowedNegSign: what it says. But if the neg. sign is not in range (i.e. is at FromPtr-1) it will not be detected. Default: false.
    // RETURNED: Array, length 3:
    //   [0], [1] -- point to first and last elements in Subject of the identified value, OR -1 if none found. In the case where IntegerOnly
    //      is absent or FALSE, -1 indicates a no-find, -2 an error in converting the find to a value (but the pointers are still returned).
    //   [2] -- the value found, or NaN (which can never be validly obtained)
    //  NB: If IntegerOnly is TRUE but the integer has a value outside of the Int32 range, a no-find will be returned; not so if this is FALSE.
    {// Deal with all arguments:
      string instr = StoreroomToString(Args[0].I, false, false, true); // Non-unicodes safely converted to unicodes for nonnumerical chars.
      if (instr == "") return Oops(Which, "1st. arg. must be an array of chars.");
      int inStrLen = instr.Length;
      int fromPtr = 0, toPtr = inStrLen, n;
      if (NoArgs > 1) fromPtr = Convert.ToInt32(Args[1].X);   if (fromPtr < 0) fromPtr = 0;
      if (NoArgs > 2) { n = Convert.ToInt32(Args[2].X);   if (n >= 0 && n < inStrLen) fromPtr = n; }
      bool integerOnly = (NoArgs > 3 && Args[3].X != 0.0);
      bool allowNegSign = (NoArgs > 4 && Args[4].X != 0.0);
      // Get seeking:
      double[] outdata = new double[4];
      if (integerOnly)
      { int[] intie = JS.FindInteger(instr, !allowNegSign, fromPtr, toPtr);
        if (intie[0] == 0) outdata = new double[] { -1.0, -1.0, double.NaN };
        else outdata = new double[] { (double) intie[2],  (double) intie[3],  (double) intie[1] };
      }
      else // not restricted to integers:
      { outdata = new double[3];
        Octet ox = JS.FindNumber(instr, fromPtr, toPtr, !allowNegSign);
        if (ox.BX) // a possible number identified
        { outdata[0] = (double) ox.IX;  outdata[1] = (double) ox.IY;
          outdata[2] = ox.BY ? ox.XX : double.NaN;  // then is a valid number
        }
        else { outdata[0] = -1.0;  outdata[1] = -1.0;  outdata[2] = double.NaN; }
      }
      result.I = V.GenerateTempStoreRoom(3);
      R.Store[result.I].Data = outdata;
      break;
    }
    case 443:// ENROL (char. array DelimitedVarNames, array Delimiter,  scalar FirstValue [, scalar Addend] ) -- Breaks the delimited string
    // into a set of putative variable names (spaces removed). if a name is found to be already assigned as a scalar, it is given a value;
    // if it exists as an unassigned variable it is made a scalar and given a value. If it has not been detected at this function level but is
    // a valid identifier, it is created as a scalar variable and assigned a value. All other names crash. Values are assigned in order of
    // occurrence in DelimitedVarNames, starting from FirstValue, and incrementing by 1 with each successor (if no 4th. arg.) or by the
    // value of Addend.  RETURNS the value that would be due for the next variable, if there were one.
    { string varnamery = StoreroomToString(Args[0].I, true, true, true);
      if (varnamery == "") return Oops(Which, "1st. arg. must be an array");
      string delimery = StoreroomToString(Args[1].I, true, true, true);
      if (delimery == "") return Oops(Which, "2nd.. arg. must be an array");
      double FirstValue = Args[2].X; // Too bad if it is an array.
      double Addend = (NoArgs > 3) ? Args[3].X : 1.0; // Too bad if it is an array.
      char delim = delimery[0];
      string[] VarNames = varnamery.Split(new char[] { delim }, StringSplitOptions.None); // empty substrings allowed here, picked up later.
      int Fn = R.CurrentlyRunningUserFnNo,  At;
      int no_names = VarNames.Length;
      double x = FirstValue;
      for (int i=0; i < no_names; i++)
      { string this_name = VarNames[i]._Purge(' ');
        if (this_name == "") return Oops(Which, "one or more substrings are empty");
        if (this_name._Extent(0, 2) == "__") return Oops(Which, "identifiers may not start with two underscores together");
        At = V.FindVar(Fn, this_name);
        if (At >= 0)
        { int usage = V.GetVarUse(Fn, At);
          if (usage != 0 && usage != 3)
          { return Oops(Which, "'" + this_name +"' is already defined as an array, constant or system variable'");
          }                             
          V.SetVarValue(Fn, At, x);
          x += Addend;
        }
        else // no such variable name registered
        { char what_it_is;
          P.NameLocn(this_name, Fn, true, out what_it_is); // only searches the given function's variable list.
          if (what_it_is == ' ') // valid name, but unused elsewhere at the same function level.
          { // Register a new scalar, and give it the due value. (The point: Although it is never referenced in the main program,
            //  it may well be referenced in a user function via "import scalar" or "lookup".)
            At = V.AddUnassignedVariable(Fn, this_name);
            V.SetVarValue(Fn, At, x);
            x += Addend;
          }
          else
          { if (what_it_is == '#') return Oops(Which, "'" + this_name +"' is not valid as an identifier");
            else return Oops(Which, "'" + this_name +"' is  a reserved name (e.g. keyword or function name)");
          }
        }
      }
      result.X = x;      
      break;
    }
    case 444: // HOTKEYSOFF: scalar GraphID. If graph not identified, nothing happens. Otherwise ALL of the menu hot keys of the graph
    // are irrevocably turned off. However the prompts for them are still visible in the menu (I haven't yet figured out how to turn them off).
    { 
      int graphID = (int) Args[0].X;
      Graph griffin;
      Trio threeoh = Board.GetBoardOfGraph(graphID, out griffin);
      if (threeoh.X == 0) break; // simply nothing happens, if board not found for this graph ID.
      Board.DisconnectAllAccelKeys(threeoh.X);
      break;
    }
    case 445: // __FINDLINENO(scalar LineNo_ToBase_1 [, bool GoThere] ) -- Always returns contents of given line (if it exists) or [NaN] (if not).
    // If GoThere is absent or FALSE, the current focus is not affected; if TRUE, the return is the same but if the line exists, focus goes to it.
    {
      double[] outdata = null;
      Gtk.TextBuffer BuffAss = MainWindow.ThisWindow.BuffAss;
      string AllText = BuffAss.Text;
      int totalLines = AllText._CountChar('\n');
      int lineNo = Convert.ToInt32(Args[0].X); // REMEMBER that it is to base 1.
      if (lineNo > 0  && lineNo <= totalLines) // valid line number:
      { int startPtr = -1, endPtr; // we will retrieve text BETWEEN these points, not at them.
        if (lineNo > 1) startPtr = AllText._IndexOfNth('\n', lineNo-1); // the last line's LF.
        endPtr = AllText._IndexOf('\n', startPtr+1);  if (endPtr == -1) endPtr = AllText.Length; // the LF at the end of this line.        
        if (endPtr == startPtr+1) outdata = new double[] {32.0}; // If the line is empty, it is returned as a single space.
        else outdata = StringToStoreroom(-1, AllText._Between(startPtr, endPtr));
      }
      if (outdata != null  &&  NoArgs > 1  &&  Args[1].X != 0.0) // put the cursor there:
      { MainWindow.ThisWindow.PlaceCursorAtAssWindowLine(lineNo-1, 0, true);
      }
      bool noData = (outdata == null);
      if (noData) outdata = new double[] {double.NaN};
      // Return the data:
      result.I = V.GenerateTempStoreRoom(outdata.Length);      
      R.Store[result.I].Data = outdata;
      if (!noData) R.Store[result.I].IsChars = true;
      break;
    }
    case 446: // FIRFILTER (array InArray,  array / scalar ImpulseRespose [, scalar Delay [, scalar LeftPadder [, scalar RightPadder] ] ] )
    // RETURNS a LIST array of the same length as InArray. At element no. i, and iterating with j over all the elements of ImpulseResponse,
    // the returned array's ith. element is the SUM of ImpulseResponse[j] * InArray[i - j - Delay].  At the ends, this system fails (iteration
    // over j not fully possible) unlessImpulseResponse trivially has length 1. For elements where this is so, padders are supplied; the
    // extent before the first valid returned data has elements = LeftPadder; those after the last valid data have elements = RightPadder.
    // Default values of optional arguments: Delay = 0,  LeftPadder = Double.MinValue,  RightPadder = Double.MaxValue.
    // SPECIAL CASE: If ImpulseResponse rounds to a positive nonzero integer, the IR will become an averaging filter; e.g. if its value is n,
    // it will be replaced internally by the impulse response [1/n, 1/n, .... 1/n] with length n.
    // ERRORS: Scalar InArray would crash; but if last three were arrays, they would be seen as scalars of value 0.0 (no test).
    {
      int inslot = Args[0].I;  if (inslot == -1) return Oops(Which, "1st. arg. must be an array");
      double[] indata = R.Store[inslot].Data;
      int indataLen = indata.Length;
      double[] IRdata;
      int IRslot = Args[1].I,  IRlen;
      if (IRslot == -1) // then a scalar has been supplied:
      { double x = Math.Round(Args[1].X);
        if (x < 1.0) return Oops(Which, "if the 2nd. arg. is scalar, it must round to an integer ≥ 1");
        if (x > (double) indataLen) { result.I = EmptyArray(); break; } // avoids chaos in next step; more refined checks on size come later.
        IRlen = (int) x;
        IRdata = new double[IRlen];
        double y = 1/x;
        for (int i=0; i < IRlen; i++)  IRdata[i] = y;
      }
      else // IR is an array:
      { IRdata = R.Store[IRslot].Data;
        IRlen = IRdata.Length;
      }
      int Delay = NoArgs > 2 ?  Convert.ToInt32(Args[2].X)  :  0 ;
      double LeftPadder = NoArgs > 3 ? Args[3].X  :  double.MinValue;      
      double RightPadder = NoArgs > 4 ? Args[4].X  :  double.MaxValue;      
      int firstValidX = Math.Max(0, IRlen + Delay - 1);
      int lastValidX =  Math.Min(indataLen-1, indataLen - 1 + Delay);
      if (firstValidX > lastValidX)  { result.I = EmptyArray(); break; }
      double[] outdata = new double[indataLen];
      for (int i = 0; i < firstValidX; i++) outdata[i] = LeftPadder;
      for (int i = lastValidX+1; i < indataLen; i++) outdata[i] = RightPadder;
      double sum;
      for (int i = firstValidX; i <= lastValidX; i++)
      { sum = 0.0;
        for (int j = 0; j < IRlen; j++)
        { sum += IRdata[j] * indata[i-Delay-j];  }
        outdata[i] = sum; 
      }
      result.I = V.GenerateTempStoreRoom(indataLen);
      R.Store[result.I].Data = outdata;      
      break;
    }
    case 447: // GRAPHTITLE(scalar GraphID,   array TheTitle) -- puts TheTitle into the window heading and panel button.
    // No error raised - simply does nothing - if GraphID is not valid.
    { int graphID = (int) Args[0].X;  if (graphID <= 0) break;
      string theTitle = StoreroomToString(Args[1].I);   if (String.IsNullOrEmpty(theTitle)) break; 
      Graph graf = null;
      Trio trill = Board.GetBoardOfGraph(graphID, out graf);   if (graf == null) break;
      Board.Texts(trill.X, theTitle, null, null);
      break;
    }
    case 448: // MONOTONICITY(array Subject,  bool ExpectAscending [, scalar StartIndex [, scalar VirtualZero [, bool ReturnAllExceptionPoints ] ] ] --
    // Checks for places in the array data strip where the trend of data to rise or to fall changes to the opposite trend (an 'exception point').
    // Also detects duplicates, though these are not registered as 'exception points'.
    // 'Subject' - array structure ignored. Should not contain NaN, as then results would be unpredictable.
    // 'ExpectAscending' - boolean; if false, expect values to be descending.
    // 'VirtualZero' - quantities this close or closer to zero than this will be regarded as zero. Can be 0; if negative, will be taken as 0.
    // RETURNED:
    //   (1) NO FINAL BOOL ARG, or final arg is the default FALSE:
    //      An array of size 4: [No of exception points, First exception point,  No duplicates, First contiguous duplicate].
    //      If no exception points, both elements [0] and [2] will be 0; if no contiguous duplicates, [1] and [3] will be zero.
    //      Example: Ascending order expected, Subject is [10, 30, 50, 20, 30, 10, 10]: There are 2 exception points, index 3 (value 20)
    //      and index 5 (value 1). There is also 1 duplicate, index 6 (the final 10). So the returned array would be [2, 3, 1, 6].
    //   (2) FINAL ARG PRESENT AND TRUE: An array of exception point indexes, or [-1] if none.
    { int inslot = Args[0].I;  if (inslot == -1) return Oops(Which, "1st. arg. must be an array");
      double[] indata = R.Store[inslot].Data;
      int insize = indata.Length;
      bool isAscending = (Args[1].X != 0.0);
      int startPtr =  (NoArgs > 2)  ?  Convert.ToInt32(Args[2].X)  :  0;
      if (startPtr < 0  ||  startPtr > (insize-2) ) return Oops(Which, "start index must lie between 0 and the penultimate element of the array");
      double virtZero =  (NoArgs > 3)  ?  Args[3].X  :  0.0;
      if (virtZero < 0.0) virtZero = 0.0;
      bool returnAll = (NoArgs > 4 && Args[4].X != 0.0);
      int noExceptionPts = 0,  firstExceptionPt = 0,   noDuplicates = 0,   firstDuplicate = 0;
      double diff, absdiff;
      List<double> allExceptions = null;
      if (returnAll) allExceptions = new List<double>();
      for (int i = startPtr+1; i < insize; i++)
      { diff = indata[i] - indata[i-1];
        absdiff = Math.Abs(diff);
        if (absdiff <= virtZero)
        { noDuplicates++;  if (noDuplicates == 1) firstDuplicate = i; } // Wasted if 'returnAll' is true, but not a big time-waster...
        else if (isAscending  ^ (diff > 0.0) )
        { noExceptionPts++;
          if (returnAll) allExceptions.Add( (double) i);
          else { if (noExceptionPts == 1) firstExceptionPt = i; }
        }
      }
      if (returnAll)
      { if (allExceptions.Count == 0) allExceptions.Add(-1.0);
        result.I = V.GenerateTempStoreRoom(allExceptions.Count);
        R.Store[result.I].Data = allExceptions.ToArray();
      }
      else
      { result.I = V.GenerateTempStoreRoom(4);
        R.Store[result.I].Data = new double[] { (double) noExceptionPts, (double) firstExceptionPt,  (double) noDuplicates,  (double) firstDuplicate };
      }
      break;
    }
    case 449: // DIVMOD(scalar / array Subject,  scalar Divisor) -- returns an array, containing the tuplet [ Subject DIV Divisor,  Subject MOD Divisor]
    // for each element of Subject; so if Subject is scalar, the returned array will have length 2.
    // Subject and Divisor are rounded to integers before division. Error raised if Divisor rounds to zero.
    // Re sign:  divmod(20, -3)  -->  Div -6,  Mod +2;  divmod(-20, 3)  -->  Div -6,  Mod -2;   divmod(-20, -3)  -->  Div +6,  Mod -2.
    // Note that the sign of the Div part is always what you would expect from simple division; the Mod sign is more tricky.
    {
      Int64 Divisor = Convert.ToInt64(Args[1].X);
      if (Divisor == 0) return Oops(Which, "2nd. arg. must not round to zero");
      // Deal with scalars separately, for speed:
      if (Args[0].I == -1)
      { Int64 Subject = Convert.ToInt64(Args[0].X), DivI,  ModI;
        DivI = Math.DivRem(Subject, Divisor, out ModI); 
        result.I = V.GenerateTempStoreRoom(2);
        R.Store[result.I].Data = new double[] { (double) DivI, (double) ModI };
      }
      else // Subject is an array:
      { double[] indata = R.Store[Args[0].I].Data;
        int len = indata.Length;
        var outdata = new double[2*len];
        Int64 ModX;
        for (int i=0; i < len; i++)    
        { outdata[2*i] = Math.DivRem(Convert.ToInt64(indata[i]), Divisor, out ModX); 
          outdata[2*i+1] = ModX;
        }
        result.I = V.GenerateTempStoreRoom(2, len);
        R.Store[result.I].Data = outdata;
      }
      break;
    }
    case 450: // EXPRESS(array WhichWindow, array SuppressWhat) -- VOID. Only the first letter of 'WhichWindow' is accessed; case-sensitive.
    // Currently the only values used: WhichWindow = 'R' (for results window); SuppressWhat therefore relates to that window.
    // SuppressWhat has format: [sign, name]. Thus, "+scalars" = display scalar results after the program run (the default);
    //   "-scalars" = suppress them instead.  *** Real soon now we will allow for the de-suppression of arrays also, with an optional
    //   third arg. which would give an upper limit to the length of data shown. Maybe you could use "+arrays" for simple display of
    //   array data, and "+arrays_neat" for structured display?
    // ERRORS: Simply no changes made.
    // Note that changes only apply for the duration of the program run (unless reset earlier).

    // . . . . . . . . . . . . .
    {
      string whichWindow = StoreroomToString(Args[0].I);
      string suppressWhat = StoreroomToString(Args[1].I);
      if (whichWindow == "" || suppressWhat == "") break; // No error; just do nothing.
      if (whichWindow == "R")
      {
        if (suppressWhat == "+scalars") MainWindow.DisplayScalarValuesAfterTheRun = true;
        else if (suppressWhat == "-scalars") MainWindow.DisplayScalarValuesAfterTheRun = false;
      }
      // Any values for args. which slipped through the above are simply ignored; nothing happens.
      break;
    }
    case 451: // SWAPROWS(matrix Mx, scalar RowNo1, scalar RowNo2)
    case 452: // SWAPCOLS(matrix Mx, scalar ColNo1, scalar ColNo2)
    // VOID; Mx is changed, unless an error occurs. (If an error does occur, and the program continues because the operation was
    //  within an "errortrap" section, Mx will not have been altered.)
    // ERRORS that crash: Mx not a matrix, row/col nos. out of range. If last two args. are the same, no error is raised, but no
    //  change occurs to Mx.

    { bool isSwapRows = (Which == 451);
      int mxslot = Args[0].I;
      if (mxslot == -1) return Oops(Which, "1st. arg. cannot be scalar");
      StoreItem sitem = R.Store[mxslot];
      int[] dimensions = sitem.DimSizes;
      if (dimensions.Length != 2) return Oops(Which, "1st. arg. must be a matrix");
      int noRows = dimensions[1];
      double[] mxdata = sitem.Data;
      if (isSwapRows)
      { int rowno1 = Convert.ToInt32(Args[1].X),   rowno2 = Convert.ToInt32(Args[2].X);
        int outcome = M2.SwapRows(ref mxdata, noRows, rowno1, rowno2);
        if (outcome != 0) return Oops(Which, "One or both row indexes is out of range");
      }
      else // is Swap Columns:
      { int colno1 = Convert.ToInt32(Args[1].X),   colno2 = Convert.ToInt32(Args[2].X);
        int outcome = M2.SwapColumns(ref mxdata, noRows, colno1, colno2); // Note - arg is noRows, not noCols.
        if (outcome != 0) return Oops(Which, "One or both column indexes is out of range");
      }
      break;
    }
    case 453: // ADDTOROWS( Matrix Mx,  array Row [, scalar / array ToWhichRows] )
    case 454: // ADDTOCOLS( Matrix Mx,  array Column [, scalar / array ToWhichColumns] )
    // Adds the contents of "Row" or "Column" to...
    //  * EVERY row / column of Mx, if there is no third argument; otherwise 
    //  * EACH INDICATED row / column within the third argument. (Out of range row / column index will simply be ignored, and do nothing.)
    // RETURNS a copy of Mx which has been so modified.    
    // Note that the second argument is simply treated as a data source - no check of structure - but its data length must be
    //  the same as the row / column length of Mx, or CRASHO. (Only a single row's worth or column's worth of data can be the added.)
    {
      int mxslot = Args[0].I,  arrslot = Args[1].I;
      if (mxslot == -1 || arrslot == -1) return Oops(Which, "1st. two args. cannot be scalar");
      StoreItem mxitem = R.Store[mxslot],  arritem = R.Store[arrslot];
      int[] mxdims = mxitem.DimSizes;
      if (mxdims.Length != 2) return Oops(Which, "1st. arg. must be a matrix");
      int norows = mxdims[1], nocols = mxdims[0];
      var arrdata = arritem.Data;
      int arrlen = arrdata.Length;
      bool isRows = (Which == 453);
      double[] outdata = mxitem.Data._Copy();
      double[] whichones = AccumulateValuesFromArgs(Args, 2,2); // If there is no 3rd. arg., the return will be an empty array.
      int whichonesLen = whichones.Length;
      int[] theseones = null;
      bool addtoall = (whichonesLen == 0); // no 3rd. arg.
      if (!addtoall) theseones = whichones._ToIntArray();
      // FN. ADDTOROWS:
      if (isRows)
      { if (arrlen != nocols) return Oops(Which, "2nd. arg. must have the same data length as the row length of the 1st. arg.");
        for (int i = 0; i < norows; i++)
        { if (addtoall || theseones._Find(i) >= 0)
          { int offset = i*nocols;
            for (int j = 0; j < nocols; j++)
            outdata[offset+j] += arrdata[j];
          }
        }
      }        
      // FN. ADDTOCOLS:
      else
      { if (arrlen != norows) return Oops(Which, "2nd. arg. must have the same data length as the column length of the 1st. arg.");
        for (int i = 0; i < nocols; i++)
        { if (addtoall || theseones._Find(i) >= 0)
          { for (int j = 0; j < norows; j++)
            outdata[j*nocols + i] += arrdata[j];
          }
        }
      }        
      // Return the result, in either case:
      result.I = V.GenerateTempStoreRoom(mxdims);
      R.Store[result.I].Data = outdata;
      break;
    }
    case 455: // GRAPHIMAGE(scalar GraphID,  array Action, ...) -- suite of operations for dealing with images placed on 2D graphs.
    // Action is one of: A(dd), R(emove), L(ist), I(nformation), M(ove). Only the first character is read; case-sensitive.
    // WARNING: Some actions return scalars, others return arrays.
    // Action arguments:
    //    "A(dd)" -- scalar GraphID, array Action, array NickName, array FileLocation, scalars Left, Top, Width, Height[, Layer]. VOID.
    //    "R(emove)" -- scalar GraphID, array Action, array NickName. Returns BOOL - only TRUE if the image was identified, and therefore removed.
    //        If NickName is the single character '#', then all images will be removed from the graph.
    //    "L(ist)" -- scalar GraphID, array Action. Returns a list array, made up of NickNames delimited by '|'. If no images, the return is " ".
    //    "I(nformation)" -- scalar GraphID, array Action, array NickName. Returns a chars. list array, being the string returned by
    //        the Imagery object's 'ToString()' method (see Board.cs). At the time of writing, this takes the form:
    //  "Name = Foo1; Location = /home/..../foodle.jpg; Image is valid; Width = 200 pixels; Height = 200 pixels; ScaledLeft = 0; ScaledTop = 1; Layer = 2"
    //     If the image is null, the middle part is replaced by "Image is NULL;", but all the other detail is still supplied.
    //    "M(ove)" -- VOID. First letter only is read; case-sensitive. DIFFERENT ARGS: array Action, scalar GraphID, array NickName, scalars Left, Top.
    // NickName - any string of characters ≥ 32 (but not the one-char string "#"). Internal spaces allowed; external ones will be stripped off,
    //    so a string of all-spaces is not allowed.
    // FileLocation -- if incomplete, will evoke a file dialog. Rules as for system fn. "load(.)".
    // Left, Top = SCALED coordinates for where the top left corner of the image is to go.
    // Width, Height = PIXEL coordinates. The image will be the largest that can fit into the rectangle so defined (this is Cairo's task).
    // Layer -- 0 = behind everything; 1 = over hairlines, under curves; 2 (the default) = over everything, curves included.
    // Note that all operations reference only one particular graph. There is no way, for example, to remove one image from several graphs at the
    //   same time; you have to do it graph by graph. On the other hand, NickName need only be unique for each graph; the same name can be
    //   used in different graphs, as there is no cross-referencing.
    // RETURNS an array, which is either a single space " " (if all went well) or an error message. Cairo-detected errors are not reported,
    //   as we have no access to the details; all you will notice is that there is no image on the graph, despite return of " " from this function.
    {
      int actionslot = Args[1].I;  if (actionslot == -1) return Oops(Which, "2nd. arg. must be an array");
      int GraphID = Convert.ToInt32(Args[0].X);
      Graph graf;
      Board.GetBoardOfGraph(GraphID, out graf);
      if (graf == null || graf.Is3D) // Sorry, we don't do 3D graphs.
      { result.I = V.GenerateTempStoreRoom(1);  R.Store[result.I].Data = new double[] { 32.0 }; break;
      }
      char Aktion = StoreroomToString(actionslot)[0]; // Sadly, "Action" is a keyname of System.
      string nickname = String.Empty, fileLocn = String.Empty;
      int existing_nickname_index = -1;
      if (Aktion != 'L') // All other actions require the NickName
      { nickname = StoreroomToString(Args[2].I, true, true);
        if (String.IsNullOrEmpty(nickname)) return Oops(Which, "3rd. arg. must be an array containing at least one printable character");
        for (int i=0; i < graf.Images.Count; i++)
        { if (nickname == graf.Images[i].Name )
          { existing_nickname_index = i; break; } // assumes only one instance of the name.
        }
      }
      string outstring = "";
      // Collect and test other arguments in accordance with Aktion:
      // ADD A NEW IMAGE
      if (Aktion == 'A')
      { if (NoArgs < 8)  return Oops(Which, "the A(dd) mode requires at least eight args.");
        if (existing_nickname_index != -1) return Oops(Which, "an image named '" + nickname + "' is already attached to this graph.");
        fileLocn = StoreroomToString(Args[3].I, true, true); // This time we will allow this to be empty or scalar, in which case
                      // a file dialog box will open.
        // Check out the file name:
        string[] fillet = fileLocn._ParseFileName(MainWindow.ThisWindow.CurrentPath);
        string fileLocn1 = fillet[0] + fillet[1];
        bool boobed = false;
        try
        { FileInfo fie = new FileInfo(fileLocn1);
          if (!fie.Exists) boobed = true;
          else result.X = Convert.ToDouble(fie.Length); // arg. is type long.
        }
        catch { boobed = true; }
        if (boobed) return Oops(Which, "unable to locate file with name '" + fileLocn1 + "'");
        double leftX = Args[4].X,  topY = Args[5].X;
        int width = Convert.ToInt32(Args[6].X),   height = Convert.ToInt32(Args[7].X);
        int layer = 2; // the default - image goes on top of everything
        if (NoArgs > 8)
        { layer = Convert.ToInt32(Args[8].X);  if (layer < 0  ||  layer > 2)  layer = 2;
        }
        Imagery imago = new Imagery(nickname, fileLocn1, width, height, leftX, topY, layer); // Too bad if this fails - no way this fn. will know about it.
        graf.Images.Add(imago);
        graf.ForceRedraw();
        break; // i.e. returns scalar 0, as this function is VOID.
      }
      // LIST IMAGES CURRENTLY STORED IN GRAPH
      else if (Aktion == 'L') 
      { foreach(Imagery imagogue in graf.Images)
        { if (outstring != "") outstring += '|';
          outstring += imagogue.Name;
        }
      }
      // LIST IMAGES CURRENTLY STORED IN GRAPH
      else if (Aktion == 'I') 
      { if (existing_nickname_index != -1)
        { outstring = graf.Images[existing_nickname_index].ToString();
        }
      }
      // REMOVE AN IMAGE
      else if (Aktion == 'R')
      { bool successful = true;
        if (existing_nickname_index == -1)
        { successful = false;
          if (nickname == "#")
          { if (graf.Images.Count > 0)
            { graf.Images.Clear();
              graf.ForceRedraw();
              successful = true;
            }
          }
        }
        else
        { graf.Images.RemoveAt(existing_nickname_index); 
          graf.ForceRedraw();
        }
        result.X = (successful)  ?  1.0  :  0.0;
        break;
      }
      // MOVE AN IMAGE
      else if (Aktion == 'M')
      { if (existing_nickname_index >= 0)
        { if (NoArgs != 5) return Oops(Which, "this action requires five args.");
          Imagery imp = graf.Images[existing_nickname_index]; // By C# laws, we can't change a property of graf.Images directly, hence local var.
          imp.ScaledLeft = Args[3].X;
          imp.ScaledTop = Args[4].X;
          graf.Images[existing_nickname_index] = imp;
          graf.ForceRedraw();
        }
        break; // i.e. the function is VOID with this Aktion.
      }

      // =========================
      // All scalar returns have been eliminated by here, so set up the returned array:
      if (outstring == "") outstring = " ";
      result.I = V.GenerateTempStoreRoom(outstring.Length);
      StringToStoreroom(result.I, outstring);
      break;
    }

    case 456: // REFLECT(array XX,  array YY, bool Horizontally, bool Vertically, scalar PivotX [, scalar PivotY[, bool Splice ] ]) -- reflect
    // the curve around a pivot, horizontally or vertically or both. If horizontally, PivotX is where the vertical mirror would sit (and PivotY
    // is ignored); if vertically, PivotY is where the horizontal mirror would sit (and PivotX is ignored). 'Splice' is only ever consulted if
    // PivotX exactly equals XX[0] or XX[last]; with the added condition that if both horizontal and vertical reflection are occurring, then
    // PivotY must be exactly YY[0] (if PivotX = XX[0]) or YY[last] (if PivotX = XX[last]). 
    // RETURNED: a matrix: row 0 is the X coordinates, row 1 the Y coordinates.
    //   If 'Splice' is TRUE AND valid for the given pivot, the row length is 2 * length(XX) - 1. If curves are spliced at XX[0], the reflected
    //   curve's coordinates precede the original curve's coordinates in the returned array; for splicing at XX[last], the reverse applies.
    //   If 'Splice' is FALSE, or is true but the above pivot conditions have not been fulfilled, the return has the same length as XX,
    //   consisting only of the reflected curve.
    { int slotX = Args[0].I,  slotY = Args[1].I;
      if (slotX == -1 || slotY == -1) return  Oops(Which, "1st. two args. must be arrays");
      double[] XX = R.Store[slotX].Data;
      double[] YY = R.Store[slotY].Data;
      int datalen = XX.Length;
      if (YY.Length != datalen || datalen < 2) return Oops(Which, "1st. two args. must be arrays of equal length (at least 2)");
      bool reflectHorizontally = (Args[2].X != 0.0);
      bool reflectVertically = (Args[3].X != 0.0);
      double pivotX = Args[4].X;
      double pivotY = (NoArgs > 5) ?  Args[5].X  :  0.0;
      bool Splice = (NoArgs > 6) ?  (Args[6].X != 0.0)  :  false;
      bool spliceLeft = false, spliceRight = false;
      // Check conditions for valid splicing
      if (Splice) // After this block, only spliceLeft and spliceRight will be accessed.
      { if (reflectHorizontally) // no splicing, if not
        { if (pivotX == XX[0] && (!reflectVertically || pivotY == XX[0])) spliceLeft = true;
          else if (pivotX == XX[datalen-1] && (!reflectVertically || pivotY == XX[datalen-1])) spliceRight = true;
        }
      }
      // Do the reflection: 
      double[] XXrefl = new double[datalen], YYrefl = new double[datalen];
      if (reflectHorizontally)
      { for (int i = 0; i < datalen; i++)
        { XXrefl[i] = 2 * pivotX - XX[datalen-i-1];
          if (reflectVertically)
          { YYrefl[i] = 2 * pivotY - YY[datalen-i-1]; }
          else YYrefl[i] = YY[datalen-i-1];
        }
      }
      else if (reflectVertically)
      { XXrefl = XX; // pointer assignment, but we won't do anything terrible with it
        for (int i = 0; i < datalen; i++)
        { YYrefl[i] = 2 * pivotY - YY[i];
        }
      }
      // Shove it all into the output array:
      double[] outdata; // X values followed by Y values, ready for building the output matrix
      if (spliceRight)
      { outdata = new double[4*datalen-2];
        // X coords go in first
        XX.CopyTo(outdata, 0);
        XXrefl.CopyTo(outdata, datalen-1); // This will overlap the above tranche by one element, at the first element (datalen-1).
        // Y coords:
        YY.CopyTo(outdata, 2*datalen-1);
        YYrefl.CopyTo(outdata, 3*datalen - 2); // This will also overlap the above, at the first element.
      }
      else if (spliceLeft)
      { outdata = new double[4*datalen-2];
        // X coords go in first
        XXrefl.CopyTo(outdata, 0);
        XX.CopyTo(outdata, datalen-1); // This will overlap the above tranche by one element, at the first element (datalen-1).
        // Y coords:
        YYrefl.CopyTo(outdata, 2*datalen-1);
        YY.CopyTo(outdata, 3*datalen - 2); // This will also overlap the above, at the first element.
      }
      else
      { outdata = new double[2 * datalen];
        XXrefl.CopyTo(outdata, 0);
        YYrefl.CopyTo(outdata, datalen);
      }
      result.I = V.GenerateTempStoreRoom(outdata.Length/2, 2);
      R.Store[result.I].Data = outdata;
      break;
    }




//###########vvvvvvvvv ORIGINAL VERSION
//    case 456: // REFLECT(array XX,  array YY, bool MirrorVertical, bool MirrorHorizontal [, bool Join] ) -- Given a curve defined by XX and YY,
//    // its reflection is calculated about its final point. The reflection may be as in a vertical mirror or as in a horizontal mirror, or
//    // as in both (e.g. if the original curve is an arc, then the output will be a sigmoid). If 'Join' present and TRUE.
//    // RETURNED: a matrix: row 0 is the X coordinates, row 1 the Y coordinates. If 'Join' absent or FALSE, the row length is the same as the
//    // lengths of XX and YY (which of course must be equal). If present and TRUE, the row length is (2 * length(XX) - 1), as the join point
//    // is not duplicated.
//    { int slotX = Args[0].I,  slotY = Args[1].I;
//      if (slotX == -1 || slotY == -1) return  Oops(Which, "1st. two args. must be arrays");
//      double[] XX = R.Store[slotX].Data;
//      double[] YY = R.Store[slotY].Data;
//      int datalen = XX.Length;
//      if (YY.Length != datalen || datalen < 2) return Oops(Which, "1st. two args. must be arrays of equal length (at least 2)");
//      bool mirrorVertical = (Args[2].X != 0.0);
//      bool mirrorHorizontal = (Args[3].X != 0.0);
//      bool joinCurves = (NoArgs > 4  &&  Args[4].X != 0.0);
//      // X coords for the reflection: 
//      double[] XXrefl = new double[datalen];
//      if (mirrorVertical)
//      { double pivotX = XX[datalen-1];
//        for (int i = 0; i < datalen; i++)
//        { XXrefl[i] = 2 * pivotX - XX[datalen-i-1]; }
//      }
//      else 
//      { for (int i = 0; i < datalen; i++)
//        { XXrefl[i] = XX[datalen - i - 1]; }
//      }
//      // Y coords for the reflection: 
//      double[] YYrefl = new double[datalen];
//      if (mirrorHorizontal)
//      { double pivotY = YY[datalen-1];
//        for (int i = 0; i < datalen; i++)
//        { YYrefl[i] = 2 * pivotY - YY[datalen-i-1]; }
//      }
//      else 
//      { for (int i = 0; i < datalen; i++)
//        { YYrefl[i] = YY[datalen - i - 1]; }
//      }
//      double[] AllOut; // X values followed by Y values, ready for building the output matrix
//      if (joinCurves)
//      { AllOut = new double[4*datalen-2];
//        // X coords go in first
//        XX.CopyTo(AllOut, 0);
//        XXrefl.CopyTo(AllOut, datalen-1); // This will overlap the above tranche by one element, at the first element (datalen-1).
//        // Y coords:
//        YY.CopyTo(AllOut, 2*datalen-1);
//        YYrefl.CopyTo(AllOut, 3*datalen - 2); // This will also overlap the above, at the first element.
//     }
//      else
//      { AllOut = new double[2 * datalen];
//        XXrefl.CopyTo(AllOut, 0);
//        YYrefl.CopyTo(AllOut, datalen);
//      }
//      result.I = V.GenerateTempStoreRoom(AllOut.Length/2, 2);
//      R.Store[result.I].Data = AllOut;
//      break;
//    }
//#########^^^^^^^^^^ original version of REflect
      // . . . . . . . . . . . . .
      default: break;                                //default  //last
    }

  // - - - - - - - - - - - - - - - - - - - -
    return result;

  } // END of SWITCHBOARD_C



} // END of PARTIAL CLASS F

} // END of NAMESPACE MONOMATHS
