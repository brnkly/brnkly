$(function () {

    window.brnkly = {};

    ko.observableArray.fn.orderBy = function (keySelector) {
        this.sort(function (a, b) {
            return keySelector(a) > keySelector(b);
        });
    },

    ko.observableArray.fn.first = function (predicate) {
        return Enumerable.From(this()).FirstOrDefault(null);
    }

    ko.observableArray.fn.removeWhere = function (predicate) {
        var underlyingArray = this();
        var result = [];
        for (var i = underlyingArray.length - 1; i >= 0; i--) {
            if (predicate(underlyingArray[i])) {
                result.push(this.splice(i, 1));
            }
        }
        return result;
    }

    function message(severity, title, text, nofade) {
        message(
            severity,
            '<h4>' + title + '</h4>' +
            '<button type="button" class="close" data-dismiss="alert">&times;</button>' +
            '<p>' + text + '</p>',
            nofade);
    };

    function message(severity, html, nofade) {
        var msg = $('#message');
        msg.removeClass().empty().append(html)
            .addClass('alert').addClass('alert-' + severity)
            .show();
        if (!nofade) {
            msg.fadeTo(1000, 1).fadeOut();
        }
    };

    function getLagValue(utcDate, secondsConsideredCurrent) {
        var lagTime = Math.floor((new Date() - utcDate) / 1000);
        if (lagTime <= secondsConsideredCurrent) {
            return 'Current';
        } else {
            var lagUnits = 's';
            if (lagTime >= 120) { lagTime = Math.floor(lagTime / 60); lagUnits = 'm'; }
            if (lagTime >= 120) { lagTime = Math.floor(lagTime / 60); lagUnits = 'h'; }
            if (lagTime >= 72) { lagTime = Math.floor(lagTime / 24); lagUnits = 'd'; }
            return lagTime + lagUnits;
        }
    };

    function getUri(url) {
        var a = document.createElement("a");
        a.href = url;
        return a;
    }

    function getDisplayName(url) {
        var a = getUri(url);
        return ko.computed(function () {
            return !brnkly.loading() && brnkly.raven.displayPorts() ? a.host : a.hostname
        }, a);
    };

    function RavenConfigVM(data) {
        var self = this;
        self.koMapping = {
            'ignore': 'id',
            'stores': {
                create: function (options) { return new StoreVM(options.data); },
                key: function (data) { return ko.utils.unwrapObservable(data.name); }
            }
        };
        ko.mapping.fromJS(data, self.koMapping, this);

        self.etag = ko.observable(data.etag);
        self.hasEtag = ko.computed(function () {
            return self.etag() != "00000000-0000-0000-0000-000000000000";
        });

        self.placeholderStore = new StoreVM({ name: 'Add a store...', instances: [] });
        self.activeStore = ko.observable(self.placeholderStore);
        self.setActiveStore = function () {
            self.activeStore(this);
            window.location.hash = this.name();
        };

        self.getStore = function (name) {
            return Enumerable.From(self.stores())
                .Where(function (s) { return s.name() == name; })
                .FirstOrDefault(null);
        };

        self.addStore = function () {
            var storeName = $('#store-name').val();
            var store = new StoreVM({ name: storeName, instances: [] });
            self.stores.push(store);
            self.stores.orderBy(function (item) { return item.name().toLowerCase(); });
            self.activeStore(store);
            setInterval(store.getStats, 15000);
            store.getStats();
        };

        self.removeActiveStore = function () {
            var index = self.stores.indexOf(self.activeStore());
            if (index >= 0) {
                self.stores.splice(index, 1);
                var newActiveStoreIndex = index == 0 ? 0 : index - 1;
                if (self.stores().length > 0) {
                    self.activeStore(self.stores()[newActiveStoreIndex]);
                } else {
                    self.activeStore(self.placeholderStore);
                }
            }
        };

        // If all ports are the same, don't display them in the UI.
        self.displayPorts = ko.computed(function () {
            var firstPortSet = false;
            var firstPort;
            for (var i = 0; i < self.stores().length; i++) {
                for (var j = 0; j < self.stores()[i].instances().length; j++) {
                    var currentPort = getUri(self.stores()[i].instances()[j].url).port;
                    if (!firstPortSet) {
                        firstPort = currentPort;
                        firstPortSet = true;
                    }
                    if (firstPort != currentPort) return true;
                }
            }
            return false;
        });

        self.save = function () {
            $.ajax({
                url: '/api/raven/replication/pending',
                type: 'PUT',
                data: ko.mapping.toJSON(self),
                contentType: "application/json;charset=utf-8",
                beforeSend: function (jqXHR, settings) { $('#working').show(); },
                complete: function (jqXHR, textStatus) { $('#working').hide(); },
                success: function (data) {
                    self.etag(data.etag);
                    self.dirtyFlag.reset();
                    setTimeout(self.activeStore().getStats, 1000);
                    message('success', 'Save succeeded');
                },
                error: function (data) {
                    var result = JSON.parse(data.responseText);
                    message('danger', 'Save failed', result.exceptionMessage, true);
                }
            });
        };

        self.publish = function() {
            $.ajax({
                url: '/api/raven/replication/live',
                type: 'PUT',
                data: '"' + self.etag() + '"',
                contentType: "application/json;charset=utf-8",
                beforeSend: function (jqXHR, settings) { $('#working').show(); },
                complete: function(jqXHR, textStatus) { $('#working').hide(); },
                success: function (data) {
                    message('success', 'Publish succeeded. Reloading...');
                    window.location.reload();
                },
                error: function (data) {
                    var result = JSON.parse(data.responseText);
                    message('danger', 'Publish failed', result.exceptionMessage, true);
                }
            });
        };
    };

    function StoreVM(data) {
        var self = this;

        var mapping = {
            'instances': {
                create: function (options) { return new InstanceVM(options.data, ko.utils.unwrapObservable(self.name)); },
                key: function (data) { return data.url; }
            }
        };
        ko.mapping.fromJS(data, mapping, self);

        self.stats = ko.observable();
        self.statsRefreshedAt = ko.observable('never');
        self.getStats = function () {
            $.ajax({
                url: '/api/raven/replication/stats',
                type: 'POST',
                data: ko.mapping.toJSON(self),
                contentType: "application/json;charset=utf-8",
                success: function (data) {
                    var dirty = brnkly.raven.dirtyFlag.isDirty();
                    self.stats(new StoreStatsVM(data));
                    self.statsRefreshedAt(new Date().toLocaleTimeString());
                    if (!dirty) { brnkly.raven.dirtyFlag.reset(); }
                },
                error: function (data) {
                    var result = JSON.parse(data.responseText);
                    message('danger', 'Stats retrieval failed', result.exceptionMessage, true);
                }
            });
        };

        self.updateTracers = function () {
            $.ajax({
                url: '/api/raven/replication/tracers',
                type: 'POST',
                data: ko.mapping.toJSON(self),
                contentType: "application/json;charset=utf-8",
                success: function (data) {
                    message('success', 'Tracer documents updated');
                    setTimeout(self.getStats, 1000);
                },
                error: function (data) {
                    var result = JSON.parse(data.responseText);
                    message('danger', 'Stats retrieval failed', result.exceptionMessage, true);
                }
            });
        };

        self.newInstance = {
            protocol: ko.observable('http'),
            host: ko.observable(''),
            port: ko.observable('8081'),
            virtualDirectory: ko.observable('')
        };

        self.addInstance = function () {

            var vdir = self.newInstance.virtualDirectory().length > 0 ?
                '/' + self.newInstance.virtualDirectory() :
                '';
            var instanceUrl = self.newInstance.protocol() + '://' +
                self.newInstance.host() + ':' + self.newInstance.port() +
                vdir + '/databases/' + self.name().toLowerCase();

            // Add the new instance as destination for all existing instances.
            for (var i = 0; i < self.instances().length; i++) {
                var inst = self.instances()[i];
                inst.destinations.push(
                    new DestinationVM({
                        url: instanceUrl,
                        disabled: true,
                        transitiveReplicationBehavior: 'None'
                    }, self.name(), inst.url()));
                inst.destinations.orderBy(
                    function (item) { return item.displayName().toLowerCase() });
            }

            // Add the new instance to the store.
            var instanceObj = new InstanceVM({
                url: instanceUrl,
                allowReads: false,
                allowWrites: false,
                destinations: []
            });
            self.instances.push(instanceObj);
            self.instances.orderBy(function (item) {
                return item.displayName().toLowerCase()
            });

            // Add all instances as destinations for the new instance (including itself).
            for (var i = 0; i < self.instances().length; i++) {
                var inst = self.instances()[i];
                instanceObj.destinations.push(
                    new DestinationVM({
                        url: inst.url(),
                        disabled: true,
                        transitiveReplicationBehavior: 'None'
                    }, self.name(), inst.url()));
            }

            self.getStats();
        };

        self.removeInstance = function (instance) {
            var index = self.instances.indexOf(instance);
            if (index >= 0) {
                self.instances.splice(index, 1);
                for (var j = 0; j < self.instances().length; j++) {
                    self.instances()[j].destinations.removeWhere(function (dest) {
                        return dest.url() == instance.url()
                    });
                }
                self.getStats();
                return;
            }
        };
    };

    function InstanceVM(data, storeName) {
        var self = this;
        self.storeName = storeName;
        var mapping = {
            'destinations': {
                create: function (options) { return new DestinationVM(options.data, storeName, data.url); },
                key: function (data) { return ko.utils.unwrapObservable(data.url); }
            }
        };
        ko.mapping.fromJS(data, mapping, self);

        self.displayName = getDisplayName(self.url());
    };

    function DestinationVM(data, storeName, instanceUrl) {
        var self = this;
        self.storeName = storeName;
        self.instanceUrl = instanceUrl;

        ko.mapping.fromJS(data, {}, self);

        self.displayName = getDisplayName(self.url());
        if (self.currentlyReplicating === undefined) {
            self.currentlyReplicating = !self.disabled();
        }
        self.replicate = ko.computed({
            read: function () { return !self.disabled(); },
            write: function (value) { self.disabled(!value); },
            owner: self
        });
        self.replicationState = ko.computed(function () {
            var msg = "Replication: " + (self.currentlyReplicating ? 'ON' : 'OFF');
            if (self.currentlyReplicating == self.disabled()) {
                if (self.currentlyReplicating) {
                    msg += ' - will be OFF after Publish';
                } else {
                    msg += ' - will be ON after Publish';
                }
            }
            return msg;
        });

        if (self.currentlyTransitive === undefined) {
            self.currentlyTransitive = (self.transitiveReplicationBehavior() == 'Replicate');
        }
        self.isTransitive = ko.computed({
            read: function () { return self.transitiveReplicationBehavior() == 'Replicate'; },
            write: function (value) { self.transitiveReplicationBehavior(value ? 'Replicate' : 'None'); },
            owner: self
        });
        self.transitiveState = ko.computed(function () {
            var msg = "Transitive: " + (self.currentlyTransitive ? 'ON' : 'OFF');
            if (self.currentlyTransitive != self.isTransitive()) {
                if (self.currentlyTransitive) {
                    msg += ' - will be OFF after Publish';
                } else {
                    msg += ' - will be ON after Publish';
                }
            }
            return msg;
        });

        self.getStats = ko.computed(function() {
            var store = brnkly.raven.getStore(self.storeName);
            if (!store || !store.stats() || !store.stats().replication) { return ''; }

            var instStats = Enumerable.From(store.stats().replication())
                .Where(function (inst) { return inst.self().toLowerCase() == self.instanceUrl.toLowerCase(); })
                .FirstOrDefault();
            if (!instStats) { return null; }

            var myStats = Enumerable.From(instStats.stats())
                .Where(function (dest) { return dest.url().toLowerCase() == self.url().toLowerCase(); })
                .FirstOrDefault();

            return { sourceLatestEtag: instStats.mostRecentDocumentEtag(), replication: myStats };
        });

        self.status = ko.computed(function () {
            var myStats = self.getStats();
            if (!myStats || !myStats.replication) { 
                return '';
            } else if (myStats.replication.failureCount() > 0) {
                return 'Error';
            } else if (myStats.sourceLatestEtag == myStats.replication.lastEtagCheckedForReplication()) {
                return 'Current';
            } else {
                var lagValue = getLagValue(new Date(myStats.replication.lastReplicatedLastModified()), 2);
                return lagValue;
            }
        });

        self.statusClass = ko.computed(function () {
            var sts = self.status();
            if (self.currentlyReplicating == self.disabled()) {
                return self.currentlyReplicating ? 'inverse' : 'info';
            } else if (self.currentlyTransitive != self.isTransitive()) {
                return 'info';
            } else if (sts == '' || self.disabled()) {
                return '';
            } else if (sts == 'Current') {
                return 'success';
            } else if (sts[sts.length - 1] == 's' && parseInt(sts) < 10) {
                return 'warning';
            } else {
                return 'danger';
            }
        });
    };

    function StoreStatsVM(data) {
        var self = this;
        var mapping = {
            'indexing': {
                create: function (options) {
                    return new IndexStatsVM(options.data);
                }
            },
        };
        ko.mapping.fromJS(data, mapping, self);
    }

    function IndexStatsVM(data) {
        var self = this;
        var mapping = {
            'instances': {
                create: function (options) {
                    return new InstanceIndexStatsVM(options.data);
                }
            }
        };
        ko.mapping.fromJS(data, mapping, self);

        self.fromInstanceUrl = ko.observable();
        self.fromInstanceDisplayName = ko.computed(function () {
            return getDisplayName(self.fromInstanceUrl())
        });
        self.copy = function (fromInstanceUrl) {
            self.fromInstanceUrl(fromInstanceUrl);
        };

        self.paste = function (toInstanceUrl) {
            if (!self.fromInstanceUrl()) {
                message('warning', 'No index instance has been copied');
                return;
            }
            $.ajax({
                url: '/api/raven/indexing/copy',
                type: 'POST',
                data: ko.toJSON({
                    fromInstanceUrl: self.fromInstanceUrl(),
                    toInstanceUrl: toInstanceUrl,
                    indexName: self.name()
                }),
                contentType: "application/json;charset=utf-8",
                success: function (data) {
                    message('success', 'Index definition copied.');
                },
                error: function (data) {
                    var result = JSON.parse(data.responseText);
                    message('danger', 'Copy of index defintion failed', result.exceptionMessage, true);
                }
            });
        };

        self.reset = function (instanceUrl) {
            $.ajax({
                url: '/api/raven/indexing/reset',
                type: 'POST',
                data: ko.toJSON({
                    instanceUrl: instanceUrl,
                    indexName: self.name()
                }),
                contentType: "application/json;charset=utf-8",
                success: function (data) {
                    message('success', 'Index reset.');
                },
                error: function (data) {
                    var result = JSON.parse(data.responseText);
                    message('danger', 'Index reset failed', result.exceptionMessage, true);
                }
            });
        };

        self.delete = function (instanceUrl) {
            $.ajax({
                url: '/api/raven/indexing/delete',
                type: 'DELETE',
                data: ko.toJSON({
                    instanceUrl: instanceUrl,
                    indexName: self.name()
                }),
                contentType: "application/json;charset=utf-8",
                success: function (data) {
                    message('success', 'Index deleted.');
                },
                error: function (data) {
                    var result = JSON.parse(data.responseText);
                    message('danger', 'Index delete failed', result.exceptionMessage, true);
                }
            });
        };
    };

    function InstanceIndexStatsVM(data) {
        var self = this;
        ko.mapping.fromJS(data, {}, self);
        self.displayName = getDisplayName(data.url);

        self.status = ko.computed(function () {
            if (!self.exists()) {
                return 'Missing';
            } else if (!self.isStale()) { 
                return 'Current'; 
            } else if (self.lastReducedTimestamp()) {
                return getLagValue(new Date(self.lastReducedTimestamp()), 2);
            } else {
                return getLagValue(new Date(self.lastIndexedTimestamp()), 2);
            }
        });

        self.statusClass = ko.computed(function () {
            var sts = self.status();
            if (sts == 'Missing') {
                return 'danger';
            } else if (sts == 'Current') {
                return 'success';
            } else if (sts[sts.length - 1] == 's' && parseInt(sts) < 10) {
                return 'warning';
            } else {
                return 'danger';
            }
        });
    };

    function loadRavenConfig() {
        $('#working').show();
        brnkly.loading = ko.observable(true);

        $.get("/api/raven/replication/live", function (data) {
            if (!data) { data = {}; }
            if (!data.stores) { data.stores = []; }

            brnkly.raven = new RavenConfigVM(data);

            $.get("/api/raven/replication/pending", function (data) {
                ko.mapping.fromJS(data, {}, brnkly.raven);
                if (brnkly.raven.stores().length > 0) {

                    brnkly.raven.activeStore(brnkly.raven.stores()[0]);

                    var enumerableStores = Enumerable.From(brnkly.raven.stores());
                    if (window.location.hash.length > 0) {
                        var storeName = window.location.hash.substring(1);
                        var store = enumerableStores.Where(function (s) {
                                return s.name().toLowerCase() == storeName.toLowerCase()
                        }).FirstOrDefault(null);
                        if (store){
                            brnkly.raven.activeStore(store);
                        }
                    }

                    enumerableStores.ForEach(function (store) {
                        store.getStats();
                        setInterval(store.getStats, 15000);
                    });
                }

                brnkly.raven.dirtyFlag = new ko.dirtyFlag(brnkly.raven.stores);
                ko.applyBindings(brnkly.raven.stores);
            });
        });

        brnkly.loading(false);
        $('#working').hide();
    }

    loadRavenConfig();
});
