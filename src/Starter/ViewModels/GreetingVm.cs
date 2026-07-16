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
        "Hope today went well"
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
        "00010 10100 111 010 111 100"
    ];

    private readonly TimeSpan minimumTimeBeforeRefresh = TimeSpan.FromMinutes(30);
    private string[]? lastGreetingSource;
    private DateTimeOffset lastRefresh;
    private readonly Random random = new();

    private string[] CurrentGreetingArray() =>
        DateTimeOffset.Now.Hour switch
        {
            >= 5 and < 12 => morning,
            >= 12 and < 18 => afternoon,
            >= 18 and < 23 => evening,
            _ => lateNight
        };

    [ObservableProperty] public partial string Greeting { get; set; }

    private void ChooseNewGreeting(bool forceTimeGreeting)
    {
        var arr = CurrentGreetingArray();

        if (forceTimeGreeting)
        {
            var idx = random.Next(arr.Length);
            Greeting = arr[idx];
            lastGreetingSource = arr;
        }
        else
        {
            var idx = random.Next(arr.Length + others.Length);
            Greeting = idx < arr.Length ? arr[idx] : others[idx-arr.Length];
            lastGreetingSource = idx < arr.Length ? arr : others;
        }

        lastRefresh = DateTimeOffset.Now;
    }

    public GreetingVm() => ChooseNewGreeting(true);

    [RelayCommand]
    private void RefreshGreeting()
    {
        if (lastRefresh.Hour != DateTimeOffset.Now.Hour &&
            lastGreetingSource != CurrentGreetingArray())
            ChooseNewGreeting(true);

        else if (lastRefresh + minimumTimeBeforeRefresh <= DateTimeOffset.Now)
            ChooseNewGreeting(false);
    }
}
