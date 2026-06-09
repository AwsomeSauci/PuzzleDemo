using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PuzzleFlow.Domain;
using UnityEngine;

namespace PuzzleFlow.Presentation.Media
{
    public interface IMediaSpriteService : IDisposable
    {
        UniTask<Sprite> LoadSpriteAsync(
            MediaReference reference,
            CancellationToken cancellationToken = default);
    }
}

