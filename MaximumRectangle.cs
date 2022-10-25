public class MaximumRectangle
{
    //find the largest rectangle without holes in the region and return the top left and bottom right corners of the rectangle as a tuple
    //with all 1s in a binary matrix 
    public (int area, int left, int right) maxHist(int C, int[] row) {

        Stack<int> result = new Stack<int>();

        int top_val; // Top of stack
        int left;

        int max_area = 0; // Initialize max area in current
        int max_left = -1;
        int max_right = -1;

        int area = 0; // Initialize area with current top

        int i = 0;

        while (i < C) {
            if (result.Count == 0 || row[result.Peek()] <= row[i])
                result.Push(i++);
            else {
                left = result.Peek();
                top_val = row[left];
                result.Pop();
                area = top_val * i;

                if (result.Count > 0) {
                    left = result.Peek() + 1;
                    area = top_val * (i - left);
                }
                if (area > max_area) {
                    max_area = area;
                    max_left = left;
                    max_right = i - 1;
                }
            }
        }

        while (result.Count > 0) {
            left = result.Peek();
            top_val = row[left];
            result.Pop();
            area = top_val * i;
            if (result.Count > 0) {
                left = result.Peek() + 1;
                area = top_val * (i - left);
            }
            if (area > max_area) {
                max_area = area;
                max_left = left;
                max_right = C - 1;
            }
        }

        return (max_area, max_left, max_right);
    }

    public (int area, int top, int bottem, int left, int right) maxRectangle(int R, int C, int[][] A) {
        int top = 0;
        int bottem = 0;

        (int result, int left, int right) = maxHist(C, A[0]);

        for (int i = 1; i < R; i++) {
            for (int j = 0; j < C; j++)
                if (A[i][j] == 1)
                    A[i][j] += A[i - 1][j];


            (int tmp_result, int tmp_left, int tmp_right) = maxHist(C, A[i]);

            if (tmp_result > result) {
                result = tmp_result;
                left = tmp_left;
                right = tmp_right;
                bottem = i;
                top = bottem - (result / (right - left + 1)) + 1;
            }


        }
        return (result, top, bottem, left, right);
    }

    private int min(int a, int b, int c) {
        int l = Math.Min(a, b);
        return Math.Min(l, c);
    }

    public (int max, int x, int y) maxSize(int[][] arg) {
        int[][] result = new int[arg.Length][];
        int max = 0;
        int x = 0;
        int y = 0;

        for (int i = 0; i < arg.Length; i++) {
            result[i] = new int[arg[i].Length];
            if (result[i][0] == 1)
                max = 1;
        }

        for (int i = 0; i < arg[0].Length; i++) {
            result[0][i] = arg[0][i];
            if (result[0][i] == 1)
                max = 1;
        }

        for (int i = 1; i < arg.Length; i++) {
            for (int j = 1; j < arg[i].Length; j++) {
                if (arg[i][j] == 0)
                    continue;
                int t = min(result[i][j - 1], result[i - 1][j], result[i - 1][j - 1]);
                result[i][j] = t + 1;
                if (result[i][j] > max) {
                    max = result[i][j];
                    x = i;
                    y = j;
                }
            }
        }
        return (max, x, y);
    }

    //givin a matrix of 1s and 0s fill in all 0s that are fully surrounded by 1s and return the matrix
    public void replaceSurrounded(int[][] mat) {
        //replace all 0s with -1
        for (int i = 0; i < mat.Length; i++) {
            for (int j = 0; j < mat[i].Length; j++) {
                if (mat[i][j] == 0) {
                    mat[i][j] = -1;
                }
            }
        }
        //call floodFill for all -1 lying on deges
        for (int i = 0; i < mat.Length; i++) {
            if (mat[i][0] == -1) {
                floodFill(mat, i, 0, -1, 0);
            }
        }
        for (int i = 0; i < mat.Length; i++) {
            if (mat[i][mat[i].Length - 1] == -1) {
                floodFill(mat, i, mat[i].Length - 1, -1, 0);
            }
        }
        for (int i = 0; i < mat[0].Length; i++) {
            if (mat[0][i] == -1) {
                floodFill(mat, 0, i, -1, 0);
            }
        }
        for (int i = 0; i < mat[mat.Length - 1].Length; i++) {
            if (mat[mat.Length - 1][i] == -1) {
                floodFill(mat, mat.Length - 1, i, -1, 0);
            }
        }

        //replace all -1 with 1
        for (int i = 0; i < mat.Length; i++) {
            for (int j = 0; j < mat[i].Length; j++) {
                if (mat[i][j] == -1) {
                    mat[i][j] = 1;
                }
            }
        }
    }

    //stack bassed flood fill algorithm
    public void floodFill(int[][] mat, int x, int y, int prevV, int newV) {
        Stack<(int, int)> stack = new Stack<(int, int)>();
        stack.Push((x, y));
        while (stack.Count > 0) {
            (int x1, int y1) = stack.Pop();
            if (x1 < 0 || x1 >= mat.Length || y1 < 0 || y1 >= mat[x1].Length) {
                continue;
            }
            if (mat[x1][y1] != prevV) {
                continue;
            }
            mat[x1][y1] = newV;
            stack.Push((x1 + 1, y1));
            stack.Push((x1 - 1, y1));
            stack.Push((x1, y1 + 1));
            stack.Push((x1, y1 - 1));
        }
    }

    public ((int, int), (int, int)) center(List<(int, int)> coordList, bool floodFill, bool useMaxSquare = false) {
        (int, int) center = (0, 0);
        (int, int) size = (0, 0);

        int minX = coordList[0].Item1;
        int maxX = coordList[0].Item1;
        int minY = coordList[0].Item2;
        int maxY = coordList[0].Item2;
        for (int i = 1; i < coordList.Count; i++) {
            if (coordList[i].Item1 < minX) {
                minX = coordList[i].Item1;
            }
            if (coordList[i].Item1 > maxX) {
                maxX = coordList[i].Item1;
            }
            if (coordList[i].Item2 < minY) {
                minY = coordList[i].Item2;
            }
            if (coordList[i].Item2 > maxY) {
                maxY = coordList[i].Item2;
            }
        }

        //create rectanglural matrix of size (maxX - minX + 1) x (maxY - minY + 1)
        int[][] A = new int[maxX - minX + 1][];
        for (int i = 0; i < maxX - minX + 1; i++) {
            A[i] = new int[maxY - minY + 1];
        }

        //fill matrix with 0s
        for (int i = 0; i < maxX - minX + 1; i++) {
            for (int j = 0; j < maxY - minY + 1; j++) {
                A[i][j] = 0;
            }
        }

        //fill matrix with 1s where there is a pixel in coordList
        for (int i = 0; i < coordList.Count; i++) {
            A[coordList[i].Item1 - minX][coordList[i].Item2 - minY] = 1;
        }

        //flood fill 
        if (floodFill) {
            replaceSurrounded(A);
        }

        //find largest square
        (int, int) squareCenter = (0, 0);
        (int, int) squareSize = (0, 0);
        
        //find the maxSize in region and return the length of the square and the bottem right corner of the square as xy    ints
        (int maxSizeSquare, int x, int y) = maxSize(A);

        //set center to middle of square givin by maxSize and x,y as ints of bottem right corner
        squareCenter = (x - maxSizeSquare / 2 + minX, y - maxSizeSquare / 2 + minY);
        squareSize = (maxSizeSquare, maxSizeSquare);
            
        if (useMaxSquare) {
            return (squareCenter, squareSize);
        }

        //find the largest rectangle without holes in the region and return the top left and bottom right corners of the rectangle as a tuple
        (int area, int top, int bottem, int left, int right) = maxRectangle(maxX - minX + 1, maxY - minY + 1, A);

        //set center to center middle of (left,top) and (right,bottem) as ints
        center = ((top + bottem) / 2 + minX, (left + right) / 2 + minY);
        size = ((bottem - top), (right - left));

        
        //check if any of the 4 corners of the rectangle are negative
        if (top < 0 || bottem < 0 || left < 0 || right < 0) {
            useMaxSquare = true;
        }
        else {
            //check if rectangle is 20% taller than it is wide
            if (size.Item1 * 1.2 < size.Item2) {
                useMaxSquare = true;
            }

            //check if corrers are not in A
            if (A[top][left] == 0 || A[top][right] == 0 || A[bottem][left] == 0 || A[bottem][right] == 0) {
                useMaxSquare = true;
            }

            //check if center is not in A
            if (A[center.Item1 - minX][center.Item2 - minY] == 0) {
                useMaxSquare = true;
            }
        }

        //fallback to largest square
        if (useMaxSquare) {
            return (squareCenter, squareSize);
        }

        return (center, size);
    }

}