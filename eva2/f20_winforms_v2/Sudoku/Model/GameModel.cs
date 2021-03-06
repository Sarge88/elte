﻿using ELTE.Forms.Sudoku.Persistence;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ELTE.Forms.Sudoku.Model
{
    /// <summary>
    /// Játéknehézség felsorolási típusa.
    /// </summary>
    public enum GameDifficulty { Easy, Medium, Hard }

    public enum Direction { Up, Right, Down, Left }

    /// <summary>
    /// Sudoku játék típusa.
    /// </summary>
    public class GameModel
    {

        public int CurrentlySelectedTileX { get; set; } = -1;
        public int CurrentlySelectedTileY { get; set; } = -1;

        public GameGraph EdgeList = new GameGraph();

        #region Difficulty constants

        private const Int32 GameTimeEasy = 3600;
        private const Int32 GameTimeMedium = 1200;
        private const Int32 GameTimeHard = 600;
        private const Int32 GeneratedFieldCountEasy = 6;
        private const Int32 GeneratedFieldCountMedium = 12;
        private const Int32 GeneratedFieldCountHard = 18;

        #endregion

        #region Fields

        private ISudokuDataAccess _dataAccess; // adatelérés
        private GameTable _table; // játéktábla
        private GameDifficulty _gameDifficulty; // nehézség
        private Int32 _gameStepCount; // lépések száma
        private Int32 _gameTime; // játékidő

        #endregion

        #region Properties

        /// <summary>
        /// Lépések számának lekérdezése.
        /// </summary>
        public Int32 GameStepCount { get; set; }

        /// <summary>
        /// Hátramaradt játékidő lekérdezése.
        /// </summary>
        public Int32 GameTime { get { return _gameTime; } }

        /// <summary>
        /// Játéktábla lekérdezése.
        /// </summary>
        public GameTable Table { get { return _table; } }

        /// <summary>
        /// Játék végének lekérdezése.
        /// </summary>
        public Boolean IsGameOver  { get { return (_gameTime == 0 || _table.IsFilled); } }

        /// <summary>
        /// Játéknehézség lekérdezése, vagy beállítása.
        /// </summary>
        public GameDifficulty GameDifficulty { get { return _gameDifficulty; } set { _gameDifficulty = value; } }


        #endregion

        #region Events

        /// <summary>
        /// Játék előrehaladásának eseménye.
        /// </summary>
        public event EventHandler<GameEventArgs> GameAdvanced;

        /// <summary>
        /// Játék végének eseménye.
        /// </summary>
        public event EventHandler<GameEventArgs> GameOver;

        #endregion

        #region Constructor

        /// <summary>
        /// Sudoku játék példányosítása.
        /// </summary>
        /// <param name="dataAccess">Az adatelérés.</param>
        public GameModel(ISudokuDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
            _table = new GameTable();
            _gameDifficulty = GameDifficulty.Medium;
        }

        #endregion

        #region Public game methods

        public System.Collections.Generic.List<Edge> NeighboursForEdge(Edge E)
        {
            return this.EdgeList.FindAll(delegate (Edge CurrentEdge) {
                return CurrentEdge.Nodes.Overlaps(E.Nodes) && CurrentEdge.Nodes != E.Nodes;
            });

        }

        public Edge SetLastDrawnEdge(int currentLine, int currentColumn)
        {
            Edge LastDrawnEdge = null;
            if (CurrentlySelectedTileX != -1 && CurrentlySelectedTileY != -1)
            {
                LastDrawnEdge = new Edge(
                    new Node(currentLine, currentColumn),
                    new Node(CurrentlySelectedTileX, CurrentlySelectedTileY)
                );
                EdgeList.Add(LastDrawnEdge);
            }

            return LastDrawnEdge;
        }
        /// <summary>
        /// Új játék kezdése.
        /// </summary>
        public void NewGame()
        {
            _table = new GameTable();
            _gameStepCount = 0;
            _gameTime = GameTimeMedium;

        }

        /// <summary>
        /// Játékidő léptetése.
        /// </summary>
        public void AdvanceTime()
        {
            if (IsGameOver) // ha már vége, nem folytathatjuk
                return;

            _gameTime--;
            OnGameAdvanced();

            if (_gameTime == 0) // ha lejárt az idő, jelezzük, hogy vége a játéknak
                OnGameOver(false);
        }

        internal void Select(int x, int y)
        {
            CurrentlySelectedTileX = x;
            CurrentlySelectedTileY = y;
            
        }

        internal void ClearSelectedTiles()
        {
            CurrentlySelectedTileX = -1;
            CurrentlySelectedTileY = -1;
        }

        internal bool IsAnyFieldSelected()
        {
            return !(CurrentlySelectedTileX == -1 && CurrentlySelectedTileY == -1);
        }

   

        /// <summary>
        /// Táblabeli lépés végrehajtása.
        /// </summary>
        /// <param name="x">Vízszintes koordináta.</param>
        /// <param name="y">Függőleges koordináta.</param>
        public void Step(Int32 x, Int32 y)
        {
            if (IsGameOver) // ha már vége a játéknak, nem játszhatunk
                return;
            if (_table.IsLocked(x, y)) // ha a mező zárolva van, nem léthatünk
                return;
         

            _gameStepCount++; // lépésszám növelés

            OnGameAdvanced();

            if (_table.IsFilled) // ha vége a játéknak, jelezzük, hogy győztünk
            {
                OnGameOver(true);
            }
        }

        /// <summary>
        /// Játék betöltése.
        /// </summary>
        /// <param name="path">Elérési útvonal.</param>
        public async Task LoadGameAsync(String path)
        {
            if (_dataAccess == null)
                throw new InvalidOperationException("No data access is provided.");

            _table = await _dataAccess.LoadAsync(path);
            _gameStepCount = 0;

            switch (_gameDifficulty) // játékidő beállítása
            {
                case GameDifficulty.Easy:
                    _gameTime = GameTimeEasy;
                    break;
                case GameDifficulty.Medium:
                    _gameTime = GameTimeMedium;
                    break;
                case GameDifficulty.Hard:
                    _gameTime = GameTimeHard;
                    break;
            }
        }

        /// <summary>
        /// Játék mentése.
        /// </summary>
        /// <param name="path">Elérési útvonal.</param>
        public async Task SaveGameAsync(String path)
        {
            if (_dataAccess == null)
                throw new InvalidOperationException("No data access is provided.");

            await _dataAccess.SaveAsync(path, _table);
        }

        #endregion


        public Player GetCurrentPlayer()
        {
            return GameStepCount % 2 == 0 ? Player.Blue : Player.Red;
        }


        #region Private event methods

        /// <summary>
        /// Játékidő változás eseményének kiváltása.
        /// </summary>
        private void OnGameAdvanced()
        {
            if (GameAdvanced != null)
                GameAdvanced(this, new GameEventArgs(false, _gameStepCount, _gameTime));
        }

        /// <summary>
        /// Játék vége eseményének kiváltása.
        /// </summary>
        /// <param name="isWon">Győztünk-e a játékban.</param>
        private void OnGameOver(Boolean isWon)
        {
            if (GameOver != null)
                GameOver(this, new GameEventArgs(isWon, _gameStepCount, _gameTime));
        }

        #endregion
    }
}
