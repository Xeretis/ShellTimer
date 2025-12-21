namespace ShellTimer.Shared.Support.Cube;

// Based on the official WCA approved scramble generator
public static class ScrambleGenerator
{
    private static readonly Random Random = new();

    private static readonly char[] Faces = ['U', 'D', 'R', 'L', 'F', 'B'];
    private static readonly string[] TurnSuffixes = ["", "'", "2"];

    public static string GenerateScramble(int size, int? length = null)
    {
        if (length == null)
            length = size switch
            {
                2 => 9,
                3 => 20,
                4 => 40,
                5 => 60,
                6 => 80,
                7 => 100,
                _ => size * 20
            }; // WCA standard for different cube sizes

        if (size < 2)
            throw new ArgumentException("Cube size must be at least 2", nameof(size));

        if (length < 0)
            throw new ArgumentException("Scramble length must be non-negative", nameof(length));

        if (size == 2)
            return GenerateScrambleForSize2((int)length);

        var scramble = new List<string>();
        var moves = new[] {"U", "D", "R", "L", "F", "B"};
        for (int i = 0; i < length; i++)
        {
            string randomMove = moves[Random.Next(moves.Length)];
            while ((scramble.Count >= 1 && scramble[i - 1] == randomMove) ||
                   (scramble.Count >= 2 && scramble[i - 2] == randomMove))
            {
                randomMove = moves[Random.Next(moves.Length)];
            }
            scramble.Add(randomMove);
        }
        for (int j = 0; j < scramble.Count; j++)
        {
            var suffixRandom = Random.Next(1, 3); // 1: "", 2: "'", 3: "2"
            if (suffixRandom == 1)
            {
                scramble[j] += "";
            }else if (suffixRandom == 2)
            {
                scramble[j] += "'";
            }else if (suffixRandom == 3)
            {
                scramble[j] += "2";
            }
        }
        return string.Join(" ", scramble);
    }

    private static string GenerateScrambleForSize2(int length)
    {
        var scrambleMoves = new List<string>();
        var lastAxis = -1;

        for (var i = 0; i < length; i++)
        {
            int axis;

            do
            {
                axis = Random.Next(3); // 0: UD, 1: RL, 2: FB
            } while (axis == lastAxis);

            lastAxis = axis;

            var faceIdx = axis * 2 + Random.Next(2);
            var faceChar = Faces[faceIdx];
            var turnSuffix = TurnSuffixes[Random.Next(TurnSuffixes.Length)];

            var move = $"{faceChar}{turnSuffix}";

            scrambleMoves.Add(move);
        }

        return string.Join(" ", scrambleMoves);
    }
}