using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class BlockPuzzleGame : MonoBehaviour
{
    [Header("Configuration")]
    public GameSettings settings;
    public ShapeData[] shapes;

    [Header("Sprites")]
    public Sprite backgroundSprite;
    public Sprite cellSprite;
    public Sprite pauseSprite;
    public Sprite rotationSprite;
    public Sprite scoreContainerSprite;

    [Header("Audio")]
    public AudioClip backgroundMusic;
    public AudioClip takeSound;
    public AudioClip placeSound;
    public AudioClip rotateSound;
    public AudioClip clearSound;

    public float TrayCellSize => Mathf.Min(32f, cellSize);

    private readonly Color emptyColor =
        new Color(0.19f, 0.18f, 0.21f);

    private readonly Color validColor =
        new Color(0.35f, 0.75f, 0.5f);

    private readonly Color invalidColor =
        new Color(0.85f, 0.3f, 0.35f);

    private readonly PieceView[] pieces = new PieceView[3];
    private readonly Button[] rotateButtons = new Button[3];
    private readonly List<ShapeData> usableShapes =
        new List<ShapeData>();

    private RectTransform canvasRect;
    private RectTransform content;
    private RectTransform board;
    private GameObject modal;
    private Text scoreText;
    private Text bestText;
    private Text modalTitle;
    private Button resumeButton;
    private Font font;

    private AudioSource music;
    private AudioSource effects;

    private bool[,] occupied;
    private Image[,] boardImages;

    private int width;
    private int height;
    private int score;
    private float cellSize;

    private bool paused;
    private bool gameOver;
    private bool busy;

    private PieceView dragged;
    private Vector2 lastPointer;
    private Sequence clearSequence;

    private void Start()
    {
        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
        }

        font = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf");

        DOTween.Init();

        CreateAudio();
        CreateInterface();
        StartNewGame();

        Debug.Log("High-score file: " + ScoreStore.FilePath);
    }

    private bool ValidateConfiguration()
    {
        if (settings == null)
        {
            Debug.LogError(
                "Assign Game Settings to BlockPuzzleGame.");
            return false;
        }

        width = Mathf.Max(1, settings.width);
        height = Mathf.Max(1, settings.height);

        usableShapes.Clear();

        if (shapes != null)
        {
            foreach (ShapeData shape in shapes)
            {
                if (shape != null && shape.GetCells().Count > 0)
                    usableShapes.Add(shape);
            }
        }

        if (usableShapes.Count == 0)
        {
            Debug.LogError(
                "Assign at least one non-empty Shape asset.");
            return false;
        }

        cellSize = Mathf.Min(
            60f,
            480f / Mathf.Max(width, height));

        return true;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame && !gameOver)
            TogglePause();

        if (!paused && !gameOver && !busy &&
            dragged != null &&
            keyboard.rKey.wasPressedThisFrame)
        {
            dragged.Rotate();
            PlaySound(rotateSound);
            MoveDrag(dragged, lastPointer);
        }
    }

    private void CreateAudio()
    {
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;
        music.spatialBlend = 0f;
        music.volume = settings.musicVolume;
        music.clip = backgroundMusic;

        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        effects.spatialBlend = 0f;

        if (backgroundMusic != null)
            music.Play();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
            effects.PlayOneShot(clip);
    }

    private void FadeMusic(float volume)
    {
        music.DOKill();
        music.DOFade(volume, 0.25f).SetUpdate(true);
    }

    private void CreateInterface()
    {
        var canvasObject = new GameObject(
            "GameCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        canvasRect = canvasObject.GetComponent<RectTransform>();

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1280f, 900f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.Expand;

        if (EventSystem.current == null)
        {
            var eventObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));

            eventObject.GetComponent<InputSystemUIInputModule>()
                .AssignDefaultActions();
        }
        else
        {
            EventSystem existing = EventSystem.current;
            
            foreach (BaseInputModule module in
                existing.GetComponents<BaseInputModule>())
            {
                if (!(module is InputSystemUIInputModule))
                    module.enabled = false;
            }

            InputSystemUIInputModule inputModule =
                existing.GetComponent<InputSystemUIInputModule>();

            if (inputModule == null)
            {
                inputModule =
                    existing.gameObject
                        .AddComponent<InputSystemUIInputModule>();
            }

            inputModule.enabled = true;
            inputModule.AssignDefaultActions();
        }

        Image background = MakeImage(
            "Background",
            canvasRect,
            Vector2.zero,
            Vector2.zero,
            backgroundSprite,
            backgroundSprite != null
                ? Color.white
                : new Color(0.06f, 0.09f, 0.12f));

        Stretch(background.rectTransform);

        content = MakeRect(
            "Content",
            canvasRect,
            Vector2.zero,
            new Vector2(1280f, 900f));

        MakeImage(
            "ScoreContainer",
            content,
            new Vector2(0f, 385f),
            new Vector2(340f, 52f),
            scoreContainerSprite,
            scoreContainerSprite != null
                ? Color.white
                : new Color(0.14f, 0.12f, 0.16f));

        scoreText = MakeText(
            "Score",
            content,
            new Vector2(0f, 385f),
            new Vector2(320f, 48f),
            "",
            28);

        MakeButton(
            "Pause",
            content,
            new Vector2(-540f, 385f),
            new Vector2(64f, 64f),
            pauseSprite,
            pauseSprite == null ? "II" : "",
            TogglePause);

        bestText = MakeText(
            "HighScores",
            content,
            new Vector2(450f, 160f),
            new Vector2(280f, 220f),
            "",
            25);

        MakeText(
            "Instructions",
            content,
            new Vector2(0f, -440f),
            new Vector2(1180f, 20f),
            "Drag to place | R while dragging: rotate | Esc: pause",
            17);

        Vector2 boardSize = new Vector2(
            width * cellSize,
            height * cellSize);

        MakeImage(
            "BoardBorder",
            content,
            new Vector2(0f, 60f),
            boardSize + Vector2.one * 10f,
            null,
            new Color(0.28f, 0.3f, 0.34f));

        board = MakeRect(
            "Board",
            content,
            new Vector2(0f, 60f),
            boardSize);

        occupied = new bool[width, height];
        boardImages = new Image[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Image image = MakeImage(
                    "Cell_" + x + "_" + y,
                    board,
                    Vector2.zero,
                    Vector2.one * Mathf.Max(1f, cellSize - 3f),
                    null,
                    emptyColor);

                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);

                rect.anchoredPosition = new Vector2(
                    x * cellSize + 1.5f,
                    -y * cellSize - 1.5f);

                boardImages[x, y] = image;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int slot = i;

            rotateButtons[i] = MakeButton(
                "Rotate_" + i,
                content,
                new Vector2((i - 1) * 360f, -397f),
                new Vector2(44f, 44f),
                rotationSprite,
                rotationSprite == null ? "R" : "",
                () => RotateSlot(slot));
        }

        CreateModal();
    }

    private void CreateModal()
    {
        Image shade = MakeImage(
            "Modal",
            canvasRect,
            Vector2.zero,
            Vector2.zero,
            null,
            new Color(0f, 0f, 0f, 0.8f));

        Stretch(shade.rectTransform);
        shade.raycastTarget = true;
        modal = shade.gameObject;

        Image panel = MakeImage(
            "Panel",
            shade.rectTransform,
            Vector2.zero,
            new Vector2(560f, 340f),
            null,
            new Color(0.13f, 0.15f, 0.2f));

        modalTitle = MakeText(
            "Title",
            panel.rectTransform,
            new Vector2(0f, 100f),
            new Vector2(520f, 90f),
            "PAUSED",
            32);

        resumeButton = MakeButton(
            "Resume",
            panel.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(260f, 52f),
            null,
            "Resume",
            TogglePause);

        MakeButton(
            "Restart",
            panel.rectTransform,
            new Vector2(0f, -80f),
            new Vector2(260f, 52f),
            null,
            "New Game",
            StartNewGame);

        modal.SetActive(false);
    }

    private void StartNewGame()
    {
        if (!gameOver && score > 0)
        {
            ScoreStore.AddScore(score);
        }

        if (clearSequence != null)
            clearSequence.Kill();

        dragged = null;
        paused = false;
        gameOver = false;
        busy = false;
        score = 0;

        modal.SetActive(false);
        FadeMusic(settings.musicVolume);

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] != null)
            {
                pieces[i].gameObject.SetActive(false);
                Destroy(pieces[i].gameObject);
            }

            pieces[i] = null;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                occupied[x, y] = false;

                RectTransform rect = boardImages[x, y].rectTransform;
                rect.DOKill();
                rect.localScale = Vector3.one;
            }
        }

        PaintBoard();
        UpdateScore();
        UpdateBestScores(ScoreStore.Load());

        SpawnBatch();
        CheckGameOver();
    }

    private void SpawnBatch()
    {
        for (int i = 0; i < 3; i++)
        {
            ShapeData data =
                usableShapes[Random.Range(0, usableShapes.Count)];

            RectTransform rect = MakeRect(
                "Piece_" + i,
                content,
                Vector2.zero,
                Vector2.zero);

            PieceView piece = rect.gameObject.AddComponent<PieceView>();

            piece.Initialize(
                this,
                data.GetCells(),
                i,
                new Vector2((i - 1) * 360f, -285f),
                cellSprite);

            pieces[i] = piece;
            rotateButtons[i].interactable = true;

            rect.localScale = Vector3.one * 0.8f;
            rect.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        }
    }

    public void BeginDrag(PieceView piece, Vector2 screenPoint)
    {
        if (paused || gameOver || busy || dragged != null)
            return;

        dragged = piece;
        piece.Rect.DOKill();
        piece.Rect.localScale = Vector3.one;
        piece.Rect.SetAsLastSibling();

        piece.Rebuild(cellSize);

        PlaySound(takeSound);
        MoveDrag(piece, screenPoint);
    }

    public void MoveDrag(PieceView piece, Vector2 screenPoint)
    {
        if (piece != dragged || paused || gameOver || busy)
            return;

        lastPointer = screenPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            content,
            screenPoint,
            null,
            out Vector2 localPoint);

        piece.CenterAt(localPoint);

        PaintBoard();

        Vector2Int origin = GetDropOrigin(piece);
        bool valid = CanPlace(piece.Cells, origin);

        foreach (Vector2Int cell in piece.Cells)
        {
            Vector2Int position = origin + cell;

            if (Inside(position))
            {
                boardImages[position.x, position.y].color =
                    valid ? validColor : invalidColor;
            }
        }
    }

    public void EndDrag(PieceView piece)
    {
        if (piece != dragged)
            return;

        dragged = null;
        PaintBoard();

        Vector2Int origin = GetDropOrigin(piece);

        if (paused || gameOver || busy ||
            !CanPlace(piece.Cells, origin))
        {
            piece.ReturnHome(true);
            return;
        }

        foreach (Vector2Int cell in piece.Cells)
        {
            Vector2Int position = origin + cell;
            occupied[position.x, position.y] = true;
        }

        pieces[piece.Slot] = null;
        rotateButtons[piece.Slot].interactable = false;

        piece.gameObject.SetActive(false);
        Destroy(piece.gameObject);

        PlaySound(placeSound);
        PaintBoard();

        ResolveLines();
    }

    private Vector2Int GetDropOrigin(PieceView piece)
    {
        Vector2 boardTopLeft =
            board.anchoredPosition +
            new Vector2(
                -board.sizeDelta.x * 0.5f,
                board.sizeDelta.y * 0.5f);

        Vector2 difference =
            piece.Rect.anchoredPosition - boardTopLeft;

        return new Vector2Int(
            Mathf.RoundToInt(difference.x / cellSize),
            Mathf.RoundToInt(-difference.y / cellSize));
    }

    private bool Inside(Vector2Int position)
    {
        return position.x >= 0 &&
               position.x < width &&
               position.y >= 0 &&
               position.y < height;
    }

    private bool CanPlace(
        List<Vector2Int> cells,
        Vector2Int origin)
    {
        foreach (Vector2Int cell in cells)
        {
            Vector2Int position = origin + cell;

            if (!Inside(position))
                return false;

            if (occupied[position.x, position.y])
                return false;
        }

        return cells.Count > 0;
    }

    private void RotateSlot(int slot)
    {
        if (paused || gameOver || busy || dragged != null)
            return;

        PieceView piece = pieces[slot];

        if (piece == null)
            return;

        piece.Rotate();
        piece.ReturnHome(false);
        PlaySound(rotateSound);
    }

    private void ResolveLines()
    {
        var toClear = new HashSet<Vector2Int>();

        for (int y = 0; y < height; y++)
        {
            bool full = true;

            for (int x = 0; x < width; x++)
            {
                if (!occupied[x, y])
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                for (int x = 0; x < width; x++)
                    toClear.Add(new Vector2Int(x, y));
            }
        }

        for (int x = 0; x < width; x++)
        {
            bool full = true;

            for (int y = 0; y < height; y++)
            {
                if (!occupied[x, y])
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                for (int y = 0; y < height; y++)
                    toClear.Add(new Vector2Int(x, y));
            }
        }

        if (toClear.Count == 0)
        {
            FinishTurn();
            return;
        }

        busy = true;
        PlaySound(clearSound);

        score += toClear.Count *
                 Mathf.Max(1, settings.pointsPerCell);

        UpdateScore();

        clearSequence = DOTween.Sequence();

        foreach (Vector2Int position in toClear)
        {
            clearSequence.Join(
                boardImages[position.x, position.y]
                    .rectTransform
                    .DOScale(0f, 0.2f)
                    .SetEase(Ease.InBack));
        }

        clearSequence.OnComplete(() =>
        {
            foreach (Vector2Int position in toClear)
            {
                occupied[position.x, position.y] = false;

                boardImages[position.x, position.y]
                    .rectTransform.localScale = Vector3.one;
            }

            PaintBoard();
            busy = false;
            FinishTurn();
        });
    }

    private void FinishTurn()
    {
        bool allUsed = true;

        foreach (PieceView piece in pieces)
        {
            if (piece != null)
            {
                allUsed = false;
                break;
            }
        }

        if (allUsed)
            SpawnBatch();

        CheckGameOver();
    }

    private void CheckGameOver()
    {
        foreach (PieceView piece in pieces)
        {
            if (piece == null)
                continue;

            var cells = new List<Vector2Int>(piece.Cells);

            for (int rotation = 0; rotation < 4; rotation++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (CanPlace(cells, new Vector2Int(x, y)))
                            return;
                    }
                }

                cells = ShapeData.RotateClockwise(cells);
            }
        }

        EndGame();
    }

    private void EndGame()
    {
        if (gameOver)
            return;

        gameOver = true;
        paused = false;

        UpdateBestScores(ScoreStore.AddScore(score));

        modalTitle.text = "GAME OVER\nScore: " + score;
        resumeButton.gameObject.SetActive(false);

        modal.SetActive(true);
        modal.transform.SetAsLastSibling();

        FadeMusic(settings.pausedMusicVolume);
    }

    private void TogglePause()
    {
        if (gameOver)
            return;

        paused = !paused;

        if (paused && dragged != null)
        {
            PieceView piece = dragged;
            dragged = null;
            piece.ReturnHome(true);
            PaintBoard();
        }

        modalTitle.text = "PAUSED";
        resumeButton.gameObject.SetActive(true);

        modal.SetActive(paused);

        if (paused)
            modal.transform.SetAsLastSibling();

        FadeMusic(
            paused
                ? settings.pausedMusicVolume
                : settings.musicVolume);
    }

    private void PaintBoard()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Image image = boardImages[x, y];

                image.sprite = occupied[x, y] ? cellSprite : null;
                image.color = occupied[x, y]
                    ? Color.white
                    : emptyColor;
            }
        }
    }

    private void UpdateScore()
    {
        scoreText.text = "Score: " + score.ToString("D6");
    }

    private void UpdateBestScores(List<int> scores)
    {
        bestText.text = "TOP 3";

        for (int i = 0; i < 3; i++)
        {
            string value = i < scores.Count
                ? scores[i].ToString()
                : "-";

            bestText.text += "\n" + (i + 1) + ". " + value;
        }
    }

    private RectTransform MakeRect(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size)
    {
        var gameObject = new GameObject(
            objectName,
            typeof(RectTransform));

        RectTransform rect =
            gameObject.GetComponent<RectTransform>();

        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return rect;
    }

    private Image MakeImage(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        Sprite sprite,
        Color color)
    {
        RectTransform rect =
            MakeRect(objectName, parent, position, size);

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    private Text MakeText(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        string value,
        int fontSize)
    {
        RectTransform rect =
            MakeRect(objectName, parent, position, size);

        Text text = rect.gameObject.AddComponent<Text>();

        text.font = font;
        text.fontSize = fontSize;
        text.text = value;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;

        return text;
    }

    private Button MakeButton(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        Sprite sprite,
        string label,
        UnityAction action)
    {
        Image image = MakeImage(
            objectName,
            parent,
            position,
            size,
            sprite,
            sprite != null
                ? Color.white
                : new Color(0.22f, 0.35f, 0.52f));

        image.raycastTarget = true;

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        button.onClick.AddListener(action);

        if (!string.IsNullOrEmpty(label))
        {
            MakeText(
                "Label",
                image.rectTransform,
                Vector2.zero,
                size,
                label,
                23);
        }

        return button;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (clearSequence != null)
            clearSequence.Kill();

        if (music != null)
            music.DOKill();

        if (canvasRect != null)
            Destroy(canvasRect.gameObject);
    }
}