using System.Windows;
using System.Windows.Media.Imaging;

namespace Rounder;

public static class ImageEditing {
    private const int BYTES_PER_PIXEL = 4;
    private const int MinX = 0;
    private const int MinY = 0;
    private const int MinHeight = 0;

    extension(WriteableBitmap img) {
        public int MaxX => img.PixelWidth - 1;
        public int MaxY => img.PixelHeight - 1;

        public void RangeCheckX(int x) {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(img.PixelWidth, x);
        }
        public void RangeCheckY(int y) {
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(img.PixelHeight, y);
        }

        public void OutOfRangeCheck(int x, int y) {
            img.RangeCheckX(x);
            img.RangeCheckY(y);
        }

        public bool IsWithinRange(int x, int y) {
            return x > MinX && y > MinY && x <= img.MaxX && y <= img.MaxY;
        }

        public (int x, int y) Clamp((int x, int y) coords) {
            return img.Clamp(coords.x, coords.y);
        }

        public (int x, int y) Clamp(int x, int y) {
            return (Math.Clamp(x, MinX, img.MaxX),
                    Math.Clamp(y, MinY, img.MaxY));
        }

        /** <exception cref="ArgumentOutOfRangeException">If it does not apply that x is within
         * <c>[0;width - 1]</c>, and y <c>[0;height - 1]</c></exception>
         */
        public void SetPixelHarsh(int x, int y, int value) {
            img.OutOfRangeCheck(x, y);
            img.SetPixelChecked(x, y, value);
        }

        public void SetPixel(int x, int y, int value) {
            x = img.UnderflowX(x);
            y = img.UnderflowY(y);
            img.SetPixelChecked(x, y, value);
        }

        /** Attempts to set the pixel. Will return `false` if an error occurred
         * (e.g. if x or y were outside their respective acceptable ranges of
         * <c>[0;width - 1]</c> and <c>[0;height - 1]</c>.
         */
        public bool TrySetPixel(int x, int y, int value) {
            if (!img.IsWithinRange(x, y)) {
                return false;
            }
            img.SetPixelChecked(x, y, value);
            return true;
        }

        /** Clamps x and y to within the image dimensions before setting the
         * pixel. */
        public void SetPixelClamped(int x, int y, int value) {
            (x, y) = img.Clamp(x, y);
            img.SetPixelChecked(x, y, value);
        }

        private void SetPixelChecked(int x, int y, int value) {
            try {
                img.Lock();
                IntPtr pBackBuffer = img.BackBuffer;
                pBackBuffer += y * img.BackBufferStride;
                pBackBuffer += x * BYTES_PER_PIXEL;
                pBackBuffer.SetValue(value);
                img.AddDirtyRect(new Int32Rect(x, y, 1, 1));
            } finally {
                img.Unlock();
            }
        }

        public void SetLine((int x, int y) from, (int x, int y) to, int value) {
            if (from.x > to.x) {
                (from.x, from.y) = (to.x, to.y);
            }
            from = img.Clamp(from);
            to = img.Clamp(to);
            switch (to.x - from.x, to.y - from.y) {
                case (0,0):
                    img.SetPixel(from.x, from.y, value);
                    return;
                case (0, var diff):
                    img.SetVertical(from.x, from.y, Math.Abs(diff), value);
                    return;
                case (var diff, 0):
                    img.SetHorizontal(from.x, from.y, Math.Abs(diff), value);
                    return;
                default:
                    img.SetCurve(value, from.x, from.y, to.x, to.y);
                    return;
            }

        }

        private int UnderflowX(int x) {
            int maxWidth = img.MaxX;
            x = Math.Clamp(x, 0-maxWidth, maxWidth);
            if (x < MinX) {
                x = img.PixelWidth + x;
            }

            return x;
        }

        private int UnderflowY(int y) {
            int maxHeight = img.MaxY;
            y = Math.Clamp(y, 0-maxHeight, maxHeight);
            if (y < MinY) {
                y = img.PixelHeight + y;
            }

            return y;
        }

        public void SetHorizontal(int x, int y, int length, int value) {
            if (img.PixelWidth == 0 || img.PixelHeight == 0) {
                return;
            }

            x = img.UnderflowX(x);
            y = img.UnderflowY(y);
            if (length < 0) {
                length = 0 - length;
                length = Math.Clamp(length, 0, x);
                x = (x + 1) - length;
            } else {
                length = Math.Clamp(length, 0, img.PixelWidth - x);
                if (length == 0) {
                    img.SetPixelChecked(x, y, value);
                    return;
                }
            }

            img.SetHorizontalChecked(x, y, length, value);
        }

        private void SetHorizontalChecked(int x, int y, int length, int value) {
            try {
                img.Lock();
                IntPtr pBackBuffer = img.BackBuffer;
                pBackBuffer += y * img.BackBufferStride;
                pBackBuffer += x * BYTES_PER_PIXEL;
                for (int target = x + length; x < target; x++) {
                    pBackBuffer.SetValue(value);
                    pBackBuffer += BYTES_PER_PIXEL;
                    img.AddDirtyRect(new Int32Rect(x, y, 1, 1));
                }
            }finally {
                img.Unlock();
            }
        }

        public void SetVertical(int x, int y, int height, int value) {
            if (img.PixelWidth == 0 || img.PixelHeight == 0) {
                return;
            }
            x = img.UnderflowX(x);
            y = img.UnderflowY(y);
            if (height < 0) {
                height = 0 - height;
                height = Math.Clamp(height, 0, y);
                y = (y + 1) - height;
            } else {
                height = Math.Clamp(height, 0, img.PixelHeight - y);
                if (height == 0) {
                    img.SetPixelChecked(x, y, value);
                    return;
                }
            }

            img.SetVerticalChecked(x, y, height, value);
        }

        private void SetVerticalChecked(int x, int y, int height, int value) {
            try {
                img.Lock();
                IntPtr pBackBuffer = img.BackBuffer;
                pBackBuffer += y * img.BackBufferStride;
                pBackBuffer += x * BYTES_PER_PIXEL;
                for (int target = y + height; y < target; y++) {
                    pBackBuffer.SetValue(value);
                    img.AddDirtyRect(new Int32Rect(x, y, 1, 1));
                    pBackBuffer += img.BackBufferStride;
                }
            } finally {
                img.Unlock();
            }
        }

        public void SetRow(int y, int value) {
            img.RangeCheckY(y);
            img.SetHorizontal(0, y, img.PixelWidth, value);
        }

        public void SetColumn(int x, int value) {
            img.RangeCheckX(x);
            img.SetVertical(x, 0, img.PixelHeight, value);
        }

        public void SetBorder(int value) {
            ArgumentOutOfRangeException.ThrowIfLessThan(img.PixelWidth, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(img.PixelHeight, 1);

            img.SetRow(0, value);
            img.SetRow(img.MaxY, value);
            img.SetColumn(0, value);
            img.SetColumn(img.MaxX, value);
        }

        public void SetBorder(int value, int thickness) {
            ArgumentOutOfRangeException.ThrowIfLessThan(thickness, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(thickness, img.PixelWidth);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(thickness, img.PixelHeight);

            for (int i = 0; i < thickness; i++) {
                img.SetRow(i, value);
            }

            for (int i = 0; i < thickness; i++) {
                img.SetRow(img.PixelHeight - (i + 1), value);
            }

            for (int i = 0; i < thickness; i++) {
                img.SetColumn(i, value);
            }

            for (int i = 0; i < thickness; i++) {
                img.SetColumn(img.PixelWidth - (i + 1), value);
            }
        }

        /** Draws a linear curve as per the equation y = ax + b. */
        public void SetCurve(int value, int a, int b) {
            img.SetCurve(value, 0, b, img.MaxX, img.MaxY, a, b);
        }

        /** Draws a linear curve as per the equation y = ax + b. */
        public void SetCurve(int value,
                             int startX, int startY,
                             int endX, int endY,
                             int a = 1, int b = 1) {
            if (a == 0) {
                img.SetHorizontal(startX, startY + b, Math.Abs(endX - startX), value);
                return;
            }
            startX = Math.Clamp(startX, MinX, img.MaxX);
            startY = Math.Clamp(startY, MinY, img.MaxY);
            endX = Math.Clamp(endX, MinX, img.MaxX);
            endY = Math.Clamp(endY, MinY, img.MaxY);

            int x = startX;
            int y = startY;
            while (0 <= x && x <= endX && 0 <= y && y <= endY) {
                img.SetPixel(x, y, value);
                x++;
                y = a * x + b;
            }
        }

        /** Draws a linear curve as per the equation y = ax + b. */
        public void SetCurve(int value, float a, float b) {
            for (int x = 0; x < img.PixelWidth; x++) {
                img.SetPixel(x, (int)Math.Round(a * x + b), value);
            }
        }

        /** Draws a quadratic curve as per the equation y = ax^2 + bx + c. */
        public void SetCurve(int value, int a, int b, int c) {
            for (int x = 0; x < img.PixelWidth; x++) {
                img.SetPixel(x, (int)Math.Pow(a * x, 2) + b * x + c, value);
            }
        }

        /** Draws a quadratic curve as per the equation y = ax^2 + bx + c. */
        public void SetCurve(int value, float a, float b, float c) {
            for (int x = 0; x < img.PixelWidth; x++) {
                img.SetPixel(x, (int)Math.Round(Math.Pow(a * x, 2) + b * x + c), value);
            }
        }

        /** Draws a polynomial curve as per the equation y = (v_n * x)^(n) + ... + (v_2 * x)^2 + v_1 * x ... + v_0. */
        public void SetCurve(int value, params float[] coefficients) {
            for (int x = 0; x < img.PixelWidth; x++) {
                double sum = 0;
                for (int n = coefficients.Length - 1; n >= 0; n--) {
                    sum += Math.Pow(coefficients[n] * x, n);
                }

                img.SetPixel(x, (int)Math.Round(sum), value);
            }
        }

        /** Draws a polynomial curve as per the equation y = (v_n * x)^(n) + ... + (v_2 * x)^2 + v_1 * x ... + v_0. */
        public void SetCurve(int value, params int[] coefficients) {
            for (int x = 0; x < img.PixelWidth; x++) {
                double sum = 0;
                for (int n = coefficients.Length - 1; n >= 0; n--) {
                    sum += Math.Pow(coefficients[n] * x, n);
                }

                img.SetPixel(x, (int)Math.Round(sum), value);
            }
        }

        public void SetQuarterCircle(int value, int radius) {
            for (int x = 0; x < img.PixelWidth && x < radius * 2; x++) {

            }
        }

        public void EraseRow(int row) {
            SetRow(img, row, Colour.Transparent);
        }

        public void EraseColumn(int column) {
            SetColumn(img, column, Colour.Transparent);
        }

        public void ErasePixel(int row, int column) {
            SetPixel(img, row, column, Colour.Transparent);
        }

        public void EraseBorder() {
            img.SetBorder(Colour.Transparent);
        }

        public void EraseBorder(int thickness) {
            img.SetBorder(Colour.Transparent, thickness);
        }
    }

    // Convenience functions

    // Internals
    private static unsafe void SetValue(this IntPtr buffer, int value) {
        *((int*)buffer) = value;
    }
}