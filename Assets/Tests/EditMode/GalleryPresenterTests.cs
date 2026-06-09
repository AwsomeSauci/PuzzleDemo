using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using PuzzleFlow.Application;
using PuzzleFlow.Domain;
using PuzzleFlow.Presentation.Gallery;
using PuzzleFlow.Presentation.Media;
using UnityEngine;

namespace PuzzleFlow.Tests
{
    public sealed class GalleryPresenterTests
    {
        [Test]
        public void Start_RendersPlaceholdersWithoutLoadingPreviews()
        {
            FakeGalleryView view = new FakeGalleryView();
            FakePuzzleMediaQueryService mediaQueryService = new FakePuzzleMediaQueryService();
            FakeMediaSpriteService mediaSpriteService = new FakeMediaSpriteService();
            GalleryPresenter presenter = CreatePresenter(mediaQueryService, mediaSpriteService);

            try
            {
                presenter.Start(view);

                Assert.That(view.RenderedItems, Is.Not.Null);
                Assert.That(view.RenderedItems.Count, Is.EqualTo(2));
                Assert.That(view.RenderedItems[0].Preview, Is.Null);
                Assert.That(view.RenderedItems[1].Preview, Is.Null);
                Assert.That(mediaQueryService.RequestCount, Is.EqualTo(0));
                Assert.That(mediaSpriteService.LoadCount, Is.EqualTo(0));
            }
            finally
            {
                presenter.Dispose();
            }
        }

        [Test]
        public async Task ItemBecameVisible_LoadsPreviewOnceForThatItem()
        {
            FakeGalleryView view = new FakeGalleryView();
            FakePuzzleMediaQueryService mediaQueryService = new FakePuzzleMediaQueryService();
            FakeMediaSpriteService mediaSpriteService = new FakeMediaSpriteService();
            GalleryPresenter presenter = CreatePresenter(mediaQueryService, mediaSpriteService);

            try
            {
                presenter.Start(view);

                view.RaiseItemBecameVisible(1);
                await WaitUntil(() => view.UpdatedItems.ContainsKey(1));
                view.RaiseItemBecameVisible(1);

                Assert.That(mediaQueryService.RequestCount, Is.EqualTo(1));
                Assert.That(mediaSpriteService.LoadCount, Is.EqualTo(1));
                Assert.That(mediaQueryService.RequestedPuzzleIds[0].Value, Is.EqualTo("second-puzzle"));
                Assert.That(view.UpdatedItems[1].Puzzle.Id.Value, Is.EqualTo("second-puzzle"));
            }
            finally
            {
                presenter.Dispose();
            }
        }

        private static GalleryPresenter CreatePresenter(
            FakePuzzleMediaQueryService mediaQueryService,
            FakeMediaSpriteService mediaSpriteService)
        {
            return new GalleryPresenter(
                new FakeGalleryQueryService(),
                mediaQueryService,
                mediaSpriteService);
        }

        private static async Task WaitUntil(Func<bool> predicate)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                if (predicate.Invoke())
                {
                    return;
                }

                await Task.Delay(10);
            }

            Assert.Fail("Condition was not met before timeout.");
        }

        private static PuzzleDefinition CreatePuzzle(string id)
        {
            return new PuzzleDefinition(
                new PuzzleId(id),
                id,
                "Tests",
                new[]
                {
                    new PuzzleCutDefinition(36, new StartOption(PuzzleStartMode.Free, 0))
                });
        }

        private sealed class FakeGalleryView : IPuzzleGalleryView
        {
            public readonly Dictionary<int, PuzzleGalleryItemViewModel> UpdatedItems =
                new Dictionary<int, PuzzleGalleryItemViewModel>();

            public event Action<int> ItemBecameVisible;
            public IReadOnlyList<PuzzleGalleryItemViewModel> RenderedItems { get; private set; }

            public void Render(IReadOnlyList<PuzzleGalleryItemViewModel> items)
            {
                RenderedItems = items;
            }

            public void UpdateItem(int itemIndex, PuzzleGalleryItemViewModel item)
            {
                UpdatedItems[itemIndex] = item;
            }

            public void RaiseItemBecameVisible(int itemIndex)
            {
                ItemBecameVisible?.Invoke(itemIndex);
            }
        }

        private sealed class FakeGalleryQueryService : IPuzzleGalleryQueryService
        {
            public IReadOnlyList<PuzzleGalleryItemData> GetItems()
            {
                return new[]
                {
                    new PuzzleGalleryItemData(
                        CreatePuzzle("first-puzzle"),
                        new StartOption(PuzzleStartMode.Free, 0)),
                    new PuzzleGalleryItemData(
                        CreatePuzzle("second-puzzle"),
                        new StartOption(PuzzleStartMode.Free, 0))
                };
            }
        }

        private sealed class FakePuzzleMediaQueryService : IPuzzleMediaQueryService
        {
            public readonly List<PuzzleId> RequestedPuzzleIds = new List<PuzzleId>();

            public int RequestCount => RequestedPuzzleIds.Count;

            public MediaReference GetPreviewMedia(PuzzleId puzzleId)
            {
                RequestedPuzzleIds.Add(puzzleId);
                return new MediaReference("preview/" + puzzleId.Value);
            }
        }

        private sealed class FakeMediaSpriteService : IMediaSpriteService
        {
            public int LoadCount { get; private set; }

            public UniTask<Sprite> LoadSpriteAsync(
                MediaReference reference,
                CancellationToken cancellationToken = default)
            {
                LoadCount++;
                return UniTask.FromResult<Sprite>(null);
            }

            public void Dispose()
            {
            }
        }
    }
}
