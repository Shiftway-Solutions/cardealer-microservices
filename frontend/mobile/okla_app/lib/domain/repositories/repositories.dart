import 'package:okla_app/core/errors/failures.dart';
import 'package:okla_app/domain/entities/models.dart';
import 'package:okla_app/domain/entities/vehicle.dart';

/// Favorites repository
abstract class FavoritesRepository {
  Future<(List<Vehicle>?, Failure?)> getFavorites();
  Future<(bool, Failure?)> addFavorite(String vehicleId);
  Future<(bool, Failure?)> removeFavorite(String vehicleId);
  Future<(bool, Failure?)> isFavorite(String vehicleId);
}

/// Messaging repository
abstract class MessagingRepository {
  Future<(List<Conversation>?, Failure?)> getConversations();
  Future<(List<ChatMessage>?, Failure?)> getMessages(String conversationId);
  Future<(ChatMessage?, Failure?)> sendMessage(String conversationId, String content);
  Future<(bool, Failure?)> markAsRead(String conversationId);
  Future<(int, Failure?)> getUnreadCount();
  Future<(Conversation?, Failure?)> startConversation(String targetUserId, {String? vehicleId, String? initialMessage});
}

/// Notifications repository
abstract class NotificationsRepository {
  Future<(List<AppNotification>?, Failure?)> getNotifications({int page = 1, int pageSize = 20});
  Future<(int, Failure?)> getUnreadCount();
  Future<(bool, Failure?)> markAsRead(String notificationId);
  Future<(bool, Failure?)> markAllAsRead();
  Future<(bool, Failure?)> deleteNotification(String notificationId);
  Future<(bool, Failure?)> updatePreferences(Map<String, bool> preferences);
}

/// Reviews repository
abstract class ReviewsRepository {
  Future<(List<Review>?, Failure?)> getReviews(String targetId, {int page = 1});
  Future<(Review?, Failure?)> createReview({
    required String targetId,
    required String targetType,
    required int rating,
    String? comment,
  });
  Future<(bool, Failure?)> respondToReview(String reviewId, String response);
  Future<(bool, Failure?)> voteHelpful(String reviewId);
  Future<(bool, Failure?)> reportReview(String reviewId, String reason);
}

/// Alerts repository
abstract class AlertsRepository {
  Future<(List<PriceAlert>?, Failure?)> getPriceAlerts();
  Future<(PriceAlert?, Failure?)> createPriceAlert({
    required String vehicleId,
    required double targetPrice,
  });
  Future<(bool, Failure?)> deletePriceAlert(String alertId);
  Future<(bool, Failure?)> togglePriceAlert(String alertId, bool isActive);
}

/// Dealers repository
abstract class DealersRepository {
  Future<(List<Dealer>?, Failure?)> getDealers({int page = 1, String? province});
  Future<(Dealer?, Failure?)> getDealerBySlug(String slug);
  Future<(Dealer?, Failure?)> getCurrentDealer();
  Future<(Map<String, dynamic>?, Failure?)> getDealerStats();
  Future<(Map<String, dynamic>?, Failure?)> getDealerAnalytics({String period = '30d'});
}
