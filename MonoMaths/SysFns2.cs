using System;
using System.Collections.Generic;
using System.IO;
using JLib;

namespace MonoMaths
{

internal partial class F
{

  public static Quad Switchboard_B(int Which, int NoArgs, PairIX[] Args, Quad result)
  {
    switch (Which)
   
    {

      case 201: // SELECT - Two versions; both select particular values from a source array.
      // (1) SELECT(SourceArray, array WhichIndices) -- WhichIndices is a list of indices within SourceArray that are to be extracted.
      //       Crash occurs if any index in WhichIndices is not a valid index of SourceArray. If SourceArray is a matrix, WhichIndices
      //       will be taken as a sequence: [Row1, Column1, Row2, Column2, ...]. If a 3D structure, [Matrix1, Row1, Column1, Matrix2, ...]. 
      //    The return is always a list array, whatever the structure of SourceArray. WhichIndices is allowed to duplicate indexes.
      // (2A) SELECT(SourceArray, SCALAR KeyValue, array KeyArray) -- For this the structure of SourceArray is irrelevant, but KeyArray MUST
      //    have the same absolute length as SourceArray. Then for every KeyArray[n] that exactly equals KeyValue, SourceArray[n] will be
      //    selected for the returned array. (E.g. SourceArray might be boolean, in which case KeyValue would be 1 or 0, depending on your needs.)
      // (2B) SELECT(SourceArray, SCALAR KeyLow, SCALAR KeyHigh, array KeyArray) -- the only difference is that th435e test for KeyArray[n] is
      //    that it lies between KeyLow and KeyHigh INCLUSIVE. Appropriate where system numeric handling errors can introduce tiny perturbations.
      // Versions 2 can return the empty array if there are no instances in WhichIndices of the key value / key range of values.
      {
        int noScalars = NoArgs-2;
        int sourceslot = Args[0].I, whichslot = Args[1 + noScalars].I;
        if (sourceslot == -1 || whichslot == -1) return Oops(Which, "the 1st. and last args. must be arrays");
        StoreItem sourceIt = R.Store[sourceslot],  whichIt = R.Store[whichslot];
        double[] sourcedata = sourceIt.Data,  whichdata = whichIt.Data;
        int sourcelen = sourcedata.Length,    whichlen = whichdata.Length;
        int outputlen;
        double[] output;
       // TYPE 1:
        if (NoArgs == 2)
        {
          int dimcnt = sourceIt.DimCnt;
          outputlen = whichlen / dimcnt;
          if (whichlen % dimcnt != 0) return Oops(Which, "the 2nd. arg. length is not a multiple of the no. of dims. of the first arg.");
          output = new double[outputlen];
          // For the sake of speed we separate off the far commoner case where SourceArray is a list array:
          int n, ndx = 0;
          if (dimcnt == 1)
          { for (int i = 0; i < whichlen; i++)
            { n = Convert.ToInt32(whichdata[i]);
              if (n < 0 || n >= sourcelen) return Oops(Which, "the {0}th. value in the last arg. is out of range for the 1st. arg.", i+1);
              output[ndx] = sourcedata[n];
              ndx++;
            }
          }
          else // structured:
          { int[] dimsizes = sourceIt.DimSz;
            int[] indexset = new int[dimcnt];
            for (int i=0; i < outputlen; i++)
            { for (int j=0; j < dimcnt; j++) 
              { indexset[dimcnt-j-1] = Convert.ToInt32(whichdata[i*dimcnt+j]); } // note reverse order, so that inner dimension goes low.
              n = LocateArrayValue(dimsizes, dimcnt, indexset);
              if (n < 0)
              { if (dimcnt == 1) return Oops(Which, "the {0}th. array index is out of range", i+1);
                else return Oops(Which, "the {0}th. index of the {1}th. set is out of range", dimcnt+n+1, i+1);
              }
              output[i] = sourcedata[n];
            }
          }
        }
       // TYPE 2:
        else
        { List<double> outstuff = new List<double>(whichlen);
          double loval = Args[1].X,  hival = loval;
          if (NoArgs == 4) hival = Args[2].X;
          double x;
          { for (int i = 0; i < whichlen; i++)
            { x = whichdata[i];
              if (x == loval || ( x > loval && x <= hival) ) // this format because most uses will probably only be of the 3 arg. form; saves time.
              { outstuff.Add(sourcedata[i]); }
            }
          }
          outputlen = outstuff.Count;
          if (outputlen == 0) { outputlen = 1;  output = new double[] {double.NaN}; }
          else
          { output = outstuff.ToArray();
            outputlen = output.Length;
          }
        }
        result.I = V.GenerateTempStoreRoom(outputlen);  
        R.Store[result.I].Data = output;   break;      
      }
     case 202: // SUBSTITUTE(array InsideThis, ...). Every occurrence of something will be replaced by something else. Four forms:
      // (1) substitute(array InsideThis, scalar Target, scalar Replacemt) -- every value exactly equal to Target is replaced.
      //       This is the one case in which Target may be NAN.
      // (2) substitute(array InsideThis, array Target, array Replacemt) -- the same. (Structure of the two arrays ignored.
      // (3) substitute(array InsideThis, array SetOfScalarTargets, scalar Replacemt) -- like calling (1) on each of values in SetOfScalarTargets,
      //       all of which get replaced by the same Replacemt.
      // (4) substitute(array InsideThis, scalar LoTarget, scalar HiTarget, scalar Replacer) -- all values in InsideThis which lie between OR
      //       at LoValue and HiValue are replaced by Replacer. (If LoTarget > HiTarget, they are internally swapped.)
      // In all cases, the output structure is the same as that of InsideThis, AS LONG AS the output data length is the same
      //   as the input length; otherwise it is a list array (with same chars. rating). This only occurs with (2), and then
      //   only where Target and Replacemt have different lengths.
      { int inslot = Args[0].I;  if (inslot == -1) return Oops(Which, "1st. arg. must be an array");
        int argDeployment = ArgTypesCode(Args, 1); // Returned int has '1' for scalar args. (from arg. 1 onwards) and '2' for array.
        StoreItem sitem = R.Store[inslot];
        double[] indata = sitem.Data;   int inLen = indata.Length;
        int[] indimsz = sitem.DimSz;
        bool aschars = sitem.IsChars;
        double[] outdata = null;
        if (argDeployment == 11) // substitute scalar for scalar. NAN IS ALLOWED:
        { outdata = indata._Copy();
          double testVal = Args[1].X,  substVal = Args[2].X;
          if (double.IsNaN(testVal))
          { for (int i=0; i < inLen; i++)
            { if (double.IsNaN(indata[i])) outdata[i] = substVal; }
          }
          else
          { for (int i=0; i < inLen; i++)
            { if (indata[i] == testVal) outdata[i] = substVal; }
          }
        }
        else if (argDeployment == 111)
        { outdata = indata._Copy();
          double lo = Args[1].X, hi = Args[2].X;   if (lo > hi) { double x = lo;  lo = hi;  hi = x; }
          double substVal = Args[3].X;
          for (int i=0; i < inLen; i++)
          { if (indata[i] >= lo && indata[i] <= hi) outdata[i] = substVal; }
        }
        else if (argDeployment == 21)
        { outdata = indata._Copy();
          double[] testVals = R.Store[Args[1].I].Data;  int testLen = testVals.Length;
          double substVal = Args[2].X;
          for (int i=0; i < inLen; i++)
          { double inval = indata[i];
            for (int j=0; j < testLen; j++)
            { if (inval == testVals[j]) { outdata[i] = substVal; break; } }
          }
        }
        else if (argDeployment == 22)
        { List<double> outlist = new List<double>();
          double[] target = R.Store[Args[1].I].Data;     int targetLen = target.Length;
          double[] replacemt = R.Store[Args[2].I].Data;
          int arrayPtr = 0;
          while (true)
          { int n = indata._Find(target, arrayPtr);
            if (n == -1)
            { if (arrayPtr == 0) { outdata = indata._Copy();  break; } // no point in using a List<.> if there are no occurrences of Target.
              for (int i = arrayPtr; i < indata.Length; i++) outlist.Add(indata[i]);
              outdata = outlist.ToArray();
              break;
            }
            // To get here, n is valid pointer:
            for (int i = arrayPtr; i < n; i++) outlist.Add(indata[i]);
            outlist.AddRange(replacemt);
            arrayPtr = n + targetLen;
          }
        }
        else return Oops(Which, "this deployment of scalar/array args. is not allowed");
        // Return the data:
        if (outdata.Length == indata.Length) result.I = V.GenerateTempStoreRoom(indimsz);
        else result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        R.Store[result.I].IsChars = aschars;
        break;
      }
      case 203: // REMOVEDUPS(array InArray [, char array RowOrCol) [, scalar/array OnlyTestThese]]). Nonvoid.
      // Three variants, depending on the number of arguments:
      // (1): 'removedups(InArray)': returns a list array with all duplications removed. InArr structure ignored.
      // The remaining two apply only for InArray a matrix. Only RowOrCol[0] is accessed; if none of 'R','r','C','c',
      //   an error is raised. The examples below use 'R'; the 'C' versions are analogous.
      // (2): 'removedups(InArray, 'R'): Any row which duplicates an earlier row is removed from the returned matrix.
      // (3): 'removedups(InArray, 'R', scalar/array OnlyTestThese ): As above, but only the column(s) specified
      //   in the last arg. are tested for duplication; rows are eliminated if just those column values are duplicated.
      //   An ERROR is raised if any values in OnlyTestThese are out of range; but duplication is permitted (just delays things).
      { int slot = Args[0].I;  if (slot == -1) return Oops(Which, "the 1st. arg. must be an array");
        StoreItem sitem = R.Store[slot];
        bool isChars = sitem.IsChars;
        List<double> dataList = null;
      // One-argument version:
        if (NoArgs == 1)
        { dataList = new List<double>(sitem.Data);
          if (sitem.TotSz > 1)
          { double thisX;   int ptr = 0, n;
            while (true)
            { thisX = dataList[ptr];
              n = 0; // dummy value
              while (true)
              { n = dataList.IndexOf(thisX, ptr+1);  if (n == -1) break;
                dataList.RemoveAt(n);
                if (dataList.Count <= ptr+1) break;
              }
              ptr++;  if (dataList.Count <= ptr+1) break;
            }
          }
          result.I = V.GenerateTempStoreRoom(dataList.Count);
          StoreItem sitem1 = R.Store[result.I];
          sitem1.Data = dataList.ToArray();
          sitem1.IsChars = isChars;
          break;
        }
      // Only the multi-arg. versions remain...
        int[] inDims = sitem.DimSizes;  if (inDims.Length != 2) return Oops(Which, "if there are 2 args., the 1st. must be a matrix");
        int RCSlot = Args[1].I;  if (RCSlot == -1) return Oops(Which, "2nd. arg. must be an array");
        double x = R.Store[RCSlot].Data[0];
        if (x > 96.0) x -= 32.0; // convert lower to upper case
        if (x != 82.0 && x != 67.0) return Oops(Which, "2nd. arg. improper"); // unicodes of 'R' and 'C'.
        bool isRows = (x == 82.0);
        int noRows = inDims[1],  noCols = inDims[0];
        if (isRows)
        { noRows = inDims[1];  noCols = inDims[0];
          dataList = new List<double>(sitem.Data);
        }
        else // transpose the data, as otherwise handling is too complex and probably slower.
        { double[] dudu = Transpose(ref sitem.Data, noRows);
          dataList = new List<double>(dudu);
          int m = noRows;  noRows = noCols;  noCols = m;
        }
        // Test for row duplication:
        bool[] killem = new bool[noRows]; // all start off false.
        int[] colsToCheck = null;  int colsToCheckLength = 0;
        if (NoArgs > 2)
        { double[] dada = AccumulateValuesFromArgs(Args, 2);
          colsToCheck = dada._ToIntArray();
          colsToCheckLength = colsToCheck.Length;
          // Check for out-of-range values; but not for duplications, which do not matter; they just prolong the loop time below.
          for (int i=0; i < colsToCheck.Length; i++)
          { if (colsToCheck[i] < 0 || colsToCheck[i] >= noCols)
            { string ss = (isRows ? "column" : "row");
              return Oops(Which, "a " + ss + " index in the last arg. is out of range");
            }
          }
        }
        for (int i = 0;  i < noRows-1;  i++) // The 'original' row.
        { if (killem[i]) continue; // no point in looking for duplicates of a duplicate row.
          int origOffset = i*noCols;
          for (int j = i+1; j < noRows; j++) // the 'test' row
          { if (killem[j]) continue;
            int testOffset = j*noCols;
            killem[j] = true; // guilty till proved otherwise
            if (NoArgs == 2) // then just iterate through all columns:
            { for (int k=0; k < noCols; k++)
              { if (dataList[origOffset+k] != dataList[testOffset+k])
                { killem[j] = false;  break; }
              }
            }
            else // only through specific columns:
            { int kk;
              for (int k=0; k < colsToCheckLength; k++)
              { kk = colsToCheck[k];
                if (dataList[origOffset+kk] != dataList[testOffset+kk])
                { killem[j] = false;  break; }
              }
            }
          }
        }
        // Remove doomed rows:
        for (int i = noRows-1; i > 0; i--)
        { if (killem[i])
          { dataList.RemoveRange(i*noCols, noCols);  }
        }
        int listSz = dataList.Count;
        int newNoRows = listSz / noCols;
        double[] outdata = dataList.ToArray();
        if (isRows) result.I = V.GenerateTempStoreRoom(noCols, newNoRows);
        else // we have to transpose the output:
        { outdata = Transpose(ref outdata, newNoRows);
          result.I = V.GenerateTempStoreRoom(newNoRows, noCols);
        }
        StoreItem sitem2 = R.Store[result.I];
        sitem2.Data = outdata;
        sitem2.IsChars = isChars;
        break;
      }
      case 204: case 205: case 63: // FIND / FINDS / FINDALL(ArrayToSearch, scalar StartPtr, (array or scalar) SearchFor [, (scalar) OtherLimit].
      // All three take the same arguments; they differ only in what they return. 
      // THE STRUCTURE OF ArrayToSearch IS IGNORED; finds are returned as absolute addresses within its data strip.
      // Four argument form: 3rd. must be scalar, and represents one limit (e.g. the lower limit) for values
      // answering to the search; OtherLimit is then the other (e.g. the higher limit). Both inclusive.
      // RETURNED: For 'find(.)', a scalar - position of the find, or -1 if no find. For 'finds(.)', an
      //  array of fixed size: [0] = no. finds (0 if none); [1] = locn. of first find (or -1); [2] =
      //  locn of last find (or -1). For 'findall(.)', a list array of length N, where N is the number of finds; 
      //  [i] is the locn of the ith. find. (Their values can be accessed using fn. 'select(.)'.) If no finds,
      //  returns an array of size 1, [0] = -1.
      { int argtypes = ArgTypesCode(Args);  
        char findtype = 'D'; if (Which == 205) findtype = 'S'; else if (Which == 63) findtype = 'A'; // (fin)D, (find)S, (find)A(ll).
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
      case 206: case 207: // SELECTROWS / SELECTCOLS (Matrix, array Where) -- structure of Where ignored, but must consist of 
      // valid row or col nos. Fn. returns a matrix of as many rows/cols as there are indices of rows/cols in Where.
      { int sourceslot = Args[0].I, whereslot = Args[1].I; 
        if (sourceslot == -1 || whereslot == -1) return Oops(Which, "args. must both be arrays");
        double[] sourcedata = R.Store[sourceslot].Data,  wheredata = R.Store[whereslot].Data;
        int wherelen = R.Store[whereslot].TotSz;
        int norows = R.Store[sourceslot].DimSz[1],  nocols = R.Store[sourceslot].DimSz[0];
        // Set up the return matrix:
        int[] dimsz = new int[TVar.MaxNoDims];
        if (Which == 206) { dimsz[0] = nocols;    dimsz[1] = wherelen; } // selectrows(.)
        else              { dimsz[0] = wherelen;  dimsz[1] = norows; } // selectcols(.)
        int ndx, newslot = V.GenerateTempStoreRoom(dimsz);   double[] output = R.Store[newslot].Data;
        for (int i=0; i < wherelen; i++)
        { ndx = Convert.ToInt32(wheredata[i]);  
          if (Which == 206) // gather rows:
          { if (ndx < 0 || ndx >= norows) return Oops(Which, "an index is out of range");
            Array.Copy(sourcedata, nocols* ndx, output, nocols*i, nocols); 
          }
          else // gather columns:
          { if (ndx < 0 || ndx >= nocols) return Oops(Which, "an index is out of range");
            for (int j=0; j < norows; j++)
            { output[j*wherelen + i] = sourcedata[j*nocols + ndx]; } 
          }
        } 
        R.Store[newslot].IsChars = R.Store[sourceslot].IsChars;
        result.I = newslot;
        break;
      }
      case 208: // PAUSE( Scalar PauseIndex: 0 to 9.  Can/Can't Pause is toggled by hot keys or by fn. 'pausable(.)'. 
      // The starting default is: Can Pause for index 0, Can't Pause for indices 1 to 9.
      // Once pgm. is paused, a click on 'GO' will get the pgm. moving again.
      { int PauseIndex = 0,  n = (int) Args[0].X;  if (n >=0 && n <= 9) PauseIndex = n; // no error msg. for wrong arg. - just correct it.
        if (CanPause[PauseIndex])
        { bool abortclicked = MainWindow.HoldingPattern(PauseIndex); // waits locked in this method till 'GO' or 'ABORT' clicked.
          if (abortclicked) return Oops(Which, "program interrupted by clicking the 'ABORT' button");
        } 
        break;
      }
      case 209: // PAUSABLE(Scalar PauseIndex: 0 to 9[, Scalar CanPause: 0 (FALSE) or nonzero (TRUE)] ).
      // If no second argument, returns the current state of CanPause[PauseIndex].
      // If a second argument, CanPause[PauseIndex] is set accordingly, but th fn. still returns the 
      // final state of CanPause[PauseIndex].
      { int PauseIndex = (int) Args[0].X;  
        if (PauseIndex < 0 || PauseIndex > 9) break; // no error msg. for wrong arg. - just don't do anything.
        if (NoArgs == 2) CanPause[PauseIndex] = (Args[1].X != 0.0); // class variable.
        if (CanPause[PauseIndex]) result.X = 1.0; // else it stays zero.
        break;  
      }
      case 210: // POKE(IntoArray, Array WhereTo, Scalar Value / Array Values ). Pokes values into IntoArray.  VOID fn.
      // The structure of IntoArray IS significant; if it has N dimensions AND the last arg. is an array (Values), then 
      //  the WhereTo array must be N times as long as the Values array, or else crasho. As with 'select(.)' (201), 
      // WhereTo values are taken in sets of N,  the first encountered being the OUTermost dimension (as with 'select(.)').
      // If the last arg. is scalar, it is the value poked into every location referenced in WhereTo.
      { int sourceslot = Args[0].I, whereslot = Args[1].I,  valueslot = Args[2].I; 
        if (sourceslot==-1 || whereslot==-1) return Oops(Which, "the first two args. must be arrays");
        int dimcnt = R.Store[sourceslot].DimCnt, wherelen = R.Store[whereslot].TotSz,  valueslen = -1;
        double[] values = null;   
        double scalarvalue = Args[2].X,  noreplacemts = wherelen / dimcnt;
        if (valueslot >= 0)
        { valueslen = R.Store[valueslot].TotSz;
          if (noreplacemts != valueslen) return Oops(Which, "the second and third array sizes are inconsistent");
          values = R.Store[valueslot].Data;
        }
        int[] dims = R.Store[sourceslot].DimSz;
        int[] coords = new int[dimcnt];
        double[] intoarr = R.Store[sourceslot].Data;
        double[] whereto = R.Store[whereslot].Data;
        for (int i = 0; i < noreplacemts; i++) // cycle through the values in the 3rd. argument array
        { int offset = i * dimcnt;
          for (int j=0; j < dimcnt; j++) coords[j] = Convert.ToInt32(whereto[offset + dimcnt-j-1]); // dimcnt is in reverse order to the order of indices in WhereTo.
          int n = LocateArrayValue(dims, dimcnt, coords);
          if (n < 0)
          { if (dimcnt == 1) return Oops(Which, "the {0}th. array index is out of range", i+1);
            else return Oops(Which, "the {0}th. index of the {1}th. set is out of range", dimcnt+n+1, i+1);
          }
            // The coordinates in WhereTo for IntoArray are satisfactory, so slide in the new value:
          if (valueslot >= 0) intoarr[n] = values[i]; else intoarr[n] = scalarvalue;
        }
        break;
      }
      case 211: case 212: // POKEROWS / POKECOLS (matrix Destination, array WhereTo, matrix Source). Void. Source is a matrix of rows/cols
      // to insert; each row represents a row/col to poke into Destination. WhereTo is a list of rows/cols in Destination. There must be 
      // one row of Source for each value in WhereTo. Note that new rows or cols overwrite preexisting ones; this is not an inserting fn.
      { int destinslot = Args[0].I, whereslot = Args[1].I, sourceslot = Args[2].I; 
        if (destinslot == -1 || whereslot == -1 || sourceslot == -1) return Oops(Which, "no args. may be scalar");
        if (R.Store[destinslot].DimCnt != 2 || R.Store[sourceslot].DimCnt != 2) return Oops(Which, "the first and third args. should be matrices");
        int destrows = R.Store[destinslot].DimSz[1], destcols = R.Store[destinslot].DimSz[0];
        int sourcerows = R.Store[sourceslot].DimSz[1], sourcecols = R.Store[sourceslot].DimSz[0];
        int wherelen = R.Store[whereslot].TotSz,  ndx=0;
        double[] destiny = R.Store[destinslot].Data, wheredata = R.Store[whereslot].Data, sourcedata = R.Store[sourceslot].Data;
        if (wherelen != sourcerows) return Oops(Which, "the second and third args. do not match");
        if ( (Which == 211 && destcols != sourcecols) || (Which == 212 && destrows != sourcecols) )
        { return Oops(Which, "the first and third args. do not match"); }
        for (int i=0; i < wherelen; i++)
        { ndx = Convert.ToInt32(wheredata[i]);  
          if (Which == 211) // gather rows:
          { if (ndx < 0 || ndx >= destrows) return Oops(Which, "the {0}th. index is out of range", i+1);
            Array.Copy(sourcedata, sourcecols*i, destiny, destcols*ndx, sourcecols); 
          }
          else // gather columns:
          { if (ndx < 0 || ndx >= destcols) return Oops(Which, "the {0}th. index is out of range", i+1);
            for (int j=0; j < destcols; j++)
            { destiny[j*destcols + ndx] = sourcedata[i*sourcecols + j]; } 
          }
        }
        break;
      }  
      case 213: // BOARDRESIZE(scalar graphID,  scalar NewWidth, scalar NewHeight. VOID. Values <= 1 are taken as factors of screen dimensions;
      // > 1 as pixel dimensions. If graph not identified or if either argument is zero or negative, nothing happens (but no crash occurs).
      // Note that GTK# will interpret NewWidth = NewHeight = 1.0 as a command to maximize the board.
      { int graphID = (int) Args[0].X;
        double newBdWidth = Args[1].X,  newBdHeight = Args[2].X;
        if (newBdWidth <= 0.0 || newBdHeight <= 0.0) break;
        if (newBdWidth <= 1.0 || newBdHeight <= 1.0)
        { Gdk.Screen scream = Gdk.Screen.Default;
          if (newBdWidth <= 1.0)  newBdWidth  *= (double) scream.Width;
          if (newBdHeight <= 1.0) newBdHeight *= (double) scream.Height;
        }
        Graph graf = null;
        Trio trill = Board.GetBoardOfGraph(graphID, out graf);
        if (graf == null) break;
        // We now have the board...
        Board.TryResize(trill.X, (int) newBdWidth, (int) newBdHeight);
        break;
      }
      case 214: // CULLBYKEY(array Victim, KeyArray ) -- the two arrays must be of the same length
      // (structure ignored). If an element in KeyArray is exactly zero, then the corresponding element in Victim is removed. 
      // If all elements of KeyArray are zero, then the return is the empty array (length 1, value NaN; call to 'empty(.)' returns TRUE).
      { int victimslot = Args[0].I,  keyslot = Args[1].I;
        if (victimslot == -1 || keyslot == -1) return Oops(Which, "the first two args. must be arrays");
        int insize = R.Store[victimslot].TotSz;
        if (insize != R.Store[keyslot].TotSz) return Oops(Which, "the two arrays must have the same size");
        List<double> data = new List<double>();
        double[] vicdata = R.Store[victimslot].Data;  double[] keydata = R.Store[keyslot].Data;
        for (int i=0; i < insize; i++)
        { if (keydata[i] != 0.0) data.Add(vicdata[i]); }
        if (data.Count == 0) result.I = EmptyArray();
        else
        { result.I = V.GenerateTempStoreRoom(data.Count);
          data.CopyTo(R.Store[result.I].Data);
        }
        break;
      }  
      case 215: // EMPTY(array / scalar Value): returns 1 if (a) Value is an array, and contains only instances of NaN;
      // or (b) Value is scalar, with value NaN.
      { int slot = Args[0].I;
        if (slot == -1)
        { if (double.IsNaN(Args[0].X)) result.X = 1.0; } // otherwise result.X stays as 0.
        else
        { double[] data = R.Store[slot].Data;
          result.X = 1.0;
          for (int i = 0; i < data.Length; i++)
          { if (!double.IsNaN(data[i])) { result.X = 0.0;  break; } }
        }
        break;
      }
      case 216: // APPENDROWS(Mx,   scalar/list array/matrix NewData [, scalar NoTimesToAdd] )
      case 217: // INSERTROWS (Mx,  scalar AtRowNo, array / scalar NewData [, scalar NoTimesToAdd] )
      case 218: // APPENDCOLS(Mx,   scalar/list array/matrix NewData [, scalar NoTimesToAdd] )
      case 219: // INSERTCOLS (Mx,  scalar AtColNo, array / scalar NewData [, scalar NoTimesToAdd] )
      // RETURNS the matrix with appended or inserted rows / columns.
      // 'AtRowNo/AtColNo' - for inserting - must lie between 0 and the index of the last row / column, inclusive.
      // 'NewData' - if scalar, is replaced by a list array chaining this value to the length of a single row / col of Mx.
      //   If a list array or matrix, it will be taken as a sequence of rows / columns to be added; if a matrix, 
      //   it must have the same row length / column length as does Mx.
      // 'NoTimesToAdd' (default is 1) - if 0 or neg., fn. simply returns original matrix. Otherwise adds the whole of NewData
      //   repetitively, this many times.
      {
        int mxslot = Args[0].I;         if (mxslot == -1) return Oops(Which, "1st. arg. must be a matrix");
        StoreItem mxitem = R.Store[mxslot];   if (mxitem.DimCnt != 2) return Oops(Which, "1st. arg. is not a matrix");
        double[] mxdata = mxitem.Data;
        int orignorows = mxitem.DimSz[1],   orignocols = mxitem.DimSz[0], mxdatalen = mxdata.Length;
        bool isRows = (Which <= 217),  isInsert = (Which == 217 || Which == 219);
        int fixedstriplength = isRows ? orignocols : orignorows;
        int orignonfixedlength = mxdatalen / fixedstriplength; 
        int argOffset = (isInsert) ? 1 : 0; // Used for args. NewData and NoTimesToAdd
        int insertAt =  (isInsert) ? (int) Args[1].X : orignonfixedlength;
        if (isInsert)
        { if (insertAt < 0 || insertAt >= orignonfixedlength) return Oops(Which, "2nd. arg. is not a valid index"); }
        int newdataslot = Args[1+argOffset].I;
        double[] newdata = null;
        if (newdataslot == -1) // SCALAR arg. NewData:
        { int n = isRows ? orignocols : orignorows;
           double x = Args[1+argOffset].X;
          newdata = new double[n];
          for (int i = 0; i < n; i++)  newdata[i] = x;
        }
        else // ARRAY arg. NewData:
        { StoreItem newitem = R.Store[newdataslot];
          if (isRows &&  newitem.DimCnt > 1 && newitem.DimSz[0] != orignocols)
          { return Oops(Which, "{0}th. arg. is a matrix but does not have the same row length as the 1st. arg.", 2+argOffset); }
          else if (!isRows &&  newitem.DimCnt > 1 && newitem.DimSz[1] != orignorows)
          { return Oops(Which, "{0}th. arg. is a matrix but does not have the same no. of rows as the 1st. arg.", 2+argOffset); }
          newdata = newitem.Data;
        }
        int newdatalen = newdata.Length;
        if (isRows && newdatalen % orignocols != 0) return Oops(Which, "length of {0}th. arg. is not a multiple of the row length of the input matrix", 2+argOffset);
        else if (!isRows && newdatalen % orignorows != 0) return Oops(Which, "length of {0}th. arg. is not a multiple of the column length of the input matrix", 2+argOffset);
        int timesToAdd = 1;
        if (NoArgs > 2 + argOffset) timesToAdd = (int) Args[2+argOffset].X;  
        int outLength = mxdatalen + newdatalen * timesToAdd;
        double[] outdata = null;
        if (timesToAdd <= 0) outdata = mxdata._Copy(); // simply return the original data
        else 
        { // COLLATE OUTPUT DATA:
          outdata = new double[outLength];
          double[] indata;
          if (isRows) indata = mxdata;
          else // then we have to transpose the two data sets:
          { indata = Transpose(ref mxdata, orignorows);
            if (newdatalen > fixedstriplength) newdata = Transpose(ref newdata, fixedstriplength);
          }
          int nextPtr = 0;
          // Stick in all mx data before the insertion point:
          if (insertAt > 0) // i.e. the fn. is either 'append..', or is 'insert..' for a row/col > 0:
          { nextPtr = fixedstriplength * insertAt;
            Array.Copy(indata, outdata, nextPtr);
          } 
          // Add on the new data:
          for (int i = 0; i < timesToAdd; i++)
          { Array.Copy(newdata, 0, outdata, nextPtr, newdatalen);
            nextPtr += newdatalen;
          }
          // Add on any old data past this point in mx, if this was 'insert':
         if (insertAt < orignonfixedlength)
          { Array.Copy(indata, fixedstriplength * insertAt, outdata, nextPtr, (orignonfixedlength - insertAt) * fixedstriplength); }
          // Untranspose, if is a columns operation
          if (!isRows) outdata = Transpose(ref outdata, outLength / orignorows);
        }
        if (isRows)
        { result.I = V.GenerateTempStoreRoom(orignocols, outdata.Length / orignocols); }
        else
        { result.I = V.GenerateTempStoreRoom(outLength / orignorows,  orignorows); }
        StoreItem outitem = R.Store[result.I];
        outitem.Data = outdata;   outitem.IsChars = mxitem.IsChars;
        break;
      }
      case 220: // PLOTSHAPE( ch.arr. array     array     ch.arr.    scal.     scal/arr     scal/arr     scal.   scal.   scal.   scal.   scal.)
               //            Shape,  XCoords,  YCoords,  LineType, LineWidth, LineColour,  FillColour [,PivotX [,PivotY [,MagnX [,MagnY [,Rotn]]]]]
               //              0       1          2         3          4          5            6          7       8        9      10      11
                // While a scalar can be used for a valid FillColour, any scalar value of FillColour will result in full transparency.
      // ERRORS result simply in no drawing; errors not raised.
      { int shapeSlot = Args[0].I,  XSlot = Args[1].I,  YSlot = Args[2].I,  lnTypeSlot = Args[3].I;
        // Type testing omitted (and .X simply accepted) for all of the following except the last two (colours):
        double lnWidth = Args[4].X;
        // The optional args, all scalar:
        double pivotX = 0.0, pivotY = 0.0, magnX = 1.0, magnY = 1.0, rotn = 0.0;
        if (NoArgs > 7)  pivotX = Args[7].X;
        if (NoArgs > 8)  pivotY = Args[8].X;
        if (NoArgs > 9)  magnX  = Args[9].X;
        if (NoArgs > 10) magnY  = Args[10].X;
        if (NoArgs > 11) rotn   = Args[11].X;
        // Process those arguments needing further work:
        string shapeStr = StoreroomToString(shapeSlot, true, true);  if (shapeStr == "") break;
        char shapeType = ' ';
        if      (shapeStr == "polygon") shapeType = 'P';  else if (shapeStr == "rectangle") shapeType = 'R';
        else if (shapeStr == "ellipse") shapeType = 'E';
        else if (shapeStr == "arc" || shapeStr == "chord"  ||  shapeStr == "sector") shapeType = 'A';
        else break;
        if (XSlot == -1 || YSlot == -1 || lnTypeSlot == -1) break;
        double[] XX = R.Store[XSlot].Data,  YY = R.Store[YSlot].Data;  if (XX.Length != YY.Length) break;
        if (shapeType == 'A' && XX.Length > 2)
        { if (shapeStr == "arc") YY[2] = 0.0;  else if (shapeStr == "chord") YY[2] = 1.0;  else YY[2] = 2.0; }
        char lnShape = (StoreroomToString(lnTypeSlot, false, false))[0];
        bool success;
        Gdk.Color lnClr = InterpretColourReference(Args[5], JTV.White, out success);
        if (!success) lnClr = Plot.DefLnClr;
        Gdk.Color fillClr = InterpretColourReference(Args[6], JTV.White, out success); // But we ignore the return for a scalar arg.
        Shape ship = new Shape(shapeType, XX, YY, new char[] {lnShape}, new double[] {lnWidth}, new Gdk.Color[] {lnClr}, fillClr);
        if (Args[6].I == -1 || !success) ship.FillShape = false; // transparent shape, if FillColour is scalar or is invalid
        ship.Rotatn = rotn;  ship.MagnifyX = magnX;  ship.MagnifyY = magnY;
        ship.PivotX = pivotX;  ship.PivotY = pivotY;
        MainWindow.Plots2D.Add(ship);
        result.X = (double)ship.ArtID;
        break;
      }
     case 221: // COPYSHAPE(scalar PlotID; 1 to 5 values: PivotX [, PivotY [,  MagnX [,  MagnY [,  Rotn ] ] ] ]). Returns new plot's ID.
     // Fields not supplied will default to corresponding fields of the original plot. No errors; if PlotID not identified, returns 0.
     { int plotID = (int) Args[0].X;   Shape sheep = null;
        foreach (Plot plotto in MainWindow.Plots2D)
        { if (plotto.ArtID == plotID) { sheep = (Shape) plotto;  break; } }
        if (sheep == null) break; // returning 0.
        double[] values = AccumulateValuesFromArgs(Args, 1);
        Shape lamb = Shape.CopyOf(sheep, values);
        MainWindow.Plots2D.Add(lamb);
        result.X = (double)lamb.ArtID;
        break;
      }
      case 222: // PLOTBINS(Three equal-sized arrays - LeftEdges, array RightEdges, array Heights - OR  one 3xN matrix FillBinsOutput...
      //               THEN: [, scalar BarWidth [, scal/arr LnShape [, scalar LnWidth[ , scal/arr LnColour [, scal/arr FillColour ]]]]] )
      // Args. from LnShape on are as for 'plot', except that there is no equivalent to LnWeight, so all arguments apply to the whole graph.
      // 'BarWidth' is the ratio of (drawn bar's width) / (data bin's X-axis extent). The default is 1 (so that bars are touching).
      //   If set to 0 or a negative value, or if > 1, it also defaults to 1.
      {
        int slot0 = Args[0].I;  if (slot0 == -1) return Oops(Which, "1st. arg. must be an array");
        StoreItem It0 = R.Store[slot0];
        bool isMx = (It0.DimCnt == 2);
        double[] leftEdges = null, rightEdges = null, Heights = null;
        int arrayLen, noDataArgs;
        if (isMx)
        { if (It0.DimSz[1] != 4) return Oops(Which, "1st. arg., if a matrix, must have exactly 4 rows");
          arrayLen = It0.DimSz[0];
          noDataArgs = 1;
          double[] indata = It0.Data;
          leftEdges = indata._Copy(0, arrayLen);
          rightEdges = indata._Copy(arrayLen, arrayLen);
          Heights = indata._Copy(2*arrayLen, arrayLen);
        }        
        else
        { leftEdges = R.Store[slot0].Data;
          arrayLen = leftEdges.Length;
          noDataArgs = 3;
          int slot1 = Args[1].I, slot2 = Args[2].I;
          bool erred = false;
          if (slot1 == -1 || slot2 == -1) erred = true;
          else
          { rightEdges = R.Store[slot1].Data;  Heights = R.Store[slot2].Data;
            if (rightEdges.Length != arrayLen || Heights.Length != arrayLen) erred = true;
          }
          if (erred) return Oops(Which, "if the 1st. arg. is not a matrix, then the 1st. 3 args. must be arrays of equal length");
        }
        // Deal with the non-data args:
        int ndx = noDataArgs;
        double barWidth = 1.0;
        if (NoArgs > ndx)
        { barWidth = Args[ndx].X;  if (barWidth <= 0.0 || barWidth > 1.0) barWidth = 1.0; }
        ndx++;
        char lnShape = '_';
        if (NoArgs > ndx)
        { if (Args[ndx].I == -1) lnShape = Args[ndx].X._ToChar('\u0000'); 
          else lnShape = R.Store[Args[ndx].I].Data[0]._ToChar('\u0000');
        }
        char[] lnShapeArray = new char[] { lnShape };
        ndx++;
        double[] lnWidthArray = new double[1];
        if (NoArgs > ndx) lnWidthArray[0] = Args[ndx].X; else lnWidthArray[0] = 1.5;
        ndx++;
        Gdk.Color[] lnClrArray = new Gdk.Color[1];
        bool filled;
        if (NoArgs > ndx) lnClrArray[0] = InterpretColourReference(Args[ndx], JTV.Black, out filled); // 'out' return ignored
        else lnClrArray[0] = JTV.Black;
        ndx++;
        Gdk.Color fillClr;
        if (NoArgs > ndx) fillClr = InterpretColourReference(Args[ndx], JTV.White, out filled); // this time, 'out' return is used
        else { fillClr = JTV.Yellow;  filled = true; }
        // Work up the plots
        double[] plotNos = new double[arrayLen];
        double barInset = (1 - barWidth) * (rightEdges[0] - leftEdges[0]) / 2.0;
        double p,q;
        for (int i=0; i < arrayLen; i++) // we rotate clockwise around the rectangle, from bottom left corner:
        { p = leftEdges[i] + barInset;  q = rightEdges[i] - barInset;
          double[] xx = new double[] { p, p, q, q };
          double[] yy = new double[] { 0, Heights[i], Heights[i], 0 };
          Shape ship = new Shape('P', xx, yy, lnShapeArray, lnWidthArray, lnClrArray, fillClr);
          if (!filled) ship.FillShape = false; // transparent shape, if FillColour is scalar or is invalid
          plotNos[i] = (double) ship.ArtID;
          MainWindow.Plots2D.Add(ship);
        }
        result.I = V.GenerateTempStoreRoom(arrayLen);
        R.Store[result.I].Data = plotNos;
        break;
      }
      case 223:  case 224: // SUMROWS / SUMCOLS (Matrix [, FromStrip [, Extent ]]).
      case 296:  case 297: // PRODROWS / PRODCOLS( "           "          "    [, ZeroesAsOnes] ] ]  ).
      // Nothing raises an error except Matrix not being a matrix.
      // Extent < 0 returns zeroes. After that, ToStrip is calculated; if any strips exist between FromStrip and ToStrip inclusive,
      //  an array of strip sums is returned; otherwise an array of zeroes is returned.
      // For products, if (assumed scalar) ZeroesAsOnes present and nonzero, every zero AFTER the first value will be treated as if it 
      //  were a 1. (A row of only zeroes will return a product of 0.) This is only useful in, say, a jagged array, where there will
      //  be no zeroes before the last nonzero value in the row.
      { int mxslot = Args[0].I;
        if (mxslot == -1 || R.Store[mxslot].DimCnt != 2) return Oops(Which, "the first arg. must be a matrix");
        bool workonrows = (Which == 223 || Which == 296); // as opposed to working on columns
        bool summing = (Which == 223 || Which == 224); // as opposed to taking products
        bool zeroes_to_1 = false;   if (NoArgs > 3 && Args[3].X != 0.0) zeroes_to_1 = true;
        int mxrows = R.Store[mxslot].DimSz[1],  mxcols = R.Store[mxslot].DimSz[0];
        double[] mxdata = R.Store[mxslot].Data;
        int newsize = mxrows, nostrips = mxcols; if (workonrows) { newsize = mxcols; nostrips = mxrows; }
        int newslot = V.GenerateTempStoreRoom(newsize);  
        double[] newdata = R.Store[newslot].Data;
        if (!summing) { for (int i=0; i < newdata.Length; i++) newdata[i] = 1.0; } // product accumulators have to start as 1.
        int extent = nostrips;
        if (NoArgs >= 3)
        { extent = (int) Args[2].X; 
          if (extent < 1) { result.I = newslot;  break; } // return an empty array.
        }  
        int fromstrip = 0;
        if (NoArgs >= 2)
        { fromstrip = (int) Args[1].X;  
          if (fromstrip >= nostrips) { result.I = newslot; break; } // return an empty array.
        }
        int tostrip = fromstrip + extent - 1;
        if (tostrip < 0) { result.I = newslot; break; } // return an empty array.
        else if (tostrip >= nostrips) tostrip = nostrips - 1;
        if (fromstrip < 0) fromstrip = 0; // has to follow setting of 'tostrip'
        double x=0;
        if (workonrows)
        { for (int rw = fromstrip; rw <= tostrip; rw++) 
          { int offset = rw * mxcols;
            if (summing)
            { for (int cl = 0; cl < mxcols; cl++) newdata[cl] += mxdata[offset + cl]; }
            else
            { for (int cl = 0; cl < mxcols; cl++) 
              { x = mxdata[offset + cl];  if (rw > 0 && zeroes_to_1 && x == 0.0) x = 1.0;
                newdata[cl] *= x; 
              }
            }
          }
        }  
        else // work on columns:
        { for (int cl = fromstrip; cl <= tostrip; cl++) 
          { if (summing){ for (int rw = 0; rw < mxrows; rw++) newdata[rw] += mxdata[rw*mxcols + cl]; }
            else            
            { for (int rw = 0; rw < mxrows; rw++)
              { x = mxdata[rw*mxcols + cl];
                if (cl > 0 && zeroes_to_1 && x == 0.0) x = 1.0;
                newdata[rw] *= x;
              }  
            }  
          }
        }  
        result.I = newslot;  break;
      }  
      case 225: case 226: // DELETEROWS / -COLS: Version 1 --  (InMatrix, scalar FromStrip, scalar Extent);
      //  Version 2 -- (InMatrix, array WhichRows).  Chars. rating is preserved. Version 2: repetitions ignored.
      // Nothing raises an error except wrong type of argument (i.e. scalar v. array v. matrix).
      // Pointer errors in version 1:
      //    Extent <= 0 --> InMatrix simply returned. Extent beyond end --> reduced to end at last strip inclusive.
      //    FromStrip negative: Rows (FromStrip + i) are removed once this sum becomes nonnegative.
      //    Fromstrip >= no. strips: InMatrix returned.
      // WhichRows errors in version 2: illegal row numbers are ignored.
      // RETURN, if whole of InMatrix has been deleted, is the empty array [NaN].
      {
       // Sort out the ARGUMENTS:
        StoreItem sitem = null;
        int mxslot = Args[0].I;   if (mxslot >= 0) sitem = R.Store[mxslot];
        if (sitem == null || sitem.DimCnt != 2) return Oops(Which, "the first arg. must be a matrix");
        bool doRows = (Which == 225);
        int mxrows = sitem.DimSz[1],  mxcols = sitem.DimSz[0];
        int mxstrips = mxrows;  if(!doRows) mxstrips = mxcols;
        int[] knockemoff = null;
        if (NoArgs == 2)
        { if (Args[1].I == -1) return Oops(Which, "in the two arg. version the second arg. must be an array");
          knockemoff = R.Store[Args[1].I].Data._ToIntArray();
        }
        else
        { if (Args[1].I != -1 || Args[2].I != -1) return Oops(Which, "3 arg. version: 2nd. and 3rd. args. must be scalar");
          int fromStrip = Convert.ToInt32(Args[1].X);
          int extent = Convert.ToInt32(Args[2].X);
          if (fromStrip >= mxstrips) extent = 0; // will result in original matrix being returned.
          else
          { if (fromStrip + extent > mxstrips) extent = mxstrips - fromStrip;         
            if (extent < 0) extent = 0; // will result in original matrix being returned.
          }
          knockemoff = new int[extent];
          for (int i=0; i < extent; i++) knockemoff[i] = fromStrip + i;
        }
       // We now have a list of rows / columns to remove. (Though the list may be empty, but not null).
       // Prepare the inverse list - rows / columns to be retained.
        int knockemoffLen = knockemoff.Length;
        int[] keepthese = new int[mxstrips];
        for (int i=0; i < knockemoffLen; i++)
        { int n = knockemoff[i];
          if (n >= 0  && n < mxstrips) keepthese[n] = -1; // at the end of the day, 0 will signify 'keep' and -1 'reject'.
        }
        int noToGo = 0;
        for (int i=0; i < mxstrips; i++) { if (keepthese[i] == -1) noToGo++; }
        if (noToGo == mxstrips) { result.I = EmptyArray();  break; }
       // Prepare the output structure:
        double[] indata = sitem.Data;
        double[] outdata = null;
        int newRowCnt = mxrows, newColCnt = mxcols; // default values only
        if (noToGo == 0) outdata = indata._Copy();
        else
        { if (doRows) newRowCnt -= noToGo;  else newColCnt -= noToGo;
          outdata = new double[newRowCnt * newColCnt];
          int outdatacnt = 0;
          if (doRows)
          { for (int i=0; i < mxrows; i++)
            if (keepthese[i] == 0)
            { Array.Copy(indata, i*mxcols, outdata, outdatacnt, mxcols);
              outdatacnt += mxcols;
            }
          }
          else // do columns:
          { int offset = 0;
            for (int i=0; i < mxrows; i++)
            { offset = i * mxcols;
              outdatacnt = i * newColCnt;
              for (int j=0; j < mxcols; j++)
              {
                if (keepthese[j] == 0)
                {
                  outdata[outdatacnt] = indata[offset + j];
                  outdatacnt++;
                }
              }
            }
          }
        }
        result.I = V.GenerateTempStoreRoom(newColCnt, newRowCnt);
        StoreItem soutem = R.Store[result.I];
        soutem.Data = outdata;
        soutem.IsChars = sitem.IsChars;
        break;
      }

      case 227: // XMENU(chars array MainMenuItemTitle, chars array SubMenuTitles) -- VOID.
      // The arguments are treated independently; if one is improper, the other (if proper) will still be applied.
      // Submenu titles within the last argument are separated from one another by "|"; the no. of submenus will be set as 1 + no. of "|"s.
      // If the string from MainMenuItemTitle is empty after trimming, the existing title remains.
      // If SubMenuTitles produces a string array with any empty items after trimming, the whole existing submenu system remains as is.
      //  otherwise the no. submenus will be the length of the string array, and its strings will become the submenu titles.
      // The Extra menu and all submenus will be visible; if you don't like this, you have to apply function XVISIBLE to change it.
      // Submenu items are trimmed in FM.SetExtra..., so blanks around the '|' delimiters are ignored.
      { MainWindow FM = MainWindow.ThisWindow;
        string maintitle = StoreroomToString(Args[0].I, true, true);
        string ss = StoreroomToString(Args[1].I, true, true);
        string[] subtitles = ss.Split(new char[]{'|'}, StringSplitOptions.None);
        FM.SetExtraMenuTitles(maintitle, subtitles);
        break;
      }
      case 228: // XVISIBLE(any no. of scalars or arrays). NONVOID; always returns the current visibility of extra menus
      // as an array; [0] = visibility of main menu item; [1]+ = visibility of submenus (so that the array's length is 1 + no. of submenus).
      // Uses the argument values as pseudobooleans to set visibilities, starting from the main menu item and proceeding to the submenus.
      // Shortfall: remaining submenus retain their former visibility. Excess: ignored.
      // If you only want to read the state of the menus, use a single argument which matches the present visibility of the main menu,
      // usually 1 for boolean TRUE (as the visibility of submenus is irrelevant if the main menu item is invisible).
      // simply be taken as 'true'.)
      { MainWindow FM = MainWindow.ThisWindow;
        bool[] subvis = null;
        // set visibilities, if arg an array:
        double[] indata = AccumulateValuesFromArgs(Args);
        int inlen = indata.Length; // must be at least 1.
        subvis = new bool[inlen];
        for (int i=0; i < inlen; i++) subvis[i] = (indata[i] != 0.0);
        FM.SetExtraMenuVisibilities(subvis);
        // Get visibilities:
        subvis = FM.GetExtraMenuVisibilities();
        int outlen = subvis.Length;
        double[] outdata = new double[outlen];
        for (int i=0; i < outlen; i++) { if (subvis[i]) outdata[i] = 1.0; }
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        break;
      }
      case 229: // XCLICK() -- Returns the index of the submenu clicked AND resets the click flag in the main form.
      // If no click since startup or the last reset, returns -1. Submenus are numbered from 1 upwards, as in 'xvisible(.)'.
      { result.X = (double) MainWindow.ThisWindow.ExtraClicked;
        if (result.X >= 0) result.X++; // i.e. submenus will be numbered by this function to base 1.
        MainWindow.ThisWindow.ExtraClicked = -1;
        break;
      }
      case 230: // PLOT( array    array   scal/arr scal/arr  scal/arr  scal/arr scal/arr scal/arr   array    array    array   char. array )
      //                YCoords, XCoords, PtShape, PtWidth,  PtColour, LnShape, LnWidth, LnColour, PtWeight LnWeight Texts   FontName(s)
      // Arg.no. = no. coord. arrays +:      0        1          2        3        4        5          6        7      8         9
      // Suitable default inducer:          scal      -          -       scal      -        -         scal     scal   scal
      //  ('-' = no default; if it will be accessed (e.g. PtWidth when PtShape is valid), arg. must be supplied with a sensible value.
      case 231: // PLOT3D(ZCoords, YCoords, XCoords, rest as above)
      // Creates a Plot object, puts that Plot onto MainWindow.Plots2D/3D and return its ID. (Use 'graph(.)' then to insert it into a graph.)
      // COORDS - 2D: If there is only one arg. (must be an array), then that is graphed against a matching X-axis array { 0, 1, 2, ...}.
      //   This is only intended for a quick look at the shape of data in the array; you obviously cannot set sizes, colours etc. To do that,
      //   instead use the method below with Args[1] scalar and set (perhaps) to 0.
      // If Args[0] and [1] are both scalar, they represent a single point (being converted into 1D arrays, length 1).
      // If Args[0] array but [1] scalar ( = n), [1] is converted to the arithmetic progression [n, n+1, n+2,..], of same length as [1].
      // If both are arrays, then Args[0] represents Y values, Args[1] X values. If the X-length is less than the Y-length, X values are
      // recycled; e.g. if the Y array were three times as long, there would be three curves on the graph. If Y-length < X-length, Y values
      // are recycled, giving a periodic curve. In all array cases, the structure of the array is ignored.
      // Note that it is illegal for Args[0] to be scalar but for Args[1] to be an array (as frequency of this being intended would be much
      // less than the frequency of this arising in error - in which case it should raise an error).
      // COORDS - 3D: Args[0] to [2] must be arrays of equal length, representing values of Z, Y and X coordinates. No variations as for 2D.
      // SPECIFIC ARGS: PT/LN SHAPE must be a chars. array or if scalar, a unicode  value. Structure ignored; no trimming occurs.
      //  If an array with length > 1, values will be used separately in accord with PtWeight.
      // PT/LN WIDTH can be scalar or array (structure ignored).
      // PT/LN COLOUR can be scalar ( = index for F.Palette), a nonchars. list array (taken as an array of indexes for F.Palette),
      //  a chars. list array (representing a single colour as hex "0xRRGGBB", or as a standard name; or if containing delimiter '|',
      //  as a set of either - can be mixed), a nonchars. matrix of row length 3 (each row is an RGB set), 
      //  a chars. matrix in which each row is a hex no. ("0xRRGGBB") or a standard colour name (no '|' recognized here).
      // PT/LN WEIGHT, if present and is an array, is transferred directly to the plot's PtWeight property.
      // TEXTS is ignored if scalar, otherwise treated without regard to its structure as a chars. array. The delimiter between texts
      // (if more than one) is '|'; spaces are significant. Examples: "A" recurrently plots A; "A|B|C" plots A,B,C,A,B,C,..
      //   If you don't like this delimiter, prefix the string with "}c{", where c is the delimiter character to be used instead of '|'.
      // FONT NAME(s) is either a single font name (no size) - "Sans" - or a series of font names in order of preference, sep'd by commas:
      // "Arial, Verdana, Ubuntu, Sans". (Spaces INSIDE a font name are significant.)
      { bool is3D = (Which == 231);
      // PROCESS COORDINATES -- end up always with two / three equal-length arrays, length at least 1:
        double[] XX = null, YY = null, ZZ = null;
        int inslotX, inslotY, inslotZ,  offset; // index of first non-coordinate arg.
        char[] ptshape = null,     lnshape = null;
        double[] ptwidth = null,  lnwidth = null;
        Gdk.Color[] ptclr = null, lnclr = null; // Parsing errors below will --> black points or lines, rather than the default colour.
        int[] ptweight = null, lnweight = null; // Parsing errors below will --> black points or lines, rather than the default colour.
        string[] texts = null;
        string fontname = "";
        if (!is3D) // 2D CASE:
        { inslotY = (int) Args[0].I;
          offset = 2;
          if (NoArgs == 1) // 'plot3d' would not get here, as minimum args. is 3.
          { if (inslotY == -1) return Oops(Which, "for the single-argument form, the single argument must be an array");
            YY = R.Store[inslotY].Data;
            XX = new double[YY.Length];
            for (int i=0; i < XX.Length; i++){ XX[i] = (double) i; }
          }
          else
          { inslotX = (int) Args[1].I;
            if (inslotY == -1) // scalar YY:
            { if (inslotX >= 0) return Oops(Which, "the combination of a scalar first arg. and an array as second arg. is not allowed");
              XX = new double[] { Args[1].X };   YY = new double[] { Args[0].X };
            }
            else // Y coords are an array:
            { YY = R.Store[inslotY].Data;
              if (inslotX == -1) // scalar X coord:
              { double x = Args[1].X;
                XX = new double[YY.Length];
                for (int i=0; i < XX.Length; i++){ XX[i] = x + (double) i; }
              }
              else // both sets of coords are arrays:
              { XX = R.Store[inslotX].Data;
                int lenY = YY.Length, lenX = XX.Length;
                if      (lenY > lenX) XX = XX._ConformSize(lenY, true, 0); // Resizes XX to YY length by recycling XX values
                else if (lenX > lenY) YY = YY._ConformSize(lenX, true, 0); // Resizes YY to XX length by recycling YY values
              }
            }
          }
        }
        else // 3D CASE:
        { inslotZ = (int) Args[0].I;  inslotY = (int) Args[1].I;  inslotX = (int) Args[2].I;
          offset = 3;
          bool oops = (inslotX == -1 || inslotY == -1 || inslotZ == -1);
          int len = 0;
          if (!oops)
          { XX = R.Store[inslotX].Data;  len = XX.Length;
            YY = R.Store[inslotY].Data;
            ZZ = R.Store[inslotZ].Data;
          }
          if (oops || YY.Length != len || ZZ.Length != len) return Oops(Which, "the first 3 args. must be arrays of equal length");
        }

      // PROCESS PARAMETER ARGUMENTS:
        int longestPtArray = 1, longestLnArray = 1; // information needed for processing ptweight and lnweight later.
       // Point and line shape:
        if (NoArgs > offset)     ptshape = Plot_Shape(Args[offset].I, Args[offset].X, ref longestPtArray); // If scalar, taken as a unicode value.
        if (NoArgs > offset + 3) lnshape = Plot_Shape(Args[offset+3].I, Args[offset+3].X, ref longestLnArray); // If scalar, taken as a unicode value
       // Point and line width:
        if (NoArgs > offset + 1) ptwidth = Plot_Width(Args[offset+1].I, Args[offset+1].X, ref longestPtArray);
        if (NoArgs > offset + 4) lnwidth = Plot_Width(Args[offset+4].I, Args[offset+4].X, ref longestLnArray);
       // Point and line colour:
        if (NoArgs > offset + 2) ptclr = Plot_Colour(Args[offset+2].I, Args[offset+2].X, ref longestPtArray, JTV.Black);
        if (NoArgs > offset + 5) lnclr = Plot_Colour(Args[offset+5].I, Args[offset+5].X, ref longestLnArray, JTV.Black);
       // Point and line weight:
        int slit; double[] dumdum = null;
        if (NoArgs > offset + 6) { slit = Args[offset+6].I;  if (slit >= 0) dumdum = R.Store[slit].Data; }
        if (dumdum == null) ptweight = new int[longestPtArray]; // No special need to make it this length; it just cuts down on modulos
                                                                //  in Board.cs's drawing methods; but this is of extremely little advantage.
        else ptweight = dumdum._ToIntArray();
        dumdum = null;
        if (NoArgs > offset + 7) { slit = Args[offset+7].I;  if (slit >= 0) dumdum = R.Store[slit].Data; }
        if (dumdum == null) lnweight = new int[longestLnArray];
        else lnweight = dumdum._ToIntArray();
       // Texts:
        if (NoArgs > offset + 8)
        { string stroo = StoreroomToString(Args[offset+8].I); // returns "" if arg. was scalar
          if (stroo.Length > 0)
          { char delimiter = '|'; // the default.
            if (stroo.Length > 3 && stroo[0] == '}' && stroo[2] == '{')
            { delimiter = stroo[1]; // E.g. "}#{..." changes delimiter to '#'.
              stroo = stroo.Substring(3);
            }
            texts = stroo.Split(new char[] {delimiter}, StringSplitOptions.None); // allow empty entries - where two delimiters are adjacent.
          }
        }
        if (NoArgs > offset + 9) fontname = StoreroomToString(Args[offset+9].I);
      // PLOT IT ALL:
        Plot pollute;
        if (is3D) pollute = new Plot(XX, YY, ZZ, ptshape, ptwidth, ptclr, lnshape, lnwidth, lnclr);
        else      pollute = new Plot(XX, YY, ptshape, ptwidth, ptclr, lnshape, lnwidth, lnclr);
        pollute.Texts = texts;   pollute.PtWeight = ptweight;    pollute.LnWeight = lnweight;
        if (fontname != "") pollute.FontName = fontname;
        if (is3D) MainWindow.Plots3D.Add(pollute);  else MainWindow.Plots2D.Add(pollute);
        result.X = (double) pollute.ArtID;
        break;
      }
      case 232: case 233: // GRAPH(..) / GRAPH3D(..)
      // Creates a new board which display(s) the plots accumulated in the arguments (scalar or array). If any plot cannot be identified
      // in the record of existing plots, it is simply not plotted - no error message. If no plot is identiable, a blank graph results.
      // Returns the ID of the board. 
      {// Read in the user's supplied list of plots:
        bool is3D = (Which == 233);
        int[] plotids = (AccumulateValuesFromArgs(Args))._ToIntArray();
        int n;
        if (is3D) n = 3;  else n = 2;
        Plot[] plots = MainWindow.GetPlotPointers(n, plotids); // 'plots' can be empty, but not null.
        int noplots = plots.Length;
        int broad;
//Got to here ################
        if (!is3D) broad = Board.Create(625, 450, 25, 350, true);
        else       broad = Board.Create(700, 600, 25, 450, true); // ###### Select a more suitable starting size
// Didn't get to here ############
        double lowX, highX, noXsegs,   lowY, highY, noYsegs,   lowZ=0, highZ=0, noZsegs=0;
        if (NextXSegments > 0)
        { lowX = NextXCornerReal;  highX = NextXTipReal;  lowY = NextYCornerReal;  highY = NextYTipReal;
          noXsegs = NextXSegments;  noYsegs = NextYSegments;
          if (is3D) { lowZ = NextZCornerReal;  highZ = NextZTipReal;  noZsegs = NextZSegments; }
          // All of the XYZ values are reset every time, whether the graph is 2D or 3D:
          NextXCornerReal = 0.0; NextYCornerReal = 0.0; NextZCornerReal = 0.0;
          NextXTipReal = 0.0;    NextYTipReal = 0.0;    NextZTipReal = 0.0;
          NextXSegments = 0;     NextYSegments = 0;     NextZSegments = 0;
        }
        else if (noplots == 0) { lowX = lowY = lowZ = 0.0;  highX = highY = highZ = 1.0;  noXsegs = noYsegs = noZsegs = 1; } // Empty graph
        else
        { double[] extremes = Graph.DevelopExtremes(plots, is3D); // extremes: [0] = lowX, [1] = highX, [2] = lowY, [3] = highY, [4,5] - for Z.
          Triplex treX = Graph.RationalizedGraphLimits(extremes[0], extremes[1]); // [0] = lowest X, [1] = highest X.
          Triplex treY = Graph.RationalizedGraphLimits(extremes[2], extremes[3]); // [2] = lowest Y, [3] = highest Y.
          lowX = treX.X;  highX = treX.Y;  noXsegs = (int) treX.Z;
          lowY = treY.X;  highY = treY.Y;  noYsegs = (int) treY.Z;
          if (is3D)
          { Triplex treZ = Graph.RationalizedGraphLimits(extremes[4], extremes[5]); // [4] = lowest Z, [5] = highest Z.
            lowZ = treZ.X;  highZ = treZ.Y;  noZsegs = (int) treZ.Z;
          }
        }
        Graph graff;
        if (!is3D) graff = new Graph(broad, lowX, highX, (int) noXsegs, lowY, highY, (int) noYsegs);
        else  graff = new Graph(broad, lowX, highX, (int) noXsegs, lowY, highY, (int) noYsegs, lowZ, highZ, (int) noZsegs);
        if (double.IsNaN(GraphAscension)) graff.Ascension = DefaultGraphAscension;
        else { graff.Ascension = GraphAscension;  GraphAscension = double.NaN; }
        if (double.IsNaN(GraphDeclination)) graff.Declination = DefaultGraphDeclination;
        else { graff.Declination = GraphDeclination;  GraphDeclination = double.NaN; }
        // The above adds graff into the board.
        graff.AddPlot(plots);
        result.X = (double) graff.ArtID;
        break;
      }
      case 234:  case 235: // HEADER,  FOOTER ( scalar: GraphID [, any no. args, scalar or array: Stuff ] ).
      // Serve 2D and 3D graphs. NON-VOID; always returns an array.
      // Forms of HEADER:
      //   (1) 'header(G, Stuff)' sets the header of existing graph G. Rules for args. after the first are as for the args. of 'write(.)',
      //         except for the use of Pango tags for formatting (see below). Returns the header (for the sake of consistency with the below).
      //   (2) 'header(G)' changes nothing, but returns the existing header of graph G. (For both usages, obviously a chars. array.)
      // Forms of FOOTER:
      //   (1) 'footer(G, Stuff)' sets the header of existing graph G. Rules for the 2nd. arg. are as for the arg. of 'write(.)',
      //        including the use of standard formatting tags. Returns the footer (for the sake of consistency with the below), with all
      //        its formatting tags. (If 'Stuff' is a single scalar arg., this form will be interpreted as form 3 below.)
      //   (2A) 'Stuff' begins with the 3 chars. '|+|' -- The cursor will become visible (these 3 chars. will not be displayed).
      //   (2B) 'Stuff' begins with the 3 chars. '|-|' -- The cursor will be forced invisible (these 3 chars. will not be displayed).
      //   (3) 'footer(G [, bool WYSIWYG] )' changes nothing, but returns the footer header of graph G. if WYSIWYG is absent or false,
      //        returns the footer as last set by a 'footer(.)' function call. If WYSIWYG true, you get what you see on the graph, inclusive
      //        of direct typing into the editable box where it is displayed. BUT whether directly altered by the user or not, what is
      //        returned will be bare text DEVOID OF FORMATTING TAGS.
      //      .
      // For either HEADER or FOOTER, if G does not exist, the fn. returns the 'empty array' - size 1, value NaN, non-chars.
      // Re HEADER FORMATTING: Footer uses JTextView tags.  Header uses Pango tags; but JTextView tags <B>,<U>,<I>,<^> and <v> are internally
      //  converted to Pango tags. However they must be paired (e.g. you can't negate <v> with <^> or </^>, as you can with JTextView tags).
      {
       // Is an existing graph mentioned, or is this a setting for some future graph?
        bool isHeader = (Which == 234);
        int slot = Args[0].I;
        int graphID = 0;
        Graph graf = null;
        Trio trill = new Trio();
        if (slot >= 0) { result.I = EmptyArray();  break; } // No graph ID, so no dice.
        graphID = (int) Args[0].X;
        trill = Board.GetBoardOfGraph(graphID, out graf);
        if (graf == null) { result.I = EmptyArray();  break; } // Graph ID found, but it does not answer to any existing graph.
        // Forms that are to return the current text, but do nothing else:
        if (NoArgs == 1 || (!isHeader && NoArgs == 2 && Args[1].I == -1) ) // then this is a request to return the EXISTING value:
        { string windowTitle, currentHdr, origFtr, currentFtr, whatever;
          Board.GetTexts(trill.X, out windowTitle, out currentHdr, out origFtr, out currentFtr);
          if (isHeader) whatever = currentHdr;
          else { if (NoArgs == 1 || Args[1].X == 0.0) whatever = origFtr; else whatever = currentFtr; }
          if (whatever == "") whatever = " ";
          result.I = V.GenerateTempStoreRoom(whatever.Length);
          StringToStoreroom(result.I, whatever);  break;
        }
       // Collate the rest of the arguments into a displayable string:
        string displaytext = ArgsToDisplayString(1, Args, REFArgLocn, "");
        if (displaytext[0] == '\u0000')
        { JD.Msg("cannot process the text for the graph: " + displaytext._Extent(1));
          displaytext = " ";
        }        
        if (isHeader)
        { if (displaytext.IndexOf('<') != -1)  displaytext = JD.ConformTagsToPangoTags(displaytext);
          Board.Texts(trill.X, null, displaytext, null);
        }
        else // footer:
        { string ss = displaytext._Extent(0, 3);
          if (ss == "|+|") Board.Texts(trill.X, null, null, displaytext._Extent(3), true);
          else if (ss == "|-|") Board.Texts(trill.X, null, null, displaytext._Extent(3), false);
          else Board.Texts(trill.X, null, null, displaytext);
        }
        Board.ForceRedraw(trill.X, true);
        // In all cases, return the display text:
        result.I = V.GenerateTempStoreRoom(displaytext.Length);
        StringToStoreroom(result.I, displaytext);  break;
      }
      case 236:  case 237:  case 238:
      // LABELX,  LABELY,  LABELZ ( scalar: GraphID [, any no. scalars and arrays: Text]). TWO CASES: (1) There is more than one argument:
      //   all from the 2nd. arg. onwards is taken as text and treated the same as e.g. args. of "show(.)". This form is VOID.
      //   (2) The only arg. is the graph ID; in that case the function RETURNS the current string from the graph.  
      //   If no such graph, simply nothing happens - no error message.
      { int slot = Args[0].I;
        int graphID = 0;
        Graph graf = null;
        Trio trill = new Trio();
        if (slot >= 0) break; // No graph ID, so no dice.
        graphID = (int) Args[0].X;
        trill = Board.GetBoardOfGraph(graphID, out graf);
        if (graf == null) break; // Do nothing, if supplied graph not identified.
        // You just want to read the text that is already there?
        if (NoArgs == 1)
        { string ss;
          if      (Which == 236) ss = graf.AxisNameX;
          else if (Which == 237) ss = graf.AxisNameY;
          else                   ss = graf.AxisNameZ; // No harm if a 2D graph - will simply be an array of one space.
          if (String.IsNullOrEmpty(ss))  ss = " ";  
          result.I = V.GenerateTempStoreRoom(ss.Length);
          StringToStoreroom(result.I, ss);
          break;
        }
      // Collate the rest of the arguments into a displayable string:
        string displaytext = ArgsToDisplayString(1, Args, REFArgLocn, "");
        if (displaytext[0] == '\u0000')
        { JD.Msg("cannot process the text for the label: " + displaytext._Extent(1));
          displaytext = " ";
        }        
        string fname = SysFn[Which].Name;
        if      (fname == "labelx") graf.AxisNameX = displaytext;
        else if (fname == "labely") graf.AxisNameY = displaytext;
        else if (fname == "labelz") graf.AxisNameZ = displaytext;
        graf.LastGraphRecalcn = DateTime.Now.Ticks;
        Board.ForceRedraw(trill.X, true);
        break;
      }  
      case 239: // PLOTSOF( GraphID). Always returns an array, its size being no. of plots (if any), and contents being the plot IDs.
      // If no plots or no graph, the empty array is returned: length 1, [0] = NaN.
      { int graphid = (int) Args[0].X;
        double[] output;  Graph carafe;
        Board.GetBoardOfGraph(graphid, out carafe);
        if (carafe == null) { result.I = EmptyArray();  break; }
        int[] plottery = carafe.PlotsExtantIDs.ToArray();
        int noplots = plottery._Length();
        if (noplots < 1) { result.I = EmptyArray(); break; }
        output = plottery._ToDubArray();
        result.I = V.GenerateTempStoreRoom(output.Length);
        R.Store[result.I].Data = output;   
        break;
      }  
      case 240: case 241: case 242: // SCALEOVERX, SCALEOVERY, SCALEOVERZ(GraphID, array Overlay) // 2D or 3D. GraphID cannot be omitted. 
      // QUITE DIFFERENT for 2D and 3D, as in 2D scale strings are dotted along the axes, whereas in 3D the only scale references are "From...
      // To... Step..." statements to the right of the graph.
      // (1) 2D CASE: Overwrite the graph's natural scale strings using a single chars. array with delimiter ',' between hairline texts.
      //   If too many, excess ignored. Blanks are significant. Contigous commas allowed. A starting or finishing comma implies a blank
      //   field to its left. To remove all scaling strings, supply a sufficiently long list of commas, OR supply an array of length 1
      //   containing the char. "#".
      // (2) 3D CASE: 'Overlay' must contain exactly 3 values (or the fn. is ignored): the From, To and Step values.
      { int graphid = (int) Args[0].X;
        Graph garth;
        Board.GetBoardOfGraph(graphid, out garth); if (garth == null) break; // Do nothing, if supplied graph ID not identified.
        int slot = Args[1].I;  if (slot == -1) break;
        string stroo = StoreroomToString(slot);  if (stroo == "") break;
        string[] shrew = stroo.Split(',');
        string name = SysFn[Which].Name;
        if (garth.Is3D)
        { if (shrew.Length != 3) break;
          if      (name == "scaleoverx") garth.TickStringsX = shrew;
          else if (name == "scaleovery") garth.TickStringsY = shrew;
          else                           garth.TickStringsZ = shrew;
        }
        else // 2D:
        { if      (name == "scaleoverx")
          { if (stroo == "#") garth.TickStringsXVisible = new int[] { int.MaxValue, 0 };  else  garth.TickStringsX = shrew; }
          else if (name == "scaleovery")
          { if (stroo == "#") garth.TickStringsYVisible = new int[] { int.MaxValue, 0 };  else  garth.TickStringsY = shrew; }
        }
        garth.ForceRedraw();
        break;
      }
      case 243:  case 244: // SCALEFUDGEX, SCALEFUDGEY (scalar GraphID, scalar FudgeFactor,
                           //     array Suffix [, bool SuppressSuffixAt0, bool SuppressValueAt1)
      // 2D ONLY!. A graph ID must always be specified. If graph not identified, simply nothing happens. FudgeFactor will multiply all
      // of the natural scale tags, so if you want them to stay as is, use 1.0. 
      // Suffix is the char. string to follow the value; if you want a space between value and suffix, prefix it to the suffix. But
      // if suffix is only made up of spaces, there will be no suffix.
      // the first bool will replace e.g. "0 PI" with just "0"; the second will replace "1 PI" with just "PI". 
      // Arguments for leave-as-is are (graphid, 1, " "[, 0, 0]).
      { int graphid = (int)Args[0].X;
        bool isX = (Which == 243);
        Graph garth;
        Board.GetBoardOfGraph(graphid, out garth);
        if (garth == null || garth.Is3D) break; // Do nothing, if supplied graph ID not identified.
        if (Args[1].I >= 0 || Args[2].I == -1) break;
        double fudge = Args[1].X; 
        int slot = Args[2].I;  
        string suffix = StoreroomToString(slot); if (suffix == "") break;
        if (suffix.Trim() == "") suffix = ""; // otherwise don't trim, as spaces would be intentional inclusions.
        bool suppresssuffixif0 = (NoArgs > 3 && Args[3].X != 0.0), suppressvalueif1 = (NoArgs > 4 && Args[4].X != 0.0);
        if (isX)
        { garth.FudgeFactorX = fudge;
          garth.SuffixX = suffix;
          garth.SuppressSuffixIfZeroX = suppresssuffixif0;
          garth.SuppressValueIfOneX = suppressvalueif1;
        }
        else
        { garth.FudgeFactorY = fudge;
          garth.SuffixY = suffix;
          garth.SuppressSuffixIfZeroY = suppresssuffixif0;
          garth.SuppressValueIfOneY = suppressvalueif1;
        }
        garth.ForceRedraw();
        break;
      }  
      case 245: // MOVESHAPE(GraphID, PlotID, PivotX [, PivotY [, MagnifierX [, MagnifierY [, Rotation ]]]]) -- VOID.
      // All values after GraphID and PlotID can be incorporated into any scalar/array mix.
      // Presupposes a previous use of 'plotshape(.)' to generate the shape with ID PlotID. If not, does nothing and raises no error.
      // Note that the arrays giving basic plot details (e.g. corner coordinates, for a polygon) are not altered, and no new Shape object
      //   is created, thus cutting down considerably on time.
      // If GraphID is invalid (preferably 0 or negative, for speed), then the shape is indeed moved, but no graph is referenced. (If the plot
      //   is in some unreferenced graph, that graph would not be refreshed here by a 'ForceRedraw()', and so would not automatically show changes.)
      // If a valid graph ID is supplied, then (a) if it contains the plot of PlotID, the moved plot is redrawn on it;
      //  (b) if not, then the moved plot is added into it before the redraw is forced.
     {  // Find the plot; if nonexistent, simply return:
        int plotID = Convert.ToInt32(Args[1].X);   Shape sheep = null;
        foreach (Plot plotto in MainWindow.Plots2D)
        { if (plotto is Shape && plotto.ArtID == plotID) { sheep = (Shape) plotto;  break; } }
        if (sheep == null) break;
       // Update the plot:
        double[] argValues = AccumulateValuesFromArgs(Args, 2);
        int len = argValues.Length;
        sheep.PivotX   = argValues[0]; // The only obligatory argument.
        if (len > 1) sheep.PivotY   = argValues[1];
        if (len > 2) sheep.MagnifyX = argValues[2];
        if (len > 3) sheep.MagnifyY = argValues[3];
        if (len > 4) sheep.Rotatn   = argValues[4];
       // identify any nominated graph:
        int graphID = Convert.ToInt32(Args[0].X);  if (graphID <= 0.0) break; // if so, user did not intend there to be any graph.
        Graph graphite = null;
        Board.GetBoardOfGraph(graphID, out graphite); if (graphite == null) break; // Do nothing, if supplied graph ID not identified.
       // Check if it has this plot in it:
        int findPtr = -1;
        List<Plot> plex = graphite.PlotsExtant;
        for (int i=0; i < plex.Count; i++)
        { if (plex[i].ArtID == plotID) { findPtr = i; break; } }
        if (findPtr >= 0) graphite.ForceRedraw(); // then stick the plot into the graph:
        else graphite.AddPlot(sheep); // that method contains its own 'ForceRedraw()'.
        break;
      }

      case 246: case 247: case 248: case 249: // SCALEFORMATX, SCALEFORMATY, SCALEFORMATZ, SCALEFORMAT(GraphID[, array FormatString])
      // 2D or 3D. You have to know what you are doing! For valid formats, see C# Visual Express help, and put in index exactly the words:
      //  "standard numeric format strings". If FormatString is missing or scalar or contains only blanks, the default format (currently "G4")
      //  is reapplied (it was originally applied when the graph was created).
      { int graphid = (int)Args[0].X;
        Graph goth;
        Board.GetBoardOfGraph(graphid, out goth); if (goth == null) break; // Do nothing, if supplied graph ID not identified.
        string formatstr = "";  if (NoArgs > 1) formatstr = StoreroomToString(Args[1].I, true, true);
        formatstr = formatstr._Purge(JS.WhiteSpaces);
        if (formatstr == "") formatstr = Graph.DefValueFormat;
        int whicho = Which - 246;
        if (whicho == 0 || whicho == 3) goth.ValueFormatX = formatstr;
        if (whicho == 1 || whicho == 3) goth.ValueFormatY = formatstr;
        if (whicho == 2 || whicho == 3) goth.ValueFormatZ = formatstr;
        goth.ForceRedraw();
        break;
      }  
      case 250: // SHOWHAIRLINES(scalar GraphID, scalar ShowWhatForX[, scalar ShowWhatForY [, scalar ShowWhatForZ ] ]. VOID.
       // ShowWhatForN (rounded): 0 (or neg.): show nothing;  1: just show a 5-pixel stub on the axis;  >=2: show a full-length hairline.
       // Default is 2. No errors raised; if the graph does not exist, simply nothing happens. NB - this fn. does not suppress scale
       // value strings, which are still regularly placed where hairlines ought to be.
      { int graphid = (int) Args[0].X;
        Graph carafe;
        byte bx = 2, by = 2, bz = 2;
        double d = Math.Round(Args[1].X);
        if (d <= 0.0) bx = 0;   else if (d == 1.0) bx = 1; // else it remains as 2.
        if (NoArgs > 2)
        { d = Math.Round(Args[2].X);
          if (d <= 0.0) by = 0;   else if (d == 1.0) by = 1; // else it remains as 2.
        }
        if (NoArgs > 3)
        { d = Math.Round(Args[3].X);
          if (d <= 0.0) bz = 0;   else if (d == 1.0) bz = 1; // else it remains as 2.
        }
        Board.GetBoardOfGraph(graphid, out carafe);   if (carafe == null) break;
        carafe.HairsXVisible = new byte[] { bx };   carafe.HairsYVisible = new byte[] { by };  carafe.HairsZVisible = new byte[] { bz };
        carafe.ForceRedraw();
        break;
      }
      case 251: // SCALEFIT(values in any form: LoValue, HiValue [, LoNoSegments [, HiNoSegments] ])
      // The internal function called depends on whether there are two or more than two arguments.
      // Both cases return an array of length 3, consistent with use in e.g. 'gridx(.)' -- [new low value] [new high] [no. segs.].
      // Version 1 -- 2 args. This calls GCage.AdjustGraphLimits(.), which uses internal lookup tables; you have no control over
      // no. segs. (it chooses between 4 and 7), and the fit is looser (new range may be quite a bit bigger than the old). On the
      // other hand scale values are more likely to be simple and end in 5 or 0. 
      // Version 2 -- 3 or 4 args. You specify the exact no. segments (3-arg. version) or a trial segment range (starting
      // from 2 upwards). This calls. JM.ImproveRange(..). The fit is mostly quite tight (if you allow it a range of trial segments), 
      // but there is a tendency towards scales like '11,6,1,...' rather than '20, 10, 0,..' as the cost of such tightness.
      // ERRORS in arguments return an array with all zeroes; you would test [2], which would otherwise never be zero.
      { double[] outdata = new double[3];
        result.I = V.GenerateTempStoreRoom(3);
      // Pile all arg. values into List args:
        List <double> args = new List<double>();
        for (int i=0; i < NoArgs; i++){ if (Args[i].I == -1) args.Add(Args[i].X);  else args.AddRange(R.Store[Args[i].I].Data); }
        double LoVal = args[0], HiVal =args[1];
        int LoSegs = 0, HiSegs = 0;        
        bool version_2_args = true;
        if (args.Count > 2) { LoSegs = (int) args[2];   HiSegs = LoSegs;   version_2_args = false; }
        if (args.Count > 3)   HiSegs = (int) args[3]; 
        if (version_2_args)
        { Triplex trilby = Graph.RationalizedGraphLimits(LoVal, HiVal);
          outdata = new double[] { trilby.X, trilby.Y, trilby.Z };
        }
        else
        { if (LoSegs > HiSegs) break; // return the blank array set above.
          double[] doo = JM.ImproveRange(LoVal, HiVal,  LoSegs,  HiSegs);
          outdata[0] = doo[0];   outdata[1] = doo[1];   outdata[2] = doo[3];
        }
        R.Store[result.I].Data = outdata;      
        break;
      }
      case 252:  case 253: // PLOTMESH(.), PLOTMESH3D(.)
      // PLOTMESH( arr/mx   arr/mx  scal/arr scal/arr  scal/arr scal/arr scal/arr scal/arr   array    array    array   array  array   char. array )
      //          YCoords, XCoords, PtShape, PtWidth,  PtColour, LnShape, LnWidth, LnColour, PtWeight LnWeight XLnWeight Loops  Texts   FontName(s)
      // Arg.no.= no. coord. arrays +: 0        1          2        3        4        5          6        7      8         9     10        11
      // Suitable default inducer:    scal      -          -       scal      -        -         scal     scal   scal      scal   scal
      //  ('-' = no default; if it will be accessed (e.g. PtWidth when PtShape is valid), arg. must be supplied with a sensible value.
      // PLOTMESH3D( the same lot, but start off with: (arr/mx ZCoords...).
      // NB - Differences from 'plot(.)':
      //    (1) The first coordinate array (YCoords for 2D, ZCoords for 3D) must always be a MATRIX, at least 2x2. The number of rows will determine
      //         the number of FORWARD curves, and the the no. of columns the number of POINTS PER FORWARD CURVE (i.e. the no. transverse curves).
      //    (2) The second array must be EITHER a matrix of the same dimensions OR an array of length = No. Points per curve (which will internally
      //         be built up into a compatible matrix by replicating the array as its rows).
      //    (3) For 3D, the third array must be either a compatible matrix or an array of length = No. forward curves (in which case it will
      //         be internally converted to a compatible matrix by replicating the array as its columns).
      //    (4) There is the extra arg. 'XLnWeight', which applies to the transverse curves.
      //    (5) There is an extra arg. 'Loops'. The default is for no looping of forward or transverse curves. If Loops is an array of
      //         length >= 1, Loops[0] TRUE --> forward curves all loop. For length >= 2, Loops[1] --> transverse curves loop.
      // A Mesh object is created and added to MainWindow.Plots2D/3D; its ID is returned. (Use 'graph(.)' then to insert it into a graph.)
      // The rest has been copied straight from the header to fn. 'plot'...
      // SPECIFIC ARGS: PT/LN SHAPE must be a chars. array or if scalar, a unicode  value. Structure ignored; no trimming occurs.
      //  If an array with length > 1, values will be used separately in accord with PtWeight.
      // PT/LN WIDTH can be scalar or array
      //  (structure ignored). PT/LN COLOUR can be scalar or list array (representing a single colour) or a matrix (each row representing
      //  a separate colour). PT/LN WEIGHT, if present and is an array, is transferred directly to the plot's PtWeight property.
      // TEXTS is ignored if scalar, otherwise treated without regard to its structure as a chars. array. The delimiter between texts
      // (if more than one) is '|'; spaces are significant. Examples: "A" recurrently plots A; "A|B|C" plots A,B,C,A,B,C,..
      // FONT NAME(s) is either a single font name (no size) - "Sans" - or a series of font names in order of preference, sep'd by commas:
      // "Arial, Verdana, Ubuntu, Sans". (Spaces INSIDE a font name are significant.)
      {  bool is3D = (Which == 253);
      // PROCESS COORDINATE DATA
       // First VET IT (a long and laborious process)
        int slotX=0, slotY=0, slotZ=0, offset=0;
        if (is3D) { slotX = Args[2].I;  slotY = Args[1].I;  slotZ = Args[0].I;  offset = 3;}
        else      { slotX = Args[1].I;  slotY = Args[0].I;  slotZ = slotY;  offset = 2;} // The assignment of slotZ to slotY saves us
                                                                      // a lot of "if (is3D).." lines further down, with no signif. cost.
        if (slotX == -1 || slotY == -1 || slotZ == -1) return Oops(Which, "scalar coordinates are not allowed");
        StoreItem itemX = R.Store[slotX],  itemY = R.Store[slotY],  itemZ = R.Store[slotZ]; // if 2D itemY and itemZ are identical.
        int dimcntX = itemX.DimCnt,  dimcntY = itemY.DimCnt, dimcntZ = itemZ.DimCnt;
        int[] dimszX = itemX.DimSz,  dimszY = itemY.DimSz,   dimszZ = itemZ.DimSz;
        // The first arg. must be a matrix, which will give the no. forward curves and the no. points per curve:
        //   Recall that for 2D, dimcntZ and dimszZ are aliasses for ..Y.)
        if (dimcntZ != 2) return Oops(Which, "the first arg. must always be a matrix");
        int noFdCurves = dimszZ[1], noPoints = dimszZ[0];
        if (noFdCurves < 2 || noPoints < 2) return Oops(Which, "the first arg. (a matrix) must have at least 2 rows and 2 columns");
        // If the remaining coord. structure(s) is a matrix, check its size:
        if ( (dimcntX == 2 && (dimszX[1] != noFdCurves || dimszX[0] != noPoints) ) ||
             (dimcntY == 2 && (dimszY[1] != noFdCurves || dimszY[0] != noPoints) ) )  // redundant for 2D, but who cares?
        { return Oops(Which, "the first and third args. are incompatible matrices"); }
        // If the remaining coord. structure(s) is a list array, check its size:
        bool booboo = false;
        if (is3D)
        { if ( (dimcntY == 1 && dimszY[0] != noPoints) || (dimcntX == 1 && dimszX[0] != noFdCurves) ) booboo = true; }
        else // 2D
        { if (dimcntX == 1 && dimszX[0] != noPoints) booboo = true; }
        if (booboo) return Oops(Which, "the array coordinates arg. is incompatible with the initial matrix coordinates arg.");
       // CONVERT coordinate data into the jagged matrices needed by Board.cs:
        double[][] dataX = new double[noFdCurves][],   dataY = new double[noFdCurves][],   dataZ = null; // and must remain NULL, for 2D.
        if (is3D)
        { dataZ = new double[noFdCurves][];
          double[] dudu = null, valuesX = itemX.Data;   double x=0.0;
          for (int i=0; i < noFdCurves; i++)
          { dataZ[i] = itemZ.Data._Copy(i*noPoints, noPoints); // dataZ always represents a matrix.
            if (dimcntY == 2) dataY[i] = itemY.Data._Copy(i*noPoints, noPoints);
            else dataY[i] = itemY.Data._Copy(0, noPoints); // dataY represents a list array of Y values which is repeated for every curve.
            if (dimcntX == 2) dataX[i] = itemX.Data._Copy(i*noPoints, noPoints);
            else // dataX represents X values which are the same for all points on any one curve.
            { x = valuesX[i];
              dudu = new double[noPoints];
              for (int j=0; j < noPoints; j++) dudu[j] = x;
              dataX[i] = dudu;
            }
          }
        }
        else // is 2D:
        { for (int i=0; i < noFdCurves; i++)
          { dataY[i] = itemY.Data._Copy(i*noPoints, noPoints); // dataY always represents a matrix.
            if (dimcntX == 2) dataX[i] = itemX.Data._Copy(i*noPoints, noPoints);
            else dataX[i] = itemX.Data._Copy(0, noPoints); // dataX represents a list array of X values which is repeated for every curve.
          }
        }
        // Now all sets of coordinates are represented by consistently-sized jagged matrices.

      // PROCESS PARAMETER ARGUMENTS:
        int longestPtArray = 1, longestLnArray = 1; // information needed for processing ptweight and lnweight later.
        char[] ptshape = null,     lnshape = null;
        double[] ptwidth = null,  lnwidth = null;
        Gdk.Color[] ptclr = null, lnclr = null; // Parsing errors below will --> black points or lines, rather than the default colour.
        int[] ptweight = null, lnweight = null, xlnweight = null; // Parsing errors below will --> as above.
        string[] texts = null;
        string fontname = "";
       // Point and line shape:
        if (NoArgs > offset)     ptshape = Plot_Shape(Args[offset].I, Args[offset].X, ref longestPtArray); // If scalar, taken as a unicode value.
        if (NoArgs > offset + 3) lnshape = Plot_Shape(Args[offset+3].I, Args[offset+3].X, ref longestLnArray); // If scalar, taken as a unicode value
       // Point and line width:
        if (NoArgs > offset + 1) ptwidth = Plot_Width(Args[offset+1].I, Args[offset+1].X, ref longestPtArray);
        if (NoArgs > offset + 4) lnwidth = Plot_Width(Args[offset+4].I, Args[offset+4].X, ref longestLnArray);
       // Point and line colour:
        if (NoArgs > offset + 2) ptclr = Plot_Colour(Args[offset+2].I, Args[offset+2].X, ref longestPtArray, JTV.Black);
        if (NoArgs > offset + 5) lnclr = Plot_Colour(Args[offset+5].I, Args[offset+5].X, ref longestLnArray, JTV.Black);
       // Point weight, forward line weight and transverse line weight:
        int slit; double[] dumdum = null;
        if (NoArgs > offset + 6) { slit = Args[offset+6].I;  if (slit >= 0) dumdum = R.Store[slit].Data; }
        if (dumdum == null) ptweight = new int[longestPtArray]; // No special need to make it this length; it just cuts down on modulos
                                                                //  in Board.cs's drawing methods; but this is of extremely little advantage.
        else ptweight = dumdum._ToIntArray();
        dumdum = null;
        if (NoArgs > offset + 7) { slit = Args[offset+7].I;  if (slit >= 0) dumdum = R.Store[slit].Data; }
        if (dumdum == null) lnweight = new int[longestLnArray];
        else lnweight = dumdum._ToIntArray();
        dumdum = null;
        if (NoArgs > offset + 8) { slit = Args[offset+8].I;  if (slit >= 0) dumdum = R.Store[slit].Data; }
        if (dumdum == null) xlnweight = new int[longestLnArray];
        else xlnweight = dumdum._ToIntArray();
       // Loops:
        bool[] loops = new bool[] {false, false};
        if (NoArgs > offset + 9)
        { slit = Args[offset+9].I;
          if (slit >= 0)
          { dumdum = R.Store[slit].Data;
            if (dumdum.Length > 0) { loops[0] = (dumdum[0] != 0.0);  if (dumdum.Length > 1) loops[1] = (dumdum[1] != 0.0); }
          }
        }
       // Texts:
        if (NoArgs > offset + 10)
        { string stroo = StoreroomToString(Args[offset+10].I); // returns "" if arg. was scalar
          if (stroo.Length > 0) texts = stroo.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
        }
        if (NoArgs > offset + 11) fontname = StoreroomToString(Args[offset+11].I);
      // PLOT IT ALL:
        Mesh mishmash = null;
        mishmash = new Mesh(dataX, dataY, dataZ, ptshape, ptwidth, ptclr, lnshape, lnwidth, lnclr);
            // If this is a 2D mesh, dataZ will be null, which signals to the constructor that it is a 2D mesh.
        mishmash.FdCloseLoop = loops[0];  mishmash.TrvCloseLoop = loops[1];
        mishmash.Texts = texts;   mishmash.FdPtWeight = ptweight;
        mishmash.FdLnWeight = lnweight;  mishmash.TrvLnWeight = xlnweight;
        if (fontname != "") mishmash.FontName = fontname;
        if (is3D) MainWindow.Plots3D.Add(mishmash);  else MainWindow.Plots2D.Add(mishmash);
        result.X = (double) mishmash.ArtID;
        break;
      }
   // case 254: // ARRAY( list of names) - has exactly the same effect as 'DIM(list of names, 1)'. See case 1.

      case 255: // FINDDUP(array Subject, bool OfEarlierValue  [, scalar FromPtr [, scalar ToPtr ] ] ) -- if bool is false, finds
      // the first value which is LATER duplicated; if true, finds the first value which is the duplicate of an EARLIER value. 
      // Returns an array of size 3: [The value duplicated; the index of instance sought; the index of the match found in the process].
      // If pointer arg(s) supplied, the examined array is inclusive of these end points. The only corrected errors: FromPtr < 0
      // (after rounding) is corrected to 0; ToPtr beyond end corrected to last element of Subject. (These are nec. to avoid array range crashes.)
      // Otherwise e.g. crossed pointers simply return [-1, -1, 0].
     {
        int inslot = Args[0].I;  if (inslot == -1) return Oops(Which, "1st. arg. must be an array");
        double[] indata = R.Store[inslot].Data;
        bool OfEarlierValue = (Args[1].X != 0.0);
        int n, inLen = indata.Length;
        int fromPtr = 0, toPtr = inLen-1;
        if (NoArgs > 2)
        { n = Convert.ToInt32(Args[2].X);
          if (n >= 0) fromPtr = n;
          if (NoArgs > 3)
          { n = Convert.ToInt32(Args[3].X);
            if (n < inLen) toPtr = n; // All other silliness will just return a no-find in what follows, so not checked for here.
          }
        }
        int dupIndex = -1, matchIndex = -1;
        double dupValue = 0.0;
        if (OfEarlierValue)
        { 
          for (int i = fromPtr+1; i <= toPtr; i++)
          { double x = indata[i];
            for (int j = fromPtr; j < i; j++)
            { if (x == indata[j])
              { dupValue = x;  dupIndex = i;  matchIndex = j;  break; }
            }
            if (dupIndex != -1) break;
          }
        }  
        else // of later value:
        { for (int i = fromPtr; i < toPtr; i++)
          { double x = indata[i];
            for (int j = i+1; j <= toPtr; j++)
            { if (x == indata[j])
              { dupValue = x;  dupIndex = i;  matchIndex = j;  break; }
            }
            if (dupIndex != -1) break;
          }
        }
        result.I = V.GenerateTempStoreRoom(3);
        R.Store[result.I].Data = new double[] { (double) dupIndex, (double) matchIndex, dupValue };
        break;
      }  
       case 256: // PERTURB( array OrigValues, scalar SD, scalar MaxDeviation ) -- Provides Gaussian values, using
        // OrigValues[i] as the mean for the Gaussian calculation of Output[i]. The output array ('Output') has the same structure
        // as OrigValues (but is a non-chars. array).
      { int inslot = Args[0].I;  if (inslot == -1) return Oops(Which, "1st. arg. must be an array");
        StoreItem sitem = R.Store[inslot];
        double[] Means = sitem.Data;
        int NoMeans = Means.Length;
        double x, mean, SD = Args[1].X;  if (SD <= 0.0) return Oops(Which, "2nd. arg. must be scalar, and greater than 0");        
        double MaxDeviation = Args[2].X;
        if (MaxDeviation <= 0) return Oops(Which, "3rd. arg. must be greater than 0");
        double[] outdata = new double[NoMeans];
        for (int i=0; i < NoMeans; i++)
        { // The loop below keeps going until a value in limits has been found; so if MaxDeviation is too restrictive, go have a coffee break...
          mean = Means[i];
          while (true)
          { x = JM.RandGauss(mean, SD, Rando);
            if (x >= mean - MaxDeviation && x <= mean + MaxDeviation) break; // If the limit window were too small, this would be a hugely delaying line.
          }
          outdata[i] = x;
        }
        result.I = V.GenerateTempStoreRoom(sitem.DimSz);
        R.Store[result.I].Data = outdata;
        break;
      }  
     case 257: // MESH( array XCuts,  array YCuts [,  array ZCuts ] ) -- ASSUMES that all arrays are in ascending sorted order,
      // with no duplications; but no tests re this ("caveat emptor" applies).
      // Notionally, XCuts holds all the X-axis cuts for some graph (the two graph extremes being included), and sim. with the other array(s).
      // If there are two arrays, a 2D graph is assumed, and you get back a matrix with two rows; row[0] holds the X coord. for every intersection
      // of hairlines in the graph (reading across the graph, starting at the bottom left), row[1] the corresponding Y coords. If there are
      // three arrays, 3D is assumed, and the same idea applies, reading from the bottom left proximal corner, reading horizontally, ending
      // up at the top right proximal corner, then moving on to the next XY plane further in. The output then has 3 rows.
      // Array sizes are irrelevant; structures are ignored.
      {
        bool is2D = (NoArgs == 2);
        int inslotX = Args[0].I,  inslotY = Args[1].I, inslotZ = -2;
        if (!is2D) inslotZ = Args[2].I;
        if (inslotX == -1  ||  inslotY == -1  ||  inslotZ == -1) return Oops(Which, "all args. must be arrays");
        double[] indataX = R.Store[inslotX].Data;
        double[] indataY = R.Store[inslotY].Data;
        double[] indataZ = null;  if (!is2D) indataZ = R.Store[inslotZ].Data;
        int inlenX = indataX.Length, inlenY = indataY.Length;
        int inlenZ = 1;  if (!is2D) inlenZ = indataZ.Length;
        // Develop a 2D grid, irresp. of whether 3D required or not.
        double[]outdataX = new double[inlenX * inlenY], outdataY = new double[inlenX * inlenY];
        int cntr = 0; // index of outdata.
        for (int iy = 0; iy < inlenY; iy++)
        { double y = indataY[iy];
          for (int ix = 0; ix < inlenX; ix++)
          { outdataX[cntr] = indataX[ix];
            outdataY[cntr] = y;
            cntr++;       
          }
        }
        int inlenXY = inlenX * inlenY;
        double[] outdataXY = new double[2 * inlenXY];
        outdataX.CopyTo(outdataXY, 0);
        outdataY.CopyTo(outdataXY, cntr);
        if (is2D)
        { result.I = V.GenerateTempStoreRoom(cntr, 2);
          R.Store[result.I].Data = outdataXY;
        }
        else
        { int inlenXYZ = inlenXY * inlenZ;
          double[] outdataXYZ = new double[3 * inlenXYZ];
          // Set up the 1st. row of the output matrix:
          for (int iz = 0; iz < inlenZ; iz++)
          { outdataX.CopyTo(outdataXYZ, iz * inlenXY); }
          // Set up the 2nd. row of the output matrix:
          for (int iz = 0; iz < inlenZ; iz++)
          { outdataY.CopyTo(outdataXYZ, inlenXYZ + iz * inlenXY);
          }
          // Add on the final row:
          cntr = 2 * inlenXYZ;
          for (int iz = 0; iz < inlenZ; iz++)
          { for (int j = 0; j < inlenXY; j++)            
            { outdataXYZ[cntr] = indataZ[iz];
              cntr++;
            }
          }
          result.I = V.GenerateTempStoreRoom(inlenXYZ, 3);
          R.Store[result.I].Data = outdataXYZ;
        }       
        break;
      }  
//#######vvvvvvvvvvvv
//########^^^^^^^^^^^
   // case 257: MESHZ(.) - see 256: MESHX.
      case 258: // __SCALAR(List of names, which must not be previously reg'd as array names)
      // **** NB: If you change the name of this function, also change the ref. to it in unit Parser,
      //               where along with 'unpack' it is preprocessed.
      { for (int arg = 0; arg < NoArgs; arg++)
        { int Fn = REFArgLocn[arg].Y,  At = REFArgLocn[arg].X;
          if (Fn < 0) return Oops(-1, "'scalar' declaration: there is a problem with a variable's name");
          int varuse = V.GetVarUse(Fn, At);
          if (varuse == 0 || varuse == 3) // Receiver is SCALAR. Unassigned scalars allowed here. (Unit Parser had set up unrecog'd. names as such.)
          { V.SetVarValue(Fn, At, 0.0);
          }
          else
          { string ss = "scalar declaration: '" + V.GetVarName(Fn, At);
            if (varuse >= 10) ss += "' was predefined as an array";
            else ss += "' was predefined as a system constant or variable";
            return Oops(-1, ss);
          }
        }
        break;
      }
      case 259: case 260:  // REPLACEROWS/-COLS(Mx, Where, NewData [, NoRowsForScalar] )
      // NB: NONVOID. They return altered copies of Mx, and do not alter the argument Mx. The remaining arguments are set up to conform
      //  to the same model as for insertrows/cols(.) and appendrows/cols(.).
      // Where: Row or col. no. in Mx, in which to insert the data. Must be in range, or raises an error. Also, if new data would extend
      //  from 'Where' to beyond the end of the Mx, an error is raised. (Fns. 'insert..' and 'append..' are more lax with 'Where'.)
      // NewData: Scalar (x): a single row/col is replaced, filled to the brim with value x. If NoRowsForScalar is present and nonzero,
      //            that number of rows is refilled with the scalar value x.
      //          List array: if its length is a multiple of Mx row/col size, the appropriate no. of rows/cols is appended/inserted.
      //            The data fills rows from left to right then downwards to the next row, and cols. from above down, then to the next col.
      //          Mx with compatible row/col size as appropriate: the whole matrix overloads onto the indicated space.
      //          Any other structure (including a wrongly size matrix or array): an error is returned.
      // NoRowsForScalar: Ignored for nonscalar 'NewData'. Must be >= 1.
      { int mxslot = Args[0].I; if (mxslot == -1) return Oops(Which, "the first arg. must be a matrix");
        int[] mxdims = R.Store[mxslot].DimSz;  
        // In what follows, 'strip' means 'row' for "-rows(.)" and 'column' for "-cols(.)".
        // stripsize will be length of rows/cols in Mx, and stripcnt their total no. in Mx, for "-rows(.) / -cols(.)":
        int stripsize = 0, stripcnt = 0;
        bool isRows = (Which == 259);
        int compatibledim = 1;  if (isRows) compatibledim = 0; // the index of .DimSz[.] which gives length of rows for 'replacerows'
                                                               // or length of columns for 'replacecols'.
        stripsize = mxdims[compatibledim];  stripcnt = mxdims[1 - compatibledim];
        double[] mxdata = R.Store[mxslot].Data,  replacemtdata = null;   int mxdatalen = mxdata.Length;
        int replacemtslot = Args[2].I; 
        int replacemtlen = 0;  bool addMx = false;
        int strips_to_alter = 0; // no. strips to append/insert/replace into Mx.
        // Set up strips_to_alter and replacemtdata, prior to the actual replacement:
        if (replacemtslot == -1) // Scalar as new data:
        { double x = Args[2].X;  
          if (NoArgs > 3) strips_to_alter = Convert.ToInt32(Args[3].X); else strips_to_alter = 1;
          if (strips_to_alter < 1) strips_to_alter = 1;
          replacemtlen = strips_to_alter * stripsize;
          replacemtdata = new double[replacemtlen];   for (int i=0; i < replacemtlen; i++) replacemtdata[i] = x;
        }       
        else // check for compatible sizes, and if a compatible matrix set 'addMx'.
        { int errno = 0;
          replacemtlen = R.Store[replacemtslot].TotSz;
          int replacemtdims = R.Store[replacemtslot].DimCnt;
          if (replacemtdims == 1) addMx = false;
          else if (replacemtdims == 2 && R.Store[replacemtslot].DimSz[compatibledim] == stripsize) addMx = true;
          else errno = 1;
          if (replacemtlen % stripsize != 0) errno = 2;  else strips_to_alter = replacemtlen / stripsize;
          if (errno != 0) return Oops(Which, "the data to insert is of incompatible size");
          replacemtdata = R.Store[replacemtslot].Data;
        }
        // Check out the insertion point argument:
        int Where = (int) Args[1].X;
        if (Where < 0 || Where >= stripcnt) return Oops(Which, "the second arg. is beyond the bounds of the input matrix");
        // Prepare the output copy of Mx:
        int outslot = V.GenerateTempStoreRoom(mxdims);    double[] outdata = R.Store[outslot].Data;
        Array.Copy(mxdata, outdata, mxdatalen);
        R.Store[outslot].IsChars = R.Store[mxslot].IsChars;
        // Overwrite rows:
        if (isRows) // no account need be taken of array structure; an input matrix or list array would be laid down in the same way.
        { int ptr = stripsize * Where; // points to where, in the output matrix, the new strip will begin.
          if (ptr + replacemtlen > mxdatalen)return Oops(Which, "the replacement data exceeds the bounds of the input matrix");
          Array.Copy(replacemtdata, 0, outdata, ptr, replacemtdata.Length); // copy the new data in.
        }
        else // Overwrite columns:  Here, structure is vitally important, so we use the boolean 'addMx' set above.
        { if ( (stripsize-1)*stripcnt + Where + strips_to_alter > mxdatalen )
          { return Oops(Which, "the replacement data exceeds the bounds of the input matrix"); }
          for (int rw=0; rw < stripsize; rw++) // shove the data in, row by row...
          { int mxoffset = rw*stripcnt;
            for (int cl = Where; cl < Where + strips_to_alter; cl++)
            { if (addMx) outdata[mxoffset+cl] = replacemtdata[rw*strips_to_alter + (cl - Where)];
              else outdata[mxoffset+cl] = replacemtdata[stripsize*(cl-Where) + rw];
            }  
          }
        }
        result.I = outslot;   break;
      }
      case 261: case 262: // CHAINROWS, -COLS (array Strip, scalar NoStrips)
      // Creates a matrix consisting of NoStrips iterations of Strip as rows / cols. Structure of Strip ignored. NoStrips must be >= 1.
      // Strip can be scalar, in which case you will end up with a column / row vector.
      { int nostrips = Convert.ToInt32(Args[1].X);
        if (nostrips < 1) return Oops(Which, "the second arg. must be 1 or greater");
        int stripslot = Args[0].I;
        double[] stripdata;
        if (stripslot == -1) {  stripdata = new double[1];  stripdata[0] = Args[0].X; }
        else stripdata = R.Store[stripslot].Data;
        int striplen = stripdata.Length;
        int outslot = -1,  outlen = striplen * nostrips;    double[] outdata = new double[outlen];
        if (Which == 261) // ROWS:
        { outslot = V.GenerateTempStoreRoom(striplen, nostrips);
          for (int i=0; i < nostrips; i++) Array.Copy(stripdata, 0, outdata, i*striplen, striplen);
        }
        else // COLUMNS:
        { outslot = V.GenerateTempStoreRoom(nostrips, striplen);
          int offset = 0;
          for (int i = 0; i < striplen; i++) // working along rows
          { offset = i*nostrips;
            for (int j=0; j < nostrips; j++) outdata[offset + j] = stripdata[i]; } // replicate the single value across the whole row.
        }
        R.Store[outslot].Data = outdata;  result.I = outslot;      
        break;
      }  
      case 263: case 264: case 265: case 266: // COPYROWS / COPYCOLS (Matrix, scalar FromStrip [, scalar NoStrips]);
      // COPYROWSTO / COPYCOLSTO (Matrix, scalar FromStrip, scalar ToStrip). Unlike the nonmatrix versions, no latitude is allowed
      //  with arguments; they must be exactly right, except that omission of 3rd. arg with the first two --> NoStrips set to include
      //  the whole rest of the matrix.
      { int sourceslot = Args[0].I, fromstrip = Convert.ToInt32(Args[1].X),  extent = -1;
        bool isRows = (Which == 263 || Which == 265),    isTo = (Which == 265 || Which == 266);
        if (sourceslot == -1 || R.Store[sourceslot].DimCnt != 2) return Oops(Which, "the first arg. must be a matrix");
        int matrixrows = R.Store[sourceslot].DimSz[1],  matrixcols = R.Store[sourceslot].DimSz[0];
        int matrixstrips;  if (isRows) matrixstrips = matrixrows; else matrixstrips = matrixcols;
        int newslot = -1;
        // Get extent for all cases:
        if (isTo) extent = Convert.ToInt32(Args[2].X) - fromstrip + 1;
        else if (NoArgs == 2) extent = matrixstrips - fromstrip;
        else extent = Convert.ToInt32(Args[2].X);
        // Silly arg. checks
        if (fromstrip < 0 || extent < 1 || fromstrip + extent > matrixstrips) return Oops(Which, "the dimensioning args. are impossible");
        double[] sourcedata = R.Store[sourceslot].Data, newdata = null;
        // Rows:
        if (isRows)
        { newslot = V.GenerateTempStoreRoom(matrixcols, extent);
          newdata = R.Store[newslot].Data;
          Array.Copy(sourcedata, fromstrip*matrixcols, newdata, 0, extent*matrixcols);
        }
        // Cols:
        else         
        { newslot = V.GenerateTempStoreRoom(extent, matrixrows);
          newdata = R.Store[newslot].Data;
          int cntr = 0;
          for (int i=0; i < matrixrows; i++)
          { int offset = i*matrixcols;
            for (int j=0; j < extent; j++)
            { newdata[cntr] = sourcedata[offset + fromstrip + j];   cntr++; } 
          }
        } 
        result.I = newslot;  break;
      }
   // case 267: COMPARETO(.) - see COMPARE(.) - 276.

      case 268: // MOUSE (GraphID [, bool ReturnHistoryMatrix ] ).
      // ARGUMENTS:
      //  GraphID - if 0 or neg, records details of any mouse clicks on any graph; if positive, only of this graph.
      //  R.H.Mx. - absent or 0 --> details of last click ret'd as list array, length 18; true --> mx of 5x15, latest click = [0].
      // RETURNED:
      //  If a list array, then as below. If a matrix,then row 0 (latest click) is as below, but for row 1 upwards the UP coords.
      //  are all zero. (Currently Board.cs does not have a stack for mouse-up events, and I can't really see much point in
      //  changing this.)
      //  Return for ERROR - i.e. GraphID > 0 and unidentifiable - does not crash, but returns with latest click row as all -1.
      // A null click has all fields 0. In particular [0] = 0 and [1] = 0, which never occur for valid clicks. If a null click is
      //  found as one of the rows in a returned matrix, higher rows will always also be null clicks.
      // Elements of a click array are:
      // (A) Flags:
      //   [0] is the graph at which the mouse button last went down. If (a) it has not yet been down, OR (b) user supplied
      //        a graph ID and it is not this one, then 0 is returned here instead.
      //       Note that if the button went down on one graph but up on another, its UP characteristics will still refer to
      //        the graph on which the button went down.
      //   [1] is the code of the last mouse button to go down (having gone down inside the Drawing Area of the graph at [0]).
      //      Codes: 0 = no button has yet been down; 1 = left button; 2 = middle button; 3 = right button. If there is no middle button,
      //      clicking both buttons together usually simulates it.
      //   [2] TRUE (1.0) if the button is still down; otherwise false (0).
      //   [3] is millisecs. that have elapsed since start of 1 AD till button down; can be measured against fn. (datetime())[9].
      //   [4] is millisecs. between button down and button up for the last time the button went up.
      //   [5] to [9] are unused for now, always 0; they are there in case I think of anything else to add in the future; also, to allow
      //       the coordinates section to start from an index ending in 0.
      // (B) User Coordinates:
      //     Note that DOWN values will only be set if the button goes down on the graph's Drawing area; but UP values will be set
      //      for the up position anywhere on the whole screen (with values properly extrapolated, if off the Drawing Area).
      //  DOWN data:
      //   [10], [11] are x and y SCALED values applying where the button went DOWN. (Always (0, 0) for 3D graphs.)
      //   [12], [13] are x and y PIXEL values applying where the button went DOWN. (Rel. to top left of Drawing Area, not of plot surface.)
      //  UP data:
      //   [14], [15] are x and y SCALED values applying where the button went UP.   (Always (0, 0) for 3D graphs.)
      //   [16], [17] are x and y PIXEL values applying where the button went UP. (Same comment re extrapolated values as above.)
      //  A running point: If graphID supplied, and you click on another graph, the only parameters that change are the UP coords and down-up time,
      //   which are all reset to 0; the rest is a history of the last click on the desired graph. (A click off all graphs changes nothing.)
      //   This behaviour may or may not be desirable; I do not currently have any inclination to do something about it.
      {
        int graphid = Convert.ToInt32(Args[0].X);
        bool specifiedGraph = (graphid > 0);
        bool returnMatrix = (NoArgs > 1 && Args[1].X != 0.0);
        if (returnMatrix) result.I = V.GenerateTempStoreRoom(18, 5);
        else result.I = V.GenerateTempStoreRoom(18);
        double[] output = R.Store[result.I].Data;
        if (specifiedGraph) // Return latest click as all -1's, if graphID supplied but unidentifiable.
        { Graph gph;   Trio dummy = Board.GetBoardOfGraph(graphid, out gph);
          if (gph == null)
          { for (int i = 0; i < 18; i++) output[i] = -1.0;
            break;
          }
        }
       // TAKE A SNAP of Board.Clicks and Board.LastUnclick, and then expand Clicks up to the full 5 rows if necessary:
        MouseRec[] allClicks = new MouseRec[5];
        Board.Clicks.CopyTo(allClicks, 0);
        MouseRec lastUnclick = Board.LastUnclick;
       // DEAL WITH THE CASE WHERE THE BUTTON IS CURRENTLY DOWN, and the graph ID is suitable
        MouseRec lastClick = allClicks[0];
        int clicksPtr = 0, outputPtr = 0; // Pointer for accessing 'allClicks' and 'output'.
        bool keyIsDown = (lastClick.MSec != 0 && lastClick.MSec > lastUnclick.MSec);
        if (keyIsDown && specifiedGraph && graphid != lastClick.GraphID) keyIsDown = false;
        if (keyIsDown)
        { output[0]  = (double) lastClick.GraphID;   output[1] = (double) lastClick.Button;
          output[2]  = 1.0;                          output[3] = (double) lastClick.MSec;
          // output[4] - the down-up time - stays 0;  output[5] to [9] are currently unused, and remain 0.
          output[10] = lastClick.X;                  output[11] = lastClick.Y;
          output[12] = lastClick.pX;                 output[13] = lastClick.pY;
          // output[14] to [17] are left 0, as a button is DOWN and we can't know its future UP position yet.
          clicksPtr = 1;  outputPtr = 18;
          if (!returnMatrix) break; // Done, if only a single click record was required.
        }
       // DEAL WITH CASE(S) WHERE THE BUTTON HAS ALREADY GONE UP
        long runStartTime = MainWindow.ThisWindow.StartTime.Ticks / 10000L; // convert 100-nanosecond units to milliseconds.
        for (int cluck = clicksPtr; cluck < 5; cluck++)
        { MouseRec thisClick = allClicks[cluck];
          if (specifiedGraph && graphid != thisClick.GraphID) continue; // Nuthin' doin', if the wrong graph
          if (thisClick.Button == 0) break; // No more valid clicks.
          if (thisClick.MSec < runStartTime) break; // Either Board.Clicks holds a click from an earlier run or this click is null.
          // We are going to deal with this click:
          output[outputPtr]  = (double) thisClick.GraphID;   output[outputPtr+1] = (double) thisClick.Button;
          output[outputPtr+3]  = (double) thisClick.MSec; // [op+2] remains 0; [op+4] is set below. [op+5] to [op+9] unused.
          output[outputPtr+10] = thisClick.X;                output[outputPtr+11] = thisClick.Y;
          output[outputPtr+12] = thisClick.pX;               output[outputPtr+13] = thisClick.pY;
          if (cluck == clicksPtr) // We only have one lastUnclick to deal with; it is not a stack.
          {
            if (lastUnclick.MSec != 0) output[outputPtr+4] = (double) (lastUnclick.MSec - thisClick.MSec);
            output[outputPtr+14] = lastUnclick.X;            output[outputPtr+15] = lastUnclick.Y;
            output[outputPtr+16] = lastUnclick.pX;           output[outputPtr+17] = lastUnclick.pY;
          }
          outputPtr += 18;
          if (!returnMatrix && outputPtr >= 18) break; // Done, if only a single click record was required.
        }
        break;
      }
      case 269: // GXMENU(graphID, chars array MainMenuItemTitle, chars array SubMenuTitles) -- NONVOID.
      // The second and third args. are treated independently; if one is improper, the other (if proper) will still be applied.
      // Submenu titles within the last argument are separated from one another by "|"; the no. of submenus will be set as 1 + no. of "|"s.
      // If the string from MainMenuItemTitle is empty after trimming, the existing title remains.
      // If SubMenuTitles produces a string array with any empty items after trimming, the whole existing submenu system remains as is.
      //  otherwise the no. submenus will be the length of the string array, and its strings will become the submenu titles.
      // The Extra menu and all submenus will be visible; if you don't like this, you have to apply function GXVISIBLE to change it.
      // If the graph is unidentified,ERRORS do not raise a message, but return FALSE (and nothing happens to menus).
      // RETURNS 'false' only if the board is not identified.
      { int graphid = (int) Args[0].X;  Graph gruffalo;
        Trio truffles = Board.GetBoardOfGraph(graphid, out gruffalo);  if (gruffalo == null) break; // No error; simply returns 0, if no graph identified.
        int boardid = truffles.X;
        string maintitle = StoreroomToString(Args[1].I, true, true);
        string subtitlery = StoreroomToString(Args[2].I, true, true);
        string[] subtitles = subtitlery.Split(new string[] {"|"}, StringSplitOptions.None);
        Board.SetExtraMenuTitles(boardid, maintitle, subtitles, true, null); // Does all the testing, and only adjusts menus if args. are proper.
        result.X = 1.0;  break;
      }
      case 270: // GXVISIBLE(graphID [, any no. of scalars or arrays]). NONVOID; always returns the current visibility of extra menus
      // as an array; [0] = visibility of main menu item; [1]+ = visibility of submenus (so that the array's length is 1 + no. of submenus).
      // If no values after graphID, simply returns this. If values, uses them as booleans to set visibilities, starting from the main
      // menu item and proceeding to the submenus. Shortfall: remaining submenus retain their former visibility. Excess: ignored.
      // If no error, the minimum return length is always 2, as it is not possible to reduce no. submenus below 1. If graphID not identified,
      // returns array of size 1, value -1.
     {  double[] outdata;
        int graphid = (int) Args[0].X;  Graph gruffalo;
        Trio truffles = Board.GetBoardOfGraph(graphid, out gruffalo);
        int boardid = truffles.X;
        if (gruffalo == null) outdata = new double[] { -1.0 };
        else
        { bool mainvis;
          bool[] subvis = null;
          // set visibilities, if more than one arg:
          if (NoArgs > 1)
          { double[] indata = AccumulateValuesFromArgs(Args, 1);
            int inlen = indata.Length; // must be at least 1.
            mainvis = (indata[0] != 0.0);
            if (inlen > 1)
            { subvis = new bool[inlen-1];
              for (int i=0; i < inlen-1; i++) subvis[i] = (indata[i+1] != 0.0);
            }
            Board.SetExtraMenuVisibilities(boardid, mainvis, subvis);
          }
          // Get visibilities:
          Board.GetExtraMenuVisibilities(boardid, out mainvis, out subvis);
          int sublen = subvis.Length;
          outdata = new double[1 + sublen];
          if (mainvis) outdata[0] = 1.0;
          for (int i=0; i < sublen; i++) { if (subvis[i]) outdata[i+1] = 1.0; }
        }
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        break;
      }
      case 271: // GXCLICK(GraphID) -- If a submenu clicked, returns the index of that submenu AND resets the click flag in the graph board.
                // (The click flag will not be reset if the click was not for this board.) 
               // If no click since startup or since the last reset, returns -1. If board can't be identified, returns -2.
      { int graphid = (int) Args[0].X;  Graph gruffalo;
        Trio truffles = Board.GetBoardOfGraph(graphid, out gruffalo);  if (gruffalo == null) { result.X = -2.0;  break; }
        int boardid = truffles.X;
        Duo duey = Board.ExtraSubMenuClicked; // However many graphs there are, the output is always to this same static property of Board.
        if (duey.X == boardid)
        { result.X = (double) duey.Y;
          Board.ExtraSubMenuClicked = new Duo(0,-1);
        }
        else result.X = -1.0; // no clicks on this board lately (or else a click on another board's menu before this was handled).
        break;
      }

      case 272: // PAINTSHAPE(GraphID, PlotID, LineType [, LineWidth [, LineColour [,  FillColour]]] ) -- VOID.
      // Presupposes a previous shape exists; if not, does nothing and raises no error. Note that the arrays giving basic plot details (e.g.
      // corner coordinates, for a polygon) are not altered, and no new Shape object is created, thus cutting down considerably on time.
      { int graphID = (int) Args[0].X,  plotID = (int) Args[1].X;
        Graph graphite;
        Board.GetBoardOfGraph(graphID, out graphite); if (graphite == null) break; // Do nothing, if supplied graph ID not identified.
        Plot P = null;  bool found = false;
        for (int i=0; i < graphite.PlotsExtant.Count; i++)
        { P = graphite.PlotsExtant[i];  if (P.ArtID == plotID) { found = true; break; } }
        if (found && (P is Shape))
        { Shape Shh = P as Shape;
          char lnShape = '\u0000';
          int slot = Args[2].I;  bool success;
          if (slot >= 0)
          { lnShape = (StoreroomToString(slot, false, false))[0];
            Shh.LnShape = new char[] {lnShape}; // Note that LnShape is a property, so you can't just go "Shh.LnShape[0] = lnShape".
            Shh.LnShape[0] = lnShape; // There is no such thing as lnWeight for shapes, so only [0] is relevant.
          }
          if (NoArgs > 3)
          { double x = Args[3].X;
            if (x >= 0) Shh.LnWidth = new double[] {x}; // As above.
          }
          if (NoArgs > 4)
          { Gdk.Color lnClr = InterpretColourReference(Args[4], JTV.Black, out success);
            if (success) Shh.LnClrArray = new Gdk.Color[] { lnClr }; // There is no such thing as lnWeight for shapes, so only [0] is relevant.
          }
          success = false; // will be used for forcing transparency
          if (NoArgs > 5 && Args[5].I >= 0) // scalars cause transparency, as not with LineColour
          { Gdk.Color fillClr = InterpretColourReference(Args[5], JTV.White, out success);
            if (success) Shh.FillClr = fillClr;
          }
          if (!success) Shh.FillShape = false;
          graphite.ForceRedraw();
        }
        break;
      }

//        // Process those arguments needing further work:
//        string shapeStr = StoreroomToString(shapeSlot, true, true);  if (shapeStr == "") break;
//        char shapeType = ' ';
//        if      (shapeStr == "polygon") shapeType = 'P';  else if (shapeStr == "rectangle") shapeType = 'R';
//        else if (shapeStr == "ellipse") shapeType = 'E';  else if (shapeStr == "arc") shapeType = 'A';
//        else break;
//        if (XSlot == -1 || YSlot == -1 || lnTypeSlot == -1) break;
//        double[] XX = R.Store[XSlot].Data,  YY = R.Store[YSlot].Data;  if (XX.Length != YY.Length) break;
//        char lnShape = (StoreroomToString(lnTypeSlot, false, false))[0];
//        bool success;
//        Gdk.Color lnClr = InterpretColourReference(Args[5], JTV.White, out success);
//        if (!success) lnClr = Plot.DefLnClr;
//        Gdk.Color fillClr = InterpretColourReference(Args[6], JTV.White, out success);
//        Shape ship = new Shape(shapeType, XX, YY, new char[] {lnShape}, new double[] {lnWidth}, new Gdk.Color[] {lnClr}, fillClr);


      case 273: // MXDIAG( SQUARE Matrix Mx [, Array DoWhat, Scalar or Array Operand]) // Does things to a copy of the main diagonal of Mx.
      // One arg. version simply returns the main diagonal as a LIST ARRAY. 3 arg. version returns a MATRIX based on Mx.
      // Only first char. of DoWhat is checked. Values of this: '=' replace main diagonal with (scalar or array) Operand; 
      // '*', '/' = multiply / divide by SCALAR operand; '+', '-' = add / subtract (scalar or array) operand.
      // If Operand is an array, its structure is ignored but its absolute size must be the same as side of Mx.
      {
        int outslot = -1, inslot = Args[0].I, mxside = 0, n;     bool booboo = false;
        if (inslot == -1) booboo = true;
        else
        { n = R.Store[inslot].DimCnt;
          if (n != 2) booboo = true;
          else 
          { mxside = R.Store[inslot].DimSz[0];
            if (mxside != R.Store[inslot].DimSz[1]) booboo = true;
        } }
        if (booboo) return Oops(Which, "the first arg. must be a square matrix");
        double[] indata = R.Store[inslot].Data;
        double d;  double[] maindiag = new double[mxside];   
        n = mxside+1;
        for (int i=0; i < mxside; i++) { maindiag[i] = indata[i*n]; }
        if (NoArgs == 1) // Simply return the main diagonal, as a list array:
        { outslot = V.GenerateTempStoreRoom(mxside);
          Array.Copy(maindiag, R.Store[outslot].Data, mxside);
        }
        else if (NoArgs != 3) return Oops(Which, "either one or three args. should be supplied");
        else // fiddle the main diagonal, in a copy of the original matrix:
        { n = Args[1].I;  
          if (n == -1) return Oops(Which, "the second arg. must be an array holding the operator");
          d = R.Store[n].Data[0];   char Opern = '?';
          if (d == 61) Opern = '=';  else if (d == 43) Opern = '+'; else if (d == 45) Opern = '-';
          else if (d == 42) Opern = '*';  else if (d == 47) Opern = '/';
          else return Oops(Which, "the operator (second arg.) is unrecognized");
          // Set up new diagonal array:
          double[] newdiag = null;
          n = Args[2].I;
          if (n == -1) 
          { newdiag = new double[mxside];  d = Args[2].X;  for (int i=0; i < mxside; i++) newdiag[i] = d; }
          else newdiag = R.Store[n].Data;// NB! Don't allow 'newdiag' to be altered anywhere here, or else the input argument variable will be altered!
          if (newdiag.Length != mxside) return Oops(Which, "the third arg. is not size-compatible with the matrix");
          // Set up the copy of the matrix for the output: 
          outslot = V.GenerateTempStoreRoom(mxside, mxside);
          double[] outdata = new double[mxside * mxside];
          Array.Copy(indata, outdata, mxside * mxside);                            
          R.Store[outslot].Data = outdata;
          // Do the operation:
          if      (Opern == '=') { maindiag = newdiag; }
          else if (Opern == '+') { for (int i=0; i < mxside; i++) maindiag[i] += newdiag[i]; }
          else if (Opern == '-') { for (int i=0; i < mxside; i++) maindiag[i] -= newdiag[i]; }
          else if (Opern == '*') { for (int i=0; i < mxside; i++) maindiag[i] *= newdiag[i]; }
          else if (Opern == '/') 
          { for (int i=0; i < mxside; i++) 
            { d = newdiag[i]; if (d == 0.0) return Oops(Which, "attempt to divide by zero");
              maindiag[i] /= d; 
          } }
          // Implant maindiag:
          n = mxside+1;
          for (int i=0; i < mxside; i++) { outdata[i*n] = maindiag[i]; }
        }
        result.I = outslot;  break;
      }

      case 274: case 275: // UNUSED
      {

        break;
      }
//      case 274: case 275: // FINDROW( Matrix,  array Row [, scalar FromRowNo [, scalar ToRowNo [, scalar ExceptRowNo ]]]);
//      //                     FINDCOL( Matrix,  array Col [, scalar FromColNo [, scalar ToColNo [, scalar ExceptColNo ]]]);
//      // Explained for FINDROW:
//      // Look for array Row in the matrix; return -1 (no find) or the row no. The fifth arg. is useful where you are looking
//      // for any duplication of a given row (so want that given row excluded). FromRowNo and ToRowNo will be adusted to 
//      // matrix limits if negative, or if ToRowNo exceeds end of matrix; e.g. use -1 if you want to supply ExceptRowNo for a complete mx. 
//      // No error raised for crossed pointers; simply no find.
//      { bool isrows = true;   string watzit = "row";    if (Which == 275) { isrows = false;  watzit = "column"; }
//        int mxslot = Args[0].I, rowlen = 0, collen = 0, n;     bool booboo = false;
//        if (mxslot == -1) booboo = true;
//        else
//        { n = R.Store[mxslot].DimCnt;
//          if (n != 2) booboo = true;
//          else 
//          { rowlen = R.Store[mxslot].DimSz[0];   collen = R.Store[mxslot].DimSz[1];
//        } }
//        if (booboo) return Oops(Which, "the first arg. must be a matrix");
//        int soughtslot = Args[1].I;   
//        double[] mxdata = R.Store[mxslot].Data, soughtrow = null, soughtcol = null;
//        if (soughtslot == -1) booboo = true;
//        else if (isrows){ soughtrow = R.Store[soughtslot].Data;  if (soughtrow.Length != rowlen) booboo = true; }
//        else { soughtcol = R.Store[soughtslot].Data; if (soughtcol.Length != collen) booboo = true; }
//        if (booboo) return Oops(Which, "The second arg. must be an array with length = the length of a {0} in the matrix", watzit);
//        int foundat = -1;
//        if (isrows)
//        { // Adjust arguments:
//          int startrow = 0, endrow = collen-1, excluderow = -1;
//          if (NoArgs > 2) { startrow = (int) Args[2].X;  if (startrow < 0) startrow = 0; }
//          if (NoArgs > 3) { endrow   = (int) Args[3].X;  if (endrow < 0 || endrow >= collen) endrow = collen-1; }
//          if (NoArgs > 4) excluderow = (int) Args[4].X;
//          // Search:
//          int offset; // value of foundat critical, as the loop below may not be entered (silly args.)
//          for (int i = startrow; i <= endrow; i++)
//          { if (i == excluderow) continue;
//            offset = i*rowlen;  foundat = i;
//            for (int j = 0; j < rowlen; j++)
//            { if (soughtrow[j] != mxdata[offset + j]) { foundat = -1;  break; } }
//            if (foundat != -1) break;
//          }
//        }
//        else // columns:
//        { int startcol = 0, endcol = rowlen - 1,   excludecol = -1;
//          if (NoArgs > 2) { startcol = (int)Args[2].X; if (startcol < 0) startcol = 0; }
//          if (NoArgs > 3) { endcol = (int)Args[3].X; if (endcol < 0 || endcol >= collen) endcol = collen - 1; }
//          if (NoArgs > 4) excludecol = (int)Args[4].X;
//          // Search:
//          for (int i = startcol; i <= endcol; i++)
//          { if (i == excludecol) continue;
//            foundat = i;
//            for (int j = 0; j < collen; j++)
//            { if (soughtcol[j] != mxdata[j*rowlen + i]) { foundat = -1;  break; } }
//            if (foundat != -1) break;
//          }
//        }  
//        result.X = foundat;  break;
//      }  




      case 276:  // COMPARE(Array1, Array2 [, scalar cutoff [, scalar FromPtr [, scalar Extent]]]).
      case 267: // COMPARETO(  "      "      "      "      "      "      "      scalar ToPtr ) -- ALL arguments must be supplied.
      // Compare two arrays, not necessarily of equal size, or segments of them. If an extent is defined (by the last two arguments),
      //  it must be an extent present in both arrays or else an error will be raised. (No adjustment of out-of-range args. occurs.)
      // NB: For returned values indexed below, note that: (1) [0] is independent of any specified extent; (2)If no extent parameters supplied,
      //  and arrays are of different lengths, then [1] to [8] refer only to their common extent. And (3) if an extent is specified,
      //  values [1] to [8] give absolute addresses, not addresses relative to the start of the extent.
      // No account is taken of structuring of arrays.
      // Returned: ('divergence' at n = Array1[n] - Array2[n])
      // [0] = Array1.Length - Array2.Length;
      // [1] is the first divergence (if 'cutoff', the first divergence greater in abs. value than absolute(cutoff),
      // [2] is the last divergence. (Both are -1, if no divergence found.)
      // [3] is the max. pos. divergence,
      // [4] the max. neg. divergence, [5] the max. absolute divergence,
      // [6] is the average divergence;
      // [7] is the average |divergence|;
      // [8] is the RMS of divergences. Note that all parameters only take into
      //  consideration |differences| above 'cutoff'; below, considered as zero.
      { int slot1 = Args[0].I,  slot2 = Args[1].I, n = 0;
        if (slot1 == -1 || slot1 == -1) n = 1;
        int len1 = (int) R.Store[slot1].TotSz, len2 = (int)R.Store[slot2].TotSz;
        int lenmin = Math.Min(len1, len2);
        for (int i=2; i < NoArgs; i++) { if (Args[i].I != -1) n = 2; }
        if (n > 0) return Oops(Which, "the 1st. two args. must be arrays, and the rest scalar");
        int newslot = V.GenerateTempStoreRoom(9);
        // Get and check the scalar arguments:
        double negligible = 0.0;  if (NoArgs > 2)negligible = Math.Abs(Args[2].X);
        int fromptr = 0;          if (NoArgs > 3) fromptr = Convert.ToInt32(Args[3].X);
        int toptr = lenmin-1;     if (NoArgs > 4) { toptr = Convert.ToInt32(Args[4].X); if (Which == 276) toptr += fromptr - 1; }
        if (toptr < 0 || toptr >= lenmin || fromptr > lenmin || fromptr > toptr) return Oops(Which, "extent specifiers are not legitimate");
        // Start the output statistics
        R.Store[newslot].Data[0] = (double) len1 - len2;
        int firstdiv = -1, lastdiv = -1;
        double diff = 0.0, absdiff = 0.0;
        double maxpos = 0.0, maxneg = 0.0, maxabs = 0.0;
        double avdiv = 0.0, absdiv = 0.0, rmsdiv = 0.0;
        int len = toptr - fromptr + 1;
        double[] slot1data = R.Store[slot1].Data,  slot2data = R.Store[slot2].Data;
        for (int i = fromptr; i <= toptr; i++)
        { diff = slot1data[i] - slot2data[i];
          absdiff = Math.Abs(diff);
          if (diff != 0.0 && absdiff > negligible)//double condition nec, as negligibl can be 0.
          { if (firstdiv == -1)firstdiv = i;   lastdiv = i;
            if (diff > maxpos) maxpos = diff;
            if (diff < maxneg) maxneg = diff;
            if (absdiff > maxabs) maxabs = absdiff;
            avdiv += diff;  absdiv += absdiff;   rmsdiv += diff*diff;
        } }
        avdiv /= (double) len;   absdiv /= (double) len;
        rmsdiv = Math.Sqrt(rmsdiv / (double) len);
        //                                        0           1         2         3       4         5        6        7        8
        R.Store[newslot].Data = new double[] {len1 - len2, firstdiv,  lastdiv,  maxpos,  maxneg,  maxabs,  avdiv,  absdiv,  rmsdiv};
        result.I = newslot;  break;
      }
      case 277:  case 278: // (two equivalent names:) STR or VALUETOSTRING (array or scalar Value [, char. array FormatHow ] )
      //  Converts the numerical value to an array of chars., the C# '.ToString()' version; in the case of an array,
      //  the separator between number strings is ", ". If a 2nd. argument supplied, it is passed to C# without checks. If C#
      //  chooses to ignore it, or to return rubbish - usually just 'formatHow' itself - or in some cases to crash MonoMaths, be it upon the user's head!
      { string ss="", formatHow = "";
        if (NoArgs > 1) formatHow = StoreroomToString(Args[1].I, 0, -1, true, true, true);
        if (Args[0].I == -1) // a scalar argument:
        { ss = Args[0].X.ToString(formatHow); }
        else // an array:
        { ss = R.Store[Args[0].I].Data._ToString(", ", "", formatHow); }
        int slot = V.GenerateTempStoreRoom(ss.Length);
        double[] slotdata = R.Store[slot].Data;
        for (int i = 0; i < ss.Length; i++)
        { slotdata[i] = (double) ss[i]; }
        R.Store[slot].IsChars = true;  result.I = slot;  break;
      }
      case 279: // LIST_CULL(list no., <any no. of arrays and scalars holding values to remove from the list>). Returns
      // final size of the list.
      { int listno = (int)Args[0].X;
        if (listno < 0 || listno >= NoSysLists) return Oops(Which, "there is no list {0}", listno);
        List<double>outvals = new List<double>();
        for (int var = 1; var < NoArgs; var++)
        { if (Args[var].I == -1) outvals.Add(Args[var].X);
          else outvals.AddRange(R.Store[Args[var].I].Data);
        }
        List<double>newlist = new List<double>();
        foreach (double x in Sys[listno].LIST)
        { int n = outvals.IndexOf(x);   if (n == -1) newlist.Add(x); }
        Sys[listno].LIST.Clear();
        Sys[listno].LIST.AddRange(newlist);
        result.X = Sys[listno].LIST.Count;
        break;
      }  
      case 280: // LIST_CULL_RANGE(list no., Limit1, Limit2). Removes any value in list between the limits inclusive.
      // Order of last two args. irrelevant.
      { int listno = (int)Args[0].X;
        if (listno < 0 || listno >= NoSysLists) return Oops(Which, "there is no list {0}", listno);
        double loval = Args[1].X,  hival = Args[2].X; // no check for scalarity or for impossible arguments.
        if (hival < loval) { double x = loval; loval = hival; hival = x; }
        List<double> newlist = new List<double>();
        foreach (double x in Sys[listno].LIST)
        { if (x < loval || x > hival) newlist.Add(x); }
        Sys[listno].LIST.Clear();
        Sys[listno].LIST.AddRange(newlist);
        result.X = Sys[listno].LIST.Count;
        break;
      }  
      case 281: // LIST_FIND(list index, ...) Two versions:
      // Version 1: LIST_FIND(list no., scalars LoValue [, HiValue] ) -- Find the first value in the list which = LoValue (if 2 args.) or
      //   lies between LoValue and HiValue inclusive. (You can get Lo- and HiValue in the wrong order - the code will simply adjust, if wrong.)
      // Version 2: LIST_FIND(list no., array Sequence) -- Find the subarray Sequence within the list.
      { int listno = (int)Args[0].X;
        if (listno < 0 || listno >= NoSysLists) return Oops(Which, "there is no list {0}", listno);
        int seekSlot = Args[1].I;
        if (NoArgs == 2 && seekSlot >= 0)
        { double[] seekThis = R.Store[seekSlot].Data;
          double[] listContent = Sys[listno].LIST.ToArray();
          int n = listContent._Find(seekThis);
          result.X = (double) n;
        }
        else
        { double LoVal = Args[1].X,  HiVal = LoVal;  if (NoArgs > 2) HiVal = Args[2].X;
          if (HiVal < LoVal) { double x = LoVal;  LoVal = HiVal;  HiVal = x; }
          result.X = -1.0;
          for (int i=0; i < Sys[listno].LIST.Count; i++)
          { double x = Sys[listno].LIST[i];  if (x >= LoVal && x <= HiVal) { result.X = (double) i;  break; }
          }
        }
        break;
      }
      case 282: // MXHALF. Two modes: (A) is for reading a half-matrix triangle, (B) is for writing one.
      // (A) READING: mxhalf(SqMatrix, array WhichHalf) - exactly two arguments. RETURNS a list array (the data of the triangle).
      // (B) WRITING: mxhalf(SqMatrix, array WhichHalf, array OtherHalfAction, (single array) Data [, array/scalar MainDiagonal ]).
      //               RETURNS A MATRIX: a copy of SqMatrix, altered as specified.
      // SqMatrix: Must be square, and must be at least 2x2.
      // WhichHalf: 'L' (lower triangle), 'U' (upper triangle). This is the half that will be either read from or written to.
      // OtherHalfAction: What to do with the other triangle. " " (SPACE) = leave the thing alone; "C" = copy (all Mx[i,j] = Mx[j,i]);
      //    "N" = copy and change sign (all Mx[i,j] = - Mx[j,i]). SCALAR VALUE: this will fill the opposite triangle (typically would be 0).
      //    I have deliberately avoided using arith. signs, as in the future I may add e.g. "add this triangle to the other triangle").
      // Data: A single array; it must contain exactly the right amount of data; i.e. for NxN mx, length = (N^2-N) / 2 .
      // MainDiagonal: If omitted, leave it as is. If scalar, fill with that value. If array, must have exactly the row length, or crasho.
      {
        int mxslot = Args[0].I;  if (mxslot == -1) return Oops(Which, "1st. arg. must be a matrix");
        StoreItem mxitem = R.Store[mxslot];
        int[]dimmo = mxitem.DimSz;
        int norows = dimmo[0];
        if (norows < 2 || dimmo[1] != norows || dimmo[2] != 0) return Oops(Which, "1st. arg. must be a square matrix, at least 2x2");
        int trisize = (norows*norows - norows) / 2; // size of a triangle from the matrix
        int whichhalfslot = Args[1].I;  if (whichhalfslot == -1) return Oops(Which, "2nd. arg. must be an array");
        double x = R.Store[whichhalfslot].Data[0];
        bool IsLowerHalf = (x == 76.0);  // unicode 76 is 'L', 85 is 'U'.
        if (!IsLowerHalf && x != 85.0) return Oops(Which, "2nd. arg. must be an array beginning with 'L' or 'R'");
        int lorow = 0;  if (IsLowerHalf) lorow = 1; // used in both READ and WRITE loops.
        int count;
        // Separate the READ and the WRITE versions of this function:
        if (NoArgs == 2) // then this is the READ version:
        { double[] income = mxitem.Data;
          double[] outflow = new double[trisize];
          count = 0;
          for (int i = lorow; i < norows; i++)
          { for (int j = 0; j < norows; j++)
            { if (IsLowerHalf)
              { if (j == i) break; // have hit the main diagonal
                outflow[count] = income[i*norows+j];
              }
              else // upper half
              { if (j <= i) continue; // ignore, if below the main diagonal
                outflow[count] = income[i*norows+j];
              }
              count++;
            }
          }
          result.I = V.GenerateTempStoreRoom(trisize);
          R.Store[result.I].Data = outflow;
          break;
        }
        // The WRITE version:
        if (NoArgs < 4) return Oops(Which, "the data-writing version of this fn. requires at least 4 args.");
        int otherhalfslot = Args[2].I; if (otherhalfslot == -1) return Oops(Which, "3rd. arg. must be an array");
        bool LeaveOtherHalf = false, CopyNegative = false;
        x = R.Store[otherhalfslot].Data[0];
        if (x != 32.0 && x != 67.0 && x != 78.0) return Oops(Which, "code unrecognized in 3rd. arg.");
        LeaveOtherHalf = (x == 32.0);
        CopyNegative = (x == 78.0);
        int indataslot = Args[3].I;  if (indataslot == -1) return Oops(Which, "4th. arg. must be an array");
        double[] indata = R.Store[indataslot].Data;
        if (indata.Length != trisize)
        { return Oops(Which, "a square matrix of side {0} requires {1} data values; you have supplied {2} values", norows, trisize, indata.Length); }
        // FILL THE DESIGNATED TRIANGLE
        double[] outdata = mxitem.Data._Copy();
        count = 0;
        for (int i = lorow; i < norows; i++)
        { for (int j = 0; j < norows; j++)
          { if (IsLowerHalf)
            { if (j == i) break; // have hit the main diagonal
              outdata[i*norows+j] = indata[count];
            }
            else // upper half
            { if (j <= i) continue; // ignore, if below the main diagonal
              outdata[i*norows+j] = indata[count];
            }
            count++;
          }
        }
        // DO SOMETHING TO THE OPPOSITE TRIANGLE, if required
        if (!LeaveOtherHalf)
        { double sign = 1.0;  if (CopyNegative) sign = -1.0;

          for (int i = lorow; i < norows; i++)
          { for (int j = 0; j < norows; j++)
            { if (!IsLowerHalf) // then we are setting the lower half from the upper:
              { if (j == i) break; // have hit the main diagonal
                outdata[i*norows+j] = sign * outdata[j*norows+i];
              }
              else // filling upper half from the lower:
              { if (j <= i) continue; // ignore, if below the main diagonal
                outdata[i*norows+j] = sign * outdata[j*norows+i];
              }
              count++;
            }
          }
        }
        // MAIN DIAGONAL
        if (NoArgs > 4)
        { int diagslot = Args[4].I;
          if (diagslot == -1)
          { x = Args[4].X;
            for (int i=0; i < norows; i++) outdata[i*norows+i] = x;
          }
          else
          { double[] diagdata = R.Store[diagslot].Data;
            if (diagdata.Length != norows) return Oops(Which, "5th. arg., if an array, must have length = no. rows of matrix");
            for (int i=0; i < norows; i++) { outdata[i*norows+i] = diagdata[i]; }
          }
        }
        // DONE!
        result.I = V.GenerateTempStoreRoom(norows, norows);
        R.Store[result.I].Data = outdata;
        break;
      }

      case 283: // OFFSET (array Struc,  list-array / matrix IndexAddress ) -- converts address(es) in indexed form ("[2,3,4]") to absolute
        // address(e)s, i.e. to offsets within the structure's data strip.
        // Struc: EITHER a 2D or higher-dimensional structure [currently only 2D and 3D allowed in MonoMaths] OR a list aray which states
        //   the dimensions of a 2D or higher structure (ANY no. of dimensions), starting from the highest. (E.g. for a 2x4 matrix, Struc
        //   would either be that matrix, or would be the array [2, 4].) *** If structures of higher dims. ever allowed in MonoMaths, no
        //   changes will be needed to this function.
        // IndexAddress: If a list array, must have length = no dims. of Struc; the return will be a scalar (absolute address, i.e. offset).
        // If a matrix, with no. columns = no. dims. of Struc, will return a list array with one element ( = offset) per row of Struc.
        // Note that address in IndexAddress should be in the order of a "2 x 3 x 4" type dimensional statement: [][0] = Highest dimension.
        // ERRORS: IndexAddresses must be dimensionally compatible with Struc; Struc must not be a list array; index addresses must be in range.
      {
        int strucslot = Args[0].I, indexslot = Args[1].I;
        if (strucslot == -1  ||  indexslot == -1) return Oops(Which, "no scalar args. allowed");
        StoreItem indexIt = R.Store[indexslot];
        double[] indexdata = indexIt.Data;
        // Case where 'Struc' is a structure:
        int[] strucdims = R.Store[strucslot].DimSizes;
        int nostrucdims = strucdims.Length;
        // Case where 'Struc' is a list array of dimensions, starting from highest:
        if (nostrucdims == 1)
        { double[] strucdata = R.Store[strucslot].Data;
          nostrucdims = strucdata.Length;
          strucdims = new int[nostrucdims];
          for (int i=0; i < nostrucdims; i++)
          { int p = Convert.ToInt32(strucdata[i]);
            if (p < 1) return Oops(Which, "as the 1st. arg. is a list array it should hold dimension sizes, all >= 1");
            strucdims[nostrucdims - i - 1] = p;
          }
        }
        int[] indexdims = indexIt.DimSizes;
        int noindexdims = indexdims.Length;
        if (indexdims[0] != nostrucdims) return Oops(Which, "array args. are not compatible in size");
        int no_addresses = 1;
        if (noindexdims > 1) no_addresses = indexdims[1];
        double[] outdata = new double[no_addresses];
        for (int i=0; i < no_addresses; i++)
        { int rowoffset = i * nostrucdims;
          int n = 0, addr = 0, factor = 1;
          for (int j=0; j < nostrucdims; j++) // from lowest dimension.
          { 
            n = Convert.ToInt32(indexdata[rowoffset + nostrucdims-1-j]); // the index address for this dimension (present in reverse order)
            if (n < 0 || n >= strucdims[j])
            { return Oops(Which, "the {0}th. index in the {1}th. row of the 2nd. arg. is out of range", nostrucdims-j, i+1); }
            addr += n * factor;
            factor *= strucdims[j];
          }
          outdata[i] = (double) addr;
        }     
        // If IndexAddress was a list array, return a scalar:
        if (noindexdims == 1) { result.X = outdata[0];  break; }
        result.I = V.GenerateTempStoreRoom(no_addresses);
        R.Store[result.I].Data = outdata; 
        break;
      }
      case 284: // INDEXED (array Struc,  scalar / list-array OffsetAddress [, bool Element0HasLowestDim ) -- converts address(es) from
        //   offset form (absolute address) to an array reflecting the indexed form ("[2,3,4]" for a 2x3x4 structure), the highest
        //   dimensional address element being in element [0], UNLESS the 3rd. arg. is present and true, when [0] is the lowest dimension.
        // Struc: EITHER a 2D or higher-dimensional structure [currently only 2D and 3D allowed in MonoMaths] OR a list aray which states
        //   the dimensions of a 2D or higher structure (ANY no. of dimensions), starting from the highest. (E.g. for a 2x4 matrix, Struc
        //   would either be that matrix, or would be the array [2, 4].) *** If structures of higher dims. ever allowed in MonoMaths, no
        //   changes will be needed to this function.
        // OffsetAddress: If a scalar, causes the return of a list array. If an array (any structure), returns a matrix in which each row
        //   is an address. Impossible offset addresses crash.
      { int strucslot = Args[0].I, offsetslot = Args[1].I;
        if (strucslot == -1) return Oops(Which, "1st. arg. must be an array");
        double[] offsetdata;
        if (offsetslot == -1) offsetdata = new double[] { Args[1].X };
        else offsetdata = R.Store[offsetslot].Data;
        int offsetlen = offsetdata.Length;
        // Case where 'Struc' is a structure:
        int[] strucdims = R.Store[strucslot].DimSizes;
        int nostrucdims = strucdims.Length;
        // Case where 'Struc' is a list array of dimensions, starting from highest:
        if (nostrucdims == 1)
        { double[] strucdata = R.Store[strucslot].Data;
          nostrucdims = strucdata.Length;
          strucdims = new int[nostrucdims];
          for (int i=0; i < nostrucdims; i++)
          { int p = Convert.ToInt32(strucdata[i]);
            if (p < 1) return Oops(Which, "as the 1st. arg. is a list array it should hold dimension sizes, all >= 1");
            strucdims[nostrucdims - i - 1] = p;
          }
        }
        bool lowtolow = (NoArgs > 2 && Args[2].X != 0.0); // TRUE if user wants lowest dimension recorded in [0] of returned index address(es).
        // Cycle through the offset addresses
        double[] outdata = new double[nostrucdims * offsetlen];
        int outaddr, factor, residual;
        for (int i=0; i < offsetlen; i++)
        { 
          int Base = i * nostrucdims; // addend for 'outdata' addresses
          factor = 1;
          residual = Convert.ToInt32(offsetdata[i]);
          if (residual < 0) return Oops(Which, "a negative offset address was supplied in the 2nd. arg.");
          for (int j=0; j < nostrucdims; j++)
          { factor = strucdims[j];
            if (lowtolow) outaddr = Base+j;   else outaddr = Base + nostrucdims - j - 1;
            outdata[outaddr] = (double) (residual % factor); 
            residual = residual / factor;
          }          
          if (residual > 0)
          { int sz = 1;
            for (int k=0; k < nostrucdims; k++) sz *= strucdims[k];
            return Oops(Which, "offset address {0} exceeds size of the structure (which is {1})", offsetdata[i], sz);
          }
        }
        // The return:
        if (offsetslot == -1) result.I = V.GenerateTempStoreRoom(nostrucdims);
        else result.I = V.GenerateTempStoreRoom(nostrucdims, offsetlen);
        R.Store[result.I].Data = outdata;
        break;
      }
      case 285: // KEYED(..) -- returns TRUE if a keying (i.e. one or more keys down together) has occurred since the last call
      // to this particular function (or since startup); next call would return FALSE. Optionally returns data re the last keying(s).
      // The keying is only registered as a new keying when the keys are all back up again.
      // KEYED()  or  KEYED(scalar): Does nothing else - just returns true or false.
      // KEYED(REF array Arr) -- Arr becomes a list array representing the last keying (whether new or old), its form depending on whether
      //   'Arr' enters as a chars. or nonchars. array. If nonchars, the array is the keys of the keying, sorted in ascending order
      //   (not in the order pressed); e.g. [97, 65507] for control-A. If a chars array, returns with single Latin letters in upper
      //     case: "Cntrl Shift A".
      //    Before translation from key number to such a string, the above sorted array is reversed. In all other respects the initial
      //    structure and data content of Arr is ignored.
      //    For the standard key names returned, see JTV.KeyDenomination(.) - names as there are converted here to upper case.
      // KEYED(Ref array Arr, scalar Depth) -- If Depth rounds to <= 1, as above: the last keying is returned (whether new or old).
      //   If rounds to positive integer N, the return is a jagged matrix with N rows, padder being 0 (whatever the chars rating of Arr).
      //   Row 0 is the latest keying. If there have not been N keyings yet, excess rows will be filled with padder.
      //   Row size: 1 + the longest amount of data in any one row. Hence there 
      // If no keyings yet, Arr[0] (if list array) or Arr[0,0] (if matrix) will be 0; this is a sufficient test.
      // ERROR: NonREF Arr simply reverts to the no-argument form, with no error message; user will find Arr unaltered after the call.
      {
        int dataslot = Args[0].I;
        bool refArrayPresent = (dataslot != -1 && REFArgLocn[0].Y >= 0); // then the 1st. arg. is a REF array, so proceed:
        StoreItem It = null;
        if (refArrayPresent) It = R.Store[dataslot];
        int[] last_key_combo = MainWindow.ThisWindow.LastKeyCombo;
        int[] zeroes = last_key_combo._FindAll(0, 1); // We are only interested in zeroes after LastKeyCombo[0], which is a cue, not data.
        if (zeroes.Length == 0) // Should never happen: all of LKC filled by one mighty handful of keys down together.
        { last_key_combo[last_key_combo.Length-1] = 0;  zeroes = new int[] {last_key_combo.Length-1}; } // lop off the last key, replace with 0.
        // Set up the return at this stage (merely a boolean)
        result.X = (last_key_combo[0] == 1) ? 1.0 : 0.0;
        if (result.X == 1.0) last_key_combo[0] = -1; // Cue to the next call to this function that the keying has already been handled.
        // If the first arg. is a REF array, set it up with the keying information:
        if (refArrayPresent)
        { // Set matrix dimensions:
          bool isChars = R.Store[dataslot].IsChars;
          int rowLength = isChars ? 60 : 6;
          int setNoRows = 1; // will be the no. of rows set by the user.
          if (NoArgs > 1) { setNoRows = Convert.ToInt32(Args[1].X);  if (setNoRows < 1) setNoRows = 1; }
          int noKeyings = Math.Min(setNoRows, zeroes.Length); // the no. of keyings that will be returned.          
          int startPtr = 1, endPtr; // startPtr points to first nonzero element in a subarray of last_key_combo, endPtr to its last nonzero element.
          int theSign = (isChars) ? -1 : 1; // to sort in descending order (isChars) or ascending order (!isChars) 
          List<int> keycollection = new List<int>(32);
          string keystr;
          double[] thedata = new double[noKeyings * rowLength];
          int longest = 0; // will track the longest valid data length within a row.
          for (int i = 0; i < noKeyings; i++)
          { keycollection.Clear();
            endPtr = zeroes[i]-1;
            for (int j = startPtr; j <= endPtr; j++)
            { int n = last_key_combo[j];
              if (n >= 97 && n <= 122) n -= 32; // Convert Latin lower case to upper case
              keycollection.Add(theSign * n);
            }
            int keycnt = keycollection.Count;
            if (keycnt == 0) keycollection.Add(0);
            keycollection.Sort();
            int fillLen = 0;
            int offset = i * rowLength;
            if (isChars)
            { keystr = "";
              for (int j = 0; j < keycnt; j++)
              { keystr += JTV.KeyDenomination(-keycollection[j]);  if (j < keycnt-1) keystr += " "; }
              char[] chars = keystr.ToCharArray();              
              fillLen = Math.Min(rowLength, chars.Length);
              for (int j = 0; j < fillLen; j++) thedata[offset + j] = Convert.ToDouble( (int) chars[j]);
            }            
            else
            { fillLen = keycnt;
              for (int j = 0; j < fillLen; j++) thedata[offset + j] = Convert.ToDouble(keycollection[j]);
            }            
            if (i == 0 && setNoRows == 1)
            { thedata = thedata._Copy(0, fillLen);  break; }
            for (int j = fillLen; j < rowLength; j++) thedata[offset + j] = 0.0; // Must pad out the subarray representing each row of the mx.
            startPtr = endPtr + 2; // hop over the zero
            if (fillLen > longest) longest = fillLen;
          }
          // Change the REF array to this setup:
          int[] newDimSz = new int[TVar.MaxNoDims];
          if (setNoRows == 1)
          { It.DimCnt = 1;
            newDimSz[0] = thedata.Length;
          }
          else
          {//Trim the rows, so that the longest row has only one 0 at its end:
            int trimmedLen = longest+1;
            double[] thedata2 = new double[trimmedLen * setNoRows];
            for (int j = 0; j < setNoRows; j++)
            { Array.Copy(thedata, j*rowLength, thedata2, j*trimmedLen, longest+1); }
            It.DimCnt = 2;
            newDimSz[0] = trimmedLen;   newDimSz[1] = setNoRows;
            thedata = thedata2;
          }
          It.DimSz = newDimSz;
          It.Data = thedata;
        }
        break; // result.X has already been assigned, at the start.
      }
      case 286: // FINDINMX(Matrix, scalar / array Sought, bool FirstFindOnly [, scalar FirstRow [, scalar LastRow] ] )
      // *** CURRENTLY ONLY WORKS FOR ROWS. *** If you want to find stuff in columns, you will have to transpose the matrix. ***
      // "Sought": where this is an array, the whole array must be contained within one row for a find - no overflow into the next row.
      // RETURNED: A matrix, always with two rows: row 0 holds the row number of the find, row 1 the column number. (If FirstFindOnly TRUE,
      //   and a find occurs, this will be a 2x1 matrix.) If NO FIND: returns a 2x1 matrix with -1 as both values.
      // FirstRow and LastRow have the obvious defaults.
        { int mxslot = Args[0].I;  if (mxslot == -1) return Oops(Which, "1st. arg. must be a matrix");
        StoreItem mxitem = R.Store[mxslot];
        if (mxitem.DimCnt != 2)  return Oops(Which, "1st. arg. should be a matrix");
        int noRows = mxitem.DimSz[1], noCols = mxitem.DimSz[0];
        double[] mxdata = mxitem.Data;
        int datalen = mxdata.Length;
        bool firstOnly = (Args[2].X != 0.0);
        int firstRow = NoArgs > 3  ?  Convert.ToInt32(Args[3].X)  :  0;
        if (firstRow < 0 || firstRow >= noRows) return Oops(Which, "value of 4th. arg. is out of range");
        int lastRow  = NoArgs > 4  ?  Convert.ToInt32(Args[4].X)  :  noRows-1;
        if (lastRow < firstRow || lastRow >= noRows) return Oops(Which, "value of 5th. arg. is either out of range or exceeds the 4th. arg.");
        var ListOfRowNos = new List<double>();
        var ListOfColNos = new List<double>();
        bool isScalar = (Args[1].I == -1);
        bool breakout = false;
        if (isScalar)
        { double soughtScalar = Args[1].X;
          for (int rw = firstRow; rw <= lastRow; rw++)
          { int[] rowfinds = mxdata._FindAll(soughtScalar, rw*noCols, rw*noCols + noCols-1);
            for (int j=0; j < rowfinds.Length; j++) // not entered if no finds (as 'rowfinds' is then an empty array)
            { ListOfRowNos.Add( (double) rw);
              ListOfColNos.Add( (double) (rowfinds[j] - rw*noCols) );
              if (firstOnly) { breakout = true;  break; }
            }
            if (breakout) break;
          }
        }
        else // an array is sought:
        { double[] soughtArray = R.Store[Args[1].I].Data;
          for (int rw = firstRow; rw <= lastRow; rw++)
          { int[] rowfinds = mxdata._FindAll(soughtArray, rw*noCols, rw*noCols + noCols-1);
            for (int j=0; j < rowfinds.Length; j++) // not entered if no finds (as 'rowfinds' is then an empty array)
            { ListOfRowNos.Add( (double) rw);
              ListOfColNos.Add( (double) (rowfinds[j] - rw*noCols) );
              if (firstOnly) { breakout = true;  break; }
            }
            if (breakout) break;
          }
        } 
        double[] outdata;
        if (ListOfRowNos.Count == 0) outdata = new double[] {-1.0, -1.0};
        else
        { ListOfRowNos.AddRange(ListOfColNos);
          outdata = ListOfRowNos.ToArray();
        }
        result.I = V.GenerateTempStoreRoom(outdata.Length / 2, 2);
        R.Store[result.I].Data = outdata;
        break;
      }
      case 287: // CAP ( scalar / array Values,  scalar CriticalValue,  scalar Asymptote) -- Values below CriticalValue remain as is;
      // values above this are reduced to lie between CriticalValue and Asymptote. The closer the two values, the sharper will be the knee
      // of the curve of returned values v. input values. No error detection - GIGO.
      { 
        double E, CritValue = Args[1].X,  Asymp = Args[2].X;
        double Gap = Asymp - CritValue; // if neg, no damage - just GIGO.
        int valueslot = Args[0].I;
        if (valueslot == -1) // Do scalar case separately, to save time.
        { result.X = Args[0].X;
          if (result.X > CritValue)
          { E = Math.Exp(2 * (CritValue - result.X) / Gap);
            result.X = CritValue + Gap * (1 - E) / (1 + E);
          }
          break;
        }
        // To get here, Values must be an array:
        StoreItem inItem = R.Store[valueslot];
        var indata = inItem.Data;
        int inlen = indata.Length;
        var outdata = indata._Copy(); // Copied because we will only change values above CriticalValue.
        for (int i=0; i < inlen; i++)
        { if (indata[i] > CritValue)
          { E = Math.Exp(2 * (CritValue - indata[i]) / Gap);
            outdata[i] = CritValue + Gap * (1 - E) / (1 + E);
          }
        }
        result.I = V.GenerateTempStoreRoom(inItem.DimSz);
        R.Store[result.I].Data = outdata;
        break;
      }  
      case 288: // LIST_OPN( scalar ListNo, scalar Pointer, array Operation, scalar SecondValue [, DontAlterList] )
      // Operates on this list's [Pointer] value, using Second Value if a binary operation (ignored, for unary ops, but a dummy must go there).
      // Op.[0] must be one of: " + - * / ^ A B S " (A = Absolute value;  B = Boolean value;  S = Sign(zero --> I.V.).
      // If final arg. missing or zero, replaces the old value in the list with the new; in any case, it returns the new value.
      // Crashes if ListNo doesn't exist or if Pointer is out of range or Opn[0] unrecognized.
      { int listno = (int) Args[0].X;
        if (Sys == null || listno < 0 || listno >= NoSysLists) return Oops(Which, "list {0} does not exist", listno);
        int listlen = Sys[listno].LIST.Count,  ptr = (int) Args[1].X;
        if (ptr < 0 || ptr >= listlen) return Oops(Which, "the pointer is out of range");
        int opslot = Args[2].I; if (opslot == -1) return Oops(Which, "the third arg. must be an array");
        int n = (int) R.Store[opslot].Data[0];
        char Op = (char) n;
        double X = Sys[listno].LIST[ptr];
        double Y = Args[3].X,  Z=0;
        if (Op == '+') Z = X+Y;  else if (Op == '-') Z = X-Y;  else if (Op == '*') Z = X*Y;
        else if (Op == '/') { if (Y == 0.0) { result.S = "divn. by 0"; result.B = false; } else Z = X/Y; }
        else if (Op == '^') 
        { if ( X < 0 && Y != Math.Round(Y) ) { result.S = "improper power opn."; result.B = false; } 
          else Z = Math.Pow(X,Y); 
        }
        else if (Op == 'A' || Op == 'a') Z = Math.Abs(X);
        else if (Op == 'B' || Op == 'b') { if (X == 0.0) Z = 0.0; else Z = 1.0; }
        else if (Op == 'S' || Op == 's') { if (X > 0.0) Z = 1.0; else if (X < 0.0) Z = -1.0; else Z = Y; }
        else { result.S = "'list_opn': operation not identified"; result.B = false; return result; }
        if (!result.B) { result.S = "'list_opn': " + result.S; return result; }
        if (NoArgs > 4 && Args[4].X != 0.0) Sys[listno].LIST[ptr] = Z;
        result.X = Z;  break; 
      }
      case 289: // HSL (colour argument) -- returns the HSL value for a colour, as an array of size 3, values being 0 to 1 inclusive. 
      // See 'InterpretColourReference(.)' for the various allowed argument deployments. Failed colour parsing returns black (no error state).
      { Gdk.Color defcolour = new Gdk.Color(0,0,0); // error returns this.
        bool success;
        Gdk.Color colour = InterpretColourReference(Args[0], defcolour, out success);
        double hue, sat, lum;
        result.I = V.GenerateTempStoreRoom(3);
        JS.HSL(colour, out hue, out sat, out lum);
        R.Store[result.I].Data = new double[] {hue, sat, lum};
        break;
      }        

      case 290: // HSL_TO_RGB(array HslColours [, bool asHex, array Delimr] ) -- Whatever the structure of 'array', it must have
      // a data length which is a multiple of 3. Every triplet of values will be taken as a sequence of hue-satn-lum values,
      // which must lie between 0 and 1. (Values out of range will be trimmed back to the nearer of the two limits.)
      // If 'asHex' FALSE or absent, the returned array will have exactly the same structure as HslColours ('chars' rating false), and
      // the third arg., if present, is totally ignored (it could be scalar). Otherwise a CHARS list array of Hex values in the form
      // "0xRRGGBB" is returned, values being delimited by Delimr.
      { result.I = V.GenerateTempStoreRoom(3);
        int inslot = Args[0].I;  if (inslot == -1) break; // exit, returning an empty array.
        StoreItem stritem = R.Store[inslot];
        double[] hsl = stritem.Data;
        if (hsl.Length % 3 != 0) return Oops(Which, "The input array of colour data must have length exactly divisible by 3");
        int noClrs = hsl.Length / 3;
        bool asHex = (NoArgs > 1 && Args[1].X != 0.0);
        if (asHex && (NoArgs < 3 || Args[2].I == -1)) return Oops(Which, "either one arg. or three args. (the 3rd. being an array) are required");
        string delimr = "";  if (asHex) delimr = StoreroomToString(Args[2].I);
        byte red, green, blue;
        if (asHex)
        { string outStr = "";
          for (int i=0; i < noClrs; i++)
          { JS.HSLtoRGB(hsl[3*i], hsl[3*i+1], hsl[3*i+2], out red, out green, out blue);
            outStr += JS.ColourToHex("0x", new int[] { (int) red, (int) green, (int) blue } );
            if (i < noClrs-1) outStr += delimr;
          }
          result.I = V.GenerateTempStoreRoom(outStr.Length);
          StringToStoreroom(result.I, outStr);
        }
        else
        { double[] outdata = new double[hsl.Length];
          for (int i=0; i < noClrs; i++)
          { JS.HSLtoRGB(hsl[3*i], hsl[3*i+1], hsl[3*i+2], out red, out green, out blue);
            outdata[3*i] = (double) red;  outdata[3*i+1] = (double) green;   outdata[3*i+2] = (double) blue;
          }
          result.I = V.GenerateTempStoreRoom(stritem.DimSz);
          R.Store[result.I].Data = outdata;
        }
        break;
      }


//      case 290: // HSL_TO_RGB(array) -- If the array is of length 3, and has elements in the range 0 to 1, an array of 3 containing
//      // RGB values (red is [0]) is returned. If any error in the argument, black is returned - [0,0,0]. (Hue is low - array[0].)
//      { result.I = V.GenerateTempStoreRoom(3);
//        int inslot = Args[0].I;  if (inslot == -1) break; // exit, returning an empty array.
//        double[] hsl = R.Store[inslot].Data;  if (hsl.Length != 3) break; // ditto.
//        byte red, green, blue;
//        JS.HSLtoRGB(hsl[0], hsl[1], hsl[2], out red, out green, out blue);
//        R.Store[result.I].Data = new double[] { (double) red, (double) green, (double) blue };
//        break;
//      }


      case 291: case 292: // SCALEJUMPX, SCALEJUMPY (GraphID, scalar FirstLabelled, scalar JumpSize // 2D only.
      // Dictates which hairlines will not have tick strings printed opposite them.
      // GraphID cannot be omitted. Suppose we have SCALEJUMPX(g,0,3); then the axis intercept would be labelled, and thereafter
      // every 3rd. hairline (the right extreme would only be labelled if it happened to be a 3rd. vertical line).
      // Does not apply to any extent overwritten by 'scaleoverx/y/z(.)'.
      // Both arguments are rounded. The fn. does nothing (but no error is raised) if FirstLabelled is < 0 or JumpSize <= 0.
      // If the first argument is > no. segments, no hair lines are labelled at all.
      { int graphid = (int)Args[0].X;
        Graph gath;
        Board.GetBoardOfGraph(graphid, out gath); if (gath == null) break; // Do nothing, if supplied graph ID not identified.
        int firstvisible = Convert.ToInt32(Args[1].X);  if (firstvisible < 0) break;
        int jumpsize = Convert.ToInt32(Args[2].X);  if (jumpsize < 1) break;
        if (Which == 291) gath.TickStringsXVisible = new int[] { firstvisible, jumpsize };
        else              gath.TickStringsYVisible = new int[] { firstvisible, jumpsize };
        gath.ForceRedraw();
        break;
      }
      case 293: // SHOWARRAY(Array) -- same display as for clicking F1 with cursor on the name of an array, EXCEPT that temp. arrays allowed.
      { int slot = Args[0].I;
        if (slot == -1) return Oops(Which, "arg. must be an array");
        int Fn = REFArgLocn[0].Y,  At = REFArgLocn[0].X;
        string varName;
        if (Fn < 0) varName = "temporary array";
        else varName = V.GetVarName(Fn, At);
        MainWindow.ThisWindow.DisplayArrayWithOptions(slot, "Array", varName);
        break;
      }
      case 294: // FACTORS(any args. - which will be rounded) -- Get the factors common to all values included. (If only one value,
      // gives all its factors.)
      // Not suitable for factors beyond the size of Int32. All values are rounded; values < 2 are ignored (no message).
      // Returned: a list array of factors. For 0 or negative numbers, or for no common factors, returns an array of size 1, value NaN.
      { List<int> Values = new List<int>();
        for (int var = 0; var < NoArgs; var++)
        { if (Args[var].I == -1) Values.Add( Convert.ToInt32(Args[var].X)); // scalar argument
          else // an array:
          { double[] invals = R.Store[Args[var].I].Data;
            for (int i = 0; i < invals.Length; i++)  Values.Add( Convert.ToInt32(invals[i]));
        } }
        int[] outfactors = null;
        if (Values.Count == 1) 
        { List<int> lint = M2.FactorsOf(Values[0]);  
          if (lint != null) outfactors = lint.ToArray();
        }  
        else          
        { // Generate the primes library which will be used by all factors:
          int maxval = Values[0], minval = maxval;  
          foreach (int n in Values) { if (n > maxval) maxval = n;  if (n < minval) minval = n; }
          if (maxval >= 2)
          { List<int> PrimesLibrary = M2.PrimeEngine(2, maxval, -1, true);
            List<int>Candidates = M2.FactorsOf(minval, ref PrimesLibrary); // minval more likely to be rich in common factors than higher values.
            if (Candidates != null)
            { List<int>NextCandidates = new List<int>();
              foreach (int val in Values)
              { if (val == minval) continue; // as minval produced our starting list of candidates.
                List<int> theseFactors = M2.FactorsOf(val, ref PrimesLibrary);
                foreach (int n in Candidates)
                { int p = theseFactors.IndexOf(n);
                  if (p >= 0)
                  { NextCandidates.Add(n);  theseFactors[p] = 0; }
                }
                // Transfer NextCandidates to Candidates, and clear NextCandidates:
                Candidates.Clear(); // must precede the next step, so that...
                if (NextCandidates.Count == 0) break; // ...this will enter the code below with 'outfactors' still = null.
                Candidates.AddRange(NextCandidates); NextCandidates.Clear();
              }
              if (Candidates.Count > 0) outfactors = Candidates.ToArray();
            }
          }
        }    
        double[] outdata = null;
        if (outfactors == null) { result.I = EmptyArray();  break; }
        outdata = new double[outfactors.Length];
        for (int i = 0; i < outfactors.Length; i++) outdata[i] = Convert.ToInt32(outfactors[i]); 
        result.I = V.GenerateTempStoreRoom(outdata.Length);  R.Store[result.I].Data = outdata;
        break;
      }
      case 295: // PECK(REF Array, Value [, Replacer]): Remove the first instance of Value. (Note - the input array is altered in situ,
      // not returned by this function.) If no 3rd. argument, simply shorten the array. In this case, Array must be a list array, 
      // or else an error will be raised. 
      // If a third argument, just replace Value with Replacer instead, AND preserve structure. No check for arrayhood of 2nd. and 3rd. args. 
      // Returns, in effect, the no. of replacements: 1 (find) or 0. Note that if the REF array would logically be empty, the empty
      // array (length 1, content NaN) is returned.
      { bool replace = (NoArgs == 3);
        double badguy = Args[1].X,   replacement = 0;  if (replace) replacement = Args[2].X;
        int slot = Args[0].I;  if (slot == -1) return Oops(Which, "the first arg. must be an array");
        if (!replace && R.Store[slot].DimCnt > 1) return Oops(Which, "the first arg. must be a list array, in the case of the 2-arg. version");
        double[] indata = R.Store[slot].Data;
        int fnd = Array.IndexOf(indata, badguy);    if (fnd == -1) break; // return from fn. with result.X = 0.
        // Definite find:
        if (replace) R.Store[slot].Data[fnd] = replacement;
        else // a list array, which will be shortened:
        { int len = indata.Length;
          if (len == 1) // then the peck would logically cause the REF arg. to be an empty array:
          { R.Store[slot].Data[0] = double.NaN; } // not shortened; but .X will still be set to 1 (below).
          else // shorten the array, which is guaranteed not to then be empty:
          { List<double> ludo = new List<double>(indata);
            ludo.RemoveAt(fnd);
            double[] dudu = new double[len-1];
            ludo.CopyTo(dudu);
            R.Store[slot].Data = dudu;
            R.Store[slot].DimSz[0]--;
          }
        }
        result.X = 1.0;  break;
      }        
  //  case 296: PRODROWS(.) -- see SUMROWS(.), 223.
  //  case 297: PRODCOLS(.) -- see SUMCOLS(.), 223.
      case 298: //PRODUCT(Array [, scalar ZeroToOne]) - product of all the array terms. If ZeroToOne 'true', Array[0] is taken as is,
      // and every value from Array[1] is multiplied with it AS LONG AS it is not zero (ignored if it is).
      { int slot = Args[0].I;  if (slot==-1) return Oops(Which, "the first arg. must be an array");
        bool zero_to_1 = (NoArgs > 1 && Args[1].X > 0.0);
        double[] doo = R.Store[slot].Data;
        double product;
        if (zero_to_1)
        { product = doo[0];
          for (int i=1; i < doo.Length; i++)
          { if (doo[i] != 0.0) product *= doo[i]; }
        }
        else { product = 1.0;  foreach( double x in doo ) product *= x; }
        result.X = product;  break;
      }
      case 299: // RGB (colour argument [, 2nd. colour argument] ) -- returns the RGB value for a Gtk.Color, as an array of size 3.
      // The default colour for parsing failure of the first argument is the second argument if supplied (and valid), or else black: [0,0,0].
      { bool success;
        result.I = V.GenerateTempStoreRoom(3);
        Gdk.Color colour = InterpretColourReference(Args[0], Gdk.Color.Zero, out success);
        if (!success && NoArgs > 1)
        { colour = InterpretColourReference(Args[1], Gdk.Color.Zero, out success); }
        if (success) // If NOT success, then leave the store room as is - filled with zeroes.
        { byte[] boodle = colour._ToByteArray();
          R.Store[result.I].Data = new double[] { (double) boodle[0], (double) boodle[1], (double) boodle[2] };
        }
        break;
      }
      case 300: // JOIN(Matrix, scalar / array Delimiter,  scalar / array Padder [, scalar / array EmptyRowCue ] -- Joins rows of matrix, --> list array out.
      // Padder: If an array, only the first element is accessed.
      // Delimiter: Will be put between material from successive rows. If an array, the whole contents is taken as the delimiter.
      // EmptyRowCue: If absent, empty rows (i.e. containing only Padder) will be ignored; if included, this virtual row will go in.
      //  If absent, and Mx is nothing but padders, the return is the empty array.
      // If Mx is a list array, it will be treated as if it were a 1 x N matrix.
      { 
        int mxslot = Args[0].I;  if (mxslot == -1) return Oops(Which, "1st. arg. cannot be scalar");
        StoreItem mxIt = R.Store[mxslot];
        double[] indata = mxIt.Data;
        int noRows = mxIt.DimSz[1];   if (noRows == 0) noRows = 1; // convert list array to 1 x N matrix
        int rowLen = mxIt.DimSz[0];
        double[] delimiter;
        if (Args[1].I == -1) delimiter = new double[] { Args[1].X };
        else delimiter = R.Store[Args[1].I].Data;
        double padder;
        if (Args[2].I == -1) padder = Args[2].X;
        else padder = R.Store[Args[2].I].Data[0];
        double[] emptyRowCue = null;
        if (NoArgs > 3)
        { if (Args[3].I == -1) emptyRowCue = new double[] { Args[3].X };
          else emptyRowCue = R.Store[Args[3].I].Data;
        }
        List <double> outstuff = new List<double>(mxIt.TotSz);
        double[] thisRow;  int lastPadder;
        for (int i=0; i < noRows; i++)
        { thisRow = indata._Copy(i*rowLen, rowLen);
          lastPadder = 0; // Will be thisRow.Length, if no terminal padder found.
          for (int j = thisRow.Length - 1; j >= 0; j--)
          { if (thisRow[j] != padder) { lastPadder = j+1; break; } }
          if (lastPadder == 0)
          { if (emptyRowCue == null) continue; // ignore an empty row, if 3rd. argument absent.
            outstuff.AddRange(emptyRowCue);
          }
          else outstuff.AddRange(thisRow._Copy(0, lastPadder));
          // Tack on the delimiter after every row contribution:
          outstuff.AddRange(delimiter);
        }        
        double[] outdata;
        int n = outstuff.Count;
        if (n == 0) outdata = new double[] { double.NaN };
        else outdata = outstuff.ToArray()._Copy(0, n - delimiter.Length); // there is always a delimiter on the end.
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        StoreItem outIt = R.Store[result.I];
        outIt.Data = outdata;
        outIt.IsChars = mxIt.IsChars;
        break;
      }
     case 301: // GRAPHKEY(scalar GraphID). Pops the latest key-combination from the board object's stack. Returns a list array of size 3;
      // [0] = key value (as in the famous C header file "gdkkeysyms.h", locatable on the Internet. At the time of writing, it is saved in
      //        the Computing Documentation folder of this computer - 'Gtk and Gdk' folder).
      // [1] = helper key code. Using 'c'(ontrol), 's'(hift), 'a'(lternate), these are:
      //         0="", 1="a", 2="c", 3="s", 4="ac", 5="as", 6="cs", 7="acs".
      // [2] = number of valid keys still on the stack, after this one has been popped off it.
      // Note that if no key has been pressed since startup or since all existing values popped off the stack, the returned array is [0,0,0].
      // If the graph was not identified, returned is [-1, -1, -1]. In both of these cases, only [0] need be checked.
      // *** Note that there is no need to have a flag to indicate whether there has been a new keying since the last call, as each call to
      //  this function pops a keypress permanently off the internal stack. Keep polling graphkey(.) in a loop; when the return is [0,0,0].
      //  you know you have handled all extant key presses; thereafter a nonzero array will only result after user keys a new key.
     { Graph graf = null;
        double[] dataout = new double[3];
        Trio trill = Board.GetBoardOfGraph((int) Args[0].X, out graf);
        if (graf == null) dataout = new double[] { -1.0, -1.0, -1.0 };
        else
        { int n, noLeft;
          Strint father = Board.PopKeyings((int) trill.X, out noLeft);
          string ss = ">" + father.S + "<";
          n = ("><   >a<  >c<  >s<  >ac< >as< >cs< >acs<").IndexOf(ss);
          if (n == -1 || n % 5 != 0) throw new Exception("Board.PopKeyings returned an impossible helper-key code: '" + father.S + "'");
          dataout[0] = (double) father.I;
          dataout[1] = (double) n / 5;
          dataout[2] = (double) noLeft;
        }
        result.I = V.GenerateTempStoreRoom(dataout.Length);
        R.Store[result.I].Data = dataout;
        break;
      }
      case 302: // GRAPHCOLOURS (GraphID,  0 to 6 scalars/arrays).
      // Any scalar will be interpreted as meaning: Leave the existing colour in place.
      // RETURNS: Always a 3 x 6 matrix (unless no graph, when it is the empty list array of length 1, value NaN.
      // Columns are R,G and B values (range 0 to 255, integral); Rows are colours of:
      //  [0]: Graph box (i.e. plotting surface);  [1]: graph hairlines;    [2]: the border of the graph box;
      //  [3]: Outside the graph box (where axis scaling info. is printed); [4]: text of unit values at hairlines;
      //  [5]: text of axis name (e.g. "velocity (kms.)".
      // If only one arg. (the graph ID), does nothing but return existing values. As args. are supplied, they alter existing
      //  values (or leave as is, if error). To indicate that you want the existing value to remain for some arg, the best is
      //  to provide a one-char array of any sort (e.g. "-"), or any other array which is definitely not a colour.
      // No errors raised.
      { Graph graf = null;
        double[] dataout = null;
        bool[] applyColour = new bool[6];
        for (int i=1; i < NoArgs; i++) { if (Args[i].I >= 0) applyColour[i-1] = true; }
        Trio dummy = Board.GetBoardOfGraph((int) Args[0].X, out graf);
        // Exclude unrecognized graph:
        if (graf == null) { result.I = EmptyArray();  break; }
        Gdk.Color[] coloury = graf.GetColours();
        bool success;   Gdk.Color clr;
        for (int i = 1; i < NoArgs; i++)
        { if (Args[i].I >= 0) // if it is scalar, just leave the original colour.
          { clr = InterpretColourReference(Args[i], Gdk.Color.Zero, out success);
            if (success) coloury[i-1] = clr;
          }
        }
        graf.SetColours(coloury, applyColour);
        // Now get the values to return:
        coloury = graf.GetColours();
        dataout = new double[18];
        for (int i=0; i < 6; i++)
        { byte[] bb = coloury[i]._ToByteArray();
          dataout[3*i] = bb[0];   dataout[3*i+1] = bb[1];   dataout[3*i+2] = bb[2];
        }
        if (dataout.Length == 1) result.I = V.GenerateTempStoreRoom(1);
        else result.I = V.GenerateTempStoreRoom(3,6);
        R.Store[result.I].Data = dataout;
        break;
      }
      case 303: // EVICT(SourceArray, array FromWhere): // Returns a LIST array made up from the contents of SourceArray, AFTER REMOVING
      // values at indices in FromWhere. Values in FromWhere are interpreted in accordance with the structure of SourceArray.
      // If it has a DimCnt of N, then values will be expected to consist of sets of N coordinates per location in SourceArray.
      // The order of indices is intuitive - outer dimensions first. E.g. if Source is MxN, "arr = select(Source, data(m1, n1, m2, n2, ...))"
      // will return [ Source[m1,n1], Source[m2,n2]...]. 
      // If FromWhere would remove all elements from SourceArray, then the empty array of length 1, [0]=NaN, is returned.
      { int sourceslot = Args[0].I, whereslot = Args[1].I; 
        if (sourceslot == -1 || whereslot == -1) return Oops(Which, "both args. must be arrays");
        int dimcnt = R.Store[sourceslot].DimCnt;
        double[] wheredata = R.Store[whereslot].Data;    int wherelen = wheredata.Length;
        if (wherelen % dimcnt != 0)
        { return Oops(Which, "the length of the second arg. is not a multiple of the number of dimensions of the first arg."); }
        int[] dimsizes = R.Store[sourceslot].DimSz;
        List<double> whatsleft = new List<double>(R.Store[sourceslot].Data); // initially holds the whole of the array.
        int sourcelen = whatsleft.Count;
        // Find a value which is not present in whatsleft, for substitution purposes.
        double dummy; int n;
        do { dummy = Rando.NextDouble();  n = whatsleft.IndexOf(dummy); } while (n >= 0); 
        // Now build an array of absolute indices, derived from wheredata:
        int[] whereat = new int[wherelen];
        for (int i=0; i < wherelen; i++) { whereat[i] = Convert.ToInt32(wheredata[i]); }
        int ablen = wherelen / dimcnt;
        int[] abs_whereat = new int[ablen];
        if (dimcnt == 1) abs_whereat = whereat;
        else // a structured array:
        { int[] thisone = new int[dimcnt];
          for (int i=0; i < ablen; i++)
          { for (int j=0; j < dimcnt; j++) thisone[dimcnt-j-1] = whereat[i*dimcnt + j]; 
            n = LocateArrayValue(dimsizes, dimcnt, thisone);
            abs_whereat[i] = n;
          }
        }            
        for (int i=0; i < ablen; i++)
        { n = abs_whereat[i];
          if (n < 0 || n >= sourcelen)
          { if (dimcnt == 1) return Oops(Which, "the {0}th. array index is out of range", i+1);
            else return Oops(Which, "an index within the {0}th. set is out of range", i+1);
          }
        }
        // Now go through FromWhere, substituting 'dummy' for any value indicated by it:        
        for (int spot = 0; spot < ablen; spot++) whatsleft[abs_whereat[spot]] = dummy;
        // Remove all the dummies:
        bool boo;  do { boo = whatsleft.Remove(dummy); } while (boo);
        double[] outdata;  int whatsleftcnt = whatsleft.Count;
        if (whatsleftcnt == 0) { result.I = EmptyArray(); break; }
        outdata = new double[whatsleftcnt];  whatsleft.CopyTo(outdata);
        result.I = V.GenerateTempStoreRoom(whatsleftcnt);
        R.Store[result.I].Data = outdata;  break;        
      }
      case 304: // GFIXEDVISIBLE (scalar GraphID, char. array MenuText, scalar IsVisible). VOID. Switches a main menu item's visibility.
      // User has to supply a string of which the first letter is the first letter of the menu (case-insensitive).
      // If not identified, nothing happens (no error raised). There is no access to the submenus of menu items, and
      // no changes to text or actions can be applied to these main menu items.
      { int graphid = (int) Args[0].X;  Graph gruffalo;
        Trio truffles = Board.GetBoardOfGraph(graphid, out gruffalo);  if (gruffalo == null) break; // Do nothing, if board not identified.
        int boardid = truffles.X;
        string menutext = StoreroomToString(Args[1].I, true, true);  if (menutext == "") break;
        bool visible = Args[2].X != 0.0;
        Board.SetMenuStatus(boardid, menutext, -1, visible);
        break;
      }
      case 305: // PROGRAMFILE(scalar Dummy  OR  array PathOrName). If arg. scalar, path and name returned as one. If an array,
      // only [0] is examined; if [0] is 'P' or 'p', only the path is returned (with final '/'); if 'N' or 'n', only the name.
      // (Any other value has the same effect as scalar Dummy - the full path and name are returned.)
      // What is returned are MainWindow.ThisWindow.CurrentPath and ~.CurrentName as they were when the 'GO' method was about
      // to call R.Run(.). It cannot be changed during the run (unless the run institutes another instance by itself calling 'GO').
      {
        string outStr = "";
        string currentPath = MainWindow.ThisWindow.CurrentPath,     currentName = MainWindow.ThisWindow.CurrentName.Trim();
        if (currentName == "") return Oops(Which, "cannot be used until the program has been saved (and so given a name)");
        bool theLot = true;
        int slot = Args[0].I;
        if (slot >= 0) // then the argument is an array. If any value except P, p, N or n, the whole path + name is returned.
        { double x = R.Store[slot].Data[0];
          if      (x == 80 || x == 112) { outStr = currentPath;  theLot = false; } // 'P', 'p'
          else if (x == 78 || x == 110) { outStr = currentName;  theLot = false; } // 'N', 'n'
        }
        if (theLot) outStr = currentPath + currentName;
        result.I = V.GenerateTempStoreRoom(outStr.Length);
        StringToStoreroom(result.I, outStr);
        break;
      }
      case 306: // SOLVE_DE(char. array RHSFnName, scalar Y0, array XX [, array Method ] --
      // The Diff'l Eqn. must be in the form , where Fn is a user function somewhere in the main program.
      // "RHSFnName" - holds the literal name of a user fn. encoding the right hand side of a first-order D.E.
      //   (written in the form dy/dx = f(x, y) ). The function so aliassed must produce a scalar for these two args. as scalars
      //   (it can have optional args, but they will be ignored. It should NOT have REF args.)
      // Y0 is an integration constant, being the value of the solution "y = f(x)" where x is XX[0].
      // XX must be an arithmetic progression over the range of integration. There is no check for this, but if you put garbage in
      //   you will get garbage out. (Minor rounding errors to e.g. the 6th. significant figure would not matter.)
      // "Method" - Case-insensitive, and only the first three chars. are significant.
      //   Currently only two possibilities: "eul(er)" (the default for omission), "run(ge-kutta, 4th order)". (Anything else crashes.)
      // Note that the code below raises an error if the call to the supplied function would be recursive; to allow this would result in utter chaos.
      { 
        int fnnameslot = Args[0].I, xxslot = Args[2].I;
        if (fnnameslot == -1 || xxslot == -1) return Oops(Which, "1st. and 3rd. args. must be arrays");
        double[] XX = R.Store[xxslot].Data;
        int len = XX.Length;  if (len < 3) return Oops(Which, "3rd. arg. must have length of at least 3 (and usually much greater)");
        double delX = (XX[len-1] - XX[0]) / (len-1);
        bool isEuler = true;
        if (NoArgs > 3)
        { string tt = StoreroomToString(Args[3].I, true, true)._Extent(0, 3).ToLower();
          if (tt == "run") isEuler = false;
          else if (tt != "eul") return Oops(Which, "4th. arg. is not a recognized solution method");
        }
        string ss = StoreroomToString(Args[0].I, true, true); // return the input string, trimmed, or "" if scalar.
          // Is it a system function?
        short FnIndex = -1;
        for (int i=1; i < P.UserFn.Length; i++) // recall that UserFn[0] is in fact the main program, hence start with i=1.
        { if (ss == P.UserFn[i].Name) { FnIndex = (short) i;  break; } }
        if (FnIndex == -1) return Oops(Which, "no user function found with the given name");
        if (P.UserFn[FnIndex].REFArgCnt > 1) return Oops(Which, "the user fn. named is already undergoing recursion, so is unavailable to this fn.");
        P.UserFn[FnIndex].ArgSource = new Duo[]{ new Duo(), new Duo() }; // Dummy values. Needed, as R.RunUserFn(.) crashes if ArgSource is null.
        // A find:
        // PROCEED WITH SOLVING THE D.E.:
        double[] YY = new double[len];
        double k1, k2, k3, k4, x, y = Args[1].X; // No test for the arg. being an array
        var args = new PairIX[2];
        args[0] = new PairIX(-1, 0);   args[1] = new PairIX(-1, 0);
        // The following are all dummy args. (except for AssNo) for RunUserFn. AS LONG AS THERE IS NO RECURSION, they can be anything.
        //  (AssNo should be real, as it is recorded on the stack that tells how often a particular function has been called, from where).
        int topdog = -1, victim = -1, AssNo = R.LatestAssignment; // This is the assignment "LHS = solve_de(..)".
          bool buryatend = false, eq_serviced = false;
        YY[0] = y;
        if (isEuler)
        { for (int i=1; i < len; i++)
          { x = XX[i-1];
            args[0].X = x;   args[1].X = y;
            y += delX * R.RunUserFn(FnIndex, ref topdog, ref victim, ref AssNo, ref buryatend, ref eq_serviced, args).X;
            YY[i] = y;
          }
        }
        else // must be Runge-Kutta method:
        { for (int i=1; i < len; i++)
          { x = XX[i-1];
            args[0].X = x;   args[1].X = y;
            k1 = R.RunUserFn(FnIndex, ref topdog, ref victim, ref AssNo, ref buryatend, ref eq_serviced, args).X;
            args[0].X = x + delX / 2.0;   args[1].X = y + 0.5 * delX * k1;
            k2 = R.RunUserFn(FnIndex, ref topdog, ref victim, ref AssNo, ref buryatend, ref eq_serviced, args).X;
                                        args[1].X = y + 0.5 * delX * k2;  // args[0].X remains unchanged
            k3 = R.RunUserFn(FnIndex, ref topdog, ref victim, ref AssNo, ref buryatend, ref eq_serviced, args).X;
            args[0].X = x + delX;       args[1].X = y +       delX * k3;
            k4 = R.RunUserFn(FnIndex, ref topdog, ref victim, ref AssNo, ref buryatend, ref eq_serviced, args).X;
            y += delX * (k1 + 2*k2 + 2*k3 + k4) / 6.0;
            YY[i] = y;
          }
        }
        result.I = V.GenerateTempStoreRoom(len);
        R.Store[result.I].Data = YY;
        break;
      }
      case 307: // UNJAG(Mx, scalar RowNo [, scalar or array Padder]). Retrieve a row from the array, and remove the given padding value
      // from its end. If no padding value is supplied, any value from 32.0 down at the end will be removed. Note: NO rounding of pad value.
      // If left with a null string, the empty list array of size 1, [0] = NaN, will be returned.
      // If 'Mx' is in fact a simple list array, it will simply be trimmed of the padder. This is necessary because some functions, e.g. 'request(.)',
      // may return either a list array or a jagged matrix, depending on circumstances.
      { int mxslot = Args[0].I;   int[] dims = null;
        int rowno = Convert.ToInt32(Args[1].X); // too bad if it was an array by mistake.
        if (mxslot == -1) return Oops(Which, "the first arg. must be a matrix or a list array");
        StoreItem sitem = R.Store[mxslot];
        dims = sitem.DimSz._Copy(); // Nec., as DimSz is a field, and we are going to alter 'dims'.
        if (dims[2] != 0) return Oops(Which, "the first arg. cannot be a structure of more than 2 dimensions");
        if (dims[1] == 0) dims[1] = 1; // We will pretend a list array is a jagged array with one row.
        if (rowno >= dims[1]) return Oops(Which, "the supplied row index exceeds the no. of rows in the first argument");
        int rowlen = dims[0];
        double[] therow = new double[rowlen];   
        Array.Copy(R.Store[mxslot].Data, rowno*dims[0], therow, 0, rowlen);
        // Remove the padder.        
        bool padvalSupplied = (NoArgs > 2);
        double padval = 0;
        if (NoArgs > 2)
        { int padslot = Args[2].I; 
          if (padslot == -1) padval = Args[2].X;  else padval = R.Store[padslot].Data[0];
        }
        int ptr = -1;
        if (!padvalSupplied) // then we look for the first character which is greater than 32.0:
        { padval = 32.0;
          for (int i = rowlen-1; i >= 0; i--) { if (therow[i] > padval) { ptr = i; break; } }
        }
        else for (int i = rowlen-1; i >= 0; i--) { if (therow[i] != padval) { ptr = i; break; } }
        if (ptr == -1) { result.I = EmptyArray(); break; }
        int outslot = V.GenerateTempStoreRoom(ptr+1);         
        Array.Copy(therow, R.Store[outslot].Data, ptr+1);
        R.Store[outslot].IsChars = R.Store[mxslot].IsChars;
        result.I = outslot;   break;
      }  
      case 308: // JAG(Matrix, array Operation, array NewRow, [, scalar RowNo [, scalar Length [, scalar Filler ]]]) NONVOID.
      // Returns a copy of Matrix with a new row appended / inserted / overwriting an old one. What happens is determined by Opn[0].
      // 'I' or 'i' causes insertion; 'O' or 'o' causes overwriting; 'A' or 'a' causes appending.
      // The WHOLE matrix will be padded / amputated as indicated by scalar args. 
      // The 3-arg. version is allowed only for operation 'A(ppend)'. For 'A', if RowNo supplied, it is ignored.
      // (a) No Length, no Filler: If the new row is longer than existing rows, they will be padded to its length with zeroes).
      //       If it is shorter, it will instead be padded to the existing row length of the matrix.
      // (b) Length supplied and >= 1: All rows will be conformed to the prescribed length. (Neg. values ignored - the above then happens.)
      // (c) Filler supplied: If rows are to be padded, this value is always used, replacing the default system mentioned above.
      // RowNo must be valid EXCEPT for the Append operation, in which case it is simply ignored.      
      // If 'Matrix' is a list array it will be treated as if it were a 1xN matrix, so you can use 'jag' to build a mx. from nothing.
      { int mxslot = Args[0].I,  opslot = Args[1].I,  newrowslot = Args[2].I;
        if (mxslot == -1 || opslot == -1 || newrowslot == -1) return Oops(Which, "the first three args. must be arrays");
       // Deal with the Matrix argument.
        int[] dims = R.Store[mxslot].DimSz;  int oldnorows = dims[1], oldrowlen = dims[0];
        if (oldnorows == 0) oldnorows = 1; // treat a list array as if it were a matrix with one row (i.e. a row vector).
        bool ischars = R.Store[mxslot].IsChars;
        double[] mxdata = R.Store[mxslot].Data;
       // Deal with the Operation argument.
        char opn = char.ToUpper(R.Store[opslot].Data[0]._ToChar(' '));
        if (opn != 'A' &&  opn != 'I' &&  opn != 'O')
        { return Oops(Which, "unrecognized operation code '{0}' in the second argument", opn); }
        // Deal with the NewRow argument.
        double[] newrowdata = R.Store[newrowslot].Data;  int newrowdatalen = newrowdata.Length;
        // Deal with the RowNo argument. 
        int rowno = -1;
        if (opn != 'A') 
        { if (NoArgs == 3) return Oops(Which, "no row no. was supplied");
          rowno = Convert.ToInt32(Args[3].X);
          if (rowno < 0 || rowno >= oldnorows) return Oops(Which, "the row no. (i.e. the 4th. arg.) is out of range");
        }
       // Deal with the Length argument.
        int finalrowlen = 0; if (NoArgs > 4) finalrowlen = Convert.ToInt32(Args[4].X);
        if (finalrowlen < 1) { finalrowlen = oldrowlen; if (finalrowlen < newrowdatalen) finalrowlen = newrowdatalen; }
       // Deal with the Filler argument.
        double padvalue = 0.0;  if (NoArgs > 5) padvalue = Args[5].X; // no check for scalarity.
       // Set up the shell of the new structure:
        int finalnorows = oldnorows; if (opn != 'O') finalnorows++;
        int outslot = V.GenerateTempStoreRoom(finalrowlen, finalnorows);
        R.Store[outslot].IsChars = ischars;
        double[] outdata = R.Store[outslot].Data;
        if (padvalue != 0.0) { for (int i=0; i < outdata.Length; i++) outdata[i] = padvalue; }
       // Copy existing data into outdata: 
        int copyrowlen = oldrowlen;  if (copyrowlen > finalrowlen) copyrowlen = finalrowlen; // length of old mx. rows to copy.
        int copyarraylen = newrowdatalen; if (copyarraylen > finalrowlen) copyarraylen = finalrowlen; // length of new row data to copy.
        if (opn == 'A')
        { for (int i=0; i < oldnorows; i++) Array.Copy(mxdata, i * oldrowlen, outdata, i*finalrowlen, copyrowlen);
          Array.Copy(newrowdata, 0, outdata, oldnorows * finalrowlen, copyarraylen);
        }
        else if (opn == 'O')
        { for (int i=0; i < rowno; i++) Array.Copy(mxdata, i * oldrowlen, outdata, i*finalrowlen, copyrowlen);
          Array.Copy(newrowdata, 0, outdata, rowno * finalrowlen, copyarraylen);
          for (int i=rowno+1; i < oldnorows; i++) Array.Copy(mxdata, i*oldrowlen, outdata, i*finalrowlen, copyrowlen);
        }          
        else // opn == 'I':        
        { for (int i=0; i < rowno; i++) Array.Copy(mxdata, i * oldrowlen, outdata, i*finalrowlen, copyrowlen);
          Array.Copy(newrowdata, 0, outdata, rowno * finalrowlen, copyarraylen);
          for (int i=rowno; i < oldnorows; i++) Array.Copy(mxdata, i*oldrowlen, outdata, (i+1)*finalrowlen, copyrowlen);
        }          
        result.I = outslot;   break;
      }
      case 309: // DIFFERENCES( InArray ) -- Suppose InArray is [a, b, c, d]. Then the return is [b-a, c-b, d-c].
      // Thus, return is one shorter than InArray; it is always a list array, irrespective of structure of InArray.
      // If the input length of InArray is 1, [NaN] is returned.
      { int indataslot = Args[0].I;
        if (indataslot == -1) return Oops(Which, "array arg. required");
        StoreItem It = R.Store[indataslot];
        int indatalen = It.TotSz;
        double[] indata = It.Data,  outdata;
        if (indatalen == 1) outdata = new double[] { double.NaN };
        else
        { double p = indata[0], q;
          outdata = new double[indatalen-1];
          for (int i=1; i < indatalen; i++)
          { q = indata[i];  outdata[i-1] = q - p;   p = q;  }
        }
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        break;
      }
      case 310: // PROGRESSIVE( array Subject,  array Operation) -- returns an array of the same size and structure as Subject (but non-chars.),
      // each term being an operation involving all previous terms of Subject. If Operation is '+', term n = Subject[0] + Subject[i] + ...
      // Subject[n];  if operation is '*', term n = Subject[0] * Subject[1] * ... * Subject[n] (so that if Subject were [1, 2, 3, ..., N],
      // the result would be factorial N). Obviously with this operation, overflow is possible - caveat emptor!
      { int inslot = Args[0].I, opslot = Args[1].I;   
        if (inslot == -1 || opslot == -1) return Oops(Which, "args. must both be arrays");
        StoreItem inIt = R.Store[inslot];
        double[] indata = inIt.Data;
        int inLen = indata.Length;
        double[] outdata = new double[inLen];
        double opn = R.Store[opslot].Data[0];
        double outvalue;
        if (opn == 42.0) // Operation '*'
        { outvalue = 1.0;
          for (int i=0; i < inLen; i++)
          { outvalue *= indata[i];
            outdata[i] = outvalue;
          }
        }
        else if (opn == 43.0) // Operation '+'
        { outvalue = 0.0;
          for (int i=0; i < inLen; i++)
          { outvalue += indata[i];
            outdata[i] = outvalue;
          }
        }
        else return Oops(Which, "unrecognized operation");
        result.I = V.GenerateTempStoreRoom(inIt.DimSz);
        R.Store[result.I].Data = outdata;
        break;
      }
      case 311: case 312: // ROWVEC / COLVEC (any mix of scalars and arrays, but at least one)
      case 313: // MATRIX ( NoRows, NoCols [, any mix of scalars and arrays] )  -- all are NONVOID operations, returning the structure.
      // For the VEC... operations, the length of data defines the size of the vector, and so must have at least length 1.
      // For MATRIX, however many values are supplied after the dimensioning arguments, these are cycled through,
      //  row by row. E.g. a scalar value x will result in a matrix in which all elements are x; an Nx3 matrix given data (x, y, z)
      // will have each row = [x, y, z]. If there is NO 3rd. argument, all elements will default to 0. If there is too much data, excess is ignored.
      { int NoRows = 0, NoCols = 0;
        double[] outdata;
        int outslot, outdataLen;
        if (Which == 313) // MATRIX:
        { NoRows = Convert.ToInt32(Args[0].X);   NoCols = Convert.ToInt32(Args[1].X);
          if (NoRows < 1 || NoCols < 1) return Oops(Which, "the 1st. 2 args. must be scalars, with rounded value ≥ 1");
          outdataLen = NoRows * NoCols;
          outdata = new double[outdataLen];
          outslot = V.GenerateTempStoreRoom(NoCols, NoRows);
          double[] indata;
          if (NoArgs > 2) // If NoArgs = 2, nothing to do; outdata has already been fully defined above.
          { indata = AccumulateValuesFromArgs(Args, 2);
            int indataLen = indata.Length;
            if (indataLen == outdataLen) outdata = indata;
            else { for (int i=0; i < outdataLen; i++)  outdata[i] = indata[i % indataLen]; } // OK whether indataLen is > or < outdataLen.
          }  
        }
        else // VECTOR:
        { outdata = AccumulateValuesFromArgs(Args);
          outdataLen = outdata.Length;
          if (Which == 311) outslot = V.GenerateTempStoreRoom(outdataLen, 1); // row vector
          else              outslot = V.GenerateTempStoreRoom(1, outdataLen); // column vector
        }
        R.Store[outslot].Data = outdata;
        result.I = outslot;  break;
      }
      case 314: // MATRIXOP(matrix OriginalMx,  char. array Opn,  array Imposed) -- Three scenarios, both returning a copy of OriginalMx
      // which has been altered by the operation. (I use 'Result' for the returned matrix). The third is only tested for if neither of the
      // first two have been found to apply.
      // (1) Imposed is a ROW VECTOR, size = row length of OriginalMx: Result[][i] = OriginalMx[][i] <Opn> Imposed[i].  
      // (2) Imposed is a COLUMN VECTOR, size = column length of OriginalMx: Result[i][] = OriginalMx[i][] <Opn> Imposed[i].  
      // (3) Imposed is typically a MATRIX, but may be ANY STRUCTURE (incl.a list array) OF THE SAME TOTAL SIZE as OriginalMx:
      //       the operation is performed element by element.
      // Opn[0] must be one of: (a) arithmetic signs (+, -, *, /, ^); (b) 'M' (Result element is the Maximum out of the OriginalMx and
      //   the Imposed elements), (c) 'm' (analogous - replace 'Maximum' with 'minimum').
      // Note: the arithmetic signs are intended for the situation where Imposed is a vector / list array. They work with Imposed as a matrix,
      //   but are much slower than the corresponding native operations.
      { int mxslot = Args[0].I, opslot = Args[1].I, arrslot = Args[2].I;
        if (mxslot == -1 || opslot == -1 || arrslot == -1) return Oops(Which, "only array args. are allowed");
        StoreItem mxItem = R.Store[mxslot],  arrItem = R.Store[arrslot];
        int[] mxdims = mxItem.DimSz;   int norows = mxdims[1], nocols = mxdims[0];
        if (norows == 0) return Oops(Which, "the 1st. arg. must be a matrix");
        double[] mxdata = mxItem.Data, arrdata = arrItem.Data;  
        int mxlen = mxdata.Length,  arrlen = arrdata.Length;
        char arrtype;      
        int[] arrdims = R.Store[arrslot].DimSz;
        int errno = 0;
        if (arrdims[1] == 1)
        { arrtype = 'R';  if (arrlen != nocols) errno = 1; }
        else if (arrdims[0] == 1)
        { arrtype = 'C';  if (arrlen != norows) errno = 2; }
        else 
        { arrtype = 'M';  if (arrlen != mxlen) errno = 3; } // Note that the 3rd. arg. doesn't have to be a matrix; just an array of the same total size.
        if (errno > 0) return Oops(Which, "the 1st. and 3rd. arrays are not of consistent sizes");
        double opn = R.Store[opslot].Data[0];
        result.I = V.GenerateTempStoreRoom(mxdims);
        StoreItem outItem = R.Store[result.I];
        double[] outdata = outItem.Data;
        int offset;  double arrspot = 0.0, mxspot = 0.0, outspot = 0.0;
        for (int rw=0; rw < norows; rw++)
        { offset = rw*nocols;
          for (int cl=0; cl < nocols; cl++)
          { if (arrtype == 'R') arrspot = arrdata[cl];
            else if (arrtype == 'C') arrspot = arrdata[rw];
            else arrspot = arrdata[offset + cl];
            mxspot = mxdata[offset + cl];
            if      (opn == 43.0) outspot = mxspot + arrspot;
            else if (opn == 45.0) outspot = mxspot - arrspot;
            else if (opn == 42.0) outspot = mxspot * arrspot;
            else if (opn == 47.0) outspot = mxspot / arrspot;
            else if (opn == 94.0) // '^'
            { if ( mxspot < 0 && arrspot != Math.Round(arrspot) )
              { return Oops(Which, "improper power operation at matrix location ({0}, {1})", rw, cl); }
              outspot = Math.Pow(mxspot, arrspot);
            }
            else if (opn == 109.0) outspot = Math.Min(mxspot, arrspot);
            else if (opn == 77.0)  outspot = Math.Max(mxspot, arrspot);
            else return Oops(Which, "Operation is not recognized");
           outdata[offset + cl] = outspot;
          }
        }
        outItem.IsChars = mxItem.IsChars;
        break;
      }
      case 315: // LOP (Array, scalar HowManyChars [, scalar ButNotShorterThanThis]) -- Returns a copy of the data strip of Array
      // (with no reference to its structure), shortened by lopping HowManyChars from its END. If HowManyChars is >= length of Array,
      // the 'empty' array is returned. UNLESS there is a third argument and it rounds to > 0; then no lopping beyond that length will occur.
      // (If Array is already as short or shorter than that, the fn. simply returns a copy of the data strip of Array.)
      { int inslot = Args[0].I,  chopCnt = Convert.ToInt32(Args[1].X); // too bad if it is an array.
        if (inslot == -1) return Oops(Which, "1st. arg. must be an array");
        int minLen = 0;  if (NoArgs > 2) minLen = Convert.ToInt32(Args[2].X);
        StoreItem inIt = R.Store[inslot];
        double[] indata = inIt.Data;
        int inLen = indata.Length, copyLen;
        if (inLen <= minLen  ||  chopCnt < 1) copyLen = inLen; // simply return the original data in toto.
        else copyLen = Math.Max(inLen - chopCnt, minLen);
        double[] outdata;
        if (copyLen == 0) { outdata = new double[] { double.NaN };  copyLen = 1; }
        else              { outdata = new double[copyLen];  Array.Copy(indata, outdata, copyLen); }
        result.I = V.GenerateTempStoreRoom(copyLen);        
        StoreItem outIt = R.Store[result.I];
        outIt.Data = outdata;
        outIt.IsChars = inIt.IsChars;
        break;
      }

      case 316: case 317: 
      // 316 = PAD (Array, scalar DesiredLength, array or scalar PadValue [, AndTruncateIfLonger ]) -- if Array shorter than DesiredLength,
      //  it is padded using PadValue (or its [0], if an array). If longer, it also truncated if last arg. present and nonzero.
      // 317 = TRUNCATE (Array, scalar DesiredLength). Shorter arrays left alone.
      // If DesiredLength is silly (<= 0), crashes.
      // Structure of Array ignored; output is a list array. However, the chars. rating of the output is the same as of the input.
      { int inslot = Args[0].I,  setlen = Convert.ToInt32(Args[1].X);
        if (inslot == -1 || setlen <= 0) return Oops(Which, "one of the first two arguments is not legitimate");
        double[] indata = R.Store[inslot].Data;   int inlen = indata.Length;
        bool isEmpty = (inlen == 1 && double.IsNaN(indata[0]) );
        int outslot = -1;  double[] outdata = null;
        double padvalue = 0.0;
        // Truncate, if indicated:
        if (inlen > setlen && (Which == 317 || (NoArgs == 4 && Args[3].X != 0.0) ) )
        { outdata = new double[setlen];
          outslot = V.GenerateTempStoreRoom(setlen);
          Array.Copy(indata, outdata, setlen);
        }
        // Pad out, if indicated:
        else if (inlen < setlen && Which == 316)
        { outdata = new double[setlen];
          outslot = V.GenerateTempStoreRoom(setlen);
          if (Args[2].I == -1) padvalue = Args[2].X;  else padvalue = R.Store[Args[2].I].Data[0];
          if (isEmpty) inlen = 0;
          else Array.Copy(indata, outdata, inlen);
          for (int i = inlen; i < setlen; i++) outdata[i] = padvalue;
        }
        else // simply return a copy of the input array:          
        { outdata = new double[inlen];
          outslot = V.GenerateTempStoreRoom(inlen);
          Array.Copy(indata, outdata, inlen);
        }
        R.Store[outslot].Data = outdata;
        R.Store[outslot].IsChars = R.Store[inslot].IsChars;
        result.I = outslot;  break;
      }

      case 318: // READTABLE(..) Two versions, differing only in whether args. start with a single 2_row matrix or with two equal list arrays:
      //  READTABLE( Mx Table,                                 scalar / array InputValue(s), array LookupRule [, scalar StartPtr] ); 
      //  READTABLE( array InRowOfTable, array OutRowOfTable,  scalar / array InputValue(s), array LookupRule [, scalar StartPtr] ); 
      //  If starts with a 2_row matrix, then row 0 ("in_row") is the row for the input value, and row 1 ("out_row") holds the corresponding
      //    output values; length >= 2. If not, the 1st. arg. is taken as "in_row", the 2nd. (same length) as "out_row".
      //  RETURNS structure of the same dimensionality as InputValue(s), containing table output values.
      //  "InputValues" - if array, any length. (Unlike the next arg., values need not be sorted
      //  "LookupRule" - only the first char. is examined. Allowed values: "=" - returns an out_row value only if the input value exactly matches
      //     an in_row value; otherwise returns MINREAL. "~" - returns the out_row value corresponding to the nearest in_row value
      //     to the input; if input is midway between two in_row values, the lower one is used. "L" - linear interpolation; e.g. if input
      //     is 1/3 between in_row[i] and in_row[i+1], the output will be 1/3 between out_row[i] and out_row[i+1].
      //    If the input value is outside the range of in_row values, extrapolation occurs using the same rule, based on the nearest in_row value(s).
      //  "StartPtr" - If omitted, every search for every input value always starts from first element of in_row (i.e. default StartPtr is 0).
      //    If supplied: (a) if >= 0, is the start pointer in in_row for the search for every element of InputValues.(b) If NEGATIVE, then
      //    it is ASSUMED that InputValues is sorted (duplicates allowed), and every search for InputValues[i] will start from the floor of the
      //    index in in_row that was identified for InputValues[i-1].
      //  NB: GIGO APPLIES, without error detection, if: (1) in_row is not in ascending order, without duplications; (2) StartPtr is supplied
      //    and is negative, but InputValues is not sorted.
      { int NoArgsForTable;
        // Get the TABLE:
        int tabslot = Args[0].I;  if (tabslot == -1) return Oops(Which, "1st. arg. cannot be scalar");
        StoreItem tabitem = R.Store[Args[0].I];
        double[] inrowData = tabitem.Data, outrowData;
        int rowOffset, tableLen;
        if (tabitem.DimCnt == 2) // we have a matrix:
        { if (tabitem.DimSz[1] != 2) return Oops(Which, "1st. arg., if a matrix, must have exactly two rows");
          tableLen = tabitem.DimSz[0];
          if (tableLen == 1) return Oops(Which, "1st. arg., if a matrix, must have a row length of at least 2");
          NoArgsForTable = 1;
          outrowData = tabitem.Data; // i.e. we have two pointers to the same data array, in the case of a matrix 1st. arg.
          rowOffset = tableLen; // the offset from which valid output data starts in 'outrowData'.
        }
        else // we have two arrays instead, for the table:
        { tableLen = inrowData.Length;  if (tableLen == 1) return Oops(Which, "table rows must have a length of at least 2");
          if (Args[1].I == -1) return Oops(Which, "wrong deployment of 1st. two args.");
          NoArgsForTable = 2;
          outrowData = R.Store[Args[1].I].Data;
          if (outrowData.Length != tableLen) return Oops(Which, "unequal list arrays as 1st. two args.");
          rowOffset = 0;
        }
        // Get the INPUT DATA:
        double[] indata;
        int inslot = Args[NoArgsForTable].I;
        bool inputScalar = (inslot == -1);
        if (inputScalar) indata = new double[] { Args[NoArgsForTable].X };
        else indata = R.Store[inslot].Data;
        int indataLen = indata.Length;
        // Set the LOOKUP RULE:
        if (NoArgs < NoArgsForTable+2) return Oops(Which, "not enough args.");
        int lookupslot = Args[NoArgsForTable+1].I;
        if (lookupslot == -1) return Oops(Which, "{0}th arg. - the lookup rule - must be an array", NoArgsForTable+2);
        int lookupRule = Convert.ToInt32(R.Store[lookupslot].Data[0]); // "=" is 61;   "~" is 126;   "L" is 76.
        if (lookupRule != 61  &&  lookupRule != 126 && lookupRule != 76) return Oops(Which, "{0}th. arg.: invalid code", NoArgsForTable+2);
        // Set the START POINTER:
        int StartPtr = 0;
        if (NoArgs > NoArgsForTable+2)
        { StartPtr = Convert.ToInt32(Args[NoArgsForTable+2].X);
          if (StartPtr >= tableLen-1)
          { if (lookupRule == 76) return Oops(Which, "last arg. sets start search pointer to at or beyond end of table"); // need 2, for interpolation
            else if (StartPtr >= tableLen) return Oops(Which, "last arg. sets start search pointer to beyond end of table");
          }
        }
        bool adjustableStartPtr = false; // If TRUE, the start ptr in in_row will be adjusted after each successive value of InputValues.
        if (StartPtr < 0) { adjustableStartPtr = true;  StartPtr = 0; }
        // LOOK UP THE TABLE:
        double[] outdata = new double[indataLen];
        double inputValue, outputValue=0, loDist, hiDist;
        int loNdx, hiNdx, floorIndex = 0; // only relevant if adjustableStartPtr is TRUE.
        for (int i=0; i < indataLen; i++)
        { inputValue = indata[i];
          int firstAtOrAbove = tableLen;
          bool isExact = false; // i.e. is an exact match between input value and in_row value
          // Adjust start ptr, if nec.
          if (adjustableStartPtr) StartPtr = floorIndex; // otherwise it remains permanently as set above
          for (int j=StartPtr; j < tableLen; j++)
          { if (inputValue <= inrowData[j]) 
            { firstAtOrAbove = j;  isExact = (inputValue == inrowData[j]);  break; }
          }
          // Assign a return value based on the arguments.
          if (isExact) { floorIndex = firstAtOrAbove;  outputValue = outrowData[firstAtOrAbove + rowOffset]; }   // Same for all lookup rules.
          else if (lookupRule == 61) outputValue = double.MinValue; // Lookup rule is "=". Note that 'floorIndex' is left as is.
          else if (lookupRule == 126) // lookup rule "~" - find nearest value in in_row:
          { if (firstAtOrAbove == StartPtr) // input value below lowest table element consulted
            { floorIndex = StartPtr;  outputValue = outrowData[StartPtr + rowOffset]; }
            else if (firstAtOrAbove == tableLen) // input value above all table elements
            { floorIndex = tableLen-1;  outputValue = outrowData[floorIndex + rowOffset]; }
            else // find the nearest index in in_row:
            { loDist = inputValue - inrowData[firstAtOrAbove-1];  hiDist = inrowData[firstAtOrAbove] - inputValue;
              outputValue = (loDist <= hiDist) ? outrowData[firstAtOrAbove-1 + rowOffset] : outrowData[firstAtOrAbove + rowOffset];
            }
          }
          else // lookup rule "L", with no exact match:
          { loNdx = firstAtOrAbove-1;  hiNdx = firstAtOrAbove; // The table indexes which will be involved in the linear interpolation
            if (loNdx < 0) { loNdx++; hiNdx++; }
            if (hiNdx == tableLen) { loNdx--; hiNdx--; }
            loDist = inputValue - inrowData[loNdx];  hiDist = inrowData[hiNdx] - inrowData[loNdx]; // Beyond table ends, one of these will be negative.
            outputValue = outrowData[loNdx + rowOffset] + loDist * (outrowData[hiNdx + rowOffset] - outrowData[loNdx + rowOffset]) / hiDist;
          }
          outdata[i] = outputValue;
        }
        if (inputScalar) { result.X = outputValue;  break; }
        result.I = V.GenerateTempStoreRoom(R.Store[inslot].DimSz);
        R.Store[result.I].Data = outdata;
        break;
      }
      case 319: // INTERSECTION(any number of arrays, but no scalars thanks). Returns a list array (nonchars) of elements
      // common to all arguments. If none, returns the empty array of size 1, [0] = NaN. Input arrays - structure ignored.
      { int[] slots = new int[NoArgs],  lens = new int[NoArgs];
        double[][] indata = new double[NoArgs][];   int minlen = Int32.MaxValue, minarg = -1;
        for (int i=0; i < NoArgs; i++)
        { slots[i] = Args[i].I;  if (slots[i] == -1) return Oops(Which, "all args. must be arrays");
          indata[i] = R.Store[slots[i]].Data;   
          lens[i] = indata[i].Length;  
          if (lens[i] < minlen) { minlen = lens[i];  minarg = i; }
        }
        // Take the array of minimum length and try its members against every other array:        
        List<double> cup = new List<double>();   bool foundinall = false;  int ptr = -1;
        // See if each of its values is in all other arrays, and if so, include it in 'cup'.
        foreach (double x in indata[minarg])
        { foundinall = true;
          for (int i=0; i < NoArgs; i++)
          { if (i != minarg)
            { ptr = Array.IndexOf(indata[i], x);
              if (ptr == -1) { foundinall = false;  break; }
            }
          }
          if (foundinall) cup.Add(x);
        }
        double[] outdata = null;  List<double>nondups = new List<double>();
        if (cup.Count == 0) { result.I = EmptyArray(); break; }
        outdata = new double[cup.Count]; cup.CopyTo(outdata);
        // Remove any duplicates:   
        if (outdata.Length > 1)
        { nondups.Add(outdata[0]);
          for (int i=1; i < outdata.Length; i++)
          { if (nondups.IndexOf(outdata[i]) == -1) nondups.Add(outdata[i]); }
          if (nondups.Count < outdata.Length)
          { outdata = new double[nondups.Count];  nondups.CopyTo(outdata); }
        }  
        int outslot = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[outslot].Data = outdata;        
        result.I = outslot;  break;
      }


      case 320: // __IMPORTSCALAR(1 or more scalars). The args. must all exist in the main program, and be assigned at the time of this call.
      case 321: // __IMPORTARRAY(1 or more arrays). Ditto.
      // If main pgm vars exist and assigned, they will be copied to local variables of the same name. If not, an error crash will occur.
      // Any local variables of the same name, assigned or not, will be overwritten, so obviously this should go at the start of the function.
      { int errno = 0;   string varname = "";
        bool areScalars = (Which == 320);
        for (int arg = 0; arg < NoArgs; arg++)
        { int Fn = REFArgLocn[arg].Y,  At = REFArgLocn[arg].X;
          if (Fn < 0){ errno = 10;  goto erratum; }
          int varuse = V.GetVarUse(Fn, At);
          if (varuse == 1 || varuse == 2) { errno = 20; goto erratum; } // an escape in case of programming changes in the future; would
                                   // only occur if function constants were allowed, or function system variables without prefix of '__'
          // Vital background: At the first call to a user fn with 'import...', all such varuse will be 0. But when the user fn. is called
          //  a second time, varuse will be 3 for scalars and 10 for arrays. '10' for arrays means they are unassigned; but the scalars
          //  remain assigned (and indeed retain their prior values, though these are only accessible through trickery).
          // We will also allow values here to overwrite earlier values assigned in the same function run - 
          //   e.g. "aa = data(1,2); import array aa; " will work, but the imported main pgm. aa will overwrite the prev. line's aa.
          // So first we find what is in the main pgm.:
          varname = V.GetVarName(Fn, At);
          int mainvarno = V.FindVar(0, varname);
          if (mainvarno == -1){ errno = 30; goto erratum; }
          byte mainvaruse = V.GetVarUse(0, mainvarno);
          // Main program entity is an assigned scalar:
          if (mainvaruse == 3) // Main program variable of this name is an assigned SCALAR:
          { if (!areScalars){ errno = 40; goto erratum; }
            V.SetVarUse(Fn, At, mainvaruse); // No reference to whether this user fn. has previously assigned the arg. variable, 
                                           //  whether in this run or a previous one. The value is simply overwritten.
            V.SetVarValue(Fn, At, V.GetVarValue(0, mainvarno));
            V.Vars[0][mainvarno].Accessed = true; // records that the variable being copied in the main pgm has been accessed.
          }
          // Main program 
          else if (mainvaruse == 11) // Main program variable of this name is an assigned ARRAY:
          { if (areScalars){ errno = 50; goto erratum; }
            if (V.GetVarUse(Fn, At) == 11) { errno = 55; goto erratum; }
            if (!V.MakeCopy(0, mainvarno, Fn, At)) { errno = 57;  goto erratum; } // error happens if Use is 11 but store room is nulled.
                          // This can occur - see error message for how. *** Future revision: make it so that Use is set to 10 in main pgm!
            // if local array already existed, 'MakeCopy' above will have demolished its former existence.
            V.Vars[0][mainvarno].Accessed = true; // records that the variable being copied in the main pgm has been accessed.
          }
          else if (mainvaruse == 1 || mainvaruse == 2) { errno = 60; goto erratum; }
          else { errno = 70; goto erratum; }
        }
     // DEAL WITH ERRORS:
        erratum:  
        { if (errno != 0)
          { string ss; if (areScalars) ss = "scalar"; else ss = "array";
            result.S = "'import " + ss + " declaration: ";   
            if (errno > 10) result.S += "'" + varname + "' ";
            // Specific error message components:
            if      (errno == 10) result.S += "problem with a variable's name";
            else if (errno == 20) result.S += "problem with MiniMaths code - call the programmer";
            else if (errno == 30) result.S += "not found in the main program";
            else if (errno == 40) result.S += "is scalar in the main program";
            else if (errno == 50) result.S += "is an array in the main program";
            else if (errno == 55) result.S += "is an array already assigned in this function, either as an argument or within the function proper";
            else if (errno == 57) result.S += "has nulled data. (This typically happens where some function Foo has a REF argument Arr; " + 
              "Arr is changed in Foo, which nulls the main program version, but does not update that version till the function closes. Meanwhile " +
              "function Foo goes on to call function Bar, which has the line 'import array Arr' - but Arr is currently nulled)";
            else if (errno == 60) result.S += "is a system constant or system variable";
            else if (errno == 70) result.S += "not yet assigned in the main program";
            result.B = false; return result;
          }
        }
        break;
      }
      case 322: // SETFUNC ( scalar WhichAlias, array FnName [, DontCrashIfUnrecognized [, LookInUserFnsOnly ] ] )
      // Currently there are two alias functions, 'func1' (323) and 'func2' (324). WhichAlias must be either 1 or 2 accordingly.
      // The function sets whichever is indicated of R.SubstituteFn1 and ..2, and and R.SubstituteAt1 and ..2; these will get
      // substituted in R.RunAssignment(.) for 323 or 324 before those functions ever get to be called. Even though 'func1' and
      // 'func2' are system functions, they may be aliasses for either system or user functions.
      //  If FnName is empty, scalar or an array of only spaces or zeroes, it resets R.Subst... fields back to null values.
      // RETURNED + ACTION DONE:
      // (a) FnName identified: returns 1 for system fn., 2 for user fn., and sets up the appropriate R.Substitute.. fields.
      // (b) FnName holds nonspace chars., but is not identified: If 'DontCrash..' not set, crashes. Otherwise returns 0, does nothing else.
      // (c) FnName is empty, any scalar, or converts to a string reducing to "" after trimming: puts the null values into R.Subst..,
      //      and returns -1. (In this case, must have only one argument, or will be seen as a case of (b).)
      // Note, from boolean testing point of view, that it returns FALSE only in the case that nonspace chars. were supplied, but
      //  that supplied name was not identified.
      { 
        double whichAlias = Args[0].X;
        if (whichAlias != 1.0  &&  whichAlias != 2.0) return Oops(Which, "1st. arg. must be scalar, with value 1 or 2");
        bool pleasedontcrash = (NoArgs > 2 && Args[2].X != 0);
        bool userfnsonly = (NoArgs > 3 && Args[3].X != 0); 
        string ss = StoreroomToString(Args[1].I, true, true); // return the input string, trimmed, or "" if scalar.
        // If a single empty argument, null the R.Subst.. fields and leave. (> 1 args: treated as any other unrecognized name.)
        if (ss == "" && NoArgs == 2)
        { if (whichAlias == 1.0) { R.SubstituteFn1 = 0; R.SubstituteAt1 = -1; }
          else { R.SubstituteFn2 = 0; R.SubstituteAt2 = -1; }
          result.X = -1;  break;
        }
        // Is it a system function?
        int substfn = -1, substat = -1;
        if (!userfnsonly) 
        { substat = SysFnNames.IndexOf(ss);
          if (substat != -1) substfn = P.SysFnMarker;
        }  
        // In any case, look through user functions, if no find yet:
        if (substat == -1)
        { for (int i=1; i < P.UserFn.Length; i++) // recall that UserFn[0] is in fact the main program, hence start with i=1.
          if (ss == P.UserFn[i].Name) { substfn = P.UserFnMarker;  substat = i;  break; }
        }        
        // A find:,
        if (substat != -1)
        { if (substfn == P.SysFnMarker) result.X = 1;  else result.X = 2; 
          if (whichAlias == 1.0)
          { R.SubstituteFn1 = (short) substfn;   R.SubstituteAt1 = (short) substat; }
          else { R.SubstituteFn2 = (short) substfn;   R.SubstituteAt2 = (short) substat; }
        }
        else if (!pleasedontcrash) 
        { ss = ""; if (userfnsonly) ss = " user";  return Oops(Which, "unidentified {0} function name", ss); }
        break;
      }
      case 323:  case 324: // FUNC1 and FUNC2 (any no. of args., which are all ignored ): USUALLY NOT USED WHEN CALLED!
      // When this fn. reference is found in the assignment at runtime, the function stored by the above function - "setfunc(.)"
      // - is substituted for it. If 'setfunc' was never called, or has been used to reset the function ref. to null, then this
      // function will indeed run, but as it is deliberately an empty fn., all it will do is return a scalar 0.
      // *** NB! If ever changing these function numbers, you MUST also change the static field of class F called 
      // 'AliasFn1Index' and 'AliasFn2Index'!
      {
        break;
      }
      case 325: // BITOP(array WhichOpn, 1 or more variables). Operations: "&", "|", "^", "1", "2".
      // These functions are bitwise: AND, OR, XOR, 1's COMPLEMENT and 2's COMPLEMENT.
      // Standard C# functions are used, which do the bitwise operations on hex versions of integers.
      // Values are all rounded and converted to Int64. They may be negative, but must be inside the limits -1e15 to +1e15 
      //  (not inclusive), i.e. have no more than 15 decimal digits of precision. (For larger values, conversions from
      //  type double could be subject to truncating error.)
      //  The operation is then performed in a chain: e.g. for 'and', the opn. is: scalar1 AND scalar2 AND scalar3 AND...
      // In the case of COMPLEMENTS, there must be only one scalar; for all the others, at least 2.
      { // Retrieve the operator:
        int opn = 0;
        int opslot = Args[0].I;
        if (opslot >= 0) opn = (int) R.Store[opslot].Data[0];
        // Retrieve the values, and convert them to Int64:
        double[] values = AccumulateValuesFromArgs(Args, 1, NoArgs-1);        
        int vallen = values.Length;  double x = 0;
        long[] intvalues = new long[vallen]; long lng = 0;
        for (int i=0; i < vallen; i++)
        { x = values[i];
          if ( Math.Abs(x) >= 1e15) return Oops(Which, "all values must have absolute value < 1E15");
          intvalues[i] = Convert.ToInt64(x);
        }                              
        if (opn == 0x31 || opn == 0x32) // '1', '2' - COMPLEMENTS:
        { if (vallen > 1) return Oops(Which, "complement operations ('1', '2') take only one value");
          if (opn == 0x31) result.X = (double) ~intvalues[0]; // every bit is simply complemented.
          else result.X = (double) -intvalues[0]; // the hex representation of a negative integer is the 2's complement of its +ve version.
          break; 
        }                        
        // Only binary operations are left.
        if (vallen == 1) return Oops(Which, "binary operations require at least two values");
        if (opn == 0x26) // '&' - AND:
        { lng = intvalues[0];
          for (int i=1; i < vallen; i++) lng = lng & intvalues[i]; 
        }
        else if (opn == 0x7C) // '|' - OR:
        { lng = intvalues[0];
          for (int i=1; i < vallen; i++) lng = lng | intvalues[i]; 
        }
        else if (opn == 0x5E) // '^' - XOR:
        { lng = intvalues[0];
          for (int i=1; i < vallen; i++) lng = lng ^ intvalues[i]; 
        }
        else return Oops(Which, "unrecognized operation");
        // Valid value from binary operation:
        result.X = (double) lng;   break;
      }
      //case 326: // EXEC (array FileAndPath [, one or more chars. arrays Arguments [, scalar WaitingMsecs ] ] ). ####OLD

      case 326: // EXEC (.) - Starts a new process, optionally either letting it run independently or waiting for it to colse. Two modes,
      // which differ only in the way in which command line arguments are presented:
      // Mode 1: EXEC(single array WholeCommand [, scalar WaitingMsecs ] ). The one chars. array gets very little parsing here - just
      //   dissects out the file path-and-name, which is delimited by the first space(s) (or by the end of the array, if no such space);
      //   all stuff after that space becomes the arguments, and is not in any way parsed here. Moreover the file name is not checked
      //   as it indeed is in mode 2).
      // Mode 2: EXEC(array FileAndPath [, one or more chars. arrays Arguments [, scalar WaitingMsecs ] ] ). Such a command is broken down
      //   into separate arguments, so that Mono can handle them and then pass them on; and there are checks.
      // The underlying MONO will start a new process, in which it will run whatever is at FileAndPath (it can be a program or a shell
      //  script or a shell command). arrays 'Arguments' - if more than one - will be trimmed, then joined together by a space to form
      //  a single string, which will be sent to that program / shell / command as its due argument(s). (You can join them yourself with
      //  spaces within just a single Argument array, if you wish.)
      // Normally the function starts the process but doesn't hang around waiting for it to end (which it won't, if for example it has
      //  started up some other application which you want to stay around). However many scripts and commands are intended to be quickly
      //  over, and then perhaps to return an exit code. For this situation, supply a final scalar argument WaitingMsecs, and give it
      //  a value that rounds to at least 1 (or it will be ignored). If WaitingMsecs is valid, then the function waits in a loop till either
      //  completion of the process or till WaitingMsecs is up. (The process is not forced to exit in any circumstance.) See below re exit code.
      //  You should ALWAYS use a valid WaitingMsecs when calling a Bash command, unless you don't care about the exit code. For example,
      //  "exec("ls", "/boo/hoo")" will fail to list this nonexistent directory, but ls's exit code of 2 will only be available if you set
      //  WaitMsecs to a valid and sufficient value (e.g. 100 msecs).
      // In mode 2 (only), the abbreviation "~/" in either FileAndPath or ANY PART of any array in Arguments will be replaced by the home
      //  directory. If you don't want this to happen in an argument, prefix the "~" with the backslash: "\~/" will be taken as literal "~/".
      //  No other abbreviations are replaced here.
      // NB! In the case of shell commands, realize that you can't use codes that are Bash keywords rather than arguments - like '>' for
      //  redirection. I haven't yet found out a way of eliciting such commands (that would work in the terminal on one command line).
      //  The work-around would be to save a temporary Bash script and then run it with 'exec' (and then, perhaps, delete it).
      // RETURNED: If no valid final scalar argument, always returns 0 (i.e. behaves like a void function). If there is a valid such argument,
      //  then (a) If the process finished within the allowed time, the exit code from that program / script / command is returned (usually 0,
      //  if all went well; but that is decided entirely by that program). If it did not, then the exit code is 999 (which would be a most
      //  unusual code to be returned by any terminating called program).
      // ERRORS crash, though errors in the process itself will have no effect.
      // SPECIAL USAGE: Invoking another instance of MonoMaths. If you just want to open the instance empty, the two args. are
      //  "mono" and the program name - typically "~/MonoMaths/MonoMaths.exe". (This is exactly what would be required to run the
      //  program from the Linux terminal.) If you want it with a particular program in place, either just loaded or actually running,
      //  then add a third argument which is either exactly "load" or "run", and then a fourth argument, which is the path and file name
      //  of the program. (Actually this one is run through "_ParseFileName()" in the MainWindow instance constructor, so abbrevns.
      //  etc. are OK here.) Again, these 4 arguments entered into a terminal would have the same effect.
      {
        int waitMsec = 0; // don't wait.
        int noArrayArgs = NoArgs, scalarArgCue = 0;
        if (Args[NoArgs-1].I == -1) // final arg. scalar:
        { noArrayArgs--;  scalarArgCue = 1;
          waitMsec = Convert.ToInt32(Args[NoArgs-1].X);
          if (waitMsec < 0) waitMsec = 0;
        }
        string ss, filename = "", arguments = "";
        // Which mode?
        if (NoArgs - scalarArgCue == 1)
        { // Mode 1:
          ss = StoreroomToString(Args[0].I, true, true); // trims fore and aft
          if (ss == "") return Oops(Which, "1st. arg. is either scalar or is an empty array");
          // Find the first space:
          int ptr = ss._IndexOf(' ');
          if (ptr == -1) filename = ss; // and leave cmdLine empty
          else { filename = ss.Substring(0, ptr);  arguments = ss.Substring(ptr+1); }
        }
        else // Mode 2:
        { string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // no final '/'
          for (int i=0; i < noArrayArgs; i++)
          { ss = StoreroomToString(Args[i].I, true, true);
            // Replace the home directory code:
            int ptr = ss._IndexOf("~/");
            if (ptr >= 0)
            { if (ptr == 0 || ss[ptr-1] != '\\')
              { ss = ss._Scoop(ptr, 1, homeDir); } // '1' because we need the '/' to stick onto the back of homeDir.
              else ss = ss.Remove(ptr-1, 1); // remove the backslash but leave the "~/"
            }
            // File+path:
            if (i == 0)
            { filename = ss;
              if (ss == "") return Oops(Which, "the 1st. arg. is not a file name"); // It was either scalar or consisted just of spaces.
            }
            // Arguments:
            else
            { if (arguments != "") arguments += " "; // delimiting space between arguments.
              arguments += ss;
            }
          }
        }
       // Make it happen:
        Quid quickie = JS.RunProcess(filename, arguments, waitMsec);
        if (!quickie.B) return Oops(Which, quickie.S);
        else result.X = quickie.IX; // the exit code emitted by whatever was run, if waitMsec >= 1; otherwise just 0.
        break;
      }          
     case 327: // KILL_ON_EXIT() -- After focus encounters this void fn., the next closure of the program through 'exit;' or
      // through reaching end of main program code will be followed by MiniMaths instance closure. Fails if the program closes
      // through an error crash, or through a call to 'crash(.)'. (NB - Don't change this! Otherwise whatever invoked the instance -
      // usually a preexisting MiniMaths instance - has no direct way of knowing whether the instance ended properly or through an error.)
      // The function is valid wherever met with, e.g. in a user function; but of course the pgm. cannot be exited without crash from a user fn.
      { MainWindow.ShutDown = true;  return result; // The code will still run. If the program was started from
             // a keypress, the rest of the keypress handler runs; if from the menu, the rest of the menu handler.
             // This is not my fault; this is just how C# works. It then closes down the main form, before finally shutting.
      }  
      case 328: 
//////       TABLE: Two different versions, distinguished by the no. of arguments:
//////       (1) TABLE(scalar TableID, array FileName) -- tries to load a properly formatted Grid and Table from a file.
//////       (2) TABLE(scalar TableID, matrix Data, array ColumnHeaders) -- sets up a new table with given column headers and table data.
//////       In both cases, if TableID exists, that form's previous occupant is executed and the new grid and table go into it; otherwise
//////        a new form is created. Either way, TableID returns.
//////       CASE 1: The file must be properly formatted - see J_Grid.cs for formatting details. If you saved the table via the grid form's
//////        menu, it is properly formatted.
//////       CASE 2: The rows of Data will be the rows of the table, so dimension 0 sets the number of columns. If the third argument is
//////        scalar, column headings will be blank; if a list array, it will be converted to a string and then split around the TAB character
//////        (\u0008). If a matrix, rows will be taken as separate column headers (after trimming of chars. 0 to 32). 
//////        If the number of column headers either way is not exactly right, any shortfall will be replaced by blank headers, any excess 
//////        will be ignored (no error raised).
// ############ THE ABOVE IS OF HISTORIC INTEREST ONLY, as the new version is being written...
      // TABLE (chars. array LayoutString) -- returns table ID.
      // LayoutStr: E.g. "LL|#[27,10]|LL". Spaces ignored. '|' starts a new horizontal layer. 'L' is label with wrap, 'l' with no wrap.
      // 'T' an entry box. '#' a table, which must be followed by "[<no. rows>, <no. cols>]". 
      {
//        string layoutStr = StoreroomToString(Args[0].I);   if (layoutStr == "") return Oops(Which, "1st. arg. must be an array");
//        layoutStr = layoutStr._Purge();
//        string[] layout = layoutStr.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
//        // Elements of 'layout' are now guaranteed to be nonempty and to contain no chars. below unicode 33.
//        Tablet Mensa = null;
//        try
//        { Mensa = new Tablet("THIS IS THE TITLE", layout, true, -1, -1, "FOO|BAR");  }
//        catch (Exception e)
//        { string ss = CleanThrowOutput(e.ToString()); // amputate unnec. junk supplied by Mono.
//          return Oops(Which, "could not create table: " + ss);
//        }
//      
//
//        result.X = (double) Mensa.TabletID;
        break;
      }
        
//        Mensa.ShowMe();

//        Tablet Mensa = new Tablet(0, 0, 3, "This is the Window Header", "The Left Header", "The Right Header", 0);
/*
        Tabula Mensa = Tabula.GrabTabula(tableID); // If the Tabula form does not yet exist, Mensa will be NULL. We wait
        if (NoArgs == 2)
        { int filenmSlot = (int) Args[1].X;
          if (filenmSlot == -1){ result.S="'table', file-loading version: no file name has been supplied."; result.B = false; return result; }
          string flnm = StoreroomToString(filenmSlot, true, true);
          if (Mensa == null)
          { try { Mensa = new Tabula(MainWindow.ThisWindow, flnm); }
            catch { result.S = "'table', file-loading version: could not load file."; result.B = false; return result; }
          }
          else
          { result.B = Mensa.ReplaceGrid(flnm);
            if (!result.B) { result.S = "'table', file-loading version: could not load file.";  return result; }
          }
        }
        else // NoArgs is 3:
        { int dataSlot = Args[1].I;
          if (dataSlot == -1 || R.Store[dataSlot].DimCnt != 2){ result.S="'table' 2nd. argument must be a matrix"; result.B = false; return result; }
          int[] dataDims = R.Store[dataSlot].DimSz;
          int noRows = dataDims[1],  noCols = dataDims[0];
          if (noCols < 2 || noCols > 50){ result.S="'table': no. columns must lie between 2 and 50"; result.B = false; return result; }
          double[] inData = R.Store[dataSlot].Data;
//############## REWRITE THIS METHOD to allow for string columns, and in the process ADAPT Table.RowFromNumbersOnly, called 26 lines below
          Table table = new Table(noCols, null);//#######Eh? What's this NULL bit?
          string[] colHeadings = new string[noCols];
          for (int i=0; i < noCols; i++) table.ColDataType[i] = 'N';
          if (NoArgs > 2)
          { int hdgSlot = Args[2].I;
            int dim1 = R.Store[hdgSlot].DimSz[1];
            bool isListArray = (dim1 == 0);
            string[] hdgs;
            if (isListArray)
            { string stroo = StoreroomToString(hdgSlot); // untrimmed string.
              hdgs = stroo.Split(new char[] {'\t'}); // there will be one almighty long heading if there are no tabs in the string.
            }
            else // assume a matrix: (too bad, if there are more than 2 dims.)
            { int dim0 = R.Store[hdgSlot].DimSz[0];
              hdgs = new string[dim1];
              for (int i=0; i <dim1; i++)
              { hdgs[i] = StoreroomToString(hdgSlot, i*dim0, dim0, true, true); // bools = headings are trimmed of chars. 0 to 32.
              }
            }
            int minnow = Math.Min(hdgs.Length, noCols);
            for (int i=0; i < minnow; i++) colHeadings[i] = hdgs[i];
          }
          Boost outcome;
          double[] rowData = new double[noCols];
          for (int i=0; i < noRows; i++)
          { Array.Copy(inData, i*noCols, rowData, 0, noCols);
            table.Data.SetRow(Table.RowFromNumbersOnly(table, rowData, out outcome), -1, 'A'); // append the new row
          }
          table.ColHdrText = colHeadings;
          DGVClass DGV = new DGVClass(noCols, ref table);
          if (Mensa == null)
          { Mensa = new Tabula(MainWindow.ThisWindow, DGV, "C:/MiniMaths/Tables");
          }
          else // table identified:
          { Mensa.ReplaceGrid(DGV);
          }
        }
        Mensa.Visible = true;
        result.X = Mensa.ID;
        */

      case 329: // TABLEGET: Two forms, based on the no. of args., 3 or 6 (always scalar):
      // Case 1 - TABLEGET(scalar TableID, scalar Row, scalar Column):
      //  Always returns a list array. For cells of type 'N' or 'D', it always has length 1; for the remaining types 'S' and '=',
      //  it always has length 2 or more (empty or single-char. strings being padded with terminal space(s)).
      //  NO latitude allowed for Row / Col args, which MUST refer to an existing location.
      // Case 2 - TABLEGET(scalar TableID, scalar FirstRow, scalar LastRow, scalar FirstColumn, scalar LastColumn, scalar StringMarker):
      // Always returns a matrix, being a subset of the table. If the whole table, it will be the table reproduced as a matrix. If a
      //  rectangular subset of the table, it will be a matrix of the size of the rectangle. If a single column of length N, it will be
      //  an Nx1 matrix; if a single row, a 1xN matrix; if a single cell, a 1x1 matrix. Any cell that is of type 'S' or '=' will have
      //  StringMarker substituted for its contents. The literal contents cannot be retrieved using this version; case 2 must be used.
      //  Row / Col args: LastRow excessive or negative (usually use -1), sets to the last row of the table; LastColumn analogously.
      //   but FirstRow or firstCol out of range is not tolerated (error).
      // For both cases, dates are returned as the double version of what JS.DateToString(.) produces.
      // ERRORS raised if (1) arguments are not scalar; (2) the table is not identifiable; (3) pointers cross; (4) [in case 2, after 
      //  automatic row/col adjustments:] Row or Col arg. is out of range.
      { break; //########### NOTHING DOING AT PRESENT ###########
//  int n = F.ArgTypesCode(Args);
//        bool isShortForm = (n == 111);
//        if (!isShortForm && n != 111111) {result.S = "'tableget': must be exactly 3 or 6 args. which are all scalar"; result.B = false; return result; }
//        Tabula Mensa = Tabula.GrabTabula( (int) Args[0].X);
//        if (Mensa == null) { result.B = false;  result.S = "'tableget': can't identify the table"; return result; }
//        Table table = Mensa.DGV.CurrentTable;
//        if (isShortForm)
//        { int thisRow = (int) Args[1].X,  thisCol = (int) Args[2].X;
//          if (thisRow < 0 || thisRow >= table.Data.DataLength)
//          { result.B = false;  result.S = "'tableget': row arg. (" + thisRow.ToString() + ") out of range"; return result; }
//          if (thisCol < 0 || thisCol >= table.NoCols)
//          { result.B = false;  result.S = "'tableget': column arg. (" + thisCol.ToString() + ") out of range"; return result; }
////############## Skip the above tests, using the tests of 'GetData' below, instead.
//          double[] outstuff = TCell.ToDoubleArray(table.Data.GetData(thisRow, thisCol) );
//          result.I = V.GenerateTempStoreRoom(outstuff.Length);
//          R.Store[result.I].Data = outstuff;
//          break;
//        }
//        // To get here, this must be Case 1 - the 'long form':
//        int firstRow = (int) Args[1].X,  lastRow = (int) Args[2].X, firstCol = (int) Args[3].X,  lastCol = (int) Args[4].X;
//        int rowCount = table.Data.DataLength,  colCount = table.NoCols;
//       // adjust errant arguments as possible:
//        if (firstRow < 0 || firstCol < 0)
//        { result.B = false;
//          result.S = "'tableget': either FirstRow (" + firstRow.ToString() + ") or FirstCol(" + firstCol.ToString() + ") is negative"; return result;
//        }
//        if (lastRow < 0 || lastRow >= rowCount) lastRow = rowCount-1;
//        if (lastCol < 0 || lastCol >= colCount) lastCol = colCount-1;
//        int extentRows = lastRow - firstRow + 1,   extentCols = lastCol - firstCol + 1;
//        if (extentRows < 1)
//        { result.B = false;  result.S = "'tableget': row pointers (" + firstRow + ", " + lastRow + ") cross"; return result; }
//        if (extentCols < 1)
//        { result.B = false;  result.S = "'tableget': column pointers (" + firstCol + ", " + lastCol + ") cross"; return result; }
//        double[] data = new double[extentRows * extentCols];
//        int offset;
//        double stringMarker = Args[5].X;
//        for (int rw = firstRow; rw <= lastRow; rw++)
//        { offset = extentCols * (rw - firstRow); // offset relative to the output matrix, not to the table
//          for (int cl = firstCol; cl <= lastCol; cl++)
//          { TCell cell = table.Data.GetData(rw, cl);
//            n = offset + cl - firstCol;
//            if      (cell.Kind == 'N')  data[n] = cell.N;
//            else if (cell.Kind == 'D')  data[n] = (double) JS.DateToInteger(cell.D);
//            else                        data[n] = stringMarker;
//          }
//        }
//        int outslot = V.GenerateTempStoreRoom(extentCols, extentRows);
//        data.CopyTo(R.Store[outslot].Data, 0);
//        result.I = outslot;  break;
      }
      case 330: // TABLESET: Two forms, based on form of last arg. Both forms VOID. This is because in both forms, ALL ARG. ERRORS CRASH;
      // no arg. adjustments are made. So you don't need a boolean return for 'success' - if the program is still going, you had success!
      // Case 1 - TABLESET(scalar TableID, scalar Row, scalar Column, scalar/array Value):
      // Distinguished by the last arg. being EITHER a scalar OR one of: (list array, 1xN vector, Nx1 vector).
      // No latitude for incorrect arguments, which always cause a crash. 'Value' must fit the cell type. If 'D' or 'N', it must
      // be scalar; if 'S' or '=', it must be an array (whatever its form, it is taken as a list array of chars. type). If 'D',
      // it must be convertible to a date using JS.DateFromInteger(.).
      // Case 2 - TABLESET(scalar TableID, scalar FirstRow, scalar FirstColumn, matrix Data )
      // Distinguished by the last arg. being a MATRIX MxN where BOTH M and N are 2 or more:
      // the matrix must be able to overlay the table without overlap. If cell type is 'D' or 'N', data is handled as for Case 1. If cell
      // type is '=' or 'S', however, the datum - a single element of the input matrix - is interpreted as a character, and that char.
      // is implanted. (There is no way of implanting, say, a column of strings, other than by recurrently calling the Case 1 form.
      // MiniMaths really isn't about to be optimized for string-handling - it is a maths program.)
      // NB - Case 2 can't be used to append data to the table.
      { // ################ NOTHING DOING AT PRESENT ###########################
//      {// Collect all the arguments up front:
//        Tabula Mensa = Tabula.GrabTabula( (int) Args[0].X);
//        if (Mensa == null) { result.S = "'tableset': can't identify the table";  result.B = false;  return result; }
//        int n = F.ArgTypesCode(Args);
//        if (n > 1112) // only possibilities not included are 1111 and 1112
//        { result.S = "'tableset': argument scalar/array typing is wrong somewhere";  result.B = false;  return result; }
//        int row = (int) Args[1].X, column = (int) Args[2].X;
//        Table table = Mensa.DGV.CurrentTable;
//        int rowCount = table.Data.DataLength,  colCount = table.NoCols;
//        if (row < 0 || row >= rowCount){ result.S = "'tableset': row number (" + row.ToString() + ") is out of range";  result.B = false;  return result; }
//        if (column < 0 || column >= colCount){ result.S = "'tableset': column number (" + column.ToString() + ") is out of range";  result.B = false;  return result; }
//        int dataSlot = Args[3].I;
//        // Decide between case types:
//        double datum=0;
//        int[] dims = null;
//        bool isCase1 = (Args[3].I == -1); // will end up TRUE if 4th. arg is scalar OR list array or 1xN or Nx1.
//        if (isCase1) datum = Args[3].X;
//        else
//        { dims = R.Store[dataSlot].DimSz;
//          if (dims[2] != 0){ result.S = "'tableset': data array cannot have more than 2 dimensions";  result.B = false;  return result; }
//          isCase1 = (dims[1] <= 1 || dims[0] == 1);
//          datum = R.Store[dataSlot].Data[0];
//        }
//        double n_data;  DateTime d_data;  string s_data;  Boost boo;
//        // CASE 1:
//        if (isCase1)
//        { char kind = table.Data.GetData(row, column).Kind;
//          n_data = 0;  d_data = DateTime.MinValue;  s_data = null; // needed as dummy args. for .Implant(.) below.
//          if (dataSlot == -1) // scalar argument:
//          { if (kind == 'N') n_data = datum;
//            else if (kind == 'D')
//            { d_data = JS.DateFromInteger( (int) datum, out result.B);
//              if (!result.B) { result.S = "'tableset': could not translate " + datum.ToString() + " to a date"; return result; }
//            }
//            else { result.B = false; result.S = "'tableset': data mismatch (scalar data for a string-type table cell)"; return result; }
//          }
//          else // array supplied:
//          { if (kind == 'S' || kind == '=') s_data = StoreroomToString(dataSlot, true, true); // bools = trim both ends.
//            else { result.B = false; result.S = "'tableset': data mismatch (array data for a numerical or date-type table cell)"; return result; }
//          }
////######## REPLACE THIS WITH A CALL TO OTHER METHODS FOR INSTALLING CELL OR ROW DATA (table.Data.SetRow, SetData).
//////          boo = Mensa.DGV.Implant(n_data, d_data, s_data, kind, row, column);
//boo.B = true; boo.S = ""; //############### To stop the compiler complaining till I can get back to repair this stuff.
//          if (!boo.B) { result.B = false; result.S = "'tableset': " + boo.S; return result; } // should never occur, after above checks.
//          break;
//        }
//        // CASE 2: (if got here)
//        double[] MxData = R.Store[dataSlot].Data;
//        int firstTableRow = row, firstTableCol = column; // just to make the program more readable below.
//        int cellRow, cellCol;
//        int noMxRows = dims[1], noMxCols = dims[0];
//        if (firstTableRow + noMxRows > rowCount){ result.B = false; result.S = "'tableset': data would exceed last row of table"; return result; }
//        if (firstTableCol + noMxCols > rowCount){ result.B = false; result.S = "'tableset': data would exceed last column of table"; return result; }
//        for (int rw = 0; rw < noMxRows; rw++)
//        { int MxOffset = rw * noMxCols;
//          for (int cl=0; cl < noMxCols; cl++)
//          { n_data = 0;  d_data = DateTime.MinValue;  s_data = null; // needed as dummy args. for .Implant(.) below.
//            datum = MxData[MxOffset + cl];
//            cellRow = firstTableRow + rw;   cellCol = firstTableCol + cl;
//            TCell cell = table.Data.GetData(cellRow, cellCol);
//            char kind = cell.Kind;
//            if (kind == 'N') n_data = datum;
//            else if (kind == 'D')
//            { d_data = JS.DateFromInteger( (int) datum, out result.B);
//              if (!result.B) { result.S = "'tableset': could not translate " + datum.ToString() + " to a date"; return result; }
//            }
//            else s_data = ( ( (int) datum)._ToChar(32) ).ToString(); // type 'S' or '='. Values outside the range 0 to 0xffff default to space (char. 32).
////######## REPLACE THIS WITH A CALL TO OTHER METHODS FOR INSTALLING CELL OR ROW DATA (table.Data.SetRow, SetData).
//////            boo = Mensa.DGV.Implant(n_data, d_data, s_data, kind, cellRow, cellCol);
//boo.B = true; boo.S = ""; //############### To stop the compiler complaining till I can get back to repair this stuff.
//            if (!boo.B) { result.B = false; result.S = "'tableset': " + boo.S; return result; } // should never occur, after above checks.
//          }
//        }
        break;
      }  
      case 331: // PLACETABLE(scalar TableID , One value [, a 2nd. value] ) -- two forms, depending on the number of values:
      // Form (1): 'placetables(TableID, any value)' - only one value after the ID. If that value is nonzero, the table is maximized;
      //  if zero, it is put into 'normal' mode and goes wherever Windows feels like putting it.
      // Form (2):  'placegraphs(TableID, X, Y)' - two values after the ID. They decree the table form's top left corner. 
      //  If either value is negative, the form is centred in that dimension; e.g. "(placegraphs(ID, -1, 50)" centres it horizontally 
      //  and puts the top edge 50 pixels from the screen top; (..-1, -1) centres the form in the screen.
      // No errors crash. If X or Y are too high, they are adjusted back to put the box at the extreme left and/or bottom.      
      { // ################ NOTHING DOING AT PRESENT ###########################
//      { double[] values = AccumulateValuesFromArgs(Args);
//        Tabula Mensa = Tabula.GrabTabula( (int) values[0]);  if (Mensa == null) break;
//        if (values.Length == 2) // maximize or 'normalize':
//        { if (values[1] != 0.0) Mensa.WindowState = System.Windows.Forms.FormWindowState.Maximized;
//          else Mensa.WindowState = System.Windows.Forms.FormWindowState.Normal;
//        }
//        else
//        { Mensa.WindowState = System.Windows.Forms.FormWindowState.Normal;
//          int X = (int) values[1], Y = (int) values[2];
//          int hSlack = MainWindow.ScreenWidth - Mensa.Width, vSlack = MainWindow.ScreenHeight - Mensa.Height;
//          if (X < 0) X = hSlack / 2;  else if (X > hSlack) X = hSlack;
//          if (Y < 0) Y = vSlack / 2;  else if (Y > vSlack) Y = vSlack;
//          Mensa.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
//          Mensa.Location = new Point(X, Y);
//        }
        break;
      }  
      case 332: // KILLTABLE(any no. values as scalars or arrays) -- For every value that can be identified, that table will be slaughtered.
      // Values not corresponding to table IDs are ignored, EXCEPT that if there is a single scalar argument 0 - usually invoked by
      // "killtable();" - then ALL tables extant will be destroyed.
      { // ################ NOTHING DOING AT PRESENT ###########################
//      { double[] doomed = AccumulateValuesFromArgs(Args);
//        if (doomed.Length == 1 && doomed[0] == 0.0) // then kill the lot:
//        { Tabula.DisposeOfAll();  break; }
//        // List of the doomed supplied:
//        for (int i=0; i < doomed.Length; i++)
//        { Tabula.DisposeOf( (int) doomed[i]); } // no need to get the boolean return; if no such tabula form, no harm done.
        break;
      }  
      case 333: // TABLEAPPEND(scalar TableID, variable Dummy) -- If one argument, appends a data row to the table; if two, appends a
      // subheading row, independent of the type or value of that second argument. If the table exists, returns 1; otherwise 0 (and
      // does nothing). Note that there is no function (yet) for appending a row filled with data; you have to add an empty row, then
      // fill it using 'tableset(.)'.
      { break; // ################ NOTHING DOING AT PRESENT ###########################
//      { Tabula Mensa = Tabula.GrabTabula( (int) Args[0].X);   if (Mensa == null) break;
//        if (NoArgs == 1) Mensa.DGV.SetGridAndTableRow(null, -1, 'A'); // empty data row appended
//        else Mensa.DGV.SetGridAndTableRow(Mensa.DGV.CurrentTable.Data.EmptySubheadingRow(), -1, 'A'); // empty subheading row appended.
//        result.X = 1.0;    break;
//      }
//      case 334: //TABLECALL(scalar TableID) -- If the table does not exist, returns an array of size 1, value 0, isDummyArray TRUE.
//      // If the table exists but there have been no calls, it still returns an array of size 1, but [0] is -1, and isDummyArray remains false.
//      // If a call found on stack, an array is returned of length at least , and with the following elements:
//      // [0] = action no. (an integer  >= 1); [1] = row no., [2] = column no.; [3] = no. calls remaining on the stack (this one having
//      //  been automatically removed by this call); [4] = cell kind: 0 for string, 1 for number, 2 for date. If a number or date,
//      // [5] is that number or date (the date being translated into an integer by JS.DateToInteger(.)). If a string, [5] onwards contains
//      // the string (i.e. its unicode values). If the table contained an empty string, [5] will be 0.
//      // Note that subheading rows don't raise edit calls.
//      { int outslot;
//        Tabula Mensa = Tabula.GrabTabula( (int) Args[0].X);
//        if (Mensa == null) { result.I = V.GenerateTempStoreRoom(1);  IsDummyArray = true;  break; }
//        int row, column, action;  TCell cell;
//        int noLeft = Mensa.DGV.PopOnEditCall(out row, out column, out cell, out action);
//        if (noLeft == -1) { result.I = V.GenerateTempStoreRoom(1);  R.Store[result.I].Data[0] = -1;  break; }
//        // A call found on the stack:
//        if (cell.Kind == 'N' || cell.Kind == 'D')
//        { outslot = V.GenerateTempStoreRoom(6);
//          double[] doo = R.Store[outslot].Data;
//          doo[0] = action;  doo[1] = row;  doo[2] = column;  doo[3] = noLeft;
//          if (cell.Kind == 'N'){ doo[4] = 1.0;  doo[5] = cell.N; }
//          else { doo[4] = 2.0;  doo[5] = JS.DateToInteger(cell.D); }
//        }
//        else // kind 'S':
//        { string ss = cell.S;  if (ss == "") ss = ('\u0000').ToString();
//          double[] diddle = StringToStoreroom(-1, ss);
//          outslot = V.GenerateTempStoreRoom(5 + diddle.Length);
//          double[] doo = R.Store[outslot].Data;
//          doo[0] = action;  doo[1] = row;  doo[2] = column;  doo[3] = noLeft;  doo[4] = 0.0;
//          Array.Copy(diddle, 0, doo, 5, diddle.Length);
//        }
//        result.I = outslot;  break;
      }
      case 335: //TABLEINFO(scalar TableID, array WhichInfo) -- Always returns an array. If the table does not exist, returns an array 
      // of size 1, value 0, isDummyArray TRUE. If WhichInfo not a legitimate value, simply crashes. In all other cases returns the
      // array dictated by WhichInfo. Values give the following returns ('[..]' indicates optional extensions; only the part before this
      // is checked). All are case-sensitive, sadly.
      // 'a[ccess]' -- array of letters, no delimiters ('N', 'E' 'F')
      // 'd[imensions]' -- [0] = no. rows, [1] = no. columns in the table (one less than in the visible grid).
      // 'e' -- OnEdit cues for column entries (0 = no cue; 1+ = no. of cue)
      // 'f[ormat]' -- column formatting strings,  delimited by '\t' (char. 9).
      // 'h[eadings]' -- array of chars., delimited by '\t' (char. 9).
      // 'iv' [indent values] -- array of DGV.Indent[col].I integers; to be coupled with...
      // 'is' [indent strings] -- array of DGV.Indent[col].S strings, delimited by the TAB char.; to be coupled with the above.
      // 's[uppress default]' -- array of 1 (true) and 0 (false).
      // 't[runcate]' -- array copying the integers from table's ColTruncate array; truncates in the table proper (not just the display).
      // 'w' -- column width ratios.
      { break; // ################ NOTHING DOING AT PRESENT ###########################
//      { Tabula Mensa = Tabula.GrabTabula( (int) Args[0].X);
//        if (Args[1].I == -1 || Mensa == null) { result.I = V.GenerateTempStoreRoom(1);  IsDummyArray = true;  break; }
//        double[] whichInfo = R.Store[Args[1].I].Data;
//        int c1 = (int) whichInfo[0];
//        double c2;
//        if (whichInfo.Length > 0) c2 = whichInfo[1]; else c2 = 0.0;      
//        string ss;  int n;
//        double[] outdata = null;
//        Table table = Mensa.DGV.CurrentTable;
//        bool allOk = true; // NB: Put in 'allOk = false;' at the end of each 'case' containing options based on c2.
//        switch (c1)
//        { case 97: // 'a': column access
//            ss = Mensa.DGV.ColumnEditable._ToString();
//            outdata = StringToStoreroom(-1, ss);   break;
//          case 100: // 'd':
//            if      (c2 == 105.0) outdata = new double[] {table.Data.DataLength, table.NoCols}; // "di[mensions of the table]"
//            else if (c2 == 97.0) outdata = StringToStoreroom(-1, table.ColDataType._ToString());// "da[ta type}"
//            else allOk = false; 
//            break;
//          case 101: // 'e': OnEdit cues (0 = no cue, 1+ is cue no.)
//            outdata = Mensa.DGV.OnEdit._ToDubArray();  break;
//          case 102: //'f': Format strings for grid display of data
//            ss = String.Join("\t", table.ColFmt);
//            outdata = StringToStoreroom(-1, ss);   break;
//          case 104: // 'h': Column headings
//            ss = String.Join("\t", table.ColHdrText);
//            outdata = StringToStoreroom(-1, ss);   break;
//          case 105: // 'i': Indenting data;
//            if (c2 == 118.0)  // "i[ndent] v[alue]": .I fields of the Strint objects
//            { outdata = new double[table.NoCols];
//              for (int i=0; i < outdata.Length; i++) outdata[i] = (double) Mensa.DGV.Indent[i].I;
//            }
//            else if (c2 == 115.0) //"i[ndent] s[tring]": .S fields of the Strint objects
//            { string[] stroo = new string[table.NoCols];
//              for (int i=0; i < stroo.Length; i++) stroo[i] = Mensa.DGV.Indent[i].S;
//              outdata = StringToStoreroom(-1, String.Join("\t", stroo));
//            }
//            else allOk = false; 
//            break;
//          case 115: // 's' : Suppress default (true = don't show the value in the grid, if it is the default value)
//            bool[] sup = table.SuppressDef;  n = sup.Length;
//            outdata = new double[n];
//            for (int i=0; i < n; i++) if (sup[i]) outdata[i] = 1.0;
//            break;
//          case 116: // 't': Truncating of table data
//            outdata = table.ColTruncate._ToDubArray();  break;
//          case 119: // 'w': column width ratios
//            outdata = Mensa.DGV.WidthFraction; break;
//            
//            
//          default: allOk = false; break;
//        }
//        if (!allOk) { result.I = V.GenerateTempStoreRoom(1);  IsDummyArray = true;  break; }
//        result.I = V.GenerateTempStoreRoom(outdata.Length);
//        R.Store[result.I].Data = outdata;  break;
      }
      case 336: // TABLESETPARAM(scalar TableID, array WhichParam, values NewValues) -- VOID.
      // Sets a parameter. Errors all crash, so it may be helpful to use 'errortrap(.)' to avoid a program crash.
      // NewValues will be collected into a single double array. It must have the length required by the parameter. Its char. rating
      //  is not checked; values are interpreted in the way that WhichParams dictates.
      // WhichParam values are as follows. ('[..]' indicates optional extensions; only the part before this is checked. Case-sensitive.)
      // 'a[ccess]' -- NewValues = array made up of letter unicodes of 'N', 'E' 'F' only (no delimiter).
      // 'e' -- NewValues = OnEdit cues for column entries (0 = no cue; 1+ = no. of cue)
      // 'f[ormat]' -- NewValues = column formatting strings,  delimited by '\t' (char. 9).
      // 'h[eadings]' -- NewValues = array of chars., delimited by '\t' (char. 9).
      // 'i'[indent] -- NewValues = array made up as follows: there are as many sequences as there are columns, the sequences being
      //    delimited by '|' (character 124); each sequence starts with an integer (would rarely be more than 10, should never anywhere near
      //    124) - Indent[i].I - and continues with Indent[i].S chars. (The usual delim '\t' can't be used, as Indent[i].I can be 9.)
      // 's[uppress default]' -- NewValues = array of 1 (true) and 0 (false).
      // 't[runcate]' -- NewValues = array of integers, defining how the table proper will truncate numbers (not just the display).
      // 'w' -- NewValues = column width ratios (the sum of all values can be any no.){
      { // ################ NOTHING DOING AT PRESENT ###########################
//      {
//        Tabula Mensa = Tabula.GrabTabula( (int) Args[0].X);   
//        if (Mensa == null) { result.S = "'tablesetparam': cannot identify table";  result.B = false;  return result; }
//        DGVClass grid = Mensa.DGV;
//        Table table = Mensa.DGV.CurrentTable;
//        int noCols = table.NoCols;
//        double[] values = AccumulateValuesFromArgs(Args, 2);
//        int valuesLen = values.Length;
//        bool wrongWhichParam = (Args[1].I == -1); // error message further down, as later steps may also set this boolean to true.
//        int errno = 0;  string ss;
//        if (!wrongWhichParam) 
//        { int ch = (int) R.Store[Args[1].I].Data[0]; // first letter of the 3rd. arg. (char. array)
//          switch (ch)
//          {
//            case 119: // 'w' -- column width ratios
//              if (valuesLen != noCols) { errno = 10; break; }
//              for (int i=0; i < valuesLen; i++) { if (values[i] <= 0.0) { errno = 20; break; } }
//              Mensa.DGV.ChangeParameter("width ratios", values); // ignore return value, as we have sifted out possible errors.
//              break;
//            case 104: // 'h' -- column headings
//              int[] delims = values._FindAll(9.0);
//              if (delims.Length != noCols-1) { errno = 30; break; }
//              ss = DubArrToChars(values, -1, 0); //######### rework args., as this method now takes different args.
//              Mensa.DGV.ChangeParameter("headings", ss); // ignore return value, as we have sifted out possible errors.
//              break;
//
//            default: wrongWhichParam = true; break;
//          }
//        }  
//        if (wrongWhichParam) { result.S = "'tablesetparam': cannot identify the type of parameter (2nd. argument)"; return result; }  
//        if (errno != 0)
//        { result.S = "'tablesetparam': ";
//          if      (errno == 10) result.S += "values argument should have length = no. columns";
//          else if (errno == 20) result.S += "values argument should not contain zero or neg. values";
//          else if (errno == 30) result.S += "values argument should contain (no. columns - 1) delimiters";
//          result.B = false;  return result;
//        }
        break;
      }
      case 337: // ISTABLE(scalar TableID) -- returns 1.0 if the table exists, otherwise 0.0.
      { // ################ NOTHING DOING AT PRESENT ###########################
//      { if (Tabula.GrabTabula( (int) Args[0].X) != null) result.X = 1.0;
        break;
      }        
      case 338: // TABLISTGET(scalar TableID, scalar/array ListID, scalar ItemNo, scalar/array FieldID):
      //  Always returns a list array. For cells of type 'N' or 'D', it always has length 1; for the remaining type 'S',
      //  it always has length 2 or more (empty or single-char. strings being padded with terminal space(s)).
      //  NO latitude allowed for errors in any of the arguments; no internal adjustments.
      { // ################ NOTHING DOING AT PRESENT ###########################
//      { Tabula Mensa = Tabula.GrabTabula( (int) Args[0].X);
//        if (Mensa == null) { result.B = false;  result.S = "'tablistget': can't identify the table"; return result; }
//        Table table = Mensa.DGV.CurrentTable;
//        TList lystery;
//        if (Args[1].I == -1)
//        { int n = (int) Args[1].X;
//          if (n >= 0 && n < table.RefList.Count) lystery = table.RefList[n];
//          else
//          { result.B = false;
//            result.S = String.Format("'tablistget': there is no list {0}. (No. table lists: {1})", n, table.RefList.Count); return result;
//          }
//        }
        ////else 
        ////int thisRow = (int) Args[1].X,  thisCol = (int) Args[2].X;
        ////if (thisRow < 0 || thisRow >= table.Rows.Count) 
        ////{ result.B = false;  result.S = "'tableget': row arg. (" + thisRow.ToString() + ") out of range"; return result; }
        ////if (thisCol < 0 || thisCol >= table.NoCols) 
        ////{ result.B = false;  result.S = "'tableget': column arg. (" + thisCol.ToString() + ") out of range"; return result; }
        ////double[] outstuff = TCell.ToDoubleArray(table.Rows[thisRow][thisCol]);
        ////result.I = V.GenerateTempStoreRoom(outstuff.Length);
        ////R.Store[result.I].Data = outstuff;
        break;
      }
      case 339: // UNICODE(scalar / array unicode nos.) -- returns a 'chars' array of the chars. whose unicodes are as given.
      // Any mix of args. is allowed. (A single scalar in would result in an array of length 1.) No errors are raised; 
      // Unicodes >= JS.FirstPrivateCharNo or < 0 will be replaced by the space, char. 32. (Chars. 0 to 31 are left as is.)
      // Values are rounded after such testing.
      { double[] outdata = AccumulateValuesFromArgs(Args);
        int outlen = outdata.Length;  double firstPrivvy = (double) JS.FirstPrivateCharNo;
        for (int i=0; i < outlen; i++) 
        { if (outdata[i] < 0.0 || outdata[i] >= firstPrivvy) outdata[i] = 32.0; 
          else outdata[i] = Math.Round(outdata[i]);  
        }
        int outslot = V.GenerateTempStoreRoom(outlen);
        R.Store[outslot].Data = outdata;
        R.Store[outslot].IsChars = true;
        result.I = outslot;  break;
      }  
      case 340: // OVERLAY(UnderArray, OverArray, array Operation [, startPtr1 [, startPtr2] ]) -- NONVOID. The square-bracket
      // args. are not really optional; they are fixed in number for any particular behaviour. Behaviours are:
      // (1) Two structures of the same dimensionality (list array or matrix): There must be one pointer for two list
      // arrays, two pointers for matrices (including vectors). The overlaid array starts its overlaying from
      // the indicated element of UnderArray, and continues overlaying till either it runs out of data or it exceeds the bounds of UnderArray.
      // (Note that the function only works for 1D and 2D structures.)
      // (2) The first structure is a matrix, the second a vector (column or row), and there are NO pointers: If a column vector, then it
      // replicates its action over each column of the matrix; and analogously for a row vector.
      // In both cases the return is a structure of the same type and dimensions as UnderArray.
      // 'Operation' is always an array; it stores the operation code(s); an operation code is always just one character (but see 'heritage' bit
      //   below). In the case of LIST ARRAY - LIST ARRAY combinations only, the operation applying at OverArray element t
      //   is always Operation[t modulo length-of-Operation-Array]. For all other combinations only one operation is allowed for the function call,
      //   and that is the one in Operation[0].
      //   Heritage adjustment: In the old days, there were three operations with codes of several characters, all beginning with '&'. As the
      //   code '&' no longer has any other significance, we will allow these codes to persist; hence, if Operation[0] is '&', the heritage code
      //   spread over the next few elements will apply as the function's sole operation.
      //  Operation codes: All apply with OverArray[i] to the LEFT of the operator and UnderArray[i] to the RIGHT.
      //   '+','-', '*', '/' have their natural meanings, as "UnderArray OPN OverArray".
      //   Overwriting operations: OverArray[i] overwrites UnderArray[i] under the following conditions:
      //    '#' = unconditional: OverArray[i] always overwrites UnderArray[i].
      //    '>' = "overwrites if OverArray[i] > UnderArray[i]";   '<' = "overwrites if OverArray[i] < UnderArray[i].
      //    'o' = "overwrites if OverArray[i]  is nonzero"  (Heritage code: "&over".) Case-sensitive; small letter 'o', not the digit zero.
      //    'u' = "overwrites if UnderArray[i] is nonzero"  (Heritage code: "&under".) Case-sensitive.
      //    'b' = "overwrites if both OverArray[i] and UnderArray[i] are nonzero"  (Heritage code: "&both".) Case-sensitive.
      // It is allowable for either or both pointers to be negative, in which case the underlap is ignored.
      { int underslot = Args[0].I,  overslot = Args[1].I,  opslot = Args[2].I;
        if (underslot == -1 || overslot == -1 || opslot == -1) return Oops(Which, "the first three args. must all be arrays");
        // Check for the right number of args., and for conformity of the first two:
        StoreItem underItem = R.Store[underslot],  overItem = R.Store[overslot],  opItem = R.Store[opslot];
        int[] underDims = underItem.DimSizes, overDims = overItem.DimSizes;
        int underDimCnt = underItem.DimCnt,  overDimCnt = overItem.DimCnt;
        bool isMxVec = (NoArgs == 3),  isLiLi = (NoArgs == 4),  isMxMx = (NoArgs == 5); // Only 3 to 5 args. allowed, so one must always be true.
        bool isMxRowVec = false; // Only referenced if isMxVec is true. If so, and is a column vector, this will be false.
        if (isMxVec)
        { bool ohdear = false;
          if (underDimCnt != 2 || overDimCnt != 2) ohdear = true;
          else
          { isMxRowVec = (overDims[1] == 1);
            if (!isMxRowVec && overDims[0] > 1) ohdear = true;
          }
          if (ohdear) return Oops(Which, "the 3-arg. form requires a matrix and a vector as the first two args.");
          if ( (isMxRowVec && underDims[0] !=overDims[0]) || (!isMxRowVec && underDims[1] !=overDims[1]) )
          { return Oops(Which, "the row/column vector (2nd. arg.) must have the same length as rows/columns of the matrix (1st. arg.)");
          }
        }
        else if (isLiLi)
        { if (underDimCnt != 1 || overDimCnt != 1) return Oops(Which, "the 4-arg. form requires two list arrays as the first two args.");
        }
        else // isMxMx
        { if (underDimCnt != 2 || overDimCnt != 2) return Oops(Which, "the 5-arg. form requires two matrices as the first two args.");
        }
      // Operation(s):
        double[] surgery = opItem.Data._Copy();
        // Heritage case: if 'surgery' begins with '&', make it an array of length 1, based on its second character:
        if (surgery[0] == 38 && surgery.Length > 1) surgery = new double[] { surgery[1] };
        // No errors, so go ahead.
        double operation = surgery[0]; // value not used, if isLiLi.
        double[] overdata = overItem.Data,  underdata = underItem.Data._Copy(); // only 'underdata' is a copy.
        int colUnder=-1;
      // Two list arrays:
        if (isLiLi)
        {
          int surgeryLen = surgery.Length;
          int startPtr = (int) Args[3].X, noOver = overDims[0], noUnder = underDims[0];
          for (int colOver=0; colOver < noOver; colOver++)
          { colUnder = startPtr + colOver;
            operation = surgery[colOver % surgeryLen];
            if (colUnder < 0) continue;   if (colUnder >= noUnder) break;
            if      (operation == 43) underdata[colUnder] += overdata[colOver]; 
            else if (operation == 42) underdata[colUnder] *= overdata[colOver];
            else if (operation == 45) underdata[colUnder] -= overdata[colOver];
            else if (operation == 47) underdata[colUnder] /= overdata[colOver]; // no checks for zero divisor
            else if (operation == 35) underdata[colUnder] = overdata[colOver]; // overwrite ('#')
          // OVERWRITING operations:
            else if (operation == 111) // &'o' -- overwrite if 'over' value is nonzero
            { if (overdata[colOver] != 0.0) underdata[colUnder] = overdata[colOver]; }
            else if (operation == 117) // &'u' -- overwrite if 'under' value is nonzero
            { if (underdata[colUnder] != 0.0) underdata[colUnder] = overdata[colOver]; }
            else if (operation == 98)  // &'b' -- overwrite if 'both' over and under value are nonzero
            { if (overdata[colOver] != 0.0 && underdata[colUnder] != 0.0) underdata[colUnder] = overdata[colOver]; }
            else if (operation == 62) // '>' -- overwrite if 'over' value is > 'under' value
            { if (overdata[colOver] > underdata[colUnder]) underdata[colUnder] = overdata[colOver]; }
            else if (operation == 60) // '<' -- overwrite if 'over' value is < 'under' value
            { if (overdata[colOver] < underdata[colUnder]) underdata[colUnder] = overdata[colOver]; }
            // That's all the legal operations...
            else return Oops(Which, "unrecognized operation");
          }
          result.I = V.GenerateTempStoreRoom(noUnder);   R.Store[result.I].Data = underdata;  break;
        }
       // Both args. 2-dimensional:
        int norowsUnder = underDims[1], nocolsUnder = underDims[0];
        int norowsOver,  nocolsOver, startRow, startCol;
        if (isMxMx)
        { norowsOver = overDims[1];   nocolsOver = overDims[0];
          startRow = (int) Args[3].X;   startCol = (int) Args[4].X;
        }
        else
        { norowsOver = norowsUnder;  nocolsOver = nocolsUnder; // a virtual matrix, made up of repetitions of the row/col. vector
          startRow = 0;   startCol = 0;
        }
        int rowUnder, overOffset, underOffset, overIndex;
        for (int rowOver = 0; rowOver < norowsOver; rowOver++)
        { rowUnder = startRow + rowOver;
          if (rowUnder < 0) continue;  if (rowUnder >= norowsUnder) break;
          overOffset = nocolsOver * rowOver;
          underOffset = nocolsUnder * rowUnder;
          for (int colOver = 0; colOver < nocolsOver; colOver++)
          { colUnder = startCol + colOver;
            if      (isMxMx)     overIndex = overOffset + colOver;
            else if (isMxRowVec) overIndex = colOver;
            else                 overIndex = rowOver;
            if (colUnder < 0) continue;  if (colUnder >= nocolsUnder) break;
            if      (operation == 43) underdata[underOffset + colUnder] += overdata[overIndex];
            else if (operation == 42) underdata[underOffset + colUnder] *= overdata[overIndex];
            else if (operation == 45) underdata[underOffset + colUnder] -= overdata[overIndex];
            else if (operation == 47) underdata[underOffset + colUnder] /= overdata[overIndex]; // no checks for zero divisor
            else if (operation == 35) underdata[underOffset + colUnder]  = overdata[overIndex]; // overwrite ('#')
          // OVERWRITING operations:
            else if (operation == 111) // &'o' -- overwrite if 'over' value is nonzero
            { if (overdata[overIndex] != 0.0) underdata[underOffset + colUnder] = overdata[overIndex]; }
            else if (operation == 117) // &'u' -- overwrite if 'under' value is nonzero
            { if (underdata[underOffset + colUnder] != 0.0) underdata[underOffset + colUnder] = overdata[overIndex]; }
            else if (operation == 98)  // &'b' -- overwrite if 'both' over and under value are nonzero
            { if (overdata[overIndex] != 0.0 && underdata[underOffset + colUnder] != 0.0) underdata[underOffset + colUnder] = overdata[overIndex]; }
            else if (operation == 62) // '>' -- overwrite if 'over' value is > 'under' value
            { if (overdata[overIndex] > underdata[underOffset + colUnder]) underdata[underOffset + colUnder] = overdata[overIndex]; }
            else if (operation == 60) // '<' -- overwrite if 'over' value is < 'under' value
            { if (overdata[overIndex] < underdata[underOffset + colUnder]) underdata[underOffset + colUnder] = overdata[overIndex]; }
            // That's all the legal operations...
            else return Oops(Which, "unrecognized operation");
          }
        }
        result.I = V.GenerateTempStoreRoom(nocolsUnder, norowsUnder);   R.Store[result.I].Data = underdata;  break;

      }
      case 341: // SWING(.) - Three forms. In each case, ALL args must either be scalar or be arrays (any structure) all of the same length.
      // (1) SWING(Xto, Yto): the angle(s) of rotation made by the unit vector on the X axis to position vector {origin to (Xto,Yto)}.
      // (2) SWING(Xto, Yto, Xfrom, Yfrom): the angle(s) of rotation from the vector(s) {origin to Xfrom, Yfrom} 
      //        to the vector(s) {origin to Xto, Yto}.
      // (3) SWING(Xto, Yto, Xfrom, Yfrom, Xpivot,Ypivot): as (2), replacing origin by (Xpivot,Ypivot).
      { if (NoArgs % 2 != 0) return Oops(Which, "there must be exactly 2, 4 or 6 arguments");
        bool isScalar = (Args[0].I == -1);
        // check that all args. are either scalar or arrays:
        for (int i=1; i < NoArgs; i++) { if (isScalar ^ Args[i].I == -1) return Oops(Which, "The args. must be either all scalar or all arrays"); }
           // No test for equal lengths; this is done inside all versions of JM.RotationAngles(.) called below.
        // Arguments are OK:
        if (isScalar) 
        { double toX = Args[0].X,  toY = Args[1].X;
          if (NoArgs == 2) {  result.X = Math.Atan2(toY, toX);  break; }
          double fromX = Args[2].X, fromY = Args[3].X, pivotX = 0.0, pivotY = 0.0;
          if (NoArgs == 6) { pivotX = Args[4].X;  pivotY = Args[5].X; }
          result.X = JM.RotationAngle(fromX, fromY, toX, toY, pivotX, pivotY); break;
        }
       // If got here, the args. are all arrays:
        double[] Xto, Yto, Xfrom, Yfrom, Xpivot, Ypivot;
        double[] outdata;
        Xto = R.Store[Args[0].I].Data; Yto = R.Store[Args[1].I].Data; 
        if (NoArgs == 2) outdata = JM.RotationAngles(Xto, Yto);
        else
        { Xfrom = R.Store[Args[2].I].Data;    Yfrom = R.Store[Args[3].I].Data;
          if (NoArgs == 4) outdata = JM.RotationAngles(Xfrom, Yfrom, Xto, Yto);
          else // 6 args:
          { Xpivot = R.Store[Args[4].I].Data;    Ypivot = R.Store[Args[5].I].Data; 
            outdata = JM.RotationAngles(Xfrom, Yfrom, Xto, Yto, Xpivot, Ypivot);
        } }
        if (outdata == null) return Oops(Which, "the array args. must all have the same length");
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        break;
      }

      case 342: // KEYDOWN(bool asCharArray). Returns the key value of the key currently down (with provisos - see below).
      // If 'asCharArray' FALSE, simply returns the key value as a scalar (which is 0, if no key down). If TRUE, returns a char. array
      //  (see body of method JTV.KeyDenomination() for values), or "null" if no key down.
      // Some principles (illustrated for the TRUE case): (1) Printable char keys
      //  produce what you would expect; e.g. key A returns 'a' or 'A' depending on whether caps lock is on or not. (2) Helper keys
      //  always feature first; e.g. if you press Shift and then 'a', this fn. will return "Shift" until the A key goes down, then will return
      //  "A" (unless the caps lock is down, in which case it returns 'a'). (3) This fn. keeps returning the key value
      //  as long as the key is down; e.g. if you hold the control key down alone, "Cntrl" will return until you let it go (or press
      //  another key).

      { int whiskey =  MainWindow.ThisWindow.WhatKeyIsDown;
        if (Args[0].X == 0.0) { result.X = (double) whiskey;  break; }
        string ss = JTV.KeyDenomination(whiskey);
        result.I = V.GenerateTempStoreRoom(ss.Length);
        StringToStoreroom(result.I, ss);
        break;
      }
      case 343: // UNBIN(array Values [, asCharsArray] ) -- Two variants: (A) Values is taken as a CHARS array: all values except
      // unicodes of chars. '0' and '1' are ignored; the values in the array go from [0] = most significant bit to [last] = least 
      // significant bit. (B) Values is a NON-CHARS. array: all values significant, and all nonzero values are taken as representing 
      // binary digit 1; and [0] = the LEAST significant bit. If the 2nd. arg. is absent, the decision as to whether or not to use 
      // (A) or (B) is taken by reading the .IsChars field of the array; otherwise that field is ignored and the 2nd. argument applies.
      // RETURNS the scalar value represented by the binary number.
      { int slot = Args[0].I;  if (slot == -1) return Oops(Which, "the first arg. must be an array");
        StoreItem arr = R.Store[slot];
        bool treatAsCharsArray;
        if (NoArgs > 1) treatAsCharsArray = (Args[1].X != 0.0);  else treatAsCharsArray = arr.IsChars;
        double[] data = arr.Data;   int dataLen = data.Length;
       // Develop a boolean array with binary digits, LSB in [0]:
        bool[] isOne = new bool[dataLen];
        if (treatAsCharsArray)
        { int cnt = 0;  double x;
          for (int i = dataLen-1; i >= 0; i--)
          { x = data[i];
            if (x == 49.0) { isOne[cnt] = true;  cnt++; }
            else if (x == 48.0) cnt++; // but leave cnt as is for any other chars.
          }        
        }        
        else for (int i = 0; i < dataLen; i++) isOne[i] = (data[i] != 0.0);
       // Produce the number represented by the boolean array:
        double sum = 0;  double addend = 1;
        for (int i = 0; i < dataLen; i++)
        { if (isOne[i]) sum += addend;
          addend *= 2;
        }
        result.X = sum;   break;
      }  
      case 344: // BESTSQUARE(scalar Value). If Value (after rounding) is >= 1, returns an array of length 2,
      // being the two factors of Value that are nearest to each other (sorted, so that [0] <= [1]). Value < 1
      // returns an array of length 1, value NaN, and an 'empty(.)' call returns TRUE.
      { double x, value = Math.Round(Args[0].X);
        if (value < 1.0) { result.I = EmptyArray();  break; }
        int outslot = V.GenerateTempStoreRoom(2);
        double[] data = R.Store[outslot].Data;        
        if (value <= 3.0) { data[0] = 1.0;  data[1] = value; }
        else
        { int divisor = (int) Math.Floor(Math.Sqrt(value)); // the nearest integer below (or at) the square root.
          double virtZero = 1 / (10*divisor);
          int factor = 1;
          while (true) // the loop will always exit, seeing that the lowest value is a positive integer, as 1 divides everything.
          { x = value / (double) divisor;
            if (Math.Abs(x - Math.Round(x)) <= virtZero) { factor = divisor;  break; }
            divisor--;
          }
          data[0] = (double) factor;   data[1] = value / (double) factor;
        }
        result.I = outslot;   break;
      }
      case 345: // CHARPOLY( Matrix [, scalar virtZero] -- returns the characteristic polynomial for the matrix. If 'virtZero'
      // is supplied and is positive, elements of absolute value < virtZero are corrected to 0.
      {
        int MxSlot = Args[0].I;  double negligible = 0.0;
        if (NoArgs == 2) negligible = Args[1].X; // too bad if an array.
        if (MxSlot == -1) return Oops(Which, "the first arg. must be a matrix");
        StoreItem store = R.Store[MxSlot];
        int size = store.DimSz[0];
        if (store.DimCnt != 2 || size < 2 || store.DimSz[1] != size) return Oops(Which, "a square matrix is required, 2x2 or larger");
        double[,] Mx = new double[size, size];  int offset = 0;
        double[] mxdata = store.Data;
        for (int i = 0; i < size; i++)
        { offset = i*size;
          for (int j = 0; j < size; j++)
          { Mx[i,j] = mxdata[offset + j]; }
        }
        double[] outdata = M2.CharacteristicPoly(Mx);
        if (negligible > 0.0)
        { for (int i=0; i < outdata.Length; i++)
          { if (Math.Abs(outdata[i]) < negligible) outdata[i] = 0.0; }
        }
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        break;
      }
      case 346: // SUBMATRIX (Matrix, scalar PivotRow, scalar PivotCol). Returns a matrix.
      case 347: // COFACTOR  (Square Matrix, scalar PivotRow, scalar PivotCol [, scalar MinorNotCofactor] ). Returns a scalar.
      // 'submatrix' returns the smaller matrix that results when the indicated row and column are removed. Matrix must be 
      // at least 2x2 (in which case a 1x1 matrix would be returned, not a scalar, but need not be square). 'cofactor' DOES
      // have to have a square matrix; it returns the determinant of the submatrix, with its sign adjusted if nec. (as for cofactor) 
      // or, if 3rd. arg. present and nonzero, not adjusted (as for minor).
      { int inslot = Args[0].I,  pivotRow = (int) Args[1].X, pivotCol = (int) Args[2].X; // no check for arrayhood of 2nd and 3rd arg
        StoreItem sightem = null;  int noRows=0, noCols=0;
        if (inslot == -1) return Oops(Which, "the first arg. must be a matrix");
        sightem = R.Store[inslot];   
        noRows = sightem.DimSz[1];  noCols = sightem.DimSz[0];
        if (noRows < 2 || noCols < 2) return Oops(Which, "the first arg. must be a matrix, 2x2 or larger");
        if (Which == 347 && noRows != noCols) return Oops(Which, "the first arg. must be a square matrix");
        double[] indata = sightem.Data;
        // We have a valid matrix. Do we have a valid pivot?
        if (pivotRow < 0 || pivotRow >= noRows || pivotCol < 0 || pivotCol >= noCols)
        { return Oops(Which, "the pivot coordinates are not compatible with the matrix"); }
        // All is kosher, so convert the data into a double[,]:
        double[,] Mx = new double[noRows, noCols];  int offset;
        for (int i = 0; i < noRows; i++)
        { offset = i*noCols;
          for (int j = 0; j < noCols; j++) { Mx[i,j] = indata[offset + j]; }
        }
        // Call the Class M2 routine:        
        double[,] subMx = M2.Submatrix(Mx, pivotRow, pivotCol); // thingo being either the cofactor or the minor.
        if (Which == 347) // Then we want the determinant of this submatrix:
        { result.X = M2.Determinant(subMx);
          if ( (NoArgs < 4 || Args[3].X == 0.0) && (pivotRow + pivotCol)%2 == 1) result.X = - result.X; // apply the cofactor sign.
          break;
        }  
        // Only 'submatrix' is left. Convert back to a MiniMaths structure:
        int subRows = noRows - 1, subCols = noCols - 1;
        result.I = V.GenerateTempStoreRoom(subCols, subRows);
        double[] outdata = R.Store[result.I].Data;
        for (int rw = 0; rw < subRows; rw++)
        { offset = rw*subCols;
          for (int cl = 0; cl < subCols; cl++) { outdata[offset + cl] = subMx[rw, cl]; }
        }
        break;
      }
  //  case 348: DIMLIKE(load of arrays) -- see case 1 - "DIM(.)"
      case 349: case 350: // COPYMX / COPYMXTO: 
      // COPYMX  (Matrix, scalar FromRow, scalar FromCol [, scalar NoRows [, scalar NoCols] ])
      // COPYMXTO(Matrix, scalar FromRow, scalar FromCol,   scalar ToRow,    scalar ToCol    )
      // Unlike the nonmatrix versions, no latitude is allowed with arguments; they must be exactly right,
      //  and must return a matrix of at least 1x1.
      // Omission of square-bracketted arg(s) in 'copymx(.)' sets default NoRows / NoCols to max. possible.
      {
        int sourceslot = Args[0].I;
        if (sourceslot == -1 || R.Store[sourceslot].DimCnt != 2) return Oops(Which, "the first arg. must be a matrix");
        int fromRow = Convert.ToInt32(Args[1].X), fromCol = Convert.ToInt32(Args[2].X);
        int matrixrows = R.Store[sourceslot].DimSz[1],  matrixcols = R.Store[sourceslot].DimSz[0];
        if (fromRow < 0 || fromRow >= matrixrows || fromCol < 0 || fromCol >= matrixcols)
        { return Oops(Which, "the given starting row and/or column for copying is impossible"); }
        int extentRows = 0, extentCols = 0;
        if (Which == 350)
        { int toRow = Convert.ToInt32(Args[3].X), toCol = Convert.ToInt32(Args[4].X);
          if (toRow < fromRow || toRow >= matrixrows || toCol < fromCol || toCol >= matrixcols)
          { return Oops(Which, "the given final row and/or column for copying is impossible"); }
          extentRows = toRow - fromRow + 1;  extentCols = toCol - fromCol + 1;
        }
        else
        { if (NoArgs <= 3) extentRows = matrixrows - fromRow;
          else
          { extentRows = Convert.ToInt32(Args[3].X);
            if (extentRows < 1 || matrixrows < fromRow + extentRows) return Oops(Which, "impossible extent of rows for copying");
          }
          if (NoArgs <= 4) extentCols = matrixcols - fromCol;
          else
          { extentCols = Convert.ToInt32(Args[4].X);
            if (extentCols < 1 || matrixcols < fromCol + extentCols) return Oops(Which, "impossible extent of columns for copying");
          }
        }
        int newslot = -1;
        List<double> values = new List<double>(extentRows * extentCols);
        double[] partRow = new double[extentCols];
        double[] sourceData = R.Store[sourceslot].Data;
        for (int rw = fromRow; rw < fromRow + extentRows; rw++)
        { Array.Copy(sourceData, rw * matrixcols + fromCol, partRow, 0, extentCols);
          values.AddRange(partRow);
        }
        newslot = V.GenerateTempStoreRoom(extentCols, extentRows);
        R.Store[newslot].Data = values.ToArray();
        result.I = newslot;  break;
      }
     // case 351, 352 -- TOZERO, FROMZERO -- see Hybrids
      case 353: case 354: case 355: case 356: // MIN / MINAT / MINABS / MINABSAT(Var,Var,Var,....) - arrays and scalars can be mixed.
      // Gives min. / min. absolute value within all scalars and all array elements. MINAT returns an array, with information about
      // the FIRST find of the minimum: [0] = min. value; [1] = which arg. holds it (to base 0); [2] = its absolute position within
      // that argument (to base 0).
      { double mino = double.MaxValue, minabso = mino;  int slot;
        int argno = 0, pos = 0; // locators of the max. value (for MAXAT(.))
          // Set to 0, in the unlikely event that all values of all args. are
          //  double.MinValue, so that loops below do not reset them.
        if (Which <= 354) // MIN, MINAT:
        { for (int var = 0; var < NoArgs; var++)
          {//If a scalar, check .X field against current mino:
            if (Args[var].I == -1)
            { if (Args[var].X < mino)
              { mino = Args[var].X;  argno = var;  pos = 0; } }
            else //If an array, check through its entries:
            { slot = Args[var].I;   int totSz = R.Store[slot].TotSz;
              double[] xx = R.Store[slot].Data;
              for (int i = 0; i < totSz; i++)
              { if (xx[i] < mino) { mino = xx[i];  argno = var;  pos = i; } }
        } } }
        else // MINABS, MINABSAT:
        { for (int var = 0; var < NoArgs; var++)
          {//If a scalar, check .X field against current mino:
            if (Args[var].I == -1)
            { if (Args[var].X < minabso && Args[var].X > -minabso)
              { mino = Args[var].X;  minabso = Math.Abs(mino);  argno = var;  pos = 0; } }
            else //If an array, check through its entries:
            { slot = Args[var].I;   int totSz = R.Store[slot].TotSz;
              double[] xx = R.Store[slot].Data;
              for (int i = 0; i < totSz; i++)
              { if (xx[i] < minabso && xx[i] > -minabso) { mino = xx[i];  minabso = Math.Abs(mino);  argno = var;  pos = i; } }
        } } }
        // Set up the return values
        if (Which == 353 || Which == 355) result.X = mino; // MIN(.), MINABS(.); the latter returns the signed value with least abs. value.
        else // MINAT(.) and MINABSAT(.) returns array with localizing data as well:
        { int newslot = V.GenerateTempStoreRoom(3);  double[] yy = R.Store[newslot].Data;
          yy[0] = mino;
          yy[1] = argno; // to base 0.
          yy[2] = pos; // to base 0, as usual for arrays.
          result.I = newslot;
        }
        break; }
      case 357: case 358: case 359: case 360: // 357 = AND;  358 = OR;  359 - XOR;  360 - XORCOMP.
      // All take arguments: FUNCTION((Variable1, Variable2 [, Scalar Tolerance]); all return an object of the same structure as Variable1. 
      // Allowed combinations: scalar-scalar; array-array (must have same total length); array-scalar (scalar --> virtual array of same
      //  size as first argument). Not allowed: scalar-array.
      // Elements of Variable1 & Variable2 are taken as '1' if nonzero and '0' if zero; the returned element corresponding to a pair
      //  of Variable_ elements is either '1' or '0', depending on the operation.
      // If Tolerance supplied and > 0, the replacement is 1 if the two elements differ by Tolerance or less, otherwise 0.  
      // 'xorcomp(.)' gives the complement of 'xor' = i.e. scores 1 if paired elements both nonzero or both zero; otherewise 0.
      { double tolerance = 0.0;  
        if (NoArgs > 2) { tolerance = Args[2].X;  if (tolerance < 0) tolerance = 0; } // no test for arrayhood.
        int slot0 = Args[0].I,  slot1 = Args[1].I;
        if (slot0 == -1) // first argument scalar:
        { if (slot1 == -1) // both arguments scalar:
          { bool isTRUE1 = (Math.Abs(Args[0].X) > tolerance);
            bool isTRUE2 = (Math.Abs(Args[1].X) > tolerance);
            bool outcome;
            if      (Which == 357) outcome = isTRUE1 & isTRUE2;
            else if (Which == 358) outcome = isTRUE1 | isTRUE2;
            else { outcome = isTRUE1 ^ isTRUE2;  if (Which == 360) outcome = !outcome; }
            if (outcome) result.X = 1.0;
          }
          else return Oops(Which, "a first arg. scalar with a second arg. an array is not a valid combination");
        }
        else // First arg. is an array:
        { double[] dub0 = R.Store[slot0].Data;   int dub0len = dub0.Length;
          double[] dub1, dubout = new double[dub0len];
          if (slot1 == -1) // second array a scalar: Build an array consisting of repetitions of this value.
          { double x = Args[1].X;  dub1 = new double[dub0len];  for (int i=0; i < dub0len; i++) dub1[i] = x; }
          else dub1 = R.Store[slot1].Data;
          if (dub1.Length != dub0len) return Oops(Which, "arrays have different lengths");
          // Arrays now guaranteed to be of equal length:        
          bool isTRUE1, isTRUE2, outcome;
          for (int i=0; i < dub0len; i++)
          { isTRUE1 = (Math.Abs(dub0[i]) > tolerance);
            isTRUE2 = (Math.Abs(dub1[i]) > tolerance);
            if      (Which == 357) outcome = isTRUE1 & isTRUE2;
            else if (Which == 358) outcome = isTRUE1 | isTRUE2;
            else { outcome = isTRUE1 ^ isTRUE2;  if (Which == 360) outcome = !outcome; }
            if (outcome) dubout[i] = 1.0;
          }
          int newslot = V.GenerateTempStoreRoom(R.Store[slot0].DimSz);
          R.Store[newslot].Data = dubout;  result.I = newslot;
        }
        break; 
      }
      case 361: // CORRELATION(array Sequence1, array Sequence2,   scalar XStep,   scalar or array XOffset 
      //                                                                              [, StartInSequence1 [, EndInSequence1 ] ] )
      // Computes: Sum { Sequence1[i] * Sequence2[i + (XOffset/XStep)] } * XStep. (NB - Note the '+' sign.)
      // 'Sequence1' and 'Sequence2' must be arrays, but need not be of same length. Both are treated algorithmically as if their indices
      //   extended to +/- infinity, all such extended locations holding zero values.
      // 'XStep' must represent the horiz. axis increment in moving from Sequence1[i] to Sequence1[i+1], and so must be > 0.
      // 'XOffset' is in terms of horiz. axis scaling, NOT in terms of indices of Sequence2 (the index offset is round(XOffset/XStep) ). 
      //   XOffset can be negative, in which case the calculation is of Sum { Sequence1[i] * Sequence2[i - |XOffset|/XStep] } * XStep.
      //   If a scalar, RETURNS a scalar; if an array, RETURNS an array of correlations for each offset in XOffset.
      // The last two arguments relate to the start and end within Sequence1, and are INDEX values, not X-scaled values. 
      //   Their defaults are: Start = 0; End = last element of Sequence1. Oversized values are corrected to range.
      { 
       // Sort out all the arguments:
        int slot1 = Args[0].I,  slot2 = Args[1].I;  
        if (slot1 == -1 || slot2 == -1) return Oops(Which, "the first two arguments must be arrays");
        double[] data1 = R.Store[slot1].Data,  data2 = R.Store[slot2].Data;
        int len1 = data1.Length,  len2 = data2.Length;
        double XStep = Args[2].X; 
        if (XStep <= 0) return Oops(Which, "the third argument must be a scalar value > 0");
        int[] offsets;  int offslot = Args[3].I, offsetsLen = 1;
        double[] results;
        bool scalarOffset = (offslot == -1);
        if (scalarOffset) 
        { offsets = new int[1];  offsets[0] = (int) Math.Round( Args[3].X / XStep); 
          results = new double[1];
        }
        else // an array of offsets has been supplied:
        { double[] offdubs = R.Store[offslot].Data;  offsetsLen = offdubs.Length;  offsets = new int[offsetsLen];
          for (int i=0; i < offsetsLen; i++) offsets[i] = (int) Math.Round( offdubs[i] / XStep);
          results = new double[offsetsLen];
        }
        int startPtr = 0;     if (NoArgs > 4) startPtr = (int) Math.Round(Args[4].X);
        int endPtr = len1-1;  if (NoArgs > 5) endPtr =   (int) Math.Round(Args[5].X);
        if (startPtr >= len1) break; // leaving result.X = 0.
        if (startPtr < 0) startPtr = 0;        
        if (endPtr >= len1) endPtr = len1-1;
        if (endPtr < 0 || endPtr < startPtr) break; // leaving result.X = 0;
       // To get here, start and end pointers must cover actual values in data1.
        for (int i=0; i < offsetsLen; i++)
        { int ptr1 = startPtr, ptr2=0;
          double sum = 0.0;
          while (true)
          { ptr2 = ptr1 + offsets[i];  
            if (ptr2 >= 0 && ptr2 < len2) sum += data1[ptr1] * data2[ptr2];
            ptr1++;  if (ptr1 > endPtr) break;
          }
          results[i] = sum * XStep;
        }
        if (scalarOffset) result.X = results[0];
        else
        { result.I = V.GenerateTempStoreRoom(offsetsLen);
          R.Store[result.I].Data = results;
        }
        break;
      }  
      case 362: // CONVOLUTION(array Sequence1, array Sequence2, scalar XStep ): Suppose Sequence1 represents discretized values of some
      // continuous function Fn1, and Sequence2 of function Fn2. If their argument is denoted as X, suppose that X for Sequence1[0] (and
      // of Sequence2[0]) is 'Xmin', and that X for the last elements in Sequence1 and Sequence2 is Xmax. Then what is computed is the
      // discretized version of the definite integral from Xmin to X of Fn1(u).Fn2(X - u).du where u is a dummy variable, and where in
      // successive integrals X takes all values from Xmin to Xmax inclusive. 'du' is represented in the discrete version by XStep, 
      // which is the X difference between two successive samples of Fn1 or Fn2. (Error raised if it is zero or negative.)
      { 
       // Sort out the arguments:
        int slot1 = Args[0].I,  slot2 = Args[1].I;  
        if (slot1 == -1 || slot2 == -1) return Oops(Which, "the first two arguments must be arrays");
        double[] data1 = R.Store[slot1].Data,  data2 = R.Store[slot2].Data;
        int len = data1.Length;
        if (data2.Length != len) return Oops(Which, "the two arrays must have the same length");
        double XStep = Args[2].X; 
        if (XStep <= 0) return Oops(Which, "the third argument must be a scalar value > 0");
        double[] convoln = new double[len];
        for (int offset = 0; offset < len; offset++)
        { int ptr2;
          double sum = 0.0;
          for (int ptr1 = 0; ptr1 < len; ptr1++)
          { ptr2 = offset - ptr1;   if (ptr2 < 0) break; // as it can only go more negative, never +ve again.
            sum += data1[ptr1] * data2[ptr2]; // safe, as ptr2 can never exceed len-1.
          }
          convoln[offset] = sum * XStep;
        }
        result.I = V.GenerateTempStoreRoom(len);
        R.Store[result.I].Data = convoln;
        break;
      }  
      case 363: // FFT( {ListArray of length N} OR {Matrix Nx2} [, bool ForwardTransform ] 
      // -- RETURNS an Nx2 matrix (even if the input was a list array - i.e. representing noncomplex values). Actually, any structured array 
      // with lowest dim. 2 is accepted; structure of same dimensions is then returned.
      // RE N: N must be at least 2. If N is not an exact power of 2, it will be padded with zeroes to length = the next power of 2 above.
      // If ForwardTransform is omitted or nonzero, the forward transform is done; otherwise the reverse. 
      // The forward transform has no universal coefficient; the inverse transform uses a universal coefficient of 1/N.
      // This function accesses a static class variable, either ForwardFiddleFactor or InverseFiddleFactor. ('Fiddle factor' is so named by
      // textbook descriptions of the FFT, not by me). If this variable is nonnull and has the appropriate size, it will be uncritically used; 
      // otherwise it will be recalculated before use.
      { int inslot = Args[0].I;  if (inslot == -1) return Oops(Which, "the first arg. must be an array");
        StoreItem instore = R.Store[inslot];
        int dimcnt = instore.DimCnt, dim0 = instore.DimSz[0];
        double[] indata = instore.Data;
        bool complexin = false;  
        if (dimcnt > 1)
        { if (dim0 == 2) complexin = true;
          else return Oops(Which, "the first arg. must either be a list array or have a lowest dim. of 2");
        }
        int inLen = indata.Length; if (complexin) inLen /= 2;
        if (inLen < 2) return Oops(Which, "the first arg. must have at least two values (whether real or complex)");
        bool forward = true; if (NoArgs > 1 && Args[1].X == 0.0) forward = false;
       // Prepare the data for the FFT method:
        // If nec., build it up to a length which is an integral power of 2:
        double x = Math.Round(Math.Log(inLen, 2));
        int properLen = (int) Math.Pow(2.0, x);
        // Tested: Even for inLen = 2^31-1 - which is 2.14 billion(+) - inLen+1 and inLen-1 are distinguished by +/-1 from inLen itself.
        while (properLen < inLen) { properLen *= 2; }
        double[] toFFT = new double[2 * properLen];
        if (complexin) // Then indata[] has the form: [real 0][imag 0][real 1][imag 1]... We reorganize such that all the reals
                       // are in the bottom half of toFFT, and all the imags in the top half:
        { for (int i=0; i < inLen; i++) // values (if any) beyond inLen and up to properLen remain zero.
          { toFFT[i] = indata[2*i];
            toFFT[properLen + i] = indata[2*i + 1];  
          }
        }
        else // real values in:
        { indata.CopyTo(toFFT, 0); } // Zeroes remain for all real values beyond inLen up to properLen, and beyond them for all imag. values.
       // Get the FFT:
        double[] fromFFT = FFT(toFFT, forward);
       // Decode the returned data:
        int outslot;
        outslot = V.GenerateTempStoreRoom(2, properLen); // It is not safe to make this structure have the same higher dimensions as
               // the input structure (as is the case for 'fourier(.)'), since padding, if it occurs, would result in dimensional mismatching.
        double[] outdata = R.Store[outslot].Data;
        for (int i=0; i < properLen; i++)
        { outdata[2*i]     = fromFFT[i];
          outdata[2*i + 1] = fromFFT[properLen + i];  
        }
        result.I = outslot;
        break;      
      }
      case 364: // HOMEDIRECTORY (dummy) -- arg. ignored. Returns the named home directory, e.g. "/home/fred/".
      { string struedel = MainWindow.HomePath;
        result.I = V.GenerateTempStoreRoom(struedel.Length);
        StringToStoreroom(result.I, struedel);
        break;
      }
      case 365: // PLAYSOUND (array Filename [, bool WaitTillSoundStops])
      // Plays the sound if the file exists and the media player can handle it; otherwise no sound (and no error messages).
      // If 'WaitTillSoundStops' is notpresent or FALSE, then the default - 'asynchronous playing' - occurs; that is,
      // the user program continues on during the sound, though on a different thread. This is normally what is required;
      // but the down side of this mode is that you can't queue sounds, as a second call to this fn. will swamp the first
      // sound so that only the second one will be heard.
      // *** The original MiniMaths version of this fn. allowed for beeping, if the file failed; but Console.Beep() does
      // not work under Mono unless one is specifically in Console mode, and I can't find any other beeping function.
      { string filename = StoreroomToString(Args[0].I, true, true); // Trims 0-32 off start and end. Scalar --> ""
        string fnm = filename.Trim();
        bool validFileName = (fnm != "");
        bool play_asynch = (NoArgs == 1 || Args[1].X == 0.0);
        if (validFileName)
        { try
          { FileInfo fee = new FileInfo(fnm);
            if (!fee.Exists) validFileName = false;
          }
          catch { validFileName = false; }
        }
        if (validFileName)
        { using (System.Media.SoundPlayer squawk = new System.Media.SoundPlayer())
          { try
            { squawk.SoundLocation = fnm;
              squawk.LoadTimeout = 10000;
              if (play_asynch) squawk.Play();
              else squawk.PlaySync();
            }
            catch
            { validFileName = false; } // Probably means that it wasn't a playable file.
          }
        }
        break;
      }  
      case 366: // (1): ASPECT (GraphID) -- RETURNS a 2-value list array, [Declination, Ascension]; or the empty array (length 1, value NaN),
                //                  if GraphID not identifiable or not a 3D graph. Angles in RADIANS.
                // (2): ASPECT (Declination Angle, Ascension Angle) -- VOID. Sets the initial viewing aspect of the 3D axis system for the
                //                  next graph to be drawn (but not for subsequent graphs).
                // (3): ASPECT (GraphID, Declination Angle, Ascension Angle) -- Sets the initial viewing aspect for this particular graph,
                //                  and returns same (or the null array, as above, if graph not recognized).
      { Graph graf = null;
        int graphID = 0;
        Trio trill = new Trio(0,0,0);
        if (NoArgs != 2)
        { graphID = (int) Args[0].X;
          trill = Board.GetBoardOfGraph(graphID, out graf);
          if (graf == null) { result.I = EmptyArray();  break; }
        }
        if (NoArgs > 1)
        { int anglePtr = NoArgs - 2; // points to the first Angle argument.
          double declination = Args[anglePtr].X,  ascension = Args[anglePtr+1].X;
          if (graphID == 0) // then no graph is specified:
          { GraphDeclination = declination;   GraphAscension = ascension; }
          else // graph specified, and was identified:
          { graf.Ascension = ascension;  graf.Declination = declination;  Board.ForceRedraw(trill.X, false); }
        }
        if (NoArgs != 2) // if '2', the fn. is void.
        { result.I = V.GenerateTempStoreRoom(2);
          R.Store[result.I].Data = new double[] { graf.Declination, graf.Ascension };
        }
        break;
        }

      case 367: // CHOOSEFILENAME(array DirectoryName [, scalar AllowMultipleFileNames ] )
      // DirectoryName is where the dialog is to open. If AllowMultipleFileNames is present and nonzero, does what it says.
      // The opening directory is all up to the last '/'; any text beyond that is ignored (even if it is a valid directory name).
      // If errors in the name, the function works back to the last valid directory. If the whole of DirectoryName is garbage,
      //  then the file browser opens where it wills (some directory which was recently accessed in the file browser).
      // RETURNS full path+directory name of chosen files; or if the user CANCELLED out of the file browser, the array " ".
      // Where there was more than one file chosen, the char. '#' delimits the names, which do not nec. have the same length.
      { string filepath = StoreroomToString(Args[0].I, true, true); // bools = trimmed at both ends.
        string[] stroo = filepath._ParseFileName("");
        filepath = stroo[0];
        string filename = "";
        Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog("SELECT A FILE NAME", null,
             Gtk.FileChooserAction.Open, "Cancel", Gtk.ResponseType.Cancel, "Select", Gtk.ResponseType.Accept);
        fc.SetCurrentFolder(filepath); // This has a boolean return, which only seems to be FALSE if you present it with a valid file (not path).
        bool allowMultNames = (NoArgs == 2 && Args[1].X != 0.0);
        fc.SelectMultiple = allowMultNames;
        int outcome = fc.Run();
        if (outcome == (int) Gtk.ResponseType.Accept)
        { if (allowMultNames) filename = String.Join("#", fc.Filenames);  else filename = fc.Filename; }
        fc.Destroy(); // Without this, the FileChooserDialog window won't get closed.
        if (filename == "")
        { result.I = V.GenerateTempStoreRoom(1);   StringToStoreroom(result.I, " "); }
        else // valid filename returned:
        { result.I = V.GenerateTempStoreRoom(filename.Length);
          StringToStoreroom(result.I, filename);
        }
        break;
      }
      case 368: // CURRENTDIRECTORY(EITHER any scalar Dummy  OR  array DirectoryName)
      // If a scalar argument, simply returns the current directory; otherwise tries to reset the current directory to the given name.
      // RETURNED:
      // (1) If arg. was scalar, returns the current directory.
      // (2) If arg. was an array, also returns an array UNLESS there was an error, in which case it returns the error message.
      // Note that the returned directory name always begins (and ends) with '/', but an error message always begins with a space ' '.
      // Note that in no situation is an error raised.
      {
        string filepath = " ";
        int slot = Args[0].I;
        if (slot == -1) // scalar argument, so just return the current CurrentPath.
        { filepath = MainWindow.ThisWindow.CurrentPath.Trim();
          if (filepath == "") { filepath = " Current path is empty"; } // note that 'error' message begins with ' '.
          else { if (filepath._Last() != '/') filepath += "/"; }
          result.I = V.GenerateTempStoreRoom(filepath.Length);
          StringToStoreroom(result.I, filepath);
          break;
        }
        else // array argument:
        {
          filepath = StoreroomToString(slot, true, true); // will be trimmed.
          if (filepath._IndexOf('/') == -1) filepath = " No '/' found in proposed path"; // note that error messages begin with ' '.
          else if (filepath[0] != '/') filepath = " The proposed path does not begin with '/'"; // note that error messages begin with ' '.
          else
          { if (filepath._Last() != '/') filepath += "/";
            try
            { System.IO.DirectoryInfo director = new System.IO.DirectoryInfo(filepath);
              if (!director.Exists) filepath = " This directory does not exist"; // note that error messages begin with ' '.
            }
            catch (Exception eek)
            { filepath = " Problem found with the proposed path: " + eek.Message; } // note that error messages begin with ' '.
                              // occurs if Mono found certain chars. in the name, or if the directory has security issues (e.g.
                              // a directory like '/', which requires 'sudo' access).
          }
          if (filepath[0] == '/') MainWindow.ThisWindow.CurrentPath = filepath;
          result.I = V.GenerateTempStoreRoom(filepath.Length);
          StringToStoreroom(result.I, filepath);
        }
        break;
      }
      case 369: // CHECKDIRECTORY(array Directory [, bool MayHaveFileName] ) -- Checks if such a directory exists on this machine,
      //  and returns scalar TRUE (1) or FALSE. If 2nd. arg. missing or FALSE, then if '/' is absent at the end it will be appended.
      //  If present and TRUE, this won't happen, and any file name (or directory name without a final '/') will be ignored.
      // File name abbreviations are allowed ("~/", "./", "../").
      // No reasons given for failure; user will need to check with a file browser. Note that FALSE returns for security issues as well as
      //  for errors; e.g. the root directory "/" will return false (in Ubuntu at least) because it only has super-user access.
      { int slot = Args[0].I;
        string filepath = StoreroomToString(slot, true, true); // will be trimmed.
        if (filepath == "") break; // scalar arg or all-spaces array
        // The following allows for abbreviations like "~/" and "./" and "../":
        if (NoArgs == 1 || Args[1].X == 0.0)
        { if (filepath._Last() != '/') filepath += '/'; }
        string[] stroo = filepath._ParseFileName("");
        filepath = stroo[0];
        try
        { System.IO.DirectoryInfo director = new System.IO.DirectoryInfo(filepath);
          if (director.Exists) result.X = 1.0;
        }
        catch { result.X = 0.0; }
        break;
      }
      case 370: // FILESIZE(array PathAndName) -- Returns file size, or -1 if unsuccessful (e.g. file name no good).
      // PathAndName has to be complete, starting from the root: "/...". Abbreviations handled by extension method
      // "_ParseFileName" are valid.
      { int slot = Args[0].I;
        string filename = StoreroomToString(slot, true, true); // will be trimmed.
        if (filename == "") { result.X = -1.0;  break; } // scalar arg or all-spaces array
        // This bit is here mainly to deal with abbreviations like "~/" and "./":
        string[] fillet = filename._ParseFileName(MainWindow.ThisWindow.CurrentPath);
        filename = fillet[0] + fillet[1];
        try
        { FileInfo fie = new FileInfo(filename);
          if (!fie.Exists) result.X = -1.0;
          else result.X = Convert.ToDouble(fie.Length); // arg. is type long.
        }
        catch { result.X = -1.0; }
        break;
      }
      case 371: // SPLIT(array Original, array/scalar Delimiter [, array/scalar Padder [, bool AcceptEmpty [, scalar SetLength [ ,
      //                  array/scalar Prefix [, array/scalar Suffix ] ] ] ] ]). Nonvoid.
      // Produces a jagged matrix with the delimited subarrays, padded at the end as nec. by Padder. The default Padder is 32,
      //  the unicode for the space char. There is no restriction on values in Original, Delimiter and Padder (though only Padder can be NAN);
      //  they don't have to be unicodes. 'Original' is taken as a list array - no notice taken of its actual structure.
      // If Delimiter is an array, the whole of it constitutes the one delimiter.
      // If Padder is an array, the whole array is consecutively added to a row until it reaches the required length, if necessary
      //  truncating the last instance of the padder array.
      // If Prefix present, its contents are added to the front of every row. If Suffix present, it is added to the end of every
      //  delimited element in 'Original' before padding is added.
      // If SetLength rounds to 0 or less it is ignored. If set, it applies to rows after any Prefix or Suffix has been added.
      // The returned array takes the same chars. rating as Original.
      // If AcceptEmpty is omitted or FALSE, empty subarrays are simply omitted, unless the whole array has no non-delimiter content,
      //  in which case the 'empty' array is returned. If TRUE, the corresponding row of the output jagged matrix will be
      //  Prefix (if any) + Suffix (of any) + Padder.
      //    E.g. "||", with delimiter '|' and Padder 'x', would return the 3 x 1 'jagged' matrix "[x;  x;  x]".
      { int inslot = Args[0].I, delimslot = Args[1].I;
        if (inslot == -1) return Oops(Which, "the first arg. must be an array");
        StoreItem sin = R.Store[inslot];
        double[] indata = sin.Data._Copy();
        int inLen = indata.Length;
        bool InIsChar = sin.IsChars;
        double[] delim;
        if (delimslot == -1) delim = new double[] {Args[1].X};
        else delim = R.Store[delimslot].Data._Copy();
        // Accept empties?
        bool acceptEmpties = (NoArgs > 3 && Args[3].X != 0.0);
        // Locate all the delimiters; include virtual delimiters, one at -delimLen and one after the end.
        int fromPtr = 0, delimLen = delim.Length;
        List<int> delimPtrList = new List<int>();  delimPtrList.Add(-delimLen);
        while (true)
        { int n = indata._Find(delim, fromPtr);
          if (n == -1) break;
          delimPtrList.Add(n);
          fromPtr = n + delimLen;
        }
        if (delimPtrList.Count == 0) // No delimiters, so simply return the original (though as a list array).
        { result.I = V.GenerateTempStoreRoom(inLen);
          R.Store[result.I].Data = indata;   R.Store[result.I].IsChars = InIsChar;
          break;
        }
        delimPtrList.Add(inLen);
        int[] delimPtr = delimPtrList.ToArray();
        // Find or set the longest length:
        int rowLen = 0;   bool isSetLength = false;
        if (NoArgs > 4) // then a set length has been stipulated:
        { rowLen = Convert.ToInt32(Args[4].X);  if (rowLen > 0) isSetLength = true;
        }
        if (rowLen < 1)
        { for (int i = 1; i < delimPtr.Length; i++)
          {
            int n = delimPtr[i] - delimPtr[i-1] - delimLen;
            if (n > rowLen) rowLen = n;
          }
        }
        // prepare prefix and/or suffix:
        double[] prefix = null, suffix = null;
        int prefixLen = 0,  suffixLen = 0;
        if (NoArgs > 5)
        { if (Args[5].I >= 0) { prefix = R.Store[Args[5].I].Data._Copy(); prefixLen = prefix.Length; }
          else { prefix = new double[] {Args[5].X}; prefixLen = 1; }
        }
        if (NoArgs > 6)
        { if (Args[6].I >= 0) { suffix = R.Store[Args[6].I].Data._Copy(); suffixLen = suffix.Length; }
          else { suffix = new double[] {Args[6].X}; suffixLen = 1; }
        }
        if (!isSetLength) rowLen += prefixLen + suffixLen;
       // ACCUMULATE ROWS OF THE JAGGED MATRIX
        double[] fullpad = new double[rowLen];
        if (NoArgs > 2 && Args[2].I >= 0)
        { double[] padarr = R.Store[Args[2].I].Data;  fullpad = padarr._Chain(rowLen); }
        else
        { double d = 32.0;  if (NoArgs > 2) d = Args[2].X;  for (int i=0; i < rowLen; i++)  fullpad[i] = d; }
        List<double>JagStuff = new List<double>();
        int noRows = 0;
        for (int i = 1; i < delimPtr.Length; i++)
        { int subLen = delimPtr[i] - delimPtr[i-1] - delimLen;
          if (!acceptEmpties && subLen == 0) continue; // An empty row, so don't add it.
          double[] thisRow = new double[rowLen];
          int nn, ptr = 0, indataPtr = delimPtr[i-1]+delimLen;
          if (prefixLen > 0) // insert prefix
          { nn = Math.Min(prefixLen, rowLen);
            for (int j=0; j < nn; j++)thisRow[j] = prefix[j];
            ptr += nn;
          }
          nn = Math.Min (subLen, rowLen - ptr); // insert the subarray
          for (int j=0; j < nn; j++) thisRow[ptr+j] = indata[indataPtr+j];
          ptr += nn;
          if (suffixLen > 0) // insert the suffix
          { nn = Math.Min(suffixLen, rowLen - ptr);
            for (int j=0; j < nn; j++)thisRow[ptr+j] = suffix[j];
            ptr += nn;
          }
          for (int j = ptr; j < rowLen; j++) thisRow[j] = fullpad[j]; //add on the padding
          JagStuff.AddRange(thisRow);   noRows++;
        }
        if (JagStuff.Count == 0) { result.I = EmptyArray(); break; } // All delimiters, and 'acceptEmpties' was false.

        double[] jagged = JagStuff.ToArray();
        if (jagged.Length != noRows * rowLen) return Oops(Which, "Programming fault!"); // **** Remove after adequate testing
        result.I = V.GenerateTempStoreRoom(rowLen, noRows);
        StoreItem stoo = R.Store[result.I];
        stoo.Data = jagged;   stoo.IsChars = InIsChar;
        break;
      }
      case 372:  case 373: // CLIPCULL / CLIPCULLABS(Variable LoLimit, Variable HiLimit, Array Values [, ExcludeValuesAtLimits]): NONVOID.
      // Args. 1 and 2: If either is an array, there is no corresponding limit. (E.g. enter 'x', 'none', '-'...). If a scalar, it
      // is the limit. Returned: a COPY of the array or scalar 3rd. arg. with all values EXCISED if they lie beyond the limits. (If the last
      // arg. is present and is nonzero, values exactly AT the limits will also be excised.)
      // In the case of 'clipcullabs(.)', the signs of HiLimit and LoLimit are ignored.
      // If the result would theoretically be an empty array, the empty array of size 1, value NaN is returned.
      { double lowLimit, highLimit;
        if (Which == 372) // CLIPCULL:
        { if (Args[0].I == -1) lowLimit = Args[0].X;  else lowLimit = double.MinValue;
          if (Args[1].I == -1) highLimit = Args[1].X;  else highLimit = double.MaxValue;
        }
        else // CLIPCULLABS:
        { if (Args[0].I == -1) lowLimit = Math.Abs(Args[0].X);  else lowLimit = 0;
          if (Args[1].I == -1) highLimit = Math.Abs(Args[1].X);  else highLimit = double.MaxValue;
        }
        int inSlot = Args[2].I;   if (inSlot == -1) return Oops(Which, "the third arg. must be an array");
        double[] inData = R.Store[inSlot].Data;
        int inLen = inData.Length;
        bool cullAtLimits = (NoArgs > 3 && Args[3].X != 0.0);
        List<double> goodguys = new List<double>();
        for (int i=0; i < inLen; i++)
        { double x = inData[i];  if (Which ==373 && x < 0.0) x = -x;
          if (x > lowLimit && x < highLimit) goodguys.Add(inData[i]);
          else if ( (x == lowLimit || x == highLimit) && !cullAtLimits) goodguys.Add(inData[i]);
        }
        int cnt = goodguys.Count;
        if (cnt == 0) result.I = EmptyArray();
        else
        { result.I = V.GenerateTempStoreRoom(cnt);
          R.Store[result.I].Data = goodguys.ToArray();
          R.Store[result.I].IsChars = R.Store[inSlot].IsChars;
        }
        break;
      }
      case 374: // NTH(integral scalar Number [, bool JustTheSuffix [, NoFullStop ] ] ) -- returns the cardinal version of the number, as a chars. array.
      // E.g. Number = 23 returns "23rd.", Number = 0 returns "0th.". Arguments, if present, modify this: 'JustTheSuffix', if present and nonzero,
      // causes return of e.g. "rd." instead of "23rd.", while 'NoFullStop' in either case omits the final fullstop (if present and nonzero).
      // The boolean arguments would be mainly useful if your plan was to use superscripting (in which case both would be 'true').
      {
        int N = Convert.ToInt32(Args[0].X); // Too bad, if an array.
        bool justTheSuffix = (NoArgs > 1  &&  Args[1].X != 0.0);
        bool noFullStop    = (NoArgs > 2  &&  Args[2].X != 0.0);
        string stroo = N._Ordinal(justTheSuffix, noFullStop);
        int len = stroo.Length;
        result.I = V.GenerateTempStoreRoom(len);
        StringToStoreroom(result.I, stroo);
        break;
      }
      case 375: // FIXEDSIZE(array InData, scalar Extent, array/scalar Padder [, bool PadAtStart [, bool TruncateIfTooLong [, array TruncationSign]]] )
      // An array of length < Extent is padded out by adding copies of 'Padder' to the end (the last instance of Padder itself being amputated
      // if nec.). If 'PadAtEnd' present and nonzero, padding is at the start instead. If Truncate..." nonzero and present, overlength array is
      // truncated. In that case, if 'TruncationSign' is present and of some length L, it will overlay the last L chars. of the truncated data
      // (typically it would be the dieresis "..").
      // Independent of chars. rating (which is preserved).
      // Errors that crash: InData scalar; Extent < 1.
      // Errors that don't crash: TruncationSign scalar, or longer than Extent (truncation will still occur, but TruncationSign will be ignored).
      {
        int indataslot = Args[0].I;  if (indataslot == -1) return Oops(Which, "the first arg. must be an array");
        int n, extent = (int) Args[1].X;
        if (extent < 1) return Oops(Which, "either 'extent' is not scalar, or else it is less than 1");
        StoreItem stitem = R.Store[indataslot];
        double[] indata = stitem.Data;
        double[] outdata;
        int inLen = indata.Length;
        if (inLen < extent)
        { outdata = new double[extent];
          // Develop an array of padding:
          double[] padstuff = null;
          int padslot = Args[2].I;
          if (padslot == -1) { padstuff = new double[1];  padstuff[0] = Args[2].X; }
          else padstuff = R.Store[padslot].Data;
          int offset = 0, len = padstuff.Length, deficit = extent - inLen;
          bool padAtStart = (NoArgs > 3  && Args[3].X != 0.0);
          if (padAtStart) indata.CopyTo(outdata, deficit);
          else { offset = inLen; indata.CopyTo(outdata, 0); }
          for (int i = 0; i < deficit; i++)
          { outdata[offset + i] = padstuff[i % len]; }
        }
        else if (inLen == extent)
        { outdata = new double[extent];  indata.CopyTo(outdata, 0); }
        else
        { bool truncate = (NoArgs > 4  && Args[4].X != 0.0);
          if (!truncate)
          { outdata = new double[inLen];  indata.CopyTo(outdata, 0);  }
          else
          { outdata = new double[extent];
            Array.Copy(indata, outdata, extent);
            if (NoArgs > 5 && Args[5].I != -1) // then we will overwrite the end...
            { double[] overdata = R.Store[Args[5].I].Data;
              n = overdata.Length;
              if (n < extent) overdata.CopyTo(outdata, extent - n);
            }
          }
        }
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        R.Store[result.I].IsChars = stitem.IsChars;
        break;
      }
      case 376: case 377: // VAL / STRINGTOVALUE(..). (Two names for same fn.) NB - can return either a scalar or an array, depending on the arguments.
      // Case 1 - SCALAR expected: x = stringtovalue( TheString [, scalar FailureCue]). (Default FailureCue is MAXREAL.)
      // Case 2 - ARRAY expected: arr = stringtovalue( TheString, ARRAY Delimiter [, scalar FailureCue]). If parsing fails,
      //            returns an array of size 1 in all cases, containing FailureCue was scalar, it will be that value as an array of size 1. (NAN allowed.)
      //            As in case 1, the default FailureCue is MAXREAL (in an array of size 1).
      //            Delimiter: No parsing. E.g. spaces allowed.
      { int strSlot = Args[0].I;  if (strSlot == -1) return Oops(Which, "1st. arg. must be an array");
        string ss = StoreroomToString(strSlot, true, true);
        double failureValue;
        bool isScalar = (NoArgs == 1 || Args[1].I == -1);
        if (isScalar)
        { if (NoArgs == 2) failureValue = Args[1].X;  else failureValue = double.MaxValue;
          result.X = ss._ParseDouble(failureValue);
        }
        else // an array:
        { int delimSlot = Args[1].I;  if (delimSlot == -1) return Oops(Which, "in the three arg. form, the second arg. must be an array");
          string delimiter = StoreroomToString(delimSlot, false, false); // NO trimming of delimiter.
          if (NoArgs == 3) failureValue = Args[2].X;  else failureValue = double.MaxValue;
          double[] outdata = ss._ToDoubleArray(delimiter);
          if (outdata == null) outdata = new double[]{failureValue};
          result.I = V.GenerateTempStoreRoom(outdata.Length);
          R.Store[result.I].Data = outdata;
        }
        break;
      }
      case 378: // CHOOSECLR(array InputColour, array OutputType). InputColour can be e.g. "7FFFD4", "0x7fffd4", "aquamarine",
      // nonchars. array [127, 255, 212] (or its string version "127, 255, 212").
      // The nonchars. form is assumed if all of these conditions apply together: (1) rating is nonchars; (2) array size is 3;
      //  (3) all values lie between 0 and 255 (rounded). In all other situations it is assumed that the array encodes a string.
      // OutputType: exactly: "hex" for "FF0080"; "bytes" for nonchars array [255, 0, 128]; "verbose" for a whole sentence.
      // No errors crash. If InputColour is silly, the dialog box announces this and waits for your correction.
      // RETURNED: Cancellation or corner icon closure returns the empty array; otherwise the return is a chars. array, as above.
      { int clrslot = Args[0].I,  typeslot = Args[1].I;
        if (clrslot == -1 || typeslot == -1) return Oops(Which, "both args. must be arrays");
        string outputType = StoreroomToString(typeslot);
        bool sendBytesBack = (outputType == "bytes");
        if (!sendBytesBack && outputType != "hex" && outputType != "verbose")
        { return Oops(Which, "the 2nd. arg. must be exactly one of: \"hex\", \"bytes\", \"verbose\" (case-sensitive)"); }
        double[] inClr = R.Store[clrslot].Data;
        double[] outClr = null;
        string ss="", inClrStr;
        bool inputWasBytes = false;
        if (inClr.Length == 3 && !R.Store[clrslot].IsChars)
        { if (inClr[0] >= -0.499 && inClr[0] <= 255.499)
          { ss = (Convert.ToInt32(inClr[0])).ToString() + ", ";
            if (inClr[1] >= -0.499 && inClr[1] <= 255.499)
            { ss += (Convert.ToInt32(inClr[1])).ToString() + ", ";
              if (inClr[2] >= -0.499 && inClr[2] <= 255.499)
              { ss += (Convert.ToInt32(inClr[2])).ToString();
                inputWasBytes = true;
        } } } }
        if (inputWasBytes) inClrStr = ss;
        else inClrStr = StoreroomToString(clrslot);
        // Get the dialog going:
        int btn = 1;
        while (btn == 1) // If the user clicks the 'SET' button in the dialog, btn returns as 1, and relooping occurs
                         // with inStr set to whatever the user put into the dialog text box before pressing 'SET'.
        { btn = JD.ColourChoiceBox("", ref inClrStr, outputType); }
        if (btn != 2) outClr = new double[] { double.NaN };
        else // the 'FINISH' button (btn = 2) was clicked:
        { if (sendBytesBack)
          { outClr = (inClrStr._ToIntArray(","))._ToDubArray();
            if (outClr == null) outClr = new double[] {double.NaN }; // should be impossible.
          }
          else outClr = StringToStoreroom(-1, inClrStr);
        }
        // In all cases...
        result.I = V.GenerateTempStoreRoom(outClr.Length);
        R.Store[result.I].Data = outClr;
        if (!sendBytesBack) R.Store[result.I].IsChars = true;
        break;
      }
      case 379: // LADDERCLR(scalar NoColours, array/scalar Colour1,  array/scalar Colour2 [, array ConstantHSL, [bool AreHSL [, Distortion ]]] )
      // Returns a matrix of dims (NoColours x 3) of RGB colours, grading from Colour1 to Colour2. (These two can be either
      // a name or an array of 3 RGB values; or if 'AreHSL' present and TRUE, they are taken as 3 HSL values.)
      // If Colour1 and Colour2 are RGB, they are converted internally to HSL. If 3rd. arg. missing, scalar or unrecognized,
      // all three HSL components vary from 1st. to 2nd. colour. If ConstantHSL holds any one or two of 'H', 'S', 'L', then the
      // nominated component(s) are held steady from Colour1's HSL form, the corresponding values of Colour2 being ignored.
      // (Only ConstantHSL[0] and (if present) [1] are examined, so e.g. "HSL" would be read as just "HS". (NOT case-sensitive.)
      // Errors only result from out-of-range values for colour arrays. RGB colours: such an error results in colour Black.
      // HSL colours:
      // back to 0 / 255 (RGB format) or to 0 / 1 (HSL format).
      // Only crashing errors: (1) NoColours rounds to 1 or less; (2) args. are HSL colours and do not have length exactly 3.
      // 'Distortion' - the gradation coefficient is raised to this power; its default is 1.0. Values should not be far away from 1
      // (e.g. 2, or 0.5). If <= 0, it will be ignored.
      {
        int NoColours = (int) Math.Round(Args[0].X);  if (NoColours < 2) return Oops(Which, "No. of colours to return must be at least 2.");
        bool ArgsAreRGB = (NoArgs <= 4 || Args[4].X == 0.0);
        double distortionIndex = 1.0;
        if (NoArgs > 5) { distortionIndex = Args[5].X;  if (distortionIndex <= 0.0) distortionIndex = 1.0; }
        // Develop the two HSL extreme colours:
        double[] HSL1, HSL2; // will each hold one HSL colour.
        if (ArgsAreRGB)
        { Gdk.Color defcolour = new Gdk.Color(0,0,0); // error returns this.
          bool success;
          Gdk.Color RGB1 = InterpretColourReference(Args[1], defcolour, out success);
          Gdk.Color RGB2 = InterpretColourReference(Args[2], defcolour, out success);
          double hue, sat, lum;
          JS.HSL(RGB1, out hue, out sat, out lum);
          HSL1 = new double[] {hue, sat, lum};
          JS.HSL(RGB2, out hue, out sat, out lum);
          HSL2 = new double[] {hue, sat, lum};
        }
        else // are HSL arguments:
        { int slot1 = Args[1].I, slot2 = Args[2].I;
          if (slot1 == -1 || slot2 == -1) return Oops(Which, "HSL colour args. cannot be scalar");
          StoreItem sitem1 = R.Store[slot1],  sitem2 = R.Store[slot2];
          if (sitem1.TotSz != 3 || sitem2.TotSz != 3) return Oops(Which, "HSL colour args. must be arrays of size 3");
          HSL1 = sitem1.Data;  HSL2 = sitem2.Data; // *** aliasses, so be careful not to alter their values!
          // No need to test values for range here, as method JS.HSLtoRGB(.) will do this later.
        }
        // Deal with ConstantHSL argument:
        bool constH = false,  constS = false,  constL = false;
        if (NoArgs > 3 && Args[3].I >= 0)
        { double[] consthsl = R.Store[Args[3].I].Data;
          double x = Math.Round(consthsl[0]);  if (x > 90.0) x -= 32.0; // Convert lower case to upper case.
          if (x == 72.0) constH = true;  else if (x == 83.0) constS = true;  else if (x == 76.0) constL = true;
          if (consthsl.Length > 1)
          { x = Math.Round(consthsl[1]);
            if (x == 72.0) constH = true;  else if (x == 83.0) constS = true;  else if (x == 76.0) constL = true;
          }
        }
        // Develop the colour range as RGB values:
        double[] outRGB = new double[NoColours * 3];
        double coeff, h, s, l;
        byte red, green, blue;
        for (int i=0; i < NoColours; i++)
        { if (distortionIndex == 1.0) coeff = (double) i / (double) (NoColours-1);
          else coeff = Math.Pow( (double) i / (double) (NoColours-1), distortionIndex);
          if (constH) { h = HSL1[0];  if (h == 0) h = HSL2[0]; } // e.g. if you are grading from white to green.
          else h = (1 - coeff) * HSL1[0] + coeff * HSL2[0];
          if (constS) s = HSL1[1];  else s = (1 - coeff) * HSL1[1] + coeff * HSL2[1];
          if (constL) l = HSL1[2];  else l = (1 - coeff) * HSL1[2] + coeff * HSL2[2];
          JS.HSLtoRGB(h, s, l, out red, out green, out blue);
          outRGB[3*i] = red;   outRGB[3*i + 1] = green;   outRGB[3*i + 2] = blue;
        }
        // Return as a matrix:
        result.I = V.GenerateTempStoreRoom(3, NoColours);
        R.Store[result.I].Data = outRGB;
        break;
      }
      case 380: // STRUCTURE(one or more values: dimension sizes, the first being the HIGHEST dimension): Creates a structure of this dimensionality.
      { double[] dims1 = AccumulateValuesFromArgs(Args, 0);
        int[] dimSize = dims1._ToIntArray();
        Array.Reverse(dimSize);
        int slot = V.GenerateTempStoreRoom(dimSize);
        if (slot == -1) return Oops(Which, "a dimension is zero or negative");
        result.I = slot;
        break;
      }
      case 381: // BOARDPLACEMT(scalar graphID) -- meant to be used with 'placeboard', to track resizing by the user during a run.
      // If board identified, returns array of size 4: [left, top, width, height]. Otherwise the 'empty array': size 1, value NaN.
      // NB! One small adjustment - if 'left' or 'top' would return as 1, it is reset to 1.001. This is because the returned array
      //   may well be supplied unaltered as the argument to 'placeboard(.)', in which case '1' (i.e. one pixel) will be
      //   interpreted as 'full screen width / height', so that the new graph will disappear off the screen.
        { int graphID = (int) Args[0].X;
        Graph graf = null;
        Trio dummy = Board.GetBoardOfGraph(graphID, out graf);
        if (graf == null) result.I = EmptyArray();
        else
        { int[] sizery = graf.SizingParameters(); // 'sizery' has the following fields: ACTUAL BOARD dimensions: [0] = width, [1] = height,
          // [2] = LblHeader width, [3] = LblHeader height, [4] = drawing area width, [5] = drawing area height, [6] = TVBelow width ( or 0,
          // if no TVBelow exists), [7] = TVBelow height (or 0). ORIG. REQUESTED BOARD dimensions: [8] = width, [9] = height,
          // [10] = eboxHeader height, [11] = drawing area height, [12] = bool: 1 if TVBelow was requested, 0 if not.
          // [13], [14] = window's Left and Top on the screen. [15] to [19] are currently unused. GRAPH perimeter: [20] = width, [21] = height.
          double leftPx = (double) sizery[13];
          if (leftPx == 1.0) leftPx = 1.001;
          double rightPx = (double) sizery[14];
          if (rightPx == 1.0) rightPx = 1.001;
          double[] outdata = new double[] { leftPx, rightPx, (double) sizery[0], (double) sizery[1] };
          result.I = V.GenerateTempStoreRoom(outdata.Length);
          R.Store[result.I].Data = outdata;
        }
        break;
      }
      case 382: // WINDOW(array WhichWindow, array Action [, array TheText, array Where [, bool Formatted OR scalar NoCharsToDelete ]]).
      // Action[0]: 'C'(clear all window text), 'R'(read all window text), 'W' (write to window), 'M' (set MarkUpTags status for
      //   Asst Window - ignored if WhichWindow is not 'A'), 'D' (delete chars.), 'F'(ocus the window)
      // There must be 2 args for Action = 'C', 'F' or 'R', 3 for 'M', at least 4 for Action = 'W', and 5 for 'D'.
      // WhichWindow[0]: 'A' or 'R' (case-insensitive).
      // TheText - in the particular case of Action 'M', this must be '+' (for switching it on) or '-' (off) or '?' (status).
      //  For action 'D' it is ignored (so may be scalar).
      // Where: scalar, or case-sensitive array. Values: "fill" = replace any current text with this text; "cursor" = at current cursor position;
      //   "start" = at start (moving existing text up);  "append" = append (i.e. at end of existing text); "1234" (or scalar 1234) = start at
      //   the specific character position 1234 (adjusted back to text extreme, if outside of existing text). Any other
      //   value aborts the method. In the case of action 'D', only (e.g.) array "1234" or scalar 1234 would have any effect.
      // Formatted (only for action 'A'): Default is FALSE. If TRUE, formatting tags are processed.
      // NoCharsToDelete (only for action 'D'): no action if rounds to 0 or negative, or if Where out of range.
      // RETURNED: VOID for actions 'C','W','D'; TEXT for action 'R' (a space, if no text in the window); array '+' or '-' for action 'M'.
      // ERRORS: No crashes; simply nothing happens if an argument is silly, apart from the last (if action ='W') which is always interpreted.
      {
        int windowSlot = Args[0].I;  if (windowSlot == -1) break; // No error message; simply no action is taken.
        char windowID = ' ';
        double x = R.Store[windowSlot].Data[0];
        if (x == 65 || x == 97) windowID = 'A';  else if (x == 82 || x == 114) windowID = 'R';  else break;
        int actionSlot = Args[1].I;  if (actionSlot == -1) break;
        char action = ' ';
        double y = R.Store[actionSlot].Data[0];
        if (y >= 97 && y <= 122) y -= 32; // convert to capitals.
        if (y >= 65 && y <= 90) action = y._ToChar(' '); // ignore anything but latin letters
        if (action == ' ') break; // only an exit if action is not a letter; does not cover illegal letter values.
        // 'Where' is used by more than one action, so get at it here:
        string whither = "";  int whereTo = -1;
        if (NoArgs > 3)
        { if (Args[3].I == -1) // scalar
          { int n = Convert.ToInt32(Args[3].X);
            if (n >= 0) { whither = n.ToString();  whereTo = n; }
          }
          else // array
          { whither = StoreroomToString(Args[3].I);
            whereTo = whither._ParseInt(-1);
          }
        }
        // FOCUS:
        if (action == 'F') MainWindow.ThisWindow.FocusWindow(windowID);
        // CLEAR:
        if (action == 'C') MainWindow.ThisWindow.ClearWindow(windowID);
        // SET MARKUP TAG STATUS
        else if (action == 'M')
        { if (NoArgs < 3 || windowID != 'A') break;
          string ss = StoreroomToString(Args[2].I, true, true); if (ss == "") break;
          int n = "+-?".IndexOf(ss[0]);  if (n == -1) break;
          if (n < 2)
          { bool turntagson = (n == 0);
            MainWindow.ThisWindow.SwitchMarkupTagSystem(turntagson);
          }
          double[] outstuff = new double[1];
          if (MainWindow.UseMarkUpTags) outstuff[0] = 43.0;  else outstuff[0] = 45.0;
          result.I = V.GenerateTempStoreRoom(1);
          R.Store[result.I].Data = outstuff;
          R.Store[result.I].IsChars = true;
          break;
        }
        // READ:
        else if (action == 'R')
        { string ss = MainWindow.ThisWindow.ReadWindow(windowID);  if (ss == "") ss = " ";
          int len = ss.Length;
          int outSlot = V.GenerateTempStoreRoom(len);
          StringToStoreroom(outSlot, ss);  result.I = outSlot;
        }
        // WRITE:
        else if (action == 'W')
        { if (whither == "") break; // occurs if no Where argument.
          int textSlot = Args[2].I;  if (textSlot == -1) break;
          string texty = StoreroomToString(textSlot);
          bool formatted = (NoArgs > 4 && Args[4].X != 0.0);
          MainWindow.ThisWindow.WriteWindow(windowID, texty, whither, formatted); // No action, if arguments faulty.
        }
        // DELETE:
        else if (action == 'D')
        { if (NoArgs < 5 || whereTo < 0) break;
          int noChars = Convert.ToInt32(Args[4].X);  if (noChars < 1) break;
          Gtk.TextIter startIt = MainWindow.ThisWindow.BuffAss.StartIter;  startIt.ForwardCursorPositions(whereTo);
          Gtk.TextIter endIt = MainWindow.ThisWindow.BuffAss.StartIter;    endIt.ForwardCursorPositions(whereTo + noChars);
          MainWindow.ThisWindow.BuffAss.Delete(ref startIt, ref endIt);
        }
        break;
      }
      case 383: // SMASH( [ array Message [, array FileName ] ] ) -- Forces the program instance to die immediately.
      // If Message present (and an array), saves it as TEXT; either in standard file "/tmp/smash.txt" or, if a 2nd.
      //  array arg present which is a valid filename, in that file instead.
      // One should consider using the function "kill_on_exit() - paired with a subsequent 'exit' - first; this is
      //  intended for use only where that method fails (as I have found it to do in the case of a Master - Slave pgm pair).
      {
        int msgSlot = Args[0].I;
        if (msgSlot >= 0)
        { string msg = StoreroomToString(msgSlot);
          bool savedElsewhere = false;
          if (NoArgs > 1 && Args[1].I >= 0)
          { string fname = StoreroomToString(Args[1].I);
            string[]fpthnm = fname._ParseFileName(MainWindow.ThisWindow.CurrentPath);
            fname = fpthnm[0] + fpthnm[1];
            try
            { StreamWriter w = File.CreateText(fname);
              w.Write(msg);
              w.Flush();   w.Close();
              savedElsewhere = true;
            }
            catch { savedElsewhere = false; }
          }
          if (!savedElsewhere) // no 'try' this time, as it doesn't really matter if an error is raised, does it?
          { StreamWriter w = File.CreateText("/tmp/smash.txt");
            w.Write(msg);
            w.Flush();   w.Close();
          }
        }
        // Now the SMASH:
        throw new Exception("I'm smashed!"); // an UNHANDLED exception.
      }
      case 384: // THISLINENO() -- returns the line number of the instruction, to base 1.
      { result.X = 1.0 + (double) R.FlowLine[R.ThisFn][R.ThisBubble].Ref;
        break;
      }
      case 385: // FINDBRACKETS(array Text, scalar Ptr, array or scalar Opener, array or scalar Closer) - finds the opener
      // and closer at the present level at position Ptr. Only Opener[0] (if an array) and Closer[0] are used.
      // Always returns an array of size 2; [0] = opener's position, [1] = closer's position.
      // Either can be -1, if just one of the pair is found; both are -1 if neither found.
      // If Ptr points to Opener (and Closer present), returned is [Ptr,  position of Closer]. If Ptr points to
      // closer, returned is [position of Opener, Ptr], with posn. of Opener < Ptr.
      // Outsized Ptr is adjusted back to the nearest valid position.
      // NB - only works if Text, Opener and Closer are comprised only of valid UFT8 values.
      // If Opener and Closer are exactly the same, ##########
      {
        string inText = StoreroomToString(Args[0].I);   if (inText == "") return Oops(Which, "the first arg. must be an array");
        double[] output = new double[] {-1.0, -1.0}; // default - failed search for both opener and closer.
        result.I = V.GenerateTempStoreRoom(2);
        R.Store[result.I].Data = output;
        int n, Ptr = (int) Args[1].X;
        if (Ptr < 0) Ptr = 0; else if (Ptr >= inText.Length) Ptr = inText.Length-1;
        double Opener, Closer;
        int openerslot = Args[2].I, closerslot = Args[3].I;
        if (openerslot == -1) Opener = Args[2].X;  else Opener = R.Store[openerslot].Data[0];
        if (closerslot == -1) Closer = Args[3].X;  else Closer = R.Store[closerslot].Data[0];
        char openC = Opener._ToChar('\u0000'); // char. 0 is the default for non-unicode values
        char closC = Closer._ToChar('\u0000');
        // Find the opener:
        n = JS.OpenerAt(inText, openC, closC, 0, Ptr);   output[0] = (double) n;
        // Find the closer (independently of whether or not the above succeeded):
        n = JS.CloserAt(inText, openC, closC, Ptr);   output[1] = (double) n;
        break; // result.I being set earlier, and its .Data field updated via alias 'output[]'.
      }
      case 386: // WINDOW_FIND(string soughtText, bool matchCase, bool wholeWord, bool fromCursor, bool markAll, bool returnReplacedText);
      //   If any omitted args, defaults to invoking a dialog box; otherwise, no dialog box.   NOT VOID.
      //   If 'soughtText[0]' zero or negative, or 'soughtText' is scalar, will also default to dialog box.
      //   Where dialog box is invoked, soughtText[0] zero or negative or scalar causes the value stored in
      //   MainWindow.ThisWindow.LastSoughtText to be displayed initially in the dialog box.
      //   RETURNS an array, which holds the character positions of find(s), if text found; otherwise an array of length 1, content -1.
      {
        bool invokeDialog = (NoArgs < 5);
        string soughtText = StoreroomToString(Args[0].I);
        if (soughtText == "" || soughtText[0] == '\u0000' || soughtText[0] == char.MaxValue)
        { invokeDialog = true;  soughtText = null; }
        int[] outstuff;
        // Dialog required:
        if (invokeDialog) outstuff = MainWindow.ThisWindow.FindInText(true, soughtText, null, true, true, true, true);
        else // no request for a dialog box, so all arguments must be filled:
        { bool matchCase =  (Args[1].X != 0.0);
          bool wholeWord =  (Args[2].X != 0.0);
          bool fromCursor = (Args[3].X != 0.0);
          bool markAll =    (Args[4].X != 0.0);
          outstuff = MainWindow.ThisWindow.FindInText(false, soughtText, null, matchCase, wholeWord, fromCursor, markAll);
        }
        double[] outdata;
        if (outstuff == null) outdata = new double[] {-1.0};
        else outdata = outstuff._ToDubArray();
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        break;
      }
      case 387: // DATETIME() -- returns an array of length 10: [0] = year (4-digit); [1] = month (1 = January); [2] = day of month;
      // [3] = hour (0 to 23), [4] = min, [5] = sec, [6] = msec. Then as extras, [7] = day of week (0 = Sunday), [8] = day of year
      //  (Jan 1 is 1, not 0), [9] = msecs. since 1 AD.
      { DateTime now = DateTime.Now;
        double[] DT = new double[10];
        DT[0] = (double) now.Year;       DT[1] = (double) now.Month;      DT[2] = (double) now.Day;
        DT[3] = (double) now.Hour;       DT[4] = (double) now.Minute;     DT[5] = (double) now.Second;
        DT[6] = (double)now.Millisecond; DT[7] = (double) now.DayOfWeek;  DT[8] = (double)now.DayOfYear;
        DT[9] = JS.Tempus('L', false); // Conversion from long to double and back was tested to around 4000 AD and found to be completely accurate.
        result.I = V.GenerateTempStoreRoom(DT.Length);
        R.Store[result.I].Data = DT;
        break;
      }
      case 388: // PLU(ral) -- Two basic versions: (1) PLU(scalar Value, array Text [, array NewText ] ) and (2) PLU(Text, Value [, NewText] ).
      // Returns plural form of Text IF Value != 1 (not rounded - must be exact), otherwise just returns Text. Form 1 puts Value in the
      // returned text: "2 cats"; Form 2 omits the value: "cats". If a third argument is given, no attempt is made to form a plural; simply
      // returns either Text or Text1, depending on Value. (Therefore it has uses beyond pluralization.) Where no third argument occurs, some
      // simple rules are used which handle plural exceptions for the great majority of words which a mathematical programmer might use (e.g. "locus"
      //  --> "loci", but "hypha" - a medical word - does not go to the correct "hyphae", just to "hyphas".) Also, case of last letter of Text
      // sets the case of the char. added to it (if any).
      {
        int textIndex, valIndex;
        if      (Args[0].I == -1 && Args[1].I >= 0 ) { textIndex = 1;  valIndex = 0; }
        else if (Args[0].I >= 0  && Args[1].I == -1) { textIndex = 0;  valIndex = 1; }
        else return Oops(Which, "of the 1st. two args., one must be scalar and one an array");
        double Value = Args[valIndex].X;
        bool doReplace = (Value != 1.0);
        string inText = StoreroomToString(Args[textIndex].I);
        string outText = "";
        if (doReplace)
        { if (NoArgs > 2)
          { outText = StoreroomToString(Args[2].I);
            if (outText == "") return Oops(Which, "if a 3rd. arg. is supplied, it must be an array");
          }
          else // convert inText to a plural form:
          { int itLen = inText.Length;
            int changedLen = 0;
            char clast, clastorig = inText[itLen-1], cprev = ' ';
            clast = char.ToUpper(clastorig);
            if (inText.Length > 1) cprev = char.ToUpper(inText[itLen-2]);
            if ("YSH".IndexOf(clast) >= 0)
            { if      (clast == 'Y')
              { if ("AEIOU".IndexOf(cprev) >= 0 ) { outText = inText + 'S';  changedLen = 1; }
                else { outText = inText._Extent(0, itLen - 1) + "IES";  changedLen = 3; }
              }
              else if (clast == 'S') { outText = inText + "ES";  changedLen = 2; }
              else if (clast == 'H')
              { if ("CS".IndexOf(cprev) >= 0 ) { outText = inText + "ES";  changedLen = 2; }
                else { outText = inText + "S";  changedLen = 1; }
              }
            }
            else { outText = inText + "S";  changedLen = 1; }
            // Case adjustment of the addend, if necessary:
            if (clast != clastorig) // then clastorig was lower case:
            { string addend = outText._Last(changedLen).ToLower();
              outText = inText._Extent(0, outText.Length - changedLen) + addend;
            }
          }
        }
        else outText = inText;
       // Add in the count, if required:
        if (valIndex == 0) outText = Value.ToString("G4") + ' ' + outText;
       // Go home:
        result.I = V.GenerateTempStoreRoom(outText.Length);
        StringToStoreroom(result.I, outText);
        R.Store[result.I].IsChars = true;
        break;
      }
      case 389: // CLIPBOARD(variable Dummy[, string StrToStore ]) -- 1-arg. (suggested: "get") - returns contents of clipboard
      // as a chars. array (or as the empty array, if none). 2-args (sugggested 1st.: "set"): if second arg. is scalar or a
      // non-chars. array, it will be converted to string form (string "1.23" for scalar, string "1.12, 2.23, -3.34, ..." for array).
      // Otherwise its contents is stored on the clipboard directly as unicode chars.
      {
        Gtk.Clipboard klepto = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
        if (NoArgs == 1) // read mode:
        { string txt = klepto.WaitForText();
          if (String.IsNullOrEmpty(txt)) result.I = EmptyArray();
          else
          { result.I = V.GenerateTempStoreRoom(txt.Length);
            StringToStoreroom(result.I, txt);
          }
        }
        else // write mode:
        { int slot = Args[1].I;
          string ss;
          if (slot == -1) ss = Args[1].X.ToString(); // string form of a double, using C#'s format default.
          else
          { StoreItem sitem = R.Store[slot];
            if (sitem.IsChars) ss = StoreroomToString(slot);
            else ss = sitem.Data._ToString(", ");
          }
          klepto.Text = ss;
        }
        break;
      }

      case 390: // LOOKUP (Chars Array VarName [, ReturnIfNotFound, ReturnIfUnassigned ] ) -- returns value of VarName in main program (only).
      case 391: // TOUCH_ARRAY  ( " "   VarName, scalar AbsAddress, scalar NewValue, scalar/array Operation -- uses NewValue to alter array VarName
                // in main program (only) at the specified absolute address.
                //  'Operation' -- first char. only is examined: '+', '-', '*', '/', '#' (if arg scalar, must be the unicode of one of these)
                //  - first 4: (final value = old value <operation> new value); last: final value = new value.
                // RETURNS IF ALL WELL: 'lookup' returns variable's value; 'touch_array' return TRUE.
                // RETURNS IF ERROR: 'lookup': 2-args - crashes with error msg. 4-args - the return given as arg., which may be array or scalar
                //  (but you usually would choose the same type as for the variable being looked up, wouldn't you?)
                //   'touch_array' return FALSE (no error messages, but also does not crash).
                // Note: no 3 args. form is allowed for 'lookup'. In that case, the 3rd. arg. is simply ignored, and all proceeds as if there
                //   were only 2 args.
      { int nameslot = Args[0].I;  if (nameslot == -1) return Oops(Which, "the first arg. must be an array (the name of a variable)");
        // Look for the variable:
        int errno = 0, At = -1;
        byte varuse = 0;
        string VarName = StoreroomToString(nameslot, true, true); // trim the string.
        if (VarName == "") errno = 1;
        else
        { At = V.FindVar(0, VarName);
          if (At == -1) errno = 2;
          else
          { varuse = V.GetVarUse(0, At);
            if (varuse == 0 || (varuse > 3 && varuse != 11)) errno = 3;
          }
        }
        // IF SUCCESS in retrieving the thing:
        if (errno == 0)
        {// LOOKUP:
          if (Which == 390)
          { if (varuse == 11) // array
            { int inslot = V.GetVarPtr(0, At);
              int outslot = V.GenerateTempStoreRoom(R.Store[inslot].DimSz);
              V.CopyStoreroom(inslot, outslot, true); // 'true' stops re-creating sizing fields already set by the last step.
              result.I = outslot;
            }
            else result.X = V.GetVarValue(0, At); // scalar
            break; // EXIT POINT FOR VALID VARIABLE
          }
         // TOUCH_ARRAY:
          else
          { if (varuse == 11 && Args[1].I == -1 && Args[2].I == -1)
            { int varslot = V.GetVarPtr(0, At);
              double[] doodad = R.Store[varslot].Data;
              int indx = Convert.ToInt32(Args[1].X);
              if (indx >= 0 && indx < doodad.Length)
              { result.X = 1.0; // 'true' (unless operation not identified)
                double chx = 0;  int slit = Args[3].I;
                if (slit == -1) chx = Args[3].X;  else chx = R.Store[slit].Data[0];
                if      (chx == 43) doodad[indx] += Args[2].X; // '+' -- trying to do these in the most likely order of use
                else if (chx == 35) doodad[indx]  = Args[2].X; // '#'
                else if (chx == 45) doodad[indx] -= Args[2].X; // '-'
                else if (chx == 42) doodad[indx] *= Args[2].X; // '*'
                else if (chx == 47) doodad[indx] /= Args[2].X; // '/'
                else result.X = 0.0;
              }
            }
            break;
          }
        }
        // ERROR:
        if (Which == 391) break; // 'TOUCH_ARRAY' - just return FALSE.
        // ERROR IN 'LOOKUP':
        // One-arg. version: Crasho!
        if (NoArgs < 3)
        { if (errno == 1) return Oops(Which, "the variable name is not present (did you forget the quote marks?");
          if (errno == 2) return Oops(Which, "the supplied name does not match any main program variable names");
          if (errno == 3) return Oops(Which, "the variable was identified, but that variable is currently unassigned");
          return Oops(Which, "not quite sure what went wrong..."); // should never happen
        }
        // Two+ arg. version: no crash, just return the appropriate thingo.
        { int errslot;  double errX;
          if (errno == 3) { errslot = Args[2].I;  errX = Args[2].X; }
          else  { errslot = Args[1].I;  errX = Args[1].X; }
          if (errslot >= 0)
          { int outslot = V.GenerateTempStoreRoom(R.Store[errslot].DimSz);
            V.CopyStoreroom(errslot, outslot, true); // 'true' stops re-creating sizing fields already set by the last step.
            result.I = outslot;
          }
          else result.X = errX;
          break;
        }
      }
      case 392: // COMMANDLINE ( bool AsOneLine ) -- Display the command line which invoked this MonoMaths instance. (The first entry
      // will always be the path and name of the MonoMaths.exe file - path may be omitted if was called from another program within the
      // same directory as MonoMaths.exe, which would be rare). The result is always a chars.-type array, but its structure depends on
      // the argument. If TRUE, the command line elements (as parsed by C#) are rejoined with a space between each, and a list array is
      // returned. If FALSE, the elements are transferred from the System.Environment string[] object to a jagged matrix with the padder
      // being the space character.
      {
        string[] cmdLine = System.Environment.GetCommandLineArgs();
        bool asListArray = (Args[0].X != 0.0);
        int noCmdArgs = cmdLine.Length;
        double[] outdata;
        string ss;
        if (asListArray)
        { ss = "";
          for (int i=0; i < noCmdArgs; i++)
          { ss += cmdLine[i];  if (i < noCmdArgs)  ss += " "; }
          outdata = StringToStoreroom(-1, ss);
          result.I = V.GenerateTempStoreRoom(outdata.Length);
        }
        else // generate a jagged matrix:
        { // Find the longest item:
          int len, longestLen = 0;
          for (int i=0; i < noCmdArgs; i++)
          { if (cmdLine[i].Length > longestLen) longestLen = cmdLine[i].Length; }
          outdata = new double[noCmdArgs * longestLen];
          // Now pack them into what will become a matrix:        
          for (int i=0; i < noCmdArgs; i++)
          { ss = cmdLine[i];
            len = ss.Length;
            if (len == 0) { ss = " ";  len = 1; }
            double[] dubbity = StringToStoreroom(-1, ss);
            dubbity.CopyTo(outdata, i * longestLen);
            result.I = V.GenerateTempStoreRoom(longestLen, noCmdArgs);
          }
        }
        R.Store[result.I].Data = outdata;
        R.Store[result.I].IsChars = true;
        break;
      }


//      case 392: // CMDLINE ( scalar WhichArg / array TheLot ) -- (1) arg. scalar: if the arg. with that index exists, it is
//      // returned as a chars. array. If not, the return is a single SPACE (not the 'empty' array). (2) arg. ANY array:
//      // returns a jagged array - padded with spaces (unicode 32) as needed - of all command line arguments,
//      // starting with the program name itself - hence will always have at least one row.
//      {
//        string[] cmdLine = System.Environment.GetCommandLineArgs();
//        int noCmdArgs = cmdLine.Length;
//        string ss = "";
//        bool isScalar = (Args[0].I == -1);
//        if (isScalar)
//        { int ndx = Convert.ToInt32(Args[0].X);
//          if (ndx >= 0 && ndx < noCmdArgs) ss = cmdLine[ndx];
//          else ss = " "; // a space
//          result.I = V.GenerateTempStoreRoom(ss.Length);
//        }
//        else // arg. is any array, so put all args. into a jagged matrix (even if only one):
//        { int len, maxLen = 0;
//          for (int i=0; i < noCmdArgs; i++) { if (cmdLine[i].Length > maxLen) maxLen = cmdLine[i].Length; }
//          string tt = "";
//          for (int i=0; i < noCmdArgs; i++)
//          { len = cmdLine[i].Length;
//            tt = cmdLine[i] + ' '._Chain(maxLen - len); // No harm if the last term is zero.
//            ss += tt;
//          }
//          result.I = V.GenerateTempStoreRoom(maxLen, noCmdArgs);
//        }
//        StringToStoreroom(result.I, ss);
//        R.Store[result.I].IsChars = true;
//        break;
//      }
      case 393: // REPOSITION (Exactly four values, in any scalar-array mix). Values are: Left, Top, Width, Height.
      // If any is negative, that dimension is unaltered. Oversized values are cut back - see called methods for details.
      // RETURNS an array of size 4: [new left, new top, new width, new height]. If none changed, simply returns current values.
      {
        double[] indata = AccumulateValuesFromArgs(Args);
        if (indata.Length != 4) return Oops(Which, "four values must be supplied (in any scalar / array mix)");
        Tetro tx = MainWindow.ThisWindow.ChangeWindowLocation(indata);
        double[] outdata = new double[] { (double) tx.X1, (double) tx.X2, (double) tx.X3, (double) tx.X4 };
        result.I = V.GenerateTempStoreRoom(4);
        R.Store[result.I].Data = outdata;
        break;
      }
      case 394: // REPARTITION (array WhichWindow, scalar NewSize) -- does NOT change size of whole MonoMaths window, just
      // size of the two windows. Always returns an array of size 2: [final 'RequestHeight' of asst. window,  final ditto results window].
      // If WhichWindow[0] is not 'A' or 'R' (case-sens), no changes; if NewSize (rounded) is negative, no changes. NewSize < 20 --> 20;
      // NewSize > (sum of current RequestHeights - 20), it is reset to that value. No promises that outcome will be as requested,
      // but so far it always seems to work.
      // If 0 < NewSize <= 1, it is taken as a fraction of the screen height; otherwise as a pixel value. (No check for impractical values;
      //  Gtk will do whatever it feels like, if NewSize is legal but ridiculous.)
      // Nothing here raises an error, and at least the existing heights are always returned. If WhichWindow is scalar, it should be
      // the unicode of 'A' or 'R'. If NewSize is an array, it is taken as 0.
      { int whichwindow = Convert.ToInt32(Args[0].X);
        if (Args[0].I >= 0) whichwindow = Convert.ToInt32(R.Store[Args[0].I].Data[0]);
        char ch = '?';  if (whichwindow == 65) ch = 'A'; else if (whichwindow == 82) ch = 'R';
        double x = Args[1].X;
        if (x <= 1.0) x *= MainWindow.ScreenHeight;
        int newsize = Convert.ToInt32(x);
        Duo dewie = MainWindow.ThisWindow.Repartition(ch, newsize);
        result.I = V.GenerateTempStoreRoom(2);
        R.Store[result.I].Data = new double[] {dewie.X, dewie.Y};
        break;
      }
      case 395: // PALETTE ( [matrix PaletteStrings, ] Index ) -- Returns the name of the colour in the palette. If just one argument,
      // the system palette is used (see its definition below), the string returned being F.Palette[ |Index| % length of F.Palette].
      // If two, the first must be a matrix, taken as a 'jagged' matrix of strings constituting a palette.
      // Note that the 2-arg. version may be used with any matrix, jagged or not; 'palette(Foo, n)' would differ from
      // 'Foo[n]' only in the correction applied to n, as above, and in that the return has the chars. setting TRUE, irrespective of input setting.
      { double[] output = null;
        int ndx = Convert.ToInt32(Args[NoArgs-1].X);   if (ndx < 0) ndx = -ndx;
        if (NoArgs == 1) output = StringToStoreroom(-1, Palette[ndx % Palette.Length]);
        else // a user-supplied palette:
        { bool isOK = false;
          int palSlot = Args[0].I;
          if (palSlot >= 0)
          { StoreItem strim = R.Store[palSlot];
            if (strim.DimCnt == 2)
            { int noRows = strim.DimSz[1],  noCols = strim.DimSz[0];
              output = strim.Data._Copy( (ndx % noRows)*noCols, noCols);
              isOK = true;
            }
          }
          if (!isOK) return Oops(Which, "For the two-arg. form, the first arg. must be a matrix, the palette)");
        }
        result.I = V.GenerateTempStoreRoom(output.Length);
        R.Store[result.I].Data = output;   R.Store[result.I].IsChars = true;
        break;
      }
      case 396: // BLOCKFILE(array FileName [, array DoWhat [, array Message ] ] ) -- block / unblock menu saving of stated file.
      // If DoWhat is missing or starts with (case-sensitive) 'B', then the name is added to an internal list. (The list does not
      //  exist till the user adds a file name to it. Any number of file names may be added. The list is eniolated at each startup
      //  which calls R.KillData().) Files named on this list cannot be saved using menu item "File|Save" or "File|SaveAs".
      // 'FileName':
      //    Standard abbrevs. allowed, e,g, "~" for personal home directory; no path --> use current path.
      //    Special code: "!" followed by any text (usually a file name or partial path + file name) will prohibit any saving of
      //      a file path+name which contains this as its suffix.
      //    There are NO CHECKS on the validity of the name; a faulty name would simply have no effects on program operation.
      // 'DoWhat': case-sens. First char.: 'B'(lock), 'U'(nblock if has been previously blocked); 'A'(ll files UNblocked); '?' - no
      //    block / unblock action, but return a list of blocked files, delimited by "|"; if none, returns space.
      //    In the case of 'A' or '?', the file name is not accessed, so e.g. may be just " ", or even a scalar.
      //    If DoWhat is unrecognizable, or if FileName is invalid, an error is raised; so e.g. the IOMessage system is not used.
      // 'Message': Omit this, and MonoMaths shows its own saving failure message (good enough for most purposes). If you want your own, or none,
      //    the contents of Message will replace it. (If Message is present but just a space, there will be no message, just an empty dialog box.)
      //    Only accessed for option 'B'.
      // RETURN: a list of file names blocked by the end of the function's action, delimited by '|'; or " ", if none.
      //    Note that if a Message was supplied when blocking a file, the return for that file will be "<file name>#<message>".
      // NB: No check for file name duplication, so don't put this instruction in a loop!!!
      {
        int nameSlot = Args[0].I; // Leave testing its arrayhood till after servicing cases where it is not needed.
        char doWhat = ' ';
        if (NoArgs == 1) doWhat = 'B';
        else
        { string ss = StoreroomToString(Args[1].I);
          if (ss.Length > 0) doWhat = ss[0];
        }
        if ("BUA?".IndexOf(doWhat) == -1) return Oops(Which, "unrecognized 2nd. arg.");
        List<string> blockade = MainWindow.ThisWindow.BlockFileSave;
        bool isNull = (blockade == null);
        if (doWhat == 'A') { if (!isNull) blockade.Clear(); }
        else if (doWhat != '?')
        { // Retrieve file name, allowing for standard abbreviations:
          if (nameSlot == -1) return Oops(Which, "the 1st. arg. must be an array containing the file name");
          string filename = StoreroomToString(nameSlot, true, true);
          if (filename[0] != '!') // if it is, just leave the name as is, for the main window method to sort out.
          { string[] flame = filename._ParseFileName(MainWindow.ThisWindow.CurrentPath);
            filename = flame[0] + flame[1];
          }
          // Do the action:
          if (doWhat == 'B')
          { if (isNull) { blockade = new List<string>();  isNull = false; }
            if (NoArgs > 2 && Args[2].I != -1)
            { string ss = StoreroomToString(Args[2].I);
              blockade.Add(filename + "#" + ss);
            }
            else blockade.Add(filename);
          }
          else if (!isNull) // doWhat = 'U':
          { for (int i=0; i < blockade.Count; i++)
            { if (blockade[i] == filename)
              { blockade.RemoveAt(i);  break; }
            }
          }
        }
        MainWindow.ThisWindow.BlockFileSave = blockade;

        // In all cases, return the names of blocked files:
        string outStr = " ";
        if (!isNull && blockade.Count > 0) outStr = String.Join("|", blockade);
        result.I = V.GenerateTempStoreRoom(outStr.Length);
        StringToStoreroom(result.I, outStr);
        break;
      }
      case 397: // BTNRELEASE(). Returns data obtained from the latest mouse button release, as recorded in the Main Program
      // instance Gdk.EventButton Glutton. In the process, resets that to NULL.
      // RETURNS a simple copy of the main window's ButtonReleaseData, which is a double[15]. This will be all zeroes, if no
      // release button event since user pgm. startup (remembering that release button events only occur for a click on SELECTED text).
      // Each access to this function resets ButtonReleaseData[0] - the button type indicator - but does not touch other values.
      // FIELDS of the returned array:
      //  [0] = button clicked (0 if none since user run startup OR if reset by fn. 'btnrelease()'; left = 1; middle = 2; right = 3.
      //          NB: function 'btnrelease' resets ONLY [0]; all other values carry over from the last click (or are 0, if no clicks this run).
      //  [1] = time of this click in msecs. since start of 1 AD.
      //  [2] = time in msecs. since last click, or same as [1] if this is the first click this run.
      //  [3], [4] currently unused, reserved for future timing entries.
      //  [5] = CNTRL down, [6] = ALT down, [7] = SHIFT down (*** doesn't happen, as Mono's TextView hijacks shift + mouse click.)
      //  [8] to [10] unused; reserved for other modifier keys detection in the future, if nec.
      //  [11, 12] = (X, Y) of click relative to the top of this window.
      //  [13, 14] = (X, Y) of click relative to the top of the screen.
      {
        double[] outdata = MainWindow.ThisWindow.ButtonReleaseData._Copy();
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        MainWindow.ThisWindow.ButtonReleaseData[0] = 0.0; // Only the button code is reset.
        break;
      }
      case 398: // CURSORDATA( [bool AddData] ) -- If arg. absent or FALSE, returns a chars. array consisting of the word at the cursor,
      // or just " " if none. (Note: never the 'empty' array.) If arg. present and TRUE, returns the same (still tagged as chars.), but
      // a delimiter MAXREAL comes straight after the word (or space), and then comes the following elements: [ptr. to first char. of word]
      // [ptr. to last char.] [bool: selection preexisted]. If there was no word found, the first two are both -1, so the full array would be:
      // { space  MAXREAL -1  -1  0 }.
      { bool verbose = (Args[0].X != 0.0);
        Strint2 cursory = MainWindow.ThisWindow.WordAtCursor(false);
        string sword = cursory.SX;
        if (sword == "") sword = " ";
        int len = sword.Length;
        double[] outdata;
        if (verbose)
        { outdata = new double[len + 4];
          double[] dudu = StringToStoreroom(-1, sword);
          dudu.CopyTo(outdata, 0);
          outdata[len] = double.MaxValue;
          outdata[len+1] = (double) cursory.IX;    outdata[len+2] = (double) cursory.IY;
          if (cursory.SY == "S") outdata[len+3] = 1.0;
        }
        else outdata = StringToStoreroom(-1, sword);
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
        R.Store[result.I].IsChars = true;
        break;
      }
      case 399: // PERSISTENT_ARRAY (scalar Anything OR array DataIn) -- scalar arg / NO arg (preferred): returns the persistent array's contents.
      // Array arg: sets the persistent array with the given data: this form is VOID.
      { int slot = Args[0].I;
        if (slot == -1)
        { double[] dudu = V.PersistentArray;
          result.I = V.GenerateTempStoreRoom(dudu.Length);
          R.Store[result.I].Data = dudu;
        }
        else V.PersistentArray = R.Store[slot].Data._Copy();
        break;
      }
      case 400: // LETTERCASE(array InString, array Model) -- Model is exactly as in the blurb at
      // "public static string _CaseMatch(this string inStr, string model)" in JExtensions.cs. B..d if I'll repeat all that here.
      // Get it wrong and the original string is returned; otherwise the altered string returned.
      // Error raised only if either arg. is scalar.
      { int inslot = Args[0].I, modelslot = Args[1].I;
        if (inslot == -1 || modelslot == -1) return Oops(Which, "both args. must be arrays");
        string indata = StoreroomToString(inslot),  modeldata = StoreroomToString(modelslot, true, true);
        string outdata = indata._CaseMatch(modeldata); // That function returns the original string, if modeldata is invalid.
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        StringToStoreroom(result.I, outdata);
        R.Store[result.I].IsChars = true;
        break;
      }
      case 401: // BRACKETLEVEL(char array String, array/scalar Opener, array/scalar Closer, scalar CharPointer)
      // If Opener or Closer is an array, only the first character is accessed; i.e. openers and closers cannot be more than one char. long.
      // RETURNS an array of size 3: [0] = level (0 upwards) or error indicator (-1); [1] = ptr to corresponding opener at / to left of CharPointer;
      //  or -1, if none. [2] = ptr to corresponding opener at / to right of CharPointer, or -1 if none.
      // Re the level (element [0]): (A) NO ERROR: The base level (e.g. of text with no brackets) is 0. If the pointer is at an opener
      //  or closer, the level is that of text which is inside the parenthesis which it borders. (B): ERROR: value is -1.
      //  The other elements are still filled and are probably accurate, but I have not tested every silly possibility, so no promises.
      // Crashing error: CharPointer out of range - i.e. no adjustment is made here, as this would allow coding errors to create
      //  hard-to-trace erratic behaviour in your program.
      {
        string inStr = StoreroomToString(Args[0].I);    if (inStr == "") return Oops(Which, "the 1st. arg. must be an array");
        int nope, nose, inLen = inStr.Length;
        if (Args[1].I == -1) nope = Convert.ToInt32(Args[1].X);
        else nope = Convert.ToInt32(R.Store[Args[1].I].Data[0]);
        if (Args[2].I == -1) nose = Convert.ToInt32(Args[2].X);
        else nose = Convert.ToInt32(R.Store[Args[2].I].Data[0]);
        char opener = Convert.ToChar(nope),  closer = Convert.ToChar(nose);
        if (Args[3].I >= 0) return Oops(Which, "the 4th. arg. must be scalar");
        int ptr = Convert.ToInt32(Args[3].X);
        if (ptr < 0 || ptr >= inLen) return Oops(Which, "the 4th. arg. points to somewhere outside the 1st. arg. array");
        Quad quoo = JS.BracketsCheck(ref inStr, opener, closer);
        int[] bracken = JS.NestLevel(inStr, opener, closer, ptr, true);
        double level = (double) bracken[5],  opPtr = (double) bracken[0],  clPtr = (double) bracken[1];
        if (!quoo.B) level = -1;
        if (clPtr >= inStr.Length) clPtr = -1; // no closer to the right.
        double[] outdata = new double[] { level, opPtr, clPtr };
        result.I = V.GenerateTempStoreRoom(outdata.Length);
        R.Store[result.I].Data = outdata;
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
    case 412 : // PREVALENCE(InArray, ...) - Two distinct modes, distinguished by whether 2nd. arg. is SCALAR.
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

//    case 423: // SIGMOID(scalar/array X [, scalar IndexCoefficient [, scalar DenomCoefficient ] ] -- sigmoid function.
//    // result = 1 / (1 + D.C. * e^-(I.C. * X) ).
//    // Returns variable of same structure as X. Defaults: both coefficients = 1.
//    { int inslot = Args[0].I;
//      double[] indata;
//      StoreItem initem = null;
//      if (inslot == -1) indata = new double[] { Args[0].X };
//      else
//      { initem = R.Store[inslot];
//        indata = initem.Data;
//      }
//      int dataLen = indata.Length;
//      double IndexCoeff = 1.0, DenomCoeff = 1.0;
//      if (NoArgs > 1) IndexCoeff = Args[1].X;
//      if (NoArgs > 2) DenomCoeff = Args[2].X;
//      double[] outdata = new double[dataLen];
//      for (int i=0; i < dataLen; i++)
//      { outdata[i] = 1.0 / (1.0 + DenomCoeff * Math.Exp(-IndexCoeff * indata[i]) );
//      }
//      if (inslot == -1) { result.X = outdata[0]; break; } // Scalar in --> Scalar out.
//      result.I = V.GenerateTempStoreRoom(initem.DimSizes);
//      R.Store[result.I].Data = outdata;
//      break;
//    }



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

    case 432: // GETSEGMT(array InArray, array / scalar Delimiter, scalar WhichSegment ) -- See above notes first. Returns the indicated segment;
    // or [NaN] if the indicated segment is empty. CRASHES if WhichSegment is out of range.
    //  If Delimiter is an array, only its first element is used. The returned segment has the chars. rating of InArray.
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
      { if (segmentWidth == 0) outdata = new double[] {double.NaN}; 
        else outdata = indata._Copy(leftDelimPtr+1, segmentWidth);
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
  case 438: // EQUAL (array Standard, array Test,  array / scalar HowFar)
  // Returns TRUE if the arrays are exactly equal, either in toto (no final scalar), or equal for the length (from the start) indicated
  //  by the 3rd arg, if present. Otherwise returns FALSE. 
  // TWO FORMS: (1) 3rd. arg. is scalar: its value is the length of data strip to compare. (If oversized or ≤ 0, set to length of Standard.)
  //  (2) 3rd. arg. an array: If that subarray is present in Standard, then the length to search is the length up to but not including the
  //   subarray. If not found in Standard, the whole of Standard is used.
  // If you want more information about how the two arrays match, use function COMPARE instead. This one has minimal functionality for speed. 
  {
    int slotStandard = Args[0].I,  slotTest = Args[1].I,  slotHowFar = Args[2].I;
    if (slotStandard == -1 || slotTest == -1) return Oops(Which, "1st. 2 args. must be arrays");
    double[] dataStandard = R.Store[slotStandard].Data,  dataTest = R.Store[slotTest].Data;
    int lenStandard = dataStandard.Length,  lenTest = dataTest.Length;
    int lengthToCheck;
    if (slotHowFar == -1) // 3rd. arg. is scalar:
    { lengthToCheck = Convert.ToInt32(Args[2].X);
      if (lengthToCheck > lenStandard  ||  lengthToCheck <= 0) lengthToCheck = lenStandard;
    }
    else // 3rd. arg. an array:
    { double[] dataHowFar = R.Store[slotHowFar].Data;
      lengthToCheck = dataStandard._Find(dataHowFar);
      if (lengthToCheck == -1)  lengthToCheck = lenStandard;
    }
    if (lengthToCheck <= lenTest) // if not, then the return must be FALSE, so just break leaving result.X as 0.
    { result.X = 1.0;
      for (int i=0; i < lengthToCheck; i++)
      { if (dataTest[i] != dataStandard[i]) { result.X = 0.0; break; } }
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
    case 443: // INJECT ([scalar or array WithWhat], array VarNames [, bool ReturnStats) ] : Injects values into the scalars named in the
    // string VarNames. The first non-identifier char. will be taken as the delimiter; if none, the whole array is regarded as a single
    // variable name. Suppose VarNames is "x|y|z"; then V.Vars will be checked for each of var. names "x", "y" and "z". If, say, "y" 
    // does not exist in V.Vars, then it still uses up a value supplied according to WithWhat, but nothing else happens. If "y" has a .Use
    // other than 0 or 3, a crash occurs. (This includes using fn. names or nonallowed name chars.) If Use is 0, then the var. is registered
    // as a scalar, and is assigned Use = 3. Note that empty strings are allowed: they use up values, which may be a desired effect.
    // The delimiter can be a space; but VarNames is trimmed before looking for the delimiter. Given a non-space delimiter, spaces not
    //   internal to a name are ignored (e.g. "cat  |  do g" is taken as two potential names, 'cat' and 'do g'; the 2nd. will crash).
    // WithWhat: If absent, named vars. are assigned 0, 1, 2, 3, .... If a scalar, all named scalars get that value. If an array,
    //   values are assigned in order. If not enough values in WithWhat, values are cyclically reused; if too many values in WithWhat,
    //   excess values are ignored.
    // RETURNED: (1) - no final scalar argument: The number of substrings in VarNames, which is also the number of values used up from
    //   WithWhat (or internally, if no WithWhat). The function can safely be used as VOID, as errors all crash. But if you really want
    //   some statistics, then the final scalar arg. must be PRESENT and TRUE; if so, then a list array is returned, of size 3:
    //   [0] = No. of substrings in VarNames, and so no. WithWhat values used up; [1] = No. of strings which are valid ID names
    //   but do not occur elsewhere at this fn. level (so remain nonexistent, as this fn. cannot create them at run-time);
    //  [2] = No. of empty substrings.
    {
      // Sort out what arg. is what:
      string ss = ArgTypesCode(Args, ' '); // length = no. args., and [i] is 'S' for scalar, 'A' for array.
      bool returnArray = false, noWithWhat = false, withWhatIsScalar = false;
      int stringsArg = 0;
      if      (ss == "A")  { noWithWhat = true; }
      else if (ss == "AA" || ss == "SA") { stringsArg = 1;   withWhatIsScalar = (ss[0] == 'S');  }
      else if (ss == "AS") { noWithWhat = true; returnArray = (Args[1].X != 0.0); }
      else if (ss == "AAS" || ss == "SAS"){ stringsArg = 1;    returnArray = (Args[2].X != 0.0);   withWhatIsScalar = (ss[0] == 'S'); }
      else return Oops(Which, "wrong deployment of arg. types");
      // Develop an array of potential variable names:
      string instr = StoreroomToString(Args[stringsArg].I, true, true, true);
      if (instr == "") return Oops(Which, "the array of variable names cannot be empty");
      string[] varNames;
      int n = instr._IndexOfNoneOf(P.IdentifierChars);
      if (n == -1) varNames = new string[] { instr };
      else varNames = instr.Split(new char[] { instr[n] }, StringSplitOptions.None); // empty substrings allowed.
      // Develop an array of values to be assigned to such variables:
      int noSubstrings = varNames.Length;
      double[] varValues = new double[noSubstrings];
      if (noWithWhat) { for (int i=0; i < noSubstrings; i++) varValues[i] = i; }
      else if (withWhatIsScalar) { for (int i=0; i < noSubstrings; i++) varValues[i] = Args[0].X; }
      else
      { double[] invals = R.Store[Args[0].I].Data;
        int invalsLen = invals.Length;
        for (int i=0; i < noSubstrings; i++) varValues[i] = invals[i % invalsLen];
      }     
      // Do the assigning, keeping count of valid assignments:
      int Fn = R.CurrentlyRunningUserFnNo,  At;
      int noUnreferenced = 0, noEmpties = 0;
      for (int i=0; i < noSubstrings; i++)
      { string var_name = varNames[i].Trim();
        if (var_name == "")  noEmpties++; // an empty name will (notionally) use up a value of varValues, but have no other effect.
        else
        { At = V.FindVar(Fn, var_name);
          if (At < 0) // no such variable name registered; therefore is either unused elsewhere OR is an invalid name...
          { char what_it_is;
            P.NameLocn(var_name, Fn, true, out what_it_is);
            if (what_it_is == ' ') noUnreferenced++; // valid name, but unused elsewhere at the same function level. Again, a varValue used up.
            else // A crashable error:
            { ss = "";
              if (what_it_is == 'F') ss = " system function name";
              else if (what_it_is == 'U') ss = " user function name";
              else ss = "n illegal name";
              return Oops(Which, "the string '" + var_name + "' is a" + ss);
            }
          }
          else
          { int usage = V.GetVarUse(Fn, At);
            if (usage != 0 && usage != 3)
            { return Oops(Which, "the variable named '" + varNames[i].Trim() +"' is already defined as an array, constant or system variable'");
            }                             
            V.SetVarValue(Fn, At, varValues[i]);
          }
        }
      }
      if (returnArray)
      { result.I = V.GenerateTempStoreRoom(3);
        R.Store[result.I].Data = new double[] { (double) noSubstrings, (double) noUnreferenced, (double) noEmpties };
      }
      else result.X = noSubstrings;
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
    case 448: // F448MONOTONICITY(array Subject,  bool ExpectAscending [, scalar StartIndex [, scalar VirtualZero ] ] --
    // Checks for places in the array data strip where the trend of data to rise or to fall changes to the opposite trend (an 'exception point').
    // Also detects duplicates, though these are not registered as 'exception points'.
    // 'Subject' - array structure ignored. Should not contain NaN, as then results would be unpredictable.
    // 'ExpectAscending' - boolean; if false, expect values to be descending.
    // 'VirtualZero' - quantities this close or closer to zero than this will be regarded as zero. Can be 0; if negative, will be taken as 0.
    // RETURNED: An array of size 4: [No of exception points, First exception point,  No duplicates, First contiguous duplicate].
    //  If no exception points, both elements [0] and [2] will be 0; if no contiguous duplicates, [1] and [3] will be zero.
    //  Example: Ascending order expected, Subject is [10, 30, 50, 20, 30, 10, 10]: There are 2 exception points, index 3 (value 20)
    //   and index 5 (value 1). There is also 1 duplicate, index 6 (the final 10). So the returned array would be [2, 3, 1, 6].
    {
      int inslot = Args[0].I;  if (inslot == -1) return Oops(Which, "1st. arg. must be an array");
      double[] indata = R.Store[inslot].Data;
      int insize = indata.Length;
      bool isAscending = (Args[1].X != 0.0);
      int startPtr =  (NoArgs > 2)  ?  Convert.ToInt32(Args[2].X)  :  0;
      if (startPtr < 0  ||  startPtr > (insize-2) ) return Oops(Which, "start index must lie between 0 and the penultimate element of the array");
      double virtZero =  (NoArgs > 3)  ?  Args[3].X  :  0.0;
      if (virtZero < 0.0) virtZero = 0.0;
      int noExceptionPts = 0,  firstExceptionPt = 0,   noDuplicates = 0,   firstDuplicate = 0;
      double diff, absdiff;
      for (int i = startPtr+1; i < insize; i++)
      { diff = indata[i] - indata[i-1];
        absdiff = Math.Abs(diff);
        if (absdiff <= virtZero)
        { noDuplicates++;  if (noDuplicates == 1) firstDuplicate = i; }
        else if (isAscending  ^ (diff > 0.0) )
        { noExceptionPts++; if (noExceptionPts == 1) firstExceptionPt = i; }
      }
      result.I = V.GenerateTempStoreRoom(4);
      R.Store[result.I].Data = new double[] { (double) noExceptionPts, (double) firstExceptionPt,  (double) noDuplicates,  (double) firstDuplicate };
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

    // . . . . . . . . . . . . .


      default: break;                                //default  //last
    }
  // - - - - - - - - - - - - - - - - - - - -
    return result;
  }

//==========================================================

//hybrid
// Used by RunSysFn(..). Effects of this fn. on Result must be consistent with
//  what RunSysFn(..) requires. Hence, Result.B enters as TRUE. XX represents
//  the first argument (as is, if scalar; or an element in the array, if array);
//  YY is the second argument, if any (always scalar), or else 0.0.
  public static void Hybrids (short Which, double XX, double YY, ref Quad result)
  { switch(Which)
    { case 5:  result.X = XX * XX; break; // SQR(XX)
      case 6:  result.X = XX * (double) 180 / Math.PI; break; // DEG(XX)
      case 7:  result.X = XX * Math.PI / (double) 180; break; // RAD(XX)
      case 8:  result.X = Math.Sin(XX); break; // SIN(XX)
      case 9:  result.X = Math.Cos(XX); break; // COS(XX)
      case 10: result.X = Math.Tan(XX); break; // TAN(XX)
      case 11:  case 12:                       // AECSIN(XX) / ARCCOS(XX)
      { if (XX > 1.0 || XX < -1.0)
        { if (YY != 0.0) { if (XX > 1.0) XX = 1.0;  else XX = -1.0; } // 2nd. arg. boolean - clip out-of-limits args. without raising error.
          else { result = Oops(Which, "args. must lie between -1.0 and +1.0 inclusive (no allowance made for tiny numerical errors)"); break; }
        }
        if (Which == 11) result.X = Math.Asin(XX);
        else result.X = Math.Acos(XX);
        break;
      }
      // case 13 - ARCTAN - used to be here, but due to argument considerations had to be taken out of the HYBRIDS block.
      case 14: result.X = Math.Abs(XX); break;  // ABS(XX)
      case 15: if (XX >= 0.0) result.X = Math.Sqrt(XX); else result = Oops(Which, "negative arg. not allowed"); break; // sqrt(XX)
      case 16: // FACT(XX); FACT(XX,YY) (for fact(XX) / fact(YY)).
      { int btm = (int) Math.Round(YY), top = (int) Math.Round(XX);
        if (btm == 0) btm = 1; // the case where 2nd. arg. was omitted by user.
        if (top == 0) top = 1; // on the notion that gamma(n+1) = n!, and gamma(1) = 1. Proves useful where n! is a denom, to avoid div. by 0.
        if (top < 0 || btm < 1 || top > 169 || btm > 169) result = Oops(Which, "arguments must lie between 0 and 169");
        else if (btm > top) result = Oops(Which, "the first argument must be > = the second");
        else
        { if (top == 1 || top == btm) result.X = 1.0;
          else result.X = JM.Factorial(top, btm).X; }
        break;
      }
      case 17: // LOGFACT(XX); LOGFACT(XX,YY) (for logfact(XX) / logfact(YY)).
      { double big = (double) Int32.MaxValue;
        if (XX > big || YY > big) { result = Oops(Which, "arg.(s) cannot exceed {0}", Int32.MaxValue); break; }
        int btm = (int) Math.Round(YY), top = (int) Math.Round(XX);
        if (btm == 0) btm = 1; // the case where 2nd. arg. was omitted by user.
        if (top < 1 || btm < 1) { result = Oops(Which, "arg.(s) must be > = 1"); break; }
        else if (btm > top) { result = Oops(Which, "the first argument must be > = the second"); break; }
        if (top == 1 || top == btm) result.X = 0.0;
        else result.X = JM.Factorial(top, btm, 0, 2).X;
        break;
      }
      case 18: result.X = Math.Exp(XX); break; // EXP(XX) ( = 2.71828.. raised to power of arg.)
      // LN(XX):
      case 19: if (XX > 0.0) result.X = Math.Log(XX);  else result = Oops(Which, "the arg. must be positive and nonzero");  break;
      case 20: // LOG(XX), LOG(XX,YY) -  log to base 10 / log to base YY.
      { if (XX <= 0.0) { result = Oops(Which, "the arg. must be positive and nonzero"); break; }
        if (YY == 0.0) result.X = Math.Log10(XX); // no 2nd. arg.
        else // 2nd. argument is the log base:
        { if (YY < 0.0) result = Oops(Which, "the  base for 'log' should not be negative");
          else result.X = Math.Log(XX, YY);
        }
        break;
      }
      case 21: // ROUND(XX), ROUND(XX, YY). If YY supplied, and is 0 (default) to 14, that many decimal places present. 
        // E.g. 'round(1.678, 2)' --> 1.68. Outsized YY corrected to within these limits (no error raised).
      { double YR = Math.Round(YY);
        if (YR > 15.0) YR = 15.0;   else if (YR < -15.0) YR = -15.0;
        if (YR >= 0.0) // then we can directly use .NET's rounding function: 
        { result.X = Math.Round(XX, (int) YR, MidpointRounding.AwayFromZero); } // None of this "banker's rounding" stuff.
        else // For negative values we have to have a multiplier before rounding:
        { double multiplier = Math.Pow(10, YR); // 10 ^ YR.
          result.X = Math.Round(XX * multiplier, 0, MidpointRounding.AwayFromZero);
          result.X /= multiplier;
        }
        break;
      }
      case 22: // FRAC(XX)
      { XX = Math.Abs(XX);  result.X = XX - Math.Floor(XX); break; }

      case 23: // FLOOR(XX [, YY]) If a second argument, is as for 'round(.)' above. 'floor(1.678,2)' --> 1.67.
      { if (YY == 0.0) { result.X = Math.Floor(XX); break; } // commonest case, dealt with separately for speed.
        double YR = Math.Round(YY); if (YR > 15.0) YR = 15.0;  else if (YR < -15.0) YR = -15.0;
        double multiplier = Math.Pow(10, YR); // 10 ^ YR.
        result.X = Math.Floor(XX * multiplier);
        result.X /= multiplier;
        break;
      }
      case 24: // CEILING(XX [, YY]) If a second argument, is as for 'round(.)' above. 'ceiling(1.678,2)' --> 1.68.
      { if (YY == 0.0) { result.X = Math.Ceiling(XX); break; } // commonest case, dealt with separately for speed.
        double YR = Math.Round(YY); if (YR > 15.0) YR = 15.0; else if (YR < -15.0) YR = -15.0;
        double multiplier = Math.Pow(10, YR); // 10 ^ YR.
        result.X = Math.Ceiling(XX * multiplier);
        result.X /= multiplier;  
        break;
      }
      case 25: // MOD(XX,YY) - equivalent to XX mod |YY|. Sign of YY irrelevant; sign of XX is reproduced - e.g. mod(-11,5) --> -1.
      { Int64 n, IX = (Int64) Math.Round(XX);  Int64 IY = (Int64) Math.Round(YY);
        if (IY == 0) { result = Oops(Which, "either no divisor supplied, or the divisor rounds to zero");  break; }
        n = IX - IY*(IX/IY);
        result.X = (double) n;
        break;
      }
      case 26: // DIV(XX,YY) - absolute XX and YY are rounded, then divided. Intended for (approximately) integer values only.
      { Int64 IX = (Int64) Math.Round(XX),  IY = (Int64) Math.Round(YY);
        if (IY == 0) result = Oops(Which, "the second arg. rounds to 0, --> division by zero");
        else result.X = (double) (IX/IY);
        break;
      }
      case 27: // ISINTEGRAL(XX, YY) - if XX differs from round(XX) by more than YY, returns FALSE; otherewise TRUE. 
        // If YY is 0 (as when no 2nd. arg.) or is neg. it is readjusted to the default of 1e-10.
      { if (YY <= 0.0) YY = 1.0E-10;
        double x = Math.Round(XX,MidpointRounding.AwayFromZero);
        result.X = ( Math.Abs(x - XX) <= YY) ? 1.0 : 0.0;
        break;
      }
      case 48: // DEFRAC(XX);
      { if (XX < 0) result.X = Math.Ceiling(XX);  else result.X = Math.Floor(XX);  break; }
      case 86: // EVEN(XX[,YY]); returns 1 ('true') if XX is even (or if YY
        // is supplied, if YY exactly divides XX), otherwise 0. Use mod(..)
        // if wanting the modulo. NB: if YY is omitted or is 0, it is automatically
        // changed to 2; otherwise if it rounds to < +2, error thrown.
      { if (YY == 0.0) YY = 2.0; // before .Round(), to pick up omitted 2nd. arg.
        Int64 n, IX = (Int64) Math.Round(XX), IY = (Int64) Math.Round(YY);
        if (IY <= 1) { result = Oops(Which, "if a second arg. is supplied, it must round to an integer of 2 or more");  break; }
        n = IX - IY*(IX/IY);
        if (n == 0) result.X = 1.0; else result.X = 0.0;
        // this is a logic function, TRUE (1.0) if the modulo is zero.
        break; }
        case 351: // TOZERO(XX, YY) -- returns XX moved closer to zero as a number with precision YY (unless it already has that precision).
        { if (XX == 0.0) result.X = XX;
          else if (YY == 0.0) // commonest case, dealt with separately for speed.
          { if (XX > 0.0) result.X = Math.Floor(XX);  else result.X = Math.Ceiling(XX); }
          else 
          { double sign = 1.0; if (XX < 0) sign = -1.0;
            XX = Math.Abs(XX);
            double YR = Math.Round(YY); if (YR > 15.0) YR = 15.0;  else if (YR < -15.0) YR = -15.0;
            double multiplier = Math.Pow(10, YR); // 10 ^ YR.
            result.X = sign * Math.Floor(XX * multiplier);
            result.X /= multiplier;
          }  
          break;
        }
        case 352: // FROMZERO(XX, YY) -- returns XX moved closer to zero as a number with precision YY (unless it already has that precision).
        { if (XX == 0.0) result.X = XX;
          else if (YY == 0.0) // commonest case, dealt with separately for speed.
          { if (XX > 0.0) result.X = Math.Ceiling(XX);  else result.X = Math.Floor(XX); }
          else
          { double sign = 1.0; if (XX < 0) sign = -1.0;
            XX = Math.Abs(XX);
            double YR = Math.Round(YY); if (YR > 15.0) YR = 15.0;  else if (YR < -15.0) YR = -15.0;
            double multiplier = Math.Pow(10, YR); // 10 ^ YR.
            result.X = sign * Math.Ceiling(XX * multiplier);
            result.X /= multiplier;
          }  
          break;
        }
        case 404: // ISNAN(XX) -- returns TRUE or FALSE
        { result.X = 0.0;  if (double.IsNaN(XX)) result.X = 1.0;  break; }
        case 406: // FIXANGLE(XX [, YY] ) // Confine a radian angle to a 2PI range. YY = low value, 0 if omitted by user.
        { double toopie = 2.0 * Math.PI;
          while (XX > YY + toopie) XX -= toopie;
          while (XX < YY) XX += toopie;
          result.X = XX;
          break;
        }

    // . . . . . . . . . . . . .
      default: break;
    }

  }
//=======================================================================
// ERROR MESSAGE COMPILER               //error
/// <summary>
/// <para>RETURNED Quad object has .B set to FALSE, .S set to the string as described below, .I = -1, .X = 0.0.</para>
/// <para>'WhichFn': If greater than 0, then the function name will start the message - e.g.: "'foo': ". This prefix will be absent
/// if WhichFn is zero or negative. </para>
/// <para>'Description' is a C#-type format string (see "String.Format" in .NET help file for details). It will be
/// added after the function name as above (if any). Any number and sort of variables may go into 'Data', as long as
/// the type's 'ToString()' method gives useful output.</para>
/// <para>ORDINALS: If you write "{1}th", and Data[1] is an integer, then 'th' will be replaced (if nec.) by 'st', 'nd'
/// or 'rd' according to that integer's value.</para>
/// <para>SUBSTITUTIONS: Only one so far - 'arg.' converts to 'argument'.</para>
/// <para>EXAMPLE: Suppose fn. 999 is foo(.). Then "Oops(999, "The {0}th. arg. should be less than {1}, {2}",  1, 3.14159, "you clot")
/// --> a FnIO object with .B FALSE and .S = "'foo': The 1st. argument should be less than 3.14159, you clot".</para>
/// <para>NO TEST for sensible 'Which' or other argument errors, as this is internally called and the programmer has done his job properly...</para>
/// </summary>
  protected static Quad Oops(int WhichFn, string Description, params object[] Data)
  { string descrip = Description;
    bool StartWithName = (WhichFn > 0);
    if (StartWithName) descrip = "'" + SysFn[WhichFn].Name + "': "  + Description;
    // Look for and adjust ordinal number suffixes:
    int[] finds = descrip._IndexesOf("}th");
    if (finds != null)
    { bool success; // failure will not raise an error, but will simply result in 'th' not being replaced.
      for (int i=0; i < finds.Length; i++)
      { int find = finds[i];
        int n = descrip._LastIndexOf('{', 0, find); if (n == -1) continue;
        string ss = descrip._FromTo(n+1, find-1);
        int p = ss._ParseInt(out success);
        if (!success) continue;
        if (Data[p] is int) // has to be an integer, if the ordinal suffix is to be meaningful.
        { ss = ( (int) Data[p])._Ordinal(true, true); // Booleans --> return suffix only (not p), and with no fullstop.
          descrip = descrip._Scoop(find+1, 2, ss);
        }
      }
    }
    // Any other substitutions:
    descrip = descrip.Replace("arg.", "argument");
    descrip = descrip.Replace("args.", "arguments");
    // Finish the job:
    string tt;
    try // Without this test, MonoMaths mysteriously crashes if a system function has not supplied promised data to this method.
    { tt = String.Format(descrip, Data); }
    catch
    { tt = descrip; }
    return new Quad(-1, 0.0, false, tt);
  }



  /// <summary>
  /// <para>Convert the whole of a storeroom data strip into characters. If 'slot' is invalid,  simply returns an empty string. 
  /// (No errors are raised anywhere in this method.)</para>
  /// <para>If TrimEndStartEtc[0], all chars. SPACE and below are stripped off the end. Default: no trim.</para>
  /// <para>If TrimEndStartEtc[1], all chars. SPACE and below are stripped off the start. Default: no trim.</para>
  /// <para>OUT OF RANGE CHARS.: (This is where the 'Etc' part comes in.) If TrimEndStartEtc[2], then chars. above 'char.MaxValue'
  ///  are set to '∞'; below zero, set to '⊗'; NaN, set to '¿'. Default: below 0 --> 0, above max char --> max char (set by NET to $FFFF),
  ///  and NaN is set to 0.</para>
  /// </summary>
  public static string StoreroomToString(int slot, params bool[] TrimEndStartEtc)
  { return StoreroomToString(slot, 0, -1, TrimEndStartEtc); }

  /// <summary>
  /// <para>Convert the defined substrip of a storeroom data strip into characters. If 'slot' is invalid,  simply returns an empty string.</para>
  /// <para>If fromPtr &lt; 0, it is adjusted to 0. If 'extent' is either negative or oversized, it is adjusted to reach to the end of the 
  ///  strip. (If 0, the empty string is returned.)</para>
  /// <para>If TrimEndStartEtc[0], all chars. SPACE and below are stripped off the end. Default: no trim.</para>
  /// <para>If TrimEndStartEtc[1], all chars. SPACE and below are stripped off the start. Default: no trim.</para>
  /// <para>OUT OF RANGE CHARS.: (This is where the 'Etc' part comes in.) If TrimEndStartEtc[2], then chars. above 'char.MaxValue'
  ///  are set to '∞'; below zero, set to '⊗'; NaN, set to '¿'. Default: below 0 --> 0, above max char --> max char (set by NET to $FFFF),
  ///  and NaN is set to 0.</para>
  /// </summary>
  public static string StoreroomToString(int slot, int fromPtr, int extent, params bool[] TrimEndStartEtc)
  { if (slot < 0 || slot >= R.Store.Count || R.StoreUsage[slot]==0) return "";
    bool trimend = (TrimEndStartEtc.Length > 0 && TrimEndStartEtc[0]);
    bool trimstart = (TrimEndStartEtc.Length > 1 && TrimEndStartEtc[1]);
    bool specialChars = (TrimEndStartEtc.Length > 2 && TrimEndStartEtc[2]);
    char minChar = char.MinValue, maxChar = char.MaxValue;
    char Cneg = minChar, Cnan = minChar, Ctoobig = maxChar;
    if (specialChars) { Cneg = '⊗'; Cnan = '¿';  Ctoobig = '∞'; }
    int len = R.Store[slot].TotSz;
    if (fromPtr < 0) fromPtr = 0; else if (fromPtr >= len) return "";
    if (extent < 0) extent = len;
    if (fromPtr + extent > len) extent = len - fromPtr;
    string result = "";
    double[] theData = R.Store[slot].Data._Copy(fromPtr, extent);
    char[] choochoo = theData._ToCharArray(Cneg, Ctoobig, Cnan);
    if (trimend || trimstart)
    { int firstgoodguy = -1, lastgoodguy = -1;
      for (int i = 0; i < extent; i++)
      { if (choochoo[i] > 32)
        { if (firstgoodguy == -1) firstgoodguy = i;
          lastgoodguy = i;
        }
      }
      if (firstgoodguy == -1) result = ""; // whether either or both trims are true
      else
      { if (trimstart && !trimend) lastgoodguy = extent-1; // no end trim
        else if (trimend && !trimstart) firstgoodguy = 0; // no start trim
        result = new string (choochoo,  firstgoodguy,  lastgoodguy-firstgoodguy+1);
      }
    }
    else result = choochoo._ToString();
    return result;
  }


  /// <summary>
  /// <para>Assumes slot is long enough. Crasho, if not. Needless to say, str must not be empty.
  /// If slot >= 0, returns NULL but plops the data into the given storeroom.</para>
  /// <para>If enters with slot = -1, returns the data array but does NOT implant it into any storeroom.</para>
  /// Converts the array to chars. form.
  /// </summary>
  public static double[] StringToStoreroom(int slot, string str)
  { double[] data = new double[str.Length];
    char[] chs = str.ToCharArray();  int num;
    for (int i = 0; i < data.Length; i++)
    { num = Convert.ToInt32(chs[i]);
      data[i] = (double) num; 
    }
    if (slot >= 0) { R.Store[slot].Data = data;  R.Store[slot].IsChars = true;   return null; }
    else return data;
  }
  /// <summary>
  /// Converts an array of type double into a string of chars.
  /// 'LowestAllowed' - values BELOW this will be replaced by char LowReplacemt. Negative LowestAllowed is taken as zero.
  /// 'HighestAllowed' - values ABOVE this will be replaced by char HighReplacemt. If HighestAllowed > $FFFF, it is reset to that value.
  /// NaN converts to 0.0, before any testing with the above limits.
  /// </summary>
  public static string DubArrToChars(double[] InArr, int LowestAllowed, int HighestAllowed, char LowReplacemt, char HighReplacemt)
  { if (LowestAllowed < 0) LowestAllowed = 0;
    if (HighestAllowed > 65535) HighestAllowed = 65535;
    char lowestChar  = Convert.ToChar(LowestAllowed);
    char highestChar = Convert.ToChar(HighestAllowed);
    char[] chew = InArr._ToCharArray('\u0000', '\uffff', '\u0000'); // replacements for: Neg, Too Large, NaN.
    if (LowestAllowed == 0 && HighestAllowed == 65535) return chew._ToString();
    // End of story, if no low and high limits. If there are...
    char ch;
    int len = InArr.Length;
    for (int i = 0; i < len; i++)
    { ch = chew[i];
      if (ch < lowestChar) chew[i] = lowestChar;
      else if (ch > highestChar) chew[i] = highestChar;
    }
    return chew._ToString();
  }

 public static Quad SaveText(int Fn, int At, string filename, bool append)
 {  Quad result = new Quad(false);   
    string varnm = V.GetVarName(Fn, At);
    if (varnm==""){ result.S = "'save' as text: can't identify array to save"; return result;}
    int varuse = V.GetVarUse(Fn, At);
    if (varuse != 11){ result.S = "'save' as text requires assigned array as arg."; return result;}
    int slot = V.GetVarPtr(Fn, At);
    string intext = StoreroomToString(slot);
    try
    { StreamWriter w;
      if (append) w = File.AppendText(filename);
      else w = File.CreateText(filename);
      w.Write(intext);
      w.Flush();   w.Close();
      result.B = true;
    }
    catch
    { result.S = "could not save text to '"+filename+"'"; }
   return result;
 }

/// <summary>
/// <para>Save a variable's contents in one of three formats, depending on FormatAs:</para>
/// <para>FormatAs = "text" --> human-readable structured text file (see bottom of file 'jmaths2.cs' for formatting details) -
///  variable may be scalar or array; FormatAs = "data" --> a binary stream consisting only of the contents of the Data field
///  of the store item, each datum being coded by System.IO to 8-bytes of file data - variable must be an array;
///  "identified data" --> same, but prefixed with a formatted segment, made up as follows:</para>
/// <para>MAXREAL, {unicodes of "NAME:"},  {unicodes of name; OR empty, if a temporary variable},   MAXINT32,   { array's DimSz },
///         MININT32,   {InChars rating as '1' or '0' },  MININT32,  { seconds since start of 1AD },  MINREAL,  {contents of Data field}.</para>
/// <para>'Slot' and 'X' should be passed as is from F.SysFn.Args[i].X, for access to data to be saved.</para>
/// <para>'PathFileName' must be complete - no abbreviations; calling code should have sorted its intricacies out.</para>
/// <para>'WhatIfFileExists' - 'A' - append; 'O' = overwrite;  anything else --> ask in a dialog box.</para>
/// <para>'VarNameInFile' with option "text": Obligatory as nonempty string. Typically you might use the variable's name, but
///  any printable chars. are fine EXCEPT the colon ':'. Also, be aware that leading and trailing spaces will be removed. It has
///  to be unique to the variable, as several variables may go into the same file, and it is this label alone which is used
///  at load time to distinguish which data in the file is required.</para>
/// <para>'VarNameInFile' with option "identified data": May be anything or nothing (empty string - but not null). Intended for
///  the name of the variable, but a limerick by Edward Lear would do just fine. At load time it is accessed for information only;
///  its value has nothing to do with the decoding or deployment of the loaded data.</para>
/// <para>Note that 'VarNameInFile' is not accessed with option "data".</para>
/// <para>RETURNED: .B true, .S empty if no error, otherwise .B false, .S holds the error message.</para>
/// </summary>
 public static Boost SaveVariable(int Slot, double X, string PathFileName, char WhatIfFileExists, string FormatAs, params string[] VarNameInFile)
 {  Boost result = new Boost(false);
   // What operation is required?
    bool asText = false, asIdentifiedData = false;
    if (FormatAs == "text") asText = true;
    else if (FormatAs == "identified data") asIdentifiedData = true; // No need for a boolean for FormatAs = "data".
    else if (FormatAs != "data"){ result.S = "argument 'FormatAs' is unrecognized";  return result; }
    bool fileExists = File.Exists(PathFileName);
    bool Append = false;
    if (fileExists)
    { if (WhatIfFileExists == 'A') Append = true;
      else if (WhatIfFileExists != 'O')
      { int btn = JD.DecideBox("FILE ALREADY EXISTS", "A file named '" + PathFileName + "' already exists. What action do you want?",
            "OVERWRITE", "APPEND", "CANCEL");
        if (btn == 0 || btn == 3) return result; // CANCEL: return with .B false, but no error message.
        Append = (btn == 2);
      }
    }
   // SAVE AS FORMATTED TEXT:
    if (asText)
    { string varName = "";
      if (VarNameInFile.Length > 0) varName = VarNameInFile[0].Trim()._Purge('\u0000'); // Last bit for padded names extracted from jagged matrices.
      if (varName == "") { result.S = "name for variable in the file is empty";  return result; }
      if (varName.IndexOf(':') != -1) { result.S = "name for variable in the file must not contain the colon ':'";  return result; }
      // Prepare arguments for method M2.FormatVarForSaving(.):
      double scalarValue = 0.0;
      string arrayToString = "";
      if (Slot == -1) scalarValue = X;
      else
      { Boost foost = V.StoreroomDataFormatted(Slot, false, false, false,  "", "", "", "N"); // "N" forces numerals rather than chars..
                         // While M2.FormatVarForSaving happily saves in char. form, the loader later on will not recognize that form.
        if (!foost.B) throw new Exception("Strange error. Call the programmer and violently complain."); // *** Eventually remove test.
        arrayToString = foost.S;
          // Prefix the dimensions, as they are required by M2.Format...(.) below:
        string ss = R.Store[Slot].DimSz._ToString(",");
        ss = ss.Replace(",0", "");
        arrayToString = '[' + ss + ']' + arrayToString;
      }
      bool includeHeader = true; // the header is basically a time stamp, only appropriate if a new file is being created.
      if (fileExists && Append) includeHeader = false;
      System.Text.StringBuilder sb = M2.FormatVarForSaving(scalarValue, null, arrayToString, "", varName, "", "", includeHeader, false );
      try
      { StreamWriter w;
        if (Append) w = File.AppendText(PathFileName);
        else w = File.CreateText(PathFileName);
        w.WriteLine(sb.ToString());
        w.Flush();   w.Close();
        result.B = true;
      }
      catch
      { result.S = "could not save to '"+PathFileName+"'"; }
      return result;
    }
   // SAVE AS DATA, prefixed or otherwise.
    if (Slot == -1) { result.S = "scalars cannot be saved with option 'data' or 'identified data'";  return result; }
    StoreItem rite = R.Store[Slot];
    double[] prefix = null;
    if (asIdentifiedData)
    {
      if (Append) { result.S = "file cannot be saved in 'append' mode if a descriptive prefix was to be saved with the data";  return result; }
     // Work up a prefix:
      //   disk file will remain, causing grief when the user tries to load this file.
      double MAXREAL = double.MaxValue,  MINREAL = double.MinValue;
      double MAXINT32 = (double) int.MaxValue,   MININT32 = (double) int.MinValue;
      List<double> dumpToFile = new List<double>();
      dumpToFile.Add(MAXREAL); // The grand opening for the prefix.
      string tt = "NAME:";
      if (VarNameInFile.Length > 0) tt += VarNameInFile[0];
      dumpToFile.AddRange(StringToStoreroom(-1, tt));
      dumpToFile.Add(MAXINT32); // Signifies end of NAME section and start of DIMENSIONS section.
      dumpToFile.AddRange(rite.DimSz._ToDubArray()); // DimSz has length TVar.MaxNoDims, which sadly has changed
                        // over the years. Fear not, the method that parses the downloaded file - F.ParseBinaryCodedFileDump -
                        // allows for different such sizes.
      dumpToFile.Add(MININT32); // Signifies end of DIMENSIONS section and start of CHARS. RATING section.
      if (rite.IsChars) dumpToFile.Add(1.0);  else dumpToFile.Add(0.0);
      dumpToFile.Add(MININT32); // Signifies end of CHARS. RATING section and start of TIME STAMP section.
      dumpToFile.Add( Convert.ToDouble(JS.Tempus('S', false))); // time in SECONDS since start of 1AD.
      dumpToFile.Add(MINREAL); // Signifies end of TIME STAMP section, and so of the prefix.
      prefix = dumpToFile.ToArray();
    }
    FileStream fs;
    try
    { if (Append)
      { fs = File.Open(PathFileName, FileMode.Append); } // If no existing file, will create a new one.
      else
      { fs = new FileStream(PathFileName, FileMode.Create); } // Overwrites without warning.
      BinaryWriter bw = new BinaryWriter(fs);
      if (prefix != null) // by testing above, prefix must be null if 'Append' true.
      { for (int i = 0; i < prefix.Length; i++) bw.Write(prefix[i]); } // sadly no form of 'Write' takes a double array.
      double[] doodle = rite.Data;
      for (int i = 0; i < doodle.Length; i++) bw.Write(doodle[i]);
      bw.Flush();  bw.Close();  fs.Close();
      result.B = true;
    }
    catch { result.S = "could not write data to '" + PathFileName; }
    return result;
  }

/// <summary>
/// Sifts through text formatted as per instructions at the bottom of file 'jmaths2.cs', looking for the  variable
///  named in 'FileVarName'. If found, (a) if it is an array or string, the method creates a new array and returns {slot, 0.0; "array", "" }
///  for it; (b) if it is scalar, it returns {-1.0, value;  "scalar", "" }; (c) decoding failure returns {-999.0,-999.0; "error", errorMessage}.
/// </summary>
  public static Strub2 ExtractFormattedTextVariable(string TheText, string FileVarName)
  { Strub2 result;
    // Extract the data from the text:
    double[] Arr1D = null; double[,] dummy2 = null; double[,,] dummy3 = null;
    double xx = JM.Unlikely; // In M2.DecodeVariable(.), this forces all the data into Arr1D, whatever the no. dimensions of the variable.
        // This is necessary in order to be able to plonk Arr1D into R.Store[..].Data.
    Quad quobble = M2.DecodeVariable(FileVarName, TheText, ref xx, ref Arr1D, ref dummy2, ref dummy3);
    if (!quobble.B) { result = new Strub2(-999.0, -999.0, "error", quobble.S);  return result; }
    // Is it SCALAR?
    if (quobble.I == 0) { result = new Strub2(-1, xx, "scalar", "");  return result; }
    // ARRAY or STRING: we have to create a new array...
    int dimcnt=0;
    int[] dimsz = new int[TVar.MaxNoDims];
    double[] data = Arr1D;
    if (quobble.I == 10) // A string, so create a chars. array:
    { if (quobble.S == "") quobble.S = " ";
      data = StringToStoreroom(-1, quobble.S);
      dimsz[0] = data.Length;   dimcnt = 1;
    }
    else if (quobble.I == 1) // 1D array:
    { data = Arr1D;  dimsz[0] = data.Length;   dimcnt = 1; }
    else // a multidimensional array:
    { int[] dimensions = quobble.S._ToIntArray(","); // size = no. dimensions, unlike size of 'dimsz' defined above.
      if (dimensions == null) { result = new Strub2(-999.0, -999.0, "error", "dimensions not identified in text");  return result; }
      int totsz = 1;
      dimcnt = dimensions.Length;
      for (int i=0; i < dimcnt; i++) { dimsz[i] = dimensions[i];  totsz *= dimensions[i]; }
      if (totsz != data.Length) { result = new Strub2(-999.0, -999.0, "error", "dimensions conflict with amount of data in text");  return result; }
    }
   // SET UP THE STOREROOM:
    int slot = V.GenerateTempStoreRoom(dimsz);
    StoreItem splot = R.Store[slot];
    splot.IsChars = (quobble.I == 10);
    splot.Data = data;
    return new Strub2(slot, 0.0, "array", "");
  }
  /// <summary>
  /// <para>Case 1: InData does not have a recognizable header: returns 0, and InData is untouched (even if it is empty or null).</para>
  /// <para>Case 2: Header identified and correctly formatted: returns 1, and sets all 4 REF args. In particular, InData returns with the
  ///  header removed, i.e. is the data expected.</para>
  /// <para>Case 3: Header identified, but not correctly formatted: returns a negative number, and InData is untouched. Returned values:
  ///  Header 'identified' if file starts with MAXREAL, and is followed by unicodes for "NAME:", and somewhere after by MAXINT32.</para>
  /// <para>ARGS: VarName is the name of the original saved variable; Dimensions always has length 5; IsChars obvious; SaveTime is the time
  ///  in msecs. since start of 1AD, as determined by the function that saved the file. (NB: No checks on validity of the time here;
  ///  it could be negative or zero or what-you-will.)</para>
  /// <para>HEADER FORMAT:  MAXREAL, {unicodes of "NAME:"},  {unicodes of name},   MAXINT32,   { the 5 dim. sizes, dims. 0 to 4 },
  ///         MININT32,   {InChars rating as '1' or '0' },  MININT32,  { seconds since start of 1AD },  MINREAL,  {contents of Data field}.</para>
  /// <para>Note that there is no test whatever for validity of name unicodes; it may even be empty. If not, it will be returned as whatever the
  ///  doubles-to-unicodes method called below makes of it.</para>
  /// </summary>
  public static int ParseBinaryCodedFileDump(ref double[] InData, ref string VarName, ref int[] Dimensions, ref bool IsChars, ref double SaveTime)
  {// **** IF CHANGING / ADDING TO ERROR CODES, be sure to change refs. in the method immediately following, that prepares error messages.
    double MAXREAL = double.MaxValue,  MINREAL = double.MinValue;
    double MAXINT32 = (double) int.MaxValue,   MININT32 = (double) int.MinValue;
    int inLen = InData._Length();   if (inLen < 8) return 0; // 'No recognizable header'.
    if (InData[0] != MAXREAL) return 0;
    int ptrAfterName = InData._Find(MAXINT32);
    if (ptrAfterName < 6) return 0;
    string ss = InData._Copy(1, ptrAfterName - 1)._ToCharString();
    if (ss._Extent(0, 5) != "NAME:") return 0;
    // A header has now been identified, so no case after this will return 0.
    VarName = ss._Extent(5); // No more will be done with this REF argument. In particular, no tests of any sort done on it. Can be empty string.
    if (inLen < ptrAfterName + 12) return -10; // length would allow for one datum.
    // Develop the dimensions:
    int ptrAfterDims = InData._Find(MININT32);  if (ptrAfterDims < ptrAfterName) return -20; // either not found, or occurs too early.
    int maxdims = TVar.MaxNoDims; // This constant has varied in the past, so the code below allows for different sizes of it (see 'Math.Min' below).
    Dimensions = new int[maxdims];
    double x=0;
    int n, totalSize = 1;
    int maxo = Math.Min(maxdims, ptrAfterDims - ptrAfterName - 1);
    for (int i=0; i < maxo; i++)
    { x = InData[ptrAfterName + i + 1];
      x = Math.Round(x);
      if (x < 0.0 || x > MAXINT32) return -30; // impossible dimension.
      n = Convert.ToInt32(x);
      if (n == 0) break; // so we never get Dimensions[] with a zero between nonzero values.
      totalSize *= n;
      Dimensions[i] = n;
    }
    if (totalSize == 0) return -40;
    if (totalSize != inLen - ptrAfterDims - 5) return -50;
    // Develop IsChars:
    x = InData[ptrAfterDims + 1];
    if ( (x != 0.0 && x != 1.0) || InData[ptrAfterDims + 2] != MININT32)  return -60;
    IsChars = (x != 0.0);
    // Extract time, find data field:
    SaveTime = InData[ptrAfterDims + 3]; // NO checks on its validity as a time stamp.
    if (InData[ptrAfterDims + 4] != MINREAL)  return -70;
   // HEADER SUCCESSFULLY DECODED:
    InData = InData._Copy(ptrAfterDims + 5);
    return 1;
  }

  public static string ParseBinaryCodedFileDumpErrMsg(int Oopsie)
  { string ff = "faulty file prefix: ";
    switch (Oopsie)
    { case 1   : return ""; // No error, header recognized.
      case 0   : return "- no file prefix identified"; // Not nec. an error, but if this was called then the explanation was wanted.
        // *** THE DASH ABOVE as the 1st. char. is deliberate; case 0 must produce the only string which starts with a dash.
      case -10 : return "either no data after file prefix, or file prefix is too short";
      case -20 : return ff + "dimensioning segment is the wrong size";
      case -30 : return ff + "a dimension is out of the range 0 to MAXINT32";
      case -40 : return ff + "all dimensions are zero";
      case -50 : return ff + "data supplied would not exactly fit the dimensions given";
      case -60 : return ff + "the 'characters' rating for the array must be either 0 or 1";
      case -70 : return ff + "the delimiter indicating the start of data is missing";
      default: return "? (unidentified error code)";
    }
  }



/// <summary>
/// <para>Input is recognized in the following circumstances:</para>
/// <para>(1) IsChars FALSE, clrData has 1 value, within the Int32 range.</para>
/// <para>(2) IsChars FALSE, clrData has 3 or 4 values, 0 to 255: the top 3 are taken as: [0/1] = red, [1/2] = green, [2]3] = blue.</para>
/// <para>There are other legal string inputs - see the palaver for the "_ParseColour()" method.</para>
/// <para>ERROR: If unparsable argument, 'success' is FALSE and FailureColour is returned.</para>
/// <para>(4) IsChars TRUE: clrData is translated into a string, and that string is uncritically passed to the J_Extension unit method
/// "_ParseColour()", q.v. If a hex number, it must be preceded by "0x", followed by either 6 or 8 hex digits; otherwise it will be
/// taken as a colour name (case-insensitive), and .NET's list of colour names will be consulted. (OK, this is MONO; but I have
/// transcribed all the .NET colour names, and they constituted the database for this method, with the inclusion of 'grey' as an
/// alternative spelling to 'gray'.)</para>
/// </summary>
  public static Gdk.Color InterpretColourReference(bool IsChars, Gdk.Color FailureColour, out bool success, params double[] clrData)
  { success = true;   string toParse;
    if (IsChars)
    { string stroo = DubArrToChars(clrData, 32, 122, ' ', '?'); // characters below unicode 32 convert to SPACE, and above 'z' to '?'.
      toParse = stroo.ToLower();
      toParse = toParse.Replace("grey", "gray");
    }
    else // not a chars. array:
    { int[] iddly = clrData._ToIntArray();
      toParse = iddly._ToString(",");
    }
    return toParse._ParseColour(FailureColour, true, out success);
  }

/// <summary>
/// Overload.
/// </summary>
  public static Gdk.Color InterpretColourReference( PairIX Arg, Gdk.Color FailureColour, out bool success)
  { if (Arg.I == -1) return InterpretColourReference(false, FailureColour, out success, Arg.X);
    else 
    { StoreItem stem = R.Store[Arg.I];
//      return InterpretColourReference(stem.IsChars, FailureColour, out success, stem.Data);
      Gdk.Color coo = InterpretColourReference(stem.IsChars, FailureColour, out success, stem.Data);
      return coo;


    }
  }

// Right-trim an array of double, for some given input value to be trimmed off.
// NB! Returns a NULL array if InArr consists entirely of BadGuy.  
  public static double[] RightTrimDubArray(double[] InArr, double BadGuy)
  { int inlen = InArr.Length; if (inlen == 0) return null;
    int lastgoodguy = -1;
    for (int i=inlen-1; i<=0; i--) { if (InArr[i] != BadGuy){ lastgoodguy = i; break; } }
    if (lastgoodguy == inlen-1) return InArr;  else if (lastgoodguy == -1) return null;
    // All remaining cases have one or more BadGuy values at the end.
    double[] result = new double[lastgoodguy+1];
    Array.Copy(InArr, result, lastgoodguy+1);    
    return result;
  }
// Given an array of dimensions without zeroes (e.g. MxNxP structure --> [P,N,M]), and a 
// compatible set of indices (e.g. [p,n,m] in given example - note: inner dimension low), 
// return the .Data location.  DimSizes may be larger than DimCnt; if so, upper elements ignored.
// Very little error detection; e.g. none for size of arrays. 
// Error return: if  Indices[n] exceeded corresp. dimension, -(n+1) is returned. NB: If the user's input was in the order of 
// dimensions - MxNxP --> user's input [m,n,p] - this order was reversed in Indices (i.e. --> [p,m,n]) before the call to here; 
// so the message to the user should be that the [DimCnt + <returned value> + 1]._Ordinal() dimensional value is out of range.
  public static int LocateArrayValue(int[] DimSizes, int DimCnt, int[] Indices)
  { int D=0, N=0, Prod = 1, Total = 0;
    for (int indx = 0; indx < DimCnt; indx++)
    { D = DimSizes[indx];  N = Indices[indx];
      if (N >= D || N < 0) return -(indx+1);
      if (indx > 0) Prod *= DimSizes[indx-1];
      Total += N*Prod;
    }  
    return Total; // check of N in loop prevents this being returned as >= TotSz for array.
  } 
// The inverse of the above. Given an absolute address within some structure, return its coordinates. Returned - an array of
//  coordinates, [0] being that of the highest / lowest dimension in accordance with HighestDimFirst. Returns NULL if AbsAddr is out of range either way.
// Handles a list array OK.
  public static int[] CoordsOfAbsAddress(int[] DimSizes, int DimCnt, int AbsAddr, bool HighestDimFirst)
  { int[] result = new int[DimCnt],  fac = new int[DimCnt+1]; // fac[0] will never be used. fac[DimCnt] only used for the error test below.
    int product = 1;
    for (int i=1; i <= DimCnt; i++) { product *= DimSizes[i-1];   fac[i] = product; }
    if (AbsAddr < 0 || AbsAddr >= fac[DimCnt]) return null; // Oopsie! Address out of range.
    int addr = AbsAddr;
    for (int j=DimCnt-1; j > 0; j--) { result[j] = addr/fac[j];   addr = addr % fac[j]; }
    result[0] = addr;
    if (HighestDimFirst) Array.Reverse(result);
    return result;
  }
/// <summary>
/// <para>All args. will score 'S' if scalar, 'A' if an array; these characters will then make up the returned string.
/// For example, if args. 0, 1, 2 are (scalar, scalar, array), then the returned string will be "SSA".</para>
/// </summary>
  public static string ArgTypesCode(PairIX[] TheArgs, char Dummy) // overload
  { string result = "";
    for (int i=0; i < TheArgs.Length; i++)
    { if (TheArgs[i].I == -1) result += "S"; else result += "A";
    }
    return result;
  }
/// <summary>
/// <para>All args. from 'FirstArg' onwards will score '1' if scalar, '2' if an array; these digits will then make up the returned integer.
/// For example, if FirstArg is 1, and args. 1, 2 and 3 are (scalar, scalar, array), then the return will be '112'.</para>
/// <para>There should be no more than 9 arguments, to avoid integer overflow. For more args., use the overloaded method.</para>
/// </summary>
  public static int ArgTypesCode(PairIX[] TheArgs,  params int[] FirstArg)
  { int n = 0, starter = 0, result = 0;
    if (FirstArg.Length > 0) starter = FirstArg[0];
    for (int i=starter; i < TheArgs.Length; i++)
    { if (TheArgs[i].I == -1) n = 1; else n = 2;
      result *= 10;   result += n;
    }
    return result;
  }

// Used by several system functions, all of which handle an unlimited number of arguments for display. 
// FirstValArg allows for any initial system function argument(s) NOT to become part of the returned string (e.g. an index).
// The arguments must all be 'ref' arguments, so that the array REFArgLocn can be passed to this fn. (as 'Ref').
// Policy for scalars: this preparsing sequence -- x, "/", 65, "/", PI, "/", 3*4 -- would return:   1.23/A/3.14.../12
//  In this example, note: (1) assigned variables ('x' here) and system constants ('PI') and expression evaluations ('3*4')
//  all have their string version printed, while standalone literal numbers like "10" are replaced by the unicode character. 
//  (If zero or negative, raises an error).--> \u0000). 
// Policy for arrays: as dictated by .IsChars rating.
// IntroText is added on to the start of the string; if present, it would usually be a formatting instruction set. 
//  (Less memory transaction occurs if done here than if left to calling code to add it to the returned string.)
// ERRORS: The returned string starts with '\u0000', this being followed by the error message.
  public static string ArgsToDisplayString(int FirstValidArg, PairIX[] Args, Duo[] Ref, string IntroText)
  { string textout = IntroText;  int NoArgs = Args.Length;
    for (int i = FirstValidArg; i < NoArgs; i++)
    { int Fn = Ref[i].Y, At = Ref[i].X; // If Fn < 0, this is an expression evaluation.
      if (Args[i].I == -1)
      {//SCALAR:
        if (Fn < 0) textout += Args[i].X.ToString(); // expression evaluation, so add string version of its value
        else
        { int varuse = V.GetVarUse(Fn, At);
          if (varuse == 0)
          { string stroo = V.GetVarName(Fn, At);
            textout = "\u0000" + "variable '" + stroo + "' has not yet been assigned a value";
            return textout;
          }
          else if (varuse == 1)
          { string tt = V.GetVarName(Fn, At);
            if (tt._Extent(0, 2) == "__")// a literal, so interpret as a character:
            { double x = Math.Round(Args[i].X);
              if (x == 0.0)  // Negative x never gets here: "-10" is seen as an expression, so Fn above enters as -600, dealt with above.
              { textout = "\u0000" + "literal unicode value '" + Args[i].X + "' rounds to zero"; return textout; }
                else if (x > (double) 0xffff)
              { textout = "\u0000" + "literal unicode value '" + Args[i].X + "' rounds to a value > 0xFFFF"; return textout; }
              textout += DubArrToChars(new double[] {x}, 0, 0xffff, ' ', char.MaxValue);
            }
            else // a user var. (or PI, EE), so print value:
            { textout += Args[i].X.ToString(); }
          }
          else textout += Args[i].X.ToString(); // varuse is 2 or 3.
        }  
      }
      else // ARRAY:
      { int n = Args[i].I;
        if (R.Store[n].IsChars) // then this array is to be interpreted as a list of characters:
        { textout += DubArrToChars(R.Store[n].Data, 1, 0xffff, ' ', char.MaxValue); }
                // Values below 1 convert to a SPACE character, and chars. above the max allowed convert to that maximum.
        else textout += R.Store[n].Data._ToString(", ");
      }
    }
    return textout;
  }
/// <summary>Accumulate values in a range of arguments, be they scalar or array. No test for silly args. - just crasho.
/// 'Args' should consist of all of the function's args. 
/// </summary>
  public static double[] AccumulateValuesFromArgs(PairIX[] Args, params int[] FirstLastValidArg)
  { int NoArgs = Args.Length, startptr = 0, endptr = NoArgs-1;
    int n = FirstLastValidArg.Length;
    if (n >= 1) startptr = FirstLastValidArg[0];   if (n >= 2) endptr = FirstLastValidArg[1];
    List<double> doobiedoo = new List<double>();
    for (int i = startptr; i <= endptr; i++)
    { if (Args[i].I == -1) doobiedoo.Add(Args[i].X);
      else doobiedoo.AddRange(R.Store[Args[i].I].Data);
    }  
    return doobiedoo.ToArray();
  }

  public static void ResetStaticParams(string Whatto) // If string contains '2', 2D params. included; if '3', 3D params. included.
                                                      // If 'C', centre/maximize included; if 'A', the bloomin' lot are reset.
  { if (Whatto._CountChar("CA") > 0)
    { CentreGraphs = false;   MaximizeGraphs = false; }
    if (Whatto._CountChar("2A") > 0)
    { NextXCornerReal = 0; NextYCornerReal = 0; NextXTipReal = 0; NextYTipReal = 0;
      NextXSegments = 0; NextYSegments = 0;
    }
    if (Whatto._CountChar("3A") > 0)
    { NextXCornerReal = 0; NextYCornerReal = 0; NextZCornerReal = 0; NextXTipReal = 0; NextYTipReal = 0; NextZTipReal = 0;
      NextXSegments = 0; NextYSegments = 0; NextZSegments = 0;
    }  
  }


// Set up a new empty array (length 1, value NaN), and return its R.Store slot.
  public static int EmptyArray()
  { int slot = V.GenerateTempStoreRoom(1);  R.Store[slot].Data[0] = double.NaN;
    return slot;
  }

// Inserts a row or col. into the data strip of a matrix. NO CHECKING OF ARGS -- if row/col no. wrong, or Tier length wrong, --> bl. great crash.
// Only DimSz[0] and [1] used, so higher structures could use this for their lowest matrix.
  public static void InsertMxUnit(bool isRow, int TierNo, ref double[] Mx, ref double[] Tier, int[] DimSz) 
  { int NoRows = DimSz[1],  NoCols = DimSz[0];
    if (isRow) Array.Copy(Tier, 0, Mx, TierNo*NoCols, NoCols);
    else // column:
    { for (int i=0; i < NoRows; i++)
      { Mx[i*NoCols + TierNo] = Tier[i]; }
    }
    return;
  }
  // Returns a row or col. from the data strip of a matrix. NO CHECKING OF ARGS -- if row/col no. wrong, or Tier length wrong, --> bl. great crash.
  // Only DimSz[0] and [1] used, so higher structures could use this for their lowest matrix.
  public static double[] CopyMxUnit(bool isRow, int TierNo, ref double[] Mx, int[] DimSz) 
  { int NoRows = DimSz[1],  NoCols = DimSz[0];
    double[] result = null;
    if (isRow) 
    { result = new double[NoCols];
      Array.Copy(Mx, TierNo*NoCols, result, 0, NoCols);
    }  
    else // column:
    { result = new double[NoRows];
      for (int i=0; i < NoRows; i++)
      { result[i] = Mx[i * NoCols + TierNo]; }
    }
    return result;
  }

// ----- FAST FOURIER TRANSFORM STUFF ---------------

/// <summary>
/// <para>Specialized function intended specifically for the FFT (method following). For Forward = TRUE, it computes the N terms 
/// of the series {exp(-j*PI*i/N)} from n = 0 to n = N-1. (Note - NOT exp(-j*2*PI*i/N), as in the DFT.) N is required to be even and >= 2,
/// but not necessarily a power of 2 (though it always is, as used by the FFT method). (No test here.)
/// For Forward = FALSE, the series {exp(+j*PI*i/N)} is calculated.
/// The result of the calculation for n = i will be stored with the real value in RealsImags[i] and the imag. value in
/// RealsImags[i+N]; total length 2N. This format is most suited for the recursive use in the FFT method.</para>
/// <para>The name 'fiddle factor' is not my idea; it is in the books (e.g. Wikipedia, whence I got the FFT algorithm).</para>
/// </summary>
  public static double[] FFTFiddleFactor(int N, bool Forward)
  { double[] RealsImags = new double[2*N];
    double ix, cosx, sinx, x = Math.PI / N;  int N_2 = N/2;
    double Sign = -1.0; if (!Forward) Sign = 1.0;
    RealsImags[0] = 1.0;  RealsImags[N + N_2] = Sign;
    for (int i=1; i < N_2; i++)   
    { ix = (double) i;
      cosx = Math.Cos(ix*x);  sinx = Sign * Math.Sin(ix*x);
      RealsImags[i]   = cosx;   RealsImags[N+i]   = sinx;
      RealsImags[N-i] = -cosx;  RealsImags[2*N-i] = sinx;
    }
    return RealsImags;
  }

  public static double[] RecursiveFFT(ref double[] RealsImags, int Start, int NoJumps, int JumpSize, ref double[] Fiddles)
  { int inarrayLen = RealsImags.Length, noValsIn = inarrayLen/2;
    double[] result;
    if (NoJumps == 2)
    { result = new double[4];
      // First complex no.:
      result[0] = RealsImags[Start] + RealsImags[Start + JumpSize];
      result[2] = RealsImags[Start + noValsIn] + RealsImags[Start + JumpSize + noValsIn]; 
      // Second complex no.:
      result[1] = RealsImags[Start] - RealsImags[Start + JumpSize];
      result[3] = RealsImags[Start + noValsIn] - RealsImags[Start + JumpSize + noValsIn]; 
      return result; 
    }
   // NoJumps > 2:
    int startE = Start, startO = Start + JumpSize;
    JumpSize *= 2;  NoJumps /= 2;
    // The names 'E' and 'O' below are used to represent the 'Ek' and 'Ok' of the standard textbook descriptions of the FFT.
    double[] E = RecursiveFFT(ref RealsImags, startE, NoJumps, JumpSize, ref Fiddles);
    int ELen = E.Length, noReals = ELen / 2;
    double[] O = RecursiveFFT(ref RealsImags, startO, NoJumps, JumpSize, ref Fiddles);
    result = new double[2*ELen];
    double ER, EI, OR, OI, expR, expI, expOR, expOI;
    for (int i=0; i < noReals; i++)
    { ER = E[i];  EI = E[noReals + i];
      OR = O[i];  OI = O[noReals + i];
      expR = Fiddles[i * JumpSize];  expI = Fiddles[noValsIn + i * JumpSize];
      expOR = expR*OR - expI*OI;   expOI = expR*OI + expI*OR;
      result[i] = ER + expOR;            result[ELen + i] = EI + expOI;
      result[noReals + i] = ER - expOR;  result[ELen + noReals + i] = EI - expOI;
    }    
    return result;
  }
/// <summary>
/// <para>Calling code MUST ensure that the length of RealsImags is an exact power of 2, and at least 4 (representing two complex nos). 
/// No checks here. RealsImags has all the real values in its bottom half and all the imag. values in its top half.
/// If the relevant FiddleFactor is NULL, then it will be computed using method 'FFTFiddleFactor(.).</para>
/// <para>The RETURNED ARRAY has the same format (real values in lower half, imag. values above).</para>
/// </summary>
  public static double[] FFT(double[] RealsImags, bool Forward)
  { int Len = RealsImags.Length;
    if (Forward)
    { if (ForwardFiddleFactor == null || ForwardFiddleFactor.Length != Len) 
      { ForwardFiddleFactor = FFTFiddleFactor(Len/2, true); }
      return RecursiveFFT(ref RealsImags, 0, Len/2, 1, ref ForwardFiddleFactor);
    }
    else // Inverse transform:
    { if (InverseFiddleFactor == null || InverseFiddleFactor.Length != Len)  
      { InverseFiddleFactor = FFTFiddleFactor(Len/2, false); }
      double[] doo = RecursiveFFT(ref RealsImags, 0, Len/2, 1, ref InverseFiddleFactor);
      double x = 2.0 / (double) Len; // The coefficient for the inverse tranform
      for (int i=0; i < doo.Length; i++) doo[i] *= x;
      return doo;
    }

  }

/// <summary>
/// <para>Intended as a utility for fns. 'plot' and 'plot3d'.</para>
/// <para>Converts the data in the given storeroom into a form suitable for either the 'PtShape' or the 'LnShape' argument of the fn.
/// Input array structure is ignored.</para>
/// <para>'Longest' returns as whichever is greater: its own entrance value or the length of the returned array.</para>
/// <para>If the slot is -1 (scalar), returns a char[] size 1, value unicode(X); or value ' ', if faulty.</para>
/// </summary>
  public static char[] Plot_Shape(int InSlot, double X, ref int Longest)
  { char[] result;
    if (InSlot == -1) result = new char[] { X._ToChar(' ') }; // ' ' is the default in case of error in converting X to a char.
    else // an array:
    { string streudel = StoreroomToString(InSlot); // no trimming of values.
      result = streudel.ToCharArray();
    }
    if (result.Length > Longest) Longest = result.Length;
    return result;
  }
/// <summary>
/// <para>Intended as a utility for fns. 'plot' and 'plot3d'.</para>
/// <para>Converts the data in the given storeroom into a form suitable for either the 'PtWidth' or the 'LnWidth' argument of the fn.
/// Input array structure is ignored. An input scalar is returned as an array of length 1, value ScalarValue. (Scalar value is otherwise
///  ignored.)</para>
/// <para>'Longest' returns as whichever is greater: its own entrance value or the length of the returned array.</para>
/// <para>If the slot is -1 (scalar), returns NULL and leaves Longest unchanged.</para>
/// </summary>
  public static double[] Plot_Width(int InSlot, double ScalarValue, ref int Longest)
  { if (InSlot <= -1) return new double[] {ScalarValue};
    double[] indata = R.Store[InSlot].Data; // no trimming of values.
    if (indata.Length > Longest) Longest = indata.Length;
    return indata;
  }

/// <summary>
/// <para>Intended as a utility for plotting fns. Returns a Gdk.Color ARRAY (not a Gdk.Colour) of length at least one.</para>
/// <para>If InSlot is -1, ScalarValue will be consulted; otherwise ScalarValue is ignored and InSlot is assumed to represent a valid array.</para>
/// <para>ALLOWED INPUT FORMATS: 
/// (1) A scalar. Taken as an argument to system function 'palette(.)'. The return will be a single colour.
/// (2) A NONchars. list array: taken as arguments for successive 'palette(.)' calls; its length will be the no. rows in the returned array.
/// (3) A CHARS list array: It will be divided up into segments if there are instances of delimiter '|'. Each segment (or the whole, if no
///    delimiters) must be EITHER a valid colour name ("blue") OR an 8-char. hex value, "0xRRGGBB", where each of R, G and B is a single hex digit.
///    The segments do not have to be all of the same of these two types (but usually would be). With colour names, spaces are everywhere ignored. 
/// (4) A Nonchars. MATRIX: Must have row length 3. Each row is taken as the array { R value, G value, B value }, values being 0 to 255 inclusive.
///    (Note that a list array of 3 such values would be taken as arguments to three successive 'palette(.)' calls. To send a single colour
///    to this function as an array of 3 R, G, B values, first convert it to a row vector (e.g. with function 'rowvec(.)').
/// (5) A CHARS. MATRIX: will be regarded as a jagged matrix of either single valid colour names or single "0xRRGGBB" strings. (Different rows
///    may be either of these two types.)</para>
/// <para>'Longest' is a convenience factor for the specific calling code for which this method was devised. It returns as whichever is
///   greater: its own entrance value or the length of the returned array.</para>
/// </summary>
  public static Gdk.Color[] Plot_Colour(int InSlot, double ScalarValue, ref int Longest, Gdk.Color FailureColour)
  {// SCALAR INPUT:
    bool dummy;
    if (InSlot == -1)
    { int nn = Convert.ToInt32(Math.Abs(ScalarValue));
      if (Longest < 1) Longest = 1;
      return new Gdk.Color[] { JS.NewColor(Palette[nn % Palette.Length], out dummy) };
    }
   // ARRAY INPUT:
    StoreItem sightem = R.Store[InSlot];
    double[] indata = sightem.Data;
    bool isChars = sightem.IsChars;
    int noRows = sightem.DimSz[1];
    int rowLen = sightem.DimSz[0]; // for a list array, will also be its whole length.
    int n;
    Gdk.Color clr;  Gdk.Color[] result = null;
    // NON-CHARS. ARRAY:
    if (!isChars) 
    {
      if (noRows == 0) // a list array
      { result = new Gdk.Color[rowLen];
        for (int i=0; i < rowLen; i++)
        { n = Convert.ToInt32(Math.Abs(indata[i]));
          result[i] = JS.NewColor(Palette[n % Palette.Length], out dummy); 
        }
      }
      else // is a matrix (or higher, but we don't check for higher).
      { if (rowLen != 3)
        { if (Longest < 1) Longest = 1;
          return new Gdk.Color[] { FailureColour };
        }
        result = new Gdk.Color[noRows];
        for (int i=0; i < noRows; i++)
        { clr = FailureColour;    n = 3*i;
          if (indata[n] >= 0 && indata[n] < 255.5 && indata[n+1] >= 0 && indata[n+1] < 255.5 && indata[n+2] >= 0 && indata[n+2] < 255.5)
          { clr = JS.NewColor( Convert.ToInt32(indata[n]),  Convert.ToInt32(indata[n+1]),  Convert.ToInt32(indata[n+2]) ); }
          result[i] = clr;
        }  
      }
    } 
    // CHARS. ARRAY:
    else        
    { string clrStr = StoreroomToString(InSlot, true);
      n = clrStr.IndexOf('|');
      if (noRows == 0  &&  n != -1) // a list array with delimiters:
      { string[] colourStrings = clrStr.Split(new char[] {'|'});
        n = colourStrings.Length;
        result = new Gdk.Color[n];
        for (int i=0; i < n; i++) { result[i] = colourStrings[i].Trim()._ParseColour(FailureColour, true, out dummy); };
      }
      else // For remaining list arrays (no delimiters), we treat them as if they were one-row jagged matrices.
      { int noStrips = noRows;  if (noStrips == 0) noStrips = 1; 
        result = new Gdk.Color[noStrips];
        for (int i=0; i < noStrips; i++)
        { string tt = clrStr._Extent(i*rowLen, rowLen)._Purge();
          result[i] = tt._ParseColour(FailureColour, true, out dummy);
        }
      }
    }
    if (Longest < result.Length) Longest = result.Length; 
    return result;
  }

/// <summary>
/// <para>Intended as a utility for fns. 'plotmesh' and 'plotmesh3d'.</para>
/// <para>Converts the data in the given storeroom into a form suitable for either the 'PtShape' or the 'LnShape' argument of the fn.</para>
/// <para>Input array structure is significant; each row of an input matrix will become a row of the output, after stripping off terminal
///  values 0 (note: not spaces, = 32). Other structures: all data will fit into the single row of the 1-row output matrix.</para>
/// <para>'Longest' returns as whichever is greater: its own entrance value or the length of the returned array.</para>
/// <para>If the slot is -1 (scalar), returns NULL and leaves Longest unchanged.</para>
/// </summary>
  public static char[][] Mesh_Type(int InSlot, ref int Longest)
  { if (InSlot <= -1) return null;
    StoreItem sitem = R.Store[InSlot];
    double[] data = sitem.Data;
    int[] dims = sitem.DimSz;
    int dataLen = data.Length, nodims = sitem.DimCnt;
    int extent = dataLen, loops = 1; // defaults as for all data becoming row 1 of a 1-row matrix
    if (nodims == 2) { extent = dims[0];  loops = dims[1]; }
    char[][] result = new char[loops][];
    for (int i=0; i < loops; i++)
    { string streudel = StoreroomToString(InSlot, i*extent, extent); // no trimming of values; the user might deliberately want terminal spaces.
      streudel = streudel._CullEndTillNot('\u0000', streudel.Length-1); // in worst case - streudel is all zeros - returns streudel as { char.0 }.
      if (streudel.Length > Longest) Longest = streudel.Length;
      result[i] = streudel.ToCharArray();
    }
    return result;
  }

/// <summary>
/// <para>'LHSCoeffs' - a square matrix represented as a 1D array, laid down row by row; at least 2x2, holding LHS coefficients.</para>
/// <para>'RHS' - an array, length = sqrt of length of LHSCoeffs. Must not be all zeroes.</para>
/// <para>NB!! Both array arguments will be extensively altered, so should be COPIES of StoreItem data segments!
/// <para>'Negligible' - essential that this be > 0, as it is used at the end to check consistency of solutions,
/// and so to detect unsolveable ("indeterminate") sets; valid solutions would be excluded. If this enters as 0 (or is negative),
/// the default 1e-10 is invoked.</para>
/// <para>OUTPUT IF SOLVED: Outcome set to .B TRUE, .I = 0, .S = ""; array of solutions is returned.</para>
/// <para>OUTPUT IF HOMOGENEOUS - i.e. all RHS values are zero: Outcome set to .B FALSE, .I = 1, .S = message; NULL returned.</para>
/// <para>OUTPUT IF INDETERMINATE because one row (at least) is a linear combination of other(s): Outcome set to .B FALSE, .I = 2, 
/// .S = message; NULL returned. (This includes the situation where all of some row or all of some column is zero.)</para>
/// <para>OUTPUT IF SILLY ARGUMENTS: Outcome set to .B FALSE, .I = -1 or -2, .S = message; NULL returned.</para>
/// </summary>
  public static double[] SolveSimultEqns(double[]LHS, double[] RHS, out Quad Outcome, double Negligible)
  { Outcome = new Quad(false);  double multr, xx, maxval;  int winner = -1;
    int NoEqns = RHS.Length; if (NoEqns < 2){ Outcome.S = "less than two equations"; Outcome.I = -1; return null; }
    if (LHS.Length != NoEqns * NoEqns)
    { Outcome.S= "the two input arrays are incompatible in size and dimensions"; Outcome.I = -2; return null; }
    double[] result = new double [NoEqns];
    if (Negligible <= 0.0) Negligible = 1e-10; // default. Zero can't be allowed - see tests for indeterminacy below.
    // Toss out homogeneous equations (RHS = all 0's):
    bool TisOK = false;
    for (int w=0; w<NoEqns; w++)
    { if (Math.Abs(RHS[w]) > Negligible){ TisOK = true; break;} }
    if (!TisOK) { Outcome.S = "eqns. unsolvable because homogeneous (RHS is all zeroes)"; Outcome.I = 1; return null;}
    for (int cl = 0; cl < NoEqns-1; cl++) // omit the last column
    {// SWAP ROWS, if nec., to get the leading diagonal element as large as possible:
      // (a) For each column, find the largest value in that column at or below the main diagonal:
      //      (We leave values above the main diagonal out of consideration, as their rows were optimized by earlier passes of this loop.)
      TisOK = false;
      maxval = Math.Abs(LHS[cl * NoEqns + cl]);   winner = cl;
      for (int rw = cl+1; rw < NoEqns; rw++) 
      { xx = Math.Abs(LHS[NoEqns * rw + cl]);  if (xx > maxval){ maxval = xx; winner = rw; } }
      // (b) If the largest value is not already on the main diagonal, get it there.
      if (winner != cl)
      { for (int coeff = 0; coeff < NoEqns; coeff++) // swap coeff. by coeff. along the length of the two eqns. being swapped
        { xx = LHS[NoEqns * cl + coeff];
          LHS[NoEqns * cl + coeff] = LHS[NoEqns * winner + coeff];
          LHS[NoEqns * winner + coeff] = xx;
        }
      // (c) finish the swapping by swapping the RHS elements also:
        xx = RHS[cl];  RHS[cl] = RHS[winner];  RHS[winner] = xx;
      }
     // Massage the equations below row 'cl' such that they contain only zeroes in column 'cl':
      for (int rw = cl+1; rw < NoEqns; rw++)
      { if (Math.Abs(LHS[NoEqns * rw + cl]) > Negligible) // if less than Negligible, ignore it. (No need to correct to 0, as it will never be accessed.)
        { multr = LHS[NoEqns * cl + cl] / LHS[NoEqns * rw + cl];// the multiplier for the row, ready for subtraction from row 'cl':
          for (int term = cl+1; term < NoEqns; term++) // no need to zero the term at cl, as it will never be accessed again.
          { xx = LHS[NoEqns * cl + term] - multr * LHS[NoEqns * rw + term];
            if (Math.Abs(xx) <= Negligible) xx = 0.0; 
            LHS[NoEqns * rw + term] = xx;
          }
          LHS[NoEqns * rw + cl] = 0; // Not really needed, but here to help if trouble shooting.
          // Adjust the RHS element similarly:
          xx = RHS[cl] - multr * RHS[rw];
          if (Math.Abs(xx) <= Negligible) xx = 0.0;
          RHS[rw] = xx;
        }
      }
    }
    // If we have done our homework right, the bottom row should now solve
    //  directly. So starting from this, we divide and substitute:
    for (int rw=NoEqns-1; rw >= 0; rw--) // work through all the rows, from the bottom up.
    { xx = LHS[NoEqns * rw + rw]; // all to left of the main diagonal are (notionally) zeroes.
      // Test for homogeneous set of equations. If some equation was a linear combination of others, then somewhere
      //   there is going to be a row of all zeroes, after all this subtraction.
      if (Math.Abs(xx) <= Negligible)
      { Outcome.S = "eqns. unsolvable because indeterminate (at least one equation is a linear combination of other(s))"; Outcome.I = 2; return null;}
      if (Math.Abs(RHS[rw]) <= Negligible) result[rw] = 0;
      else  result[rw] = RHS[rw] / xx;
      // Now substitute solved values into all the above rows, so that at each row's turn, only the main diagonal term is unknown. 
      // (We save time by leaving these terms in situ, and only subtract their substituted values from RHS[].)
      if (rw > 0) // If it is the top row, then there is no more substituting
      // to do above it.
      { for (int roe=rw-1; roe >= 0; roe--)
        { RHS[roe] -= LHS[NoEqns * roe + rw] * result[rw]; // 'rw' is the column no. here.
    } } }
    Outcome.B = true;  return result;
  }

  /// <summary>
  /// If some method called e.g. from a system function has inside it "throw new Exception("yakkety yak"), then a try..catch in the sys. fn.
  ///  will return a longwinded message with "System Exception: " tacked on the front of your message and a load of tracing info at its end.
  ///  To overcome this, the message should start and end with '|', and this function should be called. If both are present, it will return
  ///  only the stuff between the two '|'s. Otherwise you get the message back as is.
  /// </summary>
  public static string CleanThrowOutput(string errString)
  {
    int p1 = -1, p2 = -1;
    p1 = errString._IndexOf('|', 0);   if (p1 >= 0) p2 = errString._IndexOf('|', p1+1);
    if (p2 > 0) return errString._Between(p1, p2);  else return errString;
  }

  /// <summary>
  /// Used currently only by system functions "getsegmt" and "setsegmt", to locate the delimiter before and after a given segment, and other details.
  /// 'Args' should start with: (array InArray, array / scalar Delimiter, scalar WhichSegment). Extra elements allowed but ignored.
  /// RETURNED: +1, if segment located; and all REF arguments are appropriately set. If segment index not found because too big, returns 0.
  ///   If segment index was negative, returns -1. If InArray was provided as a scalar, returns -2. If InArray is the empty array returns -3.
  /// WhichEnd: -1 for segment 0, where leftDelimPtr returns as -1; 0 for intermediate segments; +1 for the last segment, for which
  ///   rightDelimPtr = length of InArray.
  /// NB! REF ARG. 'INDATA' IS A POINTER, assigned within the method. Its object is not altered here.
  /// </summary>
  public static int LocateSegment(PairIX[] Args, ref double[] indata, ref double Delimiter, ref int leftDelimPtr, ref int rightDelimPtr,
                       ref int WhichEnd, ref bool Is_Chars)
  { indata = null;
    int inslot = Args[0].I, delimslot = Args[1].I;
    if (inslot == -1) return -2;     
    StoreItem inItem = R.Store[inslot];
    indata = inItem.Data;
    int indataLen = indata.Length;
    if (indataLen == 1 &&  double.IsNaN(indata[0]) ) return -3;
    if (delimslot == -1) Delimiter = Args[1].X;
    else Delimiter = R.Store[delimslot].Data[0];
    int[] delimousine = indata._FindAll(Delimiter);
    int noDelims = delimousine.Length;
    int whichSubArray = Convert.ToInt32(Args[2].X);
    if (whichSubArray < 0) return -1;
    if (whichSubArray > noDelims) return 0;
    leftDelimPtr = 0; rightDelimPtr = 0; // dummy assignments to shut the compiler up
    WhichEnd = 0;
    if (whichSubArray == 0) { leftDelimPtr = WhichEnd = -1; }
    else leftDelimPtr = delimousine[whichSubArray-1];
    if (whichSubArray == noDelims) { rightDelimPtr = indataLen; WhichEnd = 1; }
    else rightDelimPtr = delimousine[whichSubArray];
    Is_Chars = inItem.IsChars;
    return 1;
  }  

/// <summary>
/// Return, if no error: the array [no. carriages,  length of 1st. carriage, ... length of last carriage ]; i.e. the train's header;
///   and ErrorNo = 0. If the train is NULL (which is valid), the return is therefore just the array [ 0 ].
/// Return, if error: NULL array, and 2nd. arg. is nonzero. (The length of Puffer is always checked for consistency with its header.)
/// Calling code must ensure that Puffer has length at least 1. If it is not a valid 'train', error is raised and return is NULL.
/// Rounding errors are allowed for, in that header values are rounded before used.
/// </summary>
  public static double[] TrainHeader(double[] Puffer, out int ErrorNo)
  { ErrorNo = 0; // no error
    int puffLen = Puffer.Length;
    int noCarriages = Convert.ToInt32(Puffer[0]); // Calling code ensures that arg. puffer is never null or empty.
    if (noCarriages < 0) { ErrorNo = 10;  return null; }
    if (puffLen < 2 * noCarriages + 1)
    { ErrorNo = 20;  return null; } // Test ensures that it is safe to scan the rest of the header.
    double[] result = new double[noCarriages + 1];
    result[0] = (double) noCarriages;
    int dataLen = 0;
    for (int i = 1; i <= noCarriages; i++)
    { int n = Convert.ToInt32(Puffer[i]);
      if (n < 1) { ErrorNo = 30; return null; }
      result[i] = (double) n; 
      dataLen += n;
    }
    if (puffLen != noCarriages + 1 + dataLen) { ErrorNo = 40;  return null; }
    return result;
  }

/// <summary>
/// EITHER InDataSlot is an array slot holding a list array which is delimited by the 2nd. arg., OR
///  InDataSlot is a (jagged) matrix and the 2nd. arg. is the padder for short rows.
/// RETURNED: (a) If no error, the train built from the input array. (b) If ERROR: NULL, with no error message.
///  (Maybe some day we will put in error messages?)
/// WARNING: empty entries (= a row of all Padders in a jagged matrix; or two contiguous Delimiters in a delimited list array)
///   will be ignored, as carriages of zero length are not allowed.
/// </summary>
  public static double[] BuildTrain(int InDataSlot, double PadderOrDelimiter)
  { if (InDataSlot == -1) return null;    
    StoreItem sitem = R.Store[InDataSlot];
    double[] indata = sitem.Data;  
    int indataLen = indata.Length,  n = sitem.DimCnt;
    bool isJagged = (n == 2);
    if (!isJagged && n != 1) return null;
    int noCarriages, carriageLen;
    List<double> outstuff = new List<double>(2 * indataLen);
    outstuff.Add(0.0); // outstuff[0] will eventually be the no. of carriages.
    if (isJagged)
    { int noRows = sitem.DimSz[1], rowLen = sitem.DimSz[0];
      noCarriages = 0;
      for (int i = 0; i < noRows; i++)
      { int offset = i*rowLen;
        carriageLen = rowLen; // will apply if no padder is found in the loop below
        for (int j = 0; j < rowLen; j++)
        { double x = indata[offset + j];
          if (x == PadderOrDelimiter) { carriageLen = j; break; }
          outstuff.Add(x);
        }
        if (carriageLen > 0) // if it is 0, the row is ignored.
        { noCarriages++;
          outstuff.Insert(noCarriages, (double) carriageLen); // outstuff[0] will later hold total no carriages.
        }
      }
    }
    else // input is a delimited array
    {
      noCarriages = 0;
      carriageLen = 0;
      double xx;
      for (int i=0; i <= indataLen; i++)
      { if (i < indataLen) xx = indata[i]; else xx = PadderOrDelimiter;
        if (xx == PadderOrDelimiter)
        { if (carriageLen > 0) // otherwise do nothing.
          { noCarriages++;
            outstuff.Insert(noCarriages, (double) carriageLen);
            carriageLen = 0;
          }
        }
        else // not a delimiter:
        { carriageLen++;  outstuff.Add(xx);
        }
      }
    }
    outstuff[0] = (double) noCarriages; // If jagged matrix consisted entirely of padders, outstuff will now be the null train.
    return outstuff.ToArray();
  }

  /// <summary>
  /// Taking the double[] as a matrix laid down sequentially, it transposes the matrix into another such double[].
  /// If the two integer args. do not multiply to the length of Mx, NULL is returned; otherwise the transposed version of Mx.
  ///   (Mx itself is unaltered).
  /// </summary>
  public static double[] Transpose(ref double[] Mx, int OrigNoRows)
  {
    int MxLen = Mx.Length; if (MxLen % OrigNoRows != 0) return null;
    int OrigNoCols = MxLen / OrigNoRows;
    double[] outMx = new double[MxLen];
    for (int i=0; i < MxLen; i++)
    { outMx[OrigNoRows * (i % OrigNoCols) + i / OrigNoCols] = Mx[i];  }
    return outMx;
  }


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

