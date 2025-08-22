using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prelevements_par_caisse.Data;
using Prelevements_par_caisse.DTOs;
using Prelevements_par_caisse.Models;

namespace Prelevements_par_caisse.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminDemandesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminDemandesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDemandes()
        {
            try
            {
                var demandes = await _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Categorie)
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Include(d => d.Paiement)
                    .Select(d => new
                    {
                        id = d.Id,
                        statut = d.Statut.ToString(),
                        dateDemande = d.DateDemande,
                        utilisateur = new
                        {
                            nom = d.Utilisateur.Nom,
                            prenom = d.Utilisateur.Prenom,
                            email = d.Utilisateur.Email
                        },
                        categorie = new
                        {
                            nom = d.Categorie.Nom
                        },
                        items = d.DemandeItems.Select(di => new
                        {
                            id = di.Id,
                            nom = di.Item.Nom,
                            quantite = di.Quantite,
                            prixUnitaire = di.PrixUnitaire
                        }),
                        demandeItems = d.DemandeItems.Select(di => new
                        {
                            id = di.Id,
                            quantite = di.Quantite,
                            prixUnitaire = di.PrixUnitaire,
                            description = di.Description,
                            item = new
                            {
                                id = di.Item.Id,
                                nom = di.Item.Nom
                            }
                        }),
                        paiement = d.Paiement != null ? new
                        {
                            montantTotal = d.Paiement.MontantTotal,
                            comptePaiement = d.Paiement.ComptePaiement,
                            montantEnLettres = d.Paiement.MontantEnLettres,
                            effectuePar = d.Paiement.EffectuePar,
                            datePaiement = d.Paiement.DatePaiement
                        } : null
                    })
                    .ToListAsync();

                return Ok(demandes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDemande(Guid id)
        {
            try
            {
                var demande = await _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Categorie)
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Include(d => d.Paiement)
                    .Select(d => new
                    {
                        id = d.Id,
                        statut = d.Statut.ToString(),
                        dateDemande = d.DateDemande,
                        utilisateur = new
                        {
                            nom = d.Utilisateur.Nom,
                            prenom = d.Utilisateur.Prenom,
                            email = d.Utilisateur.Email,
                        },
                        categorie = new
                        {
                            nom = d.Categorie.Nom
                        },
                        demandeItems = d.DemandeItems.Select(di => new
                        {
                            id = di.Id,
                            quantite = di.Quantite,
                            prixUnitaire = di.PrixUnitaire,
                            description = di.Description,
                            item = new
                            {
                                id = di.Item.Id,
                                nom = di.Item.Nom
                            }
                        }),
                        paiement = d.Paiement != null ? new
                        {
                            montantTotal = d.Paiement.MontantTotal,
                            comptePaiement = d.Paiement.ComptePaiement,
                            montantEnLettres = d.Paiement.MontantEnLettres,
                            effectuePar = d.Paiement.EffectuePar,
                            datePaiement = d.Paiement.DatePaiement
                        } : null
                    })
                    .FirstOrDefaultAsync(d => d.id == id);

                if (demande == null)
                    return NotFound(new { message = "Demande introuvable" });

                return Ok(demande);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpPut("valider/{id}")]
        public async Task<IActionResult> ValiderDemande(Guid id, [FromBody] PaiementValidationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var demande = await _context.Demandes
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Include(d => d.Paiement)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (demande == null)
                    return NotFound(new { message = "Demande introuvable" });

                // Update prices for each demande item
                if (dto.DemandeItems != null && dto.DemandeItems.Any())
                {
                    foreach (var dtoItem in dto.DemandeItems)
                    {
                        var demandeItem = demande.DemandeItems.FirstOrDefault(di => di.Id == dtoItem.Id);
                        if (demandeItem != null && dtoItem.PrixUnitaire.HasValue)
                        {
                            demandeItem.PrixUnitaire = dtoItem.PrixUnitaire.Value;
                        }
                        if (!string.IsNullOrEmpty(dtoItem.Description))
                        {
                            demandeItem.Description = dtoItem.Description;
                        }
                    }
                }

                // Change status
                demande.Statut = StatutDemande.Validee;

                // Calculate total amount using the prices set by admin
                var montantTotal = demande.DemandeItems.Sum(di =>
                    di.Quantite * (di.PrixUnitaire ?? 0));

                // Create payment
                var paiement = new Paiement
                {
                    DemandeId = demande.Id,
                    MontantTotal = montantTotal,
                    DatePaiement = DateTime.Now,
                    EffectuePar = dto.EffectuePar,
                    ComptePaiement = dto.ComptePaiement,
                    MontantEnLettres = dto.MontantEnLettres
                };

                _context.Paiements.Add(paiement);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Demande validée et paiement généré" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateDemande(Guid id, [FromBody] PaiementValidationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var demande = await _context.Demandes
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Include(d => d.Paiement)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (demande == null)
                    return NotFound(new { message = "Demande introuvable" });

                // Update prices for each demande item if provided
                if (dto.DemandeItems != null && dto.DemandeItems.Any())
                {
                    foreach (var dtoItem in dto.DemandeItems)
                    {
                        var demandeItem = demande.DemandeItems.FirstOrDefault(di => di.Id == dtoItem.Id);
                        if (demandeItem != null && dtoItem.PrixUnitaire.HasValue)
                        {
                            demandeItem.PrixUnitaire = dtoItem.PrixUnitaire.Value;
                        }
                        if (demandeItem != null)
                        {
                            demandeItem.Description = dtoItem.Description;
                        }
                    }
                }

                // Update status if provided
                if (!string.IsNullOrEmpty(dto.Statut))
                {
                    if (Enum.TryParse<StatutDemande>(dto.Statut, true, out var newStatus))
                    {
                        demande.Statut = newStatus;
                    }
                    else
                    {
                        return BadRequest(new { message = "Statut invalide" });
                    }
                }

                // Calculate total amount using the current prices
                var montantTotal = demande.DemandeItems.Sum(di =>
                    di.Quantite * (di.PrixUnitaire ?? 0));

                // Update or create payment
                if (demande.Paiement != null)
                {
                    demande.Paiement.ComptePaiement = dto.ComptePaiement;
                    demande.Paiement.MontantEnLettres = dto.MontantEnLettres;
                    demande.Paiement.MontantTotal = montantTotal;
                    demande.Paiement.DatePaiement = DateTime.Now;
                    demande.Paiement.EffectuePar = dto.EffectuePar;
                }
                else
                {
                    var paiement = new Paiement
                    {
                        DemandeId = demande.Id,
                        MontantTotal = montantTotal,
                        DatePaiement = DateTime.Now,
                        EffectuePar = dto.EffectuePar,
                        ComptePaiement = dto.ComptePaiement,
                        MontantEnLettres = dto.MontantEnLettres
                    };
                    _context.Paiements.Add(paiement);
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = "Demande mise à jour avec succès",
                    newStatus = demande.Statut.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpPut("refuser/{id}")]
        public async Task<IActionResult> RefuserDemande(Guid id)
        {
            try
            {
                var demande = await _context.Demandes.FindAsync(id);
                if (demande == null)
                    return NotFound(new { message = "Demande introuvable" });

                demande.Statut = StatutDemande.Refusee;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Demande refusée" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDemande(Guid id)
        {
            try
            {
                var demande = await _context.Demandes
                    .Include(d => d.DemandeItems)
                    .Include(d => d.Paiement)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (demande == null)
                    return NotFound(new { message = "Demande introuvable" });

                if (demande.Paiement != null)
                {
                    _context.Paiements.Remove(demande.Paiement);
                }

                if (demande.DemandeItems != null && demande.DemandeItems.Any())
                {
                    _context.DemandeItems.RemoveRange(demande.DemandeItems);
                }

                _context.Demandes.Remove(demande);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Demande supprimée avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur lors de la suppression: {ex.Message}" });
            }
        }

        [HttpDelete("bulk")]
        public async Task<IActionResult> DeleteMultipleDemandes([FromBody] List<Guid> demandeIds)
        {
            try
            {
                if (demandeIds == null || !demandeIds.Any())
                    return BadRequest(new { message = "Aucune demande sélectionnée" });

                var demandes = await _context.Demandes
                    .Include(d => d.DemandeItems)
                    .Include(d => d.Paiement)
                    .Where(d => demandeIds.Contains(d.Id))
                    .ToListAsync();

                if (!demandes.Any())
                    return NotFound(new { message = "Aucune demande trouvée avec les IDs fournis" });

                var deletedCount = 0;

                foreach (var demande in demandes)
                {
                    if (demande.Paiement != null)
                    {
                        _context.Paiements.Remove(demande.Paiement);
                    }

                    if (demande.DemandeItems != null && demande.DemandeItems.Any())
                    {
                        _context.DemandeItems.RemoveRange(demande.DemandeItems);
                    }

                    _context.Demandes.Remove(demande);
                    deletedCount++;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"{deletedCount} demande(s) supprimée(s) avec succès",
                    deletedCount = deletedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur lors de la suppression multiple: {ex.Message}" });
            }
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            try
            {
                var totalDemandes = await _context.Demandes.CountAsync();
                var totalSpent = await _context.Paiements.SumAsync(p => (decimal?)p.MontantTotal) ?? 0;
                var totalUsers = await _context.Users.CountAsync();

                var demandesParStatut = await _context.Demandes
                    .GroupBy(d => d.Statut)
                    .Select(g => new { Statut = g.Key.ToString(), Count = g.Count() })
                    .ToDictionaryAsync(x => x.Statut, x => x.Count);

                var topUsers = await _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Paiement)
                    .Where(d => d.Utilisateur != null)
                    .GroupBy(d => d.UtilisateurId)
                    .Select(g => new
                    {
                        Id = g.Key,
                        Nom = g.First().Utilisateur.Nom ?? "",
                        Prenom = g.First().Utilisateur.Prenom ?? "",
                        Email = g.First().Utilisateur.Email ?? "",
                        TotalDemandes = g.Count(),
                        TotalSpent = g.Sum(d => d.Paiement != null ? d.Paiement.MontantTotal : 0),
                        IsFaveur = g.First().Utilisateur.Is_Faveur
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(10)
                    .ToListAsync();

                var topCategories = await _context.Demandes
                    .Include(d => d.Categorie)
                    .Include(d => d.Paiement)
                    .Where(d => d.Categorie != null)
                    .GroupBy(d => d.CategorieId)
                    .Select(g => new
                    {
                        Id = g.Key,
                        Nom = g.First().Categorie.Nom ?? "",
                        TotalDemandes = g.Count(),
                        TotalSpent = g.Sum(d => d.Paiement != null ? d.Paiement.MontantTotal : 0)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(10)
                    .ToListAsync();

                // Enhanced Monthly trends with more details
                var monthlyTrends = await _context.Demandes
                    .Include(d => d.Paiement)
                    .Where(d => d.DateDemande >= DateTime.Now.AddMonths(-12))
                    .GroupBy(d => new { Year = d.DateDemande.Year, Month = d.DateDemande.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        TotalDemandes = g.Count(),
                        TotalSpent = g.Sum(d => d.Paiement != null ? d.Paiement.MontantTotal : 0),
                        ApprovedCount = g.Count(d => d.Statut == StatutDemande.Validee),
                        PendingCount = g.Count(d => d.Statut == StatutDemande.EnAttente),
                        RejectedCount = g.Count(d => d.Statut == StatutDemande.Refusee),
                        AverageAmount = g.Where(d => d.Paiement != null).Any()
                            ? g.Where(d => d.Paiement != null).Average(d => d.Paiement.MontantTotal)
                            : 0
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                // Daily activity for the last 30 days
                var dailyActivity = await _context.Demandes
                    .Include(d => d.Paiement)
                    .Where(d => d.DateDemande >= DateTime.Now.AddDays(-30))
                    .GroupBy(d => d.DateDemande.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count(),
                        Amount = g.Sum(d => d.Paiement != null ? d.Paiement.MontantTotal : 0)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Status distribution over time
                var statusTrends = await _context.Demandes
                    .Where(d => d.DateDemande >= DateTime.Now.AddMonths(-6))
                    .GroupBy(d => new
                    {
                        Year = d.DateDemande.Year,
                        Month = d.DateDemande.Month,
                        Status = d.Statut
                    })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Status = g.Key.Status.ToString(),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                // Top items with enhanced data - Fixed potential null reference
                var topItems = await _context.DemandeItems
                    .Include(di => di.Item)
                    .Include(di => di.Demande)
                        .ThenInclude(d => d.Paiement)
                    .Where(di => di.Demande.DateDemande >= DateTime.Now.AddMonths(-6) && di.Item != null)
                    .GroupBy(di => di.ItemId)
                    .Select(g => new
                    {
                        Id = g.Key,
                        Nom = g.First().Item.Nom ?? "",
                        TotalQuantity = g.Sum(di => di.Quantite),
                        TotalOrders = g.Count(),
                        TotalValue = g.Sum(di => (di.PrixUnitaire ?? 0) * di.Quantite),
                        AveragePrice = g.Where(di => di.PrixUnitaire.HasValue).Any()
                            ? g.Where(di => di.PrixUnitaire.HasValue).Average(di => di.PrixUnitaire.Value)
                            : 0,
                        LastOrderDate = g.Max(di => di.Demande.DateDemande)
                    })
                    .OrderByDescending(x => x.TotalValue)
                    .Take(10)
                    .ToListAsync();

                // Department/Category performance
                var categoryPerformance = await _context.Demandes
                    .Include(d => d.Categorie)
                    .Include(d => d.Paiement)
                    .Where(d => d.DateDemande >= DateTime.Now.AddMonths(-3) && d.Categorie != null)
                    .GroupBy(d => d.CategorieId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        CategoryName = g.First().Categorie.Nom ?? "",
                        TotalDemandes = g.Count(),
                        TotalSpent = g.Sum(d => d.Paiement != null ? d.Paiement.MontantTotal : 0),
                        ApprovalRate = g.Any() ? (double)g.Count(d => d.Statut == StatutDemande.Validee) / g.Count() * 100 : 0
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .ToListAsync();

                // User activity patterns - Fixed to work with EF Core
                var userActivityPatterns = await _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Paiement)
                    .Where(d => d.DateDemande >= DateTime.Now.AddMonths(-3))
                    .ToListAsync(); // Load to memory first

                var activityPatternsByHour = userActivityPatterns
                    .GroupBy(d => d.DateDemande.Hour)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        Count = g.Count(),
                        AverageAmount = g.Where(d => d.Paiement != null).Any()
                            ? g.Where(d => d.Paiement != null).Average(d => d.Paiement.MontantTotal)
                            : 0
                    })
                    .OrderBy(x => x.Hour)
                    .ToList();

                // Peak usage analysis - Fixed to work with EF Core
                var weekdayAnalysis = userActivityPatterns
                    .GroupBy(d => d.DateDemande.DayOfWeek)
                    .Select(g => new
                    {
                        DayOfWeek = g.Key.ToString(),
                        Count = g.Count(),
                        AverageAmount = g.Where(d => d.Paiement != null).Any()
                            ? g.Where(d => d.Paiement != null).Average(d => d.Paiement.MontantTotal)
                            : 0
                    })
                    .ToList();

                var recentDemandes = await _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Categorie)
                    .Include(d => d.Paiement)
                    .Include(d => d.DemandeItems)
                    .Where(d => d.Utilisateur != null && d.Categorie != null)
                    .OrderByDescending(d => d.DateDemande)
                    .Take(10)
                    .Select(d => new
                    {
                        Id = d.Id,
                        DateDemande = d.DateDemande,
                        Statut = d.Statut.ToString(),
                        Utilisateur = new
                        {
                            Nom = d.Utilisateur.Nom ?? "",
                            Prenom = d.Utilisateur.Prenom ?? "",
                            IsFaveur = d.Utilisateur.Is_Faveur
                        },
                        Categorie = new
                        {
                            Nom = d.Categorie.Nom ?? ""
                        },
                        MontantTotal = d.Paiement != null ? d.Paiement.MontantTotal : 0,
                        ItemsCount = d.DemandeItems.Count
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalDemandes = totalDemandes,
                    TotalSpent = totalSpent,
                    TotalUsers = totalUsers,
                    DemandesParStatut = demandesParStatut,
                    TopUsers = topUsers,
                    TopCategories = topCategories,
                    RecentDemandes = recentDemandes,
                    MonthlyTrends = monthlyTrends,
                    TopItems = topItems,
                    DailyActivity = dailyActivity,
                    StatusTrends = statusTrends,
                    CategoryPerformance = categoryPerformance,
                    UserActivityPatterns = activityPatternsByHour,
                    WeekdayAnalysis = weekdayAnalysis
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        // Fixed advanced analytics endpoint
        [HttpGet("analytics/advanced")]
        public async Task<IActionResult> GetAdvancedAnalytics()
        {
            try
            {
                // Financial insights
                var currentYear = DateTime.Now.Year;
                var yearlyComparison = new List<object>();

                for (int year = currentYear - 2; year <= currentYear; year++)
                {
                    var yearData = await _context.Demandes
                        .Include(d => d.Paiement)
                        .Where(d => d.DateDemande.Year == year)
                        .Select(d => new
                        {
                            UtilisateurId = d.UtilisateurId,
                            MontantTotal = d.Paiement != null ? d.Paiement.MontantTotal : 0
                        })
                        .ToListAsync();

                    yearlyComparison.Add(new
                    {
                        Year = year,
                        TotalDemandes = yearData.Count,
                        TotalSpent = yearData.Sum(d => d.MontantTotal),
                        AverageAmount = yearData.Any() ? yearData.Average(d => d.MontantTotal) : 0,
                        UniqueUsers = yearData.Select(d => d.UtilisateurId).Distinct().Count()
                    });
                }

                // User engagement metrics - Simplified
                var userEngagement = new List<object>();
                try
                {
                    var users = await _context.Users.Take(20).ToListAsync();
                    foreach (var user in users)
                    {
                        var userDemandes = await _context.Demandes
                            .Where(d => d.UtilisateurId == user.Id)
                            .Include(d => d.Paiement)
                            .ToListAsync();

                        if (userDemandes.Any())
                        {
                            userEngagement.Add(new
                            {
                                UserId = user.Id,
                                UserName = $"{user.Nom} {user.Prenom}",
                                IsFaveur = user.Is_Faveur,
                                TotalDemandes = userDemandes.Count,
                                LastActivity = userDemandes.Max(d => d.DateDemande),
                                TotalSpent = userDemandes.Sum(d => d.Paiement?.MontantTotal ?? 0),
                                AvgMonthlyDemandes = userDemandes.Count(d => d.DateDemande >= DateTime.Now.AddMonths(-12)) / 12.0
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"User engagement error: {ex.Message}");
                }

                // Predictive insights
                var last6MonthsData = await _context.Demandes
                    .Include(d => d.Paiement)
                    .Where(d => d.DateDemande >= DateTime.Now.AddMonths(-6))
                    .ToListAsync();

                var predictiveInsights = new
                {
                    AvgMonthlyDemandes = last6MonthsData.Count / 6.0,
                    AvgMonthlySpent = last6MonthsData.Sum(d => d.Paiement?.MontantTotal ?? 0) / 6.0m,
                    ProjectedNextMonth = last6MonthsData.Count > 0 ? last6MonthsData.Count / 6.0 * 1.1 : 0,
                    TrendDirection = last6MonthsData.Count > 0 ? "croissant" : "stable"
                };

                // Financial insights for the insights tab
                var highValueRequests = await _context.Demandes
                    .Include(d => d.Paiement)
                    .Where(d => d.Paiement != null && d.Paiement.MontantTotal > 1000)
                    .CountAsync();

                var averageRequestValue = await _context.Paiements.AnyAsync()
                    ? await _context.Paiements.AverageAsync(p => p.MontantTotal)
                    : 0;

                var currentMonth = DateTime.Now;
                var lastMonth = currentMonth.AddMonths(-1);

                var currentMonthSpent = await _context.Paiements
                    .Where(p => p.DatePaiement.Year == currentMonth.Year && p.DatePaiement.Month == currentMonth.Month)
                    .SumAsync(p => (decimal?)p.MontantTotal) ?? 0;

                var lastMonthSpent = await _context.Paiements
                    .Where(p => p.DatePaiement.Year == lastMonth.Year && p.DatePaiement.Month == lastMonth.Month)
                    .SumAsync(p => (decimal?)p.MontantTotal) ?? 0;

                var monthlyGrowthRate = lastMonthSpent > 0
                    ? ((double)(currentMonthSpent - lastMonthSpent) / (double)lastMonthSpent) * 100
                    : 0;

                // Top spending departments
                var topSpendingDepartments = await _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Paiement)
                    .Where(d => d.DateDemande >= DateTime.Now.AddMonths(-6) && d.Utilisateur != null)
                    .GroupBy(d => d.Utilisateur.Is_Faveur)
                    .Select(g => new
                    {
                        Department = g.Key ? "Faveur" : "Regular",
                        RequestCount = g.Count(),
                        TotalSpent = g.Sum(d => d.Paiement != null ? d.Paiement.MontantTotal : 0)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .ToListAsync();

                // Activity patterns - Fixed by loading to memory first
                var activityData = await _context.Demandes
                    .Where(d => d.DateDemande >= DateTime.Now.AddMonths(-3))
                    .ToListAsync();

                var activityPatterns = activityData
                    .GroupBy(d => new
                    {
                        DayOfWeek = d.DateDemande.DayOfWeek,
                        Hour = d.DateDemande.Hour
                    })
                    .Select(g => new
                    {
                        DayOfWeek = g.Key.DayOfWeek.ToString(),
                        Hour = g.Key.Hour,
                        RequestCount = g.Count()
                    })
                    .ToList();

                return Ok(new
                {
                    YearlyComparison = yearlyComparison,
                    UserEngagement = userEngagement.OrderByDescending(x => ((dynamic)x).TotalSpent).Take(20),
                    PredictiveInsights = predictiveInsights,
                    FinancialInsights = new
                    {
                        HighValueRequests = highValueRequests,
                        AverageRequestValue = averageRequestValue,
                        MonthlyGrowthRate = monthlyGrowthRate,
                        TopSpendingDepartments = topSpendingDepartments
                    },
                    ActivityPatterns = activityPatterns
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        // Fixed performance endpoint
        [HttpGet("analytics/performance")]
        public async Task<IActionResult> GetPerformanceMetrics()
        {
            try
            {
                var totalDemandes = await _context.Demandes.CountAsync();
                var approvedDemandes = await _context.Demandes.CountAsync(d => d.Statut == StatutDemande.Validee);
                var rejectedDemandes = await _context.Demandes.CountAsync(d => d.Statut == StatutDemande.Refusee);
                var pendingDemandes = await _context.Demandes.CountAsync(d => d.Statut == StatutDemande.EnAttente);

                var approvalRate = totalDemandes > 0 ? (double)approvedDemandes / totalDemandes * 100 : 0;
                var rejectionRate = totalDemandes > 0 ? (double)rejectedDemandes / totalDemandes * 100 : 0;

                // Calculate average processing time
                var processedDemandes = await _context.Demandes
                    .Include(d => d.Paiement)
                    .Where(d => d.Statut == StatutDemande.Validee && d.Paiement != null)
                    .Select(d => new
                    {
                        RequestDate = d.DateDemande,
                        ProcessedDate = d.Paiement.DatePaiement
                    })
                    .ToListAsync();

                var avgProcessingTime = processedDemandes.Any()
                    ? processedDemandes.Average(d => (d.ProcessedDate - d.RequestDate).TotalDays)
                    : 0;

                // Weekly comparison
                var thisWeekStart = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);
                var lastWeekStart = thisWeekStart.AddDays(-7);

                var thisWeekData = await _context.Demandes
                    .Include(d => d.Paiement)
                    .Where(d => d.DateDemande >= thisWeekStart)
                    .ToListAsync();

                var lastWeekData = await _context.Demandes
                    .Include(d => d.Paiement)
                    .Where(d => d.DateDemande >= lastWeekStart && d.DateDemande < thisWeekStart)
                    .ToListAsync();

                var weeklyComparison = new[]
                {
                    new {
                        Period = "This Week",
                        Count = thisWeekData.Count,
                        TotalValue = thisWeekData.Sum(d => d.Paiement?.MontantTotal ?? 0)
                    },
                    new {
                        Period = "Last Week",
                        Count = lastWeekData.Count,
                        TotalValue = lastWeekData.Sum(d => d.Paiement?.MontantTotal ?? 0)
                    }
                };

                // System health
                var totalUsers = await _context.Users.CountAsync();
                var activeCategories = await _context.Categories.CountAsync();
                var activeItems = await _context.Items.CountAsync();
                var lastWeekActivity = await _context.Demandes
                    .Where(d => d.DateDemande >= DateTime.Now.AddDays(-7))
                    .CountAsync();

                return Ok(new
                {
                    ApprovalRate = Math.Round(approvalRate, 1),
                    RejectionRate = Math.Round(rejectionRate, 1),
                    AvgProcessingTime = Math.Round(avgProcessingTime, 1),
                    PendingRequests = pendingDemandes,
                    WeeklyComparison = weeklyComparison,
                    SystemHealth = new
                    {
                        TotalUsers = totalUsers,
                        ActiveCategories = activeCategories,
                        ActiveItems = activeItems,
                        LastWeekActivity = lastWeekActivity
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        Id = u.Id,
                        Nom = u.Nom ?? "",
                        Prenom = u.Prenom ?? "",
                        Email = u.Email ?? ""
                    })
                    .OrderBy(u => u.Nom)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}", details = ex.InnerException?.Message });
            }
        }




 // New endpoint to get items by category
        [HttpGet("items")]
        public async Task<IActionResult> GetItems([FromQuery] Guid? categorieId = null)
        {
            try
            {
                var query = _context.Items.AsQueryable();

                // Filter by category if provided
                if (categorieId.HasValue && categorieId.Value != Guid.Empty)
                {
                    query = query.Where(i => i.CategorieId == categorieId.Value);
                }

                var items = await query
                    .Include(i => i.Categorie)
                    .Select(i => new
                    {
                        Id = i.Id,
                        Nom = i.Nom ?? "",
                      
                        CategorieId = i.CategorieId,
                        CategorieName = i.Categorie != null ? i.Categorie.Nom : "N/A"
                    })
                    .OrderBy(i => i.Nom)
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}", details = ex.InnerException?.Message });
            }
        }





























        [HttpGet("statistics/summary")]
        public async Task<IActionResult> GetStatisticsSummary()
        {
            try
            {
                var currentMonth = DateTime.Now;
                var lastMonth = currentMonth.AddMonths(-1);
                var currentYear = DateTime.Now.Year;

                // Current month stats
                var currentMonthDemandes = await _context.Demandes
                    .Where(d => d.DateDemande.Year == currentMonth.Year && d.DateDemande.Month == currentMonth.Month)
                    .CountAsync();

                var currentMonthSpent = await _context.Paiements
                    .Where(p => p.DatePaiement.Year == currentMonth.Year &&
                               p.DatePaiement.Month == currentMonth.Month)
                    .SumAsync(p => (decimal?)p.MontantTotal) ?? 0;

                // Last month stats
                var lastMonthDemandes = await _context.Demandes
                    .Where(d => d.DateDemande.Year == lastMonth.Year && d.DateDemande.Month == lastMonth.Month)
                    .CountAsync();

                var lastMonthSpent = await _context.Paiements
                    .Where(p => p.DatePaiement.Year == lastMonth.Year &&
                               p.DatePaiement.Month == lastMonth.Month)
                    .SumAsync(p => (decimal?)p.MontantTotal) ?? 0;

                // Calculate growth percentages
                var demandesGrowth = lastMonthDemandes > 0
                    ? ((double)(currentMonthDemandes - lastMonthDemandes) / lastMonthDemandes) * 100
                    : 0;

                var spentGrowth = lastMonthSpent > 0
                    ? ((double)(currentMonthSpent - lastMonthSpent) / (double)lastMonthSpent) * 100
                    : 0;

                // Year stats
                var yearlyDemandes = await _context.Demandes
                    .Where(d => d.DateDemande.Year == currentYear)
                    .CountAsync();

                var yearlySpent = await _context.Paiements
                    .Where(p => p.DatePaiement.Year == currentYear)
                    .SumAsync(p => (decimal?)p.MontantTotal) ?? 0;

                // Pending requests requiring attention
                var pendingRequests = await _context.Demandes
                    .Where(d => d.Statut == StatutDemande.EnAttente)
                    .CountAsync();

                // Faveur users stats
                var faveurUsers = await _context.Users.Where(u => u.Is_Faveur).CountAsync();
                var totalUsers = await _context.Users.CountAsync();

                return Ok(new
                {
                    CurrentMonth = new
                    {
                        Demandes = currentMonthDemandes,
                        Spent = currentMonthSpent
                    },
                    LastMonth = new
                    {
                        Demandes = lastMonthDemandes,
                        Spent = lastMonthSpent
                    },
                    Growth = new
                    {
                        Demandes = Math.Round(demandesGrowth, 2),
                        Spent = Math.Round(spentGrowth, 2)
                    },
                    Yearly = new
                    {
                        Demandes = yearlyDemandes,
                        Spent = yearlySpent
                    },
                    PendingRequests = pendingRequests,
                    FaveurUsers = faveurUsers,
                    TotalUsers = totalUsers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur serveur: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        // Simplified export endpoint - no complex filtering required
     /**   [HttpGet("export/excel")]
        public async Task<IActionResult> ExportToExcel(
            [FromQuery] Guid? categorieId,
            [FromQuery] Guid? utilisateurId)
        {
            try
            {
                var query = _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Categorie)
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Include(d => d.Paiement)
                    .AsQueryable();

                // Apply simple filters if provided
                if (categorieId.HasValue && categorieId.Value != Guid.Empty)
                {
                    query = query.Where(d => d.CategorieId == categorieId.Value);
                }

                if (utilisateurId.HasValue && utilisateurId.Value != Guid.Empty)
                {
                    query = query.Where(d => d.UtilisateurId == utilisateurId.Value);
                }

                // Execute query
                var demandes = await query.OrderByDescending(d => d.DateDemande).ToListAsync();

                // Prepare export data
                var exportData = demandes.Select(d => new
                {
                    DateDemande = d.DateDemande.ToString("dd/MM/yyyy HH:mm"),
                    Statut = d.Statut.ToString(),
                    Utilisateur = $"{d.Utilisateur?.Nom ?? ""} {d.Utilisateur?.Prenom ?? ""}",
                    Email = d.Utilisateur?.Email ?? "N/A",
                    EstFaveur = d.Utilisateur?.Is_Faveur == true ? "Oui" : "Non",
                    Categorie = d.Categorie?.Nom ?? "N/A",
                    NombreItems = d.DemandeItems?.Count ?? 0,
                    Items = d.DemandeItems != null && d.DemandeItems.Any()
                        ? string.Join("; ", d.DemandeItems.Select(di =>
                            $"{di.Item?.Nom ?? "Article inconnu"} - Qté: {di.Quantite}" +
                            (di.PrixUnitaire.HasValue ? $" - Prix: {di.PrixUnitaire.Value:F2} TND" : "") +
                            (!string.IsNullOrEmpty(di.Description) ? $" - Desc: {di.Description}" : "")
                          ))
                        : "Aucun article",
                    MontantTotal = d.Paiement?.MontantTotal.ToString("F2") ?? "0.00",
                    MontantEnLettres = d.Paiement?.MontantEnLettres ?? "N/A",
                    ComptePaiement = d.Paiement?.ComptePaiement ?? "N/A",
                    DatePaiement = d.Paiement?.DatePaiement.ToString("dd/MM/yyyy HH:mm") ?? "N/A",
                    EffectuePar = d.Paiement?.EffectuePar ?? "N/A"
                }).ToList();

                // Create filter description
                string filterDescription = "Toutes les demandes";
                if (categorieId.HasValue && categorieId.Value != Guid.Empty)
                {
                    var categorie = await _context.Categories.FindAsync(categorieId.Value);
                    filterDescription = $"Catégorie: {categorie?.Nom ?? "Inconnue"}";
                }
                if (utilisateurId.HasValue && utilisateurId.Value != Guid.Empty)
                {
                    var user = await _context.Users.FindAsync(utilisateurId.Value);
                    filterDescription = $"Utilisateur: {user?.Nom} {user?.Prenom}";
                }

                return Ok(new
                {
                    success = true,
                    data = exportData,
                    totalRecords = exportData.Count,
                    exportDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    exportType = filterDescription,
                    filters = new
                    {
                        CategorieId = categorieId,
                        UtilisateurId = utilisateurId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Erreur serveur lors de l'export: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }
        
*/

      [HttpGet("export/excel")]
        public async Task<IActionResult> ExportToExcel(
            [FromQuery] Guid? categorieId,
            [FromQuery] Guid? utilisateurId,
            [FromQuery] Guid? itemId)
        {
            try
            {
                var query = _context.Demandes
                    .Include(d => d.Utilisateur)
                    .Include(d => d.Categorie)
                    .Include(d => d.DemandeItems)
                        .ThenInclude(di => di.Item)
                    .Include(d => d.Paiement)
                    .AsQueryable();

                // Apply filters if provided
                if (categorieId.HasValue && categorieId.Value != Guid.Empty)
                {
                    query = query.Where(d => d.CategorieId == categorieId.Value);
                }

                if (utilisateurId.HasValue && utilisateurId.Value != Guid.Empty)
                {
                    query = query.Where(d => d.UtilisateurId == utilisateurId.Value);
                }

                // Filter by specific item if provided
                if (itemId.HasValue && itemId.Value != Guid.Empty)
                {
                    query = query.Where(d => d.DemandeItems.Any(di => di.ItemId == itemId.Value));
                }

                // Execute query
                var demandes = await query.OrderByDescending(d => d.DateDemande).ToListAsync();

                // Prepare export data
                var exportData = demandes.Select(d => new
                {
                    DateDemande = d.DateDemande.ToString("dd/MM/yyyy HH:mm"),
                    Statut = d.Statut.ToString(),
                    Utilisateur = $"{d.Utilisateur?.Nom ?? ""} {d.Utilisateur?.Prenom ?? ""}",
                    Email = d.Utilisateur?.Email ?? "N/A",
                    EstFaveur = d.Utilisateur?.Is_Faveur == true ? "Oui" : "Non",
                    Categorie = d.Categorie?.Nom ?? "N/A",
                    NombreItems = d.DemandeItems?.Count ?? 0,
                    Items = d.DemandeItems != null && d.DemandeItems.Any() 
                        ? string.Join("; ", d.DemandeItems
                            .Where(di => !itemId.HasValue || di.ItemId == itemId.Value) // Filter items if specific item selected
                            .Select(di => 
                                $"{di.Item?.Nom ?? "Article inconnu"} - Qté: {di.Quantite}" + 
                                (di.PrixUnitaire.HasValue ? $" - Prix: {di.PrixUnitaire.Value:F2} TND" : "") +
                                (!string.IsNullOrEmpty(di.Description) ? $" - Desc: {di.Description}" : "")
                            ))
                        : "Aucun article",
                    MontantTotal = d.Paiement?.MontantTotal.ToString("F2") ?? "0.00",
                    MontantEnLettres = d.Paiement?.MontantEnLettres ?? "N/A",
                    ComptePaiement = d.Paiement?.ComptePaiement ?? "N/A",
                    DatePaiement = d.Paiement?.DatePaiement.ToString("dd/MM/yyyy HH:mm") ?? "N/A",
                    EffectuePar = d.Paiement?.EffectuePar ?? "N/A"
                }).ToList();

                // Create filter description
                var filterParts = new List<string>();
                
                if (categorieId.HasValue && categorieId.Value != Guid.Empty)
                {
                    var categorie = await _context.Categories.FindAsync(categorieId.Value);
                    filterParts.Add($"Catégorie: {categorie?.Nom ?? "Inconnue"}");
                }
                
                if (utilisateurId.HasValue && utilisateurId.Value != Guid.Empty)
                {
                    var user = await _context.Users.FindAsync(utilisateurId.Value);
                    filterParts.Add($"Utilisateur: {user?.Nom} {user?.Prenom}");
                }
                
                if (itemId.HasValue && itemId.Value != Guid.Empty)
                {
                    var item = await _context.Items.FindAsync(itemId.Value);
                    filterParts.Add($"Article: {item?.Nom ?? "Inconnu"}");
                }

                string filterDescription = filterParts.Any() ? string.Join(", ", filterParts) : "Toutes les demandes";

                return Ok(new
                {
                    success = true,
                    data = exportData,
                    totalRecords = exportData.Count,
                    exportDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    exportType = filterDescription,
                    filters = new
                    {
                        CategorieId = categorieId,
                        UtilisateurId = utilisateurId,
                        ItemId = itemId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false,
                    message = $"Erreur serveur lors de l'export: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }











    }
}