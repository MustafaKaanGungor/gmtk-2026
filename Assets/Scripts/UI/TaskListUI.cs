using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Sol ustte duran gorev listesi. Normalde sonuk durur, uzerine mouse
/// gelince belirginlesir. Listeyi TaskManager'dan cizer ve tamamlanan
/// gorevleri isaretler.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public class TaskListUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private TMP_Text listText;

    [Header("Header")]
    [Tooltip("Listenin ustundeki baslik. Bos birakilirsa gosterilmez.")]
    [SerializeField] private string header = "Gorevler";

    [Header("Task Prefixes")]
    [Tooltip("Tamamlanan gorevin basina gelen isaret.")]
    [SerializeField] private string completedPrefix = "[X] ";

    [Tooltip("Tamamlanmayan gorevin basina gelen isaret.")]
    [SerializeField] private string pendingPrefix = "[  ] ";

    [Header("Colors")]
    [SerializeField] private Color pendingColor = Color.white;
    [SerializeField] private Color completedColor =
        new Color(0.4f, 1f, 0.4f);

    [Header("Hover Fade")]
    [Tooltip("Mouse uzerinde degilken saydamlik (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] private float idleAlpha = 0.4f;

    [Tooltip("Mouse uzerindeyken saydamlik (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] private float hoverAlpha = 1f;

    [Tooltip("Saydamlik gecis hizi.")]
    [Min(0f)]
    [SerializeField] private float fadeSpeed = 8f;

    private CanvasGroup canvasGroup;
    private float targetAlpha;
    private readonly StringBuilder builder = new StringBuilder();

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        targetAlpha = idleAlpha;
        canvasGroup.alpha = idleAlpha;
    }

    private void OnEnable()
    {
        if (taskManager != null)
        {
            taskManager.TaskCompleted += HandleTaskCompleted;
        }

        RefreshList();
    }

    private void OnDisable()
    {
        if (taskManager != null)
        {
            taskManager.TaskCompleted -= HandleTaskCompleted;
        }
    }

    private void Start()
    {
        // TaskManager.Start icinde gorevler sifirlanabilir; bir kez daha ciz.
        RefreshList();
    }

    private void Update()
    {
        if (Mathf.Approximately(canvasGroup.alpha, targetAlpha))
        {
            return;
        }

        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetAlpha = hoverAlpha;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetAlpha = idleAlpha;
    }

    private void HandleTaskCompleted(GameTask task)
    {
        RefreshList();
    }

    /// <summary>Gorev listesini bastan cizer.</summary>
    public void RefreshList()
    {
        if (listText == null || taskManager == null)
        {
            return;
        }

        builder.Clear();

        if (!string.IsNullOrEmpty(header))
        {
            builder.Append("<b>");
            builder.Append(header);
            builder.Append("</b>\n");
        }

        string completedHex = ColorUtility.ToHtmlStringRGB(completedColor);
        string pendingHex = ColorUtility.ToHtmlStringRGB(pendingColor);

        IReadOnlyList<GameTask> tasks = taskManager.Tasks;

        for (int index = 0; index < tasks.Count; index++)
        {
            GameTask task = tasks[index];

            if (task == null)
            {
                continue;
            }

            bool done = task.IsCompleted;

            builder.Append("<color=#");
            builder.Append(done ? completedHex : pendingHex);
            builder.Append(">");
            builder.Append(done ? completedPrefix : pendingPrefix);
            builder.Append(task.Title);
            builder.Append("</color>");

            if (index < tasks.Count - 1)
            {
                builder.Append('\n');
            }
        }

        listText.text = builder.ToString();
    }
}
