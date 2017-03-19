// JenkinsTraubRPoly.cs (C) yamamoto2002
// This code is C# port of RPoly C++ implementation.
//
// Following is find_polynomial_roots_jenkins_traub.cc's license:

// Copyright (C) 2015 Chris Sweeney
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//
//     * Redistributions in binary form must reproduce the above
//       copyright notice, this list of conditions and the following
//       disclaimer in the documentation and/or other materials provided
//       with the distribution.
//
//     * Neither the name of Chris Sweeney nor the names of its contributors may
//       be used to endorse or promote products derived from this software
//       without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    public class JenkinsTraubRpoly {
        private RealPolynomial mPoly;
        private RealPolynomial mKPoly;
        private List<WWComplex> mRoots = new List<WWComplex>();
        
        enum ConvergenceType {
            NO,
            LINEAR,
            QUADRATIC
        };

        private const double kDegToRad = Math.PI / 180.0;
        /// <summary>
        /// Number of zero-shift iterations to perform.
        /// </summary> 
        private const int kNumZeroShiftIterations = 20;
        
        /// <summary>
        /// If the fixed shift iterations fail to converge, we restart this many times
        /// before considering the solve attempt as a failure.
        /// </summary>
        private const int kMaxFixedShiftRestarts = 20;

        /// <summary>
        /// The number of fixed shift iterations is computed as
        /// # roots found * this multiplier.
        /// </summary>
        private const int kFixedShiftIterationMultiplier = 20;

        private bool mAttemptedLinearShift;
        private bool mAttemptedQuadraticShift;

        /// <summary>
        /// The maximum number of linear shift iterations to perform before considering
        /// the shift as a failure.
        /// </summary>
        private const int kMaxLinearShiftIterations = 20;

        /// <summary>
        /// The maximum number of quadratic shift iterations to perform before
        /// considering the shift as a failure.
        /// </summary>
        private const int kMaxQuadraticShiftIterations = 20;

        /// <summary>
        /// During quadratic iterations, the real values of the root pairs should be
        /// nearly equal since the root pairs are complex conjugates. This tolerance
        /// measures how much the real values may diverge before consider the quadratic
        /// shift to be failed.
        /// </summary>
        private const double kRootPairTolerance = 0.01;

        /// <summary>
        /// When quadratic shift iterations are stalling, we attempt a few fixed shift
        ///  iterations to help convergence.
        /// </summary>
        private const int kInnerFixedShiftIterations = 5;

        public void PrintRoots() {
            for (int i=0; i < mRoots.Count; ++i) {
                Console.WriteLine("  p={0}", mRoots[i]);
            }
            Console.WriteLine("Total {0} roots", mRoots.Count);
        }

        public bool FindRoots(RealPolynomial p) {
            mRoots.Clear();

            // 最大次数の項を1にする。
            mPoly = p.Normalize();

            RemoveZeroRoots();

            int degree = mPoly.Degree;
            if (degree == 0) {
                return true;
            }

            // Choose the initial starting value for the root-finding on the complex plane.
            double phi = 49.0 * kDegToRad;

            // Iterate until the polynomial has been completely deflated.
            for (int i = 0; i < degree; i++) {
                // Solve in closed form if the polynomial is small enough.
                if (mPoly.Degree <= 2) {
                    break;
                }

                // Compute the root radius.
                double root_radius = ComputeRootRadius();

                // Stage 1: Apply zero-shifts to the K-polynomial to separate the small zeros of the polynomial.
                ApplyZeroShiftToKPolynomial(kNumZeroShiftIterations);

                // Stage 2: Apply fixed shift iterations to the K-polynomial to separate the roots further.
                WWComplex root = WWComplex.Zero();
                ConvergenceType convergence = ConvergenceType.NO;
                for (int j = 0; j < kMaxFixedShiftRestarts; j++) {
                    root = new WWComplex(root_radius * Math.Cos(phi), root_radius * Math.Sin(phi));
                    convergence = ApplyFixedShiftToKPolynomial(
                            root, kFixedShiftIterationMultiplier * (i + 1));
                    if (convergence != ConvergenceType.NO) {
                        break;
                    }

                    // Rotate the initial root value on the complex plane and try again.
                    phi += 94.0 * kDegToRad;
                }

                // Stage 3: Find the root(s) with variable shift iterations on the
                // K-polynomial. If this stage was not successful then we return a failure.
                if (!ApplyVariableShiftToKPolynomial(convergence, root)) {
                    return false;
                }
            }

            // 2次以下の多項式の根を計算する。
            return SolveClosedFormPolynomial();
        }

        /// <summary>
        /// 2次以下の多項式の根を計算する。
        /// </summary>
        private bool SolveClosedFormPolynomial() {
            int degree = mPoly.Degree;

            // Linear
            if (degree == 1) {
                AddRootToOutput(new WWComplex(FindLinearPolynomialRoots(mPoly), 0));
                return true;
            }

            // Quadratic
            if (degree == 2) {
                var roots = FindQuadraticPolynomialRoots(mPoly);
                AddRootToOutput(roots[0]);
                AddRootToOutput(roots[1]);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Solves for the root of the equation ax + b = 0.
        /// 1次多項式の根。
        /// </summary>
        private double FindLinearPolynomialRoots(RealPolynomial p) {
            System.Diagnostics.Debug.Assert(p.Degree == 1);
            return -p.C(0) / p.C(1);
        }

        /// <summary>
        /// 2次多項式の根。有限精度計算で精度よく計算するための工夫が施されている。
        /// see this document: http://people.csail.mit.edu/bkph/articles/Quadratics.pdf
        /// </summary>
        private WWComplex[] FindQuadraticPolynomialRoots(RealPolynomial poly) {
            // 2次多項式 quadratic polynomial equation
            System.Diagnostics.Debug.Assert(poly.Degree == 2);

            var result = new WWComplex[2];

            double a = poly.C(2);
            double b = poly.C(1);
            double c = poly.C(0);

            double discriminant = b * b - 4 * a * c;
            if (0 <= discriminant) {
                double x1;
                double x2;

                if (0 <= b) {
                    x1 = 2 * c / (-b - Math.Sqrt(discriminant));
                    x2 = (-b - Math.Sqrt(discriminant)) / 2 / a;
                } else {
                    x1 = (-b + Math.Sqrt(discriminant)) / 2 / a;
                    x2 = 2 * c / (-b + Math.Sqrt(discriminant));
                }

                result[0] = new WWComplex(x1, 0);
                result[1] = new WWComplex(x2, 0);
            } else {
                WWComplex x1;
                WWComplex x2;

                if (0 <= b) {
                    x1 = new WWComplex(-b / 2 / a, -Math.Sqrt(-discriminant) / 2 / a);
                    x2 = WWComplex.Div(new WWComplex(2 * c, 0), new WWComplex(-b, -Math.Sqrt(-discriminant)));
                } else {
                    x1 = WWComplex.Div(new WWComplex(2 * c, 0), new WWComplex(-b, Math.Sqrt(-discriminant)));
                    x2 = new WWComplex(-b / 2 / a, Math.Sqrt(-discriminant) / 2 / a);
                }

                result[0] = x1;
                result[1] = x2;
            }

            return result;
        }

        /// <summary>
        /// Stage 3: Find the root(s) with variable shift iterations on the
        /// K-polynomial. If this stage was not successful then we return a failure.
        /// </summary>
        private bool ApplyVariableShiftToKPolynomial(ConvergenceType conv, WWComplex root) {
            mAttemptedLinearShift = false;
            mAttemptedQuadraticShift = false;

            if (conv == ConvergenceType.LINEAR) {
                return ApplyLinearShiftToKPolynomial(root, kMaxLinearShiftIterations);
            } else if (conv == ConvergenceType.QUADRATIC) {
                return ApplyQuadraticShiftToKPolynomial(root, kMaxQuadraticShiftIterations);
            }
            return false;
        }

        /// <summary>
        /// Generate K-Polynomials with variable-shifts that are linear. The shift is
        /// computed as:
        ///   K_next(z) = 1 / (z - s) * (K(z) - K(s) / P(s) * P(z))
        ///   s_next = s - P(s) / K_next(s)
        /// </summary>
        private bool ApplyLinearShiftToKPolynomial(WWComplex root, int max_iterations) {
            if (mAttemptedLinearShift) {
                return false;
            }

            // Compute an initial guess for the root.
            double realRoot =
                WWComplex.Div(
                    WWComplex.Sub(root, mPoly.Evaluate(root)),
                    mKPoly.Evaluate(root)).real;

            RealPolynomial deflatedPoly = null;
            RealPolynomial deflatedKPoly = null;
            double polyAtRoot = 0;
            double kPolyAtRoot = 0;

            // This container maintains a history of the predicted roots. The convergence
            // of the algorithm is determined by the convergence of the root value.
            var roots = new List<double>();
            roots.Add(realRoot);
            for (int i = 0; i < max_iterations; i++) {
                // Terminate if the root evaluation is within our tolerance. This will
                // return false if we do not have enough samples.
                if (HasRootConverged(roots)) {
                    AddRootToOutput(new WWComplex(roots[1], 0));
                    mPoly = deflatedPoly;
                    return true;
                }

                double prevPolyAtRoot = polyAtRoot;
                {
                    var p = SyntheticDivisionAndEvaluate(mPoly, realRoot);
                    deflatedPoly = p.quotient;
                    polyAtRoot = p.evaluatedValue;
                }

                // If the root is exactly the root then end early. Otherwise, the k
                // polynomial will be filled with inf or nans.
                if (polyAtRoot == 0) {
                    AddRootToOutput(new WWComplex(realRoot, 0));
                    mPoly = deflatedPoly;
                    return true;
                }

                {
                    // Update the K-Polynomial.
                    var kr = SyntheticDivisionAndEvaluate(mKPoly, realRoot);
                    deflatedKPoly = kr.quotient;
                    kPolyAtRoot = kr.evaluatedValue;
                    mKPoly = WWPolynomial.Add(
                        deflatedKPoly,
                        deflatedPoly.Scale(-kPolyAtRoot / polyAtRoot));

                    // 最大次数の項が1になるようにスケールする。
                    mKPoly.Scale(1.0 / mKPoly.C(mKPoly.Degree));
                }

                // Compute the update for the root estimation.
                kPolyAtRoot = mKPoly.Evaluate(realRoot);
                double deltaRoot = polyAtRoot / kPolyAtRoot;
                realRoot -= polyAtRoot / kPolyAtRoot;

                // Save the root so that convergence can be measured. Only the 3 most
                // recently root values are needed.
                roots.Add(realRoot);
                if (roots.Count > 3) {
                    roots.RemoveAt(0);
                }

                // If the linear iterations appear to be stalling then we may have found a
                // double real root of the form (z - p^2). Attempt a quadratic variable
                // shift from the current estimate of the root.
                if (i >= 2 &&
                        Math.Abs(deltaRoot) < 0.001 * Math.Abs(realRoot) &&
                        Math.Abs(prevPolyAtRoot) < Math.Abs(polyAtRoot)) {
                    var new_root = new WWComplex(realRoot, 0);
                    return ApplyQuadraticShiftToKPolynomial(new_root,
                        kMaxQuadraticShiftIterations);
                }
            }

            mAttemptedLinearShift = true;
            return ApplyQuadraticShiftToKPolynomial(root, kMaxQuadraticShiftIterations);
        }

        /// <summary>
        /// Generate K-polynomials with variable-shifts. During variable shifts, the
        /// quadratic shift is computed as:
        ///                | K0(s1)  K0(s2)  z^2 |
        ///                | K1(s1)  K1(s2)    z |
        ///                | K2(s1)  K2(s2)    1 |
        ///    sigma(z) = __________________________
        ///                  | K1(s1)  K2(s1) |
        ///                  | K2(s1)  K2(s2) |
        /// Where K0, K1, and K2 are successive zero-shifts of the K-polynomial.
        ///
        /// The K-polynomial shifts are otherwise exactly the same as Stage 2 after
        /// accounting for a variable-shift sigma.
        /// </summary>
        private bool ApplyQuadraticShiftToKPolynomial(WWComplex root, int max_iterations) {
            // Only proceed if we have not already tried a quadratic shift.
            if (mAttemptedQuadraticShift) {
                return false;
            }

            double kTinyRelativeStep = 0.01;

            // Compute the fixed-shift quadratic:
            // sigma(z) = (x - m - n * i) * (x - m + n * i) = x^2 - 2 * m + m^2 + n^2.
            RealPolynomial sigmaP;
            {
                var sigma = new double[3];
                sigma[2] = 1.0;
                sigma[1] = -2.0 * root.real;
                sigma[0] = root.real * root.real + root.imaginary * root.imaginary;
                sigmaP = new RealPolynomial(sigma);
            }

            // These two containers hold values that we test for convergence such that the
            // zero index is the convergence value from 2 iterations ago, the first
            // index is from one iteration ago, and the second index is the current value.
            RealPolynomial poly_quotient = null;
            RealPolynomial poly_remainder;
            RealPolynomial kPoly_quotient;
            RealPolynomial kPoly_remainder;
            double polyAtRoot = 0;
            double prevPolyAtRoot = 0;
            double prevV = 0;
            bool triedFixedShifts = false;

            double a, b, c, d;

            // These containers maintain a history of the predicted roots. The convergence
            // of the algorithm is determined by the convergence of the root value.
            var roots1 = new List<WWComplex>();
            var roots2 = new List<WWComplex>();
            roots1.Add(root);
            roots2.Add(root.ComplexConjugate());

            for (int i = 0; i < max_iterations; i++) {
                // Terminate if the root evaluation is within our tolerance. This will
                // return false if we do not have enough samples.
                if (HasRootConverged(roots1) && HasRootConverged(roots2)) {
                    AddRootToOutput(roots1[1]);
                    AddRootToOutput(roots2[1]);
                    mPoly = poly_quotient;
                    return true;
                }

                {
                    var r = WWPolynomial.AlgebraicLongDivision(mPoly, sigmaP);
                    poly_quotient = r.quotient;
                    poly_remainder = r.remainder.NumerPolynomial();
                }

                // Compute a and b from the above equations.
                b = poly_remainder.C(1);
                a = poly_remainder.C(0) - b * sigmaP.C(1);
                var roots = FindQuadraticPolynomialRoots(sigmaP);

                // Check that the roots are close. If not, then try a linear shift.
                if (Math.Abs(Math.Abs(roots[0].real) - Math.Abs(roots[1].real)) >
                        kRootPairTolerance * Math.Abs(roots[1].real)) {
                    return ApplyLinearShiftToKPolynomial(root, kMaxLinearShiftIterations);
                }

                // If the iteration is stalling at a root pair then apply a few fixed shift
                // iterations to help convergence.
                polyAtRoot = Math.Abs(a - roots[0].real * b)
                           + Math.Abs(roots[0].imaginary * b);
                double rel_step = Math.Abs((sigmaP.C(0) - prevV) / sigmaP.C(0));
                if (!triedFixedShifts && rel_step < kTinyRelativeStep &&
                    prevPolyAtRoot > polyAtRoot) {
                    triedFixedShifts = true;
                    ApplyFixedShiftToKPolynomial(roots[0], kInnerFixedShiftIterations);
                }

                {
                    // Divide the shifted polynomial by the quadratic polynomial.
                    var r = WWPolynomial.AlgebraicLongDivision(mKPoly, sigmaP);
                    kPoly_quotient = r.quotient;
                    kPoly_remainder = r.remainder.NumerPolynomial();
                }
                d = kPoly_remainder.C(1);
                c = kPoly_remainder.C(0) - d * sigmaP.C(1);

                prevV = sigmaP.C(0);
                sigmaP = ComputeNextSigma(a,b,c,d,sigmaP);

                // Compute K_next using the formula above.
                UpdateKPolynomialWithQuadraticShift(poly_quotient,
                    kPoly_quotient,a,b,c,d,sigmaP);

                // 最大次数の項が1になるように係数をスケールする。
                mKPoly.Scale(1.0 / mKPoly.C(mKPoly.Degree));

                prevPolyAtRoot = polyAtRoot;

                // Save the roots for convergence testing.
                roots1.Add(roots[0]);
                roots2.Add(roots[1]);
                if (roots1.Count > 3) {
                    roots1.RemoveAt(0);
                    roots2.RemoveAt(0);
                }
            }

            mAttemptedQuadraticShift = true;
            return ApplyLinearShiftToKPolynomial(root, kMaxLinearShiftIterations);
        }

        private class DivisionResultOfFirstOrderPoly {
            /// <summary>
            /// 商。
            /// </summary>
            public RealPolynomial quotient;

            /// <summary>
            /// 多項式のx==pのときの値。
            /// </summary>
            public double evaluatedValue;
        };

        /// <summary>
        /// 変数xの多項式polyを(x-p)で割った商の多項式と、x==pのときpolyの取る値を得る。
        /// synthetic divisionについては https://www.khanacademy.org/math/algebra2/arithmetic-with-polynomials/synthetic-division-of-polynomials/v/synthetic-division
        /// </summary>
        private DivisionResultOfFirstOrderPoly
        SyntheticDivisionAndEvaluate(RealPolynomial poly, double p) {
            // poly = x^3+x^2+x+1
            // p    = 1
            //
            // のとき、
            //
            //  x^3+x^2+x+1                   4
            // ───────────── = (x^2+2x+3) + ─────
            //      x-1                      x-1
            //
            // なので
            //
            // x^3+x^2+x+1 = (x^2+2x+3)(x-1) + 4
            //
            // 商の多項式 = x^2+2x+3
            // poly(1) = 4

            // synthetic division

            // 商の多項式はpolyよりも1次だけ少ない。
            var q = new double[poly.Degree];
            q[poly.Degree-1] = poly.C(poly.Degree);
            for (int i = poly.Degree-2; 0 <= i; --i) {
                q[i] = poly.C(i+1) + q[i + 1] * p;
            }

            // remainder = (poly.C(0) + q(0) * p) / (x-p)

            var result = new DivisionResultOfFirstOrderPoly();
            result.quotient = new RealPolynomial(q);
            result.evaluatedValue = poly.C(0) + q[0] * p;
            return result;
        }

        private void AddRootToOutput(WWComplex root) {
            mRoots.Add(root);
        }

        private ConvergenceType ApplyFixedShiftToKPolynomial(
                WWComplex root, int max_iterations) {
            // Compute the fixed-shift quadratic:
            // sigma(z) = (p - m - n * i) * (p - m + n * i) = p^2 - 2 * m + m^2 + n^2.
            RealPolynomial sigmaP;
            {
                var sigma = new double[3];
                sigma[2] = 1.0;
                sigma[1] = -2.0 * root.real;
                sigma[0] = root.real * root.real + root.imaginary * root.imaginary;
                sigmaP = new RealPolynomial(sigma);
            }

            // Compute the quotient and remainder for divinding P by the quadratic
            // divisor. Since this iteration involves a fixed-shift sigma these may be
            // computed once prior to any iterations.
            RealPolynomial polyQuotient, polyRemainder;
            {
                var r = WWPolynomial.AlgebraicLongDivision(mPoly, sigmaP);
                polyQuotient = r.quotient;
                polyRemainder = r.remainder.ToPolynomial();
                System.Diagnostics.Debug.Assert(polyRemainder != null);
            }

            // Compute a and b from the above equations.
            // このa,bは後で使用する。
            double b = polyRemainder.C(1);
            double a = polyRemainder.C(0) - b * sigmaP.C(0);

            // Precompute P(s) for later using the equation above.
            WWComplex pAtRoot = WWComplex.Sub(new WWComplex(a, 0), root.ComplexConjugate().Scale(b));

            // These two containers hold values that we test for convergence such that the
            // zero index is the convergence value from 2 iterations ago, the first
            // index is from one iteration ago, and the second index is the current value.
            var tλ = new WWComplex[3];
            for (int i=0; i<3; ++i) {
                tλ[i] = WWComplex.Zero();
            }

            var sigmaλ = new double[3];

            RealPolynomial kPolyQuotient, kPolyRemainder;
            for (int i = 0; i < max_iterations; i++) {
                mKPoly = mKPoly.Scale(1.0 / mKPoly.C(mKPoly.Degree));

                // Divide the shifted polynomial by the quadratic polynomial.
                {
                    var r = WWPolynomial.AlgebraicLongDivision(mKPoly, sigmaP);
                    kPolyQuotient = r.quotient;
                    kPolyRemainder = r.remainder.ToPolynomial();
                    System.Diagnostics.Debug.Assert(kPolyRemainder != null);
                }
                double d = kPolyRemainder.C(1);
                double c = kPolyRemainder.C(1) - d * sigmaP.C(0);

                // Test for convergence.
                var variableShiftSigma = ComputeNextSigma(a,b,c,d,sigmaP);
                var kAtRoot = WWComplex.Sub(new WWComplex(c,0), root.ComplexConjugate().Scale(d));

                // t_lambdaの2次の項←t_lambdaの1次の項
                // t_lambdaの1次の項←t_lambdaの定数項
                tλ[2] = tλ[1];
                tλ[1] = tλ[0];
                tλ[0] = WWComplex.Sub(root, WWComplex.Div(pAtRoot, kAtRoot).Minus());

                sigmaλ[2] = sigmaλ[1];
                sigmaλ[1] = sigmaλ[0];
                sigmaλ[0] = variableShiftSigma.C(0);

                // Return with the convergence code if the sequence has converged.
                if (HasConverged(sigmaλ)) {
                    return ConvergenceType.QUADRATIC;
                } else if (HasConverged(tλ)) {
                    return ConvergenceType.LINEAR;
                }

                // Compute K_next using the formula above.
                UpdateKPolynomialWithQuadraticShift(polyQuotient, kPolyQuotient,a,b,c,d,sigmaP);
            }

            return ConvergenceType.NO;
        }

        /// <summary>
        /// Determines if the root has converged by measuring the relative and absolute
        /// change in the root value. This stopping criterion is a simple measurement
        /// that proves to work well. It is referred to as "Ward's method" in the
        /// following reference:
        ///
        /// Nikolajsen, Jorgen L. "New stopping criteria for iterative root finding."
        /// Royal Society open science (2014)
        /// 
        /// 実数バージョン。
        /// </summary>
        private bool HasRootConverged(List<double> roots) {
            const double kRootMagnitudeTolerance = 1e-8;
            const double kAbsoluteTolerance = 1e-14;
            const double kRelativeTolerance = 1e-10;

            if (roots.Count != 3) {
                return false;
            }

            double e_i = Math.Abs(roots[2] - roots[1]);
            double e_i_minus_1 = Math.Abs(roots[1] - roots[0]);
            double mag_root = Math.Abs(roots[1]);
            if (e_i <= e_i_minus_1) {
                if (mag_root < kRootMagnitudeTolerance) {
                    return e_i < kAbsoluteTolerance;
                } else {
                    return e_i / mag_root <= kRelativeTolerance;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the root has converged by measuring the relative and absolute
        /// change in the root value. This stopping criterion is a simple measurement
        /// that proves to work well. It is referred to as "Ward's method" in the
        /// following reference:
        ///
        /// Nikolajsen, Jorgen L. "New stopping criteria for iterative root finding."
        /// Royal Society open science (2014)
        /// 
        /// 複素数バージョン。
        /// </summary>
        private bool HasRootConverged(List<WWComplex> roots) {
            const double kRootMagnitudeTolerance = 1e-8;
            const double kAbsoluteTolerance = 1e-14;
            const double kRelativeTolerance = 1e-10;

            if (roots.Count != 3) {
                return false;
            }

            double e_i = WWComplex.Sub(roots[2], roots[1]).Magnitude();
            double e_i_minus_1 = WWComplex.Sub(roots[1], roots[0]).Magnitude();
            double mag_root = roots[1].Magnitude();
            if (e_i <= e_i_minus_1) {
                if (mag_root < kRootMagnitudeTolerance) {
                    return e_i < kAbsoluteTolerance;
                } else {
                    return e_i / mag_root <= kRelativeTolerance;
                }
            }

            return false;
        }

        /// <summary>
        /// The iterations are computed with the following equation:
        ///              a^2 + u * a * b + v * b^2
        ///   K_next =  ___________________________ * Q_K
        ///                    b * c - a * d
        ///
        ///                      a * c + u * a * d + v * b * d
        ///             +  (z - _______________________________) * Q_P + b.
        ///                              b * c - a * d
        ///
        /// This is done using *only* realy arithmetic so it can be done very fast!
        /// 
        /// mKPolyを更新する。
        /// </summary>
        private void UpdateKPolynomialWithQuadraticShift(
                RealPolynomial polyQuotient, RealPolynomial kPolyQuotient,
                double a, double b, double c, double d, RealPolynomial sigmaP) {
            double coefficient_q_k = (a * a + sigmaP.C(1) * a * b + sigmaP.C(0) * b * b) / (b * c - a * d);

            // linearPoly[0]: 定数項、linear_poly[1]:1次の項。
            var linearPoly = new double[2];
            linearPoly[1] = 1.0;
            linearPoly[0] = -(a * c + sigmaP.C(1) * a * d + sigmaP.C(0) * b * d) / (b * c - a * d);

            mKPoly = WWPolynomial.Add(
                kPolyQuotient.Scale(coefficient_q_k),
                WWPolynomial.Mul(new RealPolynomial(linearPoly), polyQuotient)).AddConstant(b);
        }

        ///<summary>
        /// Determines whether the iteration has converged by examining the three most
        /// recent values for convergence.
        /// </summary>
        private static bool HasConverged(double [] sequence) {
            bool convergence_condition_1 = Math.Abs(sequence[1] - sequence[2]) < Math.Abs(sequence[2]) / 2.0;
            bool convergence_condition_2 = Math.Abs(sequence[0] - sequence[1]) < Math.Abs(sequence[1]) / 2.0;
            return convergence_condition_1 && convergence_condition_2;
        }

        private static bool HasConverged(WWComplex [] sequence) {
            bool convergence_condition_1 = WWComplex.Sub(sequence[1], sequence[2]).Magnitude() < sequence[2].Magnitude() / 2.0;
            bool convergence_condition_2 = WWComplex.Sub(sequence[0], sequence[1]).Magnitude() < sequence[1].Magnitude() / 2.0;
            return convergence_condition_1 && convergence_condition_2;
        }

        ///<summary>
        /// Using a bit of algebra, the update of sigma(z) can be computed from the
        /// previous value along with a, b, c, and d defined above. The details of this
        /// simplification can be found in "Three Stage Variable-Shift Iterations for the
        /// Solution of Polynomial Equations With a Posteriori Error Bounds for the
        /// Zeros" by M.A. Jenkins, Doctoral Thesis, Stanford Univeristy, 1969.
        ///
        /// NOTE: we assume the leading term of quadratic_sigma is 1.0.
        /// </summary>
        /// <returns>2次式が戻る。</returns>
        private RealPolynomial ComputeNextSigma(double a, double b, double c, double d,
                RealPolynomial sigmaP) {
            double u = sigmaP.C(1);
            double v = sigmaP.C(0);

            double b1 = -mKPoly.C(0) / mPoly.C(0);
            double b2 = -(mKPoly.C(1) + b1 * mPoly.C(1)) / mPoly.C(0);

            double a1 = b* c - a * d;
            double a2 = a * c + u * a * d + v * b * d;
            double c2 = b1 * a2;
            double c3 = b1 * b1 * (a * a + u * a * b + v * b * b);
            double c4 = v * b2 * a1 - c2 - c3;
            double c1 = c * c + u * c * d + v * d * d +
            b1 * (a * c + u * b * c + v * b * d) - c4;
            double delta_u = -(u * (c2 + c3) + v * (b1 * a1 + b2 * a2)) / c1;
            double delta_v = v * c4 / c1;

            // Update u and v in the quadratic sigma.
            var new_quadratic_sigma = new double[3];
            new_quadratic_sigma[2] = 1.0;
            new_quadratic_sigma[1] = u + delta_u;
            new_quadratic_sigma[0] = v + delta_v;
            return new RealPolynomial(new_quadratic_sigma);
        }

        /// <summary>
        /// Stage 1: Generate K-polynomials with no shifts (i.e. zero-shifts).
        /// 
        /// mKPolyが作成され、更新される。
        /// </summary>
        private void ApplyZeroShiftToKPolynomial(int numIterations) {
            // K0 is the first degree derivative of polynomial.
            var deriv = mPoly.Derivative();
            mKPoly = deriv.Scale(1.0 / ( mPoly.Degree + 1 ));

            for (int i=1; i < numIterations; ++i) {
                mKPoly = ComputeZeroShiftKPolynomial(mKPoly);
            }
        }

        /// <summary>
        /// The k polynomial with a zero-shift is
        ///  (K(p) - K(0) / P(0) * P(p)) / p.
        ///
        /// This is equivalent to:
        ///    K(p) - K(0)      K(0)     P(p) - P(0)
        ///    ___________   -  ____  *  ___________
        ///         p           P(0)          p
        ///
        /// Note that removing the constant term and dividing by p is equivalent to
        /// shifting the polynomial to one degree lower in our representation.
        /// 
        /// 要するにこの処理は、0次の項を捨て、1次の項→0次の項、2次の項→1次の項、という要領で項の移動をする。
        /// PについてはさらにKの0次の項÷Pの0次の項を各項に掛ける。
        /// </summary>
        private RealPolynomial ComputeZeroShiftKPolynomial(RealPolynomial k) {
            // Evaluating the polynomial at zero is equivalent to the constant term
            // (i.e. the last coefficient). Note that reverse() is an expression and does
            // not actually reverse the vector elements.

            double scale = -k.C(0) / mPoly.C(0);
            var p = mPoly.Scale(scale);

            return WWPolynomial.Add(k.RightShiftCoeffs(1), p.RightShiftCoeffs(1));
        }

        /// <summary>
        /// Computes a lower bound on the radius of the roots of polynomial by examining the Cauchy sequence:
        ///    z^n + |a_1| * z^{n - 1} + ... + |a_{n-1}| * z - |a_n|
        /// The unique positive zero of this polynomial is an approximate lower bound of the radius of zeros of the original polynomial.
        /// </summary>
        private double ComputeRootRadius() {
            // 係数がすべて正で、定数が負の多項式を作る。
            // この多項式は0 < p で1回だけx軸と交差する。
            RealPolynomial poly;
            {
                var c = new double[mPoly.Degree + 1];
                c[0] = -Math.Abs(mPoly.C(0));
                for (int i=1; i < c.Length; ++i) {
                    c[i] = Math.Abs(mPoly.C(i));
                }
                poly = new RealPolynomial(c);
            }

            // Find the unique positive zero using Newton-Raphson iterations.
            return NewtonsMethod.FindRoot(poly, 1.0, 1e-2, 100);
        }

        private void RemoveZeroRoots() {
            int nZeroRoots = 0; 

            // mPoly[degree]は必ず1.0になっているのでorder-1次まで調べる。
            for (int i=0; i<mPoly.Degree; ++i) {
                if (mPoly.C(i)==0) {
                    ++nZeroRoots;
                } else {
                    break;
                }
            }

            if (nZeroRoots == 0) {
                return;
            }

            // z==0の根がnZeroRoots個あった。
            for (int i=0; i < nZeroRoots; ++i) {
                mRoots.Add(WWComplex.Zero());
            }

            var c = new double[mPoly.Degree + 1 - nZeroRoots];
            for (int i=0; i<c.Length; ++i) {
                c[i] = mPoly.C(i+nZeroRoots);
            }
            mPoly = new RealPolynomial(c);
        }

        public int NumOfRoots() {
            return mRoots.Count;
        }

        public WWComplex Root(int nth) {
            return mRoots[nth];
        }

        public WWComplex [] RootArray() {
            return mRoots.ToArray();
        }

        /// <summary>
        /// 大雑把な比較
        /// </summary>
        /// <returns>大体aとbが同じときtrue</returns>
        private static bool AlmostEquals(WWComplex a, WWComplex b) {
            WWComplex diff = WWComplex.Sub(a, b);
            return diff.Magnitude() < 1e-8;
        }

        private static bool AlmostEquals(WWComplex[] a, WWComplex[] b) {
            if (a.Length != b.Length) {
                return false;
            }

            for (int i = 0; i < a.Length; ++i) {
                if (!AlmostEquals(a[i], b[i])) {
                    Console.WriteLine("Error: values different !");
                    return false;
                }
            }

            return true;
        }

        public static void Test() {
            {
                var rpoly = new JenkinsTraubRpoly();
                rpoly.FindRoots(new RealPolynomial(new double[] { 0, 0, 1 }));
                rpoly.PrintRoots();
                System.Diagnostics.Debug.Assert(AlmostEquals(rpoly.RootArray(),
                    new WWComplex[] { WWComplex.Zero(), WWComplex.Zero()}));
            }

            {
                var rpoly = new JenkinsTraubRpoly();
                rpoly.FindRoots(new RealPolynomial(new double[] { 1, 2, 1 }));
                rpoly.PrintRoots();
                System.Diagnostics.Debug.Assert(AlmostEquals(rpoly.RootArray(),
                    new WWComplex[] { new WWComplex(-1, 0), new WWComplex(-1, 0) }));
            }
        }

    }
}
