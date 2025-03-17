using ShellTimer.WebApi.Models;

namespace ShellTimer.WebApi.Services;

public class DuelManager
{
    private readonly Dictionary<string, DuelState> _duels = new();
    public IReadOnlyDictionary<string, DuelState> Duels => _duels;

    public bool TryCreateDuel(string duelCode, string hostConnectionId, int cubeSize, int inspectionTime,
        string scramble)
    {
        if (_duels.ContainsKey(duelCode))
            return false;

        _duels.Add(duelCode, new DuelState
        {
            HostConnectionId = hostConnectionId,
            Scramble = scramble,
            CubeSize = cubeSize,
            InspectionTime = inspectionTime
        });
        return true;
    }

    public bool TryJoinDuel(string duelCode, string challengerConnectionId)
    {
        if (!TryGetDuel(duelCode, out var duel) || duel.ChallengedConnectionId != null)
            return false;

        duel.ChallengedConnectionId = challengerConnectionId;
        return true;
    }

    public bool SetPlayerReady(string duelCode, string connectionId)
    {
        if (!TryGetDuel(duelCode, out var duel))
            return false;

        if (duel.HostConnectionId == connectionId)
            duel.HostReady = true;
        else if (duel.ChallengedConnectionId == connectionId)
            duel.ChallengerReady = true;
        else
            return false;

        return duel.AreBothPlayersReady;
    }

    public bool TrySetSolveTime(string duelCode, string connectionId, int solveTime)
    {
        if (!TryGetDuel(duelCode, out var duel))
            return false;

        if (duel.HostConnectionId == connectionId)
            duel.HostSolveTime = solveTime;
        else if (duel.ChallengedConnectionId == connectionId)
            duel.ChallengedSolveTime = solveTime;
        else
            return false;

        return true;
    }

    public bool TryRemoveParticipant(string duelCode, string connectionId, out string? otherParticipantId)
    {
        otherParticipantId = null;
        if (!TryGetDuel(duelCode, out var duel))
            return false;

        if (duel.HostConnectionId == connectionId)
        {
            otherParticipantId = duel.ChallengedConnectionId;
            _duels.Remove(duelCode);
            return true;
        }

        if (duel.ChallengedConnectionId == connectionId)
        {
            otherParticipantId = duel.HostConnectionId;
            duel.ChallengedConnectionId = null;
            duel.ChallengedSolveTime = null;
            duel.ChallengerReady = false;
            return true;
        }

        return false;
    }

    public bool TryGetDuelResult(string duelCode, out DuelResult? result)
    {
        result = null;
        if (!TryGetDuel(duelCode, out var duel) || !duel.AreBothSolvesComplete)
            return false;

        var isHostWinner = duel.HostSolveTime! < duel.ChallengedSolveTime!;
        result = new DuelResult(
            duel.HostConnectionId,
            duel.ChallengedConnectionId!,
            isHostWinner,
            isHostWinner ? duel.HostSolveTime!.Value : duel.ChallengedSolveTime!.Value,
            isHostWinner ? duel.ChallengedSolveTime!.Value : duel.HostSolveTime!.Value
        );
        return true;
    }

    public void RemoveDuel(string duelCode)
    {
        _duels.Remove(duelCode);
    }

    public bool TryGetDuel(string duelCode, out DuelState duel)
    {
        return _duels.TryGetValue(duelCode, out duel!);
    }
}

public record DuelResult(
    string HostConnectionId,
    string ChallengerConnectionId,
    bool IsHostWinner,
    int WinnerTime,
    int LoserTime);