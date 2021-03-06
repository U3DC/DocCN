using System.Collections.Generic;
using System.Linq;
using DocCN.Models.Json;
using DocCN.Style;
using DocCN.Utility;
using Newtonsoft.Json;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;
using UnityEngine;
using UnityEngine.Networking;
using Color = Unity.UIWidgets.ui.Color;

namespace DocCN.Components
{
    public partial class MarkdownContent
    {
        private partial class MarkdownContentState : State<MarkdownContent>
        {
            private Token[] _tokens;

            private Link _prevLink;

            private Link _nextLink;

            private Breadcrumb[] _breadcrumbs;

            private ScrollController _scrollController;

            private ImageMeta[] _imageMetas;

            private bool _loading;

            private List<TapGestureRecognizer> _spanRecognizers;

            public override void initState()
            {
                base.initState();
                _scrollController = new ScrollController();
                Load();
            }

            private void Load()
            {
                _loading = true;
                var version = DocApp.of(context).version;
                var url =
                    $"{Configuration.Instance.cdnPrefix}/{version.unityVersion}/{version.parsedVersion}/manual/json/{widget._title.Replace('-', '_')}.json";
                var request = UnityWebRequest.Get(url);
                var asyncOperation = request.SendWebRequest();
                asyncOperation.completed += operation =>
                {
                    if (!mounted)
                    {
                        return;
                    }

                    using (WindowProvider.of(context).getScope())
                    {
                        if (request.responseCode == 404)
                        {
                            DocApp.of(context).ForceUnknown();
                        }
                        else
                        {
                            var content = DownloadHandlerBuffer.GetContent(request);
                            var model = JsonConvert.DeserializeObject<ManualModel>(content);
                            setState(() =>
                            {
                                _tokens = model.tokens;
                                _prevLink = model.prev;
                                _nextLink = model.next;
                                _breadcrumbs = model.breadcrumbs;
                                _imageMetas = model.imageMetas;
                                _loading = false;
                            });
                        }
                    }
                };
            }

            public override void didUpdateWidget(StatefulWidget oldWidget)
            {
                base.didUpdateWidget(oldWidget);
                if (!(oldWidget is MarkdownContent old)) return;
                if (old._title != widget._title)
                {
                    Load();
                }
            }

            public override Widget build(BuildContext buildContext)
            {
                if (_loading)
                {
                    return new Container(
                        child: new Center(
                            child: new Loading(
                                size: 48f
                            )
                        )
                    );
                }

                var markdownBuildCtx = new BuilderContext(_imageMetas, buildContext);
                _spanRecognizers?.ForEach(recognizer => recognizer.dispose());

                var widgets = new List<Widget>();
                widgets.AddRange(_tokens
                    .Where(token => Mappings.ContainsKey(token.type))
                    .Select(token => Mappings[token.type].Invoke(token, markdownBuildCtx))
                    .Where(w => !(w is null))
                    .Select(w => new RepaintBoundary(child: w)));
                _spanRecognizers = markdownBuildCtx.spanRecognizers;

                widgets.Insert(0,
                    new Container(
                        height: 64f,
                        padding: EdgeInsets.only(bottom: 8f),
                        decoration: new BoxDecoration(
                            border: new Border(
                                bottom: new BorderSide(
                                    color: new Color(0xffd8d8d8),
                                    width: 1f
                                )
                            )
                        ),
                        child: new Align(
                            alignment: Alignment.bottomLeft,
                            child: new Breadcrumbs(
                                _breadcrumbs,
                                normalBreadcrumbStyle: new TextStyle(
                                    fontSize: 16f,
                                    color: new Color(0xff979797)
                                ),
                                hoverBreadCrumbStyle: new TextStyle(
                                    fontSize: 16f,
                                    color: new Color(0xff979797),
                                    decoration: TextDecoration.underline
                                ),
                                splitterStyle: new TextStyle(
                                    fontSize: 16f,
                                    color: new Color(0xff979797)
                                )
                            )
                        )
                    )
                );

                var linkButtons = new List<Widget>();
                if (_prevLink != null)
                {
                    linkButtons.Add(
                        new Button(
                            text: _prevLink.content ?? "",
                            onTap: () =>
                            {
                                if (!string.IsNullOrEmpty(_prevLink?.link))
                                {
                                    LocationUtil.Go(
                                        $"/Manual/{_prevLink?.link}");
                                }
                            },
                            prefix: Icons.MaterialArrowBack
                        )
                    );
                }
                else
                {
                    linkButtons.Add(new Container()); // placeholder to use spaceBetween
                }

                if (_nextLink != null)
                {
                    linkButtons.Add(
                        new Button(
                            text: _nextLink.content ?? "",
                            onTap: () =>
                            {
                                if (!string.IsNullOrEmpty(_nextLink.link))
                                {
                                    LocationUtil.Go(
                                        $"/Manual/{_nextLink.link}");
                                }
                            },
                            suffix: Icons.MaterialArrowForward
                        )
                    );
                }

                widgets.Add(
                    new Container(
                        margin: EdgeInsets.only(top: 32, bottom: 64),
                        child: new Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: linkButtons
                        )
                    )
                );

                var version = DocApp.of(buildContext).version;

                return new Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: new List<Widget>
                    {
                        new Expanded(
                            child: new Stack(
                                children: new List<Widget>
                                {
                                    new SingleChildScrollView(
                                        controller: _scrollController,
                                        child: new ScrollableOverlay(
                                            child: new Column(
                                                children: new List<Widget>
                                                {
                                                    new Container(
                                                        constraints: new BoxConstraints(
                                                            minHeight: MediaQuery.of(buildContext).size.height -
                                                                       Header.Height -
                                                                       SearchBar.Height -
                                                                       Footer.Height
                                                        ),
                                                        child: new Column(
                                                            mainAxisAlignment: MainAxisAlignment.start,
                                                            crossAxisAlignment: CrossAxisAlignment.start,
                                                            children: widgets)
                                                    ),
                                                    new Footer(style: Footer.Light, showSocials: false)
                                                }
                                            )
                                        )
                                    ),
                                    new ToTop(_scrollController, displayThreshold: 128f)
                                }
                            )
                        ),
                        new MetaFields(
                            markdownBuildCtx.positionRecords,
                            _scrollController,
                            $"https://github.com/UnityTech/documentation-cn/blob/{version.unityVersion}/Manual/md/{widget._title}.md"
                        )
                    }
                );
            }
        }
    }
}