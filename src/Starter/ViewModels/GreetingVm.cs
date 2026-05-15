namespace Starter.ViewModels;

public partial class GreetingVm : ObservableObject
{
    private readonly string[] morning =
    [
        "Good morning",
        "Morning!",
        "Rise and shine",
        "Ready for a fresh start?",
        "Let’s make today productive",
        "Coffee first?",
        "New day, new tasks",
        "Hope you slept well"
    ];

    private readonly string[] afternoon =
    [
        "Good afternoon",
        "Hope your day is going well",
        "Back to work?",
        "What’s next?",
        "Time to get things done",
        "Need something quickly?",
        "Keep the momentum going",
        "Another productive afternoon"
    ];

    private readonly string[] evening =
    [
        "Good evening",
        "Wrapping things up?",
        "Long day?",
        "Time to relax a bit",
        "Evening productivity session",
        "Finishing the last tasks?",
        "Hope today went well",
        "Quiet evenings are perfect for focus"
    ];

    private readonly string[] lateNight =
    [
        "Still awake?",
        "Burning the midnight oil?",
        "Late-night session detected",
        "Don’t forget to rest",
        "Night owl mode",
        "Working late again?",
        "The world is quiet now",
        "One last task before sleep?"
    ];

    private readonly string[] others =
    [
        "Productivity mode: ON",
        "Ready when you are",
        "Chaos organizer activated",
        "Another day, another shortcut",
        "What are we launching today?",
        "1110001101010"
    ];

    private readonly TimeSpan minimumTimeBeforeRefresh = TimeSpan.FromMinutes(5);
    private DateTimeOffset lastRefresh;
    private readonly Random random = new();

    [ObservableProperty] public partial string Greeting { get; set; }

    private void ChooseNewGreeting()
    {
        var arr = DateTimeOffset.Now.Hour switch
        {
            >= 5 and < 12 => morning,
            >= 12 and < 18 => afternoon,
            >= 18 and < 23 => evening,
            _ => lateNight
        };

        var idx = random.Next(arr.Length + others.Length);
        Greeting = idx < arr.Length ? arr[idx] : others[idx-arr.Length];
        lastRefresh = DateTimeOffset.Now;
    }

    public GreetingVm() => ChooseNewGreeting();

    [RelayCommand]
    private void RefreshGreeting()
    {
        if (lastRefresh + minimumTimeBeforeRefresh > DateTimeOffset.Now) return;
        ChooseNewGreeting();
    }
}
