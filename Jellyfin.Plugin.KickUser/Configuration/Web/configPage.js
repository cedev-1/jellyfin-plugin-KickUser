const KickUserConfig = {
    pluginUniqueId: 'a3c2e4f1-8b6d-4e2a-9f7c-1d3e5a8b9c4f'
};

export default function (view) {
    let allUsers = [];

    function loadUsers() {
        return new Promise(function (resolve, reject) {

            if (typeof ApiClient === 'undefined') {
                reject('ApiClient is undefined');
                return;
            }

            if (typeof ApiClient.getUsers === 'function') {
                ApiClient.getUsers().then(function (users) {
                    allUsers = users || [];
                    resolve(allUsers);
                }).catch(function (err) {
                    fetchUsersFallback(resolve, reject);
                });
            } else {
                fetchUsersFallback(resolve, reject);
            }
        });
    }

    function fetchUsersFallback(resolve, reject) {
        const publicUrl = ApiClient.getUrl('Users/Public');

        ApiClient.getJSON(publicUrl).then(function (users) {
            allUsers = users || [];
            resolve(allUsers);
        }).catch(function (err) {

            const adminUrl = ApiClient.getUrl('Users');

            ApiClient.getJSON(adminUrl).then(function (users) {
                allUsers = users || [];
                resolve(allUsers);
            }).catch(function (finalErr) {
                resolve([]);
            });
        });
    }

    function renderUserList(whitelistedIds) {
        const container = view.querySelector('#userWhitelistContainer');
        whitelistedIds = whitelistedIds || [];

        if (!allUsers || allUsers.length === 0) {
            container.innerHTML = '<p style="text-align: center; padding: 20px; color: #999;">No users found.</p>';
            return;
        }

        let html = '<div style="display: flex; flex-direction: column; gap: 8px;">';

        allUsers.forEach(function (user) {
            const userId = user.Id || user.id;
            const userName = user.Name || user.name;
            const isAdmin = user.Policy && user.Policy.IsAdministrator;
            const isChecked = whitelistedIds.includes(userId) || isAdmin;
            const disabledAttr = isAdmin ? 'disabled' : '';
            const adminBadge = isAdmin ? ' <span style="opacity:0.7; font-size:0.8em; margin-left: 5px;">(Admin - Auto Whitelisted)</span>' : '';

            html += `
                <div class="checkboxContainer" style="padding: 10px; background-color: rgba(255,255,255,0.05); border-radius: 4px; display: flex; align-items: center;">
                    <label class="emby-checkbox-label">
                        <input type="checkbox" 
                               is="emby-checkbox" 
                               class="userWhitelistCheckbox" 
                               data-user-id="${userId}" 
                               ${isChecked ? 'checked' : ''} 
                               ${disabledAttr} />
                        <span style="margin-left: 0.5em;">${userName}${adminBadge}</span>
                    </label>
                </div>
            `;
        });

        html += '</div>';
        container.innerHTML = html;
    }

    view.addEventListener('viewshow', function () {
        Dashboard.showLoadingMsg();

        Promise.all([
            loadUsers(),
            ApiClient.getPluginConfiguration(KickUserConfig.pluginUniqueId)
        ]).then(function (results) {
            const users = results[0];
            const config = results[1] || {};

            document.getElementById('enablePlugin').checked = config.EnablePlugin || false;
            document.getElementById('inactivityThresholdDays').value = config.InactivityThresholdDays || 30;
            document.getElementById('actionType').value = config.ActionType || 'Disable';
            document.getElementById('checkHour').value = config.CheckHour !== undefined ? config.CheckHour : 3;
            document.getElementById('dryRun').checked = config.DryRun || false;

            const whitelistedIds = config.WhitelistedUserIds || [];

            if (allUsers.length === 0) {
                const container = view.querySelector('#userWhitelistContainer');
                container.innerHTML = '<div style="padding:10px; color: #e66;">Could not load users.</div>';
            } else {
                renderUserList(whitelistedIds);
            }

            Dashboard.hideLoadingMsg();
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            Dashboard.alert({
                message: 'Error loading plugin configuration or users.',
                title: 'Plugin Error'
            });
        });
    });

    view.querySelector('#KickUserConfigForm').addEventListener('submit', function (e) {
        e.preventDefault();
        Dashboard.showLoadingMsg();

        ApiClient.getPluginConfiguration(KickUserConfig.pluginUniqueId).then(function (config) {
            config = config || {};
            config.EnablePlugin = document.getElementById('enablePlugin').checked;
            config.InactivityThresholdDays = parseInt(document.getElementById('inactivityThresholdDays').value) || 30;
            config.ActionType = document.getElementById('actionType').value;
            config.CheckHour = parseInt(document.getElementById('checkHour').value) || 3;
            config.DryRun = document.getElementById('dryRun').checked;

            const checkboxes = view.querySelectorAll('.userWhitelistCheckbox:not([disabled])');
            config.WhitelistedUserIds = Array.from(checkboxes)
                .filter(cb => cb.checked)
                .map(cb => cb.getAttribute('data-user-id'));

            ApiClient.updatePluginConfiguration(KickUserConfig.pluginUniqueId, config).then(function (result) {
                Dashboard.processPluginConfigurationUpdateResult(result);
            }).catch(function (err) {
                Dashboard.hideLoadingMsg();
                Dashboard.alert('Error saving configuration.');
            });
        }).catch(function (err) {
            Dashboard.hideLoadingMsg();
            Dashboard.alert('Error updating configuration.');
        });

        return false;
    });
}
