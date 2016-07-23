using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using JLib;


namespace JLib
{
 // A library of utilities for widescale use in programs written by me. Spread over
 //   several files, each containing a single "sublibrary".

// General points:
// 1. No check is ever made for certain error-throwing fn. arguments which would never be
//   perpetrated by the intended user of this namespace (which is me). In particular:
//    (1) No test for a null string or null array; one deserves to have the system crash
//          if one makes this mistake.
//    (2) No test for integer arguments being very close to max. / min. possible values
//         (so that adding a very small no. --> overflow / underflow).
//
// 2. Where an array or other method has been developed for math. arrays but could be
//   used for a string or char array, non-mathematical overloads for such may occur here.


public class JM
{
// PRIVATE FIELDS
  private static double defTiny = 1E-12;
  private static double[] FactLogTable = null; // created as needed by function logstirl1(n). Should otherwise never be altered.

// PUBLIC FIELDS
  public static double Tiny;
  public static double DefaultTiny { get{return defTiny;} } // reference the default.
  public static double Unlikely { get {return 1.934872E101;} } // keep it positive and above 1 E +100, if ever altering.

  // PRIVATE DUMMY CONSTRUCTOR, to prevent some galah from creating an instance of JS.
  private JM(){}

  // STATIC CONSTRUCTOR which sets fields for this class.
  static JM()
  { Tiny = defTiny;
  }


//=======================================================================
//1. SIMPLE BASIC FUNCTIONS
//-----------------------------------------------------------------------
// Sign of number. Only useful where (0 +/- Negligible) is to be regarded as zero.
//  Otherwise just use the System method Math.Sign(num). If Negligible is neg. (e.g. -1),
//  defTiny will be used. (If 0, this fn. would be equiv. to 'Math.Sign(num)')
  public static int Signof(double num, double Negligible)
  { if (Negligible < 0.0) Negligible = defTiny;
    if (Math.Abs(num) <= Negligible) num = 0.0;
    return Math.Sign(num);
  }
// Returns TRUE for any number (0 +/- Negligible). If Negligible is neg. (e.g. -1),
//  defTiny will be used. (If 0, this fn. would be trivialized to 'if (num == 0)...)
  public static bool Is0(double num, double Negligible)
  { if (Negligible < 0.0) Negligible = defTiny;
    if (Math.Abs(num) <= Negligible) return true; else return false;}
// Returns TRUE if the nos. differ by (0 +/- Negligible) or less. If Negligible is neg.
// (e.g. -1), defTiny will be used. (If Negligible = 0, this fn. would be trivial.)
// NB: Note that the uncertainty in the IEEE double representation may overrule this when
// num is not near zero; e.g. 1 + (1e-16) is seen as 0, 100 + (1e-15) is seen as 0.
  public static bool Close(double num1, double num2, double Negligible)
  { if (Negligible < 0.0) Negligible = defTiny;
    if (Math.Abs(num1 - num2) <= Negligible) return true; else return false;}
// Go through DubArr and convert everything with abs. value >= Negligible to 0.
  public static void Defluff(ref double[] DubArr, double Negligible)
  { for (int i = 0; i < DubArr.Length; i++)
    { if (Math.Abs(DubArr[i]) <= Negligible) DubArr[i] = 0.0; }
  }
/// <summary>
/// Conform the radian angle to the range +/- PI.
/// </summary>
  public static double AngleToRange(double Angle)
  { double pie = Math.PI;
    while (Angle > pie)  { Angle -= 2.0 * pie; }
    while (Angle < -pie) { Angle += 2.0 * pie; }
    return Angle;
  }

// FmtStr: string, with 'G' or 'F' first and then a number (1 to 15).
// "Gi" --> >= i total digits, in whichever is shortest: decimal or scientific form.
// "Fi" --> strict decimal form, with exactly i digits to right of dec.pt. (trail
//   0's are forced.) Default for empty FmtStr is "G5".
// I hope I one day find a better way to do this than using 'switch'...

/// <summary>
/// Overload 1. The radian angles in 'Angles' are conformed to the range +/- PI.
/// </summary>
  public static void AnglesToRange(ref double[] Angles, params int[] StartExtent)
  { if (Angles._NorE()) return;
    double pie = Math.PI;
    int start = 0, angLen = Angles.Length, extent = angLen;
    if (StartExtent.Length > 0) start  = StartExtent[0];
    if (StartExtent.Length > 1) extent = StartExtent[1];
    if (start < 0) start = 0;  if (start+extent > angLen) extent = angLen - start;
    for (int i=start; i < extent; i++)
    { double ang = Angles[i];
      while (ang > pie)  { ang -= 2.0 * pie; }
      while (ang < -pie) { ang += 2.0 * pie; }
      Angles[i] = ang;
    }
  }
/// <summary>
/// Overload 2. The radian angles in 'Angles' are conformed to the range +/- PI. Special case: if Values is nonnull and of the same length 
/// as Angles, then wherever Values[i] is 0.0, Angles[i] will be set to 0.0 also.
/// <para>RETURNED: FALSE if Values was null or had improper length (and 'Angles' is never accessed); TRUE otherwise (including where
/// Angles is null or empty).</para>
/// </summary>
  public static bool AnglesToRange(ref double[] Angles, double[] Values, params int[] StartExtent)
  { if (Angles._NorE()) return true;
    if (Values == null || Values.Length != Angles.Length) return false;
    double pie = Math.PI;
    int start = 0, angLen = Angles.Length, extent = angLen;
    if (StartExtent.Length > 0) start  = StartExtent[0];
    if (StartExtent.Length > 1) extent = StartExtent[1];
    if (start < 0) start = 0;  if (start+extent > angLen) extent = angLen - start;
    for (int i=start; i < extent; i++)
    { double ang = Angles[i];
      if (Values[i] == 0.0) ang = 0.0;
      else
      { while (ang > pie)  { ang -= 2.0 * pie; }
        while (ang < -pie) { ang += 2.0 * pie; }
      }
      Angles[i] = ang;
    }
    return true;
  }
/// <summary>
/// <para>For each argument element i there is a pivot (Xpivot[i], Ypivot[i]) and a ray attached to it. The ray starts by passing through 
/// the point (Xfrom[i], Yfrom[i]) and rotates till it passes through (Xto[i], Yto[i]). The ith. element of this method's return is 
/// the angle through which this ith. ray rotates. This returned angle lies between +/- PI.</para>
/// <para>If the 'from' point and the pivot coincide, the ray when at the 'from' point is taken as being at zero angle; similarly
/// for the case where the 'to' point and the pivot coincide.</para>
/// </summary>
  public static double[] RotationAngles(double[] Xfrom, double[] Yfrom,  double[] Xto,  double[] Yto, double[] Xpivot, double[] Ypivot)
  { int len = Xfrom.Length;  if (len == 0) return null;
    if (Yfrom.Length != len || Xto.Length != len || Yto.Length != len || Xpivot.Length != len || Ypivot.Length != len) return null;
    double angleFrom,  angleTo;
    double[] result = new double[len];
    for (int i=0; i < len; i++)
    { angleFrom = Math.Atan2(Yfrom[i] - Ypivot[i],   Xfrom[i] - Xpivot[i]);
      angleTo   = Math.Atan2(Yto[i]   - Ypivot[i],   Xto[i]   - Xpivot[i]);
      result[i] = angleTo - angleFrom;
    }
    AnglesToRange(ref result);
    return result;
  }
/// <summary>
/// <para>For each argument element i there is a ray hinged at the origin. The ray starts by passing through 
/// the point (Xfrom[i], Yfrom[i]) and rotates till it passes through (Xto[i], Yto[i]). The ith. element of this method's return is 
/// the angle through which this ith. ray rotates. This returned angle lies between +/- PI.</para>
/// <para>If the 'from' point is also the origin, the ray when at the 'from' point is taken as being at zero angle; similarly
/// for the case where the 'to' point is at the origin.</para>
/// </summary>
  public static double[] RotationAngles(double[] Xfrom, double[] Yfrom,  double[] Xto,  double[] Yto)
  { int len = Xfrom.Length;
    if (len == 0 || Yfrom.Length != len || Xto.Length != len || Yto.Length != len) return null;
    double[] result = new double[len];
    for (int i=0; i < len; i++)
    { result[i] = Math.Atan2(Yto[i], Xto[i]) - Math.Atan2(Yfrom[i], Xfrom[i]); }
    AnglesToRange(ref result);
    return result;
  }
/// <summary>
/// <para>For each argument element i there is a pivot (Xpivot[i], Ypivot[i]) and a ray attached to it. The ray starts by being 
/// horizontal and rotates in the positive direction till it passes through (XX[i], YY[i]). The ith. element of this method's return is 
/// the angle through which this ith. ray rotates. This returned angle lies between +/- PI.</para>
/// <para>The argument 'Dummy' is there to resolve between two overloads; its value is never accessed.</para>
/// <para>If the point (XX[i], YY[i]) and the pivot coincide, the ray when at that point is taken as being at zero angle.</para>
/// </summary>
  public static double[] RotationAngles(double[] XX, double[] YY,  double[] Xpivot, double[] Ypivot, double Dummy)
  { int len = XX.Length;
    if (len == 0 || YY.Length != len || Xpivot.Length != len || Ypivot.Length != len) return null;
    double[] result = new double[len];
    for (int i=0; i < len; i++) result[i] = Math.Atan2(YY[i] - Ypivot[i],   XX[i] - Xpivot[i]);
    return result;
  }
/// <summary>
/// <para>Returns an array consisting of the angles which the various position vectors from the origin to (XX[i], YY[i]) make with the X axis.
/// Basically a version of the .NET function 'Math.Atan2(.)' which takes multiple points instead of just the one point.
/// The returned angles lie between +/- PI.</para>
/// <para>If the point (XX[i], YY[i]) is at the origin, the angle for that point is set to zero.</para>
/// </summary>
  public static double[] RotationAngles(double[] XX, double[] YY)
  { int len = XX.Length;  if (len == 0 || YY.Length != len) return null;
    double[] result = new double[len];
    for (int i=0; i < len; i++) result[i] = Math.Atan2(YY[i], XX[i]);
    return result;
  }
/// <summary>
/// <para>There is a pivot (Xpivot, Ypivot) and a ray attached to it. The ray starts by passing through the point (Xfrom, Yfrom)
/// and rotates till it passes through (Xto, Yto). This method returns the angle through which it rotates.
/// The returned angle lies between +/- PI.</para>
/// <para>If the 'from' point and the pivot coincide, the ray when at the 'from' point is taken as being at zero angle; similarly
/// for the case where the 'to' point and the pivot coincide.</para>
/// <para>For the case where the pivot is the origin and the 'to' vector is on the X axis (+ve. dirn), it is pointless to use
/// this method; instead, directly call .NET's Math.Atan2(.) function.</para>
/// </summary>
  public static double RotationAngle(double Xfrom, double Yfrom,  double Xto,  double Yto, double Xpivot, double Ypivot)
  { double angleFrom,  angleTo;   double pie = Math.PI;
    angleFrom = Math.Atan2(Yfrom - Ypivot,   Xfrom - Xpivot);
    angleTo   = Math.Atan2(Yto   - Ypivot,   Xto   - Xpivot);
    double result = angleTo - angleFrom;
    while (result > pie)  { result -= 2.0 * pie; }
    while (result < -pie) { result += 2.0 * pie; }
    return result;
  }
  
  
  public static string DubStr(double Dub, string FmtStr)
  { int digits = 5; char type_ = 'G';   string result = "";
    if (FmtStr.Length > 1)
    { if (FmtStr[0] == 'F') type_ = 'F'; // otherwise defaults to above default.
      digits = FmtStr._Extent(1)._ParseInt(digits); } // defaults to above default.
    // Now the part which I hope one day to replace...
    if (type_== 'G')
    { switch (digits)
      { case 1 : result = Dub.ToString("G1");  break;
        case 2 : result = Dub.ToString("G2");  break;
        case 3 : result = Dub.ToString("G3");  break;
        case 4 : result = Dub.ToString("G4");  break;
        case 5 : result = Dub.ToString("G5");  break;
        case 6 : result = Dub.ToString("G6");  break;
        case 7 : result = Dub.ToString("G7");  break;
        case 8 : result = Dub.ToString("G8");  break;
        case 9 : result = Dub.ToString("G9");  break;
        case 10: result = Dub.ToString("G10"); break;
        case 11: result = Dub.ToString("G11"); break;
        case 12: result = Dub.ToString("G12"); break;
        case 13: result = Dub.ToString("G13"); break;
        case 14: result = Dub.ToString("G14"); break;
        case 15: result = Dub.ToString("G15"); break;
        default: break;
    } }
    else if (type_== 'F')
    { switch (digits)
      { case 1 : result = Dub.ToString("F1");  break;
        case 2 : result = Dub.ToString("F2");  break;
        case 3 : result = Dub.ToString("F3");  break;
        case 4 : result = Dub.ToString("F4");  break;
        case 5 : result = Dub.ToString("F5");  break;
        case 6 : result = Dub.ToString("F6");  break;
        case 7 : result = Dub.ToString("F7");  break;
        case 8 : result = Dub.ToString("F8");  break;
        case 9 : result = Dub.ToString("F9");  break;
        case 10: result = Dub.ToString("F10"); break;
        case 11: result = Dub.ToString("F11"); break;
        case 12: result = Dub.ToString("F12"); break;
        case 13: result = Dub.ToString("F13"); break;
        case 14: result = Dub.ToString("F14"); break;
        case 15: result = Dub.ToString("F15"); break;
        default: break;
    } }
    return result;
  }
// Convert a matrix into a one-dimensional array, with rows end to end: (returns NULL, if Mx faulty)
  public static double[] MxToArray(double[,] Mx)
  { if (Mx == null) return null;
    int norows = Mx.GetLength(0),  nocols = Mx.GetLength(1);  if (norows==0 || nocols==0) return null;
    double[] result = new double[norows * nocols];
    for (int rw=0; rw < norows; rw++)
    { int offset = rw*nocols;
      for (int cl=0; cl < nocols; cl++) result[offset + cl] = Mx[rw, cl];
    }
    return result;  
  }
// Convert a one-dim. array into a matrix, interpreting rows as being laid down end to end in the array.
// If an error in arguments, returns null.
  public static double[,] ArrayToMx(double[]Arr, int norows, int nocols)
  { if (Arr == null || Arr.Length == 0 || norows == 0 || nocols == 0 || Arr.Length != norows*nocols) return null;
    double[,] result = new double[norows, nocols];
    for (int rw=0; rw < norows; rw++)
    { int offset = rw*nocols;
      for (int cl=0; cl < nocols; cl++)
      { result[rw, cl] = Arr[offset + cl];
    } }
    return result;  
  }

// Square of a number - useful were the number has a horrifically long name.
  public static double Sqr(double num)
  { return num * num;}

// PRIVATE: Used by next fn. Computes ln(factorial n). (NB - log base E, not 10.) NO error checks.
// ACCURACY: Error in the Stirlingoid log approx. used is extremely good from 3 up (1/50,000,000 at 3; it is undetectable from 11 up. 
// This function uses a table (a class field which it generates at the first call) for values below 11.
  private static double logstirl(int n)
  { int ApproxFrom = 11; // use the Stirlingoid approximation from this value upwards. *** If ever changing, change defn. of 'facts[]' below!
    if (n < ApproxFrom)
    { if (FactLogTable == null) // then create the table. (No other function must ever be allowed to affect this class field.)
      { int[] facts = new int[] {1, 1, 2, 6, 24, 120, 720, 5040, 40320, 362880, 3628800}; // factorial 0 to 10.
        FactLogTable = new double[ApproxFrom];
        for (int i=0; i < ApproxFrom; i++) FactLogTable[i] = Math.Log( (double) facts[i]);
      }
      return FactLogTable[n];// Bad luck if n is negative. I warned you that this is 
    }       
    double x = (double) n;
    return x*Math.Log(x) - x + Math.Log(Math.Sqrt(2*Math.PI*x)) +
     1/(1188*Math.Pow(x,9.0))-1/(1680*Math.Pow(x,7.0))+1/(1260*Math.Pow(x,5.0)) -
     1/(360*Math.Pow(x,3.0))+1/(12*x);
  }

// Find either the factorial itself (for 0 to 170) or its log to base 10 (allowed up to Int32.MaxValue). 
// Or find the ratio of two factorials; Or find the binomial coefficient, given two values.
// INPUT: V.A.F. has up to 4 used values. 
//   [0] = the  value to be factorialized; 0 to Int32.MaxValue allowed. Log output results if [0] > 170 (or > [3] below).
//   [1] = denominator factorial r. If not binomial, used for (n!/r!); if binomial, for (n!/[r!(n-r)!]) Must lie between 0 and numerator n.
//   [2] = cue for binomial coefficient, treated as boolean; that is, absence or 0 --> is not binomial, anything else --> is binomial.
//   [3] = log-forcer; values at and above this level will be forced to the log form. If value supplied, must lie between 0 and 171 
//          (inclusive), or else value is ignored, so that the default (171) applies. (Use e.g. -1, to have it set to default.)
// OUTPUT: .B FALSE if an error ( = silly argument); error --> message in .S.
//  .X holds the value; if it is the factorial as is, .I is 1; if it is log10 of the factorial, .I is -1.
// ACCURACY: As the subfunction logstirl1(.) uses a table rather than an approximation for n < 11, error should be undetectable.
  public static Quad Factorial(params int[] NumrDenomrBinomialForcelog)
  { Quad result = new Quad(false);
    if (NumrDenomrBinomialForcelog.Length == 0){ result.S = "no arguments"; return result; }
    int Value = NumrDenomrBinomialForcelog[0];   
    if (Value < 0){ result.S = "negative value not allowed"; return result; }
    int DenomValue = 0; if (NumrDenomrBinomialForcelog.Length > 1)DenomValue = NumrDenomrBinomialForcelog[1];
    if (DenomValue < 0 || DenomValue > Value){ result.S = "denominator value out of range"; return result; }
    bool isbinomial = (NumrDenomrBinomialForcelog.Length > 2 && NumrDenomrBinomialForcelog[2] != 0);
    int ForceLogAt = 171; if (NumrDenomrBinomialForcelog.Length > 3) ForceLogAt = NumrDenomrBinomialForcelog[3];
    if (ForceLogAt < 0 || ForceLogAt > 171) ForceLogAt = 171;
    // All the arguments have been dealt with, so no more errors can happen...
    result.B = true;
    if (isbinomial) // then set the denominator factor (n-r)!. We avoid duplicating denominator factors, and don't change the final result,
                    // if we reset DenomValue to Value - DenomValue, if it is less than half of Value.
    { if (DenomValue < Value/2) DenomValue = Value - DenomValue; }
    if (Value < ForceLogAt) // DIRECT FORM REQUIRED:
    { double x, denomfactor = 1.0; // Becomes (n-r)! in the binomial case.
      if (isbinomial)
      { for(int i=2; i <= (Value-DenomValue); i++) denomfactor *= i; // if DenomValue was originally 0 or 1 or Value or (Value-1), 
                                  // it was adjusted to Value or (Value-1), and the loop was never entered, so denomfactor = 0! = 1! = 1.
      } // if not binomial, denomfactor simply remains unity.
      // Now do either n! or (n! / r!), whichever the arguments have required:
      x = 1.0; for(int i = DenomValue+1; i <= Value; i++) x *= i; 
      result.X = Math.Round(x / denomfactor); // result is theoretically already integral, but is rounded in case of numerical handling errors.
      result.I = 1; // '1' flags natural (nonlogarithmic) value.
    }
    else // LOG FORM REQUIRED: use a modified Stirling's approximation for log:
    { if (Value <= 1) result.X = 0.0; // applies for whichever of the three output options we have chosen.
      else // All logs are Naperian, until result.X is being calculated at the end.
      { double numr, denom1 = 0.0, denom2 = 0.0; 
        numr = logstirl(Value);
        if (DenomValue >= 2) denom1 = logstirl(DenomValue); 
        if (isbinomial) { int n = Value - DenomValue;   if (n >= 2) denom2 = logstirl(n); }
        result.X = numr - denom1 - denom2;  
        if (result.X < 0) result.X = - result.X; // in case of tiny negative numerical error, which may crash some code calling this fn.
        result.X *= Math.Log10(Math.E); // convert to logs to base 10.
      }
      result.I = -1; // -1 flags logarithmic value.
    }
    return result; 
  }

// ARRAY SUM OVERLOADS:
// Find the sum of all elements in an array (no error check for empty array)
  public static int ArrSum(byte[] Arr) // NOTE the return type - not byte!
  { int sum = 0; foreach (byte bb in Arr) { sum += (int)bb; } return sum; }
  public static int ArrSum(int[] Arr)
  { int sum = 0; foreach (int ii in Arr) { sum += ii; } return sum; }
  public static Int64 ArrSum(Int64[] Arr)
  { Int64 sum = 0; foreach (Int64 ii in Arr) { sum += ii; } return sum; }
  public static double ArrSum(double[] Arr)
  { double sum = 0.0; foreach (double dd in Arr) { sum += dd; } return sum; }

// Find largest or smallest element, including absolute versions.
// TypeStartEnd: First value is ExtremeType: 0 = minimum; 1 = maximum; 10 = minimum of
//  absolute values; 11 = maximum of absolute values. If absent, default is 1 (maximum).
//  Second value is StartPtr: 0 if absent. Third is EndPtr: last entry of array, if absent.
// RETURNED: .X is the value; .I is its posn. (or of the first of equal values); .B and
//  .S reflect errors (msg. in .S if .B FALSE).
// ERRORS: Silly arguments. (It is NOT an error for EndPtr to exceed array length.)
  public static Quad ArrExtreme(double[] Arr, params int[] TypeStartEnd)
  { Quad result = new Quad(false);
    int StartPtr = 0, EndPtr = Arr.Length-1, ExtremeType = 1; // defaults
    if (TypeStartEnd.Length >= 1)ExtremeType = TypeStartEnd[0];
    if (TypeStartEnd.Length >= 2)StartPtr = TypeStartEnd[1];
    if (TypeStartEnd.Length >= 3)EndPtr = TypeStartEnd[2];
    if (EndPtr >= Arr.Length) EndPtr = Arr.Length-1;
    if (StartPtr < 0 || StartPtr > EndPtr)
    { result.S = "invalid start and/or end pointers";  return result; }
    result.B = true;  result.X = Arr[StartPtr];  
    if (ExtremeType >= 10) result.X = Math.Abs(result.X);
    for (int i = StartPtr+1; i <= EndPtr; i++)
    { if (ExtremeType == 0)
      { if (Arr[i] < result.X) { result.X = Arr[i];  result.I = i; } }
      else if (ExtremeType == 1)
      { if (Arr[i] > result.X) { result.X = Arr[i];  result.I = i; } }
      else if (ExtremeType == 10)
      { if(Math.Abs(Arr[i]) < result.X){result.X=Math.Abs(Arr[i]); result.I=i;}}
      else if (ExtremeType == 11)
      { if(Math.Abs(Arr[i]) > result.X){result.X=Math.Abs(Arr[i]); result.I=i;}}
      else
      { result.B = false;  result.S = "invalid TypeStartEnd"; break;}
    }
    return result;
  }
// OVERLOAD of the above. All the same applies (including that value returns in .X,
//  even though it is an integer).
  public static Quad ArrExtreme(int[] Arr, params int[] TypeStartEnd)
  { Quad result = new Quad(false);
    int StartPtr = 0, EndPtr = Arr.Length-1, ExtremeType = 1; // defaults
    if (TypeStartEnd.Length >= 1)ExtremeType = TypeStartEnd[0];
    if (TypeStartEnd.Length >= 2)StartPtr = TypeStartEnd[1];
    if (TypeStartEnd.Length >= 3)EndPtr = TypeStartEnd[2];
    if (EndPtr >= Arr.Length) EndPtr = Arr.Length-1;
    if (StartPtr < 0 || StartPtr > EndPtr)
    { result.S = "invalid start and/or end pointers";  return result; }
    result.B = true;
    int extremum = Arr[StartPtr];
    if (ExtremeType >= 10) result.X = Math.Abs(result.X);
    for (int i = StartPtr+1; i <= EndPtr; i++)
    { if (ExtremeType == 0)
      { if (Arr[i] < extremum) { extremum = Arr[i];  result.I = i; } }
      else if (ExtremeType == 1)
      { if (Arr[i] > extremum) { extremum = Arr[i];  result.I = i; } }
      else if (ExtremeType == 10)
      { if(Math.Abs(Arr[i]) < extremum){extremum=Math.Abs(Arr[i]); result.I=i;}}
      else if (ExtremeType == 11)
      { if(Math.Abs(Arr[i]) > extremum){extremum=Math.Abs(Arr[i]); result.I=i;}}
      else
      { result.B = false;  result.S = "invalid TypeStartEnd"; break;}
    }
    result.X = (double) extremum;   return result;
  }
// Append one array to another, whatever the type. The master out of several overloads.
// No error protection. It will tolerate one of the arrays being empty (not null),
//  but not both.
// Your choices: (1) Use one of the overloads. Fine, if your array is a basic type.
//  (2) Use this one, with the same wrap-around as you see in the overloads below.
  public static Array AppendArray(Array Original, Array AddOn)
  { ArrayList arrgh = new ArrayList(Original);
    arrgh.AddRange(AddOn);
    Type T = (Original.GetValue(0)).GetType();
    Array result = Array.CreateInstance(T, Original.Length + AddOn.Length);
    arrgh.CopyTo(result, 0);
    return result;
  }
// Overloads for appending one array to another. A void function; you use the 'out' argmt.
// No error protection. They will tolerate one of the arrays being empty (not null),
//  but not both.
  public static void AppendArray(Array Original, Array AddOn, out int[] result)
  { Array arr = AppendArray(Original, AddOn);
    result = new int[arr.Length];   arr.CopyTo(result,0); }
  public static void AppendArray(Array Original, Array AddOn, out double[] result)
  { Array arr = AppendArray(Original, AddOn);
    result = new double[arr.Length];   arr.CopyTo(result,0); }
  public static void AppendArray(Array Original, Array AddOn, out string[] result)
  { Array arr = AppendArray(Original, AddOn);
    result = new string[arr.Length];   arr.CopyTo(result,0); }

// A series of overloads for element-by-element binary operation between the elements of
// two arrays. The arrays must be one-dimensional, and must have equal length.
// Operator: '+', '-', '*', '/', all with obvious meanings. Addditionally, 'M' returns
//  the max. in each case, 'm' the minimum.
// Errors set ErrNo and return null.
// ErrNo: 0 = no errors; 1 = arrays unequal in total size, or empty; 3 = unrecognized
//  operator; 10 = divn. by zero; 11 = other error thrown by system.
  public static double[] ArrOpArr(double[] Arr1, char Operator, double[] Arr2,out int ErrNo)
  { int len1 = Arr1.Length,  len2 = Arr2.Length;   ErrNo = 0;
    if (len1 != len2 || len1 == 0) {ErrNo = 1; return null;}
    if (Operator == '/') // check for divide-by-zero error:
    { for (int i=0; i<len1; i++) if (Arr2[i] == 0.0){ ErrNo=10; return null; } }
    double[] result = new double[len1];
    try
    { switch (Operator)
      { case '+': for (int i=0; i<len1; i++)result[i]=Arr1[i]+Arr2[i]; break;
        case '-': for (int i=0; i<len1; i++)result[i]=Arr1[i]-Arr2[i]; break;
        case '*': for (int i=0; i<len1; i++)result[i]=Arr1[i]*Arr2[i]; break;
        case '/': for (int i=0; i<len1; i++)result[i]=Arr1[i]/Arr2[i]; break;
        case 'M': for (int i=0; i<len1; i++)
        { if (Arr1[i] > Arr2[i]) result[i]=Arr1[i]; else result[i]=Arr2[i]; } break;
        case 'm': for (int i=0; i<len1; i++)
        { if (Arr1[i] < Arr2[i]) result[i]=Arr1[i]; else result[i]=Arr2[i]; } break;
        default: ErrNo = 3; break;
    }  }
    catch { ErrNo = 11; }
    if (ErrNo == 0) return result; else return null;
  }
  public static int[] ArrOpArr(int[] Arr1, char Operator, int[] Arr2, out int ErrNo)
  { int len1 = Arr1.Length,  len2 = Arr2.Length;   ErrNo = 0;
    if (len1 != len2 || len1 == 0) {ErrNo = 1; return null;}
    if (Operator == '/') // check for divide-by-zero error:
    { for (int i=0; i<len1; i++) if (Arr2[i] == 0.0){ ErrNo=10; return null; } }
    int[] result = new int[len1];
    try
    { switch (Operator)
      { case '+': for (int i=0; i<len1; i++)result[i]=Arr1[i]+Arr2[i]; break;
        case '-': for (int i=0; i<len1; i++)result[i]=Arr1[i]-Arr2[i]; break;
        case '*': for (int i=0; i<len1; i++)result[i]=Arr1[i]*Arr2[i]; break;
        case '/': for (int i=0; i<len1; i++)result[i]=Arr1[i]/Arr2[i]; break;
        case 'M': for (int i=0; i<len1; i++)
        { if (Arr1[i] > Arr2[i]) result[i]=Arr1[i]; else result[i]=Arr2[i]; } break;
        case 'm': for (int i=0; i<len1; i++)
        { if (Arr1[i] < Arr2[i]) result[i]=Arr1[i]; else result[i]=Arr2[i]; } break;
        default: ErrNo = 3; break;
    }  }
    catch { ErrNo = 11; }
    if (ErrNo == 0) return result; else return null;
  }
  public static int ProdTerms(int[] Arr, bool NonZeroTermsOnly)
  { int result = 1;
    for (int i=0; i<Arr.Length; i++)
    { if (!NonZeroTermsOnly  || Arr[i] != 0.0) result *=Arr[i]; }
    return result; }
/// <summary>Regard array elements as digits to given no. base, and increment or decrement
///  the no. that they represent. ([0] is lowest order digit.)
/// Returned: 0,1,2 = success; of these, 0 - array leaves as all 0's; 2 - array
///  leaves as all max digit (NoBase-1); 1 - any other state of array at leaving.
///  -1 = impossible NoBase. Oversized or negative digits are not detected (they
///  just result in GIGO). If error, ref Counter is left unchanged.
///  </summary>
  public static int Odometer(ref int[] Counter, int NoBase, bool CountUp)
  { if (NoBase < 2) return -1;
    int nodigits = Counter.Length, carry = 0;
    if (CountUp)
    { Counter[0]++;
      for (int i = 0; i < nodigits; i++)
      { Counter[i] += carry;
      if (Counter[i] >= NoBase){Counter[i]=0;  carry=1;} else break; }
    }
    else // Count down:
    { Counter[0]--;
      for (int i = 0; i < nodigits; i++)
      { Counter[i] -= carry;
      if (Counter[i]< 0){Counter[i]=NoBase-1;  carry=1;} else break; }
    }
    // Check to see if this is the highest or lowest poss. no.:
    bool allzero = true, allmax = true;
    for (int i = 0; i < nodigits; i++)
    { if (Counter[i] != NoBase-1) allmax = false;
      if (Counter[i] != 0) allzero = false; // not 'else if'
      if (!allzero && !allmax) return 1; }
    if (allzero) return 0; else return 2;
  }


/// <summary>
/// Increment 'Counter' as if it were an odometer, incrementing [0] first. (If 'CountUp' is FALSE, decrement instead.)
/// <para>CountUp TRUE: If, when Counter[i] is incremented, there is overflow (i.e. sum = NoBases[i]), the overflow is carried over 
/// to Counter[i+1]. If there is carryover from the highest element of Counter, 1 is returned. (Otherwise 0 is returned.)</para>
/// <para>CountUp FALSE: If, when Counter[i] is decremented, there is underflow (i.e. difference = -1), the underflow is subtracted from
/// Counter[i+1]. If there is underflow from the highest element of Counter, it is returned as a negative no. (Otherwise 0 is returned.)</para>
/// <para>SILLY ARGS.: No errors raised; just GIGO applies.</para>  
/// <para>RETURNED: The final value of 'carry' - +1 if register overflow, -1 if underflow, otherwise 0.</para>  
/// </summary>
  public static int Odometer(ref int[] Counter, int[] NoBases, bool CountUp)
  { int nodigits = Counter.Length, carry = 0;
    if (CountUp)
    { Counter[0]++;
      for (int i = 0; i < nodigits; i++)
      { Counter[i] += carry;  carry = 0;
        if (Counter[i] >= NoBases[i]) { Counter[i] = 0;  carry = 1; } 
        else break; 
      }
    }
    else // Count down:
    { Counter[0]--;
      for (int i = 0; i < nodigits; i++)
      { Counter[i] += carry; // 'carry' will be either 0 or -1
        carry = 0;
        if (Counter[i]< 0) { Counter[i] = NoBases[i]-1;  carry = -1; } 
        else break; 
      }
    }
    return carry;
  }
/// <summary>
/// <para>Shuffles the given array, using the 'randomize-in-place' method. Two cases:</para>
/// <para> (1) 'OrNoCards' zero (or neg): 'Cards' must be a non-null non-empty array; its contents will be shuffled.</para>
/// <para> (2) 'OrNoCards' > 0: 'Cards' is ignored (set it to NULL); it will be replaced internally by [0, 1, 2, ... (OrNoCards-1) ].</para>
/// <para>NB! If you call this in quick succession, the time-based internal C# time-gen'd random seed will not have time to change,
///  and you will keep getting the same shuffle! To avoid, either introduce delay, or use the overload below and pass the Random object.</para>
/// <para>ERRORS: When 'OrNoCards' is a positive integer, so that 'Cards' should be valid and nonempty: (a) NULL --> crash (no test);
///  (b) empty 'Cards' returns empty int[]; (c) 'Cards' of length 1 - a copy of 'Cards' is returned.</para>
/// </summary>
  public static int[] Shuffle (int[] Cards, int OrNoCards)
  { Random randy = new Random();
    int nocards;
    if (OrNoCards <= 0)
    { nocards = Cards.Length;
      if (nocards < 2)
      { if (nocards == 0) return new int[0]; else return new int[1] {Cards[0]}; }
    }
    else nocards = OrNoCards;
    int[] result = new int[nocards];
    for (int i = 0; i < nocards; i++) result[i] = i;
    // Shuffle this new ordered key array:
    int swapwith, temp;
    for (int i=0; i<nocards; i++)
    { swapwith = i + randy.Next(nocards-i); // swap with self or higher entry, rand. chosen.
      if (swapwith != i) // condition not nec. - just avoids unnec. unary swaps.
      { temp = result[i];  result[i] = result[swapwith];  result[swapwith] = temp; }
    }
    if (OrNoCards <= 0) // Apply the key to the input array:
    { for (int i = 0; i < nocards; i++) result[i] = Cards[result[i]]; }
    return result;
  }
/// <summary>
/// <para>Shuffles the given array, using the 'randomize-in-place' method. Two cases:</para>
/// <para> (1) 'OrNoCards' zero (or neg): 'Cards' must be a non-null non-empty array; its contents will be shuffled.</para>
/// <para> (2) 'OrNoCards' > 0: 'Cards' is ignored (set it to NULL); it will be replaced internally by [0, 1, 2, ... (OrNoCards-1) ].</para>
/// <para>NB! If you call this in quick succession, the time-based internal C# time-gen'd random seed will not have time to change,
///  and you will keep getting the same shuffle! To avoid, either introduce delay, or use the overload below and pass the Random object.</para>
/// <para>ERRORS: When 'OrNoCards' is a positive integer, so that 'Cards' should be valid and nonempty: (a) NULL --> crash (no test);
///  (b) empty 'Cards' returns empty int[]; (c) 'Cards' of length 1 - a copy of 'Cards' is returned.</para>
/// <para> THIS IS AN OVERLOAD of the other form of 'Shuffle()'. It keeps the random object outside of the fn. You must have preceded the call
///  to this version with "Random r = new Random();" or "..Random(1234);". (But keep it outside any loop calling this fn. - or you will have
///  the problem mentioned above!)</para>
/// </summary>
  public static int[] Shuffle (int[] Cards, int OrNoCards, Random randy)
  { int nocards;
    if (OrNoCards <= 0)
    { nocards = Cards.Length;
      if (nocards < 2)
      { if (nocards == 0) return new int[0]; else return new int[1] {Cards[0]}; }
    }
    else nocards = OrNoCards;
    int[] result = new int[nocards];
    for (int i = 0; i < nocards; i++) result[i] = i;
    // Shuffle this new ordered key array:
    int swapwith, temp;
    for (int i=0; i<nocards; i++)
    { swapwith = i + randy.Next(nocards-i); // swap with self or higher entry, rand. chosen.
      if (swapwith != i) // condition not nec. - just avoids unnec. unary swaps.
      { temp = result[i];  result[i] = result[swapwith];  result[swapwith] = temp; }
    }
    if (OrNoCards <= 0) // Apply the key to the input array:
    { for (int i = 0; i < nocards; i++) result[i] = Cards[result[i]]; }
    return result;
  }
// Numerical integration using Simpson's rule. Enters with the range for
//  integration (XLow to XHigh), and the array of equally-spaced  Y  values.
// NB - YValues must have an odd size, and be of at least size 3.
// OUTPUT: .X is the integral, .Y is the argmt. error indicator. If no error, then
//  (case 1): if max. value of 4th. differential is supplied, .Y returns as the
//  upper bound of error for the integral; (case 2) if not supplied, .Y = 0.0.
//  Argmt. Error: .Y = -1.0 if YValues too small; = -2.0 if YValues of even size;
//   .Y = -3.0 if XLow, XHigh are improper.
  public static Duplex Integrate(double[] YValues, double XLow, double XHigh,
                                                    params double[] Max4thDiffl )
  { int ysize = YValues.Length;
    if (ysize < 3) return new Duplex(0.0, -1.0);
    else if (ysize % 2 == 0) return new Duplex(0.0, -2.0);
    else if (XLow >= XHigh) return new Duplex(0.0, -3.0);

    double integral = YValues[0] + YValues[ysize-1];
    for (int i = 1; i < ysize-1; i++)integral += YValues[i]* (double)(2*(1 + i%2));
                             // YValue is mult. by 4 for odd i, 2 for even i.
    double stripwidth = (XHigh - XLow)/ (double)(ysize-1);
    integral *= stripwidth / 3.0;
    double simerr = 0.0;
    if (Max4thDiffl.Length > 0)
    { double diff4 = Math.Abs(Max4thDiffl[0]);
      simerr = (XHigh-XLow)*Math.Pow(stripwidth, 4.0)*diff4 /180.0; }
    return new Duplex(integral, simerr);
  }
// Produce a curve representing the integral of YValues along the interval
//  from XLow to XHigh. As above, Simpson's rule is used, and so YValues must be
//  odd and have at least 3 elements (pref. much more than 3). Returned: a
//  matrix of length (YValues.Length+1)/2, where elements 0,1,2... correspond to
//  elements 0,2,4... of YValues. That is, the curve is only half as dense in
//  points as the YValues curve. Corresponding X values are returned in the REF
//  array XValuesForResult. (Not accessed, if error.)
//  ConstOfIntegn is simply added to every element of the returned array.
//  ERROR returns an array of length 1, [0] being: -1.0 if YValues too small;
//   -2.0 if YValues of even size; -3.0 if XLow, XHigh are improper.
  public static double[] IntegralCurve(double[] YValues, double XLow, double XHigh,
                            double ConstOfIntegn, ref double[] XValuesForResult)
  { int ysize = YValues.Length;  double errcode = 0.0;  double[] result;
    if (ysize < 3) errcode = -1.0;
    else if (ysize % 2 == 0) errcode = -2.0;
    else if (XLow >= XHigh) errcode = -3.0;
    if (errcode != 0.0)
    { result = new double[1]; result[0] = errcode; return result; }
    // No errors:
    int reslen = (ysize+1)/2; // length of the output
    result = new double[reslen]; // the integral curve
    XValuesForResult = new double[reslen]; // X values corresp. to elements of the int. curve
    double XRange = XHigh - XLow;
    double StripwidthThird = (XHigh - XLow)/ (3.0 * (double)(ysize-1));
    for (int i = 0; i < reslen; i++)
    { XValuesForResult[i] = XLow + i*(XRange / (reslen-1)); }
    double ychain = 0;  result[0] = ConstOfIntegn;  int resindex = 1;
    for (int i = 2; i < ysize; i+=2)
    { ychain += YValues[i-2] + 4 * YValues[i-1] + YValues[i];
      result[resindex] = ychain * StripwidthThird + ConstOfIntegn;
      resindex++;
    }
    return result;
  }
// Given a Gaussian distribution curve of mean 0 and standard deviation SD,
//  find the X value on the positive side corresponding to Val. If PeakIsOne
//  is true, the bell curve is normalized such that its maximum is 1.0. Otherwise
//  its maximum height = 1 / sqrt(2.PI.sqr(SD)).
// ERROR: If Val is too high, -1.0 is returned; if negative, -2.0.
// LIMITS: Given peak of 1 and default maxerror of 1e-12, the outer bound is
//  slightly above SD * sqrt(2*ln(1/maxerror)) = 7.434 * SD. It would be safe
//  to use 7.45 or 7.5. To find out exactly, simply input Val = 0. Clearly, a pt.
//  7 x SD from the mean is far beyond statistical relevance in any real situation.
//  The closest to 0 that can be got is SD * 1.13E-6. (Occurs if Val = 1.)
  public static double InverseGauss(double Val, double SD, bool PeakIsOne)
  { if (!PeakIsOne)Val *= SD*(Math.Sqrt(2.0*Math.PI)); // The standard formula
    // for the distribution has 1 / R.H.S. as its max. amplitude; we are
    // normalizing it to have a max value of 1, and so upscaling Val.
    if (Val > 1.0)
    { if (Val < 1.0 + DefaultTiny) Val = 1.0; else return -1.0; }
    else if (Val < 0.0) return -2.0;
    // Find the x value for Val for the simple function EE^-x*x. (NB - to
    //  put in a quotient under x*x drastically increases the no. loops required
    //  below. Don't do it.)
    double xguess = 0.5, yguess = Math.Exp(-xguess*xguess);
    for (int i = 0; i < 100; i++) // the limit will never be reached
    { xguess = xguess - (Val - yguess)/(2.0*xguess*yguess);
      yguess = Math.Exp(-xguess*xguess);
      if (Math.Abs(Val - yguess) < 1e-12) break;
    }
    return Math.Sqrt(2.0)*SD*xguess; // adjusted for input SD.
  }
/// <summary>
/// <para>Returns a Gaussian random values distributed with the given mean and standard deviation.
///  Returned values can be of any size; the only cutoff would be the limits on type double.</para>
/// <para>The method used is "the polar form of the Box-Muller transformation", taken from site
///  http://www.taygeta.com/random/gaussian.html. It actually generates two uncorrelated random nos.; see method
///  RandGaussPair for exploitation of this.</para>
/// <para>A zero SD simply returns the Mean, as expected. A neg. SD does the same.</para>
/// <para>A class Random object must be predefined, to pass as an argument (using "Random r = new Random();" or "..Random(1234);").
///  It is not defined here as this fn. would usually be called repeatedly, --> big wastage of time and space.
/// </summary>
  public static double RandGauss(double Mean, double SD, Random randy)
  { if (SD <= 0.0) return Mean;
    double x1, x2, w, w1;
    do
    { x1 = 2.0 * randy.NextDouble() - 1.0;
      x2 = 2.0 * randy.NextDouble() - 1.0;
      w = x1*x1 + x2*x2;
    } while (w >= 1.0);
    w1 = Math.Sqrt( (-2.0 * Math.Log( w ) ) / w );
    return x1*w1*SD + Mean;
  }
/// <summary>
/// <para>Returns a PAIR of Gaussian random values distributed with the given mean and standard deviation.
///  Returned values can be of any size; the only cutoff would be the limits on type double.</para>
/// <para>The method used is "the polar form of the Box-Muller transformation", taken from site
///  http://www.taygeta.com/random/gaussian.html. The two returned values are said to be uncorrelated.</para>
/// <para>A zero SD simply returns the Mean, as expected. A neg. SD does the same.</para>
/// <para>A class Random object must be predefined, to pass as an argument (using "Random r = new Random();" or "..Random(1234);").
///  It is not defined here as this fn. would usually be called repeatedly, --> big wastage of time and space.
/// </summary>
  public static Duplex RandGaussPair(double Mean, double SD, Random randy) // [##### NOT YET TESTED for timing and covariance]
  { if (SD <= 0.0) return new Duplex (Mean, Mean);
    double x1, x2, w, w1;
    do
    { x1 = 2.0 * randy.NextDouble() - 1.0;
      x2 = 2.0 * randy.NextDouble() - 1.0;
      w = x1*x1 + x2*x2;
    } while (w >= 1.0);
    w1 = Math.Sqrt( (-2.0 * Math.Log( w ) ) / w );
    return new Duplex(x1*w1*SD + Mean, x2*w1*SD + Mean);
  }

//  Given two values - say, the extremes of an X axis (hence arg. names) - which are possibly quite irregular, find suitable
//    new extremes, inclusive of the old ones, with segmental intervals as reasonable as possible.
//    The larger the range of allowed segments, the more likely is it to get a presentable result.
//  RETURNED: An array of length 3: [0] = substitute for OrigXLo, [1] for OrigXHi, [2] = segmental interval, [3] = no. segments.
//    DEFAULT inputs for param argument are 2 and 12.
//    ERRORS: (1) NOT an error for OrigXLo > OrigXHi. (You just have an inverted axis scale.) (2) IS an error for OrigXLo = OrigXHi.
//     (3) IS an error for LoHiSegments[0] to be > L.H.S.[1], or <= 0.
//    Errors result in RETURN of an array of all zeroes; therefore test [2] or [3] for this.
// NB -- THERE IS ANOTHER FUNCTION in unit J_PLOT which does the same thing, but often gives you more rational scales (e.g. 20, 10, 0, ...
//    instead of (11, 6, 1,..) at the cost of looser scale fit (often a lot more redundant space). Also you have to accept its 
//    fixed choice of between 4 and 7 loops, based on its internal tables. Dial it up at "GCage.AdjustGraphLimits(..)".
  public static double[] ImproveRange(double OrigXLo, double OrigXHi, params int[] LoHiSegments)
  { double[] result = new double[4];
    int LoSegs = 2,   HiSegs = 12;
    if (LoHiSegments.Length > 0) LoSegs = LoHiSegments[0];
    if (LoHiSegments.Length > 1) HiSegs = LoHiSegments[1];    
    if (LoSegs > HiSegs || LoSegs < 1) return result;
    int NoSegs = HiSegs - LoSegs + 1;
    if (OrigXLo == OrigXHi) return result;
    bool InvertedScale = false; // We will swap OrigXLo and OrigXHi, if Lo > Hi; but the we will swap the final Lo and Hi back 
                                // at the end of the fn., and there will also make the difference between final Lo and Hi negative.
    if (OrigXLo > OrigXHi) { InvertedScale = true; double x = OrigXLo; OrigXLo = OrigXHi; OrigXHi = x; }
    double OrigDiff = OrigXHi - OrigXLo;
    double[] FinalLows = new double[NoSegs],  FinalDiffs = new double[NoSegs],  FinalHighs = new double[NoSegs],  Scores = new double[NoSegs];
    for (int segs = LoSegs; segs <= HiSegs; segs++)
    { // Firm up the value for the low end of the scale, and so get a new difference:
      double Exponent = Math.Floor(Math.Log10(OrigDiff) ); // First find the exponent of the scientific-notation form of the original difference.
      double Numer = OrigDiff * Math.Pow(10, -Exponent); // Numer is now the numerical part of the sci. notn., so lies between 1.0 and 9.99... 
      //    Let's consider some examples, with segs = 3:                1.0001,     3.45,     6.78,        9.991.	
      double Numer1 = Numer * 2.0 / segs;						   					//	==> 0.666...,   2.3,       4.52,        6.66066...
      double Numer3 = Math.Ceiling(Numer1);											//	==> 1,          3,          5,              7
      double Numer5 = Numer3 / 2.0; //(frac'l part can only be 0.5.)==> 0.5,        1.5,        2.5,           3.5
      double Segment = Numer5 * Math.Pow(10, Exponent); // This is now the segmental unit, back out of scientific notation.
      // We now have to 'floor' OrigXLo in terms of a scale divided up into these segmental units:
      double FinalXLo = Segment * Math.Floor(OrigXLo / Segment);
      double NewDiff = OrigXHi - FinalXLo; // The final difference has to be >= this value.
      //  Break NewDiff up into the Index and an Exponent of scientific notation. (Exponent will mostly retain the same value, but not always.)
      double NewExp = Math.Floor(Math.Log10(NewDiff)); // First find the exponent of the scientific-notation form of the original difference.
      double Index = NewDiff * Math.Pow(10, -NewExp); // Index now lies between 1.0 and 9.99... 
      // Find the first number above (or at) Index which is either an integer or has 0.5 as its fractional part:
      //    As examples, use the same numbers as above:           1.0001,     3.45,     6.78,       9.991.
      double x = Index * 2.0;													     //	==> 2.000...,   6.90,     13.56,     19.982...
      double p = Math.Ceiling(x);	  		 									//	==> 3,          7,        14,        20
      double FinalIndex = p / 2.0; //                        ==>  1.5,        3.5,       7,        10   (Fear not, '10' is OK - FinalDiff will work out right in the next step.)
      double FinalDiff = FinalIndex * Math.Pow(10, NewExp);
      FinalLows[segs-LoSegs] = FinalXLo;                FinalDiffs[segs-LoSegs] = FinalDiff;    
      FinalHighs[segs-LoSegs] = FinalXLo + FinalDiff;   Scores[segs-LoSegs] = OrigDiff / FinalDiff;
    }
    int bestloop = BestLoopValue(LoSegs, FinalLows, FinalDiffs, FinalHighs, Scores);
    int FinalSegs = LoSegs + bestloop; // this no. of segments produced the highest score.
    result[0] = FinalLows[bestloop];  result[1] = FinalHighs[bestloop];
    result[2] = FinalDiffs[bestloop] / (double) FinalSegs;  result[3] = (double) FinalSegs;
    if (InvertedScale) { double x = result[0]; result[0] = result[1]; result[1] = x; result[2] = -result[2]; }


    return result;
  }

  private static int BestLoopValue(int LoSegs, double[] FinalLows, double[] FinalDiffs, double[] FinalHighs, double[] Scores)
  {
    int NoTests = Scores.Length;
    double[] AdjustedScores = new double[NoTests];
    Array.Copy(Scores, AdjustedScores, NoTests);
    for (int i=0; i < NoTests; i++)
    { int segs = i + LoSegs;
      double x, y, addend1 = 0.0, addend2 = 0.0;
      // If a native score is at or above 0.7, and contains 0, it gets a boost of 1.
      double Incmt = FinalDiffs[i] / (double) (segs);
      if (Scores[i] >= 0.7)
      { x = FinalLows[i];
        for (int j = 0; j <= segs; j++) // include the right extreme, hence '>='
        { y = x + j * Incmt;
          y = Math.Round(y, 8); // should deal with numerical errors. If our scale is this low, no big harm done; 
                                //   we are only refining the prima facie limits choice. 
          if (y == 0.0) { addend1 = 1.0; break; }
        }
        AdjustedScores[i] += addend1;
      }
      // Add a differential score, based on the number of significant digits in the segmental stretch:
      string s1 = Incmt.ToString(); // the segmental increment as a char. string.		 
      string s2 = s1.Replace('-',' '); // Turn '-', '.' and '0' into spaces. (OK that internal 0's are also turned into spaces.)
      string s3 = s2.Replace('.',' ');
      string s4 = s3.Replace('0', ' '); 
      string s5 = s4.Trim(); // internal spaces - representing zeroes - are retained.
      int signifdigits = s5.Length;
      if (signifdigits == 1) addend2 = 1.0; else if (signifdigits == 2) addend2 = 0.8; else if (signifdigits == 3) addend2 = 0.3; 
                // If more than 3 signif. digits, leave addend2 as zero.
      AdjustedScores[i] += addend2;
    }
    // Now find the best scoring no. segments:
    int best = 0;  double bestscore = AdjustedScores[0];
    for (int i = 1; i < NoTests; i++)
    { if (AdjustedScores[i] > bestscore) { best = i;  bestscore = AdjustedScores[i]; } }
    return best;
  }

    /*
          brr = maxi(AdjustedScores);   best = brr[2];
          return best;
        }

        */



// End of class JM
}

// End of namespace J_Lib
}

/*
   RANDOM NUMBERS
   ~~~~~~~~~~~~~~
   Create the object once, using the usual 'new' construction:
      Random randy = new Random();  <-- version for time-changing seed; OR
      Random randy = new Random(SomeInt32);  <-- version for fixed seed.
   Then use that one object as often as nec. to get individual random nos.:
      int ii = randy.Next(10);  <-- range 0 to 9. Empty "()" --> whole +ve. integer range.
      double dd = randy.NextDouble();  <-- 0.0 <= x < 1.0.
   There's also a NextBytes(byte[]) method to fill a buffer of bytes with random nos.

   You can't change the seed; you have to create a new object, if you want to do so.
   There is a tad of predictability in the algorithms used. For super duper security,
    see "C# Cookbook" (see index - 'random') for a different generator.

   REPLACEMENT OF "U_Maths1.cpp" ROUTINES
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   signof(..)     --  int = Math.Sign(num);  -->  -1 / 0 / +1.
   eqx(..)        --  Close(..)
   rand...        --  See above notes on Random Numbers.
   Permuatations(..)  No replacement.
   Combinations(..)-  Not replacement.

 ||      .






KEEP THIS RESIDUE till have sorted out how to do generic methods:
// Find the sum of all elements in an array
  public static T ArrSum<T>(T[] Arr)
  { T term = Arr[0]; object ooo = (object) term;
    Type typoe = term.GetType();
    string typename = term.GetType().Name;
    if (typename == "Int32")
    {
      int sum = 0;
      foreach (T element in Arr)
      { object oo = element; sum += (int) oo; }
      ooo = (object) sum;        
    }
    return (T) ooo;  
  }
 
 */




