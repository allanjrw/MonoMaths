using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using JLib;

namespace JLib
{
// WARNING! TCx objects as function args. ARE AUTOMATICALLY COPIED BY REFERENCE.
public class TCx
{ public double re, im;  public bool polar;  public byte tag;
  public TCx (double real, double imag) // create rect. instance
  { re = real; im = imag; polar = false; tag = 0; }
  public TCx (double real, double imag, bool ispolar)// create any instance
  { re = real;   im = imag;   polar = ispolar;  tag = 0;}
  public override string ToString()
  { return this.ToString(false, "G5", true);
  }
// FmtStr should be "F" (fixed) or "G" (general format, = shorter of decimal and
//  exponential formats), and a no. from 1 to 15: no decimals for "F", no. signif.
//  digits for "G".
  public string ToString(bool AsDegrees, string FmtStr, bool ShowTag)
  {// Build the output string:
    string ss = JM.DubStr(re, FmtStr);
    if (polar)
    { ss += " angle ";
      if (AsDegrees) ss += JM.DubStr(im * 180.0 / Math.PI, FmtStr) + " deg.";
      else ss += JM.DubStr(im, FmtStr); }
    else // rectilinear form:
    { if (im >= 0) ss += " + J "; else ss += " - J ";
      ss += JM.DubStr(Math.Abs(im), FmtStr);  }
    if (ShowTag) ss += "   (tag = " + tag.ToString() + ")";
    return ss;
  }
  // If |re| or |im| is <= Negligible, it is reset to zero.
  public void Defluff(double Negligible)
  { if (Math.Abs(re) <= Negligible) re = 0.0;
    if (Math.Abs(im) <= Negligible) im = 0.0;
  }
  public double Abs() { return Math.Sqrt(re*re + im*im); }
  public void Conj() { im = - im; }
  // If rect, converts to polar; if polar, does nothing. In the indeterminate case
  //  of rect (0,0), (0,0) is returned but .tag --> 1. (Otherwise .tag --> 0.)
  // Returned angle is always in range -PI to +PI.
  public void ToPolar()
  { if (polar) return;
    polar = true;  tag = 0;
    if (re == 0)
    { if (im == 0){tag = 1;  return; } // leaving the setting (0,0).
      else {re = Math.Abs(im);  im = (double) Math.Sign(im) * Math.PI/2.0;} }
    else if (im == 0) // with re not zero:
    { if (re > 0) im = 0; else {re = - re; im = Math.PI;} }
    else
    { double old_re = re;
      re = Math.Sqrt(re*re + im*im);    im = Math.Atan2(im, old_re); }
  }
  public void ToRect() // GIGO applies, rather than error, if .re is negative.
  { if (!polar) return;
    double old_re = re;  re = re * Math.Cos(im);  im = old_re * Math.Sin(im);
    polar = false;  return;
  }
// Raise a complex number to a real power.
// (0+j0)^0 ), or ^neg. no., returns (0,0) in whatever output form.
  public TCx ToPower(double power, bool PolarResult)
  { TCx result = new TCx(0.0, 0.0, true), thisone = result;
    thisone.re = re; thisone.im = im; thisone.polar = polar;
    thisone.ToPolar();
    if (thisone.re != 0.0 || power > 0) // if both false, then leave result as
                                        //  above default.
    { result.re = Math.Pow(thisone.re, power);  result.im = power * thisone.im; }
      // Note that cx.re is never negative, so no 'powl()' test nec. for neg no. to frac. power.
    if (!PolarResult) result.ToRect();
    return result;
  }
  public TCx Copied() { return new TCx(re, im, polar); }

}

public class M2
{ private M2(){	} // inaccessible constructor - class is for static methods only.
  // FIELDS
  public static double TwoPI = 2 * Math.PI;
  // METHODS
  public static TCx CxAdd(TCx Term1, TCx Term2, bool PolarResult)
  { TCx result = new TCx(0.0, 0.0, false);
    TCx term1 = Term1.Copied(), term2 = Term2.Copied();
    term1.ToRect();  term2.ToRect();
    result.re = term1.re + term2.re;
    result.im = term1.im + term2.im;
    if (PolarResult) result.ToPolar();
    return result;
  }
  public static TCx CxSubtract(TCx Term1, TCx Term2, bool PolarResult)
  { TCx result = new TCx(0.0, 0.0, false);
    TCx term1 = Term1.Copied(), term2 = Term2.Copied();
    term1.ToRect();  term2.ToRect();
    result.re = term1.re - term2.re;
    result.im = term1.im - term2.im;
    if (PolarResult) result.ToPolar();
    return result;
  }
  public static TCx CxMult(TCx Term1, TCx Term2, bool PolarResult)
  { TCx result = new TCx(1.0, 0.0, true);
    TCx term1 = Term1.Copied(), term2 = Term2.Copied();
    term1.ToPolar();  term2.ToPolar();
    result.re = term1.re * term2.re;
    result.im = term1.im + term2.im;
    while (result.im > Math.PI){result.im -= 2*Math.PI; }
    while (result.im < -Math.PI){result.im += 2*Math.PI; }
    if (!PolarResult) result.ToRect();
    return result;
  }
// term1 divided by term2.  Attempted div. by 0 --> .re returns as Unlikely.
  public static TCx CxDivide(TCx Term1, TCx Term2, bool PolarResult)
  { TCx result = new TCx(1.0, 0.0, true);
    TCx term1 = Term1.Copied(), term2 = Term2.Copied();
    term1.ToPolar();  term2.ToPolar();
    if (term2.re == 0.0){result.re = JM.Unlikely;  return result;}
    result.re = term1.re / term2.re;
    result.im = term1.im - term2.im;
    while (result.im > Math.PI){result.im -= 2*Math.PI; }
    while (result.im < -Math.PI){result.im += 2*Math.PI; }
    if (!PolarResult) result.ToRect();
    return result;
  }
// Return exp(j*2*PI*Exponent). 
  public static TCx Expj2PI(double Exponent)
  { TCx result = new TCx(0.0, 0.0, false); // rect. form
    double dd = TwoPI*Exponent;
    result.re = Math.Cos(dd);  result.im = Math.Sin(dd);
    return result;  
  }
  // Return SUM [n=0 to N-1]{exp(j*2*PI*n*Coeff/N)}. 
  public static TCx SumExpj2PI(TCx[] InArr, double Coeff)
  { TCx result = new TCx(0.0, 0.0, false); // rect. form
    int N = InArr.Length; if (N==0) return result;
    double multr = Coeff /(double)N;
    for (int n=0; n<N; n++)
    { TCx expo = Expj2PI((double)n * multr);
      result = CxAdd(result, CxMult(expo, InArr[n], false), false); // rect. out
    }
    return result;
  }

// Given cx. array of size N and exp'l coefficient Coeff, return an array Result such that 
//       Result[k] = SUM [0 to N-1] of: (InArr[n]*Exp2PI(n*k/N)).
// If OutSize is 0 or negative, it is reset to size of InArr.
// 'Forward' true: forward transform, so coeff. is neg. False: inverse, so coeff. positive, and outer coeff. is 1/N.
// Note that input and output are 'rectangular' format.
 public static TCx[] Fourier(TCx[] InArr, bool Forward, int OutSize)
  { int N = InArr.Length; if (OutSize < 1) OutSize = N;
    TCx[] result = new TCx[OutSize];
    for (int i=0; i < OutSize; i++) 
    { double idub = (double) i;  if (Forward) idub = -idub;
      result[i] = SumExpj2PI(InArr, idub);
    }
    if (!Forward)
    { double ndub = 1.0 / (double) N;
      for (int i=0; i < OutSize; i++) 
      { result[i].re *= ndub;  result[i].im *= ndub; } }
    return result; 
  }

// Returns a string like "5.2X^3 - 4X^2 +...".
// FmtStr should be "Fi" or "Gi" where i is 1 to 15. "Fi" --> fixed decimal format
//  with i decimal digits; "Gi" --> shortest format, with i signif. digits.
// Leave it as "" to accept "G5" as default. RichTextFmt[0] - if true, '^n' replaced by fmt. instructions for superscript.
/// <summary>
/// <para>Returns a string representing the polynomial, in the form "5.2X^3 - 4X^2 +...".</para>
/// <para>'FmtStr' -- strictly must be "Fi" or "Gi" where i is 1 to 15. "Fi" --> fixed decimal format with i decimal digits; 
/// "Gi" --> shortest format, with i signif. digits.</para>
/// <para>'IndepVariable' -- the text to use for the indendent variable (e.g. "X" for the above example).</para>
/// <para>'RichTextFmt[0]' -- TRUE --> formatting tags for superscripts used; otherwise carats used as in the above example.</para>
/// </summary>
  public static string PolyToStr(double[] Poly, string FmtStr, string IndepVariable, params bool[] RichTextFmt)
  { string ss, term, result = "";    double xx;
    if (FmtStr == "")FmtStr = "G5";
    bool RTxtFmt = false; if (RichTextFmt.Length > 0) RTxtFmt = RichTextFmt[0];
    bool IsFirstTerm = true; // indicates that first NONZERO term has been found.
    for (int i = Poly.Length - 1; i >= 0; i--)
    { term = "";
      xx = Poly[i];
      if (xx != 0.0)
      { if (xx < 0.0) term = "- "; // neg. sign always shows, and is always
                                   //  followed by a space.
        if (!IsFirstTerm)
        { term = ' ' + term; // spacer between terms
          if (xx > 0.0) term += "+ "; // pos. sign shows only if not the first nonzero term.
        }
        xx = Math.Abs(xx);  ss = JM.DubStr(xx, FmtStr);
        if ((i>0)&&(xx == 1.0)) ss = ""; // remove a coeff. of 1
        term += ss;
        if (i > 0)
        { term += IndepVariable; 
          if (i > 1) 
          { if (RTxtFmt) term += "<^>" + i.ToString() + "</^>";
            else  term += '^' + i.ToString();
        } }
        IsFirstTerm = false;
      }
      result += term;
    }
  return result;
  }

/// <summary>
/// <para>Returns a string representing the polynomial, in the form "5.2X^3 - 4X^2 +...".</para>
/// <para>'FmtStr' -- strictly must be "Fi" or "Gi" where i is 1 to 15. "Fi" --> fixed decimal format with i decimal digits; 
/// "Gi" --> shortest format, with i signif. digits.</para>
/// <para>'RichTextFmt[0]' -- TRUE --> formatting tags for superscripts used; otherwise carats used as in the above example.</para>
/// </summary>
  public static string PolyToStr(double[] Poly, string FmtStr, params bool[] RichTextFmt)
  { return PolyToStr(Poly, FmtStr, "X", RichTextFmt);
  }

// If .re or .im is within +/- Negligible of zero, make it zero.
// No notice is taken of whether Roots[i] is polar or rect.
  public static void CleanRoots( ref TCx[] Roots, double Negligible)
  { for (int i=0; i<Roots.Length; i++) Roots[i].Defluff(Negligible);
  }
// Create a new TCx array of size SizePolar[0]. If a polar array required,
//  give a nonzero value to SizePolar[1]. (Default is rect.)
// Silly (or absent) Sz returns null.
  public static TCx[] CreateTCxArray(params int[] SizePolar)
  { int size, paramlen = SizePolar.Length;
    if (paramlen==0 || SizePolar[0] <= 0) return null; else size = SizePolar[0];
    bool IsPolar = false;
    if (paramlen>=2) IsPolar = (SizePolar[1] != 0);
    TCx[] result = new TCx[size];
    for (int i = 0; i < size; i++) result[i] = new TCx(0,0,IsPolar);
    return result;
  }
// Realistic limit of accuracy around 1e-13, due to addition; may be a couple of
//  orders less, for higher order polynomials.
    public static double EvaluatePoly(double[] Poly, double Argmt)
  { int Sz = Poly.Length;  if (Sz == 0) return 0.0;
    double result = Poly[0],    XX = 1.0;
    for (int i = 1; i < Sz; i++) { XX *= Argmt;  result += XX*Poly[i]; }
    return result;
  }
// Overload of the above, for a complex argument. NB: result will need cleaning,
//  as evaluation that should be real-only is almost certain to have a tiny
//  imag. component from numerical errors.
  public static TCx EvaluatePoly(double[] Poly, TCx Argmt, bool PolarResult)
  { TCx result = new TCx(0.0, 0.0, false);
    TCx argmt = Argmt.Copied();
    int Sz = Poly.Length;  if (Sz == 0) return result;
    result.re = Poly[0];   argmt.ToPolar();
    TCx XX = new TCx(1.0, 0.0, true), polyterm = new TCx(1.0, 0.0, true);
    TCx Boo = new TCx(0.0,0.0,true); //#######
    for (int i = 1; i < Sz; i++)
    { XX = CxMult(XX, argmt, true); // returns polar result
      polyterm.re = Poly[i];
      Boo = CxMult(XX, polyterm, false); 
      result = CxAdd(result, Boo, false); }
//      result = CxAdd(result, CxMult(XX, polyterm, false), false); }
    if (PolarResult) result.ToPolar();
    return result;
  }
// Evaluates (X - a)(X - b)(X - c)... where Roots[] consists of real values a,b,c...
//   (WATCH THE SIGN!).
  public static double EvaluatePolyFromRoots(double[] Roots, double Argmt)
  { int Sz = Roots.Length;  if (Sz == 0) return 0.0;
  double result = 1.0;
    for (int i = 0; i < Sz; i++){result *= (Argmt - Roots[i]);}
    return result;
  }
// Overload of above, for a complex argument. Evaluates (X - a)(X - b)(X - c)...
// where Roots[] is complex values [a, b, c, ...]. (WATCH THE SIGN!).
  public static TCx EvaluatePolyFromRoots(TCx[] Roots, TCx Argmt, bool PolarResult)
  { TCx result = new TCx(0.0, 0.0, false);
    int Sz = Roots.Length;  if (Sz == 0) return result;
    TCx argmt = Argmt.Copied();
    result.re = 1.0;   result.im = 0.0;   result.polar = true;
    TCx factor = new TCx(0.0,0.0,false);
    for (int i = 0; i < Sz; i++)
    { factor = CxSubtract(argmt, Roots[i], true);
      result = CxMult(result, factor, true); }
    if (!PolarResult) result.ToRect();
    return result;
  }
// Scan the values in Roots, and wherever a complex number occurs, look for its
//  conjugate. If one is found which fits within +/- Negligible (.re and .im),
//  then average values to get perfect conjugates; conjugates will end up with
//  no. with neg. im. part below no. with positive im. part.
// Negligible can be zero.
// Returns 'out FirstLoner' as -1 (all's well) or as index of first unpaired
//  complex no.
// Note that also any real or imag. no. with abs. value <= Negligible will be
//  set to 0. Also, any polar elements of CxArr will be 1st. converted to Rect.
 public static TCx[] MatchConjugates(TCx[] CxArr, double Negligible,
                                                        out int FirstLoner)
 { int arrlen = CxArr.Length;   TCx[] result = new TCx[arrlen];
  // Transfer to result, and in the process zero any fluff:
   for (int i = 0; i < arrlen; i++)
   { result[i] = CxArr[i];  result[i].ToRect();
     if (Math.Abs(result[i].re) <= Negligible) result[i].re = 0.0;
     if (Math.Abs(result[i].im) <= Negligible) result[i].im = 0.0; }
  //Create a tally of identified conjugates. (Only those above the current search
  // point need be registered.)
   bool[] Ignore = new bool[arrlen];  bool foundit;  double xx;
   for (int i = 0; i < arrlen; i++)// exclude real-only nos. from conjugate test.
   { if (result[i].im == 0.0) Ignore[i] = true; }
  // find a complex no., then look for its conjugate higher up:
   for (int i = 0; i < arrlen; i++)
   { if (!Ignore[i])
     { foundit = false;
       for (int j = i+1; j < arrlen; j++)
       { if ( !Ignore[j] && Math.Abs(result[i].re - result[j].re) <= Negligible
         &&  Math.Abs(result[i].im + result[j].im) <= Negligible)
         { foundit = true;  Ignore[j] = true;
           xx = (result[i].re + result[j].re) / 2.0;
           result[i].re = xx;  result[j].re = xx;
           xx = Math.Abs((result[i].im - result[j].im) / 2.0);
           result[i].im = -xx;  result[j].im = xx;  break;
         }
       }
       if (!foundit){FirstLoner = i;  return null;}
     }
   }
   FirstLoner = -1;  return result;
 }
// NB - MatchConjugates MUST be called first, to ensure all complex nos. are
//  conjugates (and all are rect.). Sorting is complete (first by reals; then
//  by absol. values of imag part; then + im above - im. NB - where there are
//  equal conjugate pairs of roots, all of the negative-imags will precede all
//  the positive-imags. (Hint: This enables counting of nos. of equal roots.)
// If RealRootsBelow, after sorting the real roots are bubbled down without
//  disturbing the sorting order.
// Returns .X as no. real roots, .Y as no imag. roots.
  public static Duo SortRoots(ref TCx[] Roots, bool RealRootsBelow)
  {// Sort the array using a homemade complex ICompare object (see end of file):
    Duo result = new Duo(0,0);
    int len = Roots.Length,  cnt = 0;
    IComparer com = new CxComparer();
    Array.Sort(Roots, com);
    if (RealRootsBelow)
    { TCx[] newie = new TCx[len];
      // Install the real roots into newie, without disturbing their order:
      for (int i = 0; i < len; i++)
      { if (Roots[i].im == 0.0){newie[cnt] = Roots[i]; cnt++; } }
      result.X = cnt;  result.Y = len - cnt;
      // Ditto for the complex roots:
      for (int i=0; i<len; i++)
      { if (Roots[i].im != 0.0){newie[cnt] = Roots[i]; cnt++; } }
      // Transfer roots across:
      for (int i = 0; i < len; i++) Roots[i] = newie[i];
    }
    else // set result:
    { for (int i = 0; i < len; i++){if (Roots[i].im == 0.0)cnt++;}
      result.X = cnt;  result.Y = len - cnt; }
    return result;
  }
// Derive the polynomial coefficients, given the roots. Extent is the length
//  of Roots (starting from [0]) which is to be used (higher entries ignored).
//  This is corrected to Extent.Length if too big or too small.
  public static double[] RootsToPoly(double[] Roots, int Extent)
  { int Sz = Roots.Length; if (Extent>=0 && Extent<Sz)Sz = Extent;
    // Sz now reflects the active-data length of Roots; ignore the rest.
    if (Sz == 0) return null; // important - used in complex overload of this fn.
    int noterms = 1; for (int i = 0; i < Sz; i++) noterms *= 2;//i.e. 2^Sz.
  // Set up an array representing e.g. all 4 coeffs. of (x+a)(x+b), namely
  //   b, 1, a, 1:
    double[] coeffs = new double[2*Sz];
    for (int i = 0; i < Sz; i++)
    { coeffs[2*i] = -Roots[i]; coeffs[2*i+1] = 1.0; }
  // Develop all possible terms:
    double[] poly = new double[Sz+1];
    int[] whicho = new int[Sz]; // acts as a binary array; if bracketted term
    // i is (x+a), whicho[i]=0 --> use the 'a'; =1--> use coeff. of x, i.e. 1.
    for (int i = 0; i < noterms; i++)
    { double xx = 1.0; // build up a single term:
      for (int k = 0; k < Sz; k++) xx *= coeffs[2*k + whicho[k]];
      // Find the degree of the multiple:                    ad
      int n = 0;
      for (int k = 0; k < Sz; k++) n += whicho[k];
      // Add to the total for coeffs. of multiples of that degree:
      poly[n] += xx;
      // increment the binary array:
      whicho[0]++;
      for (int j = 0; j < Sz-1; j++)
      { if (whicho[j] > 1) {whicho[j+1]++; whicho[j] = 0;} }
    }
    return poly;
  }
// Complex override of the above. NB - MatchConjugates(..)  MUST be called first,
//  so that complex roots are guaranteed rect. and paired. (Sorting is not nec.)
  public static double[] RootsToPoly(TCx[] Roots)
  { int Sz = Roots.Length;
    TCx[] roots = new TCx[Sz];
    for (int i = 0; i < Sz; i++) roots[i] = Roots[i].Copied();

    Duo rootcnt = SortRoots(ref roots, true); // Puts real roots at the bottom.
    // Make a poly. from the real roots only.
    double[] growingpoly;  int realsize = rootcnt.X;
    if (realsize == 0) growingpoly = null;
    else
    { double[] realrts = new double[realsize];
      for (int i = 0; i < realsize; i++) realrts[i] = roots[i].re;
      growingpoly = RootsToPoly(realrts, realsize);
    }
    // Extract the complex roots, and multiply each conjugate pair into realpoly:
    for (int rt = rootcnt.X; rt < Sz; rt++)
    { if (roots[rt].im < 0) // ignore the other members of the pairs
      { growingpoly = MultPolyByConjugatePair(growingpoly, roots[rt]); } }
    return growingpoly;
  }
// Multiply the given polynomial by CxNo and its complement. If Poly is NULL,
//  the quadratic formed from the conjugate pair will be returned.
  public static double[] MultPolyByConjugatePair(double[] Poly, TCx CxNo)
  { double[] Poly2 = new double[3];
    Poly2[2] = 1.0;  Poly2[1] = -2.0 * CxNo.re;
    Poly2[0] = CxNo.re*CxNo.re + CxNo.im*CxNo.im;
    if (Poly == null || Poly.Length == 0) return Poly2;
    else return MultPolys(Poly, Poly2);
  }
// Product of two polynomials. Either can be zero or just a constant, but
//  don't try sending the fn. a null array!
  public static double[] MultPolys(double[] Poly1, double[] Poly2)
  { int len1 = Poly1.Length, len2 = Poly2.Length, prodlen = len1+len2-1;
    double[] result = new double[prodlen];
    for (int i = 0; i < len2; i++)
    { for (int j = 0;  j < len1; j++)
      { result[i+j] += Poly2[i]*Poly1[j]; } }
    return result;
  }
  public static double[] DiffPoly(double[] Poly)
  { int difflen = Poly.Length-1;
    if (difflen == 0) return new double[] {0.0}; // Poly is just a constant.
    double[] result = new double[difflen];
    for (int i = 0; i < difflen; i++){result[i] = Poly[i+1] * (i+1);}
    return result;
  }
// Divide Poly by (X - Root).
  public static double[] DividePoly(double[] Poly, double Root,
                                                      out double Remainder)
  { int len = Poly.Length;  Remainder = 0.0;
    if (len == 1){ Remainder = Poly[0];  return new double[] {0.0}; }
    double[] result = new double [len-1];
    result[len-2] = Poly[len-1];
    for (int i = len-3; i >= 0; i--) // Only loops here if len >= 3.
    { result[i] = result[i+1]*Root + Poly[i+1]; }
    Remainder = result[0]*Root + Poly[0];
    return result;
  }
// Quad must have length 3, and [2] must be nonzero - no tests for these.
// Remainder returns as a poly. of length 2. Poly can be length 1 (length
//  < 3: Poly contents returned as the remainder, and ret. array is [0] = 0.0.)
  public static double[] DividePolyByQuad(double[] Poly, double[] Quad,
                                                       out double[] Remainder)
  { double[] result;  Remainder = new double[2];  int len = Poly.Length;
    if (len < 3) // return an array of size 1, [0] = 0; Remainder = poly.
    { Remainder[0] = Poly[0]; if (len==2) Remainder[1] = Poly[1];
      return new double[] {0.0}; }
  // Normalize the Quad:
    double[] Squad = new double[2]; // x^2 coeff. omitted, always being 1
    Squad[0]=Quad[0];  Squad[1] = Quad[1];
    double divisor = Quad[2];
    if (divisor != 1.0){Squad[0] /= divisor;   Squad[1] /= divisor;}
  // Poly is at least second degree:
    result = new double [len-2];
    result[len-3] = Poly[len-1];
    result[len-4] = Poly[len-2] - Squad[1] * Poly[len-1];
    for (int i = len-5; i >= 0; i--) // Only loops here if len >= 5.
    { result[i] = Poly[i+2] - Squad[0] * result[i+2] - Squad[1] * result[i+1]; }
    Remainder[1] = Poly[1]  - Squad[0] * result[1]   - Squad[1] * result[0];
    Remainder[0] = Poly[0]  - Squad[0] * result[0];
    if (divisor != 1.0) // no test for dividing by zero!
    { for (int i = 0; i < result.Length; i++)result[i] /= divisor; }
    return result;
  }
// ======================================================
// POLYNOMIAL-SOLVING:
// ------------------------------------------------------
// The following fn. has the job of providing a first-guess solution, from which
//  the second fn. (PolyOneRoot(..)) can start iterating to derive a soln.
//  It has seven methods for deriving a guess. If PolyOneRoot fails with the 1st.,
//  try the 2nd. method; etc. GuessNo enters as the latest guess method no.
//  NB - GuessNo must be >= 1 and <= 8; Poly must be >= 3rd. order. No checks!
//  CutOff: if negative, set to default 1E-12. Shouldn't be smaller, and for large
//  polys maybe should be higher.  Returned value is rect.
  public static TCx DeriveGuess(double[] Poly, int GuessNo, double CutOff)
  { TCx result = new TCx(0.0, 0.0);
    double Virt0 = 1E-70; // avoids no. processing errors for near-zero numbers.
    if (CutOff < 0.0) CutOff = 1e-12;
    double V, TinyNo = 1e-2; // Value of TinyNo was well researched.
    double HugeNo = 1e2; // = 1/TinyNo; but value found to be not as critical as
                         //  that of TinyNo.
    int ww, TopCoeff = Poly.Length-1;
    // the order below is based on best times over many trials:
    switch (GuessNo)
    { case 1 :{result.re= TinyNo; result.im= 1; break;}// v. fast, v. successful!
      case 2 :{result.re= TinyNo; result.im= HugeNo; break;}//lower HugeNo-->no advantage.
      case 3 :{result.re= TinyNo; result.im= TinyNo; break;}
      case 4 : // Quite powerful, but slow, so try others first. Works by taking
         // the top three terms as X^n-2(aX^2+bX+c) +... = 0, then ignores the '...'
         // and solves the quadratic. If b, and maybe c,... are zero, do this: Suppose
         // aX^7 +bX^3+...=0; treat as X^3(aX^4+b)=0, and solve aX^4+b=0.
      { if (Poly[TopCoeff-1] > Virt0)
        { V = JM.Sqr(Poly[TopCoeff-1])- 4.0*Poly[TopCoeff]*Poly[TopCoeff-2];
          if (Math.Abs(V) < CutOff) V = 0.0;
          if (V >= 0.0) // roots real:  select lower root and add a small imag.
                        //  component:
          { result.re = (-Poly[TopCoeff-1] - Math.Sqrt(V) )/(2.0*Poly[TopCoeff]);
            result.im = 1.0; // This is NOT a casual guess. It gives fastest solving
            // of eqn.(trough with steep sides), + near optimal success rate for the
            // guess.
          }
          else // roots complex: select lower root:
          { result.re = - Poly[TopCoeff-1]/(2.0*Poly[TopCoeff]);
            result.im = - Math.Sqrt(-V)/(2.0*Poly[TopCoeff]);
          }
        }
        else // X^N + 0.X^(N-1) +... This is essential, to prevent occas. failures:
           // Find the first non-zero coeff, if any.
        { ww = TopCoeff-2;
          while (ww > 0){if (Math.Abs(Poly[ww]) <= Virt0) ww--; else break; }
          TCx CN = new TCx(- Poly[ww] / Poly[TopCoeff], 0.0);
          result = CN.ToPower(1/TopCoeff, false);
        } break;
      }
      case 5  : { result.re = HugeNo;  result.im = TinyNo; break; }
      case 6  : { result.re = HugeNo; result.im = HugeNo; break; }
      case 7  : { result.re = -HugeNo; result.im = TinyNo; break; }
      case 8  : { result.re = -HugeNo;  result.im = -HugeNo; break; }
      default : break; // Cosmetic only - C# requires it.
    }
    return result;
  }
// Used in SolvePoly(..), as some intermediate results may have zero top coeff(s).
// Returns the size of the poly, ignoring zero values at the top. It will return
// 0, if all Poly[i]=0.  CutOff: if negative, set to default 1E-12.


// Downsizes Poly such that the top power coeff. is guaranteed > CutOff.
// NB - if NO coeff. of Poly is nonzero, Poly becomes null.
  public static void Collapse(ref double[] Poly, double CutOff)
  { if (CutOff <= 0.0) CutOff = 1e-12; // zero CutOff would rarely work, as convergent iterations would rarely reach zero difference.
    int oldlen = Poly.Length,  newlen = 0;
    for (int i = oldlen-1; i >= 0; i--)
    { if (Math.Abs(Poly[i]) > CutOff) {newlen = i+1;  break; } }
    if (newlen == oldlen) return; // no change to input poly
    if (newlen == 0){ Poly = null; return; }
    // downsize Poly to a valid new value:
    double[] boo = new double[newlen];
    for (int i = 0; i < newlen; i++) boo[i] = Poly[i];
    Poly = boo;
  }
// This works for polys. >= length 1, but is intended for cubic or higher, where
//  direct computation is not an option. Many guesses will fail; hence the need
//  for careful generation of guesses in fn. DeriveGuess(..), and for cycling
//  through a number of such guesses (as organized in SolvePoly(..)).
// Error pair .BX and .SX. If no error, .XX = real part of soln., .XY imag. part,
//  .IX is the no. of the iteration at which exit occurred. Guess must be rect.
//  CutOff: if negative, set to default 1E-12. Shouldn't be smaller, and for large
//  polys maybe should be higher.  Returned value is rect, and is defluffed.
  public static Octet PolyOneRoot(double[] Poly, TCx Guess, double CutOff)
  { Octet result = new Octet(0);
    if (CutOff < 0.0) CutOff = 1e-12;
    int Iteration = 0, MaxIterations = 200;
    double[] DiffedPoly = DiffPoly(Poly);
    TCx Trial = Guess.Copied();   Iteration = 0;
    TCx LastTrial = new TCx(JM.Unlikely, 0.0);
    TCx DiffedPolyValue, CN;
    do // while (Iteration < MaxIterations):
    { Iteration++;  result.IX = Iteration;
      TCx PolyValue = EvaluatePoly(Poly, Trial, false);
      if (Math.Abs(PolyValue.re) <= CutOff && Math.Abs(PolyValue.im) <= CutOff)
      {// regard as solution, though could be a point on an extremely flat curve -
       // multiple equal roots - for which the X-axis is tangential. Earlier Delphi
       // approaches failed to help this sitn.; it is rare enough that I am not
       // bothering with it here.
       // ##### But work on this... E.g. awful results for solving for (x-1)^5!
        Trial.Defluff(CutOff);
        result.BX=true; result.XX=Trial.re;  result.XY=Trial.im;  return result;
      }
      // If got here, there is no solution yet, so keep Newton and Raphson working:
      DiffedPolyValue = EvaluatePoly(DiffedPoly, Trial, false);
      CN = CxDivide(PolyValue, DiffedPolyValue, false);
      if (CN.re == JM.Unlikely) // tried to divide by zero:
      { result.SX = "ERROR: loop abandoned - the guess hit on a turning point.";
        return result; }
      LastTrial = Trial;
      Trial = CxSubtract(Trial, CN, false);
      if (Math.Abs(LastTrial.re - Trial.re) <= CutOff
                     && Math.Abs(LastTrial.im - Trial.im) <= CutOff)
      { result.BX=true; result.XX=Trial.re; result.XY=Trial.im; return result; }
    } while (Iteration < MaxIterations);
  // If got here, no solution has been found.
    result.SX = "ERROR: Maximum iterations failed to find root for polynomial:"
      + "\n\n" + PolyToStr(Poly, "");  return result;
  }

/// <summary>
/// <para>'Poly' -- [0] = zeroth degree term. Length must be at least 2 (poly. "(x - a)"), and the highest term coefficient must not be 0.</para>
/// <para>'CutOff' -- used to terminate convergent iterations, so must not be too small. If zero or negative it is reset to the
/// default for omission, which is 1E-12. If a poly is unsolveable ( --> error as below), try again with a larger value here.</para>
/// <para>'Report' -- If no errors, .B TRUE, .I is the no. of complex roots, and .S may contain a cautionary message; 
/// if errors, .B FALSE, .S holds error message.</para>
/// <para>Returned values are in Cartesian form, and have been cleaned of tiny |values| below CutOff.</para>
/// </summary>
  public static TCx[] SolvePoly(double[] Poly, double CutOff, out Quad Report)
  { Report = new Quad(false);
    int NoGuesses = 8; // Don't vary without rewriting fn. DeriveGuess(..)!
    int Sz = Poly.Length, CoSz, MainLoopCtr, NoSolns, GuessNo;
    double V1, xx;
    double[] Coeffs;
    // Initial set-up tasks:
    if (Sz < 2){Report.S = "not a valid polynomial"; return null;}
    TCx[] result = new TCx[Sz-1]; // the solutions.
    for (int i = 0; i < Sz-1; i++) result[i] = new TCx(0.0, 0.0);
    CoSz = Sz;  Coeffs = new double[CoSz]; // CoSz, Coeffs[i] progressively change.
    xx = Poly[Sz-1];
    if (xx==0.0){ Report.S = "Top polynomial coefficient is zero"; return null; }
    for (int i=0; i<Sz-1; i++){Coeffs[i] = Poly[i]/xx;}
    Coeffs[Sz-1] = 1.0;
    NoSolns = 0;

  // MAIN LOOP:
    MainLoopCtr = 0;
    while (true)
    { MainLoopCtr++; // for error message use only.
      // correct Coeffs, which may have accumulated top coeffs. of zero:
      Collapse(ref Coeffs, CutOff); CoSz = Coeffs.Length; // CutOff is adjusted there, rather than here.
           // CoSz can't end up <2, as lower entrance values were excluded above,
           //  and a value <2 can't arise during looping.
    // Polynomial is (AX + B):
      if (CoSz == 2)
      { result[NoSolns].re = -Coeffs[0]/Coeffs[1];  result[NoSolns].im = 0.0;
        NoSolns++;  break; } // successful solution.
  // Polynomial is (AX^2 + BX + C):
      else if (CoSz == 3)
      { V1 = JM.Sqr(Coeffs[1])- 4.0*Coeffs[2]*Coeffs[0];
        if (Math.Abs(V1) < CutOff) V1 = 0.0;
        if (V1 >= 0.0)
        { double sqrt = Math.Sqrt(V1);
          result[NoSolns].re = ( - Coeffs[1] - sqrt ) / (2.0*Coeffs[2]);
          result[NoSolns].im = 0.0;   NoSolns++;
          result[NoSolns].re = ( - Coeffs[1] + sqrt ) / (2.0*Coeffs[2]);
          result[NoSolns].im = 0.0;   NoSolns++;
        }
        else
        { result[NoSolns].re = - Coeffs[1]/(2.0*Coeffs[2]);
          result[NoSolns].im = - Math.Sqrt(Math.Abs(V1))/(2.0*Coeffs[2]);
          NoSolns++;
          result[NoSolns].re = - Coeffs[1]/(2.0*Coeffs[2]);
          result[NoSolns].im =   Math.Sqrt(Math.Abs(V1))/(2.0*Coeffs[2]);
          NoSolns++;
        }
        break;// successful solution.
      }

  // Polynomial is 3rd. or higher degree:
      else
      { TCx CN = new TCx(0.0, 0.0);   double[] DivResult;
        GuessNo = 1;
        while (true)
        { TCx Gu = DeriveGuess(Coeffs, GuessNo, CutOff);
          Octet ollie = PolyOneRoot(Coeffs, Gu, CutOff);
          if (ollie.BX) // Soln. found. (o'wise ignore error messages for now.)
          { CN.re = ollie.XX;  CN.im = ollie.XY; break; }
          GuessNo++; // No soln. found, so try again...
          if (GuessNo > NoGuesses)
          { Report.S = "attempt to find a root failed for this polynomial:\n\n"
                         + PolyToStr(Poly, "G5");
            return null;
          }
        }

      // A valid root is now in CN:
        if (Math.Abs(CN.im) <= CutOff) // regard as real root, and discard the tiny imag. bit:
        { result[NoSolns].re = CN.re;  result[NoSolns].im = 0.0;
          NoSolns++;  double Remainder;
          DivResult = DividePoly(Coeffs, CN.re, out Remainder);
          if (Remainder > 2 * CutOff)
          { Report.S = "Remainder high: " + Remainder.ToString("G5"); }
              // This is only a cautionary message - a soln. is still returned.
        }
        else // a complex root, so deal with it and its conjugate together:
        { result[NoSolns].re = CN.re; result[NoSolns].im = CN.im;
          NoSolns++;
          result[NoSolns].re = CN.re; result[NoSolns].im = -CN.im; // conjugate
          NoSolns++;
         // Form the quadratic from these two conjugal solutions, and divide:
          double[] Quadratic = new double[3];
          Quadratic[2] = 1.0;  Quadratic[1] = -2.0 * CN.re;
          Quadratic[0] = CN.re * CN.re + CN.im * CN.im;
          double[] Remainder;
          DivResult = DividePolyByQuad(Coeffs, Quadratic, out Remainder);
          if (Remainder[0] > 2 * CutOff || Remainder[1] > 2 * CutOff)
          { Report.S = "Remainder high: " +  PolyToStr(Remainder, "G5"); }
              // This is only a cautionary message - a soln. is still returned.
        }
        // Reduce Coeffs to its new form, robbed of the root(s) above:
        CoSz = DivResult.Length;
        Coeffs = new double[CoSz];
        for (int i=0; i<CoSz; i++) Coeffs[i] = DivResult[i];
      }
    }
  // If got here (only possible via 'break'), solutions have all been found.
    // Sort Solns in ascending order:
    Duo pt = SortRoots(ref result, true); // real roots go below.
      // (MatchConjugates(..) call not nec. before the above, as cx. roots are
      //  given perfect conjugates above.)
    CleanRoots(ref result, CutOff);
    Report.B = true;  Report.I = pt.Y;  return result;
  }
/// <summary>
/// <para>'LHSCoeffs' - a square matrix, at least 2x2, holding LHS coefficients.</para>
/// <para>'RHS' - an array, length = side of MxIn, holding RHS coefficients. Must not be all zeroes.</para>
/// <para>'Negligible' - essential that this be > 0, as it is used at the end to check consistency of solutions,
/// and so to detect unsolveable ("indeterminate") sets; valid solutions would be excluded. If this enters as 0 (or is nsolveegative),
/// the default 1e-10 is invoked.</para>
/// <para>OUTPUT IF SOLVED: Outcome set to .B TRUE, .I = 0, .S = ""; array of solutions is returned.</para>
/// <para>OUTPUT IF HOMOGENEOUS - i.e. all RHS values are zero: Outcome set to .B FALSE, .I = 1, .S = message; NULL returned.</para>
/// <para>OUTPUT IF INDETERMINATE because one row (at least) is a linear combination of other(s): Outcome set to .B FALSE, .I = 2, 
/// .S = message; NULL returned. (This includes the situation where all of some row or all of some column is zero.)</para>
/// <para>OUTPUT IF SILLY ARGUMENTS: Outcome set to .B FALSE, .I = -1 or -2, .S = message; NULL returned.</para>
/// </summary>
  public static double[] SolveSimultEqns(double[,]LHSCoeffs, double[] RHS, out Quad Outcome, double Negligible)
  // The routine in its C++ version was well tested with millions of passes using random nos., with and without zeroes. 
  // It is reliable! Through MiniMaths was tested to sets of 100 eqns., which took 20 secs. to solve.
  { Outcome = new Quad(false);  double multr, xx, maxval;  int winner = -1;
    int NoEqns = RHS.Length; if (NoEqns < 2){ Outcome.S = "less than two equations"; Outcome.I = -1; return null; }
    if (LHSCoeffs.GetLength(0) != NoEqns || LHSCoeffs.GetLength(1) != NoEqns)
    { Outcome.S= "either the LHS coefficients matrix is not square or its dimensions don't match RHS size"; Outcome.I = -2; return null; }
    double[] result = new double [NoEqns];
    if (Negligible <= 0.0) Negligible = 1e-10; // default. Zero can't be allowed - see tests for indeterminacy below.
    // RHS and MxIn must be copied, so as not to alter the calling code objects which they alias.
    //  The matrix eqn. to solve will then be:  LHSMx[result] = [YY]
    double[] YY = new double [NoEqns];  RHS.CopyTo(YY, 0);
    double[,] LHSMx = new double [NoEqns, NoEqns];
    for (int i = 0; i < NoEqns; i++) { for (int j = 0; j < NoEqns; j++) { LHSMx[i,j] = LHSCoeffs[i,j]; } }
    // Toss out homogeneous equations (YY = all 0's):
    bool TisOK = false;
    for (int w=0; w<NoEqns; w++)
    { if (Math.Abs(YY[w]) > Negligible){ TisOK = true; break;} }
    if (!TisOK) { Outcome.S = "eqns. unsolvable because homogeneous (RHS is all zeroes)"; Outcome.I = 1; return null;}
    for (int cl = 0; cl < NoEqns-1; cl++) // omit the last column
    {// SWAP ROWS, if nec., to get the leading diagonal element as large as possible:
      // (a) For each column, find the largest value in that column at or below the main diagonal:
      //      (We leave values above the main diagonal out of consideration, as their rows were optimized by earlier passes of this loop.)
      TisOK = false;
      maxval = Math.Abs(LHSMx[cl, cl]);   winner = cl;
      for (int rw = cl+1; rw < NoEqns; rw++) 
      { xx = Math.Abs(LHSMx[rw, cl]);  if (xx > maxval){ maxval = xx; winner = rw; } }
      // (b) If the largest value is not already on the main diagonal, get it there.
      if (winner != cl)
      { for (int coeff = 0; coeff < NoEqns; coeff++) // swap coeff. by coeff. along the length of the two eqns. being swapped
        { xx = LHSMx[cl, coeff];
          LHSMx[cl, coeff] = LHSMx[winner, coeff];
          LHSMx[winner, coeff] = xx;
        }
      // (c) finish the swapping by swapping the RHS elements also:
        xx = YY[cl];  YY[cl] = YY[winner];  YY[winner] = xx;
      }
     // Massage the equations below row 'cl' such that they contain only zeroes in column 'cl':
      for (int rw = cl+1; rw < NoEqns; rw++)
      { if (Math.Abs(LHSMx[rw,cl]) > Negligible) // if less than Negligible, ignore it. (No need to correct to 0, as it will never be accessed.)
        { multr = LHSMx[cl,cl] / LHSMx[rw,cl];// the multiplier for the row, ready for subtraction from row 'cl':
          for (int term = cl+1; term < NoEqns; term++) // no need to zero the term at cl, as it will never be accessed again.
          { xx = LHSMx[cl,term] - multr * LHSMx[rw,term];
            if (Math.Abs(xx) <= Negligible) xx = 0.0; 
            LHSMx[rw,term] = xx;
          }
          LHSMx[rw,cl] = 0; // Not really needed, but here to help if trouble shooting.
          // Adjust the RHS element similarly:
          xx = YY[cl] - multr * YY[rw];
          if (Math.Abs(xx) <= Negligible) xx = 0.0;
          YY[rw] = xx;
        }
      }
    }
    // If we have done our homework right, the bottom row should now solve
    //  directly. So starting from this, we divide and substitute:
    for (int rw=NoEqns-1; rw >= 0; rw--) // work through all the rows, from the bottom up.
    { xx = LHSMx[rw,rw]; // all to left of the main diagonal are (notionally) zeroes.
      // Test for homogeneous set of equations. If some equation was a linear combination of others, then somewhere
      //   there is going to be a row of all zeroes, after all this subtraction.
      if (Math.Abs(xx) <= Negligible)
      { Outcome.S = "eqns. unsolvable because indeterminate (at least one equation is a linear combination of other(s))"; Outcome.I = 2; return null;}
      if (Math.Abs(YY[rw]) <= Negligible) result[rw] = 0;
      else  result[rw] = YY[rw] / xx;
      // Now substitute solved values into all the above rows, so that at each row's turn, only the main diagonal term is unknown. 
      // (We save time by leaving these terms in situ, and only subtract their substituted values from YY[].)
      if (rw > 0) // If it is the top row, then there is no more substituting
      // to do above it.
      { for (int roe=rw-1; roe >= 0; roe--)
        { YY[roe] -= LHSMx[roe,rw] * result[rw]; // 'rw' is the column no. here.
    } } }
    Outcome.B = true;  return result;
  }

/// <summary>
/// <para>Provides the LU factors (as matrices LL - lower triangle mx - and UU - upper triangle mx) of an input SQUARE matrix AA.
/// The RETURN is an error code: 0 = no error; #######.</para>
/// <para>LL and UU must enter as (any) array, but will be recreated as square matrices of the same dimensions as AA.
/// <para>Note that input and output matrices are in the form of ONE-DIMENSIONAL arrays. All three will have length N*N, where
///   N is an integer ≥ 2. They will be laid down as consecutive rows.
/// </summary>
  public static int LUFactorization(ref double[] AA, out double[] LL,  out double[] UU)
  {   LL = null;  UU = null; // As these are OUT parameters, they have to be assigned before any error returns.
    int datalen = AA.Length;  if (datalen < 4) return 10; // Not enough data for even a 2X2 matrix
    int norows = Convert.ToInt32(Math.Sqrt((double) datalen)); 
    if (datalen != norows * norows) return 20; // Data does not represent a square matrix.
    LL = new double[datalen];   UU = new double[datalen];  
    Array.Copy(AA, 0, UU, 0, norows); // The first row of UU will always be equal to the first row of AA.
    for (int i=0; i < norows; i++)
    { LL[i*norows + i] =1.0; } // Set up LL as an identity matrix.
    // Loop through rows of AA:
    for (int rw = 1; rw < norows; rw++) // The zeroth. row of both UU and LL is already set, from the above.
    { // Work through the columns of this row:
      for (int cl = 0; cl < norows; cl++)
      { // Develop the sum of products, which will be used in both branches of the IF below:
        double sumOfProds = 0.0;
        for (int i = 0; i < cl; i++)
        { sumOfProds += LL[rw*norows + i] * UU[i*norows + cl]; }
        double numr = AA[rw*norows + cl] - sumOfProds;
        // Develop the unknown elements of LL in this row; i.e.  LL[0] to LL[rw-1]:
        if (cl < rw) 
        { LL[rw*norows + cl] = numr / UU[cl*norows + cl]; }
        else  // Next develop the unknown elements of UU in this row:  UU[rw] to the end of the row
        { UU[rw*norows + cl] = numr; } // We avoid a slow element-by-element inner loop again, as above.
      }
    }
    return 0;
  }

/// <summary>
/// <para>The input is a one-dim'l array, the data strip of a matrix. NoRows is supplied, and so enables internal calculation of
///  no. of columns. Rows will be swapped within the method, unless there was an error in args.</para>
/// <para>RETURN: 0 = no error;  1 = impossible number of rows for this data length;   2 = one or both row indices is out of range.
///  If the row indices are equal, no operation occurs but also no error is raised (return is 0).
/// </summary>
  public static int SwapRows(ref double[] Mx, int NoRows,  int RowIndex1,  int RowIndex2)
  { int datalen = Mx.Length;
    int NoCols = datalen / NoRows;
    if (NoCols * NoRows != datalen) return 1;
    if (RowIndex1 < 0 || RowIndex1 >= NoRows || RowIndex2 < 0 || RowIndex2 >= NoRows) return 2;
    if (RowIndex1 == RowIndex2) return 0; // No action, but also not an error state. (This would also pick up a 1x1 matrix.)
    // All correct, so swap rows:
    var heldrow = new double[NoCols];
    Array.Copy(Mx, NoCols * RowIndex1, heldrow, 0, NoCols);
    Array.Copy(Mx, NoCols * RowIndex2, Mx, NoCols * RowIndex1, NoCols);
    heldrow.CopyTo(Mx, NoCols * RowIndex2);
    return 0;
  }

/// <summary>
/// <para>The input is a one-dim'l array, the data strip of a matrix. NoRows is supplied, and so enables internal calculation of
///  no. of columns. Columns will be swapped within the method, unless there was an error in args.</para>
/// <para>RETURN: 0 = no error;  1 = impossible number of columns for this data length;   2 = one or both column indices is out of range.
///  If the column indices are equal, no operation occurs but also no error is raised (return is 0).
/// </summary>
  public static int SwapColumns(ref double[] Mx, int NoRows,  int ColIndex1,  int ColIndex2)
  { int datalen = Mx.Length;
    int NoCols = datalen / NoRows;
    if (NoCols * NoRows != datalen) return 1;
    if (ColIndex1 < 0 || ColIndex1 >= NoCols || ColIndex2 < 0 || ColIndex2 >= NoCols) return 2;
    if (ColIndex1 == ColIndex2) return 0; // No action, but also not an error state. (This would also pick up a 1x1 matrix.)
    // All correct, so swap columns:
    double held;
    int addr1, addr2;
    for (int i = 0; i < NoRows; i++)
    { addr1 = NoCols * i + ColIndex1;
      addr2 = NoCols * i + ColIndex2;
      held = Mx[addr1];
      Mx[addr1] = Mx[addr2];
      Mx[addr2] = held;
    }
    return 0;
  }

/// <summary>
/// <para>The input is a one-dim'l array, the data strip of a square matrix. Columns and/or rows will be swapped within the method,
///  as pivotting proceeds, unless there was an error in args.</para>
/// <para>The new order of rows and columns will be reflected in the 'out' arguments. E.g. if, for a 4x4 matrix, rows 1 and 2 have been
///  swapped, RowOrder will return as [0, 2, 1, 3].</para>
/// <para>If 'VirtualZero' is supplied, pivots with this or a lower absolute value will cause failure. Default value: 0.0.</para>
/// <para>RETURN: 0 = no error;  1 = array does not represent a square matrix (though it can be of any dimensions, including 1x1).
///   ≥10: = unable to find a nonzero row element in row (return value - 10) of the matrix as is at the time of return (which will not
///   usually be the same as at entry, as some swapping will have occurred already).</para>
/// </summary>
  public static int RookPivotting(ref double[] Mx, out int[] RowOrder, out int[] ColumnOrder, params double[] VirtualZero )
  { RowOrder = null;  ColumnOrder = null; // or the bl. compiler complains.
    int datalen = Mx.Length;
    int norows = Convert.ToInt32(Math.Sqrt((double) datalen));
    if (datalen != norows * norows) return 1; // Data does not represent a square matrix.
    int nocols = norows; // Makes comprehension of code a bit easier below.
    double virtZero = 0.0;
    if (VirtualZero.Length > 0) virtZero = Math.Abs(VirtualZero[0]);
    // Set up the trackers of row and column deployment:
    RowOrder = new int[norows];
    ColumnOrder = new int[nocols];
    for (int i=0; i < norows; i++)
    { RowOrder[i] = i;  ColumnOrder[i] = i; }
    // PIVOTTING:
    int rowmaxat, colwithmax,  offset;
    double rowmax, y;
    for (int pivot = 0; pivot < norows-1; pivot++)// "pivot" because this represents the main diagonal pivot position which we seek to fill.
                                               // '-1' because the last submatrix will only have one element so should not be inspected.
    { // We now search for an item which is the largest in both its subrow and its subcolumn in the submatrix of which the pivot is the top left corner.
      // We go through subrows of the matrix; at each subrow we find the element with the maximum absolute value; then we check if it is the
      // biggest element within its subcolumn. If it is, the search is over, and row / column swaps can occur. If not, we try the next row down.
      int chosenrow = pivot, chosencol = pivot; // the existing pivot, as a last resort, if not altered by the loop below.
      for (int rw = pivot; rw < norows; rw++)
      { offset = rw * nocols; // offset of the beginning of the full row of Mx
        rowmaxat = pivot; // As we are only sifting through the lower right submatrix, we set the default 'rowmaxat' to the first entry in the subrow.
        rowmax = Math.Abs(Mx[offset + pivot]); // the default maximum, the first element in the subrow.
        for (int cl = pivot+1;  cl < nocols; cl++)
        { y = Math.Abs(Mx[offset+cl]);
          if (y > rowmax) { rowmax = y;  rowmaxat = cl;  }
        }
        if (rowmax <= virtZero) return 10 + rw;
        // Now see if this is the highest value in its column:
        bool itsthebiggest = true;
        for (int rw1 = pivot; rw1 < norows; rw1++)
        { if (rw1 == rw) continue; // we don't compare 'rowmax' with itself.
          if (Math.Abs(Mx[rw1 * norows + rowmaxat]) > rowmax)
          { itsthebiggest = false;
            break;
          }
        }
        if (itsthebiggest)
        { chosenrow = rw;
          chosencol = rowmaxat;
          break;
        }
      }
      // Now get the element at Mx[chosenrow, chosencol] into the pivot position, as the new Mx[pivot, pivot].
      int n;
      if (chosenrow != pivot)
      { SwapRows(ref Mx, norows, pivot, chosenrow); // #### return value needed?
        n = RowOrder[pivot];  RowOrder[pivot] = RowOrder[chosenrow];  RowOrder[chosenrow] = n;
      }
      if (chosencol != pivot)
      { SwapColumns(ref Mx, norows, pivot, chosencol); // #### return value needed?
        n = ColumnOrder[pivot];  ColumnOrder[pivot] = ColumnOrder[chosencol];  ColumnOrder[chosencol] = n;
      }    
    }
    return 0;
  }



// Invert matrix. Returns inverted matrix (no error) or null matrix (error).
// Error: Outcome .B, .S as usual; .I = 1 for nonsquare matrix, 2 for noninvertible (singular) matrix.
  public static double[,] InverseOf(double[,] MxIn, out Quad Outcome,  double Negligible)
  { Outcome = new Quad(false);
    int Sz = MxIn.GetLength(0);
    if (MxIn.GetLength(1) != Sz) { Outcome.S = "not a square matrix";  Outcome.I = 1;  return null; }
    if (Negligible <= 0.0) Negligible = 1e-10; // default.
    // Build the augmented matrix (i.e. identity matrix tacked on right end):
    double[,] AugMx = new double[Sz, 2*Sz];
    for (int i = 0; i < Sz; i++)
    { for (int j = 0; j < Sz; j++) AugMx[i,j] = MxIn[i,j];
      AugMx[i,Sz+i] = 1.0;
    }
    double xx, multr;
   // STAGE 1: Cause the aug. mx. to have a triangle of zeroes to left:
    for (int cl=0; cl<Sz; cl++) // omit the last column
    {// If nec., swap rows to get the leading diagonal element as large as possible:
      // (a) Find the largest value in the column at or below the main diagonal:
      //      (We leave values above cl where they are, to maintain the upper triangular mx.)
     // Produce all zeroes below this main diagonal element (in the same column):
      for (int rw=cl+1; rw<Sz; rw++)
      { if (Math.Abs(AugMx[rw,cl]) > Negligible) // if less than Negligible, ignore
             // it. (No need to correct it to 0, as it will never be accessed.)
        { multr = AugMx[cl,cl] / AugMx[rw,cl];//the multiplier for the row, ready for subtraction.
          for (int term=cl+1; term<2*Sz; term++) // no need to zero the term at cl, as it
                                                    // will never be accessed again.
          { xx = AugMx[cl,term] - multr * AugMx[rw,term];
            if (Math.Abs(xx) <= Negligible) xx = 0.0; // a bit of cleaning.
            AugMx[rw,term] = xx;
          }
          AugMx[rw,cl] = 0; // Not really needed, but here to help if trouble shooting.
        }
      }
    }
  // STAGE 2: From the bottom row up, set value [i,i] to 1, and values[<i,1] to 0:
    for (int rw = Sz-1; rw >= 0; rw--)
    {// Normalize the row, such that AugMx[n,n] is 1:
      xx = AugMx[rw,rw];
      if (xx == 0.0) { Outcome.S = "matrix not invertible"; Outcome.I = 2; return null; }
      AugMx[rw,rw] = 1.0;
      for (int cl = rw+1; cl < 2*Sz; cl++) AugMx[rw,cl] /= xx;
     // Achieve zeroes in all rows above the column with the 1.0:
      for (int i = 0; i < rw; i++)
      { xx = AugMx[i,rw]; if (Math.Abs(xx) < Negligible) { AugMx[i,rw] = 0.0; break; }
        for (int j = i; j < 2*Sz; j++)AugMx[i,j] /= xx;
        for (int j = rw; j < 2*Sz; j++)AugMx[i,j] -= AugMx[rw,j];
      }
    }
  // STAGE 3: Convert the main diagonal to all 1's:
    for (int rw = 0; rw < Sz; rw++)
    { xx = AugMx[rw,rw];
      AugMx[rw,rw] = 1.0; // this last only for style (never ref'd.)
      for (int cl = rw+1; cl < 2*Sz; cl++) AugMx[rw,cl] /= xx;
    }

    double[,] MxOut = new double[Sz, Sz];
    for (int i = 0; i < Sz; i++)
    { for (int j = 0; j < Sz; j++)
      { xx = AugMx[i, Sz+j]; if (Math.Abs(xx)<= Negligible) xx = 0.0;
        MxOut[i,j] = xx; }
    }
    Outcome.B = true;  return MxOut;
  }




// Given a bunch of points, find the polynomial curve of best fit, for the given
//  polynomial degree (from 1 up. Upper limit depends on computer).
// NB: SORT POINTS FIRST. X values should be in ascending order, with no
//  duplications.
// NB2: *** At present there are problems with having all points with the same Y value. Must get sorted out...
// RETURNED: Polynomial coeffs. as an array (e.g. ax^2 + bx + c would return as
//  [c, b, a]). If error, null returned, and Success has .B false, .S with error
//  message, and .I as error no.: 1 for failure because eqns. homogeneous,
//  2 for failure because eqns. indeterminate, 3+ for improper arguments (these
//  all directly come from SolveSimultEqns(..). Also, 10 for unsorted or duplic'd
//  X values of points, 11 for no. pts. < Degree+1, and 20 for silly arguments.
  public static double[] PolyCurveFit(Duplex[] Points, int Degree,
                                                         out Quad Success)
  { Success = new Quad(false);
    if (Degree < 1)
    { Success.I=20; Success.S="degree of curve must be at least 1"; return null;}
    int NoTerms = Degree + 1,  NoPts = Points.Length;
    if (NoPts <= Degree)
    { Success.I=11; Success.S="too few points (< degree + 1)"; return null;}
  // Get the sums of X^n and YX^n values across all points:
    double[] XOnlySums = new double[2*Degree+1]; // sums of X^0 to X^(2*Degree)
    double[] XYSums = new double[NoTerms];
    XOnlySums[0] = NoPts; // sum of X^0 terms
    double x, y, prevx=0;
    for (int pt = 0; pt < NoPts; pt++)
    { x = Points[pt].X;  y = Points[pt].Y;
      if (pt > 0 && x <= prevx)
      { Success.I=10; Success.S="points must be sorted in X, with no duplications of X"; return null;}
      prevx = x;   XOnlySums[1] += x;
      for (int i = 2; i <= 2*Degree; i++) XOnlySums[i] +=  Math.Pow(x,i);
      XYSums[0] += y;    XYSums[1] += x*y;
      for (int i = 2; i < NoTerms; i++) XYSums[i] +=  y*Math.Pow(x,i);
    }
  // Develop LHS matrix and RHS vector for simult. eqn. solving
    double[,] Mx = new double[NoTerms, NoTerms]; // LHS of simult. eqns.
    double[] RHS = new double[NoTerms]; // RHS of simult. eqns.
    for (int i = 0; i < NoTerms; i++)
    { RHS[i] = XYSums[i];
      for (int j = 0; j < NoTerms; j++) Mx[i,j] = XOnlySums[i+j];
    }
    // Error indicators in solving directly transfer from S.S.E.'s parameter:
    double[] result = SolveSimultEqns(Mx, RHS, out Success, 0);
    return result;
  }

  public static double Determinant(double[,] Maxwell)
  { // Recursive. Never enter with mx. < 2x2, or you will never escape! This fn.
    //  is meant to be used with a wrapper fn., which filters out such situations,
    //  and also deals with a near-zero returned result. (There is so much
    //  numerical handling that what should be a zero determinant may be calculated
    //  by this fn. as e.g. 1.23...E-20.)
    double xx, det;   int maxrow, maxcol; int MxSide = Maxwell.GetLength(0);
    if (MxSide == 2) // the final dropout from recursion occurs here:
    { return Maxwell[0,0]* Maxwell[1,1] - Maxwell[0,1]* Maxwell[1,0]; }
    // matrix larger than 2x2:
    det = 0;
    double[,] Minwell = new double[MxSide-1, MxSide-1];
    // take each top row value, and work out its cofactor:
    for (int cl = 0; cl < MxSide; cl ++)
    { xx = Maxwell[0, cl];
      if (cl%2 == 1) xx = -xx; // the sign of the cofactor
      // Set up the cofactor's matrix, Minwell:
      for (int minrow = 0; minrow < MxSide-1; minrow ++)
      { maxrow = minrow+1;
        for (int mincol = 0; mincol < MxSide-1; mincol ++)
        { if (mincol < cl) maxcol = mincol; else maxcol = mincol+1;
          Minwell[minrow, mincol] = Maxwell[maxrow, maxcol];
      } }
      // Compute and add in the cofactor:
      det += xx * Determinant(Minwell);
    }
    return det;
  }
/// <summary>
/// Returns the submatrix of Matrix obtained by removing the row and column specified. The matrix need not be square, but
/// must have each dimension 2 or greater. (If a square matrix,the determinant of this would be the 'minor' of the Matrix 
/// for the given row and column.)
/// </summary>
  public static double[,] Submatrix(double[,] Matrix, int PivotRow, int PivotCol)
  { int noRows = Matrix.GetLength(0), noCols = Matrix.GetLength(1);
    int outrow = -1, outcol;   double[,] result = new double[noRows-1, noCols-1];
    for (int rw = 0;  rw < noRows; rw++)
    { if (rw != PivotRow) outrow++; else continue;
      outcol = -1;
      for (int cl = 0; cl < noCols; cl++)
      { if (cl != PivotCol) outcol++;  else continue; // hop over the pivotal column
        result[outrow, outcol] = Matrix[rw, cl];
      }
    }
    return result;  
  }

/// <summary>
/// <para>Develops the Characteristic Polynomial for the input matrix. Returns an array
/// representing the polynomial, with [0] being the lowest-degree term (degree 0).</para>
/// <para>Errors raised if the input matrix is not square or has dimensions less than 2x2.</para>
/// <para>The method hands over to control to another method that is recursive and involves a huge amount of calculation, 
/// so numerical errors will build up. It is therefore advisable to filter the output for tiny nonzero values.</para>
/// </summary>
  public static double[] CharacteristicPoly(double[,] Matrix)
  { // Reconstruct Maxwell as a Duplex[,], before calling the recursive method:
    int MxSide = Matrix.GetLength(0);
    if (MxSide != Matrix.GetLength(1)) throw new Exception("'M2.CharacteristicPoly' called with a matrix that is not square");
    Duplex[,] Maxwell = new Duplex[MxSide, MxSide]; // .X holds the coefficient of lamda, .Y holds the free term.
    // The following finds det(lamda.I - A), rather than the usual det(A - lamda.I). The only difference is in the sign
    // of the determinant, which is irrelevant as the determinant = 0. However this form ensures that the char. polynomial
    // will always have a highest-degree term with a positive coefficient (+1).
    for (int row=0; row < MxSide; row++)
    { for (int col=0; col < MxSide; col++)
      { if (row == col) Maxwell[row, col] = new Duplex(1, -Matrix[row, col] );
        else Maxwell[row, col] = new Duplex(0, -Matrix[row, col] );
      }
    }
    return CharPoly1(Maxwell);
  }
/// <summary>
/// This is intended as an internal recursive engine for externally available "CharacteristicPoly(double[,] Matrix".
/// As such it does no checking, as that method carries out the checks in advance.
/// </summary>
  public static double[] CharPoly1(Duplex[,] Maxwell)
  { int MxSide = Maxwell.GetLength(0);
    if (MxSide == 2) return DuplexDeterm_2x2(Maxwell); // the final dropout from recursion occurs here.
    double[] polyOut;
    Duplex[,] Minwell = new Duplex[MxSide-1, MxSide-1];
    int mincol;  Duplex pivotValue;
    polyOut = new double[MxSide+1];
    // work through each top row value as the pivot for a cofactor:
    for (int pivot = 0; pivot < MxSide; pivot++)
    {// Develop the value of the pivot: 
      pivotValue = Maxwell[0, pivot];
      if (pivot % 2 == 1) { pivotValue.X = -pivotValue.X;  pivotValue.Y = -pivotValue.Y; } // Attach the cofactor sign to it.
     // Develop the minor, i.e. the unsigned cofactor (since the above holds its sign); 
      for (int rw = 1;  rw < MxSide; rw++)
      { mincol = -1;
        for (int cl = 0; cl < MxSide; cl++)
        { if (cl != pivot) mincol++;  else continue; // hop over the pivotal column
          Minwell[rw-1, mincol] = Maxwell[rw, cl];
        }
      }
     // Generate the polynomial: 
      double[] polyMin = CharPoly1(Minwell);
      polyOut[0] += pivotValue.Y * polyMin[0];
      for (int i = 1; i < MxSide; i++) // rotate through the all but the first and last terms of the output poly
      { polyOut[i] += pivotValue.X * polyMin[i-1] + pivotValue.Y * polyMin[i]; }
      polyOut[MxSide] += pivotValue.X * polyMin[MxSide-1];
    }
    return polyOut;
  }
  // A routine for the above. Returns a double of size 3, as a polynomial.
  private static double[] DuplexDeterm_2x2(Duplex[,] Mx)
  { double[] result = new double[3];
    Duplex Dupe00 = Mx[0,0], Dupe01 = Mx[0,1], Dupe10 = Mx[1,0], Dupe11 = Mx[1,1];
    result[2] = Dupe00.X * Dupe11.X - Dupe10.X * Dupe01.X;
    result[1] = Dupe00.X * Dupe11.Y + Dupe11.X * Dupe00.Y - Dupe01.X * Dupe10.Y - Dupe10.X * Dupe01.Y;
    result[0] = Dupe00.Y * Dupe11.Y - Dupe10.Y * Dupe01.Y;
    return result;
  }
/// <summary>
/// <para>Develop a text description of the contents of a variable (double, array[] or
///  array[,,...] - but not array[][]...) suitable for saving in a text file, in a
///  format which can be read by other programs. The necessary format rules are at
///  the bottom of the MiniMaths unit SysFns. Returns a STRINGBUILDER object, as
///  one may well want to cumulate such descriptions.</para>
/// <para>Only one of XToSave, ArrToSave, FormattedArrToSave and StrToSave will be accessed. Order attempted:
///  ArrToSave first; if NULL, FormattedArrToSave next; if "" (never to be null!),
///  StrToSave next; if "" (must never be null!), XToSave last.</para>
/// <para>If FormattedArrToSave is used, it MUST start with e.g. "[100]" (simple array of length 100) or
///  "[2,4,6]" (structured array of dimensions 6 x 4 x 2). Note - lowest dimension goes in first.
///  whatever follows will be uncritically printed, so should already have been formatted elsewhere.</para>
/// <para>VarName can be any string, as long as it does not contain a colon and does
///  not start with a blank and does not contain unprintable chars.</para>
/// <para>The Log File Header is as below. It is not part of the necessary format, but
///  would usually be included in the first variable to go to a new text file.</para>
/// <para>Header is any string of explanation to go in before the rest (but after
///  the log file header, if any). Just don't let it start with a likely future
///  VarName followed by a colon, or start with a chain of '-'s. (Not a bad idea
///  to start each line with '//', which everyone understands as a comment.)</para>
/// <para>Tailer is any explanatory stuff to go at the end, before the '--------' line
///  (Except for STRING variables, where it precedes the line with 'String').
///  Format restrictions exactly as for 'Header'.</para>
/// </summary>
  public static StringBuilder FormatVarForSaving
     (double XToSave, Array ArrToSave, string FormattedArrToSave, string StrToSave, 
         string VarName, string Header, string Tailer, bool IncludeLogFileHeader, bool IncludeDateTimeStamp)
  { StringBuilder result = new StringBuilder();
    char wotizit;
    string dateTimeStamp = "";
    if (IncludeDateTimeStamp) dateTimeStamp = DateTime.Now.ToLongTimeString() + " on " + DateTime.Now.ToLongDateString();
    if (ArrToSave != null) wotizit = 'A';      else if (FormattedArrToSave != "") wotizit = 'F';
    else if (StrToSave != "") wotizit = '$';   else wotizit = 'X';
    if (IncludeLogFileHeader) // write a header:
    { result.Append ("VARIABLES LOG");
      if (IncludeDateTimeStamp) result.Append (" -- started at " + dateTimeStamp);
      result.Append('\n');
      result.Append(('=')._Chain(result.Length-1) + '\n');
    }
    if (Header != "")result.Append(Header + "\n");
    result.Append(VarName.TrimStart() + ":");
    if (IncludeDateTimeStamp) result.Append("    (as at " + dateTimeStamp + ')');
    result.Append("\n\n");
    if (wotizit == 'X') // SCALAR:
    { result.Append("Scalar, with value " + XToSave.ToString() + '\n');
    }
    else if (wotizit == '$')
    { if (Tailer != "") result.Append(Tailer + '\n'); // Tailer precedes the var.
        // for this type, as all after the 'String' line is taken as valid data.
      result.Append("String\n");  result.Append(StrToSave); }
    else if (wotizit == 'F') // FORMATTED STRING REPRESENTING AN ARRAY:
    { string ss = "";  int afterbkt = 0; // default for case where bracketted part absent or faulty
      result.Append("Array of size ");
      if (FormattedArrToSave[0] == '[')
      { int p = FormattedArrToSave.IndexOf(']');
        if (p != -1)
        { ss = FormattedArrToSave._FromTo(1, p-1);
          int[] DimSz = ss._Purge()._ToIntArray(",");
          if (DimSz != null)
          { ss = "";  afterbkt = p+1;
            for (int i=DimSz.Length-1; i>= 0; i--)
            { ss += DimSz[i].ToString();  if (i > 0) ss += " x "; }
            result.Append(ss);
          }
      } } 
      else
      { JD.Msg("Unable to save array size. File has been saved, but the array will not be loadable unless you edit in the size.");
        result.Append("???");
      }  
      result.Append("\nData:\n");
      result.Append(FormattedArrToSave._Extent(afterbkt));
    }
    else // ARRAY: ('A')
    { result.Append("Array of size ");
      int TotSz = ArrToSave.Length;
      int DimCnt = ArrToSave.Rank;
      int[] DimSz = new int[DimCnt];
      for (int i = 0; i < DimCnt; i++)
      { DimSz[i] = ArrToSave.GetLength(i);
        result.Append(DimSz[i].ToString());
        if (i < DimCnt-1) result.Append(" x "); }
      result.Append("\nData:\n");
      int[] ndx = new int[DimCnt]; // keeps track of current index
      // NB: ndx[length-1] will be the lowest dimension (nec. for 'Array.Get..')
      if (DimCnt > 2) // only insert running indexes '[i,j,k]' if 3+ dims.
      { result.Append("[0" + (", 0")._Chain(3*(DimCnt-1)) + "]\n");}//index of 1st.datum.
      for (int val = 0; val < TotSz; val++)
      { result.Append(ArrToSave.GetValue(ndx));
        JS.IncmtArray(ref ndx, false, DimSz); // indices of the next item
        if (ndx[DimCnt-1] != 0) result.Append(", ");
        else
        { result.Append(";\n"); // end of a row.
          if (val == TotSz-1) break;
          if (ndx[DimCnt-2] == 0) // end of a (sub)matrix.
          { result.Append("\n"); // a one-line break between (sub)matrices.
            if (ndx[DimCnt-2] == 0)
               // higher structure than mx, so add bookmark before each submatrix:
            { result.Append('[' + ndx._ToString(", ") + "]\n"); }
        } }
      }
    }
    // Finish the section:
    if (wotizit == '$' && Tailer != "") result.Append(Tailer + '\n');
    result.Append("-------------------------------\n");
    return result;
  }
///<summary>
/// Develop a variable from a file image of the sort generated by FormatVarForSaving(.)
///  (See end of this file for formatting requirements).
/// Deals with scalars and arrays from 1D to 3D, but not at present with higher
///  order structures; can be added later if nec.
/// RETURNED: .B and .S reflect success; .I is 0 (scalar), 1 (1D array), 2 (matrix)
///  3 (3D array), 10 (string) or -1 (error, in which case .B is false).
///  If .B is TRUE and .I is 10, then .S is the string extracted from wholetext.
/// REF ARGS: Only the relevant arg. is accessed/altered (e.g. Arr2D, if .I is set
///  to 2 by wholetext's array dimensioning statement).
/// SPECIAL INPUT: If an array is found which is MORE than 1D, ref X will be
///  referenced; if X = JM.Unlikely, only Arr1D will be filled; its contents
///  will be the raw array data not divided into rows and columns. In this case,
///  .S will return with the dimensions. E.g. if the array were "6 x 4 x 3", .S
///  would return as "3,4,6" (i.e. the innermost dimension first).
///</summary>
  public static Quad DecodeVariable(string varname, string wholetext, ref double X,
           ref double[] Arr1D, ref double[,] Arr2D, ref double[,,] Arr3D)
  { Quad result = new Quad(false);  result.I = -1;
    int dimcnt = 0;  int totsz = 1;  int[] dimsz=null;  double[] data=null;
    string ss;   int maxdims = 3; //**** current restriction; may be rewritten.
    // Look for the line that starts the record of this particular variable:
    int startptr = wholetext.IndexOf('\n' + varname + ':');
    if (startptr == -1) // one more possibility: first line of text is var name:
    { startptr = wholetext.IndexOf(varname + ':');
      if (startptr != 0) startptr = -1; }
    if (startptr == -1)
    { result.S = "variable '"+varname+"' not found in text"; return result; }
    // Look for the end line:
    int endptr = wholetext.IndexOf('\n' + "---", startptr);
    if (endptr == -1) endptr = wholetext.Length-1;
    // Convert this variable's lines to a string array:
    string[] varlines = wholetext._FromTo(startptr, endptr).Split('\n');
    bool boo = false, typefound = false, awaitingstructure = true;
    double val = 0.0;  int n, ptr;  string therest="";
    char wotizit = ' ';
    int ii = 0; 
    while (ii < varlines.Length)  // jerry fix; sometimes first line(s) are blank.
    { if (varlines[ii] == "") ii++; else break; }
    for (int i = ii+1; i < varlines.Length; i++) // line ii is the var. name line.
    { if (!typefound)
      { ss = varlines[i]._Extent(0,5).ToUpper();
        if (ss == "ARRAY"){wotizit = 'A'; typefound = true;}
        else if (ss == "SCALA"){wotizit = 'X'; typefound = true;}
        else if (ss == "STRIN"){wotizit = '$'; typefound = true;}
        // otherwise just keep looping till a line with a type code is found.
        else continue;
      }
      if (wotizit == 'X') // SCALAR:
      { char[] shrdlu = ("-0123456789").ToCharArray();
        ptr = varlines[i].IndexOfAny(shrdlu); // back to the prev. line
        if (ptr != -1) val = varlines[i]._Extent(ptr)._ParseDouble(out boo);
        if (ptr==-1 || !boo){result.S = "can't read the value"; return result;}
           // Got the scalar data, so fill the variable and then leave quietly.
        X = val;
        result.B = true;  result.I = 0;  break;
      }
      else if (wotizit == '$') // STRING:
      { for (int j = i+1; j < varlines.Length; j++)result.S += varlines[j]+'\n';
        result.S = result.S._CullEnd(1); // remove final LF. (No harm done, if result.S is empty.)
        result.B = true;  result.I = 10;  break;
      }
    // ARRAY:
      if (awaitingstructure) // GET STRUCTURAL DETAILS
      { ptr = varlines[i].IndexOfAny(JS.DigitList); // find first digit.
        if (ptr == -1){result.S="array dimension(s) not given"; return result;}
        ss = varlines[i]._Extent(ptr)._Purge();  ss = ss.ToUpper();
        string[] ssarr = ss.Split('X');  dimcnt = ssarr.Length;
        if (dimcnt > maxdims)
        { result.S="more than " + maxdims.ToString()+" dimensions, so can't load"; return result;}
        dimsz = new int[dimcnt];
        for (int j = 0; j < dimcnt; j++)
        { n = ssarr[j]._ParseInt(out boo);
          dimsz[dimcnt-j-1] = n; // reverse order to that in array ssarr.
          if(!boo){result.S="can't decipher array dimensions";return result;}
          else if (n == 0){result.S="zero array dimensions not allowed";return result;}
        }
        totsz = 1;
        for (int j = 0; j < dimcnt; j++) totsz *= dimsz[j];
        therest = "";  awaitingstructure = false;
      }
      else // ARRAY STRUCTURE KNOWN, SO SEEK DATA: (make a 1D array of the data)
      { varlines[i] = varlines[i]._Purge();
        if (varlines[i] != "") // Don't use 'continue' anywhere in this section,
           // or secn after 'if (i == varlines.Length-1)' may never be reached.
        { n = (JS.Digits+"-[").IndexOf(varlines[i][0]);
          if (n != -1) therest += varlines[i]; }// add on to the data string
         // Once all lines read in, do this:
        if (i == varlines.Length-1) // then therest is complete:
        { ss = JS.BlotOut(therest, '[', ']', ' ', 1); // Replace "[..]"
                                                    // with as many blanks.
          ss = ss._Purge();
          if (ss == "")continue; // null line, after removing sq. bkts.
          ss = ss.Replace(';', ','); // now the only sep'r should be the comma.
          ss = ss._CullEndTillNot(',');
          data = ss._ToDoubleArray(",");
          if (data == null) { result.S = "faulty data; could not use"; return result; }
          if (data.Length != totsz)
          { result.S = "expected " + totsz.ToString() + "data items, found "
              + data.Length.ToString(); return result; }
          result.B = true; break;
        }
      }
    }// END of FOR LOOP
    if (!result.B){ result.S = "data for '"+varname+"' not found"; return result; }
    if (wotizit=='A') // if it isn't, do nothing further. If it is, then develop
      // the relevant ref. arg. array from the 1D array 'data':
    { result.I = dimcnt;
      // A 1D array:
      if (dimcnt == 1) Arr1D = data;
      // A multidimensional array:
      else if (X == JM.Unlikely) // don't break up 'data':
      { Arr1D = data; // the raw data is returned.
        result.S = dimsz._ToString(","); // no spaces, and inner dim. first.
      }
      else if (dimcnt == 2)
      { Arr2D = new double[dimsz[1], dimsz[0]];  int cnt = 0;
        for (int row = 0; row < dimsz[1]; row++)
        { for (int col = 0; col < dimsz[0]; col++)
          { Arr2D[row,col] = data[cnt]; cnt++; } }
      }
      else if (dimcnt == 3)
      { Arr3D = new double[dimsz[2], dimsz[1], dimsz[0]];  int cnt = 0;
        for (int tier = 0; tier < dimsz[2]; tier++)
        { for (int row = 0; row < dimsz[1]; row++)
          { for (int col = 0; col < dimsz[0]; col++)
            { Arr3D[tier,row,col] = data[cnt]; cnt++; } } }
      }

    }
    return result;
 }

// Used by UpSample(..) for splining. WhichMode= 'F'(irst), 'L'(ast), 'B'(etween);
// Param[0],[1] are Y1 and Y2; Param[2],[3] are Y'1, Y'2; Param[4],[5] are
//  Y"1, Y"2. Only some parameters are used each time, depending on the mode.
// Output: result[i] is the coeff. for the ith. power of X.
// Error in input arg. --> null array returned.
  private static double[] EqnCoeffs(char WhichMode, params double[] Param)
  { double Y0, Y1, Ydiff0, Ydiff1, Ydouble0, Ydouble1;
    double[] result = new double[4];
    if (WhichMode == 'B')
    { Y0 = Param[0]; Y1 = Param[1]; Ydiff0 = Param[2]; Ydiff1 = Param[3];
      result[3] = - 2*Y1 +  2*Y0 + Ydiff1 +   Ydiff0;
      result[2] =   3*Y1  - 3*Y0 - Ydiff1 - 2*Ydiff0;
      result[1] = Ydiff0;  result[0] = Y0; }
    else if (WhichMode == 'F')
    { Y0 = Param[0]; Y1 = Param[1]; Ydiff1 = Param[3]; Ydouble1= Param[5];
      result[3] =     Y1 -   Y0 -   Ydiff1 +  Ydouble1/2;
      result[2] = - 3*Y1 + 3*Y0 + 3*Ydiff1 -  Ydouble1;
      result[1] =   3*Y1 - 3*Y0 - 2*Ydiff1 +  Ydouble1/2;
      result[0] = Y0; }
    else if (WhichMode == 'L')
    { Y0 = Param[0]; Y1 = Param[1]; Ydiff0 = Param[2]; Ydouble0 = Param[4];
      result[3] = Y1 - Y0 - Ydiff0 - Ydouble0/2;
      result[2] = Ydouble0/2;
      result[1] = Ydiff0;
      result[0] = Y0; }
    else  return null;
    return result;
  }
// Returns an array built from InArr, with added-in interpolations between StartAt
//  and EndAt. Returned Outcome fields: .B FALSE if error (error msg. in .S).
//  If TRUE: .I is the new EndAt, beyond which no changes were made. (StartAt
//  remains unchanged.) .X is the extra length. Errors --> null array returned.
// Example: StartAt=2, EndAt=3, UpSampleRate=4, WhichMethod='linear': array
//  [10,20,30,40,50,60] returns [10,20,  30,32.5,35,37.5,40,  50,60], and .I
//  returns as 7.
// WhichMethod: 'L'(inear interpolation); 'S'(tep) - above example
//   --> ..,30,30,30,30,40, ...); 'Z'(ero) - new spots filled with zeros;
//  'C'(ubic) - cubic spline.
  public static double[] UpSample(double[] InArr, int StartAt, int EndAt,
          int UpSampleRate,  char WhichMethod, out Quad Outcome) // def "zero"
  { Outcome = new Quad(false);
    int InArrSize = InArr.Length;
    if ( StartAt >= InArrSize || EndAt >= InArrSize || StartAt >= EndAt
                                                         || UpSampleRate < 2 )
    { Outcome.S = "faulty parameter"; return null; }
  // Create the output array (empty), and a copy of the target segment:
    int OldSecnLen = EndAt-StartAt+1,
                          ExtraLen = (EndAt-StartAt)*(UpSampleRate-1);
    Outcome.I = EndAt + ExtraLen;  Outcome.X = (double) ExtraLen;
    double[] OutArr = new double[InArrSize + ExtraLen];
  // Transfer unchanging values from the old to the new array:
  //   (Note: all w's in the  below refer to old array positions; and EndAt
  //    not adjusted yet.)
    for (int w = 0; w < StartAt; w++) OutArr[w] = InArr[w]; // unchanged part BELOW
    for (int w = EndAt; w < InArrSize; w++) OutArr[w + ExtraLen] = InArr[w];
                                                      // unchanged part ABOVE
    for (int w = StartAt; w < EndAt; w++) // Every (UpSampleRate)th. value in
                      // the interval is carried across to OutArr from InArr:
    { OutArr[StartAt + (w - StartAt)*UpSampleRate] = InArr[w]; }
  // Cubic splining case only - prepare diff'l parameters needed for the
  //   interpolating loop:
    double[] Param = new double[6];
                     // for Taylor interpoln: y1,y2; y1',y2'; y1'';y2''.
    double[] Slope = new double[OldSecnLen];
    double[] Coeff = null;   double YddStart = -1.0, YddEnd = -1.0;
    if (WhichMethod == 'C')
    { // Develop 1st. differentials for all but end points: (slope at each pt. =
      //  that of a line joining the previous and next pts. Slopes assume
      //  X-intervals of 1 unit.)
      for (int w = StartAt+1; w < EndAt; w++)
      { Slope[w-StartAt] = (InArr[w+1] - InArr[w-1])/2; }
      // Second diffs. are only calc'd. at penultimate points, for guessing slopes
      //   at end points.
      // Develop 2nd. differential for second point:
      Param[0] = InArr[StartAt+1];   Param[1] = InArr[StartAt+2];
      Param[2] = Slope[1];           Param[3] = Slope[2]; // [4],[5] not used in this call:
      Coeff = EqnCoeffs('B', Param);
      YddStart = 2.0*Coeff[2]; // used later for the first spline
      // Develop 2nd. differential for second last point:
      Param[0] = InArr[EndAt-2];     Param[1] = InArr[EndAt-1];
      Param[2] = Slope[OldSecnLen-3];     Param[3] = Slope[OldSecnLen-2];
                                           // [4],[5] not used in this call:
      Coeff = EqnCoeffs('B', Param);
      YddEnd = 6.0*Coeff[3] + 2.0*Coeff[2]; // used later for the last spline.
    }
 // Interpolate:
    int baseval;
    for (int w = StartAt; w < EndAt; w++)
    { baseval = StartAt + (w - StartAt)* UpSampleRate;
      // pre-adjustment for cubic case: develop cubic equation for this interval:
      if (WhichMethod == 'C')
      { Param[0] = InArr[w]; Param[1] = InArr[w+1];
        if (w == StartAt)
        { Param[3] = Slope[1];   Param[5] = YddStart;
          Coeff = EqnCoeffs('F', Param); }
        else if (w == EndAt-1)
        { Param[2] = Slope[OldSecnLen-2];   Param[4] = YddEnd;
          Coeff = EqnCoeffs('L', Param); }
        else
        { Param[2] = Slope[w - StartAt];   Param[3] = Slope[w+1 - StartAt];
          Coeff = EqnCoeffs('B', Param); }
      }
    // Actual interpolation for all methods:
      for (int ii = 1; ii < UpSampleRate; ii++)
      { if (WhichMethod == 'L')
          OutArr[baseval + ii] = InArr[w] + (InArr[w+1] -
                              InArr[w])* (double) ii/ (double) UpSampleRate;
        else if (WhichMethod == 'S')
          OutArr[baseval + ii] = InArr[w];
        else if (WhichMethod == 'C')
          OutArr[baseval + ii] =
                 EvaluatePoly(Coeff,(double) (ii)/ (double) UpSampleRate );
        else OutArr[baseval + ii] = 0;
      }
    }
    Outcome.B = true;  return OutArr;
  }
// StartEndPtr: [0] is StartPtr, [1] is EndPtr.
// Suppose offset is N. Start counting array elements from n=0 at StartPtr.
//  Then whenever n mod N is zero retain that element; discard the rest. (The
//  element at StartPtr is therefore always included.) EndPtr is the last element
//  referenced. Any array segment before StartPtr or after EndPtr is simply
//  tacked onto the result.
// If EndPtr is missing or exceeds array length, it is adjusted down to length-1.
//  Likewise absent StartPtr is set to 0.
// StartPtr CAN BE less than zero. If it is -k, the first retained element is
//  notionally element [-k], so that in fact the first retained element will
//  be [Factor - (|k| mod Factor)]. But you get the same effect with StartPtr
//  = plus this value, so why use a negative value?
// If StartPtr > EndPtr or Factor is < 2, the original array is simply returned
//  as is. That is, there are no error indicators in this fn. (To detect failure,
//  compare output array with input array.)
  public static double[] DownSample(double[] InArr, int Factor,
                                                   params int[] StartEndPtr)
  { if (Factor < 2) return InArr;
    int inlen = InArr.Length, outlen = 0,  noparams = StartEndPtr.Length;
    int StartPtr = 0, EndPtr = inlen-1;
    if (noparams > 0) StartPtr = StartEndPtr[0]; // Neg. StartPtr is allowed.
    if (noparams > 1 && StartEndPtr[1] < inlen) EndPtr = StartEndPtr[1];
    if (StartPtr > EndPtr) return InArr;
    // Create output array at max. poss. length; will later be downsized as nec.
    double[] newarr = new double[inlen];
    // Retain anything in the input array below StartPtr:
    for (int i = 0; i < StartPtr; i++){ newarr[i] = InArr[i]; outlen++; }
    // Retain only every (Factor)th element from here on till EndPtr:
    int cnt = Factor;
    for (int i = StartPtr; i <= EndPtr; i++)
    { if (cnt % Factor == 0)
      { if (i >= 0){newarr[outlen] = InArr[i]; outlen++; cnt = 0; } }
      cnt++;
    }
    // Retain anything in the input array above EndPtr:
    for (int i = EndPtr+1; i < inlen; i++){ newarr[outlen] = InArr[i]; outlen++; }
    // downsize the output array:
    double[] result = new double[outlen];
    Array.Copy(newarr, result, outlen);
    return result;
  }


// If error, ErrNo is nonzero and NULL array returned. (Error codes: 1 = < 2
//  data points).
// Returned (if no error): [0]=mean; [1]=SD, [2]=variance, both for divisor of (pop. size -1); 
//  [3], [4] - same, for divisor of pop. size. If AndKurtosis is TRUE, [5] is the fourth moment
//  (only one value, using the divisor 'pop. size').
  public static double[] SampleMoments(double[] DataIn, out int ErrNo, params bool[] AndKurtosis)
  { int datalen = DataIn.Length; if (datalen < 2){ ErrNo = 1; return null; }
    double[] result;  
    bool DoKurtosis = (AndKurtosis.Length > 0 && AndKurtosis[0]);
    if (DoKurtosis) result = new double[6];   else  result = new double[5];  
    double sum = 0.0,  sumsq = 0.0,  sum4 = 0;
    for (int dd = 0; dd < datalen; dd++) sum += DataIn[dd];
    double xx, xx2, mean = sum / datalen;  result[0] = mean;
    for (int dd = 0; dd < datalen; dd++)
    { xx = (mean - DataIn[dd]);  xx2 = xx*xx;  sumsq += xx2; 
      if (DoKurtosis) sum4 += xx2 * xx2;  
    }
    result[2] = sumsq / (datalen-1);  result[4] = sumsq / datalen;
    result[1] = Math.Sqrt(result[2]);  result[3] = Math.Sqrt(result[4]);
    if (DoKurtosis) result[5] = datalen * sum4/(sumsq * sumsq)  - 3.0;
    ErrNo = 0;  return result;
  }
/*
// If error, ErrNo is nonzero and NULL array returned. (Error codes: 1 = < 2
//  data points).
// Returned (if no error): [0]=mean; [1]=SD, [2]=variance, both for divisor of
//  (pop. size -1); [3], [4] - same, for divisor of pop. size.
  public static double[] SampleMoments(double[] DataIn, out int ErrNo)
  { int datalen = DataIn.Length; if (datalen < 2){ ErrNo = 1; return null; }
    double[] result = new double[5];
    double sum = 0.0, sumsq = 0.0;
    for (int dd = 0; dd < datalen; dd++) sum += DataIn[dd];
    double xx, mean = sum / datalen;  result[0] = mean;
    for (int dd = 0; dd < datalen; dd++)
    { xx = (mean - DataIn[dd]);  sumsq += xx*xx; }
    result[2] = sumsq / (datalen-1);  result[4] = sumsq / datalen;
    result[1] = Math.Sqrt(result[2]);  result[3] = Math.Sqrt(result[4]);
    ErrNo = 0;  return result;
  }
*/

// Given an array 'DataIn' of at least length 4 (usually much larger, otherwise a histogram is pointless),
//  returns a matrix which delimits data compartments and counts how many data items fit into each compartment.
// ARGUMENTS:
//  DataIn - at least of size 4.
//  NoBars - no. of compartments of data to be used in bar graph generation. This grouping will range across the interval LoBar to HiBar,
//   whether these values are user-supplied (in LoHiCutoff) or automatically generated below.
//   If NoBars < 2, the no. bars will be machine-generated on the basis of SD - three bars are to fit between mean and SD. There will be 
//   a minimum of 12 bars if machine-generation occurs.
//  LoHiCutoff - if has two values, first is LoCut and second is HiCut; if 1, value is LoCut.
//   Where either is omitted, it defaults to the minimum / maximum value in the array.
// BASIS for assigning to a compartment: If (low edge of c't.) <= x < (high edge of c't.), it goes into the compartment. Exception: 
//   If you DON'T supply a value for HiCut, then the last compartment will also contain x = (high edge of c't.); by virtue of the way
//   HiCut is machine-generated, this will cause the highest compartment to have at least 1 member.
// RETURNED:
//  A 2D jagged array with 4 rows and with column length ( NoBars + 2), this being at least 6, for the first 3 rows, and exactly 6 for the 4th.
//  Row 0: left limits of compartments;  [0] is double.NegativeInfinity.
//  Row 1: right limits of compartments; [last] is double.PositiveInfinity.
//  Row 2: no. data elements fitting into each compartments (i.e. >= left limit of compartments, and < right limit of compartments). 
//    Values in [0] and [last] represent the amount of data outside limits (see BASIS above). In the case of machine-generated LoCut and/or HiCut, 
//    will therefore be zero.
//  Row 3: Statistics: Contains 6 elements:
//   [0] - If > 0, the no. of bars; if < 0, is an error indicator: -1 = DataIn too small. -2 = LoHiCutoff - params equal or around the wrong way.
//   [1] - Mean of the data. [2] - S.D., using divisor (pop.size-1). [3] - Variance, same divisor.
//                           [4] - S.D. using divisor (pop.size).    [5] Variance, using same divisor.
// IF ERROR: The first 3 rows MAY be NULL, but the fourth row will always exist, with length 6; so ALWAYS CHECK [3][0] BEFORE ACCESSING DATA.
  public static double[][] HistogramData(ref double[] DataIn, int NoBars, params double[] LoHiCutoff)// Don't worry, DataIn is not altered.
  { double[][] result = new double[4][];  int dummy=0;
    result[3] = new double[6]; // STATS row.    
    int datalen = DataIn.Length;  if (datalen < 4) { result[3][0] = -1.0; return result; } // insufficient data.
    double LoCut, HiCut;
    int lohilen = LoHiCutoff.Length;
    if (lohilen >= 1) LoCut = LoHiCutoff[0];  else LoCut = JM.ArrExtreme(DataIn, 0).X; // minimum value.
    if (lohilen >= 2) HiCut = LoHiCutoff[1]; else HiCut = JM.ArrExtreme(DataIn, 1).X; // maximum value.
    if (LoCut >= HiCut) { result[3][0] = -2.0; return result; } // silly LoHiCutoff setting.
    // GET THE STATS:
    double[] moments = SampleMoments(DataIn, out dummy);
    for (int i=0; i< 5; i++) result[3][i+1] = moments[i]; 
    if (NoBars < 2) // then no suitable user value for NoBars supplied...
    // SET NO BARS:
    { double SD = moments[3];
      double MtoSD = 3.0;// no. bars to fit between the mean and the SD.
      NoBars = (int) (MtoSD*(HiCut - LoCut)/SD);
      if (NoBars < 12) NoBars = 12;
    }
    result[3][0] = NoBars;
    // SET BAR LIMITS:
    // 'lefts' holds low limits of compartments, 'rights' holds their highlimits.
    double[] lefts = new double[NoBars+2],  rights = new double[NoBars+2];
    double barwidth = (HiCut - LoCut) / NoBars;
    lefts[0] = double.NegativeInfinity;
    for (int i=1; i <= NoBars; i++) lefts[i] = LoCut + (i-1)*barwidth;   lefts[NoBars+1] = HiCut;
    for (int i=0; i <= NoBars; i++) rights[i] = lefts[i+1];   rights[NoBars+1] = double.PositiveInfinity;
    // ASSIGN VALUES:  
    double[] values = new double[NoBars+2];
    int cnt = 0;
    for (int datum = 0; datum < datalen; datum++)
    { double x = DataIn[datum];
      if (x < LoCut) { values[0]++; cnt++; }  
      else if (x >= HiCut) { values[NoBars+1]++; cnt++; }
      else 
      { for (int i = 1; i <= NoBars; i++)
        { if (x >= lefts[i] && x < rights[i]) { values[i]++; cnt++; break; } }
      }
    }      
    // Should never occur, but stick it in anyway...
    if (cnt != datalen) { JD.Msg("Internal error in function 'JM.HistogramData'. Data still usable, but " + (datalen - cnt).ToString() + " value(s) have been left out."); }
    // FINALIZE THE DATA TO RETURN
    if (lohilen < 2) { values[NoBars] += values[NoBars+1];  values[NoBars+1] = 0.0; } // see header remarks under 'BASIS:'.
    result[0] = lefts;  result[1] = rights;  result[2] = values;
    return result;
  }
// Two overloads for generating factors. The first uses a library of primes you supply; so if you are factorizing several numbers, the
//  largest of which is HighestNumber, first generate list "MyList = M2.PrimeEngine(2, HighestNumber, -1, true);", then supply that one list 
//  to each successive call. Use the second overload for one-off factorization (it generates the primes list internally).
// RULES FOR BOTH OVERLOADS:
// Divisors are regarded as factors if they are >= 2, up to and including the number itself (if prime); e.g. the factors of 13 are '13',
//  the factors of 12 are '2, 2, 3'.
// InValue < 2 returns a NULL list. 
//  ------- 
// OVERLOAD 1:
// 'Library' can be any integers (any values < 2 will be ignored). If using integers which themselves have common factors (e.g. 8 and 4) 
//   it would usually be better to have the bigger one first (depending on your application). If simply after all the factors, MAKE SURE
//   that 'Library' contains all the primes from 2 to at least InValue inclusive.
// If no factors are found (e.g. a prime no., and Library does not extend to it), a NULL list is returned. 
  public static List<int> FactorsOf(int InValue, ref List <int> Library)
  { if (InValue < 2) return null;
    List <int> factors = new List<int>();
    foreach (int defacto in Library)
    { if (defacto < 2) continue; // ignore silly factors.
      while (InValue % defacto == 0) // then defacto is a valid factor:
      { factors.Add(defacto);
        InValue /= defacto;
        if (InValue == 1) break;
      }
    }
    if (factors.Count == 0) return null;  
    return factors;
  }
  // OVERLOAD 2:
  public static List<int> FactorsOf(int InValue)
  { if (InValue < 2) return null;
    // Develop a library of prime numbers up to InValue itself:
    List<int> PrimesLibrary = PrimeEngine(2, InValue, -1, true);
    return FactorsOf(InValue, ref PrimesLibrary);
  }
  // LIST-PRODUCING OVERLOADS:
  

// Develop prime numbers. At present this uses a very altmodischer algorithm - just a regular seive.
// PrimeEngine arguments: If ArgsCardinal, then FromPtr is the first candidate to test for being 
//  a prime no., and ToPtr the last - or, if it is negative, ignore it and provide Extent primes. 
//  If not cardinal, then ordinal: start at the (FromPtr)th. prime no., and continue to the
//  (ToPtr)th. inclusive, or if <=0, provide Extent primes.
// Restraints: FromPtr - smallest cardinal is 2, ordinal is 1 (i.e. first prime is 2). Don't try silly args., as no checks.
  public static List<int> PrimeEngine(int FromPtr, int ToPtr, int Extent, bool ArgsCardinal)
  { List<int> primes = new List<int>();
    bool UseExtent = (ToPtr <= 0);
    primes.Add(2);   
    int prima = 0, thisnum = 1, enough = 0;   bool isprime = false;
    int quotient = 0, primeindex = 1; // slot for the next prime ([0] already filled, with '2')
    int firstwanted = -1; // the first index of 'primes' wanted by user. 
    if (ArgsCardinal && FromPtr <= 2) firstwanted = 0; // in all other cardinal situations, we have to develop firstwanted during the loop.
    if (!ArgsCardinal) 
    { firstwanted = FromPtr-1; if (firstwanted < 0) firstwanted = 0; } // user's 'FromPtr' is to base 1, prime[] index to base 0.
    while (true)
    { thisnum += 2; // first thisnum will be 3, which will be the second prime.
      enough = (int) Math.Round(Math.Sqrt(thisnum));
      isprime = true;
      for (int i = 1; i < primeindex; i++)      
      { prima = primes[i];
        if (i > enough) break;
        quotient = thisnum / prima;
        if (quotient*prima == thisnum){ isprime = false; break; }
      }
      if (isprime)
      { primes.Add(thisnum); // it is at index 'primeindex'
        // Where firstwanted not yet set, set it:
        if (firstwanted == -1 && thisnum >= FromPtr) firstwanted = primeindex;
        primeindex++;
        // Break out from the loop when done:
        if (UseExtent)
        { if (firstwanted >= 0 && primeindex >= firstwanted + Extent) break; }
        else // use ToPtr:
        { if (ArgsCardinal)
          { if (thisnum >= ToPtr) 
            { if (thisnum > ToPtr && primes.Count > 0) primes.RemoveAt(primes.Count-1); 
              break;
            }
          }
          else // ordinal args.:
          { if (primeindex == ToPtr) break; } // Recall that ToPtr is to base 1.
        }
      }
    }  
    // Trim off unwanted lower primes:
    if (firstwanted > 0)
    { if (firstwanted >= primes.Count) return new List<int>();
      primes.RemoveRange(0, firstwanted);
    }
    return primes;
  }

} // END OF CLASS M2

  // E.g. 3+j1 > 2+j1;  3 +/- j2 > 3 +/- j1;  3+j2 > 3-j2.
public class CxComparer : IComparer
{ public int Compare( Object x, Object y )
  { TCx cx1 = (TCx) x, cx2 = (TCx) y;
    double xx = cx1.re,  yy = cx2.re;
    if (xx > yy) return 1; else if (xx < yy) return -1;
    // Equal real parts:
    xx = Math.Abs(cx1.im);  yy = Math.Abs(cx2.im);
    if (xx > yy) return 1; else if (xx < yy) return -1;
    // Equal real and (absol. value of) complex parts:
    xx = cx1.im;  yy = cx2.im;
    if (xx > yy) return 1; else if (xx < yy) return -1;
    else return 0; // two exactly equal complex nos.
  }
}

} // END OF NAMESPACE JLIB
/*
FORMAT FOR VARIABLES LOG: requirements for successful use of DecodeVariable(..):
During processing, all lines will have initial blanks removed, but not internal
  blanks.
Each variable's section must start with the variable name immediately followed by a
  colon. (Internal spaces are not removed: the name being sought must match with
  any spaces up to the colon. Any chars. - except obviously ':' and an opening
  blank - may be used.)
Each variable's section ends with a line of at least 3 dashes.
Any lines not between these two limiting lines are completely ignored (e.g. there
  might be a heading and an introductory paragraph.)
STRINGS:
A line after the first must begin with "String" (case unimportant). All lines from
  the next to the one before '-----...' are then concatenated (with separating
  '\n' between lines - but not at the end) to form the returned variable.
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
  square brackets would usually be to indicate contents of current line of data.)
*/


