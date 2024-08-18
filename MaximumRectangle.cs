namespace Vic3MapCSharp
{
    public class MaximumRectangle
    {
        //find the largest rectangle without holes in the region and return the top left and bottom right corners of the rectangle as a tuple
        //with all 1s in a binary matrix 
        public static (int area, int left, int right) MaxHistogram(int C, int[] row) {
            Stack<int> result = new();
            int max_area = 0, max_left = -1, max_right = -1, i = 0;

            void UpdateMaxArea(int height, int width) {
                int area = height * width;
                if (area > max_area) {
                    max_area = area;
                    max_left = result.Count == 0 ? 0 : result.Peek() + 1;
                    max_right = i - 1;
                }
            }

            while (i < C) {
                if (result.Count == 0 || row[result.Peek()] <= row[i]) {
                    result.Push(i++);
                }
                else {
                    int top = result.Pop();
                    UpdateMaxArea(row[top], result.Count == 0 ? i : i - result.Peek() - 1);
                }
            }

            while (result.Count > 0) {
                int top = result.Pop();
                UpdateMaxArea(row[top], result.Count == 0 ? i : i - result.Peek() - 1);
            }

            return (max_area, max_left, max_right);
        }

        public static (int area, int top, int bottom, int left, int right) MaxRectangle(int R, int C, int[][] A) {
            int top = 0, bottom = 0;
            (int result, int left, int right) = MaxHistogram(C, A[0]);

            for (int i = 1; i < R; i++) {
                for (int j = 0; j < C; j++) {
                    if (A[i][j] == 1) {
                        A[i][j] += A[i - 1][j];
                    }
                }

                (int tmp_result, int tmp_left, int tmp_right) = MaxHistogram(C, A[i]);

                if (tmp_result > result) {
                    result = tmp_result;
                    left = tmp_left;
                    right = tmp_right;
                    bottom = i;
                    top = bottom - result / (right - left + 1) + 1;
                }
            }

            return (result, top, bottom, left, right);
        }

        public static (int max, int x, int y) MaxSize(int[][] arg) {
            int rows = arg.Length;
            int cols = arg[0].Length;
            int[][] result = new int[rows][];
            int max = 0, x = 0, y = 0;

            for (int i = 0; i < rows; i++) {
                result[i] = new int[cols];
                if (arg[i][0] == 1) {
                    result[i][0] = 1;
                    max = 1;
                }
            }

            for (int j = 0; j < cols; j++) {
                result[0][j] = arg[0][j];
                if (arg[0][j] == 1) {
                    max = 1;
                }
            }

            for (int i = 1; i < rows; i++) {
                for (int j = 1; j < cols; j++) {
                    if (arg[i][j] == 1) {
                        result[i][j] = Math.Min(Math.Min(result[i][j - 1], result[i - 1][j]), result[i - 1][j - 1]) + 1;
                        if (result[i][j] > max) {
                            max = result[i][j];
                            x = i;
                            y = j;
                        }
                    }
                }
            }
            return (max, x, y);
        }

        //given a matrix of 1s and 0s fill in all 0s that are fully surrounded by 1s and return the matrix
        public static void ReplaceSurrounded(int[][] mat) {
            int rows = mat.Length;
            int cols = mat[0].Length;

            // Replace all 0s with -1 and mark edge-connected -1s as 0
            for (int i = 0; i < rows; i++) {
                for (int j = 0; j < cols; j++) {
                    if (mat[i][j] == 0) {
                        mat[i][j] = -1;
                    }
                }
            }

            // Call floodFill for all -1 lying on edges
            for (int i = 0; i < rows; i++) {
                if (mat[i][0] == -1) {
                    FloodFill(mat, i, 0, -1, 0);
                }
                if (mat[i][cols - 1] == -1) {
                    FloodFill(mat, i, cols - 1, -1, 0);
                }
            }
            for (int j = 0; j < cols; j++) {
                if (mat[0][j] == -1) {
                    FloodFill(mat, 0, j, -1, 0);
                }
                if (mat[rows - 1][j] == -1) {
                    FloodFill(mat, rows - 1, j, -1, 0);
                }
            }

            // Replace all remaining -1 with 1
            for (int i = 0; i < rows; i++) {
                for (int j = 0; j < cols; j++) {
                    if (mat[i][j] == -1) {
                        mat[i][j] = 1;
                    }
                }
            }
        }

        // Stack-based flood fill algorithm
        public static void FloodFill(int[][] mat, int x, int y, int prevV, int newV) {
            int rows = mat.Length;
            int cols = mat[0].Length;
            Stack<(int, int)> stack = new();
            stack.Push((x, y));

            while (stack.Count > 0) {
                (int x1, int y1) = stack.Pop();
                if (x1 < 0 || x1 >= rows || y1 < 0 || y1 >= cols || mat[x1][y1] != prevV) {
                    continue;
                }
                mat[x1][y1] = newV;
                stack.Push((x1 + 1, y1));
                stack.Push((x1 - 1, y1));
                stack.Push((x1, y1 + 1));
                stack.Push((x1, y1 - 1));
            }
        }

        public static List<(int x, int y, int h, int w)> Center(List<(int, int)> coordList, bool floodFill) {
            int minX = coordList[0].Item1, maxX = coordList[0].Item1;
            int minY = coordList[0].Item2, maxY = coordList[0].Item2;

            foreach (var (x1, y1) in coordList) {
                if (x1 < minX) minX = x1;
                if (x1 > maxX) maxX = x1;
                if (y1 < minY) minY = y1;
                if (y1 > maxY) maxY = y1;
            }

            int rows = maxX - minX + 1, cols = maxY - minY + 1;
            int[][] A = new int[rows][];
            for (int i = 0; i < rows; i++) {
                A[i] = new int[cols];
            }

            foreach (var (x1, y1) in coordList) {
                A[x1 - minX][y1 - minY] = 1;
            }

            if (floodFill) {
                ReplaceSurrounded(A);
            }

            (int maxSizeSquare, int x, int y) = MaxSize(A);
            (int x, int y) squareCenter = (x - maxSizeSquare / 2 + minX, y - maxSizeSquare / 2 + minY);
            (int h, int w) squareSize = (maxSizeSquare, maxSizeSquare);

            var result = new List<(int x, int y, int h, int w)> {
                (squareCenter.x, squareCenter.y, squareSize.w, squareSize.h)
            };

            (_, int top, int bottom, int left, int right) = MaxRectangle(rows, cols, A);
            (int x, int y) center = ((top + bottom) / 2 + minX, (left + right) / 2 + minY);
            (int h, int w) = (bottom - top, right - left);

            if (top < 0 || bottom < 0 || left < 0 || right < 0 ||
                A[top][left] == 0 || A[top][right] == 0 || A[bottom][left] == 0 || A[bottom][right] == 0 ||
                A[center.x - minX][center.y - minY] == 0) {
                return result;
            }

            result.Add((center.x, center.y, w, h));
            return result;
        }
    }
}