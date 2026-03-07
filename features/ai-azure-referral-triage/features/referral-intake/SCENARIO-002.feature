Feature: Accept Valid Text Referral Document

  Scenario: HTTP intake accepts valid plain-text referral document and returns ReferralId
    Given an HTTP POST endpoint at "/referrals/intake"
    And a valid plain-text referral document (500 KB) with clinic notes
    When the POST request is sent with the text file attached
    Then the HTTP response status is 201
    And the response body contains a valid UUID in the "referralId" field
    And the response body contains a valid RFC3339 timestamp in the "timestamp" field
    And the raw text document is stored in Blob Storage at path "/referrals/incoming/{referralId}"
    And a ReferralReceived domain event is emitted with the ReferralId in the event payload
