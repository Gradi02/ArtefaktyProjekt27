using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ANTK.Game.StateEngine;
using ANTK.Game.StateEngine.Actions;
using UnityEngine;

public sealed class FightManager : IDisposable
{
    private readonly LivingMixin _player;
    private readonly LivingMixin _enemy;
    private readonly StateEngine _stateEngine;
    private readonly CombatDeck _playerDeck;
    private readonly HexEncounterState _encounter;
    private readonly int _cardsDrawnAtTurnStart;
    private readonly int _programmingCapacity;
    private readonly ActionSnapshotRecorder _actionRecorder = new ActionSnapshotRecorder();
    private readonly List<GameAction> _pendingOutcomeActions = new List<GameAction>();
    private readonly List<ProgrammedAction> _program = new List<ProgrammedAction>();
    private readonly ReadOnlyCollection<ProgrammedAction> _programView;
    private bool _isFightOver;
    private bool _isExecutingCommand;
    private string _pendingSnapshotId;

    public LivingMixin CurrentActor { get; private set; }
    public LivingMixin CurrentOpponent => ReferenceEquals(CurrentActor, _player) ? _enemy : _player;
    public LivingMixin PlayerState => _player;
    public LivingMixin EnemyState => _enemy;
    public CombatDeck PlayerDeck => _playerDeck;
    public HexEncounterState Encounter => _encounter;
    public IReadOnlyList<ProgrammedAction> Program => _programView;
    public int ProgrammingCapacity => _programmingCapacity;
    public bool IsTurnStarted { get; private set; }
    public bool IsFightOver => _isFightOver;
    public bool IsPresentationPending { get; private set; }
    public event EventHandler<FightEndedEventArgs> FightEnded;
    public event EventHandler<GameActionSnapshotEventArgs> ActionSnapshotCreated;

    public FightManager(
        LivingMixin player,
        LivingMixin enemy,
        StateEngine stateEngine,
        CardCollection cardCollection,
        int cardsDrawnAtTurnStart,
        int programmingCapacity,
        HexEncounterState encounter)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }
        if (enemy == null)
        {
            throw new ArgumentNullException(nameof(enemy));
        }
        if (ReferenceEquals(player, enemy))
        {
            throw new ArgumentException("Player and enemy must be different entities.");
        }
        if (stateEngine == null)
        {
            throw new ArgumentNullException(nameof(stateEngine));
        }
        if (cardCollection == null)
        {
            throw new ArgumentNullException(nameof(cardCollection));
        }
        if (cardsDrawnAtTurnStart < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cardsDrawnAtTurnStart));
        }
        if (programmingCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(programmingCapacity));
        }
        if (encounter == null)
        {
            throw new ArgumentNullException(nameof(encounter));
        }
        if (!ReferenceEquals(encounter.Player.Unit, player) ||
            !ReferenceEquals(encounter.Enemy.Unit, enemy))
        {
            throw new ArgumentException("The encounter ships must reference the fight entities.", nameof(encounter));
        }

        _player = player;
        _enemy = enemy;
        _stateEngine = stateEngine;
        _cardsDrawnAtTurnStart = cardsDrawnAtTurnStart;
        _programmingCapacity = programmingCapacity;
        _encounter = encounter;
        _playerDeck = new CombatDeck(cardCollection, Math.Max(1, cardsDrawnAtTurnStart));
        _programView = _program.AsReadOnly();
        CurrentActor = _player;
        _player.onDeath += OnEntityDeath;
        _enemy.onDeath += OnEntityDeath;
        _stateEngine.ActionExecuted += OnActionExecuted;
    }

    public void StartFight()
    {
        if (_isFightOver || IsTurnStarted || IsPresentationPending)
        {
            return;
        }

        ExecuteCommand("fight.start", () =>
        {
            if (!TryFinishFight())
            {
                BeginTurn();
            }
        });
    }

    public bool TryQueueBasicCommand(HexCommandType commandType)
    {
        if (!IsBasicCommand(commandType))
        {
            throw new ArgumentException("Only always-available basic commands can use this method.", nameof(commandType));
        }
        if (!RequirePlayerPlanning())
        {
            return false;
        }
        if (!RequireAvailableSlot())
        {
            return false;
        }

        _program.Add(new ProgrammedAction(commandType, null));
        return true;
    }

    public bool TryQueuePlayerCard(string cardInstanceId)
    {
        if (string.IsNullOrWhiteSpace(cardInstanceId))
        {
            throw new ArgumentException("Card instance ID must not be empty.", nameof(cardInstanceId));
        }
        if (!RequirePlayerPlanning() || !RequireAvailableSlot())
        {
            return false;
        }

        foreach (ProgrammedAction action in _program)
        {
            if (action.CardInstanceId == cardInstanceId)
            {
                Debug.LogWarning($"Card '{cardInstanceId}' is already in the program.");
                return false;
            }
        }

        foreach (CombatCard card in _playerDeck.Hand)
        {
            if (card.Source.InstanceId != cardInstanceId)
            {
                continue;
            }

            HexCommandType commandType;
            if (!TryGetCommand(card.Source.Definition.programAction, out commandType))
            {
                Debug.LogWarning(
                    $"Card '{card.Source.Definition.title}' does not define a supported programmed action.");
                return false;
            }

            _program.Add(new ProgrammedAction(
                commandType,
                cardInstanceId,
                card.Source.Definition.title));
            return true;
        }

        Debug.LogWarning($"Card '{cardInstanceId}' is not in the player's hand.");
        return false;
    }

    public bool RemoveProgramAction(int index)
    {
        if (!RequirePlayerPlanning())
        {
            return false;
        }
        if (index < 0 || index >= _program.Count)
        {
            Debug.LogWarning($"Program slot index '{index}' is invalid.");
            return false;
        }

        _program.RemoveAt(index);
        return true;
    }

    public bool MoveProgramAction(int fromIndex, int toIndex)
    {
        if (!RequirePlayerPlanning())
        {
            return false;
        }
        if (fromIndex < 0 || fromIndex >= _program.Count ||
            toIndex < 0 || toIndex >= _program.Count)
        {
            Debug.LogWarning("A program action cannot be moved outside the current sequence.");
            return false;
        }
        if (fromIndex == toIndex)
        {
            return true;
        }

        ProgrammedAction action = _program[fromIndex];
        _program.RemoveAt(fromIndex);
        _program.Insert(toIndex, action);
        return true;
    }

    public bool ClearProgram()
    {
        if (!RequirePlayerPlanning())
        {
            return false;
        }

        _program.Clear();
        return true;
    }

    public void CommitProgram()
    {
        if (!RequirePlayerPlanning())
        {
            return;
        }

        ProgrammedAction[] committedProgram = _program.ToArray();
        _program.Clear();
        ExecuteCommand("player.program", () =>
        {
            foreach (ProgrammedAction action in committedProgram)
            {
                if (_isFightOver)
                {
                    break;
                }

                CombatCard card = null;
                if (!string.IsNullOrEmpty(action.CardInstanceId))
                {
                    card = FindCardInHand(action.CardInstanceId);
                    if (card == null || !_playerDeck.Discard(card))
                    {
                        throw new InvalidOperationException(
                            $"Programmed card '{action.CardInstanceId}' could not be discarded.");
                    }

                    _stateEngine.RecordExecutedAction(new CardPlayedAction(
                        ((IStateEntity)_player).EntityId,
                        card.Source.InstanceId,
                        card.Source.Definition.Id));
                }

                ResolveProgramAction(action, card);
                TryFinishFight();
            }

            if (_isFightOver)
            {
                return;
            }

            EndCurrentTurn();
            if (_isFightOver)
            {
                return;
            }

            CurrentActor = _enemy;
            BeginTurn();
            if (_isFightOver)
            {
                return;
            }

            PerformEnemyAction();
            if (_isFightOver)
            {
                return;
            }

            EndCurrentTurn();
            if (_isFightOver)
            {
                return;
            }

            CurrentActor = _player;
            BeginTurn();
        });
    }

    public string GetProgramPreview()
    {
        HexCoordinate position = _encounter.Player.Position;
        int facing = _encounter.Player.Facing;
        string targetSummary = "No fire command";

        foreach (ProgrammedAction action in _program)
        {
            if (action.CommandType == HexCommandType.TurnLeft)
            {
                facing = (facing + 5) % 6;
            }
            else if (action.CommandType == HexCommandType.TurnRight)
            {
                facing = (facing + 1) % 6;
            }
            else if (action.CommandType == HexCommandType.MoveForward ||
                     action.CommandType == HexCommandType.MoveBackward ||
                     action.CommandType == HexCommandType.FullSails)
            {
                int moveCount = action.CommandType == HexCommandType.FullSails ? 2 : 1;
                bool backward = action.CommandType == HexCommandType.MoveBackward;
                for (int move = 0; move < moveCount; move++)
                {
                    int direction = backward ? (facing + 3) % 6 : facing;
                    HexCoordinate destination = HexBoard.GetNeighbor(position, direction);
                    if (!CanPreviewMove(destination))
                    {
                        break;
                    }
                    position = destination;
                }
            }
            else if (action.CommandType == HexCommandType.Fire)
            {
                targetSummary = _encounter.TryGetTargetInFront(
                    position,
                    facing,
                    _encounter.Enemy.Position,
                    HexEncounterState.DefaultFireRange,
                    out HexCoordinate target)
                    ? $"Target at {target}"
                    : "No target in facing";
            }
        }

        return $"Slots: {_program.Count}/{_programmingCapacity}   " +
            $"Predicted: {position}, facing {facing}   {targetSummary}";
    }

    public bool AcknowledgePresentationCompleted(string snapshotId)
    {
        if (!IsPresentationPending || snapshotId != _pendingSnapshotId)
        {
            Debug.LogWarning($"Snapshot '{snapshotId}' is not the active presentation.");
            return false;
        }

        _pendingSnapshotId = null;
        IsPresentationPending = false;
        return true;
    }

    public void Dispose()
    {
        _stateEngine.ActionExecuted -= OnActionExecuted;
        _player.onDeath -= OnEntityDeath;
        _enemy.onDeath -= OnEntityDeath;
    }

    private void BeginTurn()
    {
        if (_isFightOver)
        {
            return;
        }

        IsTurnStarted = true;
        _stateEngine.StartTurn(CurrentActor);
        if (TryFinishFight())
        {
            return;
        }

        if (ReferenceEquals(CurrentActor, _player) && _cardsDrawnAtTurnStart > 0)
        {
            IReadOnlyList<CombatCard> drawnCards = _playerDeck.DrawCards(_cardsDrawnAtTurnStart);
            foreach (CombatCard card in drawnCards)
            {
                _stateEngine.RecordExecutedAction(new CardDrawnAction(
                    ((IStateEntity)_player).EntityId,
                    card.Source.InstanceId,
                    card.Source.Definition.Id,
                    card.Source.Definition.title,
                    card.Source.Definition.type.ToString(),
                    0,
                    "draw_pile",
                    "hand",
                    "turn.start"));
            }
        }

        Debug.Log($"Turn started: {CurrentActor.name} ({CurrentActor.health}/{CurrentActor.maxHealth} HP).");
        TryFinishFight();
    }

    private void EndCurrentTurn()
    {
        if (!IsTurnStarted || _isFightOver)
        {
            return;
        }

        _stateEngine.EndTurn(CurrentActor);
        IsTurnStarted = false;
        Debug.Log($"Turn ended: {CurrentActor.name} ({CurrentActor.health}/{CurrentActor.maxHealth} HP).");
        TryFinishFight();
    }

    private void PerformEnemyAction()
    {
        HexShipState enemyShip = _encounter.Enemy;
        HexShipState playerShip = _encounter.Player;
        HexCoordinate from = enemyShip.Position;
        int facing = enemyShip.Facing;
        bool hasTarget = _encounter.TryGetTargetInFront(
            enemyShip,
            playerShip,
            HexEncounterState.DefaultFireRange,
            out HexCoordinate target);

        _stateEngine.RecordExecutedAction(new HexCommandResolvedAction(
            ((IStateEntity)_enemy).EntityId,
            "Fire",
            hasTarget ? "Hit" : "No target in facing",
            from.Q,
            from.R,
            from.Q,
            from.R,
            facing,
            facing));

        if (!hasTarget)
        {
            return;
        }

        float damage = _enemy.AttackPower.CalculateValue();
        _stateEngine.ProcessDamage(_enemy, _player, damage, "enemy.hex-fire");
        Debug.Log(
            $"{_enemy.name} dealt {damage} damage to {_player.name}. " +
            $"HP: {_player.health}/{_player.maxHealth}.");
        TryFinishFight();
    }

    private void ResolveProgramAction(ProgrammedAction action, CombatCard card)
    {
        switch (action.CommandType)
        {
            case HexCommandType.MoveForward:
                ResolveMove(_encounter.Player, backward: false, "Move Forward");
                break;
            case HexCommandType.MoveBackward:
                ResolveMove(_encounter.Player, backward: true, "Move Backward");
                break;
            case HexCommandType.TurnLeft:
                ResolveTurn(_encounter.Player, left: true, "Turn Left");
                break;
            case HexCommandType.TurnRight:
                ResolveTurn(_encounter.Player, left: false, "Turn Right");
                break;
            case HexCommandType.Wait:
                RecordCommand(_encounter.Player, "Wait", "Waited");
                break;
            case HexCommandType.Fire:
                ResolveFire(_encounter.Player, _encounter.Enemy, card);
                break;
            case HexCommandType.FullSails:
                for (int step = 0; step < 2; step++)
                {
                    if (!ResolveMove(_encounter.Player, backward: false, "Full Sails"))
                    {
                        break;
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), "Unsupported programmed command.");
        }
    }

    private bool ResolveMove(HexShipState ship, bool backward, string commandName)
    {
        HexCoordinate from = ship.Position;
        int facing = ship.Facing;
        bool moved = _encounter.TryMove(ship, backward, out _, out _, out string result);
        HexCoordinate to = ship.Position;
        RecordCommand(ship, commandName, result, from, to, facing, ship.Facing);
        return moved;
    }

    private void ResolveTurn(HexShipState ship, bool left, string commandName)
    {
        HexCoordinate position = ship.Position;
        _encounter.Turn(ship, left, out int fromFacing, out int toFacing);
        RecordCommand(
            ship,
            commandName,
            "Turned",
            position,
            position,
            fromFacing,
            toFacing);
    }

    private void ResolveFire(HexShipState attacker, HexShipState target, CombatCard card)
    {
        HexCoordinate position = attacker.Position;
        int facing = attacker.Facing;
        bool hasTarget = _encounter.TryGetTargetInFront(
            attacker,
            target,
            HexEncounterState.DefaultFireRange,
            out HexCoordinate targetPosition);
        RecordCommand(
            attacker,
            card == null ? "Fire" : card.Source.Definition.title,
            hasTarget ? $"Target at {targetPosition}" : "No target in facing",
            position,
            position,
            facing,
            facing);

        if (!hasTarget)
        {
            return;
        }

        if (card != null && card.Source.Definition.behaviour != null)
        {
            card.Source.Definition.behaviour.Perform(
                new CardContext(_stateEngine, attacker.Unit, target.Unit, card));
            return;
        }

        float damage = attacker.Unit.AttackPower.CalculateValue();
        _stateEngine.ProcessDamage(
            (IStateEntity)attacker.Unit,
            (IStateEntity)target.Unit,
            damage,
            "player.hex-fire");
        Debug.Log(
            $"{attacker.Unit.name} dealt {damage} damage to {target.Unit.name}. " +
            $"HP: {target.Unit.health}/{target.Unit.maxHealth}.");
    }

    private void RecordCommand(
        HexShipState ship,
        string commandName,
        string result,
        HexCoordinate from,
        HexCoordinate to,
        int fromFacing,
        int toFacing)
    {
        _stateEngine.RecordExecutedAction(new HexCommandResolvedAction(
            ((IStateEntity)ship.Unit).EntityId,
            commandName,
            result,
            from.Q,
            from.R,
            to.Q,
            to.R,
            fromFacing,
            toFacing));
    }

    private void RecordCommand(HexShipState ship, string commandName, string result)
    {
        RecordCommand(
            ship,
            commandName,
            result,
            ship.Position,
            ship.Position,
            ship.Facing,
            ship.Facing);
    }

    private CombatCard FindCardInHand(string cardInstanceId)
    {
        foreach (CombatCard card in _playerDeck.Hand)
        {
            if (card.Source.InstanceId == cardInstanceId)
            {
                return card;
            }
        }

        return null;
    }

    private bool RequirePlayerPlanning()
    {
        if (_isFightOver || IsPresentationPending ||
            !IsTurnStarted || !ReferenceEquals(CurrentActor, _player))
        {
            Debug.LogWarning("Commands can only be programmed during the player's planning phase.");
            return false;
        }

        return true;
    }

    private bool RequireAvailableSlot()
    {
        if (_program.Count < _programmingCapacity)
        {
            return true;
        }

        Debug.LogWarning("The action program has no available slots.");
        return false;
    }

    private bool CanPreviewMove(HexCoordinate destination)
    {
        return _encounter.Board.IsTraversable(destination) &&
            destination != _encounter.Enemy.Position;
    }

    private static bool IsBasicCommand(HexCommandType commandType)
    {
        return commandType == HexCommandType.MoveForward ||
            commandType == HexCommandType.MoveBackward ||
            commandType == HexCommandType.TurnLeft ||
            commandType == HexCommandType.TurnRight ||
            commandType == HexCommandType.Wait ||
            commandType == HexCommandType.Fire;
    }

    private static bool TryGetCommand(ProgrammedCardAction cardAction, out HexCommandType commandType)
    {
        if (cardAction == ProgrammedCardAction.Fire)
        {
            commandType = HexCommandType.Fire;
            return true;
        }
        if (cardAction == ProgrammedCardAction.FullSails)
        {
            commandType = HexCommandType.FullSails;
            return true;
        }

        commandType = default(HexCommandType);
        return false;
    }

    private void ExecuteCommand(string commandTypeId, Action command)
    {
        if (_isExecutingCommand)
        {
            throw new InvalidOperationException("Fight commands cannot be nested.");
        }

        _isExecutingCommand = true;
        _actionRecorder.BeginCommand(commandTypeId);
        try
        {
            command();
        }
        finally
        {
            _isExecutingCommand = false;
            foreach (GameAction action in _pendingOutcomeActions)
            {
                _stateEngine.RecordExecutedAction(action);
            }
            _pendingOutcomeActions.Clear();

            GameActionSnapshot snapshot = _actionRecorder.CompleteCommand();
            if (snapshot != null)
            {
                _pendingSnapshotId = snapshot.SnapshotId;
                IsPresentationPending = true;
                ActionSnapshotCreated?.Invoke(this, new GameActionSnapshotEventArgs(snapshot));
            }
        }
    }

    private void OnActionExecuted(GameAction action)
    {
        _actionRecorder.Record(action);
    }

    private void OnEntityDeath(object sender, HealthChangeEventArgs eventArgs)
    {
        if (_isFightOver)
        {
            return;
        }

        LivingMixin defeated = sender as LivingMixin;
        if (defeated == null)
        {
            return;
        }

        FinishFight(ReferenceEquals(defeated, _enemy), defeated);
    }

    private bool TryFinishFight()
    {
        if (_isFightOver)
        {
            return true;
        }
        if (_player.health <= 0)
        {
            FinishFight(false, _player);
            return true;
        }
        if (_enemy.health <= 0)
        {
            FinishFight(true, _enemy);
            return true;
        }

        return false;
    }

    private void FinishFight(bool wasWon, LivingMixin defeated)
    {
        if (_isFightOver)
        {
            return;
        }
        if (!_isExecutingCommand)
        {
            ExecuteCommand("fight.entity-defeated", () => FinishFight(wasWon, defeated));
            return;
        }

        _isFightOver = true;
        IsTurnStarted = false;
        IStateEntity defeatedState = defeated;
        if (wasWon)
        {
            _pendingOutcomeActions.Add(new OpponentKilledAction(((IStateEntity)_enemy).EntityId));
            _pendingOutcomeActions.Add(
                new FightWonAction(((IStateEntity)_player).EntityId, defeatedState.EntityId));
        }
        else
        {
            _pendingOutcomeActions.Add(new PlayerDiedAction(((IStateEntity)_player).EntityId));
        }

        FightEnded?.Invoke(this, new FightEndedEventArgs(wasWon, defeated));
    }
}

public sealed class ProgrammedAction
{
    public HexCommandType CommandType { get; }
    public string CardInstanceId { get; }
    public bool IsCard => !string.IsNullOrEmpty(CardInstanceId);
    private readonly string _displayName;

    public ProgrammedAction(
        HexCommandType commandType,
        string cardInstanceId,
        string displayName = null)
    {
        CommandType = commandType;
        CardInstanceId = cardInstanceId;
        _displayName = displayName;
    }

    public string GetDisplayName()
    {
        if (IsCard)
        {
            return _displayName ?? CommandType.ToString();
        }

        switch (CommandType)
        {
            case HexCommandType.MoveForward:
                return "Forward";
            case HexCommandType.MoveBackward:
                return "Backward";
            case HexCommandType.TurnLeft:
                return "Turn Left";
            case HexCommandType.TurnRight:
                return "Turn Right";
            case HexCommandType.Wait:
                return "Wait";
            case HexCommandType.Fire:
                return "Fire";
            default:
                return CommandType.ToString();
        }
    }
}
