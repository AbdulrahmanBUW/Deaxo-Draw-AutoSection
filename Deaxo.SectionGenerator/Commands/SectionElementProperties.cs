using Autodesk.Revit.DB;
using System;

namespace Deaxo.SectionGenerator.Commands
{
    public static class SectionGeometryHelpers
    {
        /// <summary>
        /// Rotate a vector around the Z-axis by the specified rotation in radians.
        /// Mirrors the Python rotate_vector function.
        /// </summary>
        /// <param name="vector">The vector to rotate</param>
        /// <param name="rotationRadians">Rotation angle in radians</param>
        /// <returns>Rotated vector</returns>
        public static XYZ RotateVector(XYZ vector, double rotationRadians)
        {
            double x = vector.X;
            double y = vector.Y;
            double z = vector.Z;

            double rotatedX = x * Math.Cos(rotationRadians) - y * Math.Sin(rotationRadians);
            double rotatedY = x * Math.Sin(rotationRadians) + y * Math.Cos(rotationRadians);

            return new XYZ(rotatedX, rotatedY, z);
        }
    }
}