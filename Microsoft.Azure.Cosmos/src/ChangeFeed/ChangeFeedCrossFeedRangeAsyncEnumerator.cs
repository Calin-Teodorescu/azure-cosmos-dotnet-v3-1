﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeed
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.ChangeFeed.Pagination;
    using Microsoft.Azure.Cosmos.Pagination;
    using Microsoft.Azure.Cosmos.Query.Core.Monads;
    using Microsoft.Azure.Cosmos.Serializer;
    using Microsoft.Azure.Cosmos.Tracing;
    using Microsoft.Azure.Cosmos.Tracing.AsyncEnumerable;

    internal sealed class ChangeFeedCrossFeedRangeAsyncEnumerator : ITraceableAsyncEnumerator<TryCatch<ChangeFeedPage>>
    {
        private readonly CrossPartitionChangeFeedAsyncEnumerator enumerator;
        private readonly JsonSerializationFormatOptions jsonSerializationFormatOptions;
        private readonly ITrace trace;

        public ChangeFeedCrossFeedRangeAsyncEnumerator(
            CrossPartitionChangeFeedAsyncEnumerator enumerator,
            JsonSerializationFormatOptions jsonSerializationFormatOptions,
            ITrace trace)
        {
            this.enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
            this.jsonSerializationFormatOptions = jsonSerializationFormatOptions;
            this.trace = trace ?? throw new ArgumentNullException(nameof(trace));
        }

        public TryCatch<ChangeFeedPage> Current { get; private set; }

        public ValueTask DisposeAsync() => this.enumerator.DisposeAsync();

        public ValueTask<bool> MoveNextAsync()
        {
            return this.MoveNextAsync(this.trace);
        }

        public async ValueTask<bool> MoveNextAsync(ITrace trace)
        {
            if (!await this.enumerator.MoveNextAsync(trace))
            {
                throw new InvalidOperationException("Change Feed should always be able to move next.");
            }

            TryCatch<CrossFeedRangePage<Pagination.ChangeFeedPage, ChangeFeedState>> monadicInnerChangeFeedPage = this.enumerator.Current;
            if (monadicInnerChangeFeedPage.Failed)
            {
                this.Current = TryCatch<ChangeFeedPage>.FromException(monadicInnerChangeFeedPage.Exception);
                return true;
            }

            CrossFeedRangePage<Pagination.ChangeFeedPage, ChangeFeedState> innerChangeFeedPage = monadicInnerChangeFeedPage.Result;
            CrossFeedRangeState<ChangeFeedState> crossFeedRangeState = innerChangeFeedPage.State;
            ChangeFeedCrossFeedRangeState state = new ChangeFeedCrossFeedRangeState(crossFeedRangeState.Value);
            ChangeFeedPage page = innerChangeFeedPage.Page switch
            {
                Pagination.ChangeFeedSuccessPage successPage => ChangeFeedPage.CreatePageWithChanges(
                    RestFeedResponseParser.ParseRestFeedResponse(
                        successPage.Content,
                        this.jsonSerializationFormatOptions),
                    successPage.RequestCharge,
                    successPage.ActivityId,
                    state,
                    successPage.AdditionalHeaders),
                Pagination.ChangeFeedNotModifiedPage notModifiedPage => ChangeFeedPage.CreateNotModifiedPage(
                     notModifiedPage.RequestCharge,
                     notModifiedPage.ActivityId,
                     state,
                     notModifiedPage.AdditionalHeaders),
                _ => throw new InvalidOperationException($"Unknown type: {innerChangeFeedPage.Page.GetType()}"),
            };

            this.Current = TryCatch<ChangeFeedPage>.FromResult(page);
            return true;
        }
    }
}
