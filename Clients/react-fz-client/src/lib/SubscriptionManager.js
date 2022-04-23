import Constants from "../constants";
import { SessionState } from "../components/SessionContext";

export default class SubscriptionManager {
  static _regionId = 0;
  static _session = null;
  static _settings = null;
  static _subscribed = null;

  constructor(session, regionId) {
    SubscriptionManager._session = session;
    SubscriptionManager._regionId = regionId;
  }

  async getSettings() {
    if (SubscriptionManager._settings) {
      return SubscriptionManager._settings;
    }

    const result = await SubscriptionManager._session.authFetch(Constants.subscriptionApi.SETTINGS_URL,
                                                 'GET',
                                                 null,
                                                 true,
                                                 (response) => {},
                                                 (message) => { throw message; });
    SubscriptionManager._settings = result;
    return SubscriptionManager._settings;
  }

  //$ TODO: Error handling?
  async updateSettings(newSettings) {
    SubscriptionManager._settings = newSettings;
    await SubscriptionManager._session.authFetch(Constants.subscriptionApi.SETTINGS_URL,
                                  'POST',
                                  JSON.stringify(newSettings),
                                  true,
                                  () => {},
                                  (message) => { throw message; });
  }

  async getSubscribedGages() {
    if (SubscriptionManager._subscribed) {
      return SubscriptionManager._subscribed;
    }
    if (SubscriptionManager._session.sessionState !== SessionState.LOGGED_IN) {
      return [];
    }
    try {
      const result = await SubscriptionManager._session.authFetch(
        this.getSubscriptionsUrl(),
        "GET",
        null,
        true,
        (response) => {},
        (message) => {
          throw message;
        }
      );
      SubscriptionManager._subscribed = result;
      return SubscriptionManager._subscribed;
    } catch (e) {
      console.warn("failed to get subscription data", e);
      return [];
    }
  }

  async setGageSubscription(gageid, enabled) {
    if (SubscriptionManager._subscribed === null) {
      await this.getSubscribedGages();
    }
    const subscribed = SubscriptionManager._subscribed.includes(gageid);

    // Yes, this could be simplified, but I think this intent is clearer...
    if ((subscribed && enabled) || (!subscribed && !enabled)) {
      return;
    }

    await SubscriptionManager._session.authFetch(this.getSubscriptionsUrl() + '/' + gageid,
                                  'PUT',
                                  enabled,
                                  true,
                                  (response) => {},
                                  (message) => { throw message; });
    if (enabled) {
      SubscriptionManager._subscribed.push(gageid);
    } else {
      const index = SubscriptionManager._subscribed.indexOf(gageid);
      // don't need to check index, but it doesn't hurt
      if (index !== -1) {
        SubscriptionManager._subscribed.splice(index, 1);
      }
    }
  }

  async isSubscribed(gageid) {
    return (await this.getSubscribedGages()).includes(gageid);
  }

  getSubscriptionsUrl() {
    return Constants.subscriptionApi.SUBSCRIPTIONS_URL + '/' + SubscriptionManager._regionId;
  }
}
