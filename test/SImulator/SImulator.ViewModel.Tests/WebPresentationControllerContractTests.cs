using System.Text.Json;
using NUnit.Framework;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Controllers;
using SImulator.ViewModel.Model;
using SIUI.ViewModel;

namespace SImulator.ViewModel.Tests;

public sealed class WebPresentationControllerContractTests
{
    [Test]
    public void SetTableSendsThePriceMatrixExpectedBySIOnline()
    {
        using var controller = new WebPresentationController(
            new TestScreen(),
            new StubPresentationListener(),
            new SoundsSettings(),
            sendCommonMessages: true);

        string? json = null;
        controller.SendJsonMessage += message => json = message;

        var science = new ThemeInfoViewModel { Name = "Science" };
        science.Questions.Add(new QuestionInfoViewModel { Price = 100 });
        science.Questions.Add(new QuestionInfoViewModel { Price = 200 });

        var games = new ThemeInfoViewModel { Name = "Games" };
        games.Questions.Add(new QuestionInfoViewModel { Price = 300 });

        controller.SetTable([science, games]);

        Assert.That(json, Is.Not.Null);

        using var document = JsonDocument.Parse(json!);
        var root = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("type").GetString(), Is.EqualTo("table"));
            Assert.That(root.GetProperty("table")[0][0].GetInt32(), Is.EqualTo(100));
            Assert.That(root.GetProperty("table")[0][1].GetInt32(), Is.EqualTo(200));
            Assert.That(root.GetProperty("table")[1][0].GetInt32(), Is.EqualTo(300));
            Assert.That(root.GetProperty("table")[0].ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(root.GetProperty("themes")[0].GetString(), Is.EqualTo("Science"));
            Assert.That(root.GetProperty("themes")[1].GetString(), Is.EqualTo("Games"));
        });
    }

    private sealed class StubPresentationListener : IPresentationListener
    {
        public void OnAnswerSelected(int answerIndex) { }

        public void AskNext() { }

        public void AskBack() { }

        public void AskNextRound() { }

        public void AskBackRound() { }

        public void AskStop() { }

        public void OnMediaStart() { }

        public void OnMediaEnd() { }

        public void OnMediaProgress(double progress) { }

        public void OnRoundThemesFinished() { }
    }
}
